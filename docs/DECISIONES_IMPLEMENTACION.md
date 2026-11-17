# Decisiones de implementación

Las decisiones de diseño originales están en `preproduction/uncle_extraction/DECISIONES.md`. Este registro complementa ese documento.

## 2026-11-15 — Organización del repositorio

- Mantener los cuatro paquetes fuente bajo `preproduction/`, preservando sus archivos.
- Conservar el prompt maestro completo en la raíz.
- Usar `CLAUDE.md` como entrada breve y consultar detalles por hito.
- Reservar `UnityProject/` para el proyecto real que se creará en M0.
- Entregar contenido descomprimido; los ZIP de transporte no se agregan al repositorio.
- Mantener los binarios actuales en Git normal para este primer import. El archivo individual más grande entregado es menor de 20 MB. Si el arte empieza a crecer o cambia seguido, evaluar Git LFS antes de incorporar nuevos archivos grandes y revisar su configuración real.

Motivo: facilitar lectura selectiva, continuidad y primer import sin obligar a configurar servicios adicionales antes del laboratorio.

## 2026-11-15 — Repositorio local y Git LFS

- Problema: ninguna herramienta de la sesión pudo autenticarse contra el repositorio privado (403), así que no se pudo clonar.
- Decisión: inicializar Git en `uncle-extraction/` (la carpeta ya extraída, verificada contra `ENTREGA_SHA256.json`) y unir después la historia remota con `git merge --allow-unrelated-histories` (pasos en `PROGRESS.md`). Alternativa descartada: copiar a un clon nuevo, que requería credenciales no disponibles.
- `.gitignore`: agregados generados extra de Unity/IDE (`.utmp/`, `*.pdb`, `*.unitypackage`, Addressables en StreamingAssets, `.vscode/`, etc.). `.gitattributes`: normalización LF para `.mat`, `.anim`, `.controller`, `.inputactions`; `preproduction/**` sigue `-text` (10 fuentes tienen CRLF y deben quedar intactas).
- LFS: no por ahora (máx. 19,3 MB, total ~115 MB, por debajo del límite de 100 MB por archivo de GitHub). Reevaluar antes de agregar audio, texturas 2K+, `.blend` que cambien seguido o cualquier archivo >50 MB; en ese caso migrar `*.psd, *.wav, *.blend, *.fbx` de `UnityProject/` a LFS antes de que crezca el historial.

## 2026-11-15 — M1: simplificaciones del primer PhysicsLab (reversibles)

- Tío = un Rigidbody central de 110 kg con sockets en partes visuales (la especificación 04 lo permite para el primer prototipo). El ragdoll de 11 cuerpos queda para después de medir el laboratorio.
- La caída del jugador = bloqueo de control + cámara inclinada, sin ragdoll del jugador (especificación 03).
- Tras levantarse, la compostura vuelve a ≥60 (el GDD no lo fija). Ajustar en playtest.
- Carrito: rotaciones X/Z congeladas para estabilidad; la correa (E) funciona con el tío **cerca** de la caja y lo acerca con un resorte limitado.
- Entrada leída directamente de dispositivos (Input System si está instalado; si no, Input Manager clásico). Acciones remapeables en M6.
- HUD con IMGUI para no depender de paquetes de UI; el kit de UI se integra en M6.
- Render: la escena usa el pipeline activo. **URP pendiente** (crear su asset sin el editor abierto es frágil); al pasar a URP, reconstruir la escena desde el menú.
- Build del laboratorio con Mono: IL2CPP necesita Visual Studio con C++. Se prueba IL2CPP cuando esté.
- Capas de colisión: todavía no. Las manos son virtuales (sin colisionador), así que no se realimentan con el cuerpo agarrado.
- Pendiente para cerrar M1: distracción con vaso señuelo, ayuda de compañero al levantarse, superposición con sólidos y puerta en la extracción.

## 2026-11-15 — M0/M1: paquetes fijados y levantada del tío

