using System.Numerics;
using System.Text.Json;
using Hanamura.Graphics;
using JetBrains.Annotations;
using MoonWorks.Graphics;
using MoonWorks.Storage;
using SDL3;

namespace Hanamura.AssetManagement;

public static class AssetManager
{
    public record ContentManifest(ContentDirectory[] Directories);
    public record struct ContentDirectory(string Directory, ContentEntry[] Entries);
    public record struct ContentEntry(ulong Hash, string Name);
    
    [UsedImplicitly]
    private record Mesh(
        string Name,
        Vector3 Position,
        Quaternion Rotation,
        Vector3 Scale,
        Vertex[] Vertices,
        uint[] Indices,
        int[] Children);
    
    private static TitleStorage _storage = null!;
    private static GraphicsDevice _graphicsDevice = null!;
    private static FileSystemWatcher _watcher = null!;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true
    };
    
    private static readonly Dictionary<ulong, TextureRef> TextureMap = new();
    private static readonly Dictionary<ulong, ShaderRef> ShaderMap = new();
    private static readonly Dictionary<ulong, MeshRef> MeshMap = new();
    private static readonly Dictionary<ulong, MaterialRef> MaterialMap = new();
    private static readonly List<string> ReloadedList = [];
    
    public static void Init(TitleStorage storage, GraphicsDevice graphicsDevice)
    {
        _storage = storage;
        _graphicsDevice = graphicsDevice;

        var manifestData = ReadFile("assets/asset_manifest.json");
        var manifest = JsonSerializer.Deserialize<ContentManifest>(manifestData)!;

        foreach (var directory in manifest.Directories)
        {
            foreach (var entry in directory.Entries)
            {
                if (directory.Directory == "textures")
                {
                    LoadTexture(entry.Name, entry.Hash);
                }
                else if (directory.Directory == "shaders")
                {
                    LoadShader(entry.Name, entry.Hash);
                }
                else if (directory.Directory == "meshes")
                {
                    LoadMesh(entry.Name, entry.Hash);
                }
            }
        }

        _watcher = new FileSystemWatcher("assets");
        _watcher.IncludeSubdirectories = true;
        _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
        _watcher.Changed += (_, eArgs) =>
        {
            if (eArgs.Name! == "asset_manifest.json") return;
            if (!ReloadedList.Contains(eArgs.Name!)) ReloadedList.Add(eArgs.Name!);
        };
        _watcher.EnableRaisingEvents = true;
    }

    public static void CheckForReloadedAssets()
    {
        for (var i = 0; i < ReloadedList.Count; i++)
        {
            var asset = ReloadedList[i];
            try
            {
                var name = Path.GetFileNameWithoutExtension(asset);
                var directory = Path.GetDirectoryName(asset);
                var manifestData = ReadFile("assets/asset_manifest.json");
                var newManifest = JsonSerializer.Deserialize<ContentManifest>(manifestData)!;
                var entry = newManifest.Directories.SelectMany(x => x.Entries).FirstOrDefault(x => x.Name == name);
                if (entry == default) continue;
                if (directory == "textures")
                {
                    LoadTexture(name, entry.Hash);
                }
                else if (directory == "shaders")
                {
                    LoadShader(name, entry.Hash);
                }
                else if (directory == "meshes")
                {
                    LoadMesh(name, entry.Hash);
                }
                ReloadedList.RemoveAt(i);
                i--;
            }
            catch
            {
                Console.WriteLine($"Waiting for file lock on {asset}");
            }
        }
    }

    public static MaterialRef RegisterMaterial(GraphicsPipelineCreateInfo createInfo, ulong vertexShader, ulong fragmentShader)
    {
        var vert = ShaderMap[vertexShader];
        var frag = ShaderMap[fragmentShader];
        createInfo.VertexShader = vert;
        createInfo.FragmentShader = frag;
        var material = new Material
        {
            Pipeline = GraphicsPipeline.Create(_graphicsDevice, createInfo)
        };
        var materialRef = new MaterialRef(material, createInfo);
        MaterialMap.Add(vertexShader, materialRef);
        MaterialMap.Add(fragmentShader, materialRef);
        return materialRef;
    }

    public static TextureRef GetTexture(ulong id) => TextureMap[id];
    public static MeshRef GetMesh(ulong id) => MeshMap[id];

    private static void LoadTexture(string name, ulong hash)
    {
        var resourceUploader = new ResourceUploader(_graphicsDevice);
        var compressedImageData = ReadFile($"assets/textures/{name}.png");
        ImageUtils.ImageInfoFromBytes(compressedImageData, out var width, out var height, out var _);
        var mipLevels = (uint)Math.Floor(Math.Log2(Math.Max(width, height))) + 1;
        var texture = Texture.Create2D(_graphicsDevice, name, width, height, TextureFormat.R8G8B8A8Unorm, TextureUsageFlags.Sampler, mipLevels);
        resourceUploader.SetTextureDataFromCompressed(
            new TextureRegion
            {
                Texture = texture.Handle,
                W = width,
                H = height,
                D = 1,
            },
            compressedImageData
        );
        resourceUploader.Upload();
        resourceUploader.Dispose();

        var cmdBuf = _graphicsDevice.AcquireCommandBuffer();
        SDL.SDL_GenerateMipmapsForGPUTexture(cmdBuf.Handle, texture.Handle);
        _graphicsDevice.Submit(cmdBuf);
        
        if (TextureMap.TryGetValue(hash, out var existingRef))
        {
            existingRef.Update(texture);
        }
        else
        {
            TextureMap.Add(hash, new TextureRef(texture));
        }
    }

    private static void LoadShader(string name, ulong hash)
    {
        var shader = ShaderCross.Create(
            _graphicsDevice,
            _storage,
            $"assets/shaders/{name}.hlsl",
            "main",
            ShaderCross.ShaderFormat.HLSL,
            name.Contains("vert")?ShaderStage.Vertex:ShaderStage.Fragment
        );
        if (ShaderMap.TryGetValue(hash, out var existingRef))
        {
            existingRef.Update(shader);
            if (MaterialMap.TryGetValue(hash, out var materialRef))
            {
                if (name.Contains("vert"))
                {
                    materialRef.CreateInfo = materialRef.CreateInfo with { VertexShader = shader };
                }
                else
                {
                    materialRef.CreateInfo = materialRef.CreateInfo with { FragmentShader = shader };
                }
                materialRef.Update(new Material
                {
                    Pipeline = GraphicsPipeline.Create(_graphicsDevice, materialRef.CreateInfo)
                });
            }
        }
        else
        {
            ShaderMap.Add(hash, new ShaderRef(shader));
        }
    }

    private static void LoadMesh(string name, ulong hash)
    {
        var resourceUploader = new ResourceUploader(_graphicsDevice);
        var meshData = JsonSerializer.Deserialize<Mesh[]>(ReadFile($"assets/meshes/{name}.json"), JsonOptions)!;
        var vertexBuffer = resourceUploader.CreateBuffer<Vertex>(meshData[0].Vertices, BufferUsageFlags.Vertex);
        var indexBuffer = resourceUploader.CreateBuffer<uint>(meshData[0].Indices, BufferUsageFlags.Index);
        resourceUploader.Upload();
        resourceUploader.Dispose();
        var meshRef = new MeshRef(new AssetManagement.Mesh(vertexBuffer, indexBuffer));
        if (MeshMap.TryGetValue(hash, out var existingRef))
        {
            existingRef.Update(meshRef);
        }
        else
        {
            MeshMap.Add(hash, meshRef);
        }
    }

    private static Span<byte> ReadFile(string path)
    {
        _storage.GetFileSize(path, out var size);
        var bytes = new byte[size];
        _storage.ReadFile(path, bytes);
        return bytes;
    }
}