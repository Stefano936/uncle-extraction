# Matriz de requisitos — alcance inicial (alcohol, un bar, camioneta fija, 1 a 4 jugadores)

Fuentes: GDD y especificación técnica (`preproduction/uncle_extraction/`), instrucciones vigentes del dueño (sesiones 9 a 14) y `CLAUDE.md`. Se actualiza en cada bloque. Última revisión: sesión 17 (2026-11-18, rama `s17-demo`, demo `b44386d`, QA `1bc221d`). Cada evidencia nombra su build o commit cuando no es la candidata vigente.

**Niveles de evidencia:** **C** = existe en código y compila · **E** = probado en el editor (PlayMode/EditMode) · **B** = probado en la build · **L** = procesos en localhost · **R1** = Relay desde una PC · **R2** = dos equipos y redes distintas · **H** = revisado por una persona.
**Estados:** completo · parcial · faltante · pospuesto (justificado) · bloqueado.

## Controles y entrada

| Requisito | Prio. | Implementación | Estado | Evidencia | Próxima acción |
| --- | --- | --- | --- | --- | --- |
| Moverse con WASD **y flechas**, diagonal normalizada, alternativas sin sumar velocidad | Alta | `LabInput.RealMove` (sesión 13) | completo | C, E (`RealInputTests`, teclado simulado) | B en la build final; H |
| Movimiento remapeable | Alta | `InputBindings` (`MoveKey`) + Opciones | completo | C, E (reasignar avanzar a I) | H |
| Dos manos independientes (clic izq./der.) | Alta | `PlayerHands`, `GrabIntent` | completo | C, E (`RealInputTests`), L | H |
| Agarre **mantener / alternar** (local, persistente, igual en anfitrión y cliente) | Alta | `GrabIntent`, Opciones → Agarre. Sesión 14: Alternar también exige soltar el botón tras un bloqueo; con un punto válido a la vista espera la confirmación hasta 1,5 s | completo | C, E (20 combinaciones mano × modo × bloqueo; el código de `4509f5b` fallaba 4), L (`guesttoggle` 6/6; `guesttoggle_sim` 150 ms ±30: 6/6, confirmación a los 653 ms) | H |
| Q suelta de forma fiable; el clic sostenido no reagarra | Alta | `GrabIntent` (rearmado al soltar el clic, en los dos modos) | completo | C, E, L (invitado: Q con el botón sostenido) | H |
| Menús, pausa, campos de texto, reasignación y ventana sin foco no disparan acciones | Alta | `GameFlowUI.BlockingInput` (+ `Ui.Typing`), `PlayerControls.Pressed`; Esc que cancela una reasignación ya no abre/cierra la pausa (`InputBindings.JustListened`) | completo | C, E (pausa cerrada con Esc, Opciones, reasignación, foco perdido, reinicio de ronda), L (invitado en pausa) | Foco real de Windows (H) |
| Reinicio: lo controla el anfitrión; el invitado lo pide al terminar la ronda; R solo diagnóstico | Alta | `RoundDirector` (sesión 13) | completo | C | L: `repeat`/`lose` en la regresión de red |
| Opciones: Aplicar/Descartar incluye controles; captura cancelable; reservadas (Esc, Tab, flechas) | Alta | `OptionsView` + borrador de `InputBindings` | completo | C, E | H con mando |
| Preferencias validadas (dificultad, rangos, finitos, idioma, rutas) | Alta | `GameSettings.Validate`, `InputBindings.Valid` | completo | C, E (`DamagedPreferences_AreValidatedBeforeUse`) | — |
| Tamaño de texto e idioma durante la partida | Media | `GameHud.Rebuild`, `GameFlowUI.RebuildPanels` | completo | C, E | H |
| Mando: navegación de menús | Media | `InputSystemUIInputModule`, `GamepadBack` | completo | E (`MenuNavigationTests`) | H con un mando real |
| Comodidad: reducir inclinación, sensibilidad, FOV (vertical) | Media | `GameSettings.ReduceMotion`, Opciones | completo | C | H |

## Aprendizaje y claridad

