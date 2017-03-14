using System.IO;

public class ModManager
{
    public DirectoryInfo[] ModDirectories { get; private set; }
    public ModManager(string dataPath)
    {
        ModDirectories = new DirectoryInfo(Path.Combine(dataPath, "Mods")).GetDirectories();
    }
}
