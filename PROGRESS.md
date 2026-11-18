# Estado del proyecto

Última actualización: 2026-11-17, sesión 14 (cierre de guía, alternar, miembro elegido, frente del tío, prioridad de E, red en procesos separados). Detalle de sesiones anteriores (sin reescribir, con todos los FAIL): `docs/HISTORIAL.md`.

## Estado vigente

**Build para probar** (local, no versionada): `Builds/UncleExtraction/UncleExtraction.exe`, con `VERSION.txt` (commit, cambios sin commitear, fecha, tipo), `LEEME.txt` (cómo jugar, controles) y `PRUEBA_HUMANA.txt` (guion corto de prueba con planilla). `Builds/UncleExtraction_Dev/` es la de desarrollo (simulador de red). Build de referencia anterior: `2fc2d5d` (respaldo local en `Builds/UncleExtraction_Backup_2fc2d5d/`).

**Qué se puede jugar:** Bar_LastRound solo o en cooperativo de 2 a 4 (internet por Relay con código de sala; red local por IP). Tío articulado de 11 cuerpos con poses de ataque; bar de noche con techo, luces cálidas, neones y calle; camioneta con carrocería propia; carrito con modelo propio; tío con ropa, materiales y gorro de fiesta; vaso señuelo, ayuda para levantarse, extracción con puertas. Interfaz del kit (menú, sala, HUD, pausa, opciones, resultados, errores), EN/es_UY, controles reasignables, opciones guardadas, mando en menús y juego. Sonido sintetizado y subtítulos del tío; sin voz ni música.

**Niveles de evidencia:** PM = PlayMode en el editor; EM = EditMode; AUTO = partida completa con entradas de jugador simuladas; NET = procesos de la build en **localhost** (no LAN ni internet); VIS = auditoría y capturas reales de la build. **Ninguna fila tiene prueba humana.**

### Sesión 14: cerrar los sistemas nuevos de la sesión 13 y sus interacciones

**Candidata: commit de cierre de la sesión 14 (ver `VERSION.txt`; código = `8417620`)** (release `Builds/UncleExtraction/`, desarrollo `Builds/UncleExtraction_Dev/`, ZIP `Builds/UncleExtraction_s14.zip`). Referencia anterior conservada: `Builds/UncleExtraction_Candidate_4509f5b/` (sesión 13). Capturas: `docs/capturas/sesion14/`. Matriz con evidencia por nivel y build: `docs/MATRIZ_REQUISITOS.md`.

| | Hallazgo (revisión del dueño) | Reproducción | Corrección | Evidencia |
| --- | --- | --- | --- | --- |
| A | La guía nunca terminaba al ganar (Extract no pasaba a Done; Update se detenía con la ronda terminada; la prueba llamaba a Skip) | Por lectura de código y con la prueba nueva | Ganar = completada; omitir = omitida; perder guarda el paso y el reintento/otra partida siguen; oculta no avanza ni corre tiempos; solo cuenta lo caminado con la guía visible (sin saltos de un cuadro); el traslado acepta arrastrar o carrito | PM `GuideLifecycleTests` 3/3 (el autopiloto gana arrastrando: Move > … > Transport > Extract y queda completada) |
| B | Alternar: con el botón sostenido al volver de un bloqueo, se armaba solo | Código de `4509f5b`: **4/20 combinaciones FAIL** (Alternar × reasignación/foco, ambas manos) | El rearmado (`needRelease`) vale para los dos modos; Esc que cancela una reasignación ya no abre la pausa | PM 20/20 (mano × modo × pausa con Esc / Opciones / reasignación / foco / reinicio) + Q; NET `guesttoggle` 6/6 |
| C | Manotazo/patada sin elegir lado; las poses no correspondían al miembro libre | Por código; prueba con las reglas normales de selección | El anfitrión elige un miembro libre del lado del objetivo (lado anatómico) y lo replica; poses izquierda/derecha; si le sujetan ese miembro durante la preparación el golpe se corta («lo frenaron») | PM 6 casos de sujeción con `TryNaturalAttackForTest` (mismas reglas que la decisión normal, no `ForceAttack`) |

