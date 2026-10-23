using CounterStrikeSharp.API.Core;

namespace BlockingZones;

// Menus of the "bz" panorama, driven only by the number keys 1-9, 0 (PMM IMenuApi.NumberKeyPressed).
// 8 = back (or previous page), 9 = next page where there is one, 0 = close. Aim-based actions read the
// crosshair at the moment of the key press; nothing captures the mouse, so the player aims freely.
// Feedback goes into the status line of the panel, not into chat.
public partial class BlockingZonesPlugin
{
    private enum Screen { Main, Create, Settings, Move, Rotate, Alpha, Quota, Aux, ConfirmAll }

    private sealed class View
    {
        public Screen Screen;
        public int Page;
        public string Status = "";
        public int StatusKind; // 0 hint, 1 ok, 2 error
        public int[] Keys = Array.Empty<int>(); // keys of the rows on screen, for the press highlight
        public int MoveStepIndex;               // index into Cfg.MoveSteps (key 7 on the Move screen)
        public int RotateStepIndex;             // index into Cfg.RotateSteps (key 7 on the Rotate screen)
        public float[]? MoveOrigin;             // position when the Move screen was opened (key 9 = undo)
    }

    private readonly View?[] _views = new View?[64];
    private readonly Dictionary<int, BzItem> _editTarget = new();

    private static readonly int[] AlphaPresets = { 255, 204, 153, 102, 51, 0 };
    private static readonly int[] QuotaPage0 = { 4, 8, 10, 16, 32 };          // keys 3..7
    private static readonly int[] QuotaPage1 = { 5, 12, 14, 18, 20, 24, 28, 64 }; // keys 1..7, 9
    private const int QuotaMin = 4;
    private const int QuotaMax = 64;
    private const int ModelsPerPage = 7;

    // ---------------- open / close ----------------

    private void OpenView(CCSPlayerController player, Screen screen, int page = 0, string status = "", int kind = 0)
    {
        if (player.Slot is < 0 or >= 64) return;
        var v = _views[player.Slot] ??= new View();
        v.Screen = screen;
        v.Page = page;
        v.Status = status;
        v.StatusKind = kind;
        Render(player);
    }

    private void CloseView(CCSPlayerController player)
    {
        if (player.Slot is < 0 or >= 64) return;
        _views[player.Slot] = null;
        _editTarget.Remove(player.Slot);
        ShowRoot(player, RootMenu, false);
    }

    private bool IsViewOpen(CCSPlayerController player) => player.Slot is >= 0 and < 64 && _views[player.Slot] != null;

    private void Status(CCSPlayerController player, string text, int kind)
    {
        var v = _views[player.Slot];
        if (v == null) return;
        v.Status = text;
        v.StatusKind = kind;
    }

    // ---------------- keys ----------------

    private void OnNumberKey(CCSPlayerController player, int digit)
    {
        if (player is not { IsValid: true, IsBot: false } || player.Slot is < 0 or >= 64) return;

        if (_previews.ContainsKey(player.Slot))
        {
            PreviewKey(player, digit);
            return;
        }

        var v = _views[player.Slot];
        if (v == null) return;

        // A PMM menu opened over ours takes the keys: ours steps aside.
        try
        {
            if (_menu != null && _menu.HasOpenedMenu(player))
            {
                CloseView(player);
                return;
            }
        }
        catch
        {
            // older core: ignore
        }

        if (!IsAdmin(player))
        {
            CloseView(player);
            return;
        }

        if (digit == 0)
        {
            CloseView(player);
            return;
        }

        var before = v.Screen;
        int beforePage = v.Page;
        v.Status = "";
        v.StatusKind = 0;

        switch (v.Screen)
        {
            case Screen.Main: KeyMain(player, digit); break;
            case Screen.Create: KeyCreate(player, v, digit); break;
            case Screen.Settings: KeySettings(player, digit); break;
            case Screen.Move: KeyMove(player, v, digit); break;
            case Screen.Rotate: KeyRotate(player, v, digit); break;
            case Screen.Alpha: KeyAlpha(player, digit); break;
            case Screen.Quota: KeyQuota(player, v, digit); break;
            case Screen.Aux: KeyAux(player, digit); break;
            case Screen.ConfirmAll: KeyConfirm(player, digit); break;
        }

        // Still on the same screen: redraw and light up the pressed row.
        var now = _views[player.Slot];
        if (now == null || _previews.ContainsKey(player.Slot)) return;
        Render(player);
        if (now.Screen == before && now.Page == beforePage)
        {
            int row = Array.IndexOf(now.Keys, digit);
            if (row >= 0) FlashRow(player, row);
        }
    }

