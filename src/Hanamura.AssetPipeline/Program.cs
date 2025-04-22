using System.Text;
using System.Text.Json;

namespace Hanamura.AssetPipeline;

public record AssetManifest(AssetDirectory[] Directories);
public record struct AssetDirectory(string Directory, AssetEntry[] Entries);
public record struct AssetEntry(ulong Hash, string Name);

public class AssetType(string directory, string extension, Action<AssetType, FileInfo> processMethod)
{
    public string Directory { get; } = directory;
    public string Extension { get; } = extension;
    public readonly List<AssetEntry> Entries = [];

    public void Process(FileInfo file)
    {
        processMethod(this, file);
    }
}

internal static class Program
{
    private static readonly ManualResetEvent QuitEvent = new(false);

    private static readonly List<AssetType> AssetTypes = [
        new("textures", ".png", ProcessBasicFile),
        new("shaders", ".hlsl", ProcessBasicFile),
        new("meshes", ".glb", ProcessMesh)
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true
    };
    
    private static FileSystemWatcher _watcher = null!;
    private static DirectoryInfo _sourceDir = null!;
    private static DirectoryInfo _outputDir = null!;
    
    public static void Main(string[] args)
    {
        Console.WriteLine("Asset Pipeline is running! Press Ctrl+C to exit.");

        Console.CancelKeyPress += (_, eArgs) =>
        {
            Console.WriteLine("Exiting...");
            QuitEvent.Set();
            eArgs.Cancel = true;
        };

        _sourceDir = Directory.CreateDirectory(args[0]);
        _outputDir = Directory.CreateDirectory(args[1]);

        LoadManifest();
        foreach (var assetType in AssetTypes)
        {
            InitDirectory(assetType);
        }
        RemoveMissingEntries();
        SaveManifest();
        
        _watcher = new FileSystemWatcher(_sourceDir.FullName);
        _watcher.IncludeSubdirectories = true;
        _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
        _watcher.Changed += (_, eArgs) =>
        {
            if (Directory.Exists(eArgs.FullPath)) return;
            if (!File.Exists(eArgs.FullPath)) return;

            try
            {
                Console.WriteLine($"Modified: {eArgs.Name}");
                var file = new FileInfo(eArgs.FullPath);
                LoadFile(file);
                SaveManifest();
            }
            catch
            {
                Console.WriteLine("Waiting for file lock");
            }
        };
        _watcher.Renamed += (_, eArgs) =>
        {
            Console.WriteLine($"Renamed: {eArgs.OldName} - {eArgs.Name}");
            UpdateManifestEntry(eArgs.OldName!, eArgs.Name!);
            SaveManifest();
        };

        _watcher.EnableRaisingEvents = true;
        QuitEvent.WaitOne();

        _watcher.Dispose();
        Console.WriteLine("Bye!");
    }
    
    private static void LoadFile(FileInfo file)
    {
        var directory = Path.GetFileName(file.DirectoryName);
        AssetTypes.First(x => x.Directory == directory).Process(file);
    }

    private static void AddManifestEntry(FileInfo file)
    {
        var directory = Path.GetFileName(file.DirectoryName);
        var assetType = AssetTypes.First(x => x.Directory == directory);
        var name = Path.GetFileNameWithoutExtension(file.Name);
        if (assetType.Entries.All(x => x.Name != name))
        {
            assetType.Entries.Add(new AssetEntry(HashFNV1A(), name));
        }
    }
    
    private static void RemoveMissingEntries()
    {
        foreach (var assetType in AssetTypes)
        {
            foreach (var entry in assetType.Entries)
            {
                var extension = AssetTypes.First(x => x.Directory == assetType.Directory).Extension;
                if (!File.Exists(Path.Join(_sourceDir.FullName, assetType.Directory, entry.Name) + extension))
                {
                    Console.WriteLine($"Missing: {entry.Name}");
                }
            }
        }
    }
    
    private static void UpdateManifestEntry(string oldPath, string newPath)
    {
        var assetType = AssetTypes.First(x => x.Directory == Path.GetDirectoryName(oldPath));
        var index = assetType.Entries.FindIndex(x => x.Name == Path.GetFileNameWithoutExtension(oldPath));
        if (index == -1) return;
        var entry = assetType.Entries[index];
        entry.Name = Path.GetFileNameWithoutExtension(newPath);
        assetType.Entries[index] = entry;
        LoadFile(new FileInfo(Path.Join(_sourceDir.FullName, newPath)));
    }