| Requisito | Prio. | Implementación | Estado | Evidencia | Próxima acción |
| --- | --- | --- | --- | --- | --- |
| Primera partida guiada, omisible, repetible desde pausa, local en cooperativo | Alta | `FirstSteps`: 9 pasos. Ganar la completa; omitir la cierra; perder guarda el paso y se retoma al reintentar o en otra partida; oculta no avanza ni corre tiempos; arrastrar o carrito valen para el traslado | completo (implementación) / verificación parcial | E (`GuideLifecycleTests`: ganar arrastrando recorre los 9 pasos y la completa; perder retoma; oculta no avanza), B (auditoría `hud_guide`, ES/EN) | **H: primer agarre en el primer minuto** |
| «Cómo jugar» breve y fiel (teclas reales, modo de agarre) | Media | `FrontendMenu` HowTo | completo | C | H |
| Feedback de mira: válido / ocupado / fuera de alcance | Alta | `GrabFeedback` | completo | E, B (capturas) | Sonido distinto por estado (faltante) |
| Agarre pendiente vs confirmado (autoridad del anfitrión) | Media | `GameHud.HandState.Pending` + dedos a medio cerrar (`HandRig`); aviso «No se pudo agarrar» si el anfitrión no confirma en 1,5 s | completo | C, B, L (`guesttoggle_sim`: pendiente 653 ms y confirmado) | H |
| Motivo de cierre bloqueado con orientación | Alta | `VanDoors` + rombo | completo | E (`StuckRecoveryTests`), B | H |
| Prioridad de E con compañero caído, carrito, mano anclada y camioneta | Media | `PlayerHands.ChooseInteract` (regla pura): compañero caído que se mira > mano anclada que se mira > camioneta > carrito; objetivo fijo mientras se mantiene E; línea de visión; cambiar de objetivo reinicia el progreso | completo (implementación) / verificación parcial | E (los cuatro a la vez y retención con E; mano anclada en el bar: aviso = acción, mirar a otro lado corta el progreso, mantener suelta) | Compañero caído real en 2 procesos (L) |

## Cooperación y tío

| Requisito | Prio. | Implementación | Estado | Evidencia | Próxima acción |
| --- | --- | --- | --- | --- | --- |
| 1–4 jugadores × 3 dificultades | Alta | `RoundDirector`, `LabBalance` | completo (automático) | L: 12 combinaciones con autopiloto (sesión 9) + coop/party4 en cada candidata | R2 y H con 4 personas |
| Ventajas de cooperar (tirones, anclaje, carrito, levantar, cierre) | Alta | Física de agarres, `HandleInteract` | parcial | E/L automáticos | H: medir si se siente la ventaja |
| Marcador contextual (tío/carrito/camioneta/punto) con emisor y límite | Media | `CoopMarkers` + `PlayerControls.MarkRpc` (botón central / cruceta arriba, remapeable; ayuda de puntería 0,6 m; uno por jugador, 6 s, 0,75 s entre marcas, alcance 30 m validado por el anfitrión; en el borde si queda fuera de pantalla; se limpian al reiniciar) | completo | C, E, L (`marks` 4/4: lo reciben anfitrión e invitado con el mismo nombre y color, rechazos por frecuencia y alcance en el anfitrión, caduca, el reinicio limpia), B (auditoría `hud_mark`) | H |
| Identificación de compañeros (nombre, color, caída) | Media | `CoopMarkers`: nombre con color por jugador sobre cada compañero; «CAÍDO: ayudalo» en rojo | parcial | C, L (color por dueño igual en anfitrión e invitado, `marks`) | Captura con 2+ procesos de la etiqueta de un compañero caído |
| Ataques legibles: preparación, zona real, un impacto por jugador, recuperación | Alta | `NpcBrain`, `AttackTelegraph`, `UncleAttackPose`. Sesión 14: las poses se generaban hacia el frente viejo (la nuca); el arco seguía al cuerpo y el impacto se medía desde el pecho; las propias partes del tío tapaban su golpe. Ahora arco e impacto usan la misma dirección y el mismo punto | completo | E (`AttackSignalTests` 51/51 en laboratorio, articulado y bar; jugador delante, a los lados y detrás), B, L | H |
| Disponibilidad de extremidades y miembro elegido (GDD) | Media | `NpcBrain.ChooseSide`: el anfitrión elige un brazo/pierna LIBRE del lado del objetivo (lado anatómico medido en reposo), lo replica con la dirección; poses izquierda y derecha; si le sujetan ese miembro durante la preparación, el golpe se corta («lo frenaron») sin cambiar de miembro | completo | C, E (6 casos de sujeción con la selección normal; el miembro elegido avanza 0,33–0,69 m hacia la zona anunciada y el otro ≤ 0,11 m; el jugador dentro del arco recibe el golpe) | L/H: notar que sujetar brazos protege |
| Transición tras desconexión | Media | `RoundDirector.UpdatePartySize`: el tiempo extra del grupo más chico llega en el acto; resistencia y ataques una vez a los 10 s; transcurrido conservado; mínimo 60 s restantes; aviso localizado para todos. **Limitación:** la gracia no es reconexión (la admisión rechaza entrar con la ronda empezada) | completo | E (5 casos), L (`grace`: gracia 10 s, anfitrión sigue jugando, agarres liberados, un solo ajuste; `graceend`: baja con 6,9 s restantes, +25 s ya, termina después del ajuste) | R2 |
| Suavizado visual independiente de los FPS (manos, pose del tío, barra de energía) | Media | `Smooth.Factor` (igual a 60 fps que antes) | completo | C | — |
| Rescates técnicos registrados | Media | `PhysicsWatchdog.RoundRescues` en `[Round] SUCCESS/TIMEOUT … rescates=N` | completo | C, L (rescates=0 en `grace`/`graceend`) | — |

