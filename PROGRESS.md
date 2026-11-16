# Estado del proyecto

Última actualización: 2026-11-15 (sesión 5: M3 estabilizado con la camioneta real; red repetida con latencia y pérdida simuladas; flujo de menús verificado; rescate técnico).

Build de referencia vigente: `Builds/PhysicsLab/UncleExtraction_PhysicsLab.exe` (release, Mono) y `Builds/PhysicsLab_Dev/UncleExtraction_PhysicsLab.exe` (desarrollo, con simulador de red), generadas desde el commit de esta actualización (el siguiente a `b285556`). Las pruebas anteriores a la camioneta nueva (commits ≤ `c27d209`) **no** validan la versión actual.

## 1. Implementado y verificado (versión actual)

| Función | Verificación | Evidencia |
| --- | --- | --- |
| Extracción real dentro de la camioneta: volumen de carga interior, pose válida (torso/pelvis/hombros dentro + sin solapes con sólidos) sostenida 1,5 s, abertura libre, un jugador mantiene E 1 s desde afuera, cierre de hojas y recién entonces Secured | PlayMode PASS (editor, anfitrión loopback) | `PhysicsLabPlayTests`: cierre necesario; jugador en la abertura bloquea; a medio entrar no vale; la victoria llega con las hojas cerradas (no al terminar el E) |
| Cierre robusto | PlayMode PASS | dos jugadores manteniendo E no aceleran (una acumulación por frame); si alguien entra en la abertura durante el cierre, se cancela y las hojas se reabren |
| Reinicio limpio durante el cierre y durante la carga del carrito | PlayMode PASS | sin corutinas activas, sin uniones, tío dinámico, sin colisiones ignoradas residuales |
| Vaso señuelo: atrae solo si un jugador lo colocó, está apoyado y a la vista; 2,5 s (mitad desde el 3.er uso); 12 s de enfriamiento; suelta el anclaje | PlayMode PASS | `Decoy_ReleasesAnchor_DistractsAndHasCooldown` |
| Ayuda de compañero para levantarse (mirar y mantener E: se levanta al doble de velocidad) | PlayMode PASS | `Teammate_HelpsFallenPlayer_GetUpFaster` (2.º jugador local sin red) |
| Intentos individuales completos con la camioneta nueva (autopiloto con entradas de jugador; sin teletransportes ni `Secure()` directo) | PlayMode PASS 3/3 | arrastre Normal 26,3 s, arrastre Difícil 17,7 s, carrito Normal 23,5 s (`85_solo.log`). Antes de la corrección: 1/3 |
| Pruebas de laboratorio + flujo + intentos | PlayMode PASS 14/14 | `99_playmode.xml`: 10 de laboratorio (incluido el rescate técnico), 3 intentos completos (carrito 24,1 s, arrastre Difícil 17,7 s, arrastre Normal 24,7 s) y el flujo de menús. EditMode 9/9 (`89_editmode.xml`) |
| Flujo de menús | PlayMode PASS | `GameFlowTests`: Frontend → solo en Difícil (la dificultad llega a la ronda) → derrota por tiempo → los resultados bloquean el control → reintento → menú con la red cerrada |
| Rescate técnico (especificación 08) | PlayMode PASS | `Watchdog_RescuesUncleFallenBelowMap_AndReleasesGrabs`: con posición no finita, bajo el mapa (y < -1,5) o velocidad > 25 m/s sostenida, suelta los vínculos y vuelve a la última pose segura |
| Cooperativo en red con extracción real | localhost, 2 procesos: PASS | anfitrión y cliente terminan con el mismo estado: `Success t=23,1 puertas=Closed tío=Secured` (`net_smoke.ps1 -Scenario coop`). RTT 5–6 ms |

## 2. Implementado con fallos u observaciones abiertas