    private static void InitDirectory(AssetType assetType)
    {
        var copySource = _sourceDir.CreateSubdirectory(assetType.Directory);

        foreach (var file in copySource.EnumerateFiles())
        {
            assetType.Process(file);
        }
    }

    private static void ProcessBasicFile(AssetType assetType, FileInfo file)
    {
        AddManifestEntry(file);
        var copyOutput = _outputDir.CreateSubdirectory(assetType.Directory);
        File.Copy(file.FullName, Path.Combine(copyOutput.FullName, file.Name), true);
    }
    
    private static void ProcessMesh(AssetType assetType, FileInfo file)
    {
        AddManifestEntry(new FileInfo(Path.ChangeExtension(file.FullName, ".json")));
        var copyOutput = _outputDir.CreateSubdirectory(assetType.Directory);
        var meshes = GLTFLoader.Load(file.FullName);
        var json = JsonSerializer.Serialize(meshes, JsonOptions);
        File.WriteAllText(Path.Combine(copyOutput.FullName, Path.GetFileNameWithoutExtension(file.Name) + ".json"), json);
    }

    private static void LoadManifest()
    {
        var path = Path.Combine(_outputDir.FullName, "asset_manifest.json");
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path);
        var manifest = JsonSerializer.Deserialize<AssetManifest>(json)!;
        foreach (var directory in manifest.Directories)
        {
            var entries = AssetTypes.First(x => x.Directory == directory.Directory);
            entries.Entries.AddRange(directory.Entries);
        }
    }
    
    private static void SaveManifest()
    {
        var assetDirectories = new AssetDirectory[AssetTypes.Count];
        for (var i = 0; i < AssetTypes.Count; i++)
        {
            var assetType = AssetTypes[i];
            assetDirectories[i].Directory = assetType.Directory;
            assetDirectories[i].Entries = assetType.Entries.ToArray();
        }
        var manifest = new AssetManifest(assetDirectories);
        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        File.WriteAllText(Path.Combine(_outputDir.FullName, "asset_manifest.json"), json);
        GenerateAssetClasses();
    }

    private static void GenerateAssetClasses()
    {
        var builders = new StringBuilder[AssetTypes.Count];
        for (var i = 0; i < AssetTypes.Count; i++)
        {
            builders[i] = new StringBuilder();
            foreach (var entry in AssetTypes[i].Entries)
            {
                AddAssetLine(entry, builders[i]);
            }
        }

        var body = new StringBuilder();
        body.AppendLine("// ReSharper disable InconsistentNaming");
        body.AppendLine("namespace Hanamura;");
        body.AppendLine();
        body.AppendLine("public static class Assets");
        body.AppendLine("{");
        for (var i = 0; i < AssetTypes.Count; i++)
        {
            var directory = AssetTypes[i].Directory;
            var builder = builders[i];
            body.Append(GenerateClass(directory, builder));
            if (i != AssetTypes.Count - 1) body.AppendLine();
        }
        body.AppendLine("}");

        File.WriteAllText("src/Hanamura/Assets.cs", body.ToString());
        return;
        StringBuilder GenerateClass(string directory, StringBuilder lines)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"\tpublic static class {char.ToUpper(directory[0]) + directory[1..]}");
            builder.AppendLine("\t{");
            builder.Append(lines);
            builder.AppendLine("\t}");
            return builder;
        }

        void AddAssetLine(AssetEntry entry, StringBuilder builder)
        {
            var name = entry.Name
                .Replace('.', '_')
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty);
            builder.AppendLine($"\t\tpublic const ulong {name} = {entry.Hash};");
        }
    }
    
    private static ulong HashFNV1A()
    {
        var bytes = Guid.NewGuid().ToByteArray();
        const ulong fnv64Offset = 14695981039346656037;
        const ulong fnv64Prime = 0x100000001b3;
        var hash = fnv64Offset;

        foreach (var b in bytes)
        {
            hash ^= b;
            hash *= fnv64Prime;
        }

        return hash;
    }
}