# Catálogo de recursos integrados

Estados: **importado** (el archivo está en el proyecto y Unity lo importa), **colocado** (aparece en un escenario), **funcional** (tiene colisión o comportamiento de juego verificado) y **pendiente de producción** (prototipo o placeholder que debe reemplazarse). La fuente original de cada recurso está en `preproduction/`; `preproduction/uncle_extraction/data/asset_manifest.csv` no se modifica (es la entrega original).

## Modelos (kit 3D de prototipo, procedural, sin terceros; `Assets/Game/Art/Kit3D/`)

Importación verificada con `Kit3DImporter.ImportAndReport`: metros, Y arriba, sin conversión doble.

| Recurso | Importado | Colocado | Funcional | Notas / producción |
| --- | --- | --- | --- | --- |
| `bar_counter.fbx` (4,12 × 1,08 × 0,9 m) | sí | Bar_LastRound | sí: colisión de caja del mismo tamaño y baranda de anclaje primitiva delante | Pendiente de producción. Sus marcadores `Npc_Anchor_L/R` no se usan: la baranda es una primitiva. |
| `table.fbx` (1,40 × 0,82 × 0,90 m) | sí | Bar_LastRound (4) | sí: colisión de caja | Pendiente de producción. |
| `stool.fbx` (0,54 × 0,83 m) | sí | Bar_LastRound (2) | sí: colisión de caja | Pendiente de producción. El banco del tío sigue siendo un cilindro sin colisión. |
| `exit_door.fbx` (1,5 × 2,2 m, origen en la bisagra) | sí | Bar_LastRound (girada 180°) | sí: visual sobre el panel con bisagra; manija agarrable `door_handle` en la posición de `Interact_Handle` del kit (1,3 m de la bisagra, 1,0 m de alto) | Se cierra sola: 25 N en el borde a 45° y 64 N a 90° (medido). Vuelve cerrada al reiniciar. Pendiente de producción. |
| `passenger_van.fbx` (2,52 × 5,38 × 2,33 m; piso de carga a 0,64 m, carga de 2,65 m) | sí | Bar_LastRound (como carrocería) | visual: `KitDressing.Van` ajusta techo, laterales, cabina, chasis, ruedas y parabrisas a la caja funcional (2,50 × 5,40 × 2,30 m) | **Migración documentada:** se conservan las medidas funcionales: piso a 0,30 m (la rampa de 7° está balanceada para arrastrar), zona de carga de 3,15 m, abertura y hojas de `VanDoors`. Del kit se ocultan el piso de carga, el banco y las puertas traseras, que vienen modeladas abiertas y no coinciden con las hojas animadas. El piso funcional y las hojas toman los materiales del kit. Pendiente de producción. |
| `transport_cart.fbx` (plataforma de 1,20 × 0,80 m con el manubrio en el lado largo) | sí | Bar_LastRound | visual: `KitDressing.Cart` ajusta plataforma, ruedas y manubrio a la caja funcional (0,90 × 1,40 m, asa en el extremo corto a 1,0 m, socket `cart_handle` sin cambios) | **Migración documentada:** se conserva la geometría funcional, porque el asa en el extremo corto hace falta para pasar al tío acostado (1,9 m) por puertas de 1,5 m. Las barandas laterales y el frente siguen siendo primitivas, con el material del kit. Pendiente de producción. |
| `uncle_prototype.fbx` (1,93 m, 12 huesos, Idle/Push/Kick) | sí | Bar_LastRound y PhysicsLab_Articulated | sí: malla ligada a los 11 cuerpos del ragdoll reducido (`RagdollVisual`); colisiones propias por parte | **Animaciones:** `Push_Prototype` (golpe) y `Kick_Prototype` (preparación y golpe) se muestrean al construir la escena y se convierten en objetivos de articulación (`AttackPoseBuilder`). La preparación del empujón del clip mueve 9 cm, así que se usa una autorizada; manotazo y sacudida son autorizadas. `Idle_Prototype` no se usa (de pie lo sostiene la física). Pendiente de producción. PhysicsLab conserva el tío de primitivas como respaldo. |
| `bar_blockout_shell.fbx` (18,2 × 17,1 m) | sí | no | — | Referencia de distribución. No se coloca porque duplicaría el suelo y las paredes. |

## Escenarios

| Escena | Estado |
| --- | --- |
| `Frontend` | Menú con el kit de UI (fondo `bar_idle.png` del kit). |
| `Bar_LastRound` | Escenario principal: salón de 18 × 12 m, barra, mesas, banquetas, karaoke, salida de 1,5 m, vereda, camioneta. Geometría verificada (`LevelGeometryTests`). Paredes y piso con materiales del kit; carrito y camioneta vestidos con piezas del kit sobre la geometría funcional. |
| `PhysicsLab` | Laboratorio de diagnóstico (primitivas). |

## Materiales, texturas, UI, audio

| Recurso | Estado |
| --- | --- |
| Kit de materiales (`preproduction/uncle_extraction_kit_materiales/`) | **Importado y colocado** (Bar_LastRound): madera, piso y pared como URP/Lit (`Assets/Game/Art/Materials/`), texturas a 1024 px (fuente 1254 px), UV en metros generadas por `KitMaterials`. Tela (`MAT_Shirt_Fabric`) importable pero sin usar. Sin normal, roughness ni AO (no entregados). Variantes FBX `*_material_variant` sin usar todavía. |
| Kit de UI (`preproduction/uncle_extraction_kit_UI/`) | **Importado y en uso** (`UiKitImporter`, `Assets/Game/Art/UI/`):
- 11 sprites con los bordes 9-slice de `sprite_import.json`, 20 iconos PNG de 64 px y el fondo `bar_idle.png`.
- Fuentes DejaVu sin modificar, con `FONT_LICENSE.txt`, como fuente dinámica de uGUI. No se usa TMP: requiere TMP Essentials, cuya importación es interactiva.
- Catálogo EN/es_UY copiado sin cambios a `Resources/Localization/ui_strings.json`; los textos propios del juego están en `game_strings.json`.
- Pantallas en uso: HUD, menú, pausa, resultados y opciones.
- Pendiente: lobby, pantalla de errores de conexión y pérdida del anfitrión con el kit; validación a 720p, ultrawide y texto al 150 % por una persona.
- Las capturas de `screens/` no se importan. |
| Audio y diálogos | Pendiente: no hay sonido. Los textos EN/es están en `preproduction/uncle_extraction/data/dialogue_en_es.csv` (sin integrar). |
| Materiales de color de las primitivas (`Assets/Game/Materials/`) | Placeholders generados por el constructor. |
