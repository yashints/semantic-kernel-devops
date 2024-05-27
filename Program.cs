using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.CommandLine;
namespace SK
{
  class Program
  {
    private static IConfiguration? Configuration { get; set; }
    public static async Task Main(string[] args)
    {
      string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
      var configBuilder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFiles(Directory.GetCurrentDirectory(), "Config/appsettings.json", optional: false, reloadOnChange: true)
              .AddJsonFiles(Directory.GetCurrentDirectory(), $"Config/appsettings.{env}.json", optional: false, reloadOnChange: true);

      Configuration = configBuilder.Build();

      var fileOption = new Option<FileInfo>(
             new[] { "--input", "-i" },
             "Path to the input file to be processed");

      var functionOption = new Option<string>(
          new[] { "--function", "-f" },
          "The function to be executed.");

      var rootCommand = new RootCommand
        {
            fileOption,
            functionOption
        };
      rootCommand.SetHandler(
             Run,
             fileOption, functionOption
         );

      await rootCommand.InvokeAsync(args);
    }

    private static async Task Run(FileInfo file, string function)
    {
      var kernelSettings = KernelSettings.LoadSettings();


      var builder = Kernel.CreateBuilder()
          .AddAzureOpenAIChatCompletion(kernelSettings.ChatModelId!, kernelSettings.Endpoint!, kernelSettings.ApiKey!)
          .AddAzureOpenAITextEmbeddingGeneration(kernelSettings.EmbeddingModelId!, kernelSettings.Endpoint!, kernelSettings.ApiKey!);

      builder.Services.AddLogging(c =>
        c.AddDebug()
        .AddConsole()
        .SetMinimumLevel(kernelSettings.LogLevel ?? LogLevel.Warning));

      var kernel = builder.Build();

      kernel.ImportPluginFromType<ExtrcatPipelineFromOutput>();

      if (kernelSettings.EndpointType == EndpointTypes.TextCompletion)
      {
        var pluginDirectories = Configuration!.GetSection("PluginSettings:Root").Get<string>();
        var planTemplatePath = Configuration!.GetValue<string>("PlanPath")!;
        var pipelineDir = Configuration!.GetValue<string>("PipelineDir")!;

        var skillImport = kernel.ImportPluginFromPromptDirectory(pluginDirectories!);

        // read user's task description from given file
        string description = await File.ReadAllTextAsync(file.FullName!);

        // reference input variable from config.json / skprompt.txt
        // associate user's description with "input" variable
        var initialArguments = new KernelArguments()
        {
          { "input", description },
          { "path", pipelineDir },
          { "function", function },
          { "separator", kernelSettings.Separator }
        };

        var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions() { AllowLoops = true });
        string planTemplate = "";
        HandlebarsPlan plan;
        string ask = @$"Given the provided function use the proper prompt to generate the output
          with provided input, then save them in the provided directory ${pipelineDir}";

        if (File.Exists(planTemplatePath))
        {
          planTemplate = await File.ReadAllTextAsync(planTemplatePath);
          plan = new HandlebarsPlan(planTemplate);
        }
        else
        {
          plan = await planner.CreatePlanAsync(kernel, ask, initialArguments);
          Console.WriteLine(plan.ToString());
          SaveFile.SaveContentToFile(plan.ToString(), planTemplatePath);
        }

        var result = await plan.InvokeAsync(kernel, initialArguments);

        Console.WriteLine(result);
      }
    }
  }
}