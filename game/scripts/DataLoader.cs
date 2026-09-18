using Godot;
using Overmatch.Sim.Data;

namespace Overmatch.Game;

/// <summary>Reads every JSON file under res://data (works in the editor and in exported .pck builds).</summary>
public static class DataLoader
{
    public static GameRules LoadRules(string root = "res://data")
    {
        var files = new List<DataFile>();
        Walk(root, "", files);
        GD.Print($"[Data] loaded {files.Count} data files from {root}");
        return GameRules.Load(files);
    }

    private static void Walk(string absDir, string relDir, List<DataFile> files)
    {
        using var dir = DirAccess.Open(absDir);
        if (dir is null)
        {
            GD.PushError($"[Data] cannot open {absDir}: {DirAccess.GetOpenError()}");
            return;
        }
        dir.ListDirBegin();
        for (var name = dir.GetNext(); name != ""; name = dir.GetNext())
        {
            if (name.StartsWith('.')) continue;
            var abs = absDir.PathJoin(name);
            var rel = relDir == "" ? name : relDir + "/" + name;
            if (dir.CurrentIsDir())
            {
                Walk(abs, rel, files);
            }
            else if (name.EndsWith(".json"))
            {
                var text = Godot.FileAccess.GetFileAsString(abs);
                files.Add(new DataFile(rel, text));
            }
        }
        dir.ListDirEnd();
    }
}