## Red y ciclo

| Requisito | Prio. | Implementación | Estado | Evidencia | Próxima acción |
| --- | --- | --- | --- | --- | --- |
| Anfitrión autoritativo, sala, LISTO, dificultad del anfitrión, rechazo con motivo | Alta | NGO + `RoundDirector` + `NetSession` | completo | E, L (12 escenarios), R1 | R2 |
| Relay con código; cancelar | Alta | `RelayConnector`; `NetSession.CancelledWhileWaiting` descarta una respuesta tardía tras cancelar (y el coroutine muere con la escena) | completo | E, R1 (`MQRGGG`, `69e9c6f`); cancelación tardía: C (revisión de código, sin prueba automática: requiere el servicio) | R2 |
| Pérdida del anfitrión, desconexiones con manos ocupadas | Alta | `NetSession`, `PlayerControls.ReleaseTimeout` | completo | L (`hostkill` 10,1 s, `clientdrop`, `grace`) | R2 |
| Validación de intenciones del invitado (finitos, movimiento ≤ 1, ángulos acotados, solo botones de juego) | Alta | `PlayerIntent.Sanitize` en `SubmitIntentRpc` | completo | C, E (`NetIntentTests`) | L (regresión de red) |
| Compatibilidad de versión en la admisión | Media | `VersionTag` (Application.version/ProtocolVersion) | completo | E (`AdmissionTests`) | — |

## Presentación, audio y rendimiento

| Requisito | Prio. | Implementación | Estado | Evidencia | Próxima acción |
| --- | --- | --- | --- | --- | --- |
| Bar nocturno, tío con ropa y gorro, carrito y camioneta propios | Alta | `BarDressing`, `Finishing` | completo | B (capturas) | **H** |
| Manos del jugador con forma y estados | Alta | `HandRig`: palma, dedos y pulgar, puño de manga con color de estado; dedos abiertos / a medias (pendiente) / cerrados (agarrando); con esfuerzo el puño tiembla y se tiñe; caído o levantándose, bajan abiertas | completo | C, B (capturas) | H |
| Malla y expresión del tío | Alta | Malla del kit (facetada) + `UncleFace`: cejas y boca por estado (enojado al preparar/golpear, cejas arriba al recuperarse o caer). **Corregido:** la cara del kit miraba hacia atrás de la raíz, así que el tío preparaba los golpes y escapaba de espaldas (`NpcBody.FaceSign`) | parcial | B (capturas `cara_*.png`: gira la cara hacia el jugador y frunce el ceño), E (`Windup_TurnsTheFaceTowardsThePlayer`) | Malla con más detalle (fuera de alcance de esta pasada); H |
| Interfaz 720p–21:9, texto 100–150 %, ES/EN; guía, marcador y cambio de tamaño en partida; el subtítulo no tapa la cara del tío | Alta | `Ui`, `UiAudit` (estados nuevos `hud_guide`, `hud_mark`, `hud_textsize_live`), `GameHud.PlaceSubtitle` | completo | B (`8417620`: ES 15/15, EN 15/15; rótulo acortado «Volumen de avisos» incluido) | H de legibilidad |
| Audio por categorías con volumen separado | Media | `GameAudio.CategoryVolume`: general × efectos (agarres, golpes, puertas) o × avisos (señales de ataque, marcas, fin de ronda); Opciones con dos controles nuevos | completo | C, E (persistencia) | Voz y música cuando existan fuentes |
| Voz y música | Baja | — | pospuesto | sin fuentes con licencia | Requiere grabaciones o compra (dueño) |
| Calidad Alta/Baja, detección de integrada | Media | `GraphicsQuality` | completo | B (banco RTX/Iris) | Banco a batería (H/dueño) |
| Banco reproducible y tandas protegidas | Media | `Bench.cs`, `perf_bench.ps1`, `Guard.psm1` | completo | B | — |

