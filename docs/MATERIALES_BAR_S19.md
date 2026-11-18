# Materiales del bar · sesión 19

La comparación toma como referencia `863888be7d82edf3307586197624858b28976178`. Las seis capturas usan la build QA, la escena **Bar_LastRound**, 1600 × 900, calidad Alta, las luces de la escena y los mismos valores de posición, yaw y pitch en cada par. El guion fija la cámara a los 2 s y captura a los 3,5 s. Las imágenes son PNG directos del juego.

| Vista | Antes | Después |
| --- | --- | --- |
| General | [PNG](capturas/sesion19/antes_general.png) | [PNG](capturas/sesion19/despues_general.png) |
| Barra a distancia de agarre | [PNG](capturas/sesion19/antes_barra.png) | [PNG](capturas/sesion19/despues_barra.png) |
| Acercamiento a la salida | [PNG](capturas/sesion19/antes_salida.png) | [PNG](capturas/sesion19/despues_salida.png) |

## Texturas originales

Los archivos de entrada de Unity están en `UnityProject/Assets/Game/Art/Materials/Textures/S19/`. [El generador](../tools/art/generate_s19_surfaces.py) dibuja las vetas, nudos, juntas, manchas y zonas de contacto con máscaras y ruido procedural de semilla fija. Se ejecuta con Python 3, NumPy y Pillow. Los mapas de normales salen de campos de altura propios. El color ocupa RGB y la suavidad ocupa el alfa del mismo PNG en los materiales URP/Lit opacos.

| Superficie | Tratamiento |
| --- | --- |
| Barra | Vetas a lo largo del tablero de 4,12 m, nudos y un desgaste irregular que pierde brillo hacia el canto. El frente suma paneles con ranuras discretas. |
| Baldosas | Baldosas de 50 cm con bordes biselados, juntas hundidas, variación por pieza y desgaste pintado sobre el recorrido entre la barra y la salida. El piso usa un atlas del salón. |
| Revoque | Textura mate repetible cada 2,67 m, con marcas de llana, poros pequeños y cambios de tono de bajo contraste. |
| Puerta y marco | Hoja pintada con un rebaje visual y roces alrededor de la manija, el lateral de cierre y la zona de patada. La madera repetible de las molduras corre en el sentido de cada pieza. |

La escena versionada usa los materiales `S19_*` y mallas visuales con UV proyectadas. El generador de escenas aplica el mismo acabado desde `BarDressing.ApplyS19Finishes`, después de asignar las UV métricas del kit. **Actualizar materiales del bar** en el editor vuelve a aplicar las asignaciones a la escena guardada. Para reproducir las capturas, ejecutar [s19_material_shots.ps1](../tools/windows/s19_material_shots.ps1) con el ejecutable QA de cada commit y dos carpetas de salida separadas.

## Pruebas

- **PASS · recorrido completo.** La build QA `4c56ec2` liberó al tío de la barra, atravesó la salida y aseguró la camioneta en 21,8 s en modo arrastre. Registro local `.local-setup/logs/unity/s19_route_drag.log`.
- **PASS · PlayMode dirigido.** 6/6, incluyendo distribución del escenario, prioridad de interacción y cambio de HUD desde pausa. Unity 6000.3.28f1. El guard de pruebas confirmó que los archivos versionados quedaron iguales.
- **PASS · comparación de la escena guardada.** Los 17 cuerpos rígidos, los 51 colliders de caja o cápsula, la bisagra, los componentes de juego y las 22 luces coincidieron con `863888b`. El único objeto eliminado es la tira visual que hacía de desgaste en el canto.
- **PASS · regeneración completa.** En otro checkout, `PhysicsLabBuilder.BuildFromCommandLine` volvió a conectar los 6 materiales y las 9 mallas. Los 54 archivos de textura, material, malla y sus metadatos coincidieron byte a byte. La referencia de compatibilidad de URP queda serializada en los materiales.