- **Autopiloto (no es juego):** en una pasada el tío quedó caído en el umbral, con los tobillos del lado del bar y la hoja de la puerta tapando la visión. El bot alternaba entre los dos tobillos inalcanzables. Se corrigió con un descarte de 12 s y un rodeo, verificado después en 3/3. Si reaparece, conviene un rodeo más inteligente antes de tocar el juego.
- **Tío caído en el umbral:** no puede levantarse si su cápsula se solapa con el marco o la hoja. Es comportamiento de juego válido (hay que moverlo), pero sin el rescate técnico de la especificación 08 (watchdog). Pendiente: rescate solo ante condiciones técnicas (no finitos, bajo el mapa, sin progreso por atasco técnico).
- **Balance:** con la camioneta nueva, el carrito en solitario sigue siendo la vía más estable (~23–73 s). Medirlo a mano.
- **Crash del driver NVIDIA** (`nvwgf2umx`, creación de D3D11) en 1 de 16 arranques simultáneos (sesión 4). El script ahora escalona 4 s los arranques, conserva el registro y reintenta una vez antes de marcar BLOCKED.

## 3. Pendiente de implementar (en orden)

1. M3 restante: fuga mejorada (hoy, si está libre, vuelve caminando a su banco). Derrota por tiempo, reintento, escalado por jugadores conectados y rescate técnico: implementados y verificados.
2. Flujo completo: implementado y con prueba automática (sección 1). Falta: navegación con mouse en la build (prueba manual) y reemplazar la IMGUI provisional por el kit de UI. Detalle anterior:  → escena `Frontend` (menú: solo con dificultad, con amigos (anfitrión o IP), opciones con sensibilidad, invertir Y, FOV y volumen, salir), pausa con Esc (detiene el tiempo solo en solitario), resultados con reintentar o menú, vuelta al menú si se va el anfitrión. Falta: prueba de navegación en la build y prueba automática del flujo.
3. Escenario `Bar_LastRound` con el kit 3D. Importación verificada (`Kit3DImporter.ImportAndReport`, `94_kit.log`): metros e Y arriba, sin conversión doble. Tío: 1,93 m, pose T, frente hacia −Z, 12 huesos, clips Idle 1,97 s / Kick 2,97 s / Push 2,77 s, 8 marcadores `Grab_*`. Barra: 4,12 × 0,9 m con `Npc_Anchor_L/R`. Camioneta: 2,52 × 5,38 × 2,33 m con `Extraction_Volume` y bisagras traseras. Carrito: 1,2 × 0,86 m con `Player_Handle`. Cascarón del bar: 18,2 × 17,1 m con `Npc_Start`, `Player_Start` y `Exit_Clearance_Center`. Falta: `LevelLayout` (rutas y apariciones por escena) y la escena. PhysicsLab queda para diagnóstico.
4. Ragdoll reducido del tío (11 cuerpos), con estabilidad y replicación antes de reemplazar el cuerpo central.
5. URP, interfaz con el kit de UI, animaciones, audio, localización EN/es_UY y controles remapeables (InputActionAsset).
6. Sessions/Relay (M4), QA, rendimiento y build reproducible.

## 4. Bloqueado por cuentas, equipos o participantes

| Bloqueo | Qué tiene que hacer Stefano |
| --- | --- |
| Internet (Sessions/Relay, M4) | Crear o elegir una organización y un proyecto en Unity Cloud (cloud.unity.com) con la cuenta de Unity, habilitar Multiplayer Services (Relay) y pasarme el Project ID, o vincularlo desde Edit → Project Settings → Services en el editor. |
| Prueba LAN | Una segunda PC en la misma red: correr `UncleExtraction_PhysicsLab.exe -ueHost` en una (aceptar el aviso del firewall de Windows) y `-ueClient <IP de la primera>` en la otra. |
| Steam | Cuenta de Steamworks, pago de Steam Direct y App ID. |
| IL2CPP | Instalar Visual Studio 2022 con «Desarrollo de juegos con C++». |
| Prueba manual y playtest | Jugar la build; no bloquea el desarrollo. |

## Red: resultados con la versión actual (2 procesos, **localhost**)