    // ---------------- render ----------------

    private void Render(CCSPlayerController player)
    {
        var v = _views[player.Slot];
        if (v == null) return;

        var m = v.Screen switch
        {
            Screen.Main => ModelMain(player),
            Screen.Create => ModelCreate(player, v),
            Screen.Settings => ModelSettings(player),
            Screen.Move => ModelMove(player, v),
            Screen.Rotate => ModelRotate(player, v),
            Screen.Alpha => ModelAlpha(player),
            Screen.Quota => ModelQuota(player, v),
            Screen.Aux => ModelAux(player),
            _ => ModelConfirm(player)
        };
        if (m == null) return; // the model switched the screen and rendered already

        if (v.Status != "")
        {
            m.Status = v.Status;
            m.StatusKind = v.StatusKind;
        }
        m.Close = T(player, "hud.close");
        v.Keys = m.Rows.Select(r => r.Key).ToArray();
        DrawMenu(player, m);
    }

    private static MenuRow Row(int key, string text, string note = "", NoteStyle style = NoteStyle.Plain,
        bool selected = false, bool danger = false, bool accent = false, int bar = -1) =>
        new() { Key = key, Text = text, Note = note, Style = style, Selected = selected, Danger = danger, Accent = accent, Bar = bar };

    // ---------------- 1. main ----------------

    private MenuModel ModelMain(CCSPlayerController p)
    {
        var m = new MenuModel
        {
            Title = T(p, "hud.main.title"),
            Sub = T(p, "hud.main.sub", ("MAP", _mapName), ("COUNT", _items.Count), ("ALIVE", CountAlivePlayers())),
            Status = T(p, "hud.main.hint")
        };
        string aim = T(p, "hud.tag.aim");
        m.Rows.Add(Row(1, T(p, "hud.main.create")));
        m.Rows.Add(Row(2, T(p, "hud.main.delete"), aim, NoteStyle.Chip));
        m.Rows.Add(Row(3, T(p, "hud.main.settings"), aim, NoteStyle.Chip));
        m.Rows.Add(Row(4, T(p, "hud.main.aux")));
        m.Rows.Add(_dirty
            ? Row(5, T(p, "hud.main.save"), "● " + T(p, "hud.main.unsaved"), NoteStyle.Warn)
            : Row(5, T(p, "hud.main.save"), _items.Count.ToString()));
        m.Rows.Add(Row(6, T(p, "hud.main.show_all")));
        m.Rows.Add(Row(7, T(p, "hud.main.delete_all"), danger: true));
        return m;
    }

    private void KeyMain(CCSPlayerController p, int d)
    {
        switch (d)
        {
            case 1:
                if (_catalog.Count == 0) Status(p, T(p, "msg.catalog_empty"), 2);
                else OpenView(p, Screen.Create);
                break;
            case 2: DeleteAimed(p); break;
            case 3: OpenSettingsByAim(p); break;
            case 4: OpenView(p, Screen.Aux); break;
            case 5: SaveItems(p); break;
            case 6: ShowAll(p); break;
            case 7:
                if (_items.Count == 0) Status(p, T(p, "msg.delete_all_denied"), 2);
                else OpenView(p, Screen.ConfirmAll);
                break;
        }
    }

    // ---------------- create: model list ----------------

    private int CreatePages => Math.Max(1, (_catalog.Count + ModelsPerPage - 1) / ModelsPerPage);

    private MenuModel ModelCreate(CCSPlayerController p, View v)
    {
        v.Page = Math.Clamp(v.Page, 0, CreatePages - 1);
        var m = new MenuModel
        {
            Title = T(p, "hud.create.title"),
            Sub = T(p, "hud.create.sub", ("PAGE", v.Page + 1), ("PAGES", CreatePages), ("COUNT", _catalog.Count)),
            Status = T(p, "hud.create.hint"),
            Back = T(p, "hud.back")
        };
        int start = v.Page * ModelsPerPage;
        for (int i = 0; i < ModelsPerPage && start + i < _catalog.Count; i++)
            m.Rows.Add(Row(i + 1, _catalog[start + i].Name));
        if (v.Page < CreatePages - 1) m.Rows.Add(Row(9, T(p, "hud.more"), accent: true));
        return m;
    }