**Frente del tío (revisión pedida):** las poses de ataque se seguían generando hacia el frente viejo (golpe hacia la nuca); el arco del piso seguía el giro del cuerpo y no la dirección fijada; el impacto se medía desde el pecho (la patada lo echa atrás: el jugador quedaba a 28° fuera de un arco de 40°); las propias piernas y brazos del tío «tapaban» su golpe. Corregido: poses en el marco de la cara, arco e impacto con la misma dirección (replicada a los clientes) y el mismo punto, y sus partes ya no bloquean. PM: el miembro elegido avanza 0,33–0,69 m hacia la zona anunciada (el otro ≤ 0,11 m) y el jugador dentro del arco recibe el golpe; arco = impacto con el jugador a 0/90/180/270°.

**Prioridad de E:** regla pura `ChooseInteract` (compañero caído > mano anclada > camioneta > carrito), objetivo fijo mientras se mantiene E, línea de visión, el progreso se reinicia al cambiar de objetivo. EM con los cuatro a la vez; PM en el bar con la mano anclada.

**Desconexión (política):** el tiempo extra del grupo más chico llega en el acto (ya no se pierde por reloj durante la gracia), resistencia y ataques cambian una vez a los 10 s, aviso localizado para todos (antes solo lo veía el anfitrión, sin traducir). La gracia **no** es una reconexión: la admisión sigue rechazando entrar con la ronda empezada (limitación documentada del alcance inicial).

**Presentación:** el subtítulo sube si iba a tapar la cara del tío; manos que bajan abiertas al caer y tiemblan con esfuerzo; la guía se reconstruye al cambiar el tamaño de texto en partida.

**Experiencia de juego que cambia:** el tío se da vuelta y te mira cuando te va a pegar, con el brazo o la pierna que tiene libre, y el golpe va adonde marca el piso; sujetarle el brazo que levantó lo frena. Alternar no agarra solo al volver de un menú. La guía te acompaña hasta ganar (con o sin carrito). Con latencia, Alternar espera la confirmación del anfitrión en vez de desarmarse. Si alguien se va al final, el reloj ayuda en el acto.

#### Verificación (cada resultado con su build)

| Prueba | Código / build | Resultado |
| --- | --- | --- |
| EditMode | `09433df` (código) | **64/64**, 1 ignorada |
| PlayMode completa | `8417620` (código) | **160/162**: 1 omitida, 1 FAIL `SoloAttemptTests_Articulated.SoloAttempt_Cart_Normal` (bot de carrito: el tío se suelta en la puerta, vuelve al banco y el bot abandona). Aislado: **3/3 PASS** (56, 38, 26 s). En tandas completas falló 2 de 3 veces en esta sesión: intermitencia del bot, posiblemente dependiente del orden; abierta. `lab_fail_m28` (sin límite de FPS) pasó esta vez |
| PlayMode dirigidas (ataques, entrada real, guía, interacción, interfaz) | `8417620` | AttackSignal 51/51; entrada/guía/interfaz/interacción 41/41 |
| Hallazgo B reproducido sobre el código anterior | `GrabIntent` de `4509f5b` | 16/20 (4 FAIL, como se describe arriba) |
| Red: escenarios nuevos | builds `09433df` → `cd01c30` → `8417620` | `guesttoggle` PASS 6/6 (confirmación 61 ms); `guesttoggle_sim` **FAIL 3/6** en `09433df` (la ventana de 0,6 s desarmaba antes de la confirmación a 653 ms) → corregido → PASS 6/6; `marks` PASS 4/4; `grace` PASS (gracia 10 s, sigue jugando, termina por reloj después del ajuste); `graceend` PASS (baja con 6,9 s restantes, +25 s ya, ajuste a los 10 s) — las primeras corridas de `grace`/`graceend` fueron NOT_RUN por el armado del escenario (el bot ganaba antes / la desconexión llegaba tarde), corregido |
| Red: regresión completa (17 escenarios, localhost) | build `8417620` | **17/17 PASS**: coop, repeat, lose, netsim, race, hostleave, clientdrop, loaddrop, closedrop, party3, party4, latejoin, guesttoggle (confirmación 44 ms), guesttoggle_sim (625 ms), marks, grace, graceend |
| Auditoría de interfaz con guía, marcador y cambio de tamaño en partida | build `8417620` | **ES 15/15, EN 15/15** |
| Intento con carrito del laboratorio articulado | `58ceaa7` y `8417620` (código) | Tandas: 2 FAIL de 3 (mismo patrón: soltado en la puerta, vuelve al banco, el bot abandona). Aislado: 4/4 PASS |
| Capturas de la build | `8417620` | `docs/capturas/sesion14/`: patada (pierna elegida, cara enojada, subtítulo arriba), caída (manos abajo), arrastre (puños agarrando) |

