# Estado del proyecto

Última actualización: 2026-11-16, sesión 8.

## Estado vigente

**Build de referencia** (local, no versionada): `Builds/UncleExtraction/UncleExtraction.exe` (release Mono) y `Builds/UncleExtraction_Dev/` (desarrollo, con el simulador de red).
- Ambas salen del commit **`14a8c6c`**. `VERSION.txt` lo genera `BuildTools` en cada build: commit, si había cambios sin commitear, fecha y tipo.
- `LEEME.txt` es una copia de `docs/COMO_PROBAR.md`.

**Qué se puede jugar:**
- **Bar_LastRound**, solo o en cooperativo LAN por IP. El tío articulado tiene 11 cuerpos y la malla del kit.
- **Carrito y camioneta** con piezas del kit sobre la geometría funcional.
- **Interfaz del kit:** menú, HUD, pausa, resultados y opciones.
- **Idiomas:** inglés y español rioplatense.
- **Controles** de teclado y mouse reasignables.
- **Señales de ataque** sobre el cuerpo y en el piso.
- **Feedback de agarre** y motivos visibles de bloqueo.

- **Sala previa (lobby) en red local** (desde `f2b4ebc`):
  - confirmación «listo» por jugador; dificultad solo del anfitrión;
  - inicio solo con todos listos, con el motivo visible si no se puede;
  - el reintento en red vuelve a la sala;
  - rechazo con motivo (sala llena, otra versión, ronda iniciada).

- **Sonido provisional sintetizado** (señal distinta por ataque, impactos, agarres, puertas, fin de ronda) y **subtítulos del tío** del catálogo de diálogos, con filtro de insultos. Sin voz grabada ni música; origen y licencias en `CREDITS_ASSETS.md`.

**Sin internet.**

**Evidencia de esta sesión:**
- **PM** = PlayMode en el editor (anfitrión en loopback).
- **AUTO** = partida completa automatizada con entradas de jugador.
- **NET** = dos procesos de la build en **localhost**. No es LAN ni internet.
- **VIS** = capturas reales de la build (`-ueShot`, `.local-setup/shots/s8/`, no versionadas). Ninguna fila tiene prueba humana.

