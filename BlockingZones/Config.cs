using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace BlockingZones;

// configs/plugins/BlockingZones/config.jsonc (analog of the CS:GO ConVars).
public sealed class BzConfig
{
    // Default quota for NEW items (2..64). An item is spawned while alive players < its quota.
    public int Players { get; set; } = 10;

    // 0 off, 1 chat, 2 Panorama banner (bz layout), 3 chat + Panorama.
    public int MessageMode { get; set; } = 1;

    // false: no player counting, every item is spawned every round; Players and MessageMode are inactive.
    public bool UsePlayers { get; set; } = true;

    // Bots count as players (the CS:GO plugin counted every client in game).
    public bool CountBots { get; set; } = true;

    // Who may use css_bz / css_bz_reload / preview commands.
    public string AdminFlag { get; set; } = "@css/root";

    // Alpha of a freshly placed item (0..255).
    public int DefaultAlpha { get; set; } = 255;

    // Besides m_nRenderMode + m_clrRender also fire the entity input "Alpha".
    public bool UseAlphaInput { get; set; } = true;

    // Compiled panorama of the plugin in the client addon (Content-addonmanager -> build-addon.cmd).
    public string LayoutPath { get; set; } = "panorama/layout/custom_game/bz.vxml_c";

    // Seconds the "part of the map is closed" banner stays on screen (MessageMode 2 / 3).
    public float NoticeSeconds { get; set; } = 7f;

    // Extra folders with workshop content (…/steamapps/workshop/content/730). The usual ones are checked anyway.
    public List<string> WorkshopContentDirs { get; set; } = new();

    // Steps of the item settings, cycled with key 7; the first one is used when the screen opens.
    public List<float> MoveSteps { get; set; } = new() { 10, 50, 1 };   // units (1 ≈ 2.54 cm)
    public List<float> RotateSteps { get; set; } = new() { 50, 10, 1 }; // degrees

    public PreviewConfig Preview { get; set; } = new();

    private static List<float> CleanSteps(List<float>? steps, float max, List<float> fallback)
    {
        var res = (steps ?? new List<float>()).Where(v => v > 0 && v <= max).Distinct().Take(8).ToList();
        return res.Count == 0 ? fallback : res;
    }

    public void Normalize()
    {
        Players = Math.Clamp(Players, 2, 64);
        MessageMode = Math.Clamp(MessageMode, 0, 3);
        DefaultAlpha = Math.Clamp(DefaultAlpha, 0, 255);
        AdminFlag ??= "@css/root";
        if (string.IsNullOrWhiteSpace(LayoutPath)) LayoutPath = "panorama/layout/custom_game/bz.vxml_c";
        NoticeSeconds = Math.Clamp(NoticeSeconds, 2f, 30f);
        WorkshopContentDirs ??= new List<string>();
        MoveSteps = CleanSteps(MoveSteps, 1000f, new List<float> { 10, 50, 1 });
        RotateSteps = CleanSteps(RotateSteps, 180f, new List<float> { 50, 10, 1 });
        Preview ??= new PreviewConfig();
        Preview.Normalize();
    }
}

public sealed class PreviewConfig
{
    // Seconds between ghost updates (0.02..0.5).
    public float Interval { get; set; } = 0.05f;

    // Ghost alpha and tint (R, G, B).
    public int Alpha { get; set; } = 100;
    public int[] Color { get; set; } = { 120, 255, 120 };

    // Glow outline on the ghost.
    public bool Glow { get; set; } = true;

    // Only the admin who places the item sees the ghost (CheckTransmit).
    public bool HideFromOthers { get; set; } = true;

    // After 1 (place) / 0 (cancel) open the model list again.
    public bool ReopenMenu { get; set; } = true;

    // Degrees per rotation key (2 / 3 / 4).
    public float RotateStep { get; set; } = 45f;

    public void Normalize()
    {
        Interval = Math.Clamp(Interval, 0.02f, 0.5f);
        Alpha = Math.Clamp(Alpha, 0, 255);
        if (Color is not { Length: 3 }) Color = new[] { 120, 255, 120 };
        for (int i = 0; i < 3; i++) Color[i] = Math.Clamp(Color[i], 0, 255);
        if (RotateStep <= 0 || RotateStep > 180) RotateStep = 45f;
    }
}

// items.jsonc: models offered in "Create item".
public sealed class CatalogEntry
{
    public string Name { get; set; } = "";
    public string Model { get; set; } = "";
}

// One item of maps/<map>.json.
public sealed class BzItem
{
    public string Model { get; set; } = "";
    public float[] Pos { get; set; } = new float[3];
    public float[] Ang { get; set; } = new float[3];
    public int Alpha { get; set; } = 255;
    public int Quota { get; set; } = 10;

    // Prop of the current round (null when not spawned).
    [JsonIgnore] public CBaseModelEntity? Entity { get; set; }

    public void Normalize()
    {
        if (Pos is not { Length: 3 }) Pos = new float[3];
        if (Ang is not { Length: 3 }) Ang = new float[3];
        Alpha = Math.Clamp(Alpha, 0, 255);
        Quota = Math.Clamp(Quota, 2, 64);
        Model ??= "";
    }
}

public sealed class MapFile
{
    public string Map { get; set; } = "";
    public string WorkshopId { get; set; } = "";
    public List<BzItem> Items { get; set; } = new();
}

internal static class Json
{
    public static readonly JsonSerializerOptions Read = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true
    };

    public static readonly JsonSerializerOptions Write = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