- Problema: `ProjectBootstrap` no podía ejecutarse en un proyecto nuevo porque los asmdef de pruebas no compilaban sin Test Framework. Decisión: fijar Input System 1.20.1, Test Framework 1.6.0 y uGUI 2.0.0 directamente en `Packages/manifest.json` (se versiona). Se quitaron paquetes por defecto no usados (purchasing, analytics, collab-proxy, 2D, XR helpers).
- Problema: en el intento individual automático, el tío llegaba a la zona varias veces pero no se aseguraba. Al soltarlo, si llevaba ≥2,5 s en el piso se levantaba al instante y se iba caminando antes de los 1,5 s de conteo. Decisión: para levantarse necesita 2,5 s en el piso **y** 2,5 s sin agarres (`NpcBrain.Decide`). Antes: solo 2,5 s en el piso. Evidencia: antes, 1 de 2 corridas de la batería fallaba (el tío salía de la zona 3 veces); después, 3/3 PASS en ~20 s. Reversible; revisar en el playtest si hace al tío demasiado dócil.
- Entrada simulada: `LabInput.Simulated[]` se suma a la entrada real para que las pruebas usen los mismos caminos que un jugador (E, Q, Espacio, agacharse).

## 2026-11-15 — M1: correa del carrito, autopiloto y pruebas en tiempo real

- Correa: antes era una unión con límite lineal y rotación libre; el tío quedaba en cualquier orientación y, atravesado (1,9 m), no pasaba la puerta de 1,5 m. Decisión: al asegurar, la correa lo acuesta a lo largo de la caja (cabeza hacia el frente, boca arriba) y bloquea la rotación relativa; se desactiva la colisión tío-carrito, porque el prototipo se superpone con las barandas. Alternativa pendiente: ragdoll o pose encogida. Reversible.
- `LabAutopilot`: el bot de pruebas pasó a ser un componente de la escena, para que pruebas y build compartan código y el usuario pueda mirarlo. Usa solo entradas de jugador.
- Pruebas de intento completo en tiempo real: con `timeScale` 4 el carrito pasaba 3/3, pero en la build fallaba 0/9 (el bot decide por frame y a 4x controla más grueso). Se usa `timeScale` 1 (con `[Timeout(330000)]`). La prueba del carrito estuvo `[Ignore]` hasta corregir el bot (histéresis al cruzar la puerta); hoy pasa.
- `runInBackground = true`: sin foco, la build se pausaba (una prueba de humo se congeló a los 2,8 s). También es necesario para el anfitrión de M2.

## 2026-11-15 — M2: red temprana

- Topología: listen server con NGO 2.14.0 + UTP 2.7.5 (especificación 07). El modo individual es un anfitrión en loopback (127.0.0.1), así individual y cooperativo comparten el mismo camino de autoridad. Alternativa descartada: un modo offline separado (duplica lógica y oculta errores de red).
- NetworkManager creado en tiempo de ejecución por `NetSession` (NGO lo hace DontDestroyOnLoad): al recargar la escena (reintento, pruebas) se cierra la sesión y se abre otra; un cliente cuya escena recarga la sincronización de NGO conserva la conexión.
- Gestión de escenas de NGO **activada**. Sin ella, los clientes borran los NetworkObjects colocados en la escena y esperan prefabs registrados (verificado en el código de NGO 2.14 y en una prueba: el cliente perdía el tío, el carrito y los sistemas).
- Entrada: los clientes envían intenciones secuenciadas (botones, movimiento, mirada) por RPC no fiable; el anfitrión recalcula el punto apuntado con la mirada recibida (no confía en un socket elegido por el cliente). Sin predicción del jugador en esta etapa: con 5–7 ms en localhost no hace falta; reevaluar con latencia simulada y en LAN.
- Clientes desconectados: congelados (`NetAuth.Frozen`) hasta volver a jugar solo. Sin esto, la copia local pasaba a simular sola y llegó a «ganar» una ronda inexistente.
- Laboratorio: se admite unirse con la ronda en curso y el escalado se recalcula. El lobby de M4 bloqueará incorporaciones durante la ronda (especificación 07).
- Sin migración de anfitrión (especificación 07).

## 2026-11-15 — Carga del carrito válida

