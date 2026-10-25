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
        // BlockingZones — модели для пункта «Создать предмет». Комментарии // и /* */ можно оставлять.
        // Меню: Создать предмет → раздел → модель → предпросмотр. Порядок разделов и моделей = порядок в меню (по 7 на страницу).
        //
        // Раздел:  { "Category": { "ru": "…", "en": "…" }, "Items": [ модели ] }
        // Модель:  { "Name": { "ru": "…", "en": "…" }, "Model": "models/….vmdl" }
        //   Name / Category — строка или перевод по языку игрока (язык CounterStrikeSharp, !lang);
        //   нет перевода — en, затем первый из списка. Model — путь .vmdl из pak01_dir.vpk (Source 2 Viewer).
        //   Модель можно положить и прямо в корень списка, без раздела: она попадёт в раздел «Без раздела».
        //   Если раздел всего один, меню сразу показывает модели.
        // Новые модели прекэшируются при загрузке карты: после правки — css_bz_reload и смена карты.
        // Все модели ниже есть в CS2 (pak01_dir.vpk) вместе с текстурами.
        [
          {
            "Category": { "ru": "Заборы и сетки", "en": "Fences" },
            "Items": [
              { "Name": { "ru": "Сетка-забор 256", "en": "Chain-link fence 256" }, "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_256_capped.vmdl" },
              { "Name": { "ru": "Сетка-забор 256 (без столбов)", "en": "Chain-link fence 256 (no posts)" }, "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_256.vmdl" },
              { "Name": { "ru": "Сетка-забор 128", "en": "Chain-link fence 128" }, "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_128_capped.vmdl" },
              { "Name": { "ru": "Сетка-забор 64", "en": "Chain-link fence 64" }, "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_64_capped.vmdl" },
              { "Name": { "ru": "Сетка с колючей проволокой 256", "en": "Barbed-wire fence 256" }, "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_barbwire_001_256.vmdl" },
              { "Name": { "ru": "Сетка с колючей проволокой 128", "en": "Barbed-wire fence 128" }, "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_barbwire_001_128.vmdl" },
              { "Name": { "ru": "Сетка с тентом 256", "en": "Covered fence 256" }, "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_cover_001_256.vmdl" },
              { "Name": { "ru": "Сетка с тентом 128", "en": "Covered fence 128" }, "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_cover_001_128.vmdl" },
              { "Name": { "ru": "Откатные ворота из сетки 256", "en": "Sliding fence gate 256" }, "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_gate_003a_256.vmdl" },
              { "Name": { "ru": "Ворота из сетки 128", "en": "Fence gate 128" }, "Model": "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_gate_001_128.vmdl" },
              { "Name": { "ru": "Сетка-забор Dust 256", "en": "Dust chain-link fence 256" }, "Model": "models/props/de_dust/hr_dust/dust_fences/dust_chainlink_fence_001_256.vmdl" },
              { "Name": { "ru": "Сетка с тентом Dust 256", "en": "Dust covered fence 256" }, "Model": "models/props/de_dust/hr_dust/dust_fences/dust_chainlink_fence_cover_001_256.vmdl" },
              { "Name": { "ru": "Ворота из сетки Dust 128", "en": "Dust fence gate 128" }, "Model": "models/props/de_dust/hr_dust/dust_fences/dust_chainlink_fence_gate_001_128.vmdl" },
              { "Name": { "ru": "Решётчатый забор 96x128", "en": "Security fence 96x128" }, "Model": "models/props/de_train/hr_train_s2/train_fences/train_fence_security_96x128_01.vmdl" },
              { "Name": { "ru": "Решётчатый забор 96x96", "en": "Security fence 96x96" }, "Model": "models/props/de_train/hr_train_s2/train_fences/train_fence_security_96x96_01.vmdl" },
              { "Name": { "ru": "Глухой забор 168x96", "en": "Solid fence 168x96" }, "Model": "models/props/de_train/hr_train_s2/train_fences/train_fence_security_solid_168x96_01.vmdl" },
              { "Name": { "ru": "Глухой забор 168x48", "en": "Solid fence 168x48" }, "Model": "models/props/de_train/hr_train_s2/train_fences/train_fence_security_solid_168x48_01.vmdl" },
              { "Name": { "ru": "Раздвижная решётка 64x128", "en": "Scissor gate 64x128" }, "Model": "models/props/de_train/hr_train_s2/train_fences/train_fence_scissor_gate_64x128_01.vmdl" },
              { "Name": { "ru": "Кованые ворота", "en": "Wrought-iron gate" }, "Model": "models/props/de_train/hr_train_s2/train_fences/train_fence_wrought_iron_large_gate_01.vmdl" },
              { "Name": { "ru": "Кованая ограда 64", "en": "Wrought-iron fence 64" }, "Model": "models/props/de_train/hr_train_s2/train_fences/train_fence_wrought_iron_b_64x64.vmdl" },
              { "Name": { "ru": "Деревянный забор 128", "en": "Wooden fence 128" }, "Model": "models/props/hr_massive/wood_fence/wood_fence_128.vmdl" },
              { "Name": { "ru": "Калитка деревянного забора", "en": "Wooden fence door" }, "Model": "models/props/hr_massive/wood_fence/wood_fence_door.vmdl" },
              { "Name": { "ru": "Строительное ограждение", "en": "Construction fence" }, "Model": "models/de_overpass/construction/fence/construction_fence_1.vmdl" },
              { "Name": { "ru": "Ворота депо (Train)", "en": "Train yard gate" }, "Model": "models/props/de_train/hr_t/gate_fence_a/gate_fence_a.vmdl" },
              { "Name": { "ru": "Складная решётка 128x128", "en": "Folding gate 128x128" }, "Model": "models/generic/metal_gates_kit_01/metal_gate_folding_01_128x128_a_closed.vmdl" },
              { "Name": { "ru": "Складная решётка 128x112", "en": "Folding gate 128x112" }, "Model": "models/generic/metal_gates_kit_01/metal_gate_folding_128x112_a_closed.vmdl" },
              { "Name": { "ru": "Металлическая створка ворот", "en": "Metal gate leaf" }, "Model": "models/generic/metal_gates_kit_01/metal_gate_01_a_left.vmdl" },
              { "Name": { "ru": "Ржавый забор A (Mirage)", "en": "Rusty fence A (Mirage)" }, "Model": "models/props/de_mirage/rusted_fence_a.vmdl" },
              { "Name": { "ru": "Деревянный забор (Inferno)", "en": "Wooden fence (Inferno)" }, "Model": "models/props/de_inferno/wood_fence.vmdl" }
            ]
          },
          {
            "Category": { "ru": "Двери, ворота, рольставни", "en": "Doors, gates, roller shutters" },
            "Items": [
              { "Name": { "ru": "Рольставня 96x128", "en": "Roller shutter 96x128" }, "Model": "models/props/de_dust/hr_dust/dust_windows/dust_rollupdoor_96x128_surface_lod.vmdl" },
              { "Name": { "ru": "Рольставня 128x128", "en": "Roller shutter 128x128" }, "Model": "models/props/de_dust/hr_dust/dust_doors/dust_rollupdoor_128x128_surface_01.vmdl" },
              { "Name": { "ru": "Рольставня 192x128", "en": "Roller shutter 192x128" }, "Model": "models/props/de_dust/hr_dust/dust_windows/dust_rollupdoor_192x128_surface_lod.vmdl" },
              { "Name": { "ru": "Деревянная дверь 80x128", "en": "Wooden door 80x128" }, "Model": "models/props/de_dust/hr_dust/dust_doors/dust_door_80x128_01.vmdl" },
              { "Name": { "ru": "Деревянная дверь 80x128 (вариант 2)", "en": "Wooden door 80x128 (variant 2)" }, "Model": "models/props/de_dust/hr_dust/dust_doors/dust_door_80x128_03.vmdl" },
              { "Name": { "ru": "Двустворчатая дверь 148x176", "en": "Double door 148x176" }, "Model": "models/props/de_dust/hr_dust/dust_doors/dust_door_148x176_01.vmdl" },
              { "Name": { "ru": "Большие ворота (Dust)", "en": "Large gate (Dust)" }, "Model": "models/props/de_dust/hr_dust/dust_doors/dust_door_large_door_01.vmdl" },
              { "Name": { "ru": "Ворота мида (Dust II)", "en": "Mid doors (Dust II)" }, "Model": "models/props/de_dust/hr_dust/dust_doors/dust_door_mid_doors_01.vmdl" },
              { "Name": { "ru": "Ворота лонга (Dust II)", "en": "Long doors (Dust II)" }, "Model": "models/props/de_dust/hr_dust/dust_doors/dust_door_long_doors_01.vmdl" },
              { "Name": { "ru": "Двери B (Dust II)", "en": "B doors (Dust II)" }, "Model": "models/props/de_dust/hr_dust/dust_doors/dust_door_b_doors_01.vmdl" },
              { "Name": { "ru": "Арочная дверь", "en": "Arched door" }, "Model": "models/props/de_dust/hr_dust/dust_doors/dust_arch_door_01.vmdl" },
              { "Name": { "ru": "Ворота Mirage (большие)", "en": "Mirage gate (large)" }, "Model": "models/props/de_mirage/large_door_c.vmdl" },
              { "Name": { "ru": "Ворота Mirage (большие, вариант 2)", "en": "Mirage gate (large, variant 2)" }, "Model": "models/props/de_mirage/large_door_a.vmdl" },
              { "Name": { "ru": "Дверь Mirage (малая)", "en": "Mirage door (small)" }, "Model": "models/props/de_mirage/small_door_b.vmdl" },
              { "Name": { "ru": "Металлическая дверь", "en": "Metal door" }, "Model": "models/props/de_nuke/hr_nuke/metal_door_001/metal_door_001.vmdl" },
              { "Name": { "ru": "Металлическая дверь с окном", "en": "Metal door with window" }, "Model": "models/props/de_nuke/hr_nuke/metal_door_001/metal_door_002_window.vmdl" },
              { "Name": { "ru": "Двойная дверь", "en": "Double door" }, "Model": "models/generic/doors/door_kit_01/door_double_01.vmdl" },
              { "Name": { "ru": "Простая дверь 56x112", "en": "Simple door 56x112" }, "Model": "models/generic/doors/door_kit_01/door_simple_56_112_01.vmdl" },
              { "Name": { "ru": "Металлическая дверь (Overpass)", "en": "Metal door (Overpass)" }, "Model": "models/props/de_overpass/overpass_metal_door01.vmdl" },
              { "Name": { "ru": "Дверь из жести 80x128", "en": "Sheet-metal door 80x128" }, "Model": "models/props/de_dust/hr_dust/dust_metal_panels/dust_metal_panel_door_80x128.vmdl" },
              { "Name": { "ru": "Гермодверь (Nuke)", "en": "Blast door (Nuke)" }, "Model": "models/props/de_nuke/hr_nuke/nuke_industrial_props/nuke_industrial_core_door_001a.vmdl" }
            ]
          },
          {
            "Category": { "ru": "Стены, щиты, барьеры", "en": "Walls, panels, barriers" },
            "Items": [
              { "Name": { "ru": "Разрушенная стена (Mirage)", "en": "Broken wall (Mirage)" }, "Model": "models/props/de_mirage/broken_wall_1.vmdl" },
              { "Name": { "ru": "Лист жести 64x128", "en": "Metal panel 64x128" }, "Model": "models/props/de_dust/hr_dust/dust_metal_panels/dust_metal_panel_01_64x128.vmdl" },
              { "Name": { "ru": "Лист жести 32x128", "en": "Metal panel 32x128" }, "Model": "models/props/de_dust/hr_dust/dust_metal_panels/dust_metal_panel_01_32x128.vmdl" },
              { "Name": { "ru": "Повреждённая стена 64", "en": "Damaged wall 64" }, "Model": "models/generic/damaged_wall_kit_01/damaged_wall_kit_01_64_01a.vmdl" },
              { "Name": { "ru": "Груда кирпича-сырца", "en": "Mud brick pile" }, "Model": "models/props/de_dust/hr_dust/dust_kasbah/dust_kasbah_mudbrick_pile_01.vmdl" },
              { "Name": { "ru": "Каменные блоки", "en": "Stone blocks" }, "Model": "models/de_mirage/construction/stoneblock_01c.vmdl" },
              { "Name": { "ru": "Вертикальное ограждение", "en": "Vertical barrier" }, "Model": "models/de_overpass/vertical_barrier/vertical_barrier_full.vmdl" },
              { "Name": { "ru": "Вертикальное ограждение (секция)", "en": "Vertical barrier (section)" }, "Model": "models/de_overpass/vertical_barrier/vertical_barrier_01.vmdl" },
              { "Name": { "ru": "Бетонный блок-барьер", "en": "Concrete barrier" }, "Model": "models/props/de_nuke/hr_nuke/nuke_concrete_barrier/nuke_concrete_barrier.vmdl" },
              { "Name": { "ru": "Бетонный блок 128", "en": "Concrete block 128" }, "Model": "models/props/de_nuke/hr_nuke/nuke_concrete_barrier/nuke_concrete_block128.vmdl" },
              { "Name": { "ru": "Бетонное ограждение", "en": "Concrete road barrier" }, "Model": "models/generic/barrier_concrete_kit_01/barrier_concrete_01.vmdl" },
              { "Name": { "ru": "Бетонное ограждение (вариант 2)", "en": "Concrete road barrier (variant 2)" }, "Model": "models/generic/barrier_concrete_kit_01/barrier_concrete_02.vmdl" },
              { "Name": { "ru": "Шлагбаум (основание)", "en": "Security barrier" }, "Model": "models/props/de_nuke/hr_nuke/security_barrier/security_barrier_base.vmdl" },
              { "Name": { "ru": "Мешки с цементом (большая стопка)", "en": "Cement bags (large stack)" }, "Model": "models/de_overpass/construction/cement_bags/cement_bag_stack_64.vmdl" },
              { "Name": { "ru": "Мешки с цементом (стопка)", "en": "Cement bags (stack)" }, "Model": "models/de_overpass/construction/cement_bags/cement_bag_stack_24.vmdl" },
              { "Name": { "ru": "Шлакоблок", "en": "Cinder block" }, "Model": "models/de_overpass/construction/cinderblock_1/cinderblock_1.vmdl" },
              { "Name": { "ru": "Стопка арматуры", "en": "Rebar stack" }, "Model": "models/props/de_dust/hr_dust/dust_construction/dust_rebar_stack.vmdl" },
              { "Name": { "ru": "Дорожный конус", "en": "Traffic cone" }, "Model": "models/de_overpass/construction/traffic_cone_1.vmdl" },
              { "Name": { "ru": "Строительные леса", "en": "Scaffolding" }, "Model": "models/props/de_dust/hr_dust/dust_scaffolds/dust_roof_scaffolds_tunnels_01.vmdl" }
            ]
          },
          {
            "Category": { "ru": "Контейнеры и ящики", "en": "Containers and crates" },
            "Items": [
              { "Name": { "ru": "Морской контейнер (длинный)", "en": "Shipping container (long)" }, "Model": "models/generic/shipping_container_01/shipping_container_01_120x448x136_01.vmdl" },
              { "Name": { "ru": "Морской контейнер (средний)", "en": "Shipping container (medium)" }, "Model": "models/generic/shipping_container_01/shipping_container_01_120x288x136_01.vmdl" },
              { "Name": { "ru": "Морской контейнер (короткий)", "en": "Shipping container (short)" }, "Model": "models/generic/shipping_container_01/shipping_container_01_120x160x136_01.vmdl" },
              { "Name": { "ru": "Малый контейнер", "en": "Small container" }, "Model": "models/generic/shipping_container_01/shipping_container_small_01_base_01.vmdl" },
              { "Name": { "ru": "Большой металлический ящик", "en": "Large metal crate" }, "Model": "models/props/de_nuke/hr_nuke/metal_crate_001/metal_crate_001_128x112x256.vmdl" },
              { "Name": { "ru": "Металлический ящик 96", "en": "Metal crate 96" }, "Model": "models/props/de_nuke/hr_nuke/metal_crate_001/metal_crate_001_96.vmdl" },
              { "Name": { "ru": "Металлический ящик 76", "en": "Metal crate 76" }, "Model": "models/props/de_nuke/hr_nuke/metal_crate_001/metal_crate_002_76.vmdl" },
              { "Name": { "ru": "Металлический ящик 32x64", "en": "Metal crate 32x64" }, "Model": "models/props/de_nuke/hr_nuke/metal_crate_001/metal_crate_004_32x64.vmdl" },
              { "Name": { "ru": "Деревянный ящик A", "en": "Wooden crate A" }, "Model": "models/generic/crate_wooden_01/crate_wooden_01_a.vmdl" },
              { "Name": { "ru": "Деревянный ящик B", "en": "Wooden crate B" }, "Model": "models/generic/crate_wooden_01/crate_wooden_01_b.vmdl" },
              { "Name": { "ru": "Деревянный ящик C", "en": "Wooden crate C" }, "Model": "models/generic/crate_wooden_01/crate_wooden_01_c.vmdl" },
              { "Name": { "ru": "Деревянный ящик D", "en": "Wooden crate D" }, "Model": "models/generic/crate_wooden_01/crate_wooden_01_d.vmdl" },
              { "Name": { "ru": "Деревянный ящик E", "en": "Wooden crate E" }, "Model": "models/generic/crate_wooden_01/crate_wooden_01_e.vmdl" },
              { "Name": { "ru": "Ящик Dust 32", "en": "Dust crate 32" }, "Model": "models/props/de_dust/hr_dust/dust_crates/dust_crate_style_01_32x32x32.vmdl" },
              { "Name": { "ru": "Ящик Dust 64", "en": "Dust crate 64" }, "Model": "models/props/de_dust/hr_dust/dust_crates/dust_crate_style_02_64x64x64.vmdl" },
              { "Name": { "ru": "Ящик Dust 72", "en": "Dust crate 72" }, "Model": "models/props/de_dust/hr_dust/dust_crates/dust_crate_style_02_72x72x72.vmdl" },
              { "Name": { "ru": "Высокий ящик Dust 37x74", "en": "Tall Dust crate 37x74" }, "Model": "models/props/de_dust/hr_dust/dust_crates/dust_crate_style_01_37x37x74.vmdl" },
              { "Name": { "ru": "Штабель ящиков", "en": "Crate stack" }, "Model": "models/props/de_dust/hr_dust/dust_crates/dust_crate_assembly_100x100_01.vmdl" },
              { "Name": { "ru": "Штабель ящиков под тентом", "en": "Crate stack with tarp" }, "Model": "models/props/de_dust/hr_dust/dust_crates/dust_crate_assembly_100x100_01_tarp.vmdl" },
              { "Name": { "ru": "Стопка ящиков для фруктов", "en": "Fruit crate stack" }, "Model": "models/props/de_dust/hr_dust/dust_crates/dust_wooden_fruit_crate_stack_01.vmdl" },
              { "Name": { "ru": "Стопка пластиковых ящиков", "en": "Plastic crate stack" }, "Model": "models/props/de_dust/hr_dust/dust_crates/dust_plastic_fruit_crate_stack_01.vmdl" },
              { "Name": { "ru": "Еврокуб (IBC)", "en": "IBC tank" }, "Model": "models/generic/crate_ibc_kit_01/crate_ibc_kit_large_01_a.vmdl" },
              { "Name": { "ru": "Пластиковый ящик (большой)", "en": "Plastic crate (large)" }, "Model": "models/generic/plastic_crate_kit_01/pkkit01_crate_01_a_large.vmdl" },
              { "Name": { "ru": "Деревянный ящик (Italy)", "en": "Wooden crate (Italy)" }, "Model": "models/cs_italy/crate/italy_wood_crate_1.vmdl" },
              { "Name": { "ru": "Деревянная бочка-ящик (Inferno)", "en": "Wooden barrel crate (Inferno)" }, "Model": "models/de_inferno/wood_barrel_01/inferno_crate_wood_03.vmdl" },
              { "Name": { "ru": "Военный кейс", "en": "Military case" }, "Model": "models/props/crates/military_case_02.vmdl" },
              { "Name": { "ru": "Склад боеприпасов", "en": "Ammo stash" }, "Model": "models/props/coop_cementplant/coop_ammo_stash/coop_ammo_stash_full.vmdl" }
            ]
          },
          {
            "Category": { "ru": "Поддоны, бочки, мешки, мусор", "en": "Pallets, barrels, sacks, trash" },
            "Items": [
              { "Name": { "ru": "Стопка поддонов", "en": "Pallet stack" }, "Model": "models/props/de_dust/hr_dust/dust_wood_pallet/dust_wood_pallet_stack_01.vmdl" },
              { "Name": { "ru": "Деревянный поддон", "en": "Wooden pallet" }, "Model": "models/props/de_dust/hr_dust/dust_wood_pallet/dust_wood_pallet_01.vmdl" },
              { "Name": { "ru": "Поддон с грузом", "en": "Loaded pallet" }, "Model": "models/de_mirage/industrial/industrial_wood_pallet/industrial_wood_pallet_01.vmdl" },
              { "Name": { "ru": "Металлический поддон", "en": "Metal pallet" }, "Model": "models/generic/pallet_metal_01/pallet_metal_01_96x100.vmdl" },
              { "Name": { "ru": "Бочка", "en": "Barrel" }, "Model": "models/generic/barrel_kit_01/barrel_01_a.vmdl" },
              { "Name": { "ru": "Группа бочек", "en": "Barrel group" }, "Model": "models/props/de_vertigo/barrel_group_tintable_01.vmdl" },
              { "Name": { "ru": "Бочка (Vertigo)", "en": "Barrel (Vertigo)" }, "Model": "models/props/de_vertigo/barrel_single_tintable_01.vmdl" },
              { "Name": { "ru": "Деревянная бочка", "en": "Wooden barrel" }, "Model": "models/props/de_inferno/hr_i/barrel_a/barrel_a_full.vmdl" },
              { "Name": { "ru": "Ржавая бочка", "en": "Rusty barrel" }, "Model": "models/props/de_dust/hr_dust/dust_rusty_barrel_hr/dust_rusty_barrel_hr.vmdl" },
              { "Name": { "ru": "Стальная бочка", "en": "Steel drum" }, "Model": "models/props/containers/barrels/steel_drum_barrel_01a.vmdl" },
              { "Name": { "ru": "Тюк сена", "en": "Hay bale" }, "Model": "models/props/de_inferno/hr_i/hay_bale/hay_bale_a.vmdl" },
              { "Name": { "ru": "Мешки с мукой", "en": "Flour sacks" }, "Model": "models/props/de_dust/hr_dust/dust_flour_sack/dust_flour_sack_stack_01.vmdl" },
              { "Name": { "ru": "Биг-бэг", "en": "Bulk bag" }, "Model": "models/de_overpass/construction/cargo_big_bag/cargo_big_bag_1.vmdl" },
              { "Name": { "ru": "Газовые баллоны", "en": "Propane tanks" }, "Model": "models/props/de_dust/hr_dust/dust_propane_tanks/propane_tank_01_cluster_01.vmdl" },
              { "Name": { "ru": "Стопка шин", "en": "Tire stack" }, "Model": "models/props/de_nuke/hr_nuke/nuke_forklift/nuke_tire_stack.vmdl" },
              { "Name": { "ru": "Мусорный бак (большой)", "en": "Dumpster" }, "Model": "models/props/de_dust/hr_dust/dust_garbage_container/dust_garbage_dumpster.vmdl" },
              { "Name": { "ru": "Мусорный контейнер", "en": "Garbage container" }, "Model": "models/props/de_dust/hr_dust/dust_garbage_container/dust_garbage_container.vmdl" },
              { "Name": { "ru": "Мусорный бак (Italy)", "en": "Dumpster (Italy)" }, "Model": "models/cs_italy/props/dumpster/italy_dumpster_1.vmdl" },
              { "Name": { "ru": "Куча мусорных мешков", "en": "Trash bag pile" }, "Model": "models/props/de_dust/hr_dust/dust_garbage_container/dust_trash_bag_pile.vmdl" }
            ]
          },
          {
            "Category": { "ru": "Транспорт", "en": "Vehicles" },
            "Items": [
              { "Name": { "ru": "Грузовик (Overpass)", "en": "Truck (Overpass)" }, "Model": "models/de_overpass/vehicles/flatnose_truck/flatnose_truck_generic_1.vmdl" },
              { "Name": { "ru": "Разбитый грузовик", "en": "Wrecked truck" }, "Model": "models/de_overpass/vehicles/flatnose_truck/flatnose_truck_wrecked_1.vmdl" },
              { "Name": { "ru": "Грузовик (Nuke)", "en": "Truck (Nuke)" }, "Model": "models/props/de_nuke/hr_nuke/nuke_cars/nuke_truck_01.vmdl" },
              { "Name": { "ru": "Остов машины", "en": "Car wreck" }, "Model": "models/props/de_dust/hr_dust/dust_vehicles/dust_car_wreck_body.vmdl" },
              { "Name": { "ru": "Машина под тентом", "en": "Car under tarp" }, "Model": "models/de_overpass/vehicles/car_tarp_cover/car_tarp_cover_1.vmdl" },
              { "Name": { "ru": "Погрузчик", "en": "Forklift" }, "Model": "models/props/de_nuke/hr_nuke/nuke_forklift/nuke_forklift_base.vmdl" },
              { "Name": { "ru": "Багажная тележка", "en": "Baggage cart" }, "Model": "models/generic/baggage_cart_01/baggage_cart_01_body_01.vmdl" },
              { "Name": { "ru": "Тележка торговца", "en": "Market cart" }, "Model": "models/props/de_dust/hr_dust/dust_cart/dust_cart.vmdl" },
              { "Name": { "ru": "Маневровый локомотив", "en": "Shunter locomotive" }, "Model": "models/props/de_train/hr_train_s2/train_railcars/train_locomotive_shunter_01.vmdl" }
            ]
          },
          {
            "Category": { "ru": "Разное", "en": "Misc" },
            "Items": [
              { "Name": { "ru": "Биотуалет", "en": "Portable toilet" }, "Model": "models/de_overpass/construction/portapotty/portapotty_closed.vmdl" },
              { "Name": { "ru": "Генератор", "en": "Power generator" }, "Model": "models/de_overpass/construction/power_generator/power_generator_1.vmdl" },
              { "Name": { "ru": "Шкафчики", "en": "Lockers" }, "Model": "models/generic/lockers_01/lockers_cabinet_3x3.vmdl" },
              { "Name": { "ru": "Стеллаж", "en": "Shelving" }, "Model": "models/generic/interior_shelving/interior_shelving_001_a.vmdl" },
              { "Name": { "ru": "Торговый автомат", "en": "Vending machine" }, "Model": "models/props/de_nuke/hr_nuke/nuke_vending_machine/nuke_vending_machine.vmdl" },
              { "Name": { "ru": "Диван (Mirage)", "en": "Couch (Mirage)" }, "Model": "models/props/de_mirage/couch_a.vmdl" },
              { "Name": { "ru": "Металлические козлы", "en": "Metal sawhorse" }, "Model": "models/props/ar_dizzy/dizzy_metal_sawhorse/dizzy_metal_sawhorse.vmdl" },
              { "Name": { "ru": "Пальма", "en": "Palm tree" }, "Model": "models/props/de_dust/hr_dust/foliage/palm_tree_01.vmdl" },
              { "Name": { "ru": "Скала", "en": "Rock" }, "Model": "models/props/dust_massive/rocks/cliff_rock002.vmdl" }
            ]
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
