using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace BlockingZones;

// Preview ("ghost") placement. One preview per player.
// The ghost is a non-solid, translucent, glowing prop_dynamic that follows the crosshair every Interval seconds.
// Number keys only (bz-prev strip): 1 place, 2 / 3 / 4 rotate X / Y / Z by RotateStep, 5 reset angles, 0 cancel.
// Ends on: place, cancel, death, disconnect, map change, css_bz again.
public partial class BlockingZonesPlugin
{
    private sealed class PreviewState
    {
        public string Model = "";
        public string Name = "";
        public CBaseModelEntity? Ghost;
        public float RotX, RotY, RotZ;
        public Vector? Pos;
        public QAngle? Ang;
        public bool OnSurface;
        public float ErrorUntil;
    }

    private readonly Dictionary<int, PreviewState> _previews = new();

    private void StartPreview(CCSPlayerController player, CatalogEntry entry)
    {
        if (!player.PawnIsAlive)
        {
            Status(player, T(player, "preview.not_alive"), 2);
            return;
        }

        StopPreview(player.Slot, false);
        var pv = new PreviewState { Model = entry.Model, Name = entry.Name };
        _previews[player.Slot] = pv;

        ShowRoot(player, RootMenu, false);
        UpdatePreview(player, pv);
        DrawPreview(player, pv);
        ShowRoot(player, RootPrev, true);
    }

    // backToMenu: return to the model list (ReopenMenu) or close everything.
    internal void StopPreview(int slot, bool backToMenu)
    {
        if (!_previews.Remove(slot, out var pv)) return;

        if (pv.Ghost != null && pv.Ghost.IsValid) pv.Ghost.AcceptInput("Kill");
        pv.Ghost = null;

        var player = Utilities.GetPlayerFromSlot(slot);
        if (player == null || !player.IsValid) return;
        ShowRoot(player, RootPrev, false);

        if (backToMenu && Cfg.Preview.ReopenMenu && _views[slot] != null) Render(player);
        else if (!backToMenu || !Cfg.Preview.ReopenMenu) CloseViewSilently(slot, player);
    }

    private void CloseViewSilently(int slot, CCSPlayerController player)
    {
        _views[slot] = null;
        ShowRoot(player, RootMenu, false);
    }

    private void RestartPreviewTimer()
    {
        if (_previewTimer != null && Math.Abs(_previewInterval - Cfg.Preview.Interval) < 0.0001f) return;
        _previewTimer?.Kill();
        _previewInterval = Cfg.Preview.Interval;
        _previewTimer = AddTimer(_previewInterval, PreviewTick, TimerFlags.REPEAT);
    }

    private void PreviewTick()
    {
        if (_previews.Count == 0) return;

        foreach (var (slot, pv) in _previews.ToList())
        {
            var player = Utilities.GetPlayerFromSlot(slot);
            if (player == null || !player.IsValid)
            {
                _previews.Remove(slot);
                if (pv.Ghost != null && pv.Ghost.IsValid) pv.Ghost.AcceptInput("Kill");
                continue;
            }
            if (!player.PawnIsAlive)
            {
                StopPreview(slot, false);
                continue;
            }

            UpdatePreview(player, pv);
            DrawPreview(player, pv);
        }
    }

    private void UpdatePreview(CCSPlayerController player, PreviewState pv)
    {
        var hit = Aim(player, pv.Ghost);
        if (hit == null)
        {
            pv.OnSurface = false;
            return;
        }

        var baseAng = SurfaceAngles(hit.Normal, hit.EyeYaw);
        var ang = new QAngle(NormAngle(baseAng.X + pv.RotX), NormAngle(baseAng.Y + pv.RotY), NormAngle(baseAng.Z + pv.RotZ));
        pv.Pos = hit.Pos;
        pv.Ang = ang;
        pv.OnSurface = true;

        if (pv.Ghost == null || !pv.Ghost.IsValid)
        {
            // Not there yet, or removed by the round restart: (re)create it.
            var c = Cfg.Preview.Color;
            var tint = Color.FromArgb(255, c[0], c[1], c[2]);
            pv.Ghost = SpawnProp(pv.Model, hit.Pos, ang, Cfg.Preview.Alpha, tint, solid: false,
                name: "BZPREVIEW_" + player.Slot, glow: Cfg.Preview.Glow ? tint : null);
        }
        else
        {
            pv.Ghost.Teleport(hit.Pos, ang, new Vector(0, 0, 0));
        }
    }

    // The bottom strip. Only changed values are sent (SetVar / SetClass cache).
    private void DrawPreview(CCSPlayerController player, PreviewState pv)
    {
        if (!EnsureLayout()) return;

        SetVar(player, "bz-p-cap", "pcap", T(player, "hud.prev.cap"));
        SetVar(player, "bz-p-name", "pname", pv.Name);
        SetVar(player, "bz-p-ang", "pang", $"X {Fmt(pv.RotX)}° · Y {Fmt(pv.RotY)}° · Z {Fmt(pv.RotZ)}°");

        bool error = Server.CurrentTime < pv.ErrorUntil;
        string state = error ? T(player, "hud.prev.missed")
            : pv.OnSurface ? T(player, "hud.prev.surface")
            : T(player, "hud.prev.nosurface");
        SetVar(player, "bz-p-state-t", "pstate", state);
        SetOnly(player, "bz-p-state", pv.OnSurface && !error ? "p-ok" : "p-bad", "p-");

        SetVar(player, "bz-pk1-t", "pk1", T(player, "hud.prev.place"));
        SetVar(player, "bz-pk2-t", "pk2", "X +" + Fmt(Cfg.Preview.RotateStep) + "°");
        SetVar(player, "bz-pk3-t", "pk3", "Y +" + Fmt(Cfg.Preview.RotateStep) + "°");
        SetVar(player, "bz-pk4-t", "pk4", "Z +" + Fmt(Cfg.Preview.RotateStep) + "°");
        SetVar(player, "bz-pk5-t", "pk5", T(player, "hud.prev.reset"));
        SetVar(player, "bz-pk0-t", "pk0", T(player, "hud.prev.cancel"));

        SetClass(player, RootMenu, Hidden, true);
        SetClass(player, RootPrev, Hidden, false);
    }

    private void PreviewKey(CCSPlayerController player, int digit)
    {
        if (!_previews.TryGetValue(player.Slot, out var pv)) return;
        if (!IsAdmin(player))
        {
            StopPreview(player.Slot, false);
            return;
        }

        float step = Cfg.Preview.RotateStep;
        switch (digit)
        {
            case 1:
                FlashPreviewKey(player, 1);
                ConfirmPreview(player, pv);
                return;
            case 2: pv.RotX = NormAngle(pv.RotX + step); break;
            case 3: pv.RotY = NormAngle(pv.RotY + step); break;
            case 4: pv.RotZ = NormAngle(pv.RotZ + step); break;
            case 5: pv.RotX = pv.RotY = pv.RotZ = 0; break;
            case 0:
                StopPreview(player.Slot, true);
                if (_views[player.Slot] != null) Status(player, T(player, "preview.cancel"), 0);
                if (_views[player.Slot] != null) Render(player);
                return;
            default: return;
        }

        FlashPreviewKey(player, digit);
        UpdatePreview(player, pv);
        DrawPreview(player, pv);
    }

    private void ConfirmPreview(CCSPlayerController player, PreviewState pv)
    {
        // Fresh trace at the moment of the key press.
        UpdatePreview(player, pv);
        if (!pv.OnSurface || pv.Pos == null || pv.Ang == null)
        {
            pv.ErrorUntil = Server.CurrentTime + 1.5f;
            DrawPreview(player, pv);
            return;
        }

        var item = new BzItem
        {
            Model = pv.Model,
            Pos = new[] { pv.Pos.X, pv.Pos.Y, pv.Pos.Z },
            Ang = new[] { pv.Ang.X, pv.Ang.Y, pv.Ang.Z },
            Alpha = Cfg.DefaultAlpha,
            Quota = Cfg.Players
        };
        item.Normalize();

        _items.Add(item);
        _dirty = true;
        // Placed by the admin: shown at once (also in warmup / with enough players) so it can be checked.
        // From the next round on it follows the quota like every other item.
        bool spawned = SpawnItem(item);

        StopPreview(player.Slot, true);
        if (_views[player.Slot] == null) return;
        Status(player, spawned
            ? T(player, "msg.created", ("NAME", pv.Name), ("QUOTA", item.Quota), ("INDEX", _items.Count))
            : T(player, "msg.spawn_failed", ("NAME", pv.Name)), spawned ? 1 : 2);
        Render(player);
    }

    // ---------------- Visibility ----------------

    // HideFromOthers: the ghost is transmitted only to its owner.
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_previews.Count == 0 || !Cfg.Preview.HideFromOthers) return;

        foreach (var (info, player) in infoList)
        {
            if (player == null || !player.IsValid) continue;
            foreach (var (slot, pv) in _previews)
            {
                if (slot == player.Slot) continue;
                var ghost = pv.Ghost;
                if (ghost != null && ghost.IsValid) info.TransmitEntities.Remove(ghost);
            }
        }
    }
}
