# UNCLE EXTRACTION

Juego cooperativo de comedia física en primera persona, de 1 a 4 jugadores: sacar de un bar a un tío controlado por el juego, que se resiste, se aferra, escapa, golpea y empuja, y meterlo en una camioneta. Unity 6.3 LTS, C#, Windows; Steam más adelante.

**Estado: prototipo jugable.** Hay proyecto Unity (`UnityProject/`) y builds de Windows que se generan localmente:
- Escenario principal **The Last Round** (`Bar_LastRound`) con modelos de prototipo del kit 3D.
- Laboratorio de diagnóstico (`PhysicsLab`).
- Menú, partida individual y cooperativo directo en LAN o la misma PC: anfitrión autoritativo con Netcode for GameObjects.
- Extracción real dentro de la camioneta con cierre de puertas, carrito, cuatro ataques anunciados, vaso señuelo y ayuda para levantarse.

Pendiente: arte, sonido y UI finales, ragdoll, internet (Relay) y Steam. El estado exacto y la evidencia están en [PROGRESS.md](PROGRESS.md).

## Probar

[docs/COMO_PROBAR.md](docs/COMO_PROBAR.md): abrir la build, controles y qué anotar.

## Desarrollar

1. Leer [CLAUDE.md](CLAUDE.md), que tiene las instrucciones vigentes, el entorno y los comandos verificados, y [PROGRESS.md](PROGRESS.md).
2. Regenerar las escenas, el prefab y las Build Settings: `PhysicsLabBuilder.BuildFromCommandLine`. Build: `BuildTools.BuildWindows` (`-ueDev` para la de desarrollo).
3. Pruebas: EditMode y PlayMode con el Test Framework de Unity, más la red en dos procesos con `tools/windows/net_smoke.ps1`.

## Contenido

| Ruta | Uso |
| --- | --- |
| `UnityProject/` | Proyecto Unity: `Assets/Game` (código, escenas, prefabs, arte importado), `Packages/`, `ProjectSettings/`. |
| `CLAUDE.md` / `PROGRESS.md` | Instrucciones y estado real (implementado, verificado, pendiente, bloqueado). |
| `DEPENDENCIES.md` | Versiones fijadas del editor y los paquetes. |
| `docs/` | Decisiones de implementación, catálogo de recursos integrados y guía de prueba. |
| `tools/windows/` | Scripts de preparación, pruebas de red y subida. |
| `preproduction/` | Documentación y kits originales (GDD, especificación, datos, modelos, UI y materiales). Se conservan sin modificar. |
| `PROMPT_MAESTRO_CLAUDE_UNCLE_EXTRACTION.md`, `00_EMPEZAR_AQUI.md`, `ENTREGA_SHA256.json` | Antecedentes de la entrega inicial (históricos). |

Las builds (`Builds/`), `Library/` y los registros (`.local-setup/`) no se versionan.
