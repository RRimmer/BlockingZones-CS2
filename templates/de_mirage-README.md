# Шаблон de_mirage для BlockingZones (из CS:GO-конфига)

Файл `de_mirage.json` — в `game/csgo/addons/counterstrikesharp/configs/plugins/BlockingZones/maps/`, затем смена карты
(модели прекэшируются при загрузке карты). Если на сервере уже есть свой `de_mirage.json` — сохраните его копию.

Сейчас в шаблоне **202** предмета. Из CS:GO-конфига (282) перенесено 228; потом убраны 15 предметов
с моделями без текстур в CS2 и переделан коннектор (42 старых предмета → 31 новый).

Все модели шаблона сверены со списком файлов CS2 (`pak01_dir.txt`, SteamDatabase/GameTracking-CS2) — каждая есть в игре.
Позиции, углы, видимость (`Invisibility` → `Alpha`) и квоты взяты из оригинала без изменений; углы приведены к −180…180.

## Что проверить в игре

Координаты — из CS:GO Mirage. Карта в CS2 перенесена с той же раскладкой, но точка привязки и размер у **заменённых**
моделей другие, поэтому главные «стены» стоит осмотреть: `!bz` → 6 «Показать все предметы», затем навести прицел →
3 «Настройки» → 1 «Передвинуть» / 2 «Поворот». После правок — 5 «Сохранить».

- Полупрозрачные двери на A (3 шт., 1133…1322 / 139…147) и в коннекторе/окнах (около −865 / 55): были `autumn_sliding_door`, стали рольставни 96×128.
- Бетонные стенки 96 (−736…−561 / −1127, и −1010 / −87): тоже рольставни 96×128, alpha 51.
- Невидимые стены (alpha 0, 3 шт. `fence03a` → сетка 256): 521 / 812, −517 / −1158, −949 / −7.
- Решётки (`dust_underpass_gate` → сетка-калитка 128): −1771 / −597, −1477 / 177, −1467 / 454.
- Гаражные ворота (6 шт. `survival_garage_door` → рольставня 128×128): у оригинала модель лежала, поэтому наклон обнулён,
  поворот по Y сохранён — проверьте, что ворота стоят в проёме, а не поперёк.
- Машины (`hmmwv`, `swat_van` ×2, `police_car_city` → грузовик Overpass) крупнее/длиннее оригиналов — могут влезать в стены.

## Замены

| Шт. | Было (CS:GO) | Стало (CS2) |
|---|---|---|
| 4 | moon_rock_01 | `props/ar_lunacy/moon_rocks/moon_rock_02` |
| 4 | moon_rock_03 | `props/ar_lunacy/moon_rocks/moon_rock_02` |
| 1 | moon_rock_04 | `props/ar_lunacy/moon_rocks/moon_rock_02` |
| 2 | moon_rock_05 | `props/ar_lunacy/moon_rocks/moon_rock_02` |
| 8 | autumn_sliding_door | `props/de_dust/hr_dust/dust_windows/dust_rollupdoor_96x128_surface_lod` |
| 1 | fraggrenade_pile | `props/coop_cementplant/grenade_box/fraggrenade_box` |
| 1 | coop_bunk_bed | `props/de_house/bed_rustic` |
| 1 | painting_01 | `de_inferno/inferno_paintings_01/inferno_painting_01` |
| 1 | painting_02 | `de_inferno/inferno_paintings_01/inferno_painting_02` |
| 1 | sick_bed | `props/de_house/bed_rustic` |
| 1 | crate_extrasmallmill | `cs_italy/crate/italy_wood_crate_1` |
| 1 | haybale_target_03 | `props/de_inferno/hr_i/hay_bale/hay_bale_a` |
| 1 | logpile2 | `props/de_inferno/logpile_new` |
| 1 | table_kitchen | `de_cache_s2/table_01/ch2_table_01` |
| 1 | offpaintingd | `props/cs_office/offpaintingf` |
| 3 | dust_underpass_gate | `props/de_dust/hr_dust/dust_fences/dust_chainlink_fence_gate_001_128` |
| 4 | de_house_table01 | `de_cache_s2/table_01/ch2_table_01` |
| 2 | swat_van | `de_overpass/vehicles/flatnose_truck/flatnose_truck_generic_1` |
| 2 | picture1_static | `props/cs_office/offpaintinga` |
| 1 | basilica_door_2 | `props/de_mirage/large_door_c` |
| 1 | venice_police_barrier_taped_02 | `generic/barrier_concrete_kit_01/barrier_concrete_01` |
| 6 | survival_garage_door | `props/de_dust/hr_dust/dust_doors/dust_rollupdoor_128x128_surface_01` |
| 3 | fence03a | `props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_256_capped` |
| 3 | lamppost03a_off | `props/de_inferno/light_streetlight` |
| 1 | oildrum001 | `props/de_vertigo/barrel_single_tintable_01` |
| 4 | concrete_wall001_96_reference | `props/de_dust/hr_dust/dust_windows/dust_rollupdoor_96x128_surface_lod` |
| 1 | briefcase | `de_overpass/luggage/suitcase/suitcase_003` |
| 2 | case_explosive | `props/crates/military_case_02` |
| 1 | case_heavy_weapon | `props/crates/military_case_02` |
| 1 | case_paradrop_static | `props/crates/military_case_02` |
| 2 | case_pistol | `props/crates/military_case_02` |
| 1 | case_tools | `props/crates/military_case_02` |
| 1 | crate_ammobox | `cs_italy/crate/italy_wood_crate_1` |
| 1 | upgrade_dz_armor_helmet | `weapons/w_eq_armor_helmet` |
| 1 | upgrade_heavy_armor | `weapons/w_eq_assault_suit` |
| 1 | hotel_halfmoon_table001 | `de_cache_s2/table_01/ch2_table_01` |
| 1 | hmmwv | `de_overpass/vehicles/flatnose_truck/flatnose_truck_generic_1` |
| 1 | police_car_city | `de_overpass/vehicles/flatnose_truck/flatnose_truck_generic_1` |
| 3 | window_farmhouse_big | `anubis/dust_kasbah/dust_kasbah_window_36x64_wood_01` |

