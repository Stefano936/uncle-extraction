# Estado del proyecto

Última actualización: 2026-11-17, sesión 13 (controles, guía, cooperación, justicia del tío, presentación). Detalle de sesiones anteriores (sin reescribir, con todos los FAIL): `docs/HISTORIAL.md`.

## Estado vigente

**Build para probar** (local, no versionada): `Builds/UncleExtraction/UncleExtraction.exe`, con `VERSION.txt` (commit, cambios sin commitear, fecha, tipo), `LEEME.txt` (cómo jugar, controles) y `PRUEBA_HUMANA.txt` (guion corto de prueba con planilla). `Builds/UncleExtraction_Dev/` es la de desarrollo (simulador de red). Build de referencia anterior: `2fc2d5d` (respaldo local en `Builds/UncleExtraction_Backup_2fc2d5d/`).

**Qué se puede jugar:** Bar_LastRound solo o en cooperativo de 2 a 4 (internet por Relay con código de sala; red local por IP). Tío articulado de 11 cuerpos con poses de ataque; bar de noche con techo, luces cálidas, neones y calle; camioneta con carrocería propia; carrito con modelo propio; tío con ropa, materiales y gorro de fiesta; vaso señuelo, ayuda para levantarse, extracción con puertas. Interfaz del kit (menú, sala, HUD, pausa, opciones, resultados, errores), EN/es_UY, controles reasignables, opciones guardadas, mando en menús y juego. Sonido sintetizado y subtítulos del tío; sin voz ni música.

**Niveles de evidencia:** PM = PlayMode en el editor; EM = EditMode; AUTO = partida completa con entradas de jugador simuladas; NET = procesos de la build en **localhost** (no LAN ni internet); VIS = auditoría y capturas reales de la build. **Ninguna fila tiene prueba humana.**

### Sesión 13: controles, primera partida guiada, cooperación y justicia del tío

**Build de referencia: `{BUILD}`** (release `Builds/UncleExtraction/`, desarrollo `Builds/UncleExtraction_Dev/`, ZIP `{ZIP}`). Respaldo anterior: `Builds/UncleExtraction_Candidate_69e9c6f/`. Matriz de requisitos con evidencia por nivel: `docs/MATRIZ_REQUISITOS.md`.

**Hallazgos A–M (revisión del dueño), todos reproducidos antes de corregir salvo F (revisión de código):**

| | Hallazgo | Corrección | Evidencia |
| --- | --- | --- | --- |
| A | Flechas no movían | `LabInput.RealMove`: asignadas + flechas, diagonal normalizada, sin sumar velocidad | E (`RealInputTests`, teclado simulado) |
| B | Movimiento no reasignable | `InputBindings` con `MoveKey` | E |
| C | Sin agarre alternado | `GrabIntent` (mantener / alternar, local y guardado) | E |
| D | Q soltaba y el clic sostenido volvía a agarrar | rearmado al soltar el botón | E |
| E | `Pressed` ignoraba menús y campos de texto | `PlayerControls.Pressed` con `BlockingInput` (+ escritura) | E |
| F | Cualquiera reiniciaba con R | R solo diagnóstico en el anfitrión; el invitado pide al terminar | C, L (`repeat`, `lose`) |
| G | Reasignar guardaba al instante | borrador con Aplicar / Descartar | E |
| H | Tamaño de texto no cambiaba en partida | reconstrucción del HUD y paneles | E |
| I | Preferencias dañadas | `GameSettings.Validate`, `InputBindings.Valid` | E |
| J | El tío pegaba con un brazo sujeto | ataques según extremidades libres | E (`FairnessRulesTests`) |
| K | Una desconexión recalculaba la ronda al instante | 10 s de gracia, tiempo conservado, mínimo 60 s | E |
| L | Laboratorios en el menú | solo con `-ueDebug` / build de desarrollo | C |
| M | README desactualizado | reescrito | — |

**Nuevo:** primeros pasos (9 indicaciones, omitibles y repetibles desde la pausa, locales en cooperativo); marcador contextual (clic central) y nombres de compañeros con aviso de caído; validación de intenciones del invitado; descarte de respuestas tardías de Relay tras cancelar; manos con forma (palma, dedos que se cierran, puño con color de estado); expresión del tío (cejas y boca por estado) y **corrección de orientación: la cara del kit miraba hacia atrás de la raíz, así que el tío preparaba los golpes de espaldas al jugador y escapaba caminando de espaldas** (`NpcBody.FaceSign`; captura `docs/capturas/sesion13/tio_expresiones.jpg`); volúmenes de efectos y de avisos; suavizado visual independiente de los FPS; rescates técnicos por ronda en el registro.