## Producción y rejugabilidad (sesión 15)

| Requisito | Prio. | Implementación | Estado | Evidencia | Próxima acción |
| --- | --- | --- | --- | --- | --- |
| Respuesta inmediata al clic y sensación de agarre (contacto, confirmación, tensión, resbalón, liberación) | Alta | `HandRig.Pulse`, `GameAudio` (grab_try, grab, strain, slip, release) | completo (implementación) / verificación parcial | C, B (capturas) | **H: ¿se siente?** |
| Carrito físico (ruedas, traqueteo, golpes contra el marco) | Media | `GameAudio.LoopSources` (cart_roll por velocidad, cart_bump) | completo | C | H |
| Final con peso (portazo, pestillo, remate breve, «Otra ronda» enseguida) | Media | `latch`, `win_sting`, resultados existentes | completo | C, B (clip `cierre`) | H |
| Reacciones del tío a eventos reales (camioneta, levantarse, golpe frenado, vasos) | Media | `UncleDialogue` + DLG_101–109 | completo | C, B (subtítulos en clips) | Voz grabada (guion listo) |
| Bar que reacciona: torre de vasos que distrae al tío | Media | `BarProps` (anfitrión decide, RPC a todos, una vez por ronda, se rearma) | completo | E (`ReplayFeatureTests`) | L con 2+ procesos; H |
| Desafíos opcionales: Sin carrito, Ruta estrecha | Media | `Challenges` (modo replicado; barricada estática igual en todos los pares; ruta alternativa) | completo | E (ganan solo: 21,7 s y 40,4 s) | Viabilidad con 4 personas (H) |
| Parte de extracción con datos reales y vista para captura | Media | `RoundStats` (anfitrión registra, RPC a invitados), `GameFlowUI.CleanView` | completo | E, B (clip `cierre`: «Cerraron con 9 s de margen») | H: ¿lo comparten? |
| Ropa del tío con volumen y costuras; luces que no laven; malla sin facetas | Alta | `UncleOutfit`, `BarDressing`, `Finishing`; sesión 16: normales calculadas (60°) en la malla del kit | parcial | B (antes/después sesión 15 y 16) | Modelado nuevo (silueta, manos con dedos) |
| Camioneta con detalle y luz que orienta | Alta | `VanDetails` | completo (implementación) | B (capturas con 4 procesos) | H |
| Barra, puerta y carrito: vetas, desgaste, herrajes | Alta | `Finishing` (sesión 16): acero cepillado, pintura gastada, goma, madera oscura con normales; moldura, desgaste y apoyapiés en la barra; marco, placas y bisagras en la puerta | completo (implementación) / verificación parcial | B (primeros planos `docs/capturas/sesion16/`) | **H** de cerca |
| Demo distribuible + material de tienda | Alta | `Builds/UncleExtraction_Demo_s15.zip`, `docs/lanzamiento/` | completo (borradores) | — | Revisión del dueño; video con OBS/ffmpeg |
| Destrabe del autopiloto (carrito en la puerta) | Media | `LabAutopilot.MoveWorld` | completo | E (12/12 intentos completos) | — |

## Sesión 17 (auditoría y demo)

