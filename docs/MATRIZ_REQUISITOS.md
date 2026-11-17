# Matriz de requisitos — alcance inicial (alcohol, un bar, camioneta fija, 1 a 4 jugadores)

Fuentes: GDD y especificación técnica (`preproduction/uncle_extraction/`), instrucciones vigentes del dueño (sesiones 9 a 13) y `CLAUDE.md`. Se actualiza en cada bloque. Última revisión: sesión 13 (2026-11-17).

**Niveles de evidencia:** **C** = existe en código y compila · **E** = probado en el editor (PlayMode/EditMode) · **B** = probado en la build · **L** = procesos en localhost · **R1** = Relay desde una PC · **R2** = dos equipos y redes distintas · **H** = revisado por una persona.
**Estados:** completo · parcial · faltante · pospuesto (justificado) · bloqueado.

## Controles y entrada

| Requisito | Prio. | Implementación | Estado | Evidencia | Próxima acción |
| --- | --- | --- | --- | --- | --- |
| Moverse con WASD **y flechas**, diagonal normalizada, alternativas sin sumar velocidad | Alta | `LabInput.RealMove` (sesión 13) | completo | C, E (`RealInputTests`, teclado simulado) | B en la build final; H |
| Movimiento remapeable | Alta | `InputBindings` (`MoveKey`) + Opciones | completo | C, E (reasignar avanzar a I) | H |
| Dos manos independientes (clic izq./der.) | Alta | `PlayerHands`, `GrabIntent` | completo | C, E (`RealInputTests`), L | H |
| Agarre **mantener / alternar** (local, persistente, igual en anfitrión y cliente) | Alta | `GrabIntent`, Opciones → Agarre | completo | C, E (alternar: agarra, suelta, se desarma sin objetivo) | L con cliente en alternar; H |
| Q suelta de forma fiable; el clic sostenido no reagarra | Alta | `GrabIntent` (rearmado al soltar el clic) | completo | C, E | H |
| Menús, pausa, campos de texto, reasignación y ventana sin foco no disparan acciones | Alta | `GameFlowUI.BlockingInput` (+ `Ui.Typing`), `PlayerControls.Pressed` | completo | C, E (escribir R/E/Espacio/W; pausa con agarres) | Perder/recuperar foco en la build (H) |
| Reinicio: lo controla el anfitrión; el invitado lo pide al terminar la ronda; R solo diagnóstico | Alta | `RoundDirector` (sesión 13) | completo | C | L: `repeat`/`lose` en la regresión de red |
| Opciones: Aplicar/Descartar incluye controles; captura cancelable; reservadas (Esc, Tab, flechas) | Alta | `OptionsView` + borrador de `InputBindings` | completo | C, E | H con mando |
| Preferencias validadas (dificultad, rangos, finitos, idioma, rutas) | Alta | `GameSettings.Validate`, `InputBindings.Valid` | completo | C, E (`DamagedPreferences_AreValidatedBeforeUse`) | — |
| Tamaño de texto e idioma durante la partida | Media | `GameHud.Rebuild`, `GameFlowUI.RebuildPanels` | completo | C, E | H |
| Mando: navegación de menús | Media | `InputSystemUIInputModule`, `GamepadBack` | completo | E (`MenuNavigationTests`) | H con un mando real |
| Comodidad: reducir inclinación, sensibilidad, FOV (vertical) | Media | `GameSettings.ReduceMotion`, Opciones | completo | C | H |

## Aprendizaje y claridad

| Requisito | Prio. | Implementación | Estado | Evidencia | Próxima acción |
| --- | --- | --- | --- | --- | --- |
| Primera partida guiada, omisible, repetible desde pausa, local en cooperativo | Alta | `FirstSteps` (9 pasos que avanzan con la acción real) | parcial | C | E de avance de pasos; B capturas; **H: primer agarre en el primer minuto** |
| «Cómo jugar» breve y fiel (teclas reales, modo de agarre) | Media | `FrontendMenu` HowTo | completo | C | H |
| Feedback de mira: válido / ocupado / fuera de alcance | Alta | `GrabFeedback` | completo | E, B (capturas) | Sonido distinto por estado (faltante) |
| Agarre pendiente vs confirmado (autoridad del anfitrión) | Media | — | faltante | — | Marca «agarrando…» en el cliente hasta la confirmación |
| Motivo de cierre bloqueado con orientación | Alta | `VanDoors` + rombo | completo | E (`StuckRecoveryTests`), B | H |
| Prioridad de E con compañero caído, carrito, mano anclada y camioneta | Media | `PlayerHands.HandleInteract` | parcial | C | Prueba dirigida con los cuatro casos juntos |

