# Origen y licencia de los recursos

Inventario de lo que entra en la build. Lo pide la biblia 03: «Si utiliza un asset de terceros, debe conservar origen y licencia en CREDITS_ASSETS.md». Los recursos de producción y el contenido creado con IA se inventarían acá antes de publicar.

| Recurso | Dónde está | Origen | Licencia / condición | Estado |
| --- | --- | --- | --- | --- |
| Modelos 3D (tío, barra, mesa, banqueta, puerta, carrito, camioneta, cascarón) | `Assets/Game/Art/Kit3D/` | Kit 3D de prototipo del proyecto (`preproduction/uncle_extraction_kit_3D/`), generado proceduralmente; su README declara que no usa terceros | Propio del proyecto | Prototipo; pendiente de producción |
| Animaciones `Push_Prototype` y `Kick_Prototype` | Dentro de `uncle_prototype.fbx` | Kit 3D | Propio del proyecto | Muestreadas como poses de ataque; aproximadas |
| Texturas de madera, piso, pared y tela | `Assets/Game/Art/Materials/Textures/` | Kit de materiales del proyecto (`preproduction/uncle_extraction_kit_materiales/`) | Propio del proyecto (ver su README) | En uso: madera, piso y pared |
| Materiales s19 de barra, baldosas, revoque y salida | `Assets/Game/Art/Materials/Textures/S19/` | Generación procedural original con semilla fija, campos de altura y máscaras de desgaste. Fuente en `tools/art/generate_s19_surfaces.py` | Propio del proyecto | Integrados en Bar_LastRound |
| Detalle de superficies, trama de ropa y mapas normales | `Assets/Game/Art/Materials/Textures/` | `tools/art/refine_surfaces.py` combina los mapas del corte S21 con ruido periódico por material. La trama y el detalle se generan por código a partir de semillas fijas. | Propio del proyecto | Bar, exteriores, utilería, camioneta y personajes |
| Sprites, íconos y fondo de interfaz | `Assets/Game/Art/UI/` | Kit de UI del proyecto (`preproduction/uncle_extraction_kit_UI/`). Los íconos y sprites se construyeron por código; el fondo es un render del kit 3D | Propio del proyecto | En uso |
| Fuentes DejaVu Sans y DejaVu Sans Bold | `Assets/Game/Art/UI/Fonts/` | DejaVu Fonts, sin modificar, distribuidas con el kit de UI | Licencia libre de DejaVu (derivada de Bitstream Vera), con aviso completo en `FONT_LICENSE.txt`, que se conserva junto a los TTF | En uso |
| Catálogo de textos EN/es_UY de la interfaz | `Assets/Game/Resources/Localization/ui_strings.json` | Kit de UI (`data/localization_en_es.json`, sin cambios) | Propio del proyecto | En uso |
| Textos propios del juego EN/es_UY | `Assets/Game/Resources/Localization/game_strings.json` | Escritos en este repositorio | Propio del proyecto | En uso |
| Catálogo de diálogos del tío | `Assets/Game/Resources/Localization/dialogue_en_es.csv` | Paquete original (`preproduction/uncle_extraction/data/dialogue_en_es.csv`, sin cambios) | Propio del proyecto | En uso como subtítulos. **Sin voz**: `audio_status=NOT_RECORDED`. Las voces finales necesitan actor o una voz licenciada con derechos de uso comercial (biblia 03, sección 08). |
| Efectos de sonido (agarrar, soltar, impacto, golpe, señales de los cuatro ataques, puertas, victoria, derrota) | Generados al ejecutar (`SfxSynth.cs`) | Sintetizados por el código del juego: no hay archivos de audio ni muestras de terceros | Propio del proyecto | Provisionales; pendiente de producción |
| Escenario nocturno: techo, terminaciones, carteles, calle, edificios, camioneta, luces, cielo | Generado al construir la escena (`Editor/BarDressing.cs`) | Geometría primitiva y materiales escritos en este repositorio; cielo con el sombreador procedural de Unity | Propio del proyecto | En uso |
| Texturas de asfalto y losetas | `Assets/Game/Art/Materials/Textures/Generated/` | Ruido procedural generado por `BarDressing` (sin fuentes externas) | Propio del proyecto | En uso |
| Sombreador de texto 3D `UncleExtraction/WorldText` | `Assets/Game/Art/Shaders/WorldText.shader` | Escrito en este repositorio | Propio del proyecto | En uso |
| Sonidos de la sesión 15 (contacto del clic, resbalón, tensión de tela, ruedas y golpes del carrito, pestillo, ambiente de bar, remate de victoria, vasos) | Generados al ejecutar (`SfxSynth.cs`) | Síntesis propia en el código | Propio del proyecto | En uso; provisionales |
| Manos, ropa del tío, detalles de la camioneta, barricada y torre de vasos | Generados al ejecutar (`HandRig`, `UncleOutfit`, `VanDetails`, `Challenges`, `BarProps`) | Primitivas y materiales escritos en este repositorio | Propio del proyecto | En uso |
| Líneas nuevas del tío y del barman (DLG_101–DLG_109) | `dialogue_en_es.csv` | Escritas en este repositorio | Propio del proyecto | Subtítulos; sin voz (guion grabable en `docs/GUION_VOZ.md`) |
| Clips y cápsulas de tienda (borradores) | `docs/lanzamiento/` | Capturas del juego real | Propio del proyecto | Borradores para revisión |
| Música | — | — | — | No hay |
