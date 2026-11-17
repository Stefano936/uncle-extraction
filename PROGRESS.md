# Estado del proyecto

Última actualización: 2026-11-17, sesión 12 (versión candidata). Detalle de sesiones anteriores (sin reescribir, con todos los FAIL): `docs/HISTORIAL.md`.

## Estado vigente

**Build para probar** (local, no versionada): `Builds/UncleExtraction/UncleExtraction.exe`, con `VERSION.txt` (commit, cambios sin commitear, fecha, tipo), `LEEME.txt` (cómo jugar, controles) y `PRUEBA_HUMANA.txt` (guion corto de prueba con planilla). `Builds/UncleExtraction_Dev/` es la de desarrollo (simulador de red). Build de referencia anterior: `2fc2d5d` (respaldo local en `Builds/UncleExtraction_Backup_2fc2d5d/`).

**Qué se puede jugar:** Bar_LastRound solo o en cooperativo de 2 a 4 (internet por Relay con código de sala; red local por IP). Tío articulado de 11 cuerpos con poses de ataque; bar de noche con techo, luces cálidas, neones y calle; camioneta con carrocería propia; carrito con modelo propio; tío con ropa, materiales y gorro de fiesta; vaso señuelo, ayuda para levantarse, extracción con puertas. Interfaz del kit (menú, sala, HUD, pausa, opciones, resultados, errores), EN/es_UY, controles reasignables, opciones guardadas, mando en menús y juego. Sonido sintetizado y subtítulos del tío; sin voz ni música.

**Niveles de evidencia:** PM = PlayMode en el editor; EM = EditMode; AUTO = partida completa con entradas de jugador simuladas; NET = procesos de la build en **localhost** (no LAN ni internet); VIS = auditoría y capturas reales de la build. **Ninguna fila tiene prueba humana.**

### Versión candidata (sesión 12): rendimiento, calidad Baja y verificación

**Build candidata: `69e9c6f`** (release `Builds/UncleExtraction/`, desarrollo `Builds/UncleExtraction_Dev/`; `VERSION.txt` sin cambios sin commitear). Respaldo: `Builds/UncleExtraction_Backup_2fc2d5d/`. Guion de revisión humana: `REVISION_CANDIDATA.txt` junto al ejecutable (`docs/REVISION_CANDIDATA.md`). Capturas actuales: `docs/capturas/candidata_69e9c6f/`.

**Todo en una PC** (i7-12700H, RTX 3070 Ti Laptop + Intel Iris Xe, pantalla 1920×1080 a 360 Hz, plan de energía «Equilibrado» —el único instalado—, enchufada). Red: procesos en localhost; Relay sale a internet desde la misma PC. **Sin pruebas entre dos equipos.**

#### Rendimiento comparable

Banco de vistas fijas (`-ueBench`, `tools/windows/perf_bench.ps1`): Bar_LastRound, tío quieto, 6 vistas × 6 s, 8 s de calentamiento **no medidos**, 1920×1080 en ventana sin bordes, mismos procesos abiertos (Chrome, OneDrive, Overwolf). `2fc2d5d` se compiló aparte con **el mismo `Bench.cs`** agregado (`Builds/UncleExtraction_Bench_2fc2d5d/`). «normal» = como se juega (sincronización vertical, sin límite propio); «sin vsync» = solo diagnóstico.

| Build | GPU | Calidad | Resolución | Modo | FPS | Cuadro p95 / p99 (ms) | CPU (ms) | GPU (ms) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `2fc2d5d` (2 corridas) | RTX 3070 Ti | Alta | 1080p | normal | 119,6 / 120,1 | 8,35 / 11,11 | 3,92 / 3,83 | 2,06 / 2,15 |
| `2fc2d5d` (2 corridas) | RTX 3070 Ti | Alta | 1080p | sin vsync | 160,4 / 159,4 | 7,8 / 9,0 | 3,00 | 2,55 |
| `a056b8a` (2 corridas) | RTX 3070 Ti | Alta | 1080p | normal | 119,6 / 120,2 | 8,36 / 11,11 | 3,71 / 3,84 | 2,09 / 2,03 |
| `a056b8a` (2 corridas) | RTX 3070 Ti | Alta | 1080p | sin vsync | 159,6 / 160,3 | 7,7 / 9,1 | 2,94 | 2,50 |
| **`69e9c6f`** | RTX 3070 Ti | Alta | 1080p | normal | **120,2** | 8,37 / 11,11 | 3,73 | 2,06 |
| **`69e9c6f`** | RTX 3070 Ti | Alta | 1080p | sin vsync | 160,7 | 7,49 / 8,93 | 2,92 | 2,50 |
| `a056b8a` | Iris Xe (forzada) | Alta | 1080p | normal | 125,4 | 11,11 / 11,11 | 2,79 | 7,21 |
| `a056b8a` | Iris Xe | Alta | 1080p | sin vsync | 125,6 | 9,25 / 9,44 | 2,49 | 7,16 |
| `a056b8a` | Iris Xe | **Baja** | 1080p | normal | 204,8 | 8,33 / 8,33 | 2,43 | 3,67 |
| `a056b8a` | Iris Xe | Baja | 1080p | sin vsync | 225,2 | 5,18 / 5,49 | 2,10 | 3,68 |
| **`69e9c6f`** | Iris Xe | Baja | 1080p | normal | **204,6** | 8,33 / 8,33 | 2,39 | 3,68 |
| `a056b8a` | Iris Xe | Baja | **720p** | normal | 344,6 | 2,78 / 5,56 | 1,96 | 1,94 |
| `a056b8a` | Iris Xe | Baja | 720p | sin vsync | 428,4 | 2,69 / 2,82 | 1,67 | 1,91 |