## Cooperación y tío

| Requisito | Prio. | Implementación | Estado | Evidencia | Próxima acción |
| --- | --- | --- | --- | --- | --- |
| 1–4 jugadores × 3 dificultades | Alta | `RoundDirector`, `LabBalance` | completo (automático) | L: 12 combinaciones con autopiloto (sesión 9) + coop/party4 en cada candidata | R2 y H con 4 personas |
| Ventajas de cooperar (tirones, anclaje, carrito, levantar, cierre) | Alta | Física de agarres, `HandleInteract` | parcial | E/L automáticos | H: medir si se siente la ventaja |
| Marcador contextual (tío/carrito/salida) con emisor y límite | Media | — | faltante | — | Implementar (bloque D) |
| Identificación de compañeros (nombre, manos, caída) | Media | Nombres en sala | parcial | L | Etiqueta sobre cada compañero |
| Ataques legibles: preparación, zona real, un impacto por jugador, recuperación | Alta | `NpcBrain`, `AttackTelegraph`, `UncleAttackPose` | completo | E (`AttackSignalTests`), B, L | H |
| Disponibilidad de extremidades y espacio para atacar (GDD) | Media | — | faltante | — | Bloque D: brazo controlado = sin manotazo de ese lado |
| Transición tras desconexión (no recalcular el grupo al instante) | Media | `RoundDirector.autoPartySize` (inmediato) | faltante | — | Bloque D: período de gracia de 10 s |
| Rescates técnicos registrados | Media | `PhysicsWatchdog` | parcial | E | Contador en el registro de cada ronda |

## Red y ciclo

| Requisito | Prio. | Implementación | Estado | Evidencia | Próxima acción |
| --- | --- | --- | --- | --- | --- |
| Anfitrión autoritativo, sala, LISTO, dificultad del anfitrión, rechazo con motivo | Alta | NGO + `RoundDirector` + `NetSession` | completo | E, L (12 escenarios), R1 | R2 |
| Relay con código; cancelar | Alta | `RelayConnector` | completo | E, R1 (`MQRGGG`, `69e9c6f`) | R2; operación tardía tras cancelar (prueba) |
| Pérdida del anfitrión, desconexiones con manos ocupadas | Alta | `NetSession`, `PlayerControls.ReleaseTimeout` | completo | L (`hostkill` 10,1 s, `clientdrop`) | R2 |
| Compatibilidad de versión en la admisión | Media | `VersionTag` (Application.version/ProtocolVersion) | completo | E (`AdmissionTests`) | — |

## Presentación, audio y rendimiento

| Requisito | Prio. | Implementación | Estado | Evidencia | Próxima acción |
| --- | --- | --- | --- | --- | --- |
| Bar nocturno, tío con ropa y gorro, carrito y camioneta propios | Alta | `BarDressing`, `Finishing` | completo | B (capturas) | **H** |
| Manos del jugador (hoy cubos) | Alta | — | faltante | — | Bloque E: manos con forma y estados |
| Malla y expresión del tío | Alta | malla del kit facetada | parcial | B | Bloque E |
| Interfaz 720p–21:9, texto 100–150 %, ES/EN | Alta | `Ui`, `UiAudit` | completo | B (15/15 por idioma) | H de legibilidad |
| Audio por categorías con volumen separado | Media | `SfxSynth`, `GameAudio` (volumen general) | parcial | E | Volumen de efectos / voz / música |
| Voz y música | Baja | — | pospuesto | sin fuentes con licencia | Requiere grabaciones o compra (dueño) |
| Calidad Alta/Baja, detección de integrada | Media | `GraphicsQuality` | completo | B (banco RTX/Iris) | Banco a batería (H/dueño) |
| Banco reproducible y tandas protegidas | Media | `Bench.cs`, `perf_bench.ps1`, `Guard.psm1` | completo | B | — |

## Bloqueos externos

| Tema | Paso mínimo del dueño |
| --- | --- |
| Prueba entre dos equipos y redes (R2) | Otra PC: Jugar con amigos → Crear sala por internet / Unirse con código. |
| Revisión humana (H) | `REVISION_CANDIDATA.txt` y `PRUEBA_HUMANA.txt` junto al ejecutable. |
| Steam | Cuenta de Steamworks, pago de Steam Direct, App ID. |
| IL2CPP | Visual Studio 2022 con «Desarrollo de juegos con C++». |
| Voz y música | Grabaciones propias o recursos con licencia comercial. |
