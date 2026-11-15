# UNCLE EXTRACTION - materiales, texturas e interacción

Complemento v0.1 · 15 de noviembre de 2026. Usar junto con la entrega unificada anterior. Este kit agrega arte de superficie y fichas de objetos, manteniendo las mecánicas del GDD.

## Qué se entrega

- Cuatro texturas originales de color: madera, baldosas, pared y tela bordó. Archivos RGB de 1254 x 1254 px, sin editar después de su generación.
- Una copia de la escena Blender con materiales aplicados y coordenadas UV preparadas para el prototipo.
- Ocho variantes FBX con esa preparación y una escena GLB de revisión visual.
- Renders del bar, el personaje y la repetición de materiales.
- Recetas de importación a Unity, once fichas de objetos, reglas visuales de interacción y veinte casos de QA pendientes.
- Guía PDF, documentación editable, prompts de texturas y prompt para Claude.

## Empezar

Extraer uncle_extraction_kit_materiales junto a las carpetas de la entrega unificada. La escena del kit 3D queda en una carpeta hermana, como esperan los scripts. Leer claude/INTEGRAR_MATERIALES.md. Si se usa otro destino, pasar --scene RUTA a apply_materials.py.

No reemplazar a ciegas el proyecto o los prefabs de Claude. Importar variantes a una carpeta de arte, comparar escala/UV y cambiar las referencias visuales conservando lógica y física ya comprobadas. Los nuevos nombres evitan sobrescribir los archivos anteriores.

## Alcance real

Las texturas son Base Color, no un set PBR completo. No hay mapas normal, height, roughness, AO ni metallic generados. Rugosidad y smoothness se proponen como valores de material. El tamaño real es 1254 px; el pedido de 2048 en los prompts no cambia lo que devolvió la generación. Registrar el tamaño que finalmente importe Unity.

Mirror se propone para madera, pared y tela; Repeat para el piso. Mirror es una decisión de muestreo que refleja el patrón y evita una discontinuidad brusca en el borde, no una prueba de periodicidad perfecta del archivo. La escena y el tablero de repetición muestran esa receta.

Los modelos continúan siendo placeholders de producción. Las UV son proyecciones simples apropiadas para experimentar, no un unwrap final pintado a mano. Las variantes que usan color plano siguen pudiendo ser parte del estilo: carrito, vehículo, piel, vinilo y piezas pequeñas no necesitan recibir una textura artificialmente.

No hay prefabs Unity, archivos .mat, colliders, Rigidbody ni componentes de juego dentro de este kit. Los marcadores de interacción requieren conexión a sistemas reales. Los tres objetos sin modelo en interaction_manifest.json se identifican expresamente como pendientes.

## Reproducir

```bash
blender --background --python-exit-code 1 --python source/apply_materials.py
python source/build_material_data.py
blender --background --python-exit-code 1 --python source/validate_material_exports.py
```

El primer comando requiere la escena del kit 3D y reemplaza las salidas de esta carpeta, manteniendo intacta la escena original. Acepta -- --skip-renders para regenerar solo archivos 3D. Los scripts Python de datos requieren Pillow y NumPy. El PDF se puede regenerar con source/build_guide.py y ReportLab.

## Verificación

Los reportes separan inspección visual, comprobaciones de archivo y reimportación en Blender de las pruebas todavía pendientes en Unity. No presentar las fichas, masas propuestas o recetas como comportamiento ya probado del juego.

Los prompts completos y el modo de generación están en docs/PROMPTS_TEXTURAS.md. Los PNG suministrados son recursos de arte; los renders sirven para revisar apariencia y no para anunciar gameplay en Steam.