- Antes: `Strap` recolocaba y rotaba al tío al instante sobre el carrito. Con un jugador o una pared en ese volumen, la física despedía al carrito o al jugador (incluso a través de la pared).
- Ahora: valida el volumen del tío acostado (`OverlapCapsule`, ignorando al tío y al carrito) y, si está ocupado, no carga y avisa por qué. La subida dura 0,45 s y es cinemática, con un arco corto; el destino sigue al carrito. Las colisiones tío-carrito se ignoran mientras sube y está asegurado, y se restauran al soltarlo o reiniciar. Se conserva la ayuda para jugar solo: E carga y asegura en un paso.
- Pendiente: animación del tío; ragdoll reducido.

## 2026-11-15 — M3: camioneta, cierre, señuelo y red degradada

- Camioneta hueca con el tamaño del kit (2,5 × 5,4 m), piso de carga a 0,30 m y rampa de 2,4 m (7°). Con 0,45 m y 1,5 m (16,7°), subir al tío arrastrándolo exigía ~750 N, más que el tope de un jugador (700 N); el bot quedaba al pie de la rampa. Ahora hacen falta ~570 N. La rampa es una ayuda de prototipo; revisar con el arte final.
- Cierre en fases (Open → Holding → Closing → Closed): E acumula una vez por frame aunque lo mantengan varios jugadores. La victoria se declara con las hojas efectivamente cerradas y las condiciones todavía válidas; si fallan durante el cierre, se aborta y se reabre. Las hojas tienen Rigidbody cinemático (colisionador que se mueve).
- Vaso señuelo: solo atrae si un jugador lo movió (`Placed`). Antes, el vaso sobre la mesa distraía al tío al empezar la ronda y consumía el enfriamiento. La distracción se evalúa también durante Recover (GDD: «si no está atacando»), pero no durante la apertura forzada ni en el carrito.
- Red degradada: el simulador integrado de UTP quedó obsoleto en NGO 2.14. Se usa Multiplayer Tools 2.2.12 (NetworkSimulator), que NGO solo compila en editor y builds de desarrollo; por eso hay una build de desarrollo separada (`-ueDev`). La build de distribución no lleva simulador.

## 2026-11-15 — Escenario principal, LevelLayout, freno del carrito y fuga

- `LevelLayout` (componente de escena escrito por el constructor) concentra apariciones, ejes de puerta y camioneta, rutas de prueba y puntos de interacción. Ningún sistema usa coordenadas fijas de PhysicsLab.
- Constructor por especificación (`LevelSpecs`): PhysicsLab sin cambios para diagnóstico; `Bar_LastRound` como escenario principal con los FBX del kit como visuales sobre cajas del tamaño medido. No se colocan la camioneta, el carrito ni el tío del kit, ni el cascarón del bar (ver `docs/CATALOGO_RECURSOS.md`).
- Freno de estacionamiento del carrito: sin nadie en el asa (y sin el tío asegurado) tiene amortiguación alta. Sin freno, el tío arrastrado hacia él lo empujaba varios metros y lo encajaba contra una pared con el asa inaccesible.
- Fuga: si está libre, vuelve a su banco pasando por la puerta del escenario (antes iba en línea recta contra el muro frontal). Si hay alguien a menos de 3 m, va a 2,2 m/s (`EscapeSpeedCap` del balance); si no, a 1,4 m/s.
- Prueba de intento completo: la condición «el tío atacó» pasó a «atacó **o** intentó soltarse del carrito». Asegurado en el carrito, el tío no ataca: su resistencia según el GDD 05 son los intentos de soltarse. Las condiciones de extracción no cambiaron.
- `net_smoke.ps1`: código 0 solo si todos los escenarios pedidos dieron PASS.

## 2026-11-16 — Tío articulado (ragdoll reducido) como variante de prueba