    private void KeyCreate(CCSPlayerController p, View v, int d)
    {
        if (d == 8)
        {
            if (v.Page > 0) v.Page--;
            else OpenView(p, Screen.Main);
            return;
        }
        if (d == 9)
        {
            if (v.Page < CreatePages - 1) v.Page++;
            return;
        }
        int index = v.Page * ModelsPerPage + d - 1;
        if (d is >= 1 and <= ModelsPerPage && index < _catalog.Count)
            StartPreview(p, _catalog[index]);
    }

    // ---------------- 2. delete (aim) ----------------

    private void DeleteAimed(CCSPlayerController p)
    {
        var it = AimedItem(p, out string reason);
        if (it == null)
        {
            Status(p, T(p, reason), 2);
            return;
        }

        string name = ModelName(it.Model);
        RemoveItem(it);
        Status(p, T(p, "msg.deleted", ("NAME", name)), 1);
    }

    private void RemoveItem(BzItem it)
    {
        KillItem(it);
        _items.Remove(it);
        foreach (int slot in _editTarget.Where(kv => kv.Value == it).Select(kv => kv.Key).ToList())
            _editTarget.Remove(slot);
        _dirty = true;
    }

    // ---------------- 3. settings (aim) ----------------

    private void OpenSettingsByAim(CCSPlayerController p)
    {
        var it = AimedItem(p, out string reason);
        if (it == null)
        {
            Status(p, T(p, reason), 2);
            return;
        }
        _editTarget[p.Slot] = it;
        OpenView(p, Screen.Settings);
    }

    // The item chosen by aim stays selected through the settings screens.
    private BzItem? EditTarget(CCSPlayerController p)
    {
        if (_editTarget.TryGetValue(p.Slot, out var it) && _items.Contains(it)) return it;
        _editTarget.Remove(p.Slot);
        OpenView(p, Screen.Main, 0, T(p, "msg.target_lost"), 2);
        return null;
    }

    private (string, string, string) Card(CCSPlayerController p, BzItem it) =>
        ("#" + (_items.IndexOf(it) + 1), ModelName(it.Model),
            T(p, "hud.card.info", ("STATE", T(p, it.Entity is { IsValid: true } ? "state.spawned" : "state.hidden")),
                ("QUOTA", it.Quota), ("PERCENT", Percent(it.Alpha))));

    private string ItemLine(BzItem it) => "#" + (_items.IndexOf(it) + 1) + " " + ModelName(it.Model);
    private static int Percent(int alpha) => (int)Math.Round(alpha * 100 / 255.0);
    private static string Position(BzItem it) => $"{Fmt(it.Pos[0])} · {Fmt(it.Pos[1])} · {Fmt(it.Pos[2])}";
    private static string Angles(BzItem it) => $"X {Fmt(it.Ang[0])}° · Y {Fmt(it.Ang[1])}° · Z {Fmt(it.Ang[2])}°";

    private MenuModel? ModelSettings(CCSPlayerController p)
    {
        var it = EditTarget(p);
        if (it == null) return null;
        var m = new MenuModel
        {
            Title = T(p, "hud.settings.title"),
            Sub = _mapName,
            Card = Card(p, it),
            Status = T(p, "hud.settings.hint"),
            Back = T(p, "hud.back")
        };
        m.Rows.Add(Row(1, T(p, "hud.settings.move"), Position(it)));
        m.Rows.Add(Row(2, T(p, "hud.settings.rotate"), Angles(it)));
        m.Rows.Add(Row(3, T(p, "hud.settings.alpha"), Percent(it.Alpha) + "%", bar: (int)Math.Round(it.Alpha / 25.5)));
        m.Rows.Add(Row(4, T(p, "hud.settings.quota"), it.Quota.ToString(), NoteStyle.Value));
        m.Rows.Add(Row(5, T(p, "hud.settings.reselect"), T(p, "hud.tag.aim"), NoteStyle.Chip));
        m.Rows.Add(Row(6, T(p, "hud.settings.delete"), danger: true));
        return m;
    }

    private void KeySettings(CCSPlayerController p, int d)
    {
        switch (d)
        {
            case 1:
                var target = EditTarget(p);
                if (target == null) return;
                var view = _views[p.Slot]!;
                view.MoveOrigin = (float[])target.Pos.Clone();
                view.MoveStepIndex = 0;
                OpenView(p, Screen.Move);
                break;
            case 2:
                if (EditTarget(p) == null) return;
                _views[p.Slot]!.RotateStepIndex = 0;
                OpenView(p, Screen.Rotate);
                break;
            case 3: if (EditTarget(p) != null) OpenView(p, Screen.Alpha); break;
            case 4: if (EditTarget(p) != null) OpenView(p, Screen.Quota); break;
            case 5: OpenSettingsByAim(p); break;
            case 6:
                var it = EditTarget(p);
                if (it == null) return;
                string name = ModelName(it.Model);
                RemoveItem(it);
                OpenView(p, Screen.Main, 0, T(p, "msg.deleted", ("NAME", name)), 1);
                break;
            case 8: OpenView(p, Screen.Main); break;
        }
    }

