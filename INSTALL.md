# BlockingZones 0.1.0 — install

## EN

### What you need

- CounterStrikeSharp 1.0.374+ (built against 1.0.377)
- PanoramaMenuManager (MenuManagerCore) **1.3.00+** and `shared/MenuManagerApi` of the same version
- A client addon for MultiAddonManager (the one that already carries the PMM panorama) and CS2 Workshop Tools to compile it

### Server

Copy `Server-plugins/counterstrikesharp` from the release (or your build) into `game/csgo/addons/`. You get:

- `addons/counterstrikesharp/plugins/BlockingZones/BlockingZones.dll` and `lang/`
- `addons/counterstrikesharp/configs/plugins/BlockingZones/config.jsonc`, `items.jsonc` (do not overwrite your own)

`MenuManagerApi.dll` is not put into the plugin folder — only into `shared/`. Change the map after the install (models are precached on map load).

### Client addon

Run `tools/build-addon.cmd`: it asks for the addon name, copies `BlockingZones/panorama` into it and compiles `bz.xml` and `bz.css`.
Update the addon in the Workshop; players reconnect. Without the addon a player just sees no panel — the plugin still works.

### Map layouts

The plugin writes `configs/plugins/BlockingZones/maps/<map>.json` (workshop maps: `maps/workshop/<id>/<map>.json`) when you press "Save items".
`templates/de_mirage.json` can be copied there as `maps/de_mirage.json` by hand.

## RU

### Что нужно

- CounterStrikeSharp 1.0.374+ (собрано под 1.0.377)
- PanoramaMenuManager (MenuManagerCore) **1.3.00+** и `shared/MenuManagerApi` той же версии
- Клиентский аддон MultiAddonManager (тот, где уже лежит панорама PMM) и CS2 Workshop Tools для его сборки

### Сервер

Скопируй `Server-plugins/counterstrikesharp` из релиза (или своей сборки) в `game/csgo/addons/`. Получится:

- `addons/counterstrikesharp/plugins/BlockingZones/BlockingZones.dll` и `lang/`
- `addons/counterstrikesharp/configs/plugins/BlockingZones/config.jsonc`, `items.jsonc` (свои не перезаписывай)

`MenuManagerApi.dll` в папку плагина не кладётся — только в `shared/`. После установки смени карту (модели прекэшируются при загрузке карты).

### Клиентский аддон

Запусти `tools/build-addon.cmd`: спросит имя аддона, скопирует в него `BlockingZones/panorama` и скомпилирует `bz.xml` и `bz.css`.
Обнови аддон в Workshop, игроки перезаходят. Без аддона игрок просто не видит панель — плагин работает.

### Раскладки карт

Плагин пишет `configs/plugins/BlockingZones/maps/<map>.json` (workshop-карты: `maps/workshop/<id>/<map>.json`) по пункту «Сохранить предметы».
`templates/de_mirage.json` можно вручную положить туда как `maps/de_mirage.json`.