| Función | Código / build | Resultado |
| --- | --- | --- |
| Regresión completa (arreglos del bot en la puerta) | `ddecaf2` | PM **84/86**, EditMode 9/9. AUTO **12/12** (los tres intentos de carrito pasaron: Bar 57,8 s, laboratorio 52,2 s, articulado 33,2 s). **FAIL 1** `PhysicsLabPlayTests_Articulated.Uncle_Escapes_BackInsideThroughTheDoor`: el tío quedó en el umbral (z = −1,50 con la condición > −1,5) a los 15 s; aislada pasó 2/2, así que es intermitente (1 de 3 corridas). **FAIL 2** prueba nueva mal planteada: el caso «pie en la abertura» no aplica al cuerpo central (una cápsula, motivo «una parte del cuerpo»); se marca como no aplicable (Ignore) en esa variante. |
| Regresión completa (interfaz del kit) | `ba443bf` | PM **75/77**, EditMode 9/9. **FAIL 1** (`SoloAttemptTests_Bar` carrito): el bot quedó 150 s sosteniendo el asa del carrito cargado trabado en la puerta (yaw −38°, hoja a 52°) a 1,8 m, esperando que el agarre lo trajera. **FAIL 2** (`SoloAttemptTests_Articulated` carrito): el bot no llegó a cargarlo en el carrito y lo arrastró a mano hasta la camioneta (ganó, pero la prueba exige el carrito). Resto: Bar arrastre Normal 21,4 s y Difícil 81,0 s; laboratorio central carrito 31,6 s y arrastre 174,4/53,6 s; laboratorio articulado arrastre 18,9/41,8 s. |
| Pose del FAIL 1 reproducida (`StuckRecoveryTests`, pose `bar_fail_m38`) | código siguiente a `ba443bf` | PM: un jugador que tira caminando del asa la saca. **Causa: punto muerto del bot**, no la geometría: con el asa a más de 1,25 m se quedaba quieto esperando. Corregido: si el carrito no avanza en 1,5 s, el bot se acerca al asa y vuelve a tirar. El FAIL 2 queda abierto como fallo intermitente del bot. |
| Bot de carrito en la puerta: tres modos más (código posterior a `ba443bf`) | ver commit siguiente | Partidas completas (AUTO) tras el arreglo de la espera: **14/15**. FAIL: laboratorio central, carrito a −28° en el marco. **Reproducido** (`lab_fail_m28`, en el bar y en el laboratorio): tirar recto **no** lo saca. Un jugador lo resuelve empujándolo hacia adentro, alineándose y volviendo a tirar (3,7–3,9 s, un intento); no hay cambio de juego. El bot ahora hace esa maniobra. Tanda siguiente **8/9**: laboratorio central, carrito derecho pero corrido 1,37 m del eje contra la pared interior; el bot ahora también se recentra. Tanda siguiente de los tres intentos de carrito: **3/3** (Bar 63,9 s, laboratorio 29,9 s, articulado 33,9 s). Una tanda no prueba que la intermitencia desapareció. |
| Regresión completa (señales de ataque, convergencia, desvío del bot) | `dfedb95` | PM **74/74**, EditMode 9/9. AUTO 12/12: Bar carrito 51,3 s; arrastre Normal 17,7 s y Difícil 52,3 s; laboratorio articulado carrito 37,2 s y arrastre 30,7/26,9 s; laboratorio central carrito 27,7 s y arrastre 23,8/17,4 s. |
| Regresión anterior (atascos) | `a68a318` | PM **61/62**. FAIL del bot de carrito en el laboratorio articulado: el asa quedó contra la pared y el bot caminó contra el carrito 280 s. Corregido en el bot (`dfedb95`): ahora rodea hasta un costado libre del asa. |
| Regresión completa (lobby) | `9dc24c6` | PM **86 pasadas, 0 fallas, 1 omitida** (pie en la abertura con el cuerpo central: no aplica), EditMode 14/14. AUTO 12/12: Bar carrito 92,0 s, arrastre 21,2/69,8 s; laboratorio carrito 27,4 s, arrastre 49,7/84,6 s; articulado carrito 34,1 s, arrastre 18,9/60,8 s. |
| Sonido y subtítulos (`AudioDialogueTests`, `UiTests`) | commit siguiente | EditMode 16/16. La prueba encontró saturación en el impacto (pico 1,13), corregida normalizando los clips. Las cuatro señales de ataque son distintas entre sí. Catálogo: 54 líneas del perfil alcohol, columnas limpias con el filtro, enfriamiento respetado. PM interfaz y flujo 5/5, con líneas reales en el registro (`round_start`, `grab_first`, `round_timeout`). Sin prueba de escucha humana. |
| **Red completa con sala, sonido y subtítulos** (`-Scenario all`, 12 escenarios) | build `14a8c6c` | NET **12/12 PASS en la primera tanda, sin repeticiones**: coop, repeat, lose (derrota a los 10 s), netsim, race, hostleave, clientdrop, loaddrop, closedrop, **party3** (3 procesos), **party4** (4 procesos), latejoin. Todos los finales convergieron dentro del mismo intento. Solo localhost: no es LAN ni internet. |
| **Red con sala previa, 3 y 4 procesos** (`-Scenario all`, 12 escenarios) | build `f2b4ebc` | NET primera tanda: **10/12**. Pasaron coop, netsim (RTT ~252 ms), race, hostleave, clientdrop, loaddrop, closedrop, **party3** (3 procesos, inicio desde la sala con 3, los tres con victoria y mismo final convergido), **party4** (4 procesos, ídem con 4) y **latejoin** (cliente que llega con la ronda iniciada: el anfitrión lo rechaza y el cliente ve `error.already_started`). **FAIL `repeat` y `lose`, los dos de instrumentación:** iniciar desde la sala cuenta un reinicio (el script esperaba `reinicios=1`) y el límite de prueba de 20 s ahora corre desde el inicio en la sala (el bot ganó a los 16,6 s). Script corregido (intento inicial + 1; límite de 10 s). Repetición de esos dos: PASS (2/2). Una tanda completa nueva sigue pendiente. Todo en localhost: no es LAN ni internet. |
| Red en Bar_LastRound, `-Scenario all` | build `ba443bf` | NET **9/9 PASS en la primera tanda, sin repeticiones**: coop, repeat, lose, netsim (RTT del cliente ~255 ms, 3 % de pérdida), race, hostleave, clientdrop, loaddrop, closedrop. Todos los finales convergieron dentro del mismo intento (`convergencia=0,00s`). Historial en `.local-setup/logs/net/net_smoke_history.csv`. |
| **Atascos abiertos** (`StuckRecoveryTests`, jugador guionado con entradas normales) | `a68a318` y siguientes | PM 5/5. Pie en la abertura: el HUD dice «Le queda un pie afuera: empujalo adentro» y el jugador lo resuelve. Jugador detrás del tío: sale por el costado. Carrito torcido en la puerta (45° y 90°) con el tío atado: sale tirando recto del asa. Fuerza de la puerta medida. |
| **Señales de ataque** (`AttackSignalTests`, 3 variantes × 4 ataques) | `dfedb95` | PM 12/12. En el articulado, la preparación mueve una extremidad 0,13–0,51 m y el golpe, 0,16–0,24 m más; sigue de pie y sin uniones estiradas. El aviso del piso es visible y se llena con la preparación. VIS: patada con el sector del piso apuntando al jugador y «VA A PATEAR». |
| **Interfaz del kit** (`UiTests`) | `4860437` | PM 3/3: manos «sujetando» tras agarrar; aviso de ataque localizado que cambia con el idioma; resultados reales; avisos limpios al terminar; menú con texto sin cambiar de idioma; remapeo con intercambio. VIS: menú, HUD, aviso de ataque y resultados (fallos vistos y corregidos: botones vacíos, superposiciones, «1 JUGADORES», nombres del mouse en inglés). |
| Escenario, extracción, red, tío articulado, URP y materiales | sesión 7 | Ver «Historial»; esta sesión se volvió a verificar con la regresión y la red de arriba. |

