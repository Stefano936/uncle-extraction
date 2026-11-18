# Tráiler y clips — juego real

## Lo que ya existe (sesión 15)

Clips animados (WebP, 15 fps, juego real con autopiloto, sin texto de depuración; `-ueCleanCapture`), en `docs/lanzamiento/clips/`:

| Clip | Formato | Qué se ve | Cómo se grabó |
| --- | --- | --- | --- |
| `anclaje_vertical.webp` / `anclaje_horizontal.webp` | 9:16 y 16:9 | Primer agarre, el tío se aferra a la barra, «Mantené E: soltarle la mano», forcejeo | `-ueAutopilot drag -ueAutopilotDelay 1.5 -ueFrames <dir>@1@7@15` |
| `carrito_vertical.webp` | 9:16 | El tío atado al carrito, pasar la puerta, sacudidas | `-ueAutopilot cart -ueFrames <dir>@7@10@15` |
| `cierre_vertical.webp` | 9:16 | Rampa, «Cerrando…», portazo y pestillo, parte de extracción con «Cerraron con 9 s de margen» | `-ueAutopilot drag -ueRoundTime 30 -ueFrames <dir>@17@8@15` |

Limitación: sin ffmpeg en esta PC (descargarlo requiere tu permiso), así que no hay MP4 ni audio en los clips. Para video final: grabar con OBS siguiendo la lista de abajo.

## Tráiler (60–75 s) — lista de tomas

Primera toma = el problema. Gameplay real, sin HUD donde no aporte (`-ueNoHud -ueCleanCapture`).

| # | Duración | Toma | Cómo obtenerla |
| --- | --- | --- | --- |
| 1 | 0–4 s | Cartel «THE LAST ROUND» y el tío de espaldas en la barra; texto: «Es la hora del cierre.» | Solo, mirar la barra al empezar (`-ueNoHud`). |
| 2 | 4–9 s | Primer plano: lo agarran y se aferra a la barra; cara enojada | Clip `anclaje`; o `-ueFaceUncle 2 -ueForceAttack slap@5` |
| 3 | 9–14 s | Manotazo anunciado (arco en el piso) y el jugador cae | `-ueFaceUncle 2 -ueForceAttack kick@4` con HUD |
| 4 | 14–24 s | Cuatro amigos: uno sostiene, otro suelta la mano, otro trae el carrito | 4 procesos: `-ueHost … -ueExpectPlayers 4` + 3 `-ueClient`, autopiloto `drag`; grabar el invitado 1 |
| 5 | 24–32 s | Carrito trabado en la puerta, empujarlo y pasar | Clip `carrito` (o modo `cart` y esperar el marco) |
| 6 | 32–38 s | Torre de vasos al piso y el tío distraído | Empujar el carrito rápido contra la torre junto a la salida |
| 7 | 38–48 s | Rampa, acomodar un brazo, «Cerrando…», portazo y pestillo | Clip `cierre` |
| 8 | 48–55 s | Parte de extracción en la vista para capturas | Botón «Vista para captura» en resultados |
| 9 | 55–60 s | «Otra ronda» + logo + «1–4 jugadores · cooperativo en línea» | Pantalla de resultados |

Música: no hay pista propia todavía (ver `docs/GUION_VOZ.md` y el pendiente de música con licencia). Para el tráiler: ambiente del bar + efectos del juego.

## Clips verticales (redes) — 3 situaciones

1. **«No se quiere ir»** — agarre desesperado a la barra (clip `anclaje_vertical`). Texto arriba: «Cuando tu tío dice “una más”».
2. **«Rescate del carrito»** — el carrito en la puerta (clip `carrito_vertical`). Texto: «El carrito pasa. El tío no tanto.»
3. **«Último segundo»** — cierre con 9 s de margen (clip `cierre_vertical`). Texto: «Cerramos con 9 segundos de margen. Fue trabajo en equipo.»
