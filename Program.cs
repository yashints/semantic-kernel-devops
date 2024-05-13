using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Memory;
using System.CommandLine;
using System.Reflection;
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

      if (kernelSettings.EndpointType == EndpointTypes.TextCompletion)
      {
        var pluginDirectories = Configuration!.GetSection("PluginSettings:Root").Get<string>();

        var skillImport = kernel.ImportPluginFromPromptDirectory(pluginDirectories!);

        // read user's task description from given file
        string description = await File.ReadAllTextAsync(file.FullName!);

        // this is a data structure that holds temporary data while SK-task runs
        var context = new KernelArguments();

        // reference input variable from config.json / skprompt.txt
        // associate user's description with "input" variable
        string key = "input";
        context.Add(key, description);

        // ultimately, execute the appropriate plugin function against the LLM
        var result = await kernel.InvokeAsync(skillImport[function], context);
        Console.WriteLine(result.GetValue<string>());
      }
    }
  }
}