**Diagnóstico de los dos atascos (pedido de esta sesión):**
- **Pie o mano en la abertura de la camioneta: problema de interacción, corregido en el juego.**
  - El motivo ahora nombra la parte y una marca la señala.
  - La mira ya no deja que el punto que sostiene una mano le gane al segundo punto cercano: esa era la causa de que la segunda mano no tomara el otro hombro, tanto del jugador como del bot.
  - Un jugador lo resuelve empujando el tobillo o tirando de los dos hombros hacia la cabina.
  - No se tocaron colisiones, el cuerpo ni la extracción.
- **Carrito atravesado en la puerta del bar: fallo del bot.**
  - Un jugador lo saca tirando recto del asa.
  - El primer «atasco irrecuperable» era un error del armado de la prueba (carrito encimado con la hoja); se corrigió y se revirtió el retén de la puerta que había motivado.
  - Bot: junto a la puerta no zigzaguea y rodea el carrito cuando el asa queda contra una pared.

**Fallos intermitentes conocidos** (se mantienen visibles):
- **Bot del modo carrito:** falló en 2 de las 4 regresiones completas de esta sesión.
  - `a68a318`: el asa contra la pared. Corregido.
  - `ba443bf`: dos FAIL. El bot quedó esperando con el asa lejos (corregido) y no llegó a cargarlo en el laboratorio articulado (abierto).
  - Sigue siendo el punto más inestable de las pruebas automáticas. No es una regla del juego.
- **Red:** en la sesión 7, `coop` falló 1 de 3 veces (bot con el pie del tío en la puerta) y hubo un crash del driver NVIDIA en un arranque simultáneo. En esta sesión, 9/9 en una tanda.

## Pendiente (en orden)

1. Lobby: falta la pantalla de conexión con cancelación y la pérdida del anfitrión con el kit (hoy, mensaje localizado en el menú). Nombres de jugador elegibles (hoy, «Jugador N»).
2. 3 y 4 procesos verificados en localhost (`party3`/`party4`). Falta LAN con varias PC.
3. Audio de producción: voz del tío (actor o voz licenciada), música de bar ficticia, efectos grabados por material. Hoy: efectos sintetizados y subtítulos.
4. Validación de la interfaz a 720p, ultrawide y texto al 150 % (necesita una persona). Navegación con mando.
5. Rendimiento, sesiones largas, las tres dificultades, QA, build reproducible.