    // ---------------- move ----------------
    // Directions are relative to where the admin looks, snapped to the nearest world axis, so every press
    // moves the item exactly one step (Cfg.MoveSteps; Hammer units: 1 ≈ 1 inch ≈ 2.54 cm, a player is 72 tall).

    private float MoveStep(View v) => Cfg.MoveSteps[Math.Clamp(v.MoveStepIndex, 0, Cfg.MoveSteps.Count - 1)];
    private float RotateStep(View v) => Cfg.RotateSteps[Math.Clamp(v.RotateStepIndex, 0, Cfg.RotateSteps.Count - 1)];
    private static string StepList(List<float> steps) => string.Join(" / ", steps.Select(Fmt));

    private MenuModel? ModelMove(CCSPlayerController p, View v)
    {
        var it = EditTarget(p);
        if (it == null) return null;
        var m = new MenuModel
        {
            Title = T(p, "hud.move.title"),
            Sub = ItemLine(it),
            Now = (T(p, "hud.now"), Position(it)),
            Status = T(p, "hud.move.hint"),
            Back = T(p, "hud.back")
        };
        string step = T(p, "hud.move.units", ("STEP", Fmt(MoveStep(v))));
        m.Rows.Add(Row(1, T(p, "hud.move.forward"), step));
        m.Rows.Add(Row(2, T(p, "hud.move.back"), step));
        m.Rows.Add(Row(3, T(p, "hud.move.left"), step));
        m.Rows.Add(Row(4, T(p, "hud.move.right"), step));
        m.Rows.Add(Row(5, T(p, "hud.move.up"), step));
        m.Rows.Add(Row(6, T(p, "hud.move.down"), step));
        m.Rows.Add(Row(7, T(p, "hud.step", ("LIST", StepList(Cfg.MoveSteps))), Fmt(MoveStep(v)), NoteStyle.Value, accent: true));
        m.Rows.Add(Row(9, T(p, "hud.move.undo"), accent: true));
        return m;
    }

    private void KeyMove(CCSPlayerController p, View v, int d)
    {
        if (d == 8)
        {
            OpenView(p, Screen.Settings);
            return;
        }
        if (d == 7)
        {
            v.MoveStepIndex = (v.MoveStepIndex + 1) % Cfg.MoveSteps.Count;
            return;
        }
        var it = EditTarget(p);
        if (it == null) return;

        if (d == 9)
        {
            if (v.MoveOrigin is { Length: 3 } origin) it.Pos = (float[])origin.Clone();
        }
        else if (d is >= 1 and <= 6)
        {
            float step = MoveStep(v);
            if (d is 5 or 6)
            {
                it.Pos[2] += d == 5 ? step : -step;
            }
            else
            {
                // forward = the admin's view yaw snapped to 90°; left = forward rotated +90° (Source: Y is left of X)
                float yaw = p.PlayerPawn.Value is { IsValid: true } pawn ? pawn.EyeAngles.Y : 0f;
                int q = (int)MathF.Round(yaw / 90f);
                int fx = (int)MathF.Round(MathF.Cos(q * MathF.PI / 2f));
                int fy = (int)MathF.Round(MathF.Sin(q * MathF.PI / 2f));
                int lx = -fy, ly = fx;
                (int dx, int dy) = d switch
                {
                    1 => (fx, fy),
                    2 => (-fx, -fy),
                    3 => (lx, ly),
                    _ => (-lx, -ly)
                };
                it.Pos[0] += dx * step;
                it.Pos[1] += dy * step;
            }
        }
        else return;

        ApplyTransform(it);
        _dirty = true;
    }

    // ---------------- rotation ----------------

