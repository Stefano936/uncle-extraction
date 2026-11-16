# Origen y licencia de los recursos

Inventario de lo que entra en la build. Lo pide la biblia 03: «Si utiliza un asset de terceros, debe conservar origen y licencia en CREDITS_ASSETS.md». Los recursos de producción y el contenido creado con IA se inventarían acá antes de publicar.

| Recurso | Dónde está | Origen | Licencia / condición | Estado |
| --- | --- | --- | --- | --- |
| Modelos 3D (tío, barra, mesa, banqueta, puerta, carrito, camioneta, cascarón) | `Assets/Game/Art/Kit3D/` | Kit 3D de prototipo del proyecto (`preproduction/uncle_extraction_kit_3D/`), generado proceduralmente; su README declara que no usa terceros | Propio del proyecto | Prototipo; pendiente de producción |
| Animaciones `Push_Prototype` y `Kick_Prototype` | Dentro de `uncle_prototype.fbx` | Kit 3D | Propio del proyecto | Muestreadas como poses de ataque; aproximadas |
| Texturas de madera, piso, pared y tela | `Assets/Game/Art/Materials/Textures/` | Kit de materiales del proyecto (`preproduction/uncle_extraction_kit_materiales/`) | Propio del proyecto (ver su README) | En uso: madera, piso y pared |
| Sprites, íconos y fondo de interfaz | `Assets/Game/Art/UI/` | Kit de UI del proyecto (`preproduction/uncle_extraction_kit_UI/`). Los íconos y sprites se construyeron por código; el fondo es un render del kit 3D | Propio del proyecto | En uso |
| Fuentes DejaVu Sans y DejaVu Sans Bold | `Assets/Game/Art/UI/Fonts/` | DejaVu Fonts, sin modificar, distribuidas con el kit de UI | Licencia libre de DejaVu (derivada de Bitstream Vera), con aviso completo en `FONT_LICENSE.txt`, que se conserva junto a los TTF | En uso |
| Catálogo de textos EN/es_UY de la interfaz | `Assets/Game/Resources/Localization/ui_strings.json` | Kit de UI (`data/localization_en_es.json`, sin cambios) | Propio del proyecto | En uso |
| Textos propios del juego EN/es_UY | `Assets/Game/Resources/Localization/game_strings.json` | Escritos en este repositorio | Propio del proyecto | En uso |
| Catálogo de diálogos del tío | `Assets/Game/Resources/Localization/dialogue_en_es.csv` | Paquete original (`preproduction/uncle_extraction/data/dialogue_en_es.csv`, sin cambios) | Propio del proyecto | En uso como subtítulos. **Sin voz**: `audio_status=NOT_RECORDED`. Las voces finales necesitan actor o una voz licenciada con derechos de uso comercial (biblia 03, sección 08). |
| Efectos de sonido (agarrar, soltar, impacto, golpe, señales de los cuatro ataques, puertas, victoria, derrota) | Generados al ejecutar (`SfxSynth.cs`) | Sintetizados por el código del juego: no hay archivos de audio ni muestras de terceros | Propio del proyecto | Provisionales; pendiente de producción |
| Música | — | — | — | No hay |
