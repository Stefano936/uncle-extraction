# Estado del proyecto

Última actualización: 2026-11-15 (sesión 5: estabilización de M3 con la camioneta real; red repetida; flujo de menús).

Build de referencia vigente: `Builds/PhysicsLab/UncleExtraction_PhysicsLab.exe` (release, Mono) y `Builds/PhysicsLab_Dev/UncleExtraction_PhysicsLab.exe` (desarrollo, con simulador de red), generadas desde el árbol de trabajo del commit que sigue a `f1728e0` (ver el log de Git). Las pruebas anteriores a la camioneta nueva (commits ≤ `c27d209`) **no** validan la versión actual.

## 1. Implementado y verificado (versión actual)

| Función | Verificación | Evidencia |
| --- | --- | --- |
| Extracción real dentro de la camioneta: volumen de carga interior, pose válida (torso/pelvis/hombros dentro + sin solapes con sólidos) sostenida 1,5 s, abertura libre, un jugador mantiene E 1 s desde afuera, cierre de hojas y recién entonces Secured | PlayMode PASS (editor, anfitrión loopback) | `PhysicsLabPlayTests`: cierre necesario; jugador en la abertura bloquea; a medio entrar no vale; la victoria llega con las hojas cerradas (no al terminar el E) |
| Cierre robusto | PlayMode PASS | dos jugadores manteniendo E no aceleran (una acumulación por frame); si alguien entra en la abertura durante el cierre, se cancela y las hojas se reabren |
| Reinicio limpio durante el cierre y durante la carga del carrito | PlayMode PASS | sin corutinas activas, sin uniones, tío dinámico, sin colisiones ignoradas residuales |
| Vaso señuelo: atrae solo si un jugador lo colocó, está apoyado y a la vista; 2,5 s (mitad desde el 3.er uso); 12 s de enfriamiento; suelta el anclaje | PlayMode PASS | `Decoy_ReleasesAnchor_DistractsAndHasCooldown` |
| Ayuda de compañero para levantarse (mirar y mantener E: se levanta al doble de velocidad) | PlayMode PASS | `Teammate_HelpsFallenPlayer_GetUpFaster` (2.º jugador local sin red) |
| Intentos individuales completos con la camioneta nueva (autopiloto con entradas de jugador; sin teletransportes ni `Secure()` directo) | PlayMode PASS 3/3 | arrastre Normal 26,3 s, arrastre Difícil 17,7 s, carrito Normal 23,5 s (`85_solo.log`). Antes de la corrección: 1/3 |
| Pruebas de laboratorio | PlayMode PASS 9/9 en 2 corridas | `84_lab_{1,2}.xml` |
| Cooperativo en red con extracción real | localhost, 2 procesos: PASS | anfitrión y cliente terminan con el mismo estado: `Success t=23,1 puertas=Closed tío=Secured` (`net_smoke.ps1 -Scenario coop`). RTT 5–6 ms |
| Base de red (M2) | localhost, 2 procesos | carrera por el mismo socket, desconexión del cliente agarrando, salida del anfitrión: PASS en `c27d209`; se repiten con la versión actual (ver tabla de red) |

## 2. Implementado con fallos u observaciones abiertas

- **Autopiloto (no es juego):** en una pasada el tío quedó caído en el umbral, con los tobillos del lado del bar y la hoja de la puerta tapando la visión. El bot alternaba entre los dos tobillos inalcanzables. Se corrigió con un descarte de 12 s y un rodeo, verificado después en 3/3. Si reaparece, conviene un rodeo más inteligente antes de tocar el juego.
- **Tío caído en el umbral:** no puede levantarse si su cápsula se solapa con el marco o la hoja. Es comportamiento de juego válido (hay que moverlo), pero sin el rescate técnico de la especificación 08 (watchdog). Pendiente: rescate solo ante condiciones técnicas (no finitos, bajo el mapa, sin progreso por atasco técnico).
- **Balance:** con la camioneta nueva, el carrito en solitario sigue siendo la vía más estable (~23–73 s). Medirlo a mano.
- **Crash del driver NVIDIA** (`nvwgf2umx`, creación de D3D11) en 1 de 16 arranques simultáneos (sesión 4). El script ahora escalona 4 s los arranques, conserva el registro y reintenta una vez antes de marcar BLOCKED.

## 3. Pendiente de implementar (en orden)

1. M3 restante: fuga mejorada (rutas hacia la salida o el banco cuando está libre), derrota y reintento ya funcionan por tiempo; revisar escalado efectivo al cambiar jugadores y el rescate técnico (watchdog).
2. Flujo completo: **escrito y compilado, sin verificar en uso** → escena `Frontend` (menú: solo con dificultad, con amigos (anfitrión o IP), opciones con sensibilidad, invertir Y, FOV y volumen, salir), pausa con Esc (detiene el tiempo solo en solitario), resultados con reintentar o menú, vuelta al menú si se va el anfitrión. Falta: prueba de navegación en la build y prueba automática del flujo.
3. Escenario `Bar_LastRound` con el kit 3D (FBX copiados en `Assets/Game/Art/Kit3D/` y `Kit3DImporter` escrito; falta ejecutar el informe de importación y armar la escena). PhysicsLab queda para diagnóstico.
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

## Red: resultados con la versión actual

Ver la última ejecución de `powershell -ExecutionPolicy Bypass -File tools\windows\net_smoke.ps1 -Scenario all` en `.local-setup/logs/net/net_smoke_results.csv`. Todo es **localhost** (2 procesos en la misma PC). La red degradada usa el simulador de red oficial (Multiplayer Tools 2.2.12) en la build de desarrollo: 120 ms ±20 ms y 3 % de pérdida en el cliente. LAN e internet: NOT_RUN.

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

1. Leer `net_smoke_results.csv` de la corrida completa y registrar PASS/FAIL por escenario arriba; corregir lo que falle.
2. Verificar el flujo de menús en la build (Frontend → solo → resultados → menú) y agregar una prueba PlayMode del flujo.
3. Ejecutar `Kit3DImporter.ImportAndReport`, registrar medidas reales y armar `Bar_LastRound`.

## Al cerrar una sesión

Anotar comportamiento implementado, archivos, commit, comandos y pruebas ejecutadas, resultados, bloqueos y siguiente acción.