#### Verificación (cada resultado con su build)

| Prueba | Código / build | Resultado |
| --- | --- | --- |
| EditMode | `40d17af` | **58/58**, 1 ignorada (+21 nuevas: extremidades, gracia, intenciones, marcador, suavizado, volúmenes) |
| PlayMode completa | `aac5b90` (código) | **97/99**: 1 omitida, 1 FAIL conocido `lab_fail_m28` (carrito torcido del laboratorio sin límite de FPS; pasa a 30/60/120 fps) |
| PlayMode completa con la regla de extremidades | `83e55c7` (código) | 97/98, 1 omitida (esta vez también pasó `lab_fail_m28`) |
| Señales de ataque + orientación de la cara | `aac5b90` | **15/15**; la cara queda a 4–9° del jugador al final de la preparación |
| Entrada real, interfaz, flujo, marcador | `40d17af` | **29/29** |
| Red en localhost (12 escenarios) | build `07dbc8f` | 11 PASS + `race` FAIL (rechazos=0); repetido ×3: 3/3 PASS → intermitencia de tiempos del escenario |
| Red en localhost (12 escenarios) | build `83e55c7` | **12/12 PASS** |
| Red en localhost tras la corrección de orientación | build `{NETBUILD}` | {NETRES} |
| Auditoría de interfaz | build `07dbc8f` | EN 15/15; ES 11/15 (texto «Reducir inclinación de cámara» recortado en Opciones → acortado) |
| Auditoría de interfaz (Opciones con volúmenes y Marcar) | build `{UIBUILD}` | {UIRES} |
| Capturas de la build | `aac5b90` | cara del tío normal / enojado (gira hacia el jugador) / recuperándose; manos nuevas (`docs/capturas/sesion13/`) |

**No verificado en esta sesión:** LAN real, dos equipos por internet (Relay solo desde una PC en sesiones anteriores), revisión humana. La gracia de desconexión no se ejercita en `clientdrop` (el anfitrión termina enseguida): evidencia E, no L. En `latejoin` el bot del anfitrión quedó trabado con la cabeza del tío afuera de la camioneta (no es criterio del escenario; inestabilidad conocida del autopiloto).


### Pendientes reales (en orden)

0. **Terminaciones visuales que siguen pendientes** (sesión 11): la malla del tío sigue siendo la de prueba del kit (facetada; sin cara expresiva ni manos con dedos); camioneta, edificios y carteles son geometría simple; texturas sin mapas de rugosidad; falta medir la calidad Baja en la Intel Iris Xe y repetir el rendimiento con Windows en alto rendimiento. **Nada de esto lo revisó todavía una persona.**

1. **Prueba con personas** (`docs/PRUEBA_HUMANA.md`): objetivo sin explicación, controles, ataques, carrito y atascos, camioneta, perder/reintentar, sala con amigos. Registrar en `docs/OBSERVACIONES_PRUEBAS.md`. No bloquea el desarrollo.
2. **Internet:** Relay ya funciona en una PC. Falta verificar dos equipos por Relay en la misma red y dos equipos en **redes distintas** (por ejemplo, uno con datos del celular), anotando el código, la región y si se nota la latencia.
3. **LAN real** con dos PC (conexión directa).
4. **Revisión humana de la interfaz:** legibilidad y jerarquía a 720p, 21:9 y texto al 150 % (la auditoría automática solo detecta textos fuera de pantalla, cortados o encimados); escribir el código de sala con mando (no hay teclado en pantalla).
5. **Audio de producción** (voz del tío, música, efectos grabados): necesita fuentes con licencia o actor; no bloquea la primera prueba con amigos.
6. Fallos intermitentes del autopiloto (ver abajo): revisar solo cuando cambie su código.
7. Steam e IL2CPP (bloqueados).

### Fallos intermitentes conocidos (se mantienen visibles)

- **Bot del modo carrito:** falló en 2 de 4 regresiones completas de la sesión 8 (asa contra la pared, corregido; no llegó a cargar en el laboratorio articulado, abierto). Es el punto más inestable de las pruebas automáticas, no una regla del juego.
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

1. **Vos:** revisar la candidata `69e9c6f` con `REVISION_CANDIDATA.txt` (iluminación, controles, carrito, extracción); si podés, también a batería.
2. **Con otra PC:** el punto 5 del guion (Relay entre dos equipos en redes distintas) y LAN por IP.
3. Sin equipos: repetir el banco a batería (`perf_bench.ps1 -Device 1 -Quality low` y `high`) para confirmar la hipótesis de alimentación y ajustar Baja si hace falta.