| Escenario | Resultado | Detalle |
| --- | --- | --- |
| Cooperativo con extracción real | PASS (2 corridas) | anfitrión y cliente terminan igual: `Success`, puertas `Closed`, tío `Secured` (17,6/17,5 s y 23,3/23,3 s). RTT 3–7 ms |
| Reintento tras ganar (el anfitrión reinicia y los dos vuelven a jugar) | PASS 2/2 | 2 victorias y `reinicios=1` en ambos procesos |
| Red degradada (simulador oficial en el cliente: 120 ms ±20 ms, 3 % de pérdida; build de desarrollo) | PASS (2 corridas) | éxito y estado final igual en ambos (26,8/24,9 s y 37,4/37,3 s); RTT medido por el cliente ~236–265 ms |
| Carrera por el mismo socket | PASS | nunca hay 2 agarres vivos sobre `belt`; uno gana y el otro recibe «Ocupado» |
| Salida del anfitrión a mitad de ronda | PASS | el cliente ve la desconexión, no simula ni gana solo y vuelve al menú |
| Desconexión del cliente mientras agarra | PASS | el anfitrión queda con 0 agarres |
| LAN / internet | NOT_RUN | ver bloqueos |

Fallos encontrados y corregidos en esta tanda, antes de los PASS de arriba:
- Al volver al menú, el cliente que había entrado por argumentos de prueba volvía a la partida y jugaba solo. Ahora el reenvío ocurre una vez por proceso.
- En el reintento, el cliente arrancaba antes de recibir las posiciones reiniciadas.
- El tío llegó a salir del mapa por el costado de la camioneta. Ahora hay piso extendido y límites invisibles en la vereda.
- El bot insistía en cerrar con las piernas del tío afuera. Ahora vuelve a acomodarlo.

Pendiente de red: desconexión exactamente durante el cierre de puertas o durante la carga del carrito. El reinicio en esos momentos sí está cubierto en PlayMode. Comando: `powershell -ExecutionPolicy Bypass -File tools\windows\net_smoke.ps1 -Scenario all` → `.local-setup/logs/net/net_smoke_results.csv`.

## Arquitectura y decisiones

- Red: `docs/DECISIONES_IMPLEMENTACION.md` (M2) y `Scripts/Net/`. Anfitrión autoritativo con NGO 2.14.0 + UTP 2.7.5; individual = anfitrión en loopback; gestión de escenas de NGO activada; clientes desconectados congelados; sin migración de anfitrión.
- Extracción: `ExtractionZone` (pose) + `VanDoors` (fases Open → Holding → Closing → Closed; aborta y reabre si fallan las condiciones durante el cierre).
- Carga del carrito validada: `TransportCart` (volumen libre, subida de 0,45 s, colisiones tío-carrito ignoradas solo mientras sube y está asegurado).

## Comandos verificados

Ver `CLAUDE.md` («Entorno y comandos verificados»). Novedades: `-ueDev` en `BuildTools.BuildWindows` (build de desarrollo), `-ueNetSim ms jitter %`, `-ueRepeat N` y `-ueAutopilot race -ueRaceAt <ms Unix>`.

## Ubicación y versionado

- Carpeta principal `C:\Users\stuff\OneDrive\Escritorio\uncle-extraction`; `origin` = `Stefano936/uncle-extraction` (privado), rama `main`. Builds locales no versionadas en `Builds/`.
- OneDrive sincroniza `UnityProject/Library` (ignorado por Git): conviene excluirlo de OneDrive.

## Próxima acción exacta

1. Crear `LevelLayout` (componente de escena con las rutas del autopiloto, las apariciones de jugadores y el punto de cierre) y hacer que `LabAutopilot` y `PlayerMotor` lo lean, manteniendo PhysicsLab igual.
2. `BarLastRoundBuilder`: bar de 18 × 12 m con barra, mesas, banquetas y rincón de karaoke; puerta de 1,5 m; vereda de 14 × 5 m; camioneta. Colisiones con primitivas y visuales de los FBX del kit. Agregarla al menú como escenario principal y PhysicsLab como «Laboratorio».
3. Repetir los intentos completos y `net_smoke.ps1` en la escena nueva.
4. Después: ragdoll reducido, URP y presentación.

## Al cerrar una sesión

Anotar comportamiento implementado, archivos, commit, comandos y pruebas ejecutadas, resultados, bloqueos y siguiente acción.
