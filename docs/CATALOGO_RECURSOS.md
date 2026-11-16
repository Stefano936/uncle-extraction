# Catálogo de recursos integrados

Estados: **importado** (el archivo está en el proyecto y Unity lo importa), **colocado** (aparece en un escenario), **funcional** (tiene colisión o comportamiento de juego verificado) y **pendiente de producción** (prototipo o placeholder que debe reemplazarse). La fuente original de cada recurso está en `preproduction/`; `preproduction/uncle_extraction/data/asset_manifest.csv` no se modifica (es la entrega original).

## Modelos (kit 3D de prototipo, procedural, sin terceros; `Assets/Game/Art/Kit3D/`)

Importación verificada con `Kit3DImporter.ImportAndReport`: metros, Y arriba, sin conversión doble.

| Recurso | Importado | Colocado | Funcional | Notas / producción |
| --- | --- | --- | --- | --- |
| `bar_counter.fbx` (4,12 × 1,08 × 0,9 m) | sí | Bar_LastRound | sí: colisión de caja del mismo tamaño y baranda de anclaje primitiva delante | Pendiente de producción. Sus marcadores `Npc_Anchor_L/R` no se usan: la baranda es una primitiva. |
| `table.fbx` (1,40 × 0,82 × 0,90 m) | sí | Bar_LastRound (4) | sí: colisión de caja | Pendiente de producción. |
| `stool.fbx` (0,54 × 0,83 m) | sí | Bar_LastRound (2) | sí: colisión de caja | Pendiente de producción. El banco del tío sigue siendo un cilindro sin colisión. |
| `exit_door.fbx` (1,5 × 2,2 m, origen en la bisagra) | sí | Bar_LastRound (girada 180°) | sí: visual sobre el panel con bisagra existente | Pendiente de producción. |
| `passenger_van.fbx` (2,52 × 5,38 × 2,33 m) | sí | no | — | No colocada: sus puertas traseras son parte del modelo y no coinciden con las hojas animadas de `VanDoors`. Por ahora la camioneta es de primitivas con las mismas medidas. |
| `transport_cart.fbx` (1,2 × 0,86 m) | sí | no | — | No colocado: sus proporciones no coinciden con la caja de carga del juego (0,9 × 1,4 m, para el tío acostado de 1,9 m). Hay que rehacerlo o adaptar la caja. |
| `uncle_prototype.fbx` (1,93 m, 12 huesos, Idle/Push/Kick) | sí | Bar_LastRound y PhysicsLab_Articulated | sí: malla ligada a los 11 cuerpos del ragdoll reducido (`RagdollVisual`); colisiones propias por parte | Pendiente de producción: es el modelo de prototipo del kit; las animaciones Idle/Push/Kick no se usan (la pose la da la física) y las señales de ataque siguen siendo la esfera amarilla. PhysicsLab conserva el tío de primitivas como respaldo. |
| `bar_blockout_shell.fbx` (18,2 × 17,1 m) | sí | no | — | Referencia de distribución. No se coloca porque duplicaría el suelo y las paredes. |

## Escenarios

| Escena | Estado |
| --- | --- |
| `Frontend` | Menú provisional (IMGUI); pendiente el kit de UI. |
| `Bar_LastRound` | Escenario principal: salón de 18 × 12 m, barra, mesas, banquetas, karaoke, salida de 1,5 m, vereda, camioneta. Geometría verificada (`LevelGeometryTests`). Paredes, piso y camioneta de primitivas. |
| `PhysicsLab` | Laboratorio de diagnóstico (primitivas). |

## Materiales, texturas, UI, audio

| Recurso | Estado |
| --- | --- |
| Kit de materiales (`preproduction/uncle_extraction_kit_materiales/`) | Pendiente de importar: solo Base Color de 1254 px, sin normal, roughness ni AO. |
| Kit de UI (`preproduction/uncle_extraction_kit_UI/`) | Pendiente: la interfaz actual es IMGUI provisional. |
| Audio y diálogos | Pendiente: no hay sonido. Los textos EN/es están en `preproduction/uncle_extraction/data/dialogue_en_es.csv` (sin integrar). |
| Materiales de color de las primitivas (`Assets/Game/Materials/`) | Placeholders generados por el constructor. |
