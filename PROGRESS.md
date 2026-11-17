# Estado del proyecto

Última actualización: 2026-11-16, sesión 10 (pulido visual). Detalle de sesiones anteriores (sin reescribir, con todos los FAIL): `docs/HISTORIAL.md`.

## Estado vigente

**Build para probar** (local, no versionada): `Builds/UncleExtraction/UncleExtraction.exe`, con `VERSION.txt` (commit, cambios sin commitear, fecha, tipo), `LEEME.txt` (cómo jugar, controles) y `PRUEBA_HUMANA.txt` (guion corto de prueba con planilla). `Builds/UncleExtraction_Dev/` es la de desarrollo (simulador de red). Build de referencia anterior: `e8ee30f` (respaldo local en `Builds/UncleExtraction_Backup_e8ee30f/`).

**Qué se puede jugar:** Bar_LastRound solo o en cooperativo de 2 a 4 (internet por Relay con código de sala; red local por IP). Tío articulado de 11 cuerpos con poses de ataque; bar de noche con techo, luces cálidas, neones y calle; camioneta con carrocería propia; carrito con piezas del kit; vaso señuelo, ayuda para levantarse, extracción con puertas. Interfaz del kit (menú, sala, HUD, pausa, opciones, resultados, errores), EN/es_UY, controles reasignables, opciones guardadas, mando en menús y juego. Sonido sintetizado y subtítulos del tío; sin voz ni música.

**Niveles de evidencia:** PM = PlayMode en el editor; EM = EditMode; AUTO = partida completa con entradas de jugador simuladas; NET = procesos de la build en **localhost** (no LAN ni internet); VIS = auditoría y capturas reales de la build. **Ninguna fila tiene prueba humana.**

### Resultados de la sesión 10: pulido visual (por commit y build)

Código final: **`1e23c82`**; la build entregada sale del commit de documentación siguiente (mismo código; ver `VERSION.txt`). Respaldo de la anterior: `Builds/UncleExtraction_Backup_e8ee30f/` (local). Capturas comparativas reales (misma cámara, `-ueCamTour`): `docs/capturas/pulido_s10/`.

| Prueba | Código / build | Resultado |
| --- | --- | --- |
| Revisión visual de partida (13 vistas fijas: salón, barra, tío, karaoke, carrito, puerta por dentro y por fuera, vereda, camioneta, interior de la camioneta, esquina, techo, general) | `e8ee30f` (antes) y varias iteraciones | VIS. Encontrado: bar sin techo con cielo de día; baldosas del bar también afuera; **banquetas, mesas, mostrador y puerta del kit acostados** (el kit viene con Z arriba; la puerta era una placa en el piso); camioneta de placas planas sin techo y piso verde lima; paredes lisas sin terminaciones. Durante el pulido: carteles 3D que se veían a través de muros y techo (sombreador propio con prueba de profundidad), dos carteles espejados, una lámpara tapando el cartel de la barra, manchas de luz en el techo. Todo corregido y vuelto a capturar. |
| PM afectadas (geometría, señales de ataque, interfaz, menú, flujo, atascos, intentos del bar) | `315e7b2` | PM **39/41 + 1 omitida, 1 FAIL**: `StuckRecoveryTests_Lab` pose `lab_fail_m28` (laboratorio, que este pulido no viste). Repetida aislada: **FAIL 3/3**. Causa encontrada: el posproceso activo en la cámara del jugador en todas las escenas cambia el ritmo de cuadros y la maniobra guionada (empujar, alinear, tirar) depende de él. Con posproceso solo en el bar (`a64cb3c`): **PASS 3/3**; atascos + ataques en el bar + geometría **21/22 + 1 omitida, 0 FAIL**. Las reglas del juego no cambiaron; la prueba queda marcada como sensible al ritmo de cuadros. |
| Red tras el pulido (`coop`, `party4`) | builds `315e7b2` y `a64cb3c` | NET PASS (coop 21,6 s y 20,7 s; party4 34,5 s; mismo final convergido). Solo localhost. |
| **Rendimiento con iluminación y posproceso** (`perf1080`, 1 proceso, 1920×1080, vsync a 360 Hz) | `836a4bd` → `a64cb3c` | Antes: 347 fps, cuadro p99 2,78 ms, CPU 1,03 ms, GPU 0,40 ms, memoria de Unity 93 MB. Después: **338 fps**, p99 5,56 ms, **CPU 1,54 ms, GPU 0,89 ms**, 104 MB (reservada 189 MB), 0 cuadros > 33 ms. 4 procesos en una PC (`315e7b2`): 269–302 fps, CPU 2,9–3,6 ms. Costo real: +0,5 ms de CPU y +0,5 ms de GPU por cuadro en esta PC (RTX 3070 Ti); sin medir en gama baja. |
| **Auditoría de interfaz** con la verificación nueva de **paneles que se pisan** y la pantalla del **aviso de ataque** (5 aspectos × texto 100/125/150 %) | código final `1e23c82` | VIS español **15/15**, inglés **15/15**. Corridas intermedias registradas: 15/15 (antes de agregar las verificaciones nuevas); 14/15 (aviso de ataque y subtítulo del tío en 21:9 al 150 %: ahora manda el aviso); inglés 10/15 (título de la sala en dos líneas al 150 %: ahora se ajusta a una). En la captura del ataque además se corrigieron: subtítulo del ataque encimado con el consejo, el rótulo 3D gigante al estar pegado al tío (se achica y se oculta a menos de 2,4 m: el HUD ya lo dice) y el aviso rozando el objetivo con texto grande. |

**Qué cambió (todo visual; colisiones, masas, dificultad, red y condiciones de victoria sin cambios):**
- **Dirección visual:** noche de cierre; interior cálido (tungsteno, madera oscura, verde botella), exterior frío (luna, sodio de la calle) y acentos de neón rosa y magenta. La luz guía a lo importante: focos sobre la barra y el tío, relleno sobre el carrito, cartel **SALIDA** verde sobre la puerta, farol y luces de la camioneta.
- **Bar:** techo con vigas, friso de madera, moldura, zócalo y cornisa; marco y umbral de la puerta; estante con botellas, neones «CERVEZA HELADA» y «KARAOKE», cuadros y diana (fuera de los recorridos y sin colisión); la banqueta del tío pasó de cilindro gris a la del kit.
- **Afuera:** fachada con zócalo, cornisa, cartel de neón «THE LAST ROUND» y apliques; calle de asfalto, vereda de losetas, cordón, marcas viales, farol, contenedor y cajones; edificios vecinos y la cuadra de enfrente cierran la vista.
- **Camioneta:** carrocería propia sobre la caja funcional (laterales, techo, frente bajo, parabrisas inclinado, ruedas con taza, franja, luces, paragolpes, luz interior); piso de carga oscuro con la zona marcada con cinta amarilla; rampa de acero con bordes amarillos.
- **Render:** URP Forward+ (sin límite de luces por objeto), sombras de luces adicionales, volumen con ACES, bloom contenido, contraste y viñeta suave, solo en el bar.
- **Interfaz:** aparición de paneles en 0,12 s (opacidad desde 35 %, para que un aviso de peligro se lea desde el primer cuadro); texto atenuado en botones deshabilitados; jerarquía peligro > diálogo; aviso de ataque en filas propias.

### Pendientes reales (en orden)

0. **Terminaciones visuales que siguen pendientes** (sesión 10): el tío y el carrito siguen siendo modelos de prueba del kit (cajas facetadas; el carrito con laterales primitivos); no hay normales ni rugosidad en las texturas; los cuadros y edificios son geometría simple; el menú usa el render original del kit (no el bar nuevo). Falta mirar el resultado en una PC de gama baja y con una persona (brillo, contraste, mareo con el bloom).

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