## Выброшено

| Шт. | Модель | Почему |
|---|---|---|
| 1 | street_basketball_hoop | в CS2 нет модели и нет похожей замены |
| 1 | autumn_2_briefing | в CS2 нет модели и нет похожей замены |
| 1 | autumn_curtain | в CS2 нет модели и нет похожей замены |
| 2 | coop_coarkboard | в CS2 нет модели и нет похожей замены |
| 8 | coop_wooden_table_deco01 | в CS2 нет модели и нет похожей замены |
| 4 | coop_wooden_table_deco02 | в CS2 нет модели и нет похожей замены |
| 1 | phoenix_camcorder | в CS2 нет модели и нет похожей замены |
| 1 | bed_cloth | в CS2 нет модели и нет похожей замены |
| 1 | hospital_bucket | в CS2 нет модели и нет похожей замены |
| 1 | instrument_table | в CS2 нет модели и нет похожей замены |
| 1 | instrument_table_cloth | в CS2 нет модели и нет похожей замены |
| 1 | sick_bed_iv | в CS2 нет модели и нет похожей замены |
| 1 | surgical_instruments | в CS2 нет модели и нет похожей замены |
| 1 | surgical_lamp | в CS2 нет модели и нет похожей замены |
| 1 | wheelchair | в CS2 нет модели и нет похожей замены |
| 2 | newspaperstack01 | в CS2 нет модели и нет похожей замены |
| 4 | knight_armour | в CS2 нет модели и нет похожей замены |
| 1 | propane_tank_01_cluster_01 | точный дубликат (та же модель в той же точке) |
| 1 | soccer_goal | в CS2 нет модели и нет похожей замены |
| 1 | inferno_bike | в CS2 нет модели и нет похожей замены |
| 1 | street_sign_a_d | в CS2 нет модели и нет похожей замены |
| 3 | lifepreserver | в CS2 нет модели и нет похожей замены |
| 1 | studio_spotlight_small_off | в CS2 нет модели и нет похожей замены |
| 2 | target_bullseye | в CS2 нет модели и нет похожей замены |
| 3 | mall_mannequin_rarm1 | в CS2 нет модели и нет похожей замены |
| 1 | mall_mannequin_torso2 | в CS2 нет модели и нет похожей замены |
| 1 | v_knife_default_ct | в точке 0 0 0 — мусор оригинала |
| 2 | w_defuser | в CS2 у оружия другие модели, «брошенного» оружия-пропа нет |
| 1 | w_pist_223_dropped | в CS2 у оружия другие модели, «брошенного» оружия-пропа нет |
| 2 | w_pist_p250_dropped | в CS2 у оружия другие модели, «брошенного» оружия-пропа нет |
| 1 | w_rif_ak47_dropped | в CS2 у оружия другие модели, «брошенного» оружия-пропа нет |
| 1 | w_snip_awp_dropped | в CS2 у оружия другие модели, «брошенного» оружия-пропа нет |

Квоты: 4 — 116 предметов (A, рампа, магазин и т.п.), 10 — 112 предметов. Видимость: 255 — 209, 51 — 14, 0 — 5.

## Коннектор: чайная лавка (переделан)

Старый декор коннектора (офисный стол с компьютером и снеками, автомат, аквариум, таблички и камеры Nuke,
часы, EXIT, 9 картин, дрова, баллоны, мешки с мусором, бочка, катушка кабеля, метла) убран.
Блокираторы остались как были: грузовик в арке, бетонный барьер, три полупрозрачные стенки 96×128, невидимый забор.

Новый декор (все модели CS2, у каждой проверены текстуры), квота 4 как у старого декора:

| Где | Что |
|---|---|
| Западная платформа (на месте стола) | ковёр Inferno, диван Mirage у стены, низкий столик, 3 напольные подушки Mirage, корзина, подвесной фонарь Dust |
| Западная стена, южная часть (на месте картин) | книжная полка, 2 настенные лампы |
| Место автомата / кабеля | большое кашпо, растение в горшке, рыночная тележка с ковром |
| Восточный проход (дрова, баллоны, мешки, бочка) | мешки муки, набор глиняных горшков, ящик с фруктами, 3 ящика (один сверху), деревянная бочка, мешок |
| Северо-восточный угол (метла) | высокий кувшин, корзина |
| Стены (таблички, часы, камеры, EXIT) | 5 настенных ламп |
| Нижний уровень, у лестницы | второй ковёр Inferno |

Точки и углы взяты у старых предметов, которые в CS2 стояли правильно, но размеры и «перёд» у новых моделей другие.
После установки пройдитесь по коннектору: `!bz` → 6 «Показать все», прицел на предмет → 3 «Настройки» →
1 «Передвинуть» / 2 «Поворот», затем 5 «Сохранить». Чаще всего поправить нужно: настенные лампы (могут смотреть
в стену — поверните на 180° по Y), подвесной фонарь (высота зависит от потолка), тележку и полку (у стен).

## Убрано после проверки в игре

Модели есть в CS2, но у них нет текстур (`Failed loading resource … File not found`):
`moon_rock_02` (12 шт.), `radiator01a` (2), `furniture_couch01a` (1).
