# Estado del proyecto

Última actualización: 2026-11-16, sesión 9. Detalle de sesiones anteriores (sin reescribir, con todos los FAIL): `docs/HISTORIAL.md`.

## Estado vigente

**Build para probar** (local, no versionada): `Builds/UncleExtraction/UncleExtraction.exe`, con `VERSION.txt` (commit, cambios sin commitear, fecha, tipo), `LEEME.txt` (cómo jugar, controles) y `PRUEBA_HUMANA.txt` (guion corto de prueba con planilla). `Builds/UncleExtraction_Dev/` es la de desarrollo (simulador de red). Build de referencia anterior: `378fa91`.

**Qué se puede jugar:** Bar_LastRound solo o en cooperativo de 2 a 4 (internet por Relay con código de sala; red local por IP). Tío articulado de 11 cuerpos con poses de ataque; carrito y camioneta con piezas del kit; vaso señuelo, ayuda para levantarse, extracción con puertas. Interfaz del kit (menú, sala, HUD, pausa, opciones, resultados, errores), EN/es_UY, controles reasignables, opciones guardadas, mando en menús y juego. Sonido sintetizado y subtítulos del tío; sin voz ni música.

**Niveles de evidencia:** PM = PlayMode en el editor; EM = EditMode; AUTO = partida completa con entradas de jugador simuladas; NET = procesos de la build en **localhost** (no LAN ni internet); VIS = auditoría y capturas reales de la build. **Ninguna fila tiene prueba humana.**

### Resultados de la sesión 9 (por commit y build)

Build vigente: **`e8ee30f`** (release, vinculada a Unity Cloud; el código del juego es el de `836a4bd`). Desarrollo: `836a4bd`. PC de prueba: i7-12700H, RTX 3070 Ti Laptop, 16 GB, monitor 1920×1080 a 360 Hz.

| Prueba | Código / build | Resultado |
| --- | --- | --- |
| Proveedor Relay sin red (`RelayProviderTests`: código, motivos de error, sin proyecto → `error.online_unlinked` sin tocar la red, textos EN/es_UY) | `71d11e6` | EM PASS (12 casos). |
| **Relay en una PC** (`net_smoke -Scenario relay`) | build `8c90742` | **BLOCKED**: proyecto sin vincular a Unity Cloud (`cloudProjectId` vacío). |
| **Relay en una PC, tras vincular el proyecto** (Unity Cloud «UnityProject», vinculado con `unity projects link cloud` con tu autorización) | build `e8ee30f` | NET **PASS en la primera corrida**: código de sala `96CM9R`, asignación en `southamerica-east1`, el invitado entró por Relay, partida ganada con el mismo final convergido en ambos, RTT 86–92 ms a través del servidor de Relay. No hizo falta habilitar Relay a mano en el panel. **Es una sola PC**: el tráfico sale a internet y vuelve, pero no prueba dos equipos. **Dos equipos por Relay y dos redes distintas: NOT_RUN** (necesitan otra PC). |
| EditMode completo (incluye persistencia de opciones, idioma y controles tras «reiniciar», valores corruptos) | `8c90742` | EM **31/31**. |
| PM de interfaz, menú con mando virtual, diálogo de anfitrión perdido, flujo y señales de ataque | build `8c90742` | PM **20/20**. Antes, en el código intermedio: 7/8 por un `MissingReferenceException` real (cerrar Opciones al cambiar de escena volvía a entrar en `Show`; corregido) y 7/8 por un choque de puerto con la auditoría corriendo a la vez (entorno; auditoría movida al puerto 7807). |
| **Auditoría de interfaz** (`ui_audit.ps1`: 720p, 1080p, 21:9 = 1920×810 equivalente a 2560×1080, 16:10, 4:3; texto 100/125/150 %; textos fuera, recortados, encimados o tapados) | código de `8c90742` | VIS **15/15 PASS** en la quinta corrida. Corridas anteriores, todas registradas: 1.ª 0/10 (página duplicada al volver de Opciones —error de flujo— y 131 problemas al 150 %); 2.ª 5/15 (todo 100 % limpio; quedaban títulos y filas al 125/150 %); 3.ª 14/15 (Cómo jugar fuera de pantalla en 21:9 al 150 %); 4.ª 10/15 (la verificación nueva «tapado» encontró la barra de los deslizadores sobre su etiqueta al 150 %). Capturas en `.local-setup/shots/s9/audit5/`. Revisado a ojo: sala, HUD, Cómo jugar, resultados y opciones. |
| `Uncle_Escapes_BackInsideThroughTheDoor` (central y articulado), 3 corridas | código de `8c90742` | PM **6/6**. Duración 11,4 s y 13,3 s contra un límite de 15 s: el margen es chico y explica el fallo intermitente de la sesión 8. No se aflojó el límite; la prueba ahora registra la trayectoria si vuelve a fallar. |
| Red completa (`-Scenario all`, 12 escenarios, localhost) | build `8c90742` | NET **12/12** en la primera tanda. |
| **QA ampliada** (`-Scenario qa`) | build `8c90742` | NET 13 PASS: solo Relajado 39,2 s, Normal 21,1 s, Difícil 193,6 s; Relajado y Difícil con 2, 3 y 4 procesos (6/6, mismo final convergido en todos); sesión larga 8 rondas seguidas, 8 victorias, 0 excepciones, memoria de Unity 95→95 MB; rendimiento 1 y 4 procesos. **Hallazgo:** con caída brusca del anfitrión el invitado tardaba **30,1 s** en enterarse (congelado). Corregido en `836a4bd` (tiempo de desconexión 10 s). |
| **Red tras el cambio de transporte** (`-Scenario all` + hostkill, perf1080, perf4) | build `836a4bd` | NET **12/12** en la primera tanda (netsim RTT 234 ms con 3 % de pérdida). **hostkill: detecta en 10,1 s**, vuelve al menú con el diálogo, 0 excepciones. |
| **Rendimiento** (`-uePerf`, esta PC, vsync a 360 Hz) | build `836a4bd` | 1 proceso a 1920×1080: 347 fps (límite de vsync), cuadro p95 2,78 ms, 0 cuadros > 33 ms, CPU principal 1,03 ms, GPU 0,40 ms, memoria de Unity 92→93 MB (reservada 171 MB). 4 procesos en la misma PC (800×450): 344–349 fps, CPU 1,5–1,8 ms, 1–5 cuadros > 33 ms en 30 s. **No representa cuatro equipos ni PC más modestas.** Falta medir en una PC de gama baja. |

Sin prueba humana, sin LAN entre dos PC y sin internet en ninguna fila.

### Pendientes reales (en orden)

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

1. **Vos o testers:** Relay entre dos equipos en redes distintas con la build `e8ee30f` (Crear sala por internet / Unirse con código).
2. **Vos o testers:** prueba con personas con `PRUEBA_HUMANA.txt` (solo y con amigos por red local).
3. Mientras tanto (sin cuentas): medir en una PC de gama baja si hay una disponible; preparar Sessions sobre el proveedor Relay solo si la prueba con amigos pide listar salas o reconectar.