**Qué explica cada diferencia:**
- **Costo de los cambios `2fc2d5d` → `69e9c6f`: nulo dentro del ruido** (±0,5 fps; ±0,15 ms de CPU) en las mismas condiciones. No hay regresión del juego que corregir.
- **Sincronización vertical:** con el monitor a 360 Hz, un cuadro que tarda más de 2,78 ms espera al siguiente refresco útil; la build normal queda en 120 fps (cada 3 refrescos) mientras la misma escena sin vsync da ~160.
- **Condiciones del equipo (hipótesis, sin demostrar):** en la sesión 10 una partida a 1080p midió 338 fps con 1,0 ms de CPU; hoy el mismo código mide ~3–4 ms de CPU. Las mediciones no son iguales (partida con autopiloto contra banco de vistas fijas) y el estado de energía, la carga de fondo o el paso de la imagen de la RTX por la integrada (Optimus) pueden explicarlo; no se probó cuál. Indicio a favor de Optimus: la Iris Xe, que maneja la pantalla, da más fps en Baja (205) que la RTX en Alta (120) con menos tiempo de GPU por cuadro en la RTX.
- **Batería (hipótesis fuerte, comparación no controlada):** la Iris Xe midió **11,7 fps** en partida a batería y en Alta (`480ad7d`, sesión 11) contra **125 fps** enchufada en el banco. Falta repetir el banco a batería (requiere desenchufar).

#### Calidad Baja en la integrada

- Baja reduce el tiempo de GPU a la mitad en la Iris Xe (7,2 → 3,7 ms a 1080p); a 720p, 1,9 ms. No hizo falta endurecer el perfil con estas mediciones; se revisa cuando haya una medición a batería.
- **Juego con Baja en la Iris Xe** (editor forzado a la Iris, `UE_QUALITY=low`): atascos del carrito, señales de ataque e intentos completos del bar **28/28, 0 FAIL, 1 omitida**. En el editor no se aplican escala de render ni MSAA (no se modifica el asset del proyecto); sí luces sin sombra y sin bloom.
- **Capturas en la build con Baja en la Iris** (`calidad_alta_vs_baja.jpg`): aviso del HUD, arco del piso, brillo del tío, anillo de la mira y estado de las manos se leen igual que en Alta.
- **Detección automática** por tipo de dispositivo y no por fabricante: Intel Iris/UHD/HD = integrada, Intel Arc = dedicada; Radeon «Graphics»/Vega = integrada, RX/Pro = dedicada; NVIDIA = dedicada; render por software = Baja. EditMode: 14 casos de GPU. **La elección guardada en Opciones manda siempre** (probado).

#### Verificación (cada resultado con su build)

| Prueba | Build | Resultado |
| --- | --- | --- |
| Guardia de tandas (`Guard.psm1`): sintaxis de scripts, `VERSION.txt` al día con el código, una tanda a la vez, cada script cierra solo sus procesos | — | En uso en `net_smoke`, `ui_audit`, `perf_bench`; la tanda final pasó el preflight («builds al día con el código»). |
| EditMode | `abc8bb2` (código) | **44/44**, 1 ignorada. |
| PlayMode afectadas en la Iris Xe con Baja (atascos, ataques, intentos del bar) | `abc8bb2` | **28/28**, 1 omitida. |
| PlayMode señales de ataque e interfaz | `69e9c6f` | **16/16**. |
| **Cooperativo** (coop, 4 procesos) y **Relay** | build `69e9c6f` | **PASS, PASS, PASS** (Relay código `MQRGGG`). |
| Fallos en la integrada a batería (sesión 11, `480ad7d`, ~12 fps reales) | `480ad7d` | Se conservan como evidencia de **rendimiento insuficiente**, no del código: PM 2 FAIL (intento con carrito del tío articulado sin terminar; carrito −28° del laboratorio) y red 3 FAIL + 1 NOT_RUN (`race`, `hostleave`, `latejoin`: ventanas de tiempo de los scripts —automatización—; `clientdrop`: el bot no llegó a agarrar). Los mismos casos pasan en la RTX y en la Iris enchufada. Relay BLOCKED por `error.service` en ese momento (servicio/red). |

**Correcciones de esta etapa:** guardia de tandas; banco reproducible; detección de GPU por tipo; rótulo 3D del ataque de tamaño constante y oculto a menos de 2,8 m (de cerca tapaba el reloj, visto en la Iris y en la RTX); capturas del ataque actualizadas tras suavizar el brillo.

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
