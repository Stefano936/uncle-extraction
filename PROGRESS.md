# Estado del proyecto

Última actualización: 2026-11-16 (sesión 7: tío articulado en Bar_LastRound y `PhysicsLab_Articulated`; pruebas de resistencia separadas por modo).

**Builds de referencia** (locales, no versionadas). Cada carpeta tiene `VERSION.txt` con el commit de origen; las actuales salen de `a0309d7`:
- `Builds/UncleExtraction/UncleExtraction.exe`: release Mono, para jugar. Incluye `LEEME.txt` con controles e instrucciones (`docs/COMO_PROBAR.md`).
- `Builds/UncleExtraction_Dev/UncleExtraction.exe`: desarrollo, con el simulador de red. Solo para pruebas.

Las rutas anteriores (`Builds/PhysicsLab*`) quedaron obsoletas.

## 1. Implementado y verificado

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
| `net_smoke.ps1` con códigos estrictos | 0 solo si todos los escenarios pedidos dieron PASS; 1 si hubo FAIL; 2 si algo quedó BLOCKED o NOT_RUN. Cierra procesos previos del juego y marca BLOCKED si el puerto está ocupado. |

Los fallos de esta etapa ya están corregidos y se verificaron después:
- **Carrito en la esquina (geometría):** quedaba encajado contra las paredes; ahora está en el salón.
- **Bot empujando el carrito (ruta):** recalculaba los objetivos con el carrito en movimiento y lo empujaba contra la pared.
- **`[Timeout]` (prueba):** no se heredaba en la subclase.
- **Script de red:** colisión de variables `$H`/`$h`, array desenvuelto y procesos zombis que ocupaban el puerto 7777.

## 2. Implementado con observaciones

- **Batería con `a0309d7`** (PM, bar con el tío articulado): PlayMode **57/57** (`pt_full_f4.xml`), EditMode 9/9. Intentos: Bar carrito 43,1 s, arrastre Normal 20,7 s, Difícil 52,0 s; laboratorio articulado carrito 67,4 s, arrastre 41,6/37,2 s; laboratorio central carrito 27,5 s, arrastre 43,4/105,1 s.
- **Observación (playtest):** con el tío atado y nadie en el asa, el carrito no tiene freno y un jugador que lo choca caminando lo empuja varios metros. Se probó frenarlo también atado y se revirtió (la amortiguación sobre los 30 kg del carrito no alcanza: 3–4 m en 2 s, y empeoró al bot).
- **Batería con `724ffe1`** (PM): PlayMode **50/50** (`pt_full_art3.xml`), EditMode 9/9. Intentos completos: PhysicsLab carrito 84,7 s, arrastre Normal 41,5 s, arrastre Difícil 78,1 s; Bar_LastRound carrito 30,5 s, arrastre Normal 30,5 s, arrastre Difícil 53,6 s; PhysicsLab_Articulated carrito 24,6 s, arrastre Normal 12,0 s, arrastre Difícil 14,5 s. En esa misma sesión, corridas anteriores dieron FAIL y se corrigieron antes de esta batería (detalle abajo); ninguna se cuenta como PASS.
- **Fallos encontrados y corregidos en la sesión 7:**
  - *Juego:* la carga cinemática del tío articulado en el carrito lanzaba al jugador (~3 m en 0,45 s). Ahora las colisiones tío-jugador se ignoran solo durante la subida y se restauran siempre (prueba de reinicio a mitad de la carga lo verifica).
  - *Juego:* caído junto a la baranda, el tío no se levantaba nunca (buscaba lugar solo bajo su centro de masa). Ahora busca el lugar libre más cercano hasta 0,8 m. Prueba nueva; **no** comprobé que fallara con el código anterior.
  - *Juego:* el articulado caía con un empujón leve (el par de equilibrio actuaba sobre 50 kg). Ahora usa la inercia de todo el cuerpo. Prueba nueva que fallaba antes del cambio (reproducido) y pasa después.
  - *Bot:* quieto para siempre con el asa del carrito tapada por el tío acostado; carrito girado 90° por la hoja de la puerta y arrastrado contra la pared (intermitente, también con el tío central); cliente fuera del alcance de cierre sin acercarse (`closedrop` NOT_RUN 2/2 en Bar). Corregidos en la ruta del bot; las reglas del juego no cambiaron.
  - *Build:* `PhysicsLab_Articulated` no estaba en la lista de escenas de `BuildTools` (la primera corrida de red dio FAIL/NOT_RUN por eso).
- **FAIL intermitente conocido — bot del modo carrito en PhysicsLab con el tío central.** En la sesión 7 los intentos con carrito fallaron de forma intermitente en las tres escenas (central de laboratorio: 5 de 8 antes de los últimos ajustes; después, fallos aislados también en el articulado y en el bar). Los tres fallos son de navegación del bot con el carrito por la puerta (carrito girado o atravesado en el marco; en el último, el tío se soltó en la puerta, volvió al bar y el bot trabó el carrito vacío en el marco). Ninguno es una regla del juego, pero un jugador también puede trabar el carrito ahí: revisarlo en el playtest. Última corrida de intentos completos sobre el código del commit que agrega esta nota (`pt_solo_cs2`): **8/9**; el fallo es este (Lab carrito central). Los demás: Lab arrastre Normal 51,3 s y Difícil 53,6 s; articulado carrito 22,4 s, arrastre 12,0/13,6 s; Bar carrito 30,2 s, arrastre Normal 31,7 s y Difícil 87,9 s. No se debilitó ninguna condición.
- **Prueba de carrito en partida completa:** exige que el tío intente soltarse cuando la regla lo permite (atado con más de 40 % de presupuesto por más que el intervalo máximo de 10 s). Agotado no lo intenta (fatiga, GDD 05); eso pasó en una corrida del articulado y es conducta correcta. La conducta en sí la exige siempre `Cart_StrappedUncle_SignalsAndTriesToWriggleFree`.
- **Dificultad del articulado (resuelto con evidencia automática, falta prueba humana):** de pie lo sostiene un resorte de altura que reemplazaba el apoyo de los pies pero no su fricción; se deslizaba (arrastre Difícil 14,5 s con 1 ataque). Primero se agregó fricción repartida por masa y quedó inamovible para un jugador (no se inclinaba nunca); finalmente se aplica en el punto de apoyo, como los pies del central. Con `a0309d7`: arrastre Difícil 37–55 s con 4–7 ataques en el laboratorio y 52,0 s con 6 en el bar (central: 52–120 s con 6–13). Arrastre del tío caído (`Measure_DragFallenUncle`): hombros 2,45 vs 2,60 m/s, tobillos 1,29 vs 1,11 m/s. Si se siente distinto al jugarlo, revisar `footFriction` y `hoverHeight`.
- **Red con el articulado** (NET localhost, build de `724ffe1`, `-Scene PhysicsLab_Articulated`): coop, reintento, red simulada (RTT ~209 ms), carrera, salida del anfitrión, desconexión agarrando, durante la carga y durante el cierre PASS; `lose` FAIL (ver arriba).
- **Red en Bar_LastRound** (build de `724ffe1`): coop, reintento, derrota, red simulada (RTT ~236 ms), carrera, salida del anfitrión, desconexión agarrando y durante la carga PASS; `closedrop` NOT_RUN (bot). Con la corrección del bot (commit siguiente): `closedrop` y `coop` PASS 2/2.
- **Balance:** el carrito en solitario sigue siendo probablemente demasiado fácil; revisar en el playtest antes de tocar valores.
- **Visuales del kit:** camioneta y carrito siguen siendo primitivas; el tío del kit solo está en la variante articulada. Ver `docs/CATALOGO_RECURSOS.md`.

## 3. Pendiente de implementar (en orden)

2. URP, materiales y texturas del kit, animaciones del kit (Idle/Push/Kick) y feedback visual de agarres y golpes.
3. Interfaz con el kit de UI (hoy IMGUI provisional), localización EN/es_UY, controles remapeables (InputActionAsset) y opciones completas.
4. Sonido y diálogos con procedencia y licencia registradas.
5. Sessions/Relay y lobby: preparación, cupos, compatibilidad de versión, carga, errores y bloqueo de incorporación con la ronda iniciada. Hoy el laboratorio admite unirse en curso.
6. Pruebas con 3 y 4 clientes, las tres dificultades, rendimiento y sesiones largas; QA; build reproducible.

## 4. Bloqueado por cuentas, equipos o participantes

| Bloqueo | Qué tiene que hacer Stefano |
| --- | --- |
| Internet (Sessions/Relay) | En cloud.unity.com: crear o elegir organización y proyecto, habilitar Multiplayer Services/Relay y pasarme el Project ID, o vincularlo en el editor (Edit → Project Settings → Services). |
| LAN | Otra PC en la misma red: `UncleExtraction.exe` → Jugar con amigos → Crear partida en una (aceptar el firewall) y Unirse con su IP en la otra. |
| Steam | Cuenta de Steamworks, pago de Steam Direct y App ID. |
| IL2CPP | Visual Studio 2022 con «Desarrollo de juegos con C++». |
| Prueba humana | Jugar `Builds/UncleExtraction/UncleExtraction.exe` siguiendo `LEEME.txt`; no bloquea el desarrollo. |

## Antecedentes (resumen; el detalle está en el log de Git y en `docs/DECISIONES_IMPLEMENTACION.md`)

- **M0/M1:** Unity 6000.3.28f1, PhysicsLab y autopiloto.
- **M2 (`2a42a0b`, `c27d209`):** NGO con anfitrión autoritativo.
- **M3 (`f1728e0` → `9f9b0bb`):** camioneta real con cierre, señuelo, ayuda, menús, rescate técnico y red con latencia simulada en PhysicsLab.
- **Sesión 6 (`d8c204b` y siguientes):** Bar_LastRound y LevelLayout.

## Próxima acción exacta

1. **Materiales del kit sobre URP** (`preproduction/uncle_extraction_kit_materiales/claude/INTEGRAR_MATERIALES.md`, `data/texture_import.json`): copiar las 4 texturas Base Color (1254 px) a `Assets/Game/Art/Materials/` con sRGB, mipmaps, Trilinear, máx. 2048, wrap Mirror (madera, pared, tela) y Repeat (piso); crear `MAT_Wood_Boards`, `MAT_Floor_Tiles`, `MAT_Wall_Plaster`, `MAT_Fabric` en URP/Lit con la smoothness de la receta (madera 0,22, piso 0,18; ver el JSON para el resto). Las cajas del constructor tienen UV 0–1 por cara: generar mallas de caja con UV en metros (por tamaño) para usar Tiling 1,1 como en las variantes FBX, sin estirar. No cambiar colliders, masas ni sockets. Verificar con `-ueShot`.
2. Variantes FBX `*_material_variant.fbx` (camioneta, carrito, cascarón): comparar medidas con la camioneta y el carrito del juego (carrito del kit 1,2 × 0,86 m frente a la caja de 0,9 × 1,4 m) y decidir si adaptar el modelo o la caja; registrar en `docs/CATALOGO_RECURSOS.md`.
3. Señales de ataque sobre la malla del tío (hoy esfera amarilla), lenguaje de interacción (contorno de agarre válido, tensión de anclaje, cierre bloqueado explicado).
4. Después: UI del kit, EN/es_UY, controles remapeables, sonido con licencia; Sessions/Relay (bloqueado por Unity Cloud).

## Al cerrar una sesión

Anotar comportamiento implementado, archivos, commit, comandos y pruebas ejecutadas, resultados, bloqueos y siguiente acción.
