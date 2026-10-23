using System.Globalization;
using System.Runtime.CompilerServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using MenuManager;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace BlockingZones;

// BlockingZones for CS2 / CounterStrikeSharp. Author: Rimmer.
// Menus: own "bz" panorama (custom_hud_layout), number keys only. The keys come from PanoramaMenuManager
// (PMM core >= 1.3.00, IMenuApi "menu:nfcore" NumberKeyPressed: the player's css_1..css_0 binds).
[MinimumApiVersion(374)] // Trace API: CSS 1.0.372, CustomHudLayout API: CSS 1.0.374
public partial class BlockingZonesPlugin : BasePlugin
{
    public override string ModuleName => "BlockingZones";
    public override string ModuleVersion => "0.1.0";
    public override string ModuleAuthor => "Rimmer";
    public override string ModuleDescription => "Blocks parts of the map with props while there are not enough alive players";

    private static readonly PluginCapability<IMenuApi?> MenuCapability = new("menu:nfcore");
    private IMenuApi? _menu;
    private bool _keysHooked;

    internal BzConfig Cfg = new();

    private Timer? _previewTimer;
    private float _previewInterval;

    // Round state: props are spawned once per round (freeze end, or the fallback timer).
    private int _roundToken;
    private bool _spawnedThisRound;