    private MenuModel? ModelRotate(CCSPlayerController p, View v)
    {
        var it = EditTarget(p);
        if (it == null) return null;
        var m = new MenuModel
        {
            Title = T(p, "hud.rotate.title"),
            Sub = ItemLine(it),
            Now = (T(p, "hud.now"), Angles(it)),
            Status = T(p, "hud.rotate.hint"),
            Back = T(p, "hud.back")
        };
        string step = Fmt(RotateStep(v));
        m.Rows.Add(Row(1, $"X +{step}°"));
        m.Rows.Add(Row(2, $"X −{step}°"));
        m.Rows.Add(Row(3, $"Y +{step}°"));
        m.Rows.Add(Row(4, $"Y −{step}°"));
        m.Rows.Add(Row(5, $"Z +{step}°"));
        m.Rows.Add(Row(6, $"Z −{step}°"));
        m.Rows.Add(Row(7, T(p, "hud.step", ("LIST", StepList(Cfg.RotateSteps))), step + "°", NoteStyle.Value, accent: true));
        m.Rows.Add(Row(9, T(p, "hud.rotate.reset"), accent: true));
        return m;
    }

    private void KeyRotate(CCSPlayerController p, View v, int d)
    {
        if (d == 8)
        {
            OpenView(p, Screen.Settings);
            return;
        }
        if (d == 7)
        {
            v.RotateStepIndex = (v.RotateStepIndex + 1) % Cfg.RotateSteps.Count;
            return;
        }
        var it = EditTarget(p);
        if (it == null) return;

        if (d == 9) it.Ang = new float[3];
        else if (d is >= 1 and <= 6)
        {
            int axis = (d - 1) / 2;
            float step = RotateStep(v);
            it.Ang[axis] = NormAngle(it.Ang[axis] + (d % 2 == 1 ? step : -step));
        }
        else return;

        ApplyTransform(it);
        _dirty = true;
    }

    // ---------------- visibility ----------------

    private MenuModel? ModelAlpha(CCSPlayerController p)
    {
        var it = EditTarget(p);
        if (it == null) return null;
        var m = new MenuModel
        {
            Title = T(p, "hud.alpha.title"),
            Sub = ItemLine(it),
            Now = (T(p, "hud.now"), $"{Percent(it.Alpha)}% · {it.Alpha}"),
            Status = T(p, "hud.alpha.hint"),
            Back = T(p, "hud.back")
        };
        for (int i = 0; i < AlphaPresets.Length; i++)
        {
            int a = AlphaPresets[i];
            string text = a == 0 ? T(p, "hud.alpha.invisible") : Percent(a) + "%";
            m.Rows.Add(Row(i + 1, text, a.ToString(), selected: it.Alpha == a));
        }
        return m;
    }

    private void KeyAlpha(CCSPlayerController p, int d)
    {
        if (d == 8)
        {
            OpenView(p, Screen.Settings);
            return;
        }
        if (d is < 1 or > 6) return;
        var it = EditTarget(p);
        if (it == null) return;

        it.Alpha = AlphaPresets[d - 1];
        if (it.Entity is { IsValid: true } e) ApplyRender(e, it.Alpha, System.Drawing.Color.White, network: true);
        _dirty = true;
    }

    // ---------------- quota ----------------

    private MenuModel? ModelQuota(CCSPlayerController p, View v)
    {
        var it = EditTarget(p);
        if (it == null) return null;
        var m = new MenuModel
        {
            Title = T(p, "hud.quota.title"),
            Sub = ItemLine(it),
            Now = (T(p, "hud.now"), T(p, "hud.quota.now", ("QUOTA", it.Quota))),
            Status = T(p, "hud.quota.hint", ("QUOTA", it.Quota)),
            Back = T(p, "hud.back")
        };

        if (v.Page == 0)
        {
            m.Rows.Add(Row(1, "−1"));
            m.Rows.Add(Row(2, "+1"));
            for (int i = 0; i < QuotaPage0.Length; i++)
                m.Rows.Add(Row(i + 3, QuotaPage0[i].ToString(), selected: it.Quota == QuotaPage0[i]));
            m.Rows.Add(Row(9, T(p, "hud.quota.more", ("LIST", string.Join(" · ", QuotaPage1))), accent: true));
        }
        else
        {
            for (int i = 0; i < QuotaPage1.Length; i++)
            {
                int key = i < 7 ? i + 1 : 9;
                m.Rows.Add(Row(key, QuotaPage1[i].ToString(), selected: it.Quota == QuotaPage1[i]));
            }
        }
        return m;
    }

