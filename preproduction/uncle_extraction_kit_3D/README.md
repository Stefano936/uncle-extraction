# UNCLE EXTRACTION - kit 3D de prototipo

Complemento del paquete de diseño v1.0. Fecha: 15 de noviembre de 2026.

Este paquete contiene geometría 3D real y editable para arrancar las pruebas. No es una segunda versión del diseño ni un juego terminado. Usar junto con la documentación original. Los assets son intencionalmente simples y no reemplazan el arte final aprobado.

## Qué incluye

- Ocho assets, cada uno en FBX y GLB: tío, taburete, mesa, barra, carrito, puerta, camioneta y estructura del bar.
- Personaje de 3.992 triángulos, rig Generic de 12 huesos (11 deformantes y un root), pesos por segmentos y ocho marcadores de agarre.
- Tres acciones de prueba: Idle_Prototype, Push_Prototype y Kick_Prototype.
- source/Uncle_Extraction_Blockout.blend: escena editable con los assets colocados.
- Tres renders de la geometría entregada; son vistas de revisión en Blender, no capturas de gameplay.
- Scripts de generación, render de poses y verificación de exportaciones.
- Datos de medidas, colocación, sockets y reporte de validación.
- GUIA_DEL_KIT_3D.pdf y claude/INTEGRAR_KIT.md.

## Empezar con Claude

Entregar este ZIP extraído junto con el paquete de documentación. Pedir que lea claude/INTEGRAR_KIT.md. El siguiente trabajo es importar el FBX del tío y el carrito en PhysicsLab, configurar colisiones y física según el GDD y probar un agarre. No tiene que modelar todo desde cero antes de ese experimento.

Unity usa una cadena de importación basada en FBX. Por eso se entrega ese formato como ruta principal; GLB sirve para intercambio y revisión con una herramienta compatible. El GLB no se presupone importable en un proyecto Unity vacío sin herramientas adicionales. Fuente oficial: https://docs.unity.com/en-us/engine/6000.3/manual/assets-and-media/asset-types/models/creating-dccassets/3d-formats

## Qué se comprobó

Se exportaron y reimportaron realmente los 8 FBX y 8 GLB en Blender 4.3.2. Se verificaron presencia de mallas, medidas, materiales, huesos, vértices con pesos, sockets y acciones. En el personaje se comprobó además un cambio real de pose del antebrazo al reproducir el empujón. El reporte completo está en data/validation_report.json.

No se ejecutó Unity. No se probaron ragdoll, colisiones, agarres, ataques, red ni Steam. Estas pruebas corresponden a los hitos del proyecto. Que un archivo se reimporte bien en Blender no garantiza importación o comportamiento correcto en Unity.

## Límites intencionales

- Pesos rígidos por segmento: útiles para una primera prueba, con transiciones visibles; no skinning final.
- Las animaciones son aproximadas. Sus ventanas de contacto deben ajustarse al AttackResolver del juego, no inferirse como ya validadas.
- Sin Rigidbody, joints, colliders, scripts de juego ni prefabs Unity incorporados.
- Materiales de colores planos; no texturas finales, UV de producción ni expresiones faciales animadas.
- La pared delantera del escenario está rebajada para inspeccionar el conjunto. Completar colisión y paredes en la escena de juego.
- La camioneta tiene puertas traseras abiertas. Su bounding box total incluye esas puertas y es mayor que el chasis.
- Source: Z arriba, frente del personaje -Y. FBX: exportación -Z Forward / Y Up. Comprobar orientación y escala en Unity con una referencia de un metro.

## Regenerar y comprobar

Desde una terminal con Blender 4.3.2 disponible:

```bash
blender --background --python-exit-code 1 --python source/generate_kit.py
blender --background --python-exit-code 1 --python source/validate_exports.py
blender --background --python-exit-code 1 --python source/render_pose_preview.py
```

Los scripts escriben dentro de esta carpeta y reemplazan sus salidas del kit. Ejecutarlos con Blender en background, sin abrir un proyecto propio con trabajo sin guardar. Para regenerar solo modelos y escena, agregar `-- --skip-renders` al primer comando.

La versión exacta utilizada y las opciones de exportación se registran en los scripts y metadatos. El script de validación distingue la geometría entregada de las mallas auxiliares que Blender puede crear para mostrar huesos.

## Relación con el diseño original

Conservar el GDD, balance y arquitectura como autoridad para mecánicas. Actualizar el manifiesto original con estado «prototipo entregado / producción pendiente» para estos assets. No marcar animaciones definitivas, controles o físicas como terminados por recibir este kit. No cambia el alcance de 1 a 4, dificultad por grupo, bot resistente, cuatro ataques ni etapas de red.

## Origen

Geometría, materiales de color y scripts producidos para este prototipo mediante construcción procedural. No se incorporaron modelos, texturas o audio descargados de terceros. Las imágenes de previews son renders de esos modelos. El arte conceptual del paquete anterior sigue siendo la referencia de dirección final.
