# Entrada del repositorio

Esta entrega agrega organización para GitHub al material ya preparado. Incluye el paquete unificado, el complemento de materiales y el prompt maestro, todos extraídos y listos para consultar. No hace falta volver a descargar los ZIP anteriores.

## Para Stefano

Copiar **el contenido** de esta carpeta dentro de la copia local de `Stefano936/uncle-extraction`. El README de esta entrega amplía el README inicial del repositorio. Si hay avances nuevos, revisar los cambios antes de reemplazar archivos. Ver [docs/GUIA_GITHUB.md](docs/GUIA_GITHUB.md).

Abrir esa copia local en VS Code y comenzar una sesión local de Claude Code. Mensaje de arranque:

> Trabajá sobre este repositorio. Leé CLAUDE.md y PROGRESS.md, inspeccioná el estado real y continuá con M0 y M1. Consultá el prompt maestro y las fuentes del hito según el mapa de lectura. Implementá archivos reales y ejecutá las comprobaciones disponibles. Actualizá PROGRESS.md y distinguí lo implementado de lo probado. Si falta acceso a Unity, indicá el paso concreto necesario y avanzá con las tareas independientes.

## Organización y lectura

`CLAUDE.md` define la entrada breve y la lectura por hito para evitar cargar todos los documentos en cada sesión. El prompt maestro original se conserva completo; sus rutas de paquetes están ahora bajo `preproduction/`. Esta adaptación de rutas y lectura inicial se aplica sobre sus instrucciones de extracción y lectura exhaustiva inicial. El alcance, la física, la red y los criterios de verificación del prompt siguen vigentes.

| Ruta indicada en el material original | Ubicación en este repositorio |
| --- | --- |
| `00_EMPEZAR_AQUI.md` del paquete unificado | `preproduction/00_EMPEZAR_AQUI.md` |
| `uncle_extraction/` | `preproduction/uncle_extraction/` |
| `uncle_extraction_kit_3D/` | `preproduction/uncle_extraction_kit_3D/` |
| `uncle_extraction_kit_UI/` | `preproduction/uncle_extraction_kit_UI/` |
| `uncle_extraction_kit_materiales/` | `preproduction/uncle_extraction_kit_materiales/` |
| Proyecto Unity nuevo | `UnityProject/`, a crear en M0 |

Los kits pueden contener rutas de la máquina donde se generaron. Sus scripts son fuentes originales: revisar dependencias y adaptar rutas antes de ejecutarlos; no hace falta regenerar los recursos para empezar.

## Qué se conserva

Primera persona, dos manos, individual viable y cooperativo hasta cuatro; NPC resistente con ataques anticipables; dificultad separada de cantidad de jugadores; masa constante; física autoritativa del anfitrión; un bar, vereda y camioneta estacionada. Variantes adicionales se posponen.

El detalle está en `preproduction/uncle_extraction/DECISIONES.md`. Los números son hipótesis iniciales ajustables con evidencia. Las capturas son diseños; los FBX no incluyen un ragdoll funcional ni lógica de juego.

## Estado y controles

- No hay juego implementado en esta entrega.
- Las comprobaciones de Blender y recursos constan en los informes originales; no se repitieron en Unity al organizar este repositorio.
- Las matrices del juego, UI y materiales no son pruebas aprobadas.
- En esta entrega se verificaron integridad de los ZIP fuente, preservación de sus archivos y hashes del paquete conjunto.

Conservar las fuentes originales; copiar al proyecto Unity solo lo necesario para el hito. Evitar incluir de nuevo los ZIP fuente dentro del repositorio: su contenido ya está extraído.