## Bloqueado por cuentas, equipos o participantes

| Bloqueo | Paso mínimo |
| --- | --- |
| Internet (Sessions/Relay) | En cloud.unity.com: crear o elegir organización y proyecto, habilitar Multiplayer Services/Relay y pasarme el Project ID, o vincularlo en el editor (Edit → Project Settings → Services). |
| LAN | Otra PC en la misma red: `UncleExtraction.exe` → Jugar con amigos → Crear partida en la red local (aceptar el firewall) en una, y Unirse por IP en la otra. |
| Steam | Cuenta de Steamworks, pago de Steam Direct y App ID. |
| IL2CPP | Visual Studio 2022 con «Desarrollo de juegos con C++». |
| Prueba humana | Jugar `Builds/UncleExtraction/UncleExtraction.exe` siguiendo `LEEME.txt`. No bloquea el desarrollo. |

## Próxima acción exacta

1. Pantalla de conexión con cancelación y nombres de jugador elegibles.
2. Validación humana de la build (LEEME.txt) y LAN con dos PC cuando haya una segunda PC.

## Historial (resultados de sesiones anteriores, sin reescribir)

### Sesión 7 (2026-11-16): tabla de evidencia tal como quedó

Los niveles de evidencia son: **PM** = PlayMode en el editor (anfitrión en loopback); **AUTO** = partida completa automatizada con entradas de jugador (sin teletransportes ni `Secure()` directo); **NET** = dos procesos de la build en **localhost**. Nada de esta tabla tiene prueba humana, LAN ni internet.

