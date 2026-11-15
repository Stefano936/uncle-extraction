# UNCLE EXTRACTION

Juego cooperativo de comedia física para 1–4 jugadores: sacar del bar a un tío controlado por el juego que se resiste, golpea, empuja y se aferra al entorno. Objetivo futuro: Windows y Steam.

**Estado actual: preproducción.** Esta entrega contiene diseño, datos, modelos 3D, interfaz y texturas. Todavía no contiene un proyecto Unity ni una build jugable.

## Empezar

1. Leer [00_EMPEZAR_AQUI.md](00_EMPEZAR_AQUI.md).
2. Claude debe leer [CLAUDE.md](CLAUDE.md) y [PROGRESS.md](PROGRESS.md).
3. Para incorporar esta entrega al repositorio, seguir [docs/GUIA_GITHUB.md](docs/GUIA_GITHUB.md).

## Contenido

| Ruta | Uso |
| --- | --- |
| `CLAUDE.md` | Instrucciones breves y mapa de lectura por tarea. |
| `PROGRESS.md` | Estado real y siguiente objetivo. |
| `PROMPT_MAESTRO_CLAUDE_UNCLE_EXTRACTION.md` | Prompt detallado original, conservado como referencia. |
| `preproduction/uncle_extraction/` | GDD, especificación técnica, dirección artística, plan, datos, PDF y prompts por hito. |
| `preproduction/uncle_extraction_kit_3D/` | Ocho modelos en FBX/GLB, Blender, animaciones y guía. |
| `preproduction/uncle_extraction_kit_UI/` | Pantallas, componentes, iconos, fuentes, localización y reglas. |
| `preproduction/uncle_extraction_kit_materiales/` | Texturas, modelos con UV, Blender y fichas de interacción. |
| `DEPENDENCIES.md` | Versiones a fijar durante M0. |
| `docs/DECISIONES_IMPLEMENTACION.md` | Registro de decisiones nuevas. |
| `ENTREGA_SHA256.json` | Tamaños y hashes de los archivos entregados. |

`UnityProject/` será creado por Claude en M0. Cuando exista, se versionarán `Assets/` con sus `.meta`, `Packages/` y `ProjectSettings/`. El `.gitignore` excluye cachés, temporales y builds locales.

## Primera entrega esperada

`PhysicsLab`: movimiento, cámara, dos manos, agarre, liberación, empujón anunciado, caída, recuperación, carrito y extracción de prueba. Después, host y segundo cliente temprano.

La base propuesta es Unity, C#, URP y física autoritativa del anfitrión. La versión exacta del editor y los paquetes se comprobarán y fijarán en M0. Los informes de Blender y validación documental son antecedentes; importación, físicas y red en Unity siguen pendientes.

Los materiales fuente se conservan sin modificaciones dentro de `preproduction/`. Sus manifiestos describen sus paquetes originales; `ENTREGA_SHA256.json` describe esta entrega conjunta al momento de empaquetarla.
