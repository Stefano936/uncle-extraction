# Integrar materiales y fichas de interacción

Continuá el proyecto existente sin reiniciar. Este complemento usa la escena del kit 3D y conserva GDD, balance e interfaz anteriores. Leer README.md, docs/ESPECIFICACION_MATERIALES.md, data/texture_import.json y data/interaction_manifest.json.

1. Importá las cuatro texturas y creá materiales en la versión real de Unity/URP. Son Base Color RGB de 1254 px; no mapas normal ni un set PBR completo. Revisá sRGB, mipmaps, wrap, dimensiones importadas y smoothness según receta.
2. Compará las variantes FBX con los modelos ya integrados. Usan MaterialUV como UV0 con escala física codificada, de modo que el material inicia en Tiling 1,1. No volver a multiplicar la densidad accidentalmente.
3. Conservá prefabs, scripts, masa, colliders, referencias de sockets y red ya probados. Sustituí referencias visuales con cuidado; no reconstruyas toda la física para cambiar una camisa.
4. La escena Blender y el GLB son revisión de aspecto. El GLB no se presupone importable en Unity sin herramientas. Las recetas no son archivos .mat ni comportamientos ejecutados.
5. Implementá lenguaje de interacción con estado real: mano y contorno en agarres válidos, tensión en anclaje activo, cierre bloqueado explicado, éxito solo confirmado por autoridad.
6. La mesa sigue fija, la camioneta no se conduce y las masas nuevas de las fichas son hipótesis de prototipo. No se infiere fricción de roughness ni peso del color de un shader.
7. Añadí los sockets de taburete indicados como pendientes y comprobá alcance antes de habilitar la interacción. Vaso/karaoke/snacks no tienen modelo entregado por aparecer en el catálogo.
8. Si el núcleo físico todavía no está validado, usá este kit solo para preparar arte y una escena de comparación. El primer hito sigue siendo agarre y empujón con recuperación, seguido de cliente remoto.

Entrega: materiales creados, imports y dimensiones registrados, comparación visual, referencias preservadas, pruebas realmente ejecutadas y PROGRESS.md actualizado. No afirmar que se probó Unity si solo se abrieron los archivos en Blender.
