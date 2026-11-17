# Estado del proyecto

Última actualización: 2026-11-17, sesión 11 (segunda pasada de pulido). Detalle de sesiones anteriores (sin reescribir, con todos los FAIL): `docs/HISTORIAL.md`.

## Estado vigente

**Build para probar** (local, no versionada): `Builds/UncleExtraction/UncleExtraction.exe`, con `VERSION.txt` (commit, cambios sin commitear, fecha, tipo), `LEEME.txt` (cómo jugar, controles) y `PRUEBA_HUMANA.txt` (guion corto de prueba con planilla). `Builds/UncleExtraction_Dev/` es la de desarrollo (simulador de red). Build de referencia anterior: `2fc2d5d` (respaldo local en `Builds/UncleExtraction_Backup_2fc2d5d/`).

**Qué se puede jugar:** Bar_LastRound solo o en cooperativo de 2 a 4 (internet por Relay con código de sala; red local por IP). Tío articulado de 11 cuerpos con poses de ataque; bar de noche con techo, luces cálidas, neones y calle; camioneta con carrocería propia; carrito con modelo propio; tío con ropa, materiales y gorro de fiesta; vaso señuelo, ayuda para levantarse, extracción con puertas. Interfaz del kit (menú, sala, HUD, pausa, opciones, resultados, errores), EN/es_UY, controles reasignables, opciones guardadas, mando en menús y juego. Sonido sintetizado y subtítulos del tío; sin voz ni música.

**Niveles de evidencia:** PM = PlayMode en el editor; EM = EditMode; AUTO = partida completa con entradas de jugador simuladas; NET = procesos de la build en **localhost** (no LAN ni internet); VIS = auditoría y capturas reales de la build. **Ninguna fila tiene prueba humana.**

### Resultados de la sesión 11: segunda pasada de pulido (por commit y build)

**Build vigente: `e1656f8`** (release `Builds/UncleExtraction/` y desarrollo `Builds/UncleExtraction_Dev/`, ambas con `VERSION.txt` sin cambios sin commitear). **Respaldo funcional anterior:** `Builds/UncleExtraction_Backup_2fc2d5d/` (local). Comparativas reales contra `2fc2d5d` (misma cámara fija y mismos instantes de juego): `docs/capturas/pulido_s11/`.

**Dónde corrió cada cosa:** todo en **una PC** (i7-12700H, RTX 3070 Ti Laptop, monitor 1920×1080 a 360 Hz). Red en procesos de **localhost**, salvo Relay, que pasa por el servidor de Unity en internet desde la misma PC. **Ninguna prueba con dos equipos ni dos redes** (no hay otra PC disponible): queda pendiente.

| Prueba | Código / build | Resultado |
| --- | --- | --- |
| **Carrito trabado según los FPS** (`Cart_CrookedInDoorway`, 4 poses × bar y laboratorio, `UE_TEST_FPS` = 30/60/120/libre, FPS reales registrados) | `2fc2d5d` → `7877651` | Antes: la pose −28° del laboratorio **fallaba a 30, 60 y 120 FPS** y solo pasaba sin límite (~2.200 FPS): sacar el posproceso del laboratorio (`a64cb3c`) solo había devuelto ese ritmo irreal. Causas encontradas y corregidas: **automatización** (el bot movía según el giro interpolado del cuerpo, no según la mirada ordenada), **juego** (el objetivo de la mano salía de la cámara, orientada una vez por cuadro: con dos pasos de física por cuadro apuntaba más allá del giro; ahora sale de la mirada ordenada y la pose de física) y **armado de la prueba** (esperas por tiempo real; ahora por pasos de física). Después (`f387b84`, RTX): **8/8 a 30, 60 y 120 FPS** (reales 27–30, 53–59, 106–118). **Sin límite (~2.000–2.500 FPS): 7/8**; falla `StuckRecoveryTests_Lab` −28° (el carrito no pasa la puerta en 3 intentos). Ese ritmo no ocurre jugando (la build usa sincronización vertical); queda registrado. |
| EditMode | `f387b84` | **30/30**, 1 ignorada. |
| **PlayMode completo** (incluye los 9 intentos completos en las tres escenas) | `f387b84` (RTX) | **89/90, 0 FAIL, 1 omitida** (pie en la camioneta con el tío central: no aplica). Intentos: bar carrito 43,7 s, arrastre 21,1/46,7 s; laboratorio articulado carrito 31,2 s, arrastre 20,1/116,1 s; laboratorio central carrito 27,0 s, arrastre 42,0/73,4 s. Corrida anterior registrada: **87/90 con 2 FAIL** en `480ad7d`, con la notebook a batería sobre la **Intel Iris Xe** (~12 FPS reales): no se cuenta como PASS ni como FAIL del código. |
| **Red completa** (`-Scenario all`, 12 escenarios) | build `f387b84` | **12/12 en la primera tanda válida**. Dos tandas anteriores no cuentan: una con la GPU integrada (4 FAIL/NOT_RUN por ritmo) y otra con la build de release vieja por un archivo bloqueado; además un error de sintaxis mío en `net_smoke.ps1` impidió correr la red (corregido). |
| **Relay** (una PC, por internet) | build `f387b84` | **PASS**: código `T6HPGW`, el invitado entró por Relay, partida ganada en ambos. |
| **Interfaz** (5 aspectos × texto 100/125/150 %, ES y EN; fuera, recortado, encimado, tapado, paneles) | build `f387b84` | **15/15 español, 15/15 inglés**. |
| **Rendimiento** (`perf1080`, 1 proceso) | `f387b84` | RTX 3070 Ti, Alta: 116 fps (sincronización a 360 Hz que cuantiza a 120), CPU 4,2 ms, GPU 2,3 ms; Baja: 118 fps, CPU 3,8 ms, GPU 1,7 ms. La CPU midió 4,2 ms contra 1,5 ms antes del episodio a batería: probablemente el plan de energía de Windows; repetir con la PC en modo alto rendimiento. **Intel Iris Xe** (a batería, `480ad7d`, Alta): **11,7 fps**, CPU 32 ms, GPU 67 ms; por eso se agregó la calidad gráfica Baja (sin medir todavía en la Iris Xe). |
| Señales de ataque e interfaz tras suavizar el brillo del tío | `e1656f8` | PM **16/16**. Captura de juego: el tío se lee mientras se prepara (antes se quemaba casi a blanco). |

**Qué cambió (todo visual o de presentación, salvo el objetivo de la mano, que es una corrección de juego):**
- **Iluminación y legibilidad:** focos embutidos en el techo sobre la barra (las lámparas colgantes tapaban «CERVEZA HELADA» desde el salón y el menú); luz frontal sobre el tío y relleno del lado de la cámara (`CharacterFill`); tapa de la barra de madera oscura (la crema del kit reflejaba los focos); menos bloom; brillo de preparación del tío más suave.
- **Tío** (rig, huesos, colliders y puntos de agarre sin cambios): ropa con trama (musculosa, camisa, bermuda), piel con brillo, zapatos de cuero, ojos y pelo propios; gorro de fiesta torcido. Se quitó la franja violeta de la panza: el color de estado ya no reemplaza un material; la preparación se ve como brillo del color del ataque en todo el cuerpo.
- **Carrito:** modelo propio sobre la geometría funcional (0,90 × 1,40 m, asa a 1,0 m): chasis naranja de seguridad, tablas de madera, barandas tubulares, barra de acero con empuñaduras de goma, ruedas de goma con taza.
- **Materiales:** mapas de normales generados (madera, baldosas, revoque, tela, asfalto, losetas).
- **Menú:** fondo con una captura del bar actual (`Art/UI/Backgrounds/bar_night.png`) y velo más liviano.
- **Calidad gráfica** en Opciones (Alta/Baja; Baja automática con GPU integrada).

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

1. **Vos o testers:** jugar la build `e1656f8` (`Builds/UncleExtraction/UncleExtraction.exe`) con `PRUEBA_HUMANA.txt`; mirar especialmente brillo y contraste del bar, el tío frente a la barra, el carrito y el menú. Anotar en `docs/OBSERVACIONES_PRUEBAS.md`.
2. **Con otra PC:** Relay entre dos equipos en redes distintas (Jugar con amigos → Crear sala por internet / Unirse con código) y LAN por IP.
3. Sin cuentas ni equipos: medir la calidad Baja en la Intel Iris Xe (forzando esa GPU) y el rendimiento con Windows en alto rendimiento; investigar el caso −28° del laboratorio a más de 2.000 FPS solo si vuelve a aparecer a ritmos reales.
