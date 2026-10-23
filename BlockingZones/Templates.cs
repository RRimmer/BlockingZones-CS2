using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BlockingZones;

// config.jsonc / items.jsonc: JSON with // and /* */ comments (read with JsonCommentHandling.Skip).
// The plugin writes the whole file only when it is missing. Toggles from the menu patch single values
// in place (PatchJsoncValue), so the admin's comments and formatting stay.
internal static class Templates
{
    public static string Config(BzConfig c)
    {
        string B(bool v) => v ? "true" : "false";
        string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        string L(List<string> v) => JsonSerializer.Serialize(v);
        string S(List<float> v) => "[ " + string.Join(", ", v.Select(F)) + " ]";
        var p = c.Preview;

        return $$"""
        // BlockingZones — настройки. Комментарии // и /* */ можно оставлять.
        // Перечитать без перезагрузки плагина: css_bz_reload. Меню: css_bz (только цифры 1-9, 0)
        {
          // Квота для НОВЫХ предметов: предмет стоит, пока живых игроков T+CT меньше квоты. 2..64
          "Players": {{c.Players}},

          // Уведомление, когда появился хотя бы один предмет:
          // 0 — выключено, 1 — только чат, 2 — только Panorama (баннер bz), 3 — чат + Panorama
          "MessageMode": {{c.MessageMode}},

          // false — подсчёт игроков выключен, все предметы стоят всегда; Players и MessageMode неактивны
          "UsePlayers": {{B(c.UsePlayers)}},

          // Считать ботов игроками
          "CountBots": {{B(c.CountBots)}},

          // Флаг доступа к css_bz и css_bz_reload ("" — всем)
          "AdminFlag": {{JsonSerializer.Serialize(c.AdminFlag)}},

          // Видимость только что поставленного предмета 0..255
          "DefaultAlpha": {{c.DefaultAlpha}},

          // Кроме m_nRenderMode + m_clrRender слать input "Alpha". false — если в консоли ошибки про Alpha
          "UseAlphaInput": {{B(c.UseAlphaInput)}},

          // Скомпилированная панорама плагина в клиентском аддоне (Content-addonmanager -> build-addon.cmd)
          "LayoutPath": {{JsonSerializer.Serialize(c.LayoutPath)}},

          // Сколько секунд держится баннер «Часть карты закрыта» (MessageMode 2 и 3), 2..30.
          // Баннер плавно появляется и гаснет, полоска внизу показывает, сколько осталось
          "NoticeSeconds": {{F(c.NoticeSeconds)}},

          // Доп. папки workshop-контента (…/steamapps/workshop/content/730). Стандартные проверяются всегда
          "WorkshopContentDirs": {{L(c.WorkshopContentDirs)}},

          // Шаги в «Настройки предмета». Клавиша 7 переключает шаг по кругу, первый — при входе в экран.
          // Передвинуть — в юнитах Hammer (1 юнит ≈ 2,5 см, игрок ростом 72), до 1000
          "MoveSteps": {{S(c.MoveSteps)}},
          // Поворот — в градусах, до 180 (например [ 90, 45, 15, 1 ])
          "RotateSteps": {{S(c.RotateSteps)}},

          // Предпросмотр установки (призрак за прицелом). Управление только цифрами:
          // 1 — поставить, 2 / 3 / 4 — повернуть X / Y / Z, 5 — сбросить углы, 0 — отмена
          "Preview": {
            "Interval": {{F(p.Interval)}},        // секунд между обновлениями призрака, 0.02..0.5
            "Alpha": {{p.Alpha}},              // прозрачность призрака 0..255
            "Color": [ {{p.Color[0]}}, {{p.Color[1]}}, {{p.Color[2]}} ], // оттенок призрака R, G, B
            "Glow": {{B(p.Glow)}},             // обводка призрака
            "HideFromOthers": {{B(p.HideFromOthers)}},   // призрак видит только тот, кто ставит
            "ReopenMenu": {{B(p.ReopenMenu)}},       // после 1 / 0 снова открыть список моделей
            "RotateStep": {{F(p.RotateStep)}}          // градусов на одно нажатие 2 / 3 / 4
          }
        }

        """;
    }

    public const string Items = """
        // BlockingZones — модели для пункта «Создать предмет». Комментарии можно оставлять.
        // Name  — подпись в меню, Model — путь .vmdl из pak01_dir.vpk (удобно искать в Source 2 Viewer).
        // Новые модели прекэшируются при загрузке карты: после правки — css_bz_reload и смена карты.
        // Ниже только модели, проверенные в CS2 (конфиги BlockerPasses).
        [
          {
            "Name": "Сетка-забор 256",
            "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_256_capped.vmdl"
          },
          {
            "Name": "Сетка-забор 256 (без столбов)",
            "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_256.vmdl"
          },
          {
            "Name": "Рольставня 96x128",
            "Model": "models/props/de_dust/hr_dust/dust_windows/dust_rollupdoor_96x128_surface_lod.vmdl"
          },
          {
            "Name": "Дверь Mirage (малая)",
            "Model": "models/props/de_mirage/small_door_b.vmdl"
          },
          {
            "Name": "Ворота Mirage (большие)",
            "Model": "models/props/de_mirage/large_door_c.vmdl"
          }
        ]

        """;

    // Replaces the value (number or true/false) of the first "key": in the text. Comments around it stay.
    public static bool PatchJsoncValue(ref string text, string key, string value)
    {
        var re = new Regex("(\"" + Regex.Escape(key) + "\"\\s*:\\s*)(-?\\d+(?:\\.\\d+)?|true|false)");
        var m = re.Match(text);
        if (!m.Success) return false;
        text = text[..m.Groups[2].Index] + value + text[(m.Groups[2].Index + m.Groups[2].Length)..];
        return true;
    }
}