    public override void Load(bool hotReload)
    {
        LoadConfig();
        LoadCatalog();

        RegisterListener<Listeners.OnServerPrecacheResources>(OnPrecache);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);

        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);

        RestartPreviewTimer();

        if (hotReload)
        {
            RemoveOrphanLayouts();
            AddTimer(0.5f, () => EnsureLayout());
            LoadMap(Server.MapName);
            Logger.LogInformation("Hot reload: items of {Map} loaded, they appear next round. New models need a map change (precache)", _mapName);
        }
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        AttachMenu();
    }

    public override void Unload(bool hotReload)
    {
        foreach (int slot in _previews.Keys.ToList()) StopPreview(slot, false);
        KillSpawned();
        _previewTimer?.Kill();
        try { UnhookKeys(); } catch { /* older core */ }
        KillLayout();
    }

    private void AttachMenu()
    {
        try { _menu = MenuCapability.Get(); }
        catch { _menu = null; }

        if (_menu == null)
        {
            Logger.LogError("PanoramaMenuManager (capability menu:nfcore) not found. Install PMM core >= 1.3.00: css_bz will not open");
            return;
        }
        // An older core (< 1.3.00) has no NumberKeyPressed: the JIT fails on HookKeys itself.
        try { HookKeys(); }
        catch (Exception e) { Logger.LogError("PMM core >= 1.3.00 is required ({Error})", e.Message); }
    }

    // Number keys 1..9, 0 of every player (css_N from the PMM binds), also when no PMM menu is open.
    // 1.3.00 API calls live in NoInlining helpers: with an older core only the helper fails, not its caller.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void HookKeys()
    {
        if (_keysHooked || _menu == null) return;
        try
        {
            _menu.NumberKeyPressed += OnNumberKey;
            _keysHooked = true;
        }
        catch (Exception e)
        {
            Logger.LogError("PMM NumberKeyPressed not available ({Error}): PMM core >= 1.3.00 is required", e.Message);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UnhookKeys()
    {
        if (!_keysHooked || _menu == null) return;
        try { _menu.NumberKeyPressed -= OnNumberKey; }
        catch { /* core already unloaded */ }
        _keysHooked = false;
    }

    // ---------------- Commands ----------------

    [ConsoleCommand("css_bz", "BlockingZones: admin menu")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCmdBz(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid) return;
        if (!IsAdmin(player))
        {
            Chat(player, T(player, "msg.no_access"));
            return;
        }

        if (_menu == null) AttachMenu();
        if (_menu == null || !_keysHooked)
        {
            Chat(player, T(player, "msg.pmm_missing"));
            return;
        }

        // css_bz is a toggle: a running preview or an open menu is closed.
        if (_previews.ContainsKey(player.Slot))
        {
            StopPreview(player.Slot, false);
            return;
        }
        if (IsViewOpen(player))
        {
            CloseView(player);
            return;
        }

        // Our menu and a PMM menu would both take the digits.
        try { _menu.CloseMenu(player); } catch { /* nothing open */ }

        bool bound = true;
        try { bound = CheckBinds(player); }
        catch { /* older core: no key check */ }

        OpenView(player, Screen.Main, 0, bound ? "" : T(player, "hud.binds"), bound ? 0 : 2);
    }

    // Not through the PMM number keys check yet: print the bind line into the player's console.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool CheckBinds(CCSPlayerController player)
    {
        if (_menu == null || _menu.HasNumberKeysChecked(player)) return true;
        _menu.PrintNumberKeysBinds(player);
        return false;
    }

    [ConsoleCommand("css_bz_reload", "BlockingZones: reload config.jsonc and items.jsonc")]
    public void OnCmdReload(CCSPlayerController? player, CommandInfo info)
    {
        if (player != null && !IsAdmin(player))
        {
            Chat(player, T(player, "msg.no_access"));
            return;
        }

        LoadConfig();
        LoadCatalog();
        RestartPreviewTimer();

        string text = T(player, "msg.reloaded", ("COUNT", _catalog.Count));
        if (player == null) Server.PrintToConsole("[BlockingZones] " + StripColors(text));
        else Chat(player, text);
    }

    internal bool IsAdmin(CCSPlayerController player)
    {
        if (string.IsNullOrWhiteSpace(Cfg.AdminFlag)) return true;
        return AdminManager.PlayerHasPermissions(player, Cfg.AdminFlag);
    }

    // ---------------- Map ----------------

    private void OnMapStart(string mapName)
    {
        _previews.Clear();
        _editTarget.Clear();
        Array.Clear(_views);
        ForgetLayout();
        AddTimer(1.0f, () => EnsureLayout());
        _roundToken++;
        _spawnedThisRound = false;
        LoadMap(mapName);
    }

    private void OnMapEnd()
    {
        if (_dirty)
            Logger.LogWarning("Map {Map} ends with unsaved BlockingZones changes: they are lost (use \"Save\" in css_bz)", _mapName);

        // Entities die with the map: forget the references.
        foreach (var it in _items) it.Entity = null;
        _previews.Clear();
        _editTarget.Clear();
        Array.Clear(_views);
        ForgetLayout();
    }

    private void OnClientDisconnect(int slot)
    {
        if (slot is < 0 or >= 64) return;
        if (_previews.Remove(slot, out var pv) && pv.Ghost != null && pv.Ghost.IsValid) pv.Ghost.AcceptInput("Kill");
        _views[slot] = null;
        _editTarget.Remove(slot);
    }

    // The slot may carry the previous occupant's per-player HUD state: write the authored state.
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        AddTimer(0.5f, () =>
        {
            if (player == null || !player.IsValid || player.IsBot) return;
            if (EnsureLayout()) ResetPlayerHud(player);
        });
        return HookResult.Continue;
    }

    // ---------------- Round ----------------
    // round_freeze_end is the moment the CS:GO plugin used: everybody who plays this round has spawned,
    // so "alive T + CT" is final. In CS2 the event is raised as well (also with mp_freezetime 0).
    // Fallback: round_start arms a timer for mp_freezetime + 1 s; if freeze_end did not come by then
    // (custom game modes, plugins that skip it), the props are spawned from there. A round token and
    // the "spawned" flag make sure it happens exactly once per round.

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _roundToken++;
        _spawnedThisRound = false;
        KillSpawned();
        // A round restart can delete the layout entity: have it back (and redraw open menus).
        AddTimer(0.3f, () => EnsureLayout());

        int token = _roundToken;
        float delay = GetFreezeTime() + 1.0f;
        AddTimer(delay, () =>
        {
            if (token != _roundToken || _spawnedThisRound) return;
            Logger.LogDebug("round_freeze_end did not come within {Delay}s: spawning from the fallback timer", delay);
            SpawnRound();
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    private HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
    {
        if (!_spawnedThisRound) SpawnRound();
        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player != null && player.IsValid && _previews.ContainsKey(player.Slot))
            StopPreview(player.Slot, false);
        return HookResult.Continue;
    }

    private void SpawnRound()
    {
        _spawnedThisRound = true;
        KillSpawned();

        if (IsWarmup()) return;
        if (_items.Count == 0) return;

        int alive = Cfg.UsePlayers ? CountAlivePlayers() : 0;
        int spawned = 0, maxQuota = 0;

        foreach (var it in _items)
        {
            if (Cfg.UsePlayers && it.Quota <= alive) continue;
            if (SpawnItem(it))
            {
                spawned++;
                maxQuota = Math.Max(maxQuota, it.Quota);
            }
        }

        Logger.LogInformation("Round: alive {Alive}, items {Spawned}/{Total} spawned (UsePlayers {Use})",
            alive, spawned, _items.Count, Cfg.UsePlayers);

        if (spawned > 0 && Cfg.UsePlayers) Announce(maxQuota, alive, spawned);
    }

    // Only ALIVE players of T and CT (as the CS:GO plugin: IsPlayerAlive + team 2/3).
    private int CountAlivePlayers()
    {
        int count = 0;
        foreach (var p in Utilities.GetPlayers())
        {
            if (p == null || !p.IsValid || p.IsHLTV) continue;
            if (p.IsBot && !Cfg.CountBots) continue;
            if (p.Team != CsTeam.Terrorist && p.Team != CsTeam.CounterTerrorist) continue;
            if (!p.PlayerPawn.IsValid) continue;
            var pawn = p.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;
            if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
            count++;
        }
        return count;
    }

    // GameRules_GetProp("m_bWarmupPeriod") of the CS:GO plugin = CCSGameRules.m_bWarmupPeriod via cs_gamerules.
    internal static bool IsWarmup()
    {
        var rules = GetGameRules();
        return rules != null && rules.WarmupPeriod;
    }

    internal static CCSGameRules? GetGameRules()
    {
        try
        {
            return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
        }
        catch
        {
            return null;
        }
    }

    private static float GetFreezeTime()
    {
        try
        {
            var cv = ConVar.Find("mp_freezetime");
            if (cv == null) return 15f;
            try { return Math.Max(0, cv.GetPrimitiveValue<int>()); }
            catch { return Math.Max(0f, cv.GetPrimitiveValue<float>()); }
        }
        catch
        {
            return 15f;
        }
    }

    // ---------------- Text ----------------

    // Localized string with named placeholders {NAME}. Color tags ({GREEN} …) are replaced by the CSS localizer.
    internal string T(CCSPlayerController? player, string key, params (string Key, object Value)[] args)
    {
        string s = Localizer.ForPlayer(player, key);
        foreach (var (k, v) in args)
            s = s.Replace("{" + k + "}", Convert.ToString(v, CultureInfo.InvariantCulture) ?? "");
        return s;
    }

    internal void Chat(CCSPlayerController player, string text)
    {
        if (!player.IsValid || player.IsBot) return;
        string prefix = Localizer.ForPlayer(player, "prefix").TrimStart(' ');
        player.PrintToChat(" " + prefix + " " + text.TrimStart(' '));
    }

    // Panorama shows plain text: drop the chat color control characters.
    internal static string StripColors(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
            if (c >= ' ') sb.Append(c);
        return sb.ToString().Trim();
    }

    internal static string Fmt(float v) => v.ToString("0.#", CultureInfo.InvariantCulture);
}
