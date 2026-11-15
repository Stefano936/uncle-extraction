# UNCLE EXTRACTION - kit de interfaz y experiencia

Complemento de diseño v0.1 · 15 de noviembre de 2026. Usar junto con el paquete principal y el kit 3D. No modifica el alcance del juego ni trae una interfaz ya programada en Unity.

## Entrega

- 12 pantallas en PNG 1920x1080 y SVG editable con fondos enlazados.
- 20 iconos en SVG y PNG transparente; 11 sprites modulares sin texto, con receta de importación y bordes 9-slice.
- Dos fondos renderizados desde la geometría real del kit 3D.
- Fuentes DejaVu Sans y Bold sin modificar, con su aviso de licencia completo.
- Catálogos de traducción EN/es_UY, tokens de diseño, estados, bindings y pruebas.
- UI_SPEC.md, ATLAS_INTERFAZ.pdf, scripts para reproducir los recursos y un prompt de integración.

## Usar con Claude

Leer claude/INTEGRAR_UI.md. Los PNG/SVG de screens/ muestran intención visual; no se importan como una sola textura que pretenda ser la interfaz. Construir componentes con sprites, fuentes y textos localizados. Los valores de reloj, jugadores y resultados de las pantallas son ilustrativos.

El GDD, el balance y la arquitectura originales siguen siendo autoridad para mecánicas. Este kit detalla su presentación. El código de source/ genera recursos y documentación; no implementa movimiento, red ni UI interactiva del juego.

## Fuentes y SVG

Extraer la carpeta completa para que los SVG encuentren fondos, iconos y fuentes relativos. Algunos editores pueden pedir instalar las fuentes o reenlazar imágenes. Los PNG sirven como referencia universal. Para Unity, crear el asset de fuente que corresponda a la versión instalada y verificarlo; no se entrega un atlas SDF ficticio.

## Reproducir

python source/build_ui_assets.py
python source/build_ui_data.py

Requiere Python y Pillow. Para regenerar fondos, requiere Blender y la escena del kit 3D; source/render_backgrounds.py admite --scene RUTA. Los fondos ya están incluidos, por lo que no hace falta Blender para editar la interfaz o regenerar los PNG/SVG.

La exportación SVG conserva las piezas de interfaz como vectores y usa fondos raster enlazados. Las etiquetas de mockup y valores de muestra no se trasladan a la interfaz final.

## Estado de verificación

Se revisaron las pantallas e integridad de archivos, tamaños, transparencia y catálogos. La aplicación real en Unity, navegación con mando, red y layouts en ejecución siguen pendientes. Las pruebas de aceptación empiezan NOT_RUN. El reporte de validación distingue revisión de recursos de pruebas del juego.