    private void KeyQuota(CCSPlayerController p, View v, int d)
    {
        if (d == 8)
        {
            if (v.Page > 0) v.Page = 0;
            else OpenView(p, Screen.Settings);
            return;
        }
        var it = EditTarget(p);
        if (it == null) return;

        int? value = null;
        if (v.Page == 0)
        {
            if (d == 1) value = it.Quota - 1;
            else if (d == 2) value = it.Quota + 1;
            else if (d is >= 3 and <= 7) value = QuotaPage0[d - 3];
            else if (d == 9)
            {
                v.Page = 1;
                return;
            }
        }
        else
        {
            if (d is >= 1 and <= 7) value = QuotaPage1[d - 1];
            else if (d == 9) value = QuotaPage1[7];
        }
        if (value == null) return;

        it.Quota = Math.Clamp(value.Value, QuotaMin, QuotaMax);
        ApplyQuotaName(it);
        _dirty = true;
    }

    // ---------------- 4. auxiliary ----------------

    private MenuModel ModelAux(CCSPlayerController p)
    {
        var m = new MenuModel
        {
            Title = T(p, "hud.aux.title"),
            Sub = T(p, "hud.aux.sub"),
            Status = T(p, "hud.aux.hint"),
            Back = T(p, "hud.back")
        };
        string off = Cfg.UsePlayers ? "" : T(p, "hud.tag.inactive");
        m.Rows.Add(Row(1, T(p, "hud.aux.mode", ("MODE", T(p, "mode." + Cfg.MessageMode))), off, NoteStyle.Chip));
        m.Rows.Add(Row(2, T(p, Cfg.UsePlayers ? "hud.aux.useplayers_on" : "hud.aux.useplayers_off"), selected: Cfg.UsePlayers));
        m.Rows.Add(Row(3, T(p, "hud.aux.players"), Cfg.UsePlayers ? Cfg.Players.ToString() : off,
            Cfg.UsePlayers ? NoteStyle.Value : NoteStyle.Chip));
        return m;
    }

    private void KeyAux(CCSPlayerController p, int d)
    {
        switch (d)
        {
            case 1:
                Cfg.MessageMode = (Cfg.MessageMode + 1) % 4;
                SaveConfig();
                break;
            case 2:
                Cfg.UsePlayers = !Cfg.UsePlayers;
                SaveConfig();
                break;
            case 3:
                // 2 → 4 → 6 … → 64 → 2: default quota for new items.
                Cfg.Players = Cfg.Players >= 64 ? 2 : Math.Min(64, Cfg.Players + 2);
                SaveConfig();
                break;
            case 8:
                OpenView(p, Screen.Main);
                break;
        }
    }

    // ---------------- 5. save ----------------

    private void SaveItems(CCSPlayerController p)
    {
        if (_items.Count == 0 && !File.Exists(_mapFile))
        {
            Status(p, T(p, "msg.no_items"), 2);
            return;
        }
        int count = SaveMap();
        if (count < 0) Status(p, T(p, "msg.save_error"), 2);
        else Status(p, T(p, "msg.saved", ("COUNT", count), ("FILE", MapFileShort)), 1);
    }

    // ---------------- 6. show all (editing) ----------------
    // Items hidden by the quota or by the warmup cannot be aimed at: spawn all of them for the rest of the round.
    private void ShowAll(CCSPlayerController p)
    {
        int n = 0;
        foreach (var it in _items)
            if (SpawnItem(it)) n++;
        Status(p, T(p, "msg.shown_all", ("COUNT", n)), 1);
    }

    // ---------------- 7. delete all ----------------

    private MenuModel ModelConfirm(CCSPlayerController p)
    {
        var m = new MenuModel
        {
            Title = T(p, "hud.confirm.title"),
            Sub = T(p, "hud.confirm.sub", ("COUNT", _items.Count), ("FILE", MapFileShort)),
            Status = T(p, "hud.confirm.hint"),
            Back = T(p, "hud.back")
        };
        m.Rows.Add(Row(1, T(p, "hud.confirm.yes"), danger: true));
        m.Rows.Add(Row(2, T(p, "hud.confirm.no")));
        return m;
    }

    private void KeyConfirm(CCSPlayerController p, int d)
    {
        switch (d)
        {
            case 1:
                int count = _items.Count;
                KillSpawned();
                _items.Clear();
                _editTarget.Clear();
                int written = SaveMap(); // an empty item list = the map file is cleared
                OpenView(p, Screen.Main, 0,
                    written < 0 ? T(p, "msg.save_error") : T(p, "msg.deleted_all", ("COUNT", count)), written < 0 ? 2 : 1);
                break;
            case 2:
            case 8:
                OpenView(p, Screen.Main, 0, T(p, "msg.delete_all_cancel"), 0);
                break;
        }
    }
}
