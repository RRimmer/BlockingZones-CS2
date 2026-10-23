using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Vector3 = System.Numerics.Vector3;

namespace BlockingZones;

public partial class BlockingZonesPlugin
{
    internal const string NamePrefix = "BZMODELID_";

    // ---------------- Spawn / kill ----------------

    internal bool SpawnItem(BzItem it)
    {
        if (it.Entity != null && it.Entity.IsValid) return true;

        var prop = SpawnProp(it.Model, ToVector(it.Pos), ToQAngle(it.Ang), it.Alpha, Color.White, solid: true,
            name: NamePrefix + it.Quota, glow: null);
        it.Entity = prop;
        return prop != null;
    }

    internal void KillItem(BzItem it)
    {
        var e = it.Entity;
        it.Entity = null;
        if (e != null && e.IsValid) e.AcceptInput("Kill");
    }

    // Props of the previous round go first, then the new ones are spawned.
    internal void KillSpawned()
    {
        foreach (var it in _items) KillItem(it);
    }

    // Spawn the way that is known to work in CS2 (BlockerPasses): prop_dynamic_override, SOLID_VPHYSICS,
    // Teleport before DispatchSpawn, SetModel on the next frame. Render and glow are applied before the
    // spawn and once more after SetModel (with SetStateChanged, so clients get the values).
    internal CBaseModelEntity? SpawnProp(string model, Vector pos, QAngle ang, int alpha, Color tint, bool solid,
        string name, Color? glow)
    {
        try
        {
            var prop = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic_override");
            if (prop == null) return null;

            if (prop.Entity != null) prop.Entity.Name = name;
            prop.Collision.SolidType = solid ? SolidType_t.SOLID_VPHYSICS : SolidType_t.SOLID_NONE;
            ApplyRender(prop, alpha, tint, network: false);
            if (glow != null) ApplyGlow(prop, glow.Value, network: false);

            prop.Teleport(pos, ang, new Vector(0, 0, 0));
            prop.DispatchSpawn();

            Server.NextFrame(() =>
            {
                if (!prop.IsValid) return;
                prop.SetModel(model);
                ApplyRender(prop, alpha, tint, network: true);
                if (glow != null) ApplyGlow(prop, glow.Value, network: true);
            });

            return prop;
        }
        catch (Exception e)
        {
            Logger.LogError("Cannot spawn {Model}: {Error}", model, e.Message);
            return null;
        }
    }

    // ---------------- Transparency ----------------
    // SetEntityRenderMode(RENDER_TRANSCOLOR) + SetEntityRenderColor(…, alpha) of Source 1 becomes in CS2:
    //   m_nRenderMode = kRenderTransAlpha  (otherwise the alpha of m_clrRender is ignored by the renderer)
    //   m_clrRender   = ARGB(alpha, r, g, b) (Render = Color.FromArgb(a, r, g, b), the A channel is used)
    //   + SetStateChanged on both fields (a schema write alone is not networked after the spawn)
    //   + optional entity input "Alpha" (UseAlphaInput) – the Hammer way, sets the same field engine-side.
    // alpha 255 -> kRenderNormal (no translucency pass), alpha 0 -> kRenderNone: the prop is not drawn at all
    // but keeps its collision. This is the guaranteed "invisible wall" even if a client build ignored alpha.
    internal void ApplyRender(CBaseModelEntity e, int alpha, Color tint, bool network)
    {
        alpha = Math.Clamp(alpha, 0, 255);
        e.RenderMode = alpha >= 255 ? RenderMode_t.kRenderNormal
            : alpha <= 0 ? RenderMode_t.kRenderNone
            : RenderMode_t.kRenderTransAlpha;
        e.Render = Color.FromArgb(alpha, tint.R, tint.G, tint.B);

        if (!network) return;
        try
        {
            Utilities.SetStateChanged(e, "CBaseModelEntity", "m_nRenderMode");
            Utilities.SetStateChanged(e, "CBaseModelEntity", "m_clrRender");
            if (Cfg.UseAlphaInput) e.AcceptInput("Alpha", value: alpha.ToString());
        }
        catch (Exception ex)
        {
            Logger.LogDebug("Render update failed: {Error}", ex.Message);
        }
    }

    private void ApplyGlow(CBaseModelEntity e, Color color, bool network)
    {
        try
        {
            e.Glow.GlowColorOverride = color;
            e.Glow.GlowRange = 5000;
            e.Glow.GlowRangeMin = 0;
            e.Glow.GlowTeam = -1;
            e.Glow.GlowType = 3;
            if (network) Utilities.SetStateChanged(e, "CBaseModelEntity", "m_Glow");
        }
        catch (Exception ex)
        {
            Logger.LogDebug("Glow failed: {Error}", ex.Message);
        }
    }

    internal void ApplyTransform(BzItem it)
    {
        if (it.Entity != null && it.Entity.IsValid)
            it.Entity.Teleport(ToVector(it.Pos), ToQAngle(it.Ang), new Vector(0, 0, 0));
    }

    internal void ApplyQuotaName(BzItem it)
    {
        if (it.Entity is { IsValid: true, Entity: not null })
            it.Entity.Entity!.Name = NamePrefix + it.Quota;
    }

    // ---------------- Aim ----------------

    internal sealed record AimHit(Vector Pos, Vector3 Normal, CEntityInstance? Entity, float EyeYaw);

    // World, brushes and props. Players/NPC/triggers/debris are skipped, so a player in front of the
    // crosshair does not block the trace and the non-solid ghost is never hit.
    private static readonly TraceOptions AimTrace = new()
    {
        InteractsWith = Masks.Solid,
        InteractsExclude = Contents.Player | Contents.Npc | Contents.Trigger | Contents.Debris
    };

