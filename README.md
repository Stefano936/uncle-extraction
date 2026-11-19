# UNCLE EXTRACTION

Juego cooperativo de comedia física en primera persona, de 1 a 4 jugadores: sacar de un bar a un tío controlado por el juego, que se resiste, se aferra, escapa, golpea y empuja, y meterlo en una camioneta. Unity 6.3 LTS, C#, Windows; Steam más adelante.

**Candidata de Windows para playtest** `42dcfcd0d7b8861b89cdc374d9a1eee81a449bc5`, versión **0.17.0**, protocolo **2**. La build de jugadores está en `UncleExtraction_Demo_42dcfcd_Windows_x64.zip` (local, con su `.sha256`). El ZIP de la sesión 17 permanece disponible como referencia. Consultá [PROGRESS.md](PROGRESS.md) para el resultado y las comprobaciones pendientes. [PR #3](https://github.com/Stefano936/uncle-extraction/pull/3)

El proyecto Unity (`UnityProject/`) genera builds de Windows que incluyen:
- Escenario **The Last Round** (`Bar_LastRound`): bar nocturno con techo, luces cálidas, neones, vereda y calle; tío articulado de 11 cuerpos con ropa y gorro de fiesta; carrito y camioneta con modelos propios sobre la geometría funcional.
- Solo o cooperativo de 2 a 4: anfitrión autoritativo (Netcode for GameObjects), sala con LISTO, **internet por Relay con código de sala** y red local por IP.
- Extracción real con cierre de puertas, carrito, cuatro ataques anunciados, vaso señuelo, ayuda para levantarse y primera partida guiada.
- Cooperación: marcador contextual (clic central: tío, carrito, camioneta o un punto, con el nombre y color de quien marca), nombres de los compañeros y aviso de «caído»; el tío solo ataca con las extremidades que nadie le sujeta; una desconexión no cambia la ronda de golpe (10 s de gracia).
- Rejugabilidad: desafíos *Sin carrito*, *Ruta estrecha* y *Arranca en el karaoke*, campana de última ronda, torre de vasos que distrae al tío y parte de extracción con lo que pasó de verdad (vista para captura).
- Controles: WASD o flechas, clic izquierdo y derecho para cada mano (mantener o alternar), E, Q, Shift, Ctrl, Espacio, clic central, Esc; reasignables. Interfaz EN/es_UY, texto hasta 150 %, volúmenes general / efectos / avisos, calidad gráfica Alta/Baja.

Pendiente: voz y música, revisión humana, prueba entre dos equipos y Steam. Matriz de requisitos: [docs/MATRIZ_REQUISITOS.md](docs/MATRIZ_REQUISITOS.md).

## Probar

Descomprimí el ZIP en una carpeta nueva. La [guía de juego](docs/COMO_PROBAR.md) explica controles y objetivo. Usá el [guion de la candidata](docs/VOLVER_A_PROBAR_CANDIDATA.md) para revisar ambos HUD y hacer una extracción completa.

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
| `docs/MATRIZ_REQUISITOS.md` | Requisitos, estado, evidencia por nivel y próxima acción. |
| `PROMPT_MAESTRO_CLAUDE_UNCLE_EXTRACTION.md`, `00_EMPEZAR_AQUI.md`, `ENTREGA_SHA256.json` | Antecedentes de la entrega inicial (históricos). |

Las builds (`Builds/`), `Library/` y los registros (`.local-setup/`) no se versionan.
