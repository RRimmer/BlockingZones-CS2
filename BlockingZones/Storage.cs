using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace BlockingZones;

// Files (all under counterstrikesharp/configs/plugins/BlockingZones):
//   config.jsonc                        plugin settings (JSON with comments)
//   items.jsonc                         models for "Create item" (JSON with comments)
//   maps/<map>.json                     items of a regular map
//   maps/workshop/<id>/<map>.json       items of a workshop map (like the CS:GO plugin: workshop/<id>/<map>.txt)
public partial class BlockingZonesPlugin
{
    private List<CatalogEntry> _catalog = new();

    // Every item of the current map, spawned this round or not. "Save" writes all of them,
    // so items hidden by the quota are not lost (the CS:GO plugin saved only the spawned entities).
    private readonly List<BzItem> _items = new();
    private string _mapName = "";
    private string _workshopId = "";
    private string _mapFile = "";
    private bool _dirty;

    private string DataDir => Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "configs", "plugins", "BlockingZones"));
    private string ConfigPath => Path.Combine(DataDir, "config.jsonc");
    private string CatalogPath => Path.Combine(DataDir, "items.jsonc");
    private string MapsDir => Path.Combine(DataDir, "maps");

    // ---------------- config.jsonc ----------------

    private void LoadConfig()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            if (File.Exists(ConfigPath))
            {
                Cfg = JsonSerializer.Deserialize<BzConfig>(File.ReadAllText(ConfigPath), Json.Read) ?? new BzConfig();
                Cfg.Normalize();
            }
            else
            {
                Cfg = new BzConfig();
                Cfg.Normalize();
                File.WriteAllText(ConfigPath, Templates.Config(Cfg));
                Logger.LogInformation("Created {Path}", ConfigPath);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("config.jsonc is broken ({Error}): defaults are used until it is fixed", e.Message);
            Cfg = new BzConfig();
            Cfg.Normalize();
        }
    }

    // Writes the values the menu can change (MessageMode, UsePlayers, Players) into config.jsonc.
    // Only the values are replaced, the admin's comments and layout stay. A missing file (or a key that
    // is not in it) is written anew from the commented template.
    internal void SaveConfig()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            if (File.Exists(ConfigPath))
            {
                string text = File.ReadAllText(ConfigPath);
                bool ok = Templates.PatchJsoncValue(ref text, "MessageMode", Cfg.MessageMode.ToString())
                          & Templates.PatchJsoncValue(ref text, "UsePlayers", Cfg.UsePlayers ? "true" : "false")
                          & Templates.PatchJsoncValue(ref text, "Players", Cfg.Players.ToString());
                if (ok)
                {
                    File.WriteAllText(ConfigPath, text);
                    return;
                }
                Logger.LogWarning("config.jsonc misses MessageMode/UsePlayers/Players: the file is rewritten from the template");
            }
            File.WriteAllText(ConfigPath, Templates.Config(Cfg));
        }
        catch (Exception e)
        {
            Logger.LogError("Cannot write {Path}: {Error}", ConfigPath, e.Message);
        }
    }

    // ---------------- items.jsonc ----------------

    private void LoadCatalog()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            if (!File.Exists(CatalogPath))
            {
                File.WriteAllText(CatalogPath, Templates.Items);
                Logger.LogInformation("Created {Path}", CatalogPath);
            }

            _catalog = (JsonSerializer.Deserialize<List<CatalogEntry>>(File.ReadAllText(CatalogPath), Json.Read) ?? new())
                .Where(c => !string.IsNullOrWhiteSpace(c.Model))
                .Select(c => new CatalogEntry
                {
                    Model = c.Model.Trim(),
                    Name = string.IsNullOrWhiteSpace(c.Name) ? ShortModelName(c.Model) : c.Name.Trim()
                })
                .ToList();
        }
        catch (Exception e)
        {
            Logger.LogError("items.jsonc is broken ({Error}): the model list is empty", e.Message);
            _catalog = new List<CatalogEntry>();
        }
    }

    internal string ModelName(string model)
    {
        var entry = _catalog.FirstOrDefault(c => c.Model.Equals(model, StringComparison.OrdinalIgnoreCase));
        return entry?.Name ?? ShortModelName(model);
    }

    private static string ShortModelName(string model)
    {
        string name = Path.GetFileNameWithoutExtension(model.Replace('\\', '/'));
        return string.IsNullOrEmpty(name) ? model : name;
    }

    // ---------------- maps/<map>.json ----------------

    private void LoadMap(string mapName)
    {
        _items.Clear();
        _dirty = false;
        _mapName = mapName ?? "";
        _workshopId = FindWorkshopId(_mapName) ?? "";
        _mapFile = MapFilePath(_mapName, _workshopId);

        string path = _mapFile;
        // A workshop map that was first saved without its id: use the plain file until the next save.
        if (!File.Exists(path) && _workshopId != "")
        {
            string plain = MapFilePath(_mapName, "");
            if (File.Exists(plain)) path = plain;
        }

        if (File.Exists(path))
        {
            try
            {
                foreach (var it in ReadItems(File.ReadAllText(path)))
                {
                    it.Normalize();
                    if (it.Model != "") _items.Add(it);
                }
            }
            catch (Exception e)
            {
                Logger.LogError("{Path} is broken ({Error}): no items on this map", path, e.Message);
            }
        }

        Logger.LogInformation("Map {Map}{Ws}: {Count} items ({File})", _mapName,
            _workshopId == "" ? "" : $" (workshop {_workshopId})", _items.Count, path);
    }

    // { "Map", "WorkshopId", "Items": [...] } or just [...] (hand-written files).
    private static List<BzItem> ReadItems(string json)
    {
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            return doc.RootElement.Deserialize<List<BzItem>>(Json.Read) ?? new();

        return doc.RootElement.Deserialize<MapFile>(Json.Read)?.Items ?? new();
    }

    // Writes every item of the map. Returns the number written or -1 on error.
    internal int SaveMap()
    {
        try
        {
            var file = new MapFile
            {
                Map = _mapName,
                WorkshopId = _workshopId,
                Items = _items.Select(i => new BzItem
                {
                    Model = i.Model,
                    Pos = i.Pos.Select(Round).ToArray(),
                    Ang = i.Ang.Select(Round).ToArray(),
                    Alpha = i.Alpha,
                    Quota = i.Quota
                }).ToList()
            };

            Directory.CreateDirectory(Path.GetDirectoryName(_mapFile)!);
            File.WriteAllText(_mapFile, JsonSerializer.Serialize(file, Json.Write));
            _dirty = false;
            return file.Items.Count;
        }
        catch (Exception e)
        {
            Logger.LogError("Cannot write {Path}: {Error}", _mapFile, e.Message);
            return -1;
        }
    }

    private static float Round(float v) => MathF.Round(v, 2);

    internal string MapFileShort => Path.GetRelativePath(DataDir, _mapFile).Replace('\\', '/');

    private string MapFilePath(string map, string workshopId)
    {
        string file = SafeFileName(LastPart(map)) + ".json";
        return workshopId == ""
            ? Path.Combine(MapsDir, file)
            : Path.Combine(MapsDir, "workshop", SafeFileName(workshopId), file);
    }

    private static string LastPart(string map)
    {
        string[] parts = map.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "unknown" : parts[^1];
    }

    private static string SafeFileName(string name)
    {
        char[] bad = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => bad.Contains(c) ? '_' : c).ToArray();
        string s = new string(chars).Trim();
        return s == "" ? "unknown" : s;
    }

    // ---------------- Workshop ----------------
    // CS2 has no GetCurrentWorkshopMap(). Server.MapName of a workshop map is the vpk name (awp_lego_2),
    // the id is the folder the vpk sits in: …/steamapps/workshop/content/730/<id>/<map>.vpk.
    // A Source 1 style name "workshop/<id>/<map>" is understood as well.
    private string? FindWorkshopId(string map)
    {
        try
        {
            string[] parts = map.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 && parts[0].Equals("workshop", StringComparison.OrdinalIgnoreCase))
                return parts[1];

            string name = LastPart(map);
            foreach (string dir in WorkshopDirs())
            {
                if (!Directory.Exists(dir)) continue;
                foreach (string sub in Directory.EnumerateDirectories(dir))
                {
                    if (File.Exists(Path.Combine(sub, name + ".vpk")))
                        return Path.GetFileName(sub);
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogDebug("Workshop lookup failed: {Error}", e.Message);
        }
        return null;
    }

    private IEnumerable<string> WorkshopDirs()
    {
        string game = Server.GameDirectory; // …/game
        yield return Path.Combine(game, "bin", "linuxsteamrt64", "steamapps", "workshop", "content", "730");
        yield return Path.Combine(game, "bin", "win64", "steamapps", "workshop", "content", "730");
        yield return Path.GetFullPath(Path.Combine(game, "..", "steamapps", "workshop", "content", "730"));
        foreach (string d in Cfg.WorkshopContentDirs)
            if (!string.IsNullOrWhiteSpace(d)) yield return d;
    }

    // ---------------- Precache ----------------
    // prop_dynamic needs the model in the precache list of the map: every model of items.jsonc and of all map files.
    private void OnPrecache(ResourceManifest manifest)
    {
        var models = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in _catalog) models.Add(c.Model);

        try
        {
            if (Directory.Exists(MapsDir))
            {
                foreach (string file in Directory.EnumerateFiles(MapsDir, "*.json", SearchOption.AllDirectories))
                {
                    try
                    {
                        foreach (var it in ReadItems(File.ReadAllText(file)))
                            if (!string.IsNullOrWhiteSpace(it.Model)) models.Add(it.Model);
                    }
                    catch
                    {
                        // a broken file is reported when its map loads
                    }
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning("Precache scan of {Dir} failed: {Error}", MapsDir, e.Message);
        }

        foreach (string m in models) manifest.AddResource(m);
        Logger.LogInformation("Precached {Count} models", models.Count);
    }
}