| Requisito | Prio. | Implementación | Estado | Evidencia | Próxima acción |
| --- | --- | --- | --- | --- | --- |
| Sin carrito persiste en rondas seguidas; Estándar devuelve un carrito jugable | Alta | `Challenges` (reaplica por ronda replicada), `TransportCart.Teleport` | completo | E (3× Sin carrito → Estándar gana con carrito), L (`cycle` 9/9) | H en red |
| Torre de vasos rearmada en todos los pares, una caída por ronda | Alta | `BarProps` + `TowerFallRpc(ronda)` | completo | E, L (`cycle`) | H |
| Versión y protocolo con una sola fuente; incompatibilidad con mensaje claro y reintento | Alta | `GameVersion` 0.17.0/2, admisión `NetSession.Decide` | completo | E, L (`compat` en los dos sentidos contra la demo s16) | R2 con dos versiones |
| Nombres por bytes UTF-8 reales; parte estructurado traducido en cada cliente | Alta | `PlayerMotor.Sanitize`, `Loc.Escape`, `RoundStats.Report` | completo | E (tildes, emoji, `\|`, `=`, límite), L (parte llega al invitado) | H con nombres raros en red |
| Disputa por un agarre: un solo dueño, evidencia atada al intento; caso de juego normal | Alta | `LabAutopilot.RaceRun`, `net_smoke` `race`/`race_natural` | completo | L (`race` 5 PASS; `race_natural` 12/12 nunca dos a la vez, disputa real 5/12) | R2 con personas |
| Las pruebas no modifican el proyecto | Alta | `.mat` del tío sin `_EMISSION`; `NpcPresentation` en copias; `run_tests.ps1` | completo | E (PlayMode completa 168/168 con assets sin cambios) | — |
| Tandas aisladas: bloqueo atómico, solo procesos propios, puerto libre | Media | `Guard.psm1` (mutex), `net_smoke` | completo | autoprueba PASS | — |
| Build de jugadores y ZIP verificable | Alta | `BuildTools -uePlayer`, `make_demo_zip.ps1` | completo | B (copia descomprimida: menú, argumentos de QA ignorados), SHA-256 | H |
| Ayudas que dicen el botón, quién tiene el punto y siguen al dispositivo | Alta | `GrabFeedback`, `InputBindings.PadActive` | completo (implementación) | E (`Hints_FollowTheDeviceInUse`) | **H**: primer agarre < 60 s |
| Secuencia audiovisual completa del agarre (bloque 3) | Media | parcial: anillo, línea de tensión, sonidos de sesión 15 | parcial | — | Diseño y prueba con personas |
| Campana de última ronda (distracción con costo) | Media | `BarProps.RingBell`, `NpcBrain.HearBell` | completo (implementación) | E (suena una vez, espera, tope, rearme) | H de a dos; L en red |
| Otra ronda con nueva condición inicial (karaoke) | Media | `ChallengeMode.Karaoke` | completo (implementación) | E (×2, vuelve a la barra, gana arrastrando 31,8 s) | H; L con grupo |
| Rendimiento comparable y sesión larga de 4 | Media | banco de vistas fijas, `long4` | completo (medido) | B (Alta/Baja igual o mejor que s16), L (`long4` 10/10, +≤1 MB) | Profiler de Unity; GPU integrada confirmada |
| Arte del bloque 5, remate del cierre, clip con sonido | Media | — | faltante | — | Siguiente sesión; grabador con permiso |

## Bloqueos externos

| Tema | Paso mínimo del dueño |
| --- | --- |
| Prueba entre dos equipos y redes (R2) | Otra PC: Jugar con amigos → Crear sala por internet / Unirse con código. |
| Revisión humana (H) | `REVISION_CANDIDATA.txt` y `PRUEBA_HUMANA.txt` junto al ejecutable. |
| Steam | Cuenta de Steamworks, pago de Steam Direct, App ID. |
| IL2CPP | Visual Studio 2022 con «Desarrollo de juegos con C++». |
| Voz y música | Grabaciones propias o recursos con licencia comercial (guion: `docs/GUION_VOZ.md`). |
| Video del tráiler y clips en MP4 | Permiso para instalar ffmpeg u OBS (los clips actuales son WebP de juego real). |
