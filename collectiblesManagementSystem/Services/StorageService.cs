using System.Diagnostics;

namespace collectiblesManagementSystem.Services;

public class StorageService
{
    private static string FilePath => Path.Combine(FileSystem.AppDataDirectory, "collectibles_data.txt");

    public static void LogPath()
    {
        Debug.WriteLine($"\n[DATA PATH]: {FilePath}\n");
    }

    public static void SaveAll(List<Models.Collection> collections)
    {
        var lines = collections.Select(c => c.Serialize()).ToArray();
        File.WriteAllLines(FilePath, lines);
    }

    public static List<Models.Collection> LoadAll()
    {
        if (!File.Exists(FilePath)) return new List<Models.Collection>();
        
        var lines = File.ReadAllLines(FilePath);
        return lines.Select(Models.Collection.Deserialize).ToList();
    }
}