| Función | Evidencia |
| --- | --- |
| Escenario principal `Bar_LastRound` (salón de 18 × 12 m, barra del kit con baranda de anclaje y banco, 4 mesas y 2 banquetas del kit, karaoke, puerta del kit de 1,5 m, vereda de 14 m, camioneta hueca con rampa y puertas) | Geometría PM: `LevelGeometryTests` 2/2 (apariciones libres, ruta del tío sin choques con sólidos fijos, nada atravesado al empezar). AUTO: arrastre Normal 24,2 s, arrastre Difícil 31,0 s, carrito Normal 54,3 s. |
| `LevelLayout`: apariciones, ejes de puerta y camioneta, rutas y puntos de prueba por escena. Autopiloto y apariciones sin coordenadas fijas | Los mismos intentos pasan en PhysicsLab y en Bar_LastRound. |
| Menú: escenario principal y «Laboratorio»; build con Frontend → Bar_LastRound → PhysicsLab | PM: `GameFlowTests` (menú → solo en Difícil → derrota → reintento → menú). |
| Extracción real (pose dentro, sin solapes, abertura libre, E 1 s desde afuera, victoria con las hojas cerradas; aborta y reabre si fallan las condiciones; dos jugadores no aceleran) | PM (PhysicsLab): 5 pruebas de cierre y reinicio. AUTO y NET en ambas escenas. |
| Vaso señuelo, ayuda para levantarse, rescate técnico | PM. |
| Fuga: libre, vuelve a su banco **por la puerta** (no contra el muro); más rápido (2,2 m/s) si hay alguien a menos de 3 m | PM: `Uncle_Escapes_BackInsideThroughTheDoor`. |
| Red en **Bar_LastRound** (NET, `tools/windows/net_smoke.ps1 -Scene Bar_LastRound`) | coop PASS (24,1/24,0 s, mismo estado final en ambos); reintento tras ganar PASS (2 victorias, `reinicios=1` en ambos); derrota por tiempo → reintento → cliente al menú PASS; red simulada 120 ms ±20 ms y 3 % de pérdida PASS (59,0/58,9 s, RTT del cliente ~246 ms); carrera por el socket PASS; salida del anfitrión PASS (el cliente vuelve al menú sin ganar solo); desconexión del cliente agarrando PASS (0 agarres); **desconexión durante la carga del carrito** PASS (anfitrión con 0 agarres, carrito «Vacío», sin excepciones); **desconexión durante el cierre** PASS (puertas `Open`, sin asegurar, sin excepciones). |
| **Tío articulado (ragdoll reducido)** en **Bar_LastRound** y `PhysicsLab_Articulated` (11 cuerpos, 110 kg en total, malla del kit ligada; PhysicsLab conserva el cuerpo central como respaldo; volver atrás en el bar: `LevelSpecs.Bar()` → `articulatedUncle = false`) | PM: las 18 pruebas de laboratorio corren en las dos variantes (`PhysicsLabPlayTests` y `PhysicsLabPlayTests_Articulated`): agarres y 10 reinicios sin huérfanos, arrastre, empujón y caída del jugador, cierre con dos jugadores y abortos, extracción por partes (medio afuera no cierra), reinicio a mitad del cierre y de la carga (sin colisiones ignoradas residuales tío-carrito ni tío-jugador), señuelo, ayuda, fuga por la puerta, rescate técnico de todo el cuerpo, intento de soltarse del carrito con señal, masa 110 kg, quieto sin temblor ni uniones estiradas, empujón leve sin caída, se levanta pegado a la baranda. NET (localhost, `-Scene PhysicsLab_Articulated`): coop, carrera por el socket, desconexión agarrando, durante la carga, durante el cierre y salida del anfitrión PASS. |
| **Red con el tío articulado en Bar_LastRound** (NET localhost, build `a0309d7`) | `-Scenario all`: coop (22,0/21,9 s), reintento, derrota → reintento → menú, red simulada 120 ms ±20/3 % (26,0 s), carrera, salida del anfitrión, desconexión agarrando y durante el cierre PASS; **desconexión durante la carga: NOT_RUN** en la corrida completa (el bot cliente no llegó a cargar en 240 s) y **PASS** al repetirla sola. `PhysicsLab_Articulated`: `lose` y `coop` PASS. Sin LAN ni internet. |
| **URP** 17.3.0 (asset y renderer reproducibles por código, materiales del constructor y del kit en URP/Lit, sombras) | Build: capturas con `-ueShot` del bar al empezar, arrastrando y en la camioneta (`.local-setup/shots/`, no versionadas): el tío del kit, la barra, las banquetas y las sombras se ven. PM: 56/57 (`pt_full_urp.xml`; el fallo es el FAIL intermitente del bot de carrito, esta vez en el laboratorio articulado), EditMode 9/9. |
| **Red con URP** (NET localhost, builds de `e236eea` y del commit siguiente) | `-Scenario all` en Bar_LastRound con `e236eea`: 8 PASS; `netsim` FAIL porque el cliente registraba el estado final en el primer cuadro con la ronda terminada y, con 3 % de pérdida, la zona asegurada llegaba después (`asegurado=False` en el cliente). Corregido en el autopiloto: el estado final se registra al salir (1 s/3 s). Después: `netsim` y `coop` PASS 2/2. |
| **Materiales del kit sobre URP** en Bar_LastRound: `MAT_Floor_Tiles` (repetición 2 m, Repeat, smoothness 0,18), `MAT_Wall_Taupe` (2 m, Mirror, 0,05), `MAT_Wood_Boards` (1 m, Mirror, 0,22) en 8 superficies; mallas de caja con UV en metros/repetición (Tiling 1,1 como las variantes FBX). Texturas importadas a **1024 px** (fuente 1254 px; NPOT «ToNearest» propuesto por el kit). Colliders, masas y sockets sin cambios; el laboratorio conserva colores lisos | Captura de la build (`mat_bar_start.png`, local): baldosas y revoque a escala física. PM: geometría 4/4, menú 1/1, intentos del bar: carrito 53,5 s y arrastre Normal 19,7 s PASS, **arrastre Difícil FAIL** (bot: quedó afuera contra la pared frontal yendo en línea recta hacia el tío que volvió a su banco; no pasa por la puerta). |
| **Red en Bar_LastRound con URP, materiales y el tío articulado** (NET localhost) | Build de `ac4e014`: 7 PASS; `lose` FAIL por una regresión mía de instrumentación (el cliente registraba el estado final después del reinicio) y `race` FAIL por un **crash del driver NVIDIA** al crear el dispositivo D3D11 en un arranque simultáneo (`nvwgf2umx.dll`, entorno). Corregido el registro (0,5 s después de terminar la ronda): `-Scenario all` con 8 PASS (incluidos `lose`, `race`, `netsim`) y `coop` FAIL una vez (bot: el pie del tío articulado quedó en la franja de la puerta —«le queda algo afuera», condición correcta— y ningún bot lo terminó de meter; el cliente quedó dentro de la camioneta entre el brazo del tío y la pared); repetido `coop` 2/2 PASS. |
| `net_smoke.ps1` con códigos estrictos | 0 solo si todos los escenarios pedidos dieron PASS; 1 si hubo FAIL; 2 si algo quedó BLOCKED o NOT_RUN. Cierra procesos previos del juego y marca BLOCKED si el puerto está ocupado. |

Los fallos de esta etapa ya están corregidos y se verificaron después:
- **Carrito en la esquina (geometría):** quedaba encajado contra las paredes; ahora está en el salón.
- **Bot empujando el carrito (ruta):** recalculaba los objetivos con el carrito en movimiento y lo empujaba contra la pared.
- **`[Timeout]` (prueba):** no se heredaba en la subclase.
- **Script de red:** colisión de variables `$H`/`$h`, array desenvuelto y procesos zombis que ocupaban el puerto 7777.

### Sesión 7: observaciones y fallos

- **Batería con `a0309d7`** (PM, bar con el tío articulado): PlayMode **57/57** (`pt_full_f4.xml`), EditMode 9/9. Intentos: Bar carrito 43,1 s, arrastre Normal 20,7 s, Difícil 52,0 s; laboratorio articulado carrito 67,4 s, arrastre 41,6/37,2 s; laboratorio central carrito 27,5 s, arrastre 43,4/105,1 s.
- **Observación (playtest):** con el tío atado y nadie en el asa, el carrito no tiene freno y un jugador que lo choca caminando lo empuja varios metros. Se probó frenarlo también atado y se revirtió (la amortiguación sobre los 30 kg del carrito no alcanza: 3–4 m en 2 s, y empeoró al bot).
- **Batería con `724ffe1`** (PM): PlayMode **50/50** (`pt_full_art3.xml`), EditMode 9/9. Intentos completos: PhysicsLab carrito 84,7 s, arrastre Normal 41,5 s, arrastre Difícil 78,1 s; Bar_LastRound carrito 30,5 s, arrastre Normal 30,5 s, arrastre Difícil 53,6 s; PhysicsLab_Articulated carrito 24,6 s, arrastre Normal 12,0 s, arrastre Difícil 14,5 s. En esa misma sesión, corridas anteriores dieron FAIL y se corrigieron antes de esta batería (detalle abajo); ninguna se cuenta como PASS.
- **Fallos encontrados y corregidos en la sesión 7:**
  - *Juego:* la carga cinemática del tío articulado en el carrito lanzaba al jugador (~3 m en 0,45 s). Ahora las colisiones tío-jugador se ignoran solo durante la subida y se restauran siempre (prueba de reinicio a mitad de la carga lo verifica).
  - *Juego:* caído junto a la baranda, el tío no se levantaba nunca (buscaba lugar solo bajo su centro de masa). Ahora busca el lugar libre más cercano hasta 0,8 m. Prueba nueva; **no** comprobé que fallara con el código anterior.
  - *Juego:* el articulado caía con un empujón leve (el par de equilibrio actuaba sobre 50 kg). Ahora usa la inercia de todo el cuerpo. Prueba nueva que fallaba antes del cambio (reproducido) y pasa después.
  - *Bot:* quieto para siempre con el asa del carrito tapada por el tío acostado; carrito girado 90° por la hoja de la puerta y arrastrado contra la pared (intermitente, también con el tío central); cliente fuera del alcance de cierre sin acercarse (`closedrop` NOT_RUN 2/2 en Bar). Corregidos en la ruta del bot; las reglas del juego no cambiaron.
  - *Build:* `PhysicsLab_Articulated` no estaba en la lista de escenas de `BuildTools` (la primera corrida de red dio FAIL/NOT_RUN por eso).