### Pendientes reales (en orden)

0. **Terminaciones visuales que siguen pendientes:** la malla del tío es la del kit (facetada) con cejas y boca propias; camioneta, edificios y carteles son geometría simple; texturas sin mapas de rugosidad; banco a batería. **Nada de esto lo revisó todavía una persona.**

1. **Prueba con personas** (`docs/PRUEBA_HUMANA.md`): objetivo sin explicación, controles, ataques, carrito y atascos, camioneta, perder/reintentar, sala con amigos. Registrar en `docs/OBSERVACIONES_PRUEBAS.md`. No bloquea el desarrollo.
2. **Internet:** Relay ya funciona en una PC. Falta verificar dos equipos por Relay en la misma red y dos equipos en **redes distintas** (por ejemplo, uno con datos del celular), anotando el código, la región y si se nota la latencia.
3. **LAN real** con dos PC (conexión directa).
4. **Revisión humana de la interfaz:** legibilidad y jerarquía a 720p, 21:9 y texto al 150 % (la auditoría automática solo detecta textos fuera de pantalla, cortados o encimados); escribir el código de sala con mando (no hay teclado en pantalla).
5. **Audio de producción** (voz del tío, música, efectos grabados): necesita fuentes con licencia o actor; no bloquea la primera prueba con amigos.
6. Fallos intermitentes del autopiloto (ver abajo): revisar solo cuando cambie su código.
7. Steam e IL2CPP (bloqueados).

### Fallos intermitentes conocidos (se mantienen visibles)

- **Bot del modo carrito:** falló en 2 de 4 regresiones completas de la sesión 8 y 2 de 3 tandas completas en la sesión 14 (laboratorio articulado: el tío se suelta en la puerta, vuelve al banco y el bot no lo vuelve a buscar; aislado 4/4 PASS). Es el punto más inestable de las pruebas automáticas, no una regla del juego.
- **`StuckRecoveryTests_Lab lab_fail_m28`** (carrito −28° en el laboratorio) falla solo **sin límite de FPS** (~2000 fps en el editor por lotes); pasa a 30, 60 y 120 fps (`UE_TEST_FPS`). Prioridad baja; si fallara a 30/60/120 fps se trata como regresión.
- **`Uncle_Escapes_BackInsideThroughTheDoor` (articulado):** 1 de 3 corridas en la sesión 8 terminó con el tío en el umbral (z = −1,50 con la condición > −1,5) a los 15 s. En la sesión 9 pasó 6/6, pero con 11–13 s de un límite de 15 s; la prueba ahora registra la trayectoria.

## Bloqueado por cuentas, equipos o participantes

| Bloqueo | Paso mínimo |
| --- | --- |
| Internet entre equipos | Otra PC (idealmente en otra red): Jugar con amigos → Crear sala por internet en una; Unirse con código en la otra. Proyecto de Unity Cloud ya vinculado (`UnityProject`). |
| LAN | Otra PC en la misma red: `UncleExtraction.exe` → Jugar con amigos → Red local (IP) → Crear partida (aceptar el firewall) en una; Unirse por IP en la otra. |
| Prueba con personas | Jugar con `PRUEBA_HUMANA.txt`. No bloquea el desarrollo. |
| Steam | Cuenta de Steamworks, pago de Steam Direct y App ID. |
| IL2CPP | Visual Studio 2022 con «Desarrollo de juegos con C++». |

## Próxima acción exacta

1. **Vos (20 min):** jugar la candidata de la sesión 14 con `REVISION_CANDIDATA.txt` (junto al ejecutable): ganar con la guía sin omitirla; Alternar + pausa con el botón sostenido; sujetar un brazo del tío y mirar con cuál pega; E sobre la mano aferrada. Anotar en `docs/OBSERVACIONES_PRUEBAS.md`.
2. **Con otra PC:** Relay entre dos equipos en redes distintas y LAN por IP: marcador del otro, nombres/colores, cerrar un juego a mitad de ronda (aviso y ajuste).
3. **Desarrollo siguiente (sin depender de nadie):** compañero caído real en 2 procesos para la prioridad de E (L); captura con 2 procesos de la etiqueta «CAÍDO»; malla del tío con más detalle; balance de duración de `coop`/`netsim` tras el giro del tío (más lentos que antes de la corrección, siguen ganando).