- Pruebas de resistencia separadas por modo: en arrastre se exige que el tío **ataque** (`AttacksStarted > 0`); en carrito, que **intente soltarse** (`WrigglesStarted > 0`). Además hay una prueba específica (`Cart_StrappedUncle_SignalsAndTriesToWriggleFree`) que exige la señal visible antes del intento. Reemplaza el «atacó o intentó soltarse» anterior, que podía ocultar que uno de los dos fallara.
- `NpcBody` es ahora un contrato común a las dos variantes: `AllBodies`, `Colliders`, `Owns`, `Of`, `GrabCount`, `ReleaseAllGrabs`, `SetKinematic`, `TeleportTo`, `MoveKinematic`, `TotalMass`. Carrito, extracción, puertas, manos, rescate técnico, director y autopiloto usan solo el contrato; el cuerpo central (una cápsula) sigue en PhysicsLab y Bar_LastRound como respaldo.
- Variante articulada (`ArticulatedUncleBuilder`, escena `PhysicsLab_Articulated`): pelvis raíz + pecho, cabeza, brazos, antebrazos, muslos y pantorrillas (11 cuerpos) sobre las posiciones de reposo medidas del rig del kit. Masa total 110 kg repartida (20/30/7/2×4/2×3/2×11/2×8,5), no 110 por parte. `ConfigurableJoint` con traslación bloqueada, límites angulares, proyección (3 cm / 5°) contra estiramientos y slerp drive hacia la pose de reposo (fuerte de pie, flojo caído). Colisiones internas ignoradas entre todas las partes del tío; `maxDepenetrationVelocity` 3 m/s y 12 iteraciones del solver para evitar explosiones.
- Equilibrio: resorte de altura sobre la pelvis (raycast desde el centro de masa total, fuerza limitada a 1,6× el peso) + el par de equilibrio existente en pelvis y pecho. No es un ragdoll activo con pasos de piernas: el tío se desliza como el central, con la pose reducida encima. Alternativa descartada por ahora: locomoción con piernas, costosa y frágil para el alcance inicial.
- Sockets con los mismos ids sobre la parte que corresponde (antebrazos, tobillos en las pantorrillas, hombros en el pecho, cinturón en la pelvis). El ancla del tío a la baranda se crea en la parte de la mano.
- Extracción: en lugar de una cápsula única, cada collider del tío se verifica contra sólidos con `ComputePenetration` (tolerancia 4 cm). Rescate técnico: vigila todas las partes (posición no finita, bajo el mapa, velocidad) y devuelve el cuerpo entero a la última pose segura; no interviene por jugar mal.
- Red: el anfitrión simula; cada parte tiene `NetworkTransform` de autoridad del servidor y en clientes las partes son cinemáticas (pose reducida replicada).
- Malla del kit: `uncle_prototype.fbx` girado 180° y ligado a las partes con `RagdollVisual` (cada hueso copia la pose de su parte con el desfase medido en reposo). El lado de cada hueso se elige por cercanía, no por el sufijo `.L/.R`.

## 2026-11-16 — URP y dificultad del tío articulado

- URP 17.3.0 (el que trae el editor; no requiere descarga). `UrpSetup.Ensure()` crea el asset y el renderer si faltan, los asigna a Graphics y a todos los niveles de calidad y, si cambió el pipeline, reimporta los FBX del kit para que sus materiales salgan con URP/Lit. Lo llama `PhysicsLabBuilder.Build`, así que reconstruir las escenas deja el proyecto en URP aunque se borre la configuración. Los materiales del constructor toman el shader del pipeline activo.
- `-ueShot archivo@segundos`: capturas desde la propia build para verificar la presentación sin depender de la ventana (PowerShell no obtiene el handle de la ventana de Unity).
- Tío articulado de pie: el resorte de altura reemplaza el apoyo de los pies y ahora también su fricción (Coulomb, μ 0,42, en el punto de apoyo). Sin fricción se deslizaba; repartida por masa quedaba inamovible y no se inclinaba nunca; en el punto de apoyo da el mismo par que inclina al central. Bar_LastRound usa el articulado; respaldo: `articulatedUncle = false`.
- Freno del carrito con el tío atado: probado y revertido (ver PROGRESS).

## 2026-11-16 — Atascos abiertos: fallos del bot frente a problemas de interacción

Se armaron reproducciones controladas (`StuckRecoveryTests`) con un jugador guionado que solo usa entradas normales.

**Extremidad en la abertura de la camioneta (problema de interacción, corregido en el juego):**
- El motivo era genérico («Le queda algo afuera»). Ahora nombra la parte: «Le queda un pie afuera: empujalo adentro».
- La mira no dejaba tomar un segundo punto cerca del primero: el punto que ya sostenía la otra mano ganaba la selección. Por eso la segunda mano (del jugador y del bot) no tomaba el otro hombro para acomodarlo. Ahora un punto de capacidad 1 que ya sostiene el propio jugador no compite.
- La recuperación (empujar el tobillo hacia adentro o, si la pierna vuelve a estirarse, entrar por el costado y tirar de los dos hombros hacia la cabina) funciona.
- Salir de la camioneta por el costado con el tío acostado en el centro también funciona.
- No se tocaron colisiones, el tamaño del cuerpo ni las condiciones de extracción.