- **FAIL intermitente conocido — bot del modo carrito en PhysicsLab con el tío central.** En la sesión 7 los intentos con carrito fallaron de forma intermitente en las tres escenas (central de laboratorio: 5 de 8 antes de los últimos ajustes; después, fallos aislados también en el articulado y en el bar). Los tres fallos son de navegación del bot con el carrito por la puerta (carrito girado o atravesado en el marco; en el último, el tío se soltó en la puerta, volvió al bar y el bot trabó el carrito vacío en el marco). Ninguno es una regla del juego, pero un jugador también puede trabar el carrito ahí: revisarlo en el playtest. Otro modo visto en arrastre Difícil del bar: el bot afuera caminaba en línea recta hacia el tío adentro y quedaba contra la pared; corregido (`Via`: alinearse con la puerta y cruzarla). Después: intentos completos **9/9** (`pt_via.xml`). Última corrida de intentos completos sobre el código del commit que agrega esta nota (`pt_solo_cs2`): **8/9**; el fallo es este (Lab carrito central). Los demás: Lab arrastre Normal 51,3 s y Difícil 53,6 s; articulado carrito 22,4 s, arrastre 12,0/13,6 s; Bar carrito 30,2 s, arrastre Normal 31,7 s y Difícil 87,9 s. No se debilitó ninguna condición.
- **Prueba de carrito en partida completa:** exige que el tío intente soltarse cuando la regla lo permite (atado con más de 40 % de presupuesto por más que el intervalo máximo de 10 s). Agotado no lo intenta (fatiga, GDD 05); eso pasó en una corrida del articulado y es conducta correcta. La conducta en sí la exige siempre `Cart_StrappedUncle_SignalsAndTriesToWriggleFree`.
- **Dificultad del articulado (resuelto con evidencia automática, falta prueba humana):** de pie lo sostiene un resorte de altura que reemplazaba el apoyo de los pies pero no su fricción; se deslizaba (arrastre Difícil 14,5 s con 1 ataque). Primero se agregó fricción repartida por masa y quedó inamovible para un jugador (no se inclinaba nunca); finalmente se aplica en el punto de apoyo, como los pies del central. Con `a0309d7`: arrastre Difícil 37–55 s con 4–7 ataques en el laboratorio y 52,0 s con 6 en el bar (central: 52–120 s con 6–13). Arrastre del tío caído (`Measure_DragFallenUncle`): hombros 2,45 vs 2,60 m/s, tobillos 1,29 vs 1,11 m/s. Si se siente distinto al jugarlo, revisar `footFriction` y `hoverHeight`.
- **Red con el articulado** (NET localhost, build de `724ffe1`, `-Scene PhysicsLab_Articulated`): coop, reintento, red simulada (RTT ~209 ms), carrera, salida del anfitrión, desconexión agarrando, durante la carga y durante el cierre PASS; `lose` FAIL (ver arriba).
- **Red en Bar_LastRound** (build de `724ffe1`): coop, reintento, derrota, red simulada (RTT ~236 ms), carrera, salida del anfitrión, desconexión agarrando y durante la carga PASS; `closedrop` NOT_RUN (bot). Con la corrección del bot (commit siguiente): `closedrop` y `coop` PASS 2/2.
- **Balance:** el carrito en solitario sigue siendo probablemente demasiado fácil; revisar en el playtest antes de tocar valores.
- **Visuales del kit:** camioneta y carrito siguen siendo primitivas; el tío del kit solo está en la variante articulada. Ver `docs/CATALOGO_RECURSOS.md`.


### Antecedentes (resumen; el detalle está en el log de Git y en `docs/DECISIONES_IMPLEMENTACION.md`)

- **M0/M1:** Unity 6000.3.28f1, PhysicsLab y autopiloto.
- **M2 (`2a42a0b`, `c27d209`):** NGO con anfitrión autoritativo.
- **M3 (`f1728e0` → `9f9b0bb`):** camioneta real con cierre, señuelo, ayuda, menús, rescate técnico y red con latencia simulada en PhysicsLab.
- **Sesión 6 (`d8c204b` y siguientes):** Bar_LastRound y LevelLayout.

