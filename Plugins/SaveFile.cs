using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SK
{
  class SaveFile
  {
    public static bool SaveContentToFile(string content, string path)
    {
      try
      {
        File.WriteAllText(path, content);
        return true;
      }
      catch (Exception e)
      {
        Console.WriteLine($"Error saving pipeline to file: {e.Message}");
        return false;
      }
    }
  }
}