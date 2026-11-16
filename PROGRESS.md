# Estado del proyecto

Última actualización: 2026-11-15 (sesión 6: escenario principal `Bar_LastRound`, `LevelLayout`, red en el escenario principal, fuga).

**Builds de referencia** (locales, no versionadas), generadas desde el commit de esta actualización:
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
| `net_smoke.ps1` con códigos estrictos | 0 solo si todos los escenarios pedidos dieron PASS; 1 si hubo FAIL; 2 si algo quedó BLOCKED o NOT_RUN. Cierra procesos previos del juego y marca BLOCKED si el puerto está ocupado. |

Los fallos de esta etapa ya están corregidos y se verificaron después:
- **Carrito en la esquina (geometría):** quedaba encajado contra las paredes; ahora está en el salón.
- **Bot empujando el carrito (ruta):** recalculaba los objetivos con el carrito en movimiento y lo empujaba contra la pared.
- **`[Timeout]` (prueba):** no se heredaba en la subclase.
- **Script de red:** colisión de variables `$H`/`$h`, array desenvuelto y procesos zombis que ocupaban el puerto 7777.

## 2. Implementado con observaciones

- **Freno del carrito y nueva estrategia del modo carrito del bot** (llevar el carrito vacío a un punto junto a la ruta y cargar al tío al pasar): carrito con correa PASS en PhysicsLab (27,3 s) y Bar_LastRound (27,6 s), con el tío intentando soltarse dos veces. Los resultados de la batería completa con estos cambios están en el mensaje del commit.
- **Batería completa con este commit** (PM, `126_playmode.xml`): 19/20.
  - **Bar_LastRound:** arrastre Normal 23,9 s, arrastre Difícil 39,1 s, carrito 27,5 s, todos PASS.
  - **PhysicsLab:** carrito 38,4 s PASS, arrastre Normal 55,1 s PASS, **arrastre Difícil FAIL**. Es un caso conocido de la ruta del bot: el tío quedó caído en el umbral, la hoja de la puerta tapa la línea de visión a sus agarres y el bot dejó de moverse al descartar todos los puntos. Se corrigió que se quede quieto (limpia el descarte), sin volver a correr la batería. Un jugador empujaría la puerta o lo tomaría desde adentro.
  - **EditMode** 9/9. Builds release y dev OK.
- **Balance:** el carrito en solitario se resuelve en ~20–30 s con 0–1 ataques porque, asegurado, el tío solo intenta soltarse. Probablemente sea demasiado fácil; revisarlo en el playtest antes de tocar valores.
- **Carrito en solitario:** a veces el primer intento de carga choca con una pared y el bot reubica el carrito. Funciona, pero un jugador podría frustrarse; revisarlo en el playtest.
- **Visuales del kit:** la camioneta, el carrito y el tío siguen siendo primitivas. Ver `docs/CATALOGO_RECURSOS.md`.
- **Driver NVIDIA:** un crash en un arranque simultáneo en sesiones anteriores. El script escalona los arranques y reintenta; no volvió a ocurrir.

## 3. Pendiente de implementar (en orden)

1. Ragdoll reducido del tío (11 cuerpos) conservando la versión estable hasta verificar agarres, ataques, carrito, extracción y replicación.
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

1. Registrar el resultado de los intentos completos con la fuga nueva (PhysicsLab y Bar_LastRound). Si alguno falla, diagnosticar si es por el juego (la fuga) o por la ruta.
2. Ragdoll reducido: prototipo en PhysicsLab detrás de una opción (`NpcBody` actual como respaldo). Verificar estabilidad (watchdog), agarres en los 11 cuerpos, carrito, extracción y replicación de la pose reducida antes de pasarlo a Bar_LastRound.
3. URP y presentación (pendientes 2 y 3).

## Al cerrar una sesión

Anotar comportamiento implementado, archivos, commit, comandos y pruebas ejecutadas, resultados, bloqueos y siguiente acción.