    // GetClientEyePosition + GetClientEyeAngles + TR_TraceRayFilter(MASK_SOLID) of the CS:GO plugin.
    // CSS >= 1.0.372 has a native ray trace (Trace.TraceShape, 8192 units).
    internal AimHit? Aim(CCSPlayerController player, CBaseEntity? alsoIgnore = null)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return null;
        var origin = pawn.AbsOrigin;
        if (origin == null) return null;

        float viewZ = pawn.ViewOffset.Z;
        if (viewZ <= 0) viewZ = 64f;
        var eye = new Vector(origin.X, origin.Y, origin.Z + viewZ);
        var ea = pawn.EyeAngles;
        var angles = new QAngle(ea.X, ea.Y, 0);

        var tr = Trace.TraceShape(eye, angles, pawn, AimTrace);
        if (!tr.DidHit() || tr.IsAllSolid) return null;

        CEntityInstance? hitEnt = SafeHitEntity(tr);

        // Safety net: if the ghost was hit anyway, trace again through it.
        if (alsoIgnore != null && hitEnt != null && alsoIgnore.IsValid && hitEnt.IsValid && hitEnt.Index == alsoIgnore.Index)
        {
            tr = Trace.TraceShape(eye, angles, alsoIgnore, AimTrace);
            if (!tr.DidHit() || tr.IsAllSolid) return null;
            hitEnt = SafeHitEntity(tr);
        }

        var n = tr.Normal;
        return new AimHit(tr.EndPos, new Vector3(n.X, n.Y, n.Z), hitEnt, ea.Y);
    }

    private static CEntityInstance? SafeHitEntity(TraceResult tr)
    {
        try
        {
            var e = tr.HitEntity();
            return e.IsValid ? e : null;
        }
        catch
        {
            return null;
        }
    }

    // GetClientAimTarget: our item under the crosshair, or null with the reason (lang key).
    internal BzItem? AimedItem(CCSPlayerController player, out string reasonKey)
    {
        reasonKey = "msg.not_found";
        CEntityInstance? ent = Aim(player)?.Entity;
        BzItem? it = ent != null ? FindItem(ent) : null;
        if (it != null) return it;

        // Second opinion: the engine picker (what "ent_fire !picker" uses).
        try
        {
            var picked = GetGameRules()?.FindPickerEntity<CBaseEntity>(player);
            if (picked != null)
            {
                it = FindItem(picked);
                if (it != null) return it;
                ent ??= picked;
            }
        }
        catch
        {
            // picker offset missing on this build: the trace result stands
        }

        if (ent != null && ent.IsValid)
        {
            string designer = ent.DesignerName ?? "";
            if (designer != "worldent" && designer != "")
                reasonKey = "msg.not_ours";
        }
        return null;
    }

    internal BzItem? FindItem(CEntityInstance e)
    {
        if (!e.IsValid) return null;
        uint index = e.Index;
        return _items.FirstOrDefault(i => i.Entity != null && i.Entity.IsValid && i.Entity.Index == index);
    }

    // ---------------- Geometry ----------------

    // Orientation from the surface normal:
    //   floor / slope / ceiling (|n.z| >= 0.7): the model's up axis = normal, its front looks at the player;
    //   wall: the model stands upright with its front away from the wall (back to the wall).
    internal static QAngle SurfaceAngles(Vector3 normal, float eyeYaw)
    {
        Vector3 up = normal.LengthSquared() > 0.0001f ? Vector3.Normalize(normal) : Vector3.UnitZ;

        if (MathF.Abs(up.Z) < 0.7f)
        {
            float wallYaw = MathF.Atan2(up.Y, up.X) * 180f / MathF.PI;
            return new QAngle(0, NormAngle(wallYaw), 0);
        }

        float yawRad = (eyeYaw + 180f) * MathF.PI / 180f;
        var desired = new Vector3(MathF.Cos(yawRad), MathF.Sin(yawRad), 0);
        Vector3 fwd = desired - up * Vector3.Dot(desired, up);
        if (fwd.LengthSquared() < 0.0001f) fwd = Vector3.UnitX - up * Vector3.Dot(Vector3.UnitX, up);
        fwd = Vector3.Normalize(fwd);
        Vector3 left = Vector3.Cross(up, fwd);
        return MatrixToAngles(fwd, left, up);
    }

    // Source MatrixAngles: forward / left / up columns -> pitch, yaw, roll (degrees).
    private static QAngle MatrixToAngles(Vector3 f, Vector3 l, Vector3 u)
    {
        float xy = MathF.Sqrt(f.X * f.X + f.Y * f.Y);
        float pitch, yaw, roll;
        if (xy > 0.001f)
        {
            yaw = MathF.Atan2(f.Y, f.X);
            pitch = MathF.Atan2(-f.Z, xy);
            roll = MathF.Atan2(l.Z, u.Z);
        }
        else
        {
            yaw = MathF.Atan2(-l.X, l.Y);
            pitch = MathF.Atan2(-f.Z, xy);
            roll = 0;
        }
        const float k = 180f / MathF.PI;
        return new QAngle(NormAngle(pitch * k), NormAngle(yaw * k), NormAngle(roll * k));
    }

    internal static float NormAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a <= -180f) a += 360f;
        return a;
    }

    internal static Vector ToVector(float[] v) => new(v[0], v[1], v[2]);
    internal static QAngle ToQAngle(float[] v) => new(v[0], v[1], v[2]);
}