**Carrito atravesado en la puerta del bar (fallo del bot):**
- Con el carrito cargado en diagonal (45°) o atravesado (90°) en el marco y la hoja de la puerta apoyada, un jugador que tira recto del asa lo saca. La hoja y el marco lo enderezan.
- Primero pareció irrecuperable: era un error del armado de la prueba (el carrito aparecía encimado con la hoja cerrada). Se corrigió el armado y se descartó el retén de apertura que se había agregado por ese motivo.
- En el bot: junto a la puerta ya no zigzaguea, sino que tira recto; el zigzag lo terminaba de girar contra la pared.
- Otro modo del bot, visto en la regresión: con el asa contra una pared, caminaba contra el carrito para siempre. Ahora rodea hasta un costado libre del asa.

**Puerta del bar:**
- Se midió que se cierra sola con 25 N en el borde a 45° y 64 N a 90°.
- Se agregó una manija agarrable (`door_handle`; GDD: «manija visible», «si nadie abre una puerta») y el reinicio de la hoja al reiniciar la ronda, que faltaba.

## 2026-11-16 — Final de ronda por convergencia (instrumentación)

- El registro del estado final ya no espera 0,5 s fijos. Cada proceso espera hasta que el estado replicado es coherente con el resultado del mismo intento: con victoria, tío asegurado, puertas `Closed` y tío `Secured`; con derrota, nada asegurado.
- El tiempo máximo es 2 s, por debajo de los 3 s en que el anfitrión reinicia o cierra. Se registra `convergencia=<s>` o `TIMEOUT(...) faltan=...`. Si llega un reinicio antes de converger, se registra `reinicio` sin mezclarlo con la ronda siguiente.
- `net_smoke.ps1` compara anfitrión y cliente del mismo intento (`reinicios`), exige convergencia y anota en cada resultado la versión de la build (`VERSION.txt`). El historial acumulado (`net_smoke_history.csv`) conserva fallos iniciales y repeticiones.
- No cambió la arquitectura del juego.

## 2026-11-16 — Señales de ataque y lenguaje de interacción

- **Poses de ataque del tío articulado:** se mueven los objetivos de las articulaciones en el anfitrión (pose física, replicada con las partes). Nunca se desincronizan del golpe, que decide `NpcBrain` con su reloj.
  - Empujón y patada salen de los clips del kit (`Push_Prototype`, `Kick_Prototype`) muestreados al construir la escena. El cuadro de preparación es el efector más atrás antes del golpe y el cuadro de golpe, el más adelante.
  - Manotazo y sacudida son poses autorizadas por direcciones de segmento (el kit no trae esos clips).
- **Aviso en el piso** con el alcance y el arco reales del ataque (`LabBalance`), con relleno que crece durante la preparación. **Nombre del ataque** sobre la cabeza, con las claves del catálogo del kit (`hud.slap`…). Ninguno de los dos depende solo del color.
- **Feedback de agarre del jugador local:**
  - anillo sobre el punto apuntado: válido (sólido), ocupado (grueso, con «ocupado») o fuera de alcance (punteado, «acercate»);
  - línea de tensión de cada mano que se engrosa y parpadea cerca de soltarse;
  - vibración de las manos con esfuerzo;
  - rombo sobre lo que bloquea el cierre de la camioneta, con el motivo.

## 2026-11-16 — Interfaz del kit, idiomas y controles

