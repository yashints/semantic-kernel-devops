using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SK
{
  class ExtrcatPipelineFromOutput
  {
    [KernelFunction, Description(@"Saves a pipeline to a file. The pipeline is a YAML file.")]
    public static bool Extract(string modelOutPut, string separator, string path)
    {
      Console.WriteLine(modelOutPut);
      try
      {
        var chunks = modelOutPut.Split(separator);

        for (int i = 0; i < chunks.Length; i++)
        {
          if (chunks[i].IndexOf("FILE: ") >= 0)
          {
            var fileName = chunks[i].Split("FILE: ")[1].TrimEnd('\r', '\n');
            SaveFile.SaveContentToFile(chunks[i + 1], Path.Combine(path, fileName));
            i++;
          }
        }

        return true;
      }
      catch (Exception e)
      {
        Console.WriteLine($"Error saving the pipeline: {e.Message}");
        return false;
      }

    }
  }
}