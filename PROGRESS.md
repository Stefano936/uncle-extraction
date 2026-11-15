# Estado del proyecto

Última actualización: 2026-11-15 (Claude Code local, sesión 4: M2 red temprana).

## Hito actual

**M2 — red temprana: núcleo implementado y verificado con dos procesos en localhost.** Host autoritativo con Netcode for GameObjects 2.14.0 + Unity Transport 2.7.5. El modo individual es un anfitrión en loopback (mismo camino de autoridad). Pendiente de M2: prueba en LAN real y medición con latencia simulada. Internet (Relay) es M4.

Siguiente hito en curso: interacciones y recorrido (M1/M3 pendientes) — ver «Próxima acción exacta».

## Verificación M2 (2026-11-15)

| Prueba | Condición | Resultado | Evidencia |
| --- | --- | --- | --- |
| Compilación | Unity 6000.3.28f1 | PASS, 0 errores | build `51_build.log` (1 warning de Unity, no de código del juego) |
| EditMode | — | PASS 9/9 | `47_editmode.xml` |
| PlayMode (modo individual = anfitrión loopback) | tiempo real | PASS 7/7 | `50_playmode.xml`: 4 de laboratorio + arrastre Normal 55,8 s, Difícil 42,8 s, carrito Normal 25,8 s |
| Cooperativo 2 jugadores reales (anfitrión + cliente, cada uno con autopiloto) | **localhost**, 2 procesos | PASS 2/3; 1 BLOCKED (crash del driver NVIDIA al crear D3D11 en el arranque simultáneo, antes de cargar el juego) | `net_smoke.ps1`: éxito en anfitrión 21,5 s / cliente 21,4 s; 15,6 / 15,6 s; RTT anfitrión→cliente 5–7 ms. Antes: 16,4 s (`64_*`) |
| Carrera por el mismo socket (cinturón) en el mismo instante de reloj | localhost | PASS 2/2 | nunca dos agarres vivos sobre `belt`; gana uno (anfitrión), el otro recibe `SocketBusy` / «Ocupado» |
| Desconexión del cliente mientras agarra | localhost (proceso matado) | PASS 2/2 | el anfitrión detecta la salida y queda en 0 agarres |
| Salida del anfitrión a mitad de ronda | localhost | PASS 2/2 | el cliente recibe la desconexión, no simula por su cuenta ni «gana», vuelve a jugar solo |
| Cliente solo contra tío escalado a 2 jugadores (anfitrión quieto) | localhost | NO TERMINA en 280 s | esperable: N=2 → resistencia ×1,55; el bot avanza lento en la puerta (`63_*`). No es fallo de red |
| Golpes, caída y liberación vistos desde un cliente | localhost | PASS parcial | el cliente ve preparación/ataque replicados (`62_client.log`: MANOTAZO); impactos y caída se resuelven en el anfitrión. Sin verificación visual humana |
| LAN (dos PCs) | — | NOT_RUN | requiere otra PC; usar `-ueHost` (escucha 0.0.0.0, aviso del firewall de Windows) y `-ueClient <IP>` |
| Latencia simulada | — | NOT_RUN | pendiente: simulador de red de UTP |
| Internet (Sessions/Relay) | — | NOT_RUN (M4) | requiere proyecto de Unity Cloud vinculado (cuenta de Stefano) |
| Jugado a mano | — | NOT_RUN | sin control de pantalla en la sesión |

Comando de las pruebas de red: `powershell -ExecutionPolicy Bypass -File tools\windows\net_smoke.ps1 -Scenario all -Runs 2` → `.local-setup/logs/net/net_smoke_results.csv`.

## Arquitectura de red implementada (M2)

- `Scripts/Net/NetSession.cs`: crea y reutiliza el NetworkManager en tiempo de ejecución. Modos: Solo (host 127.0.0.1, puertos 7790+), Host (`-ueHost`, `-ueListen`, `-uePort`), Client (`-ueClient <ip>`). Aprobación con tope de 4 y `ProtocolVersion` (compatibilidad de versión). Gestión de escenas de NGO activada. Si se va el anfitrión: aviso, el cliente queda congelado (`NetAuth.Frozen`) y vuelve a jugar solo. Diagnóstico `[NetDiag]` cada 5 s (RTT, jugadores, agarres, ronda).
- `Scripts/Net/PlayerControls.cs`: separa la entrada del jugador local (dueño) de los personajes remotos. El dueño envía intenciones secuenciadas (RPC no fiable, la identidad sale del canal); el anfitrión las aplica; sin intención 0,5 s cancela esfuerzo y 1 s suelta las manos.
- `NetAuth`: el anfitrión simula; los clientes leen estado replicado. `PlayerMotor`, `PlayerHands`, `NpcBrain`, `NpcBody`, `TransportCart`, `ExtractionZone` y `RoundDirector` son NetworkBehaviours con NetworkVariables (estados, finales de estados en tiempo de servidor, sockets por mano, tensión, textos de interacción, ronda, dificultad y N).
- Jugador = prefab de red (`Assets/Game/Prefabs/Player.prefab`) generado por conexión; cámara y oído solo del dueño. Tío, carrito, puerta y silla: NetworkObjects de escena con NetworkTransform (servidor) + NetworkRigidbody (cinemáticos en clientes).
- Escalado por número efectivo de jugadores conectados (`autoPartySize`; el panel puede fijarlo). Un cliente pide reiniciar con R y decide el anfitrión. En el laboratorio se admite unirse con la ronda en curso (el lobby de M4 lo bloqueará).

## Correcciones de juego encontradas en M2

- El tío cacheaba la lista de jugadores en `Start`, antes de que la red los genere, y los ataques directos no tenían objetivo. Ahora usa un registro vivo (`PlayerMotor.All`).
- Carga del carrito: `Strap` ya no recoloca a ciegas. Valida el volumen del tío acostado contra paredes, jugadores y objetos («No hay lugar para acostarlo (…)»). Lo sube en 0,45 s (cinemático, el destino sigue al carrito) y, mientras sube y está asegurado, ignora las colisiones tío-carrito. Antes, con alguien o una pared en el volumen, la física despedía al carrito o al jugador a través de la pared.
- El autopiloto maneja al jugador local con entrada simulada, así que sirve igual en el anfitrión que en un cliente. Modos `drag`, `cart` y `race`. En modo carrito, si no hay lugar, lleva el carrito a un espacio abierto.

## Estado de M0/M1 (sesión 3, sigue vigente)

- Laboratorio completo: jugador de dos manos, agarre con resorte limitado, tío de cuerpo central con 4 ataques anunciados, anclaje a la barra, fatiga y apertura, carrito con correa, zona de extracción provisional, HUD de depuración, 10 reinicios sin huérfanos.
- Build Windows Mono: `Builds/PhysicsLab/UncleExtraction_PhysicsLab.exe`. Con autopiloto, completa arrastre y carrito sola (sesión 3: arrastre 4/4, carrito 3/3).
- Hallazgos para el playtest: una mano no alcanza para arrastrar al tío caído (fricción ~460 N > 420 N); la puerta de 1,5 m es un cuello de botella; hay que entrarlo de costado a la zona; el carrito podría ser demasiado fácil en solitario; la varianza del arrastre es alta.

## Pendientes (trabajo propio, en orden)

1. Extracción real: el tío **dentro** de la camioneta (volumen interior, pose válida, sin solapes con sólidos, puertas traseras que se cierran con progreso). Reemplaza a la zona provisional delante del vehículo.
2. Interacciones: vaso señuelo y distracciones (`DistractionService`), ayuda de un compañero para levantarse, derrota y reintento completos, fuga mejorada.
3. Ragdoll reducido del tío (11 cuerpos), verificando estabilidad y replicación antes de reemplazar el cuerpo central.
4. Flujo completo: escena Frontend (menú, individual, cooperativo directo, dificultad, opciones, pausa, resultados, volver) y escena Bar_LastRound con el kit 3D.
5. Presentación: URP, kits de modelos/materiales/UI, señales de ataque, feedback de agarre/golpe, audio, EN/es_UY, controles remapeables (InputActionAsset).
6. M2 restante: latencia simulada; prueba LAN cuando haya otra PC.
7. M4 internet (Sessions/Relay), M5–M8 QA, rendimiento, Steam, build reproducible.

## Bloqueos externos

- **Unity Cloud (Relay/Sessions):** requiere vincular el proyecto a una organización de Unity Cloud con la cuenta de Stefano (dashboard de Unity). Bloquea M4 internet.
- **LAN real:** requiere una segunda PC en la misma red.
- **Steam:** requiere cuenta Steamworks (pago de Steam Direct) y App ID.
- **IL2CPP:** requiere Visual Studio 2022 con «Desarrollo de juegos con C++».
- **Prueba manual:** la hace Stefano; no bloquea la implementación.
- **Entorno:** crash ocasional del driver NVIDIA (`nvwgf2umx`) al arrancar dos instancias a la vez; no involucra código del juego.

## Faltantes y contradicciones de la preproducción

- `preproduction/uncle_extraction/data/asset_manifest.csv` marca los modelos como `pending/TBD`, pero el kit 3D entregó prototipos de 8 (bar, barra, puerta, camioneta, banqueta, mesa, carrito, tío). Actualizar al importarlos.
- Sin modelo: cuerpo/brazos del jugador, marco de puerta, vaso señuelo, vereda. Audio pendiente. Texturas solo Base Color 1254 px. Arte entregado = prototipo; los GLB no importan sin paquete extra (usar FBX). El kit no trae ragdoll ni triggers.
- `ENTREGA_SHA256.json` es una foto del empaquetado. `00_EMPEZAR_AQUI.md` y `docs/GUIA_GITHUB.md` describen la vinculación con GitHub ya realizada.

## Ubicación y versionado

- Carpeta principal `C:\Users\stuff\OneDrive\Escritorio\uncle-extraction`; `origin` = `Stefano936/uncle-extraction` (privado), rama `main`.
- OneDrive sincroniza `UnityProject/Library` (ignorado por Git, pesado): conviene excluirlo de OneDrive.
- Builds locales (no versionadas): `Builds/PhysicsLab/UncleExtraction_PhysicsLab.exe`.

## Próxima acción exacta

1. Claude: extracción real dentro de la camioneta (pendiente 1). Construir la camioneta con interior hueco y puertas traseras con bisagra en `PhysicsLabBuilder`, una `ExtractionZone` interior con pose y solapes, y el cierre de puertas con progreso. Adaptar el autopiloto y las pruebas sin debilitar las condiciones.
2. Después, pendientes 2 → 4 en orden. Repetir `net_smoke.ps1` tras cada cambio de red o de autoridad.
3. Stefano, cuando pueda: jugar la build (`-ueHost -ueListen 127.0.0.1` en una ventana y `-ueClient 127.0.0.1` en otra para probar el cooperativo en la misma PC) y anotar sensaciones.

## Al cerrar una sesión

Anotar comportamiento implementado, archivos, commit, comandos y pruebas ejecutadas, resultados, bloqueos y siguiente acción.