- **uGUI con texto estándar y las fuentes DejaVu del kit, no TextMeshPro.** El proyecto no tiene TMP Essentials (falta `TMP_Settings`) y su importación es interactiva. El texto de uGUI con una fuente TTF dinámica muestra acentos, ñ y signos.
- **Pantallas construidas por código en tiempo de ejecución,** con `UiSkin` (Resources) que guarda los sprites, íconos y fuentes del kit. Son reproducibles y no hay prefabs que mantener a mano.
- **Mensajes que viajan por red como claves con parámetros** (`van.part_outside|part=part.foot`): cada jugador los ve en su idioma. Los textos fijos en español dentro del código se reemplazaron por claves.
- **Controles reasignables con un mapa propio** (`InputBindings`: rutas del Input System por acción, guardadas en PlayerPrefs) y no con un `InputActionAsset`. `LabInput` ya centraliza la lectura por acción y el autopiloto y las pruebas la simulan. Si una tecla ya estaba asignada, se intercambia. El mando conserva su asignación fija.
- **Datos de resultados:** solo los reales (tiempo, caídas del grupo, dificultad y jugadores). No hay contador de fugas, así que no se muestra. Tampoco hay premios.
- **Lobby y preparación de jugadores:** implementados después (`f2b4ebc`): sala con LISTO, dificultad del anfitrión y rechazo con motivo.

## 2026-11-16 — Internet por Relay, auditoría de interfaz y rendimiento

- **Relay directo (allocation + código), no la API de Sessions.** Se usa `com.unity.services.multiplayer` 2.4.1 (la 2.5.0 salió el mismo día; se eligió la anterior, con un mes de uso). Con la API de Relay el juego sigue arrancando `NetworkManager` por su cuenta: la misma aprobación de conexión (versión, sala llena, ronda iniciada), la misma sala y el mismo anfitrión autoritativo que la conexión directa. Sessions arranca la red por sí misma y habría duplicado ese flujo. El código de Relay hace de código de sala. Si más adelante hace falta listar salas o reconectar, se puede envolver con Sessions sin tocar el juego.
  - Anfitrión: sesión anónima → asignación para 3 invitados → código → `SetRelayServerData(dtls)` → `StartHost`. Invitado: código → `JoinAllocation` → `StartClient`.
  - Sin proyecto vinculado (`Application.cloudProjectId` vacío), el proveedor devuelve `error.online_unlinked` sin tocar la red: no hay credenciales ficticias. El menú lo avisa antes de intentar. La conexión directa por IP y el modo solo no cambian.
  - Fallos de internet: vuelven al menú con el motivo en un diálogo, sin pasar a jugar solo en silencio.
- **Diálogo de error del kit en el menú** (anfitrión perdido con detalle, sala llena, otra versión, ronda iniciada, código inválido, servicio, tiempo). La línea de estado queda para avisos breves.
- **Tamaño de texto (UI_SPEC: «el layout puede crecer; no achicar toda la tipografía»):**
  - las etiquetas usan `Truncate` para que el ajuste automático funcione de verdad (con `Overflow` no actuaba en vertical y el texto se derramaba sobre lo de abajo); el mínimo al 150 % es el tamaño base;
  - filas, cajas y separaciones que dependen del texto crecen con la escala (`Ui.K`);
  - el título del menú es un logo y no crece;
  - `FitInCanvas` reduce un panel solo si no entra en el canvas (21:9 con texto grande). Es un compromiso para revisar en la prueba humana.
- **Pantallas 21:9 en un monitor 16:9:** se prueban con una ventana del mismo aspecto (1920×810 para 2560×1080). El `CanvasScaler` (1920×1080, match 0,5) escala uniformemente, así que la disposición en unidades de canvas es idéntica; cambia solo la densidad de píxeles.
- **Rendimiento:** `PerfProbe` usa `FrameTimingManager` (se habilitó *Frame Timing Stats*) para separar el trabajo de CPU y GPU del límite de sincronización vertical. Varias instancias en una PC compiten por CPU y GPU: sirven para encontrar fugas y picos, no para estimar cuatro equipos.

## 2026-11-16 — Pulido visual (sesión 10)

