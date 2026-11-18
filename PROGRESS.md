# Estado del proyecto

Última actualización: 2026-11-18, sesión 15 (producción: manos, sonido, tío, camioneta, bar que reacciona, desafíos, parte de extracción, material de lanzamiento). Detalle de sesiones anteriores (sin reescribir, con todos los FAIL): `docs/HISTORIAL.md`.

## Estado vigente

**Build para probar** (local, no versionada): `Builds/UncleExtraction/UncleExtraction.exe`, con `VERSION.txt` (commit, cambios sin commitear, fecha, tipo), `LEEME.txt` (cómo jugar, controles) y `PRUEBA_HUMANA.txt` (guion corto de prueba con planilla). `Builds/UncleExtraction_Dev/` es la de desarrollo (simulador de red). Build de referencia anterior: `2fc2d5d` (respaldo local en `Builds/UncleExtraction_Backup_2fc2d5d/`).

**Qué se puede jugar:** Bar_LastRound solo o en cooperativo de 2 a 4 (internet por Relay con código de sala; red local por IP). Tío articulado de 11 cuerpos con poses de ataque; bar de noche con techo, luces cálidas, neones y calle; camioneta con carrocería propia; carrito con modelo propio; tío con ropa, materiales y gorro de fiesta; vaso señuelo, ayuda para levantarse, extracción con puertas. Interfaz del kit (menú, sala, HUD, pausa, opciones, resultados, errores), EN/es_UY, controles reasignables, opciones guardadas, mando en menús y juego. Sonido sintetizado y subtítulos del tío; sin voz ni música.

**Niveles de evidencia:** PM = PlayMode en el editor; EM = EditMode; AUTO = partida completa con entradas de jugador simuladas; NET = procesos de la build en **localhost** (no LAN ni internet); VIS = auditoría y capturas reales de la build. **Ninguna fila tiene prueba humana.**

### Sesión 15: producción — lo que se ve, se oye y se siente; rejugabilidad; material de lanzamiento

**Rama:** `s15-produccion` (sin fusionar a `main`). **Candidata: `fd0bce5`** (`Builds/UncleExtraction/VERSION.txt`, rama `s15-produccion`; código de juego igual a `9a2d7cb` salvo dos rótulos de interfaz); ZIP de demo `Builds/UncleExtraction_Demo_s15.zip`. **Referencia de la sesión 14 conservada:** `Builds/UncleExtraction_Ref_s14/` y `Builds/UncleExtraction_Ref_s14.zip` (`85998c0`). Comparativa: `docs/capturas/sesion15/antes_despues.jpg` (mismas vistas fijas con `tools/windows/compare_shots.sh`).

**Qué cambia al jugar**
- **Manos:** antebrazos mate que salen hacia las esquinas inferiores (antes: cilindros con tapas a la vista); cada clic aprieta y empuja la mano al instante con un roce de tela, antes de la confirmación del anfitrión.
- **Sonido (síntesis propia):** contacto del clic, resbalón (te lo arrancaron con el botón apretado) distinto de soltar, crujido de tela con tensión alta, ruedas del carrito según velocidad y golpes contra el marco, pestillo al cerrar, ambiente de bar, remate breve al ganar, vasos.
- **El tío reacciona a lo que pasa:** se da cuenta de que está en la camioneta, se levanta, le frenan un golpe, se rompen vasos (líneas nuevas DLG_101–109, con guion grabable en `docs/GUION_VOZ.md`).
- **Ropa y luz:** cuello abierto, bolsillo con costura, botones, ruedos, cinturón con hebilla; menos exposición y luces que lavaban la camisa; piel y camisa con tono propio.
- **Camioneta:** burletes, ventanillas, manijas y bisagras en las hojas; piso con nervaduras; luz que marca dónde acomodar al tío y huellas con luz en el lugar desde donde se cierra; menos brillo saturado.
- **Bar que reacciona:** torre de vasos junto a la salida; si el carrito, el tío o alguien pasa rápido, se cae, el barman reacciona y el tío se distrae 2,5 s (se suelta de la barra). Una vez por ronda.
- **Desafíos opcionales** (menú solo o sala, anunciados en el objetivo y en el parte): *Sin carrito* y *Ruta estrecha* (barricada de cajones con un solo paso de 1,2 m y marca amarilla en el piso). La ronda estándar sigue siendo la principal.
- **Parte de extracción:** tiempo, caídas, ataques del tío (y cuántos frenaron), momentos reales con su minuto (primer agarre, mano soltada, carrito, camioneta, cierre, torre) y destacados solo positivos; «Vista para captura» sin HUD.

**Error encontrado antes de entregar:** al agregar `-ueNoHud` (capturas de tienda) un comentario quedó en la misma línea que la creación del HUD: la candidata `ed7d493` **no mostraba el HUD**. Lo detectaron dos pruebas de interfaz; corregido en `04d5df4`. También: el aviso del desafío nunca aparecía (corregido) y la guía quedaba atrás cuando el jugador ya llevaba al tío (ahora salta a la camioneta).

**Carrito en la puerta (investigado):** en las tandas que fallaban, el jugador del autopiloto quedaba 200 s empujando la hoja entreabierta de la puerta de salida; una persona la empuja o la rodea. Era el autopiloto abandonando una situación recuperable, no un atasco del juego: se agregó destrabe genérico (si camina y no avanza en 2,5 s, se corre de costado). Fallo anterior registrado en el historial de la sesión 14.

#### Verificación (cada resultado con su build)

| Prueba | Código / build | Resultado |
| --- | --- | --- |
| EditMode | `2b36fd4` (código) | 64/64, 1 ignorada |
| PlayMode: desafíos, torre, parte, guía, entrada real | `2b36fd4` → `9fbdbe3` | Sin carrito: gana solo en 21,7 s; Ruta estrecha: gana solo en 40,4 s por el paso; torre: el carrito a 2,2 m/s la tira y el tío se distrae; se rearma al reiniciar; guía y entrada real verdes |
| PlayMode: intentos completos solo (laboratorio, articulado) + desafíos | `9fbdbe3` (código) | **12/12** (incluye los intentos con carrito del laboratorio articulado) |
| PlayMode completa | `9a2d7cb` (código) | **164/165, 0 FAIL**, 1 omitida (sobre `ed7d493` había dado 162/165: 2 FAIL de interfaz que destaparon el HUD faltante, ver abajo) |
| Red en localhost (17 escenarios, incluye 4 procesos y latencia simulada) | build `9fbdbe3` | **16/17 PASS**; `race` FAIL (rechazos=0). Repetido ×3: 1 PASS, 2 FAIL. Causa vista en el registro: el invitado tenía como candidato el antebrazo y no el cinturón, así que no hubo disputa simultánea que medir; la invariante (un solo ganador, nunca dos manos en el mismo punto) se cumplió en las 4 corridas. En las sesiones 13–14 había pasado 3/3: **abierto**, revisar la puntería del autopiloto en la carrera |
| Cuatro procesos reales con capturas | build `S15` previa | Sala con selector de desafío, agarres simultáneos, carrito, cierre de la camioneta (`.local-setup/clips/cuatro_*.png`) |
| Capturas comparables antes/después | builds `85998c0` y `S15` | `docs/capturas/sesion15/antes_despues.jpg` |
| Clips de juego real | build `S15` | 3 verticales + 2 horizontales (WebP) en `docs/lanzamiento/clips/` |
| Red en localhost (17 escenarios) | build `9a2d7cb` | **16/17 PASS**; `race` FAIL otra vez (rechazos=0): abierto |
| Auditoría de interfaz ES/EN | build `9a2d7cb` | 4/15 por idioma: rótulo de la sala «DIFICULTAD · Desafío: …» y botón «Vista para captura» recortados con texto 125–150 % → corregido |
| Auditoría de interfaz ES/EN (reauditoría) | build `fd0bce5` | **15/15 por idioma** |
| Partida completa de humo con la build final | build `fd0bce5` | Gana sola con el autopiloto, sin excepciones |
| Prueba humana del dueño | demo `fd0bce5` | **Hecha** (Stefano, 2026-11-18); detalles por tarea pendientes de que los pase (`docs/OBSERVACIONES_PRUEBAS.md`) |
| Relay desde una PC / dos equipos / grupos nuevos | — | **NOT_RUN** |

**Material de lanzamiento (borradores, sin publicar):** `docs/lanzamiento/` — referencia de mercado, textos de tienda ES/EN, cápsulas desde capturas reales, lista de tomas del tráiler, clips verticales, plan de pruebas sin instrucciones y de difusión.

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

1. **Vos (25 min, sin explicar nada):** pasale `Builds/UncleExtraction_Demo_s15.zip` a alguien que no lo vio, con `docs/lanzamiento/PLAN_PRUEBAS_Y_DIFUSION.md` §1 (tiempo al primer agarre, dónde se estanca, si pide otra ronda, qué le contaría a un amigo). Anotar en `docs/OBSERVACIONES_PRUEBAS.md`.
2. **Con otra PC y 2–4 personas:** cooperativo por código de sala con el desafío *Ruta estrecha* y la torre de vasos; revisar el parte al final.
3. **Revisar borradores** de `docs/lanzamiento/` (textos, cápsulas, tomas del tráiler) y decidir si fusionar `s15-produccion` en `main`.
4. **Desarrollo siguiente (sin depender de nadie):** malla del tío con más detalle (silueta y ropa con volumen real), materiales de la barra, puerta y carrito (vetas, desgaste, herrajes), grabación de video (requiere tu permiso para instalar ffmpeg u OBS), música con licencia.