- **El kit 3D viene con Z hacia arriba.** `KitVisual` lo convierte a Y arriba para todo menos el tío (su rig ya importaba bien). Antes, banquetas, mesas, mostrador y puerta estaban acostados; la camioneta y el carrito no se notaban porque `KitDressing.Fit` los ajusta por nombre a cajas del mundo.
- **Camioneta con carrocería propia, no la del kit.** El kit es un bloque de prueba (placas sin techo visible). La carrocería sigue la caja funcional (2,50 × 5,40 × 2,30 m): colisiones, piso, rampa, zona y hojas no cambian. Los renderers del kit y de los primitivos quedan ocultos.
- **Todo el vestido es visual y sin colisión** (`BarDressing`, solo Bar_LastRound): nada nuevo bloquea recorridos, raycasts de agarre ni el cierre de la camioneta. Los objetos decorativos no se parecen a los agarrables (sin asas, fuera del alcance o en paredes y techo).
- **Forward+** en URP: con ~15 luces cálidas, Forward limitaba las luces por objeto y el piso mostraba cortes. Sombras solo en la luna, un foco de la barra y el farol.
- **Posproceso solo en la escena que tiene volumen** (`ScenePostFx`): en todas las escenas costaba un pase sin aportar nada y cambió el ritmo de cuadros lo suficiente para romper una maniobra guionada en el laboratorio (`lab_fail_m28`, 4/4 FAIL → 3/3 PASS). Es un hallazgo sobre la prueba, no sobre las reglas.
- **Texto 3D con sombreador propio** (`UncleExtraction/WorldText`): el material de las fuentes dinámicas dibuja siempre encima y se veía a través de muros y techo. `WorldTextFont` mantiene la textura de la fuente al reconstruirse.
- **Aparición de interfaz:** 0,12 s, desde 35 % de opacidad y 97 % de escala; sin movimientos de cámara ni destellos. El rótulo 3D del ataque se oculta a menos de 2,4 m porque el aviso del HUD ya lo dice y de cerca tapaba la pantalla.
- **Jerarquía del HUD:** peligro > interacción > diálogo. Si el aviso de ataque y el subtítulo del tío no entran juntos, manda el aviso.

## 2026-11-17 — Segunda pasada de pulido y FPS (sesión 11)

- **Objetivo de la mano desde la mirada ordenada** (`PlayerMotor.EyePosition` + `LookRotation`), no desde el transform de la cámara: la cámara se orienta una vez por cuadro y, con menos de 60 FPS, el segundo paso de física del cuadro veía el cuerpo ya girado con el desfase anterior de la cámara. Es un arreglo de juego medido con límites de 30/60/120 FPS; no cambia fuerzas, alcances ni criterios.
- **El bot y el jugador guionado se mueven relativos a la mirada ordenada** (como WASD), no al giro interpolado del cuerpo.
- **Esperas de las pruebas en pasos de física** cuando arman una situación física (puerta que se cierra sola), no en segundos de reloj.
- **El posproceso solo en el bar se mantiene** por costo, pero ya no es la explicación del fallo del laboratorio.
- **Tío:** se conserva la malla del kit (rig y uniones verificados); el pulido es de materiales, gorro y señal. Cambiar la malla exige rehacer el ligado de huesos y repetir las pruebas de ragdoll.
- **Carrito y focos de la barra con geometría propia**, como la camioneta en la sesión 10: el kit es un bloque de prueba y la geometría funcional manda.
- **Calidad gráfica Baja** (escala de render 75 %, sin MSAA, sombras solo de la luna a 20 m, sin bloom), automática con GPU integrada: medición real de 11,7 fps en la Iris Xe con Alta.
- **Fondo del menú:** captura del escenario actual con la misma cámara reproducible (`CamTour`), no una escena 3D en vivo: mismo resultado con costo nulo en el menú.

## 2026-11-17 — Versión candidata (sesión 12)

- **Medir rendimiento con un banco de vistas fijas**, no con partidas de autopiloto: el recorrido del bot cambia en cada corrida. El calentamiento y la carga no se miden; se registran GPU, alimentación, pantalla, vsync, límite, escala, MSAA, luces con sombra y posproceso.
- **Comparar builds con el mismo código de medición:** `2fc2d5d` se compiló en un árbol aparte con el mismo `Bench.cs`.
- **GPU forzada con `-force-device-index`** (parámetro de Unity), sin tocar la configuración gráfica de Windows.
- **Detección de integrada por tipo de dispositivo** (nombre y fabricante), no solo por fabricante; la memoria informada no decide sola porque en las integradas es compartida.
- **Guardia de tandas:** el preflight acepta una build si su commit no difiere del actual en `UnityProject/` (los commits de documentación posteriores no la invalidan).

## Plantilla para nuevas decisiones

Fecha, problema observado, decisión, alternativas relevantes, impacto y evidencia. Para balance, registrar valores anteriores y nuevos y el resultado de la prueba.
