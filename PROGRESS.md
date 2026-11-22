# Estado del proyecto

## Verificación antes de fusionar · UE-52

El workflow `Verificación del proyecto` revisa el diff, la versión fijada de Unity, GameVersion, las dependencias directas del manifiesto y las escenas habilitadas. Registra el SHA en el log de GitHub Actions. También se puede ejecutar localmente con PowerShell 7 mediante `./tools/windows/check_project.ps1`.

`run_tests.ps1` conserva el guard de assets y usa el editor fijado en `ProjectVersion.txt`. En un Windows con licencia autorizada, ejecutar EditMode y PlayMode con `-Tag <sha>` y guardar los XML y logs de `.local-setup/logs/unity/`, incluido el recuento de omitidas. Ejecutar luego `BuildTools.BuildWindows` en el mismo checkout y guardar el log y VERSION.txt. Los pasos deben correr en serie. Si se reutiliza la caché de Unity, su clave debe incluir la versión del editor y el hash de `packages-lock.json`.

**PASS local:** verificación estática de la configuración. **NOT_RUN:** ejecución remota desde un PR, pruebas y build de Unity con licencia.

## Candidata integrada de Windows · 54b5246

Fuente de las builds de jugadores, QA y desarrollo `54b5246bc9e7f3d793ea6ec9faeb0a1c74840eef`. Unity **6000.3.28f1**, Windows x64 Mono, juego 0.17.0 y protocolo 2. El historial de este corte contiene el HUD del PR #1, el desenganche y el reinicio de agarre del PR #3 y los materiales del PR #2.

| Comprobación de este corte | Resultado |
| --- | --- |
| EditMode completo | **75 PASS**, 1 omitida. El proyecto ya está vinculado a Relay. |
| PlayMode completo | **173 PASS**, 1 omitida. La variante de cápsula central del laboratorio no tiene pie independiente. Los assets versionados quedaron intactos tras ambas tandas. |
| Copia extraída de jugadores | **PASS** de arranque, menú, Opciones y persistencia del perfil. Los argumentos de laboratorio, auditoría, cámara, rendimiento, sonda, autopiloto y capturas no activaron herramientas de QA. |
| Extracción desde la copia extraída | **PASS** en ronda estándar Normal de dos procesos. Abrí la sala e inicié la ronda en el ejecutable de jugadores. Un cliente de QA del mismo SHA hizo el recorrido por localhost. El anfitrión de jugadores registró el primer agarre, desenganche, tío dentro de la camioneta y cierre completo en **39,2 s**, con victoria en pantalla. |
| Auditoría de interfaz | **30/30 PASS** de disposición, 15 por idioma. Dos perfiles, cinco formatos de pantalla y texto al 100 %, 125 % y 150 %. En las capturas a 720p y 150 %, el aviso de Contextual se lee y el rótulo 3D de Detallado pierde contraste sobre la ropa iluminada. Las tarjetas de mano se leen. |
| Regresión localhost | **20/20 escenarios verificados** con las builds QA y desarrollo del mismo SHA. En la primera tanda hubo 18 PASS, un FAIL de compatibilidad y un BLOCKED de ingreso tardío. Las dos repeticiones de cada uno pasaron. En la disputa ambos apuntaron al cinturón y el anfitrión registró un titular y un rechazo `SocketBusy`. |
| Regresión ampliada localhost | **16/16 PASS**. Combinadas con la tanda base cubren 1–4 jugadores en tres dificultades. Se verificaron caída del anfitrión en 10,1 s, ocho victorias consecutivas en solitario y diez victorias concordantes en cada uno de cuatro procesos, sin excepciones. |
| Relay en una PC | **PASS** en dos procesos. Ambos terminaron con el tío asegurado y las puertas cerradas. Anfitrión 67,2 s e invitado 67,1 s. RTT 77–85 ms. |
| PC sin Unity, LAN y Relay entre equipos, mando físico y grupos nuevos | Pendientes de equipo o participantes. |

ZIP `UncleExtraction_Demo_54b5246_Windows_x64.zip`. SHA-256 `9167da7fa203beaa3dcbd216b4fbfe4b349c116bae35dfcf4a717b63f39f5bbe`. La copia extraída está en `UncleExtraction_Demo_54b5246_Jugar`. El ZIP anterior sigue disponible como referencia. XML, logs de builds, arranque y extracción en `.local-setup/qa/main_54b5246/`.

El registro de red conserva la tanda inicial en `localhost_all_first.csv`, las repeticiones en `localhost_rerun.csv` y la verificación por escenario en `localhost_20_verificados.csv`. Los archivos `localhost_qa.csv` y `relay_una_pc.csv` registran la tanda ampliada y Relay por separado. Los logs están en `red/`, `compat_first/` y `latejoin_first/`. Los CSV y capturas de interfaz están en `ui_es_UY/` y `ui_en/`.

- **Compatibilidad.** El cliente antiguo recibió el rechazo de versión, pero el script lo cerró antes de observar el regreso al menú. La espera ahora termina al observar el rechazo y el regreso o al vencer el límite. Dos repeticiones con los mismos binarios, en ambos sentidos, confirmaron los tres pasos.
- **Ingreso tardío.** El anfitrión rechazó correctamente al invitado y ganó en 21,6 s. En el cierre automático posterior hubo una excepción nativa `0xc0000005` en `UnityPlayer.dll`. La causa queda sin confirmar. Dos repeticiones de la misma prueba pasaron sin reproducir el cierre anómalo.

**Decisión de entrega.** Candidata apta para playtest. La aprobación para distribución queda pendiente de una ronda con input físico en una PC sin Unity, LAN, Relay entre redes independientes y dos grupos nuevos. La [prueba S18 de HUD](docs/PRUEBA_HUD_S18.md) orienta la comprobación con mando físico.

**Próxima prueba humana.** Abrí este ZIP en una PC de Windows sin Unity y completá una ronda estándar con input físico. Anotá el tiempo al primer agarre y cualquier ayuda necesaria. Después alterná los dos HUD desde pausa, comprobá español e inglés con texto al 150 % y probá un mando físico. Hacé una ronda de LAN por IP y otra por Relay en redes independientes con la segunda PC. [Guion de la candidata](docs/VOLVER_A_PROBAR_CANDIDATA.md)

Última actualización: 2026-11-19, integración de la candidata y sesión 19 (materiales del bar), sesión 18 (variante HUD y agarre), sesión 17 (correcciones de la auditoría, campana, desafío karaoke, demo `UncleExtraction_Demo_s17.zip`). Sesión 16 (malla del tío, materiales de barra/puerta/carrito, race). Sesión 15 (producción: manos, sonido, tío, camioneta, bar que reacciona, desafíos, parte de extracción, material de lanzamiento). Detalle de sesiones anteriores (sin reescribir, con todos los FAIL): `docs/HISTORIAL.md`.

## Sesión 19 · materiales del bar

**Rama de evaluación** `codex/s19-materiales-bar`, desde `main` en `863888b`. Madera barnizada y desgastada en el canto, baldosas con juntas y marcas de paso, revoque mate y roces alrededor de la manija y el marco. Los PNG originales y sus metadatos están versionados e integrados en Bar_LastRound.

**Verificación:** recorrido de arrastre desde la barra hasta asegurar la camioneta en 21,8 s, PlayMode dirigido 6/6 y comparación de escena con los colliders, cuerpos rígidos, bisagra y luces idénticos a la referencia. [Capturas comparables, origen y pasos de reproducción](docs/MATERIALES_BAR_S19.md).

**Integración con la candidata:** 42/42 en PlayMode sobre `9abd55e827e4c9996031b457328d5a5dd45b45b3`. Se comprobó la geometría del bar, el cambio de HUD en pausa, el desenganche y el ciclo de la guía. Unity no modificó archivos versionados. `tests_PlayMode_main_integracion_9abd55e.xml`.

## Desenganche de la baranda · revisión de la candidata

Stefano probó la copia local y reportó que costaba descubrir cómo desenganchar al tío. Cerca y con línea de visión hacia la mano o su punto de apoyo, ambos perfiles del HUD ahora indican hacia dónde mirar y qué tecla mantener. La acción alcanza hasta 2,1 m desde la cámara. El cono pasa de 30° a 46° y llega a 56° mientras se mantiene la tecla sobre el objetivo. La duración base sigue en 0,9 s.

- **FAIL antes del cambio.** Sin apuntar con precisión a la mano, el HUD quedaba vacío a pesar de estar cerca. `tests_PlayMode_demo_anchor_red_cleanport.xml`.
- **38 PASS en PlayMode** con `RealInputTests|InteractPriorityTests|GuideLifecycleTests`. La regresión comprueba los dos HUD, la tecla reasignada, el progreso al mantener, la cancelación al soltar y el desenganche con un leve movimiento de cámara. El caso previo comprueba que mirar claramente hacia otro lado cancela el progreso. `tests_PlayMode_demo_anchor_targeted.xml`.
- **Prueba humana.** Stefano confirmó que pudo resolver el desenganche. El desafío **Arranca en el karaoke** permite repetir ese paso desde el inicio. El recorrido completo de la ronda estándar sigue en el guion de prueba.

## Candidata de demo de Windows · corte del HUD

Rama `codex/demo-candidate` desde el `main` de GitHub `863888be7d82edf3307586197624858b28976178`. El PR #1 está fusionado con cabeza `82c28e123adde35455ba0a6d8064a75e6823c3c7`. Stefano confirmó que esa build del HUD funciona. El PR #3 se fusionó con cabeza `4565749348de4e9d410703145fae7014abbd22a2`. S19 se verificó en el PR #2 sobre `5a478cb21be923b07d19c8a96c774cb6c319a6d7` antes de integrar ambos cambios. Las ramas de selección, frenado y cámara siguen disponibles para comparaciones.

| Comprobación sobre el corte de `main` | Resultado original y reproducción |
| --- | --- |
| Intento pendiente en Alternar durante un reinicio | **FAIL.** Mirar hacia arriba sin candidato, pulsar la mano izquierda con Alternar, reiniciar mientras el intento sigue armado. `PendingToggle_IsCancelledByRoundRestartBeforeConfirmation` confirma que la intención de la ronda anterior seguía activa. XML y log `tests_PlayMode_demo_red_pending.*`. |
| Argumentos de auditoría en la build de jugadores | **FAIL.** Abrir jugadores `863888b` con `-ueUiAudit <carpeta>` ejecuta la auditoría, entra al bar y genera capturas. Registro `player_qa_argument_FAIL.log`. |

La candidata rearma ambas manos cuando cambia la identidad del jugador local o el número de ronda. Los argumentos de diagnóstico toman una lista vacía en jugadores. La prueba de arranque del ZIP incluye auditoría, recorrido de cámara, rendimiento, sonda, autopiloto y captura de menú. `VERSION.txt` incluye el SHA completo.

**SHA de jugadores, QA y desarrollo** `42dcfcd0d7b8861b89cdc374d9a1eee81a449bc5`, Unity **6000.3.28f1**, Windows x64 Mono. ZIP `UncleExtraction_Demo_42dcfcd_Windows_x64.zip`, SHA-256 `8ab1916922116067d4a2770aaa251e8d604beafb3f2bb65a5878160281612547`. El ZIP de s17 queda como referencia. [PR #3](https://github.com/Stefano936/uncle-extraction/pull/3)

| Verificación de la candidata | Estado |
| --- | --- |
| EditMode completo | **75 PASS, 1 omitida.** El caso sin proyecto Relay vinculado no aplica al proyecto configurado. |
| PlayMode completo | **172 PASS, 1 omitida.** El laboratorio de cápsula central no tiene un pie independiente. Assets versionados iguales antes y después de ambas tandas. |
| Copia extraída de jugadores | **PASS** de arranque, opciones, persistencia de HUD y exclusión de herramientas de QA. Entró al bar con ambos perfiles. Falta completar la extracción con input físico. |
| Extracción individual automática | **PASS** por arrastre, 26,9 s. Usó la build QA de este SHA. |
| Auditoría de UI | **30/30 PASS**, 15 por idioma. Cinco resoluciones o aspectos y texto al 100 %, 125 % y 150 %. La inspección visual encontró contraste bajo en el rótulo 3D de Detallado sobre ropa muy iluminada. |
| Localhost | **20/20 PASS**, con las rutas de ejecutable verificadas en los logs. `race` confirmó candidato `belt` en las dos vistas, un titular y un rechazo `SocketBusy` del otro jugador. El ciclo pasó 9/9 y las desconexiones liberaron agarres. |
| Matriz ampliada de localhost (UE-02) | **PASS** en las 12 combinaciones de 1–4 jugadores y tres dificultades. `hostkill` detectó la caída en **10,1 s**, con retorno al menú y sin excepciones. `long4` repitió **10 rondas ganadas y concordantes en los cuatro procesos**, sin excepciones. |
| QA complementaria de localhost | **5/5 PASS.** Ocho victorias consecutivas en solitario, sin excepciones ni crecimiento sostenido de memoria. Rendimiento medido con uno y cuatro procesos, y a 1920×1080 en Alta y Baja. En dos repeticiones de la disputa guiada, ambos jugadores llegaron al cinturón y el anfitrión registró un solo titular. En dos carreras naturales hubo como máximo un titular, sin rechazo por el cinturón. |
| Relay en una PC | **PASS** en dos procesos. Ambos registraron victoria con el tío asegurado y las puertas cerradas. Anfitrión 40,2 s, invitado 40,1 s. RTT 84–101 ms. |
| LAN y Relay entre equipos | **NOT_RUN.** Requieren una segunda PC. Para Relay entre redes, usar dos redes distintas y guardar los dos logs. |

**S19** `5a478cb` pasó la revisión técnica aparte. Comparé tres pares a 1600 × 900 en Baja con luces, posiciones y ángulos iguales. Veta, juntas y puerta se distinguen. Los roces alrededor de la manija se leen de cerca. Los 55 archivos de arte y `.meta` coinciden tras regenerar, junto con las referencias de 6 materiales y 9 mallas. La cámara, luces, colisiones, bisagra y posiciones de juego coinciden con `863888b`. La extracción con carrito pasó en ambos cortes. [PR #2](https://github.com/Stefano936/uncle-extraction/pull/2)

El script de localhost usaba `$exe` para elegir el binario de la sonda. En PowerShell ese nombre sobrescribía `$Exe` y los casos siguientes usaban desarrollo bajo la etiqueta QA. Se cambió la variable y la etiqueta de los escenarios con latencia. La primera tanda quedó archivada. Se repitieron y pasaron los siete casos afectados. El CSV consolidado local `localhost_20_escenarios_verificados.csv` identifica el binario usado y la carpeta del registro por escenario. El árbol de Unity sigue siendo el del SHA compilado.

La primera corrida de `long4` informó aprobado con diez victorias del anfitrión y una por invitado. El script ahora pasa la repetición a los cuatro procesos y compara sus resultados por ronda. Se preservó la primera corrida. Los logs corregidos confirman los intentos 1–10, cierre completo y tío asegurado en cada proceso. El cambio se limita al script de QA. Las dos corridas usaron la build QA `42dcfcd`.

**Próxima acción exacta:** abrir la build extraída en **Jugar solo → Iniciar extracción**, alternar los dos HUD desde pausa con input físico y completar el recorrido hasta la victoria. Guardar el log con el resultado. La candidata está lista para playtest dirigido. La distribución requiere ese recorrido, el humo en otra PC sin Unity, dos PC para LAN y Relay, y grupos nuevos. Usar [el guion de la candidata](docs/VOLVER_A_PROBAR_CANDIDATA.md). En Trello quedaron completas UE-00 y UE-04. UE-02 tiene resultado publicado y 3/4 comprobaciones. Los adjuntos de UE-01 y UE-02 están preparados en la evidencia local. [UE-02](https://trello.com/c/a7HoL8uf)

## Sesión 18 · variante de HUD y feedback de agarre

**Rama de evaluación** `codex/s18-hud-agarre`, desde `main` en `c21dce7`. Builds Windows Mono QA y desarrollo de `dc892ce`, editor **6000.3.28f1**, versión 0.17.0, protocolo 2.

- **Contextual** es la opción inicial. En reposo quedan la mira y el reloj de perfil bajo. La pista central usa el dispositivo y la asignación en uso, identifica al titular de un agarre ocupado y da prioridad a los avisos de peligro y las interacciones. **Detallado** mantiene las tarjetas, barras de tensión y rótulos en el mundo.
- La mano se cierra parcialmente al pedir agarre y cierra del todo con la confirmación. La tensión tiene gesto de muñeca y sonido. El registro autoritativo distingue soltada y pérdida en el anfitrión. El invitado usa la intención local y la caída replicada para elegir el sonido, con un roce neutro para las causas indeterminadas.

| Verificación propia de la sesión 18 | Código o build | Resultado |
| --- | --- | --- |
| Builds Windows Mono QA y desarrollo | `dc892ce` | **PASS**, 0 errores |
| Auditoría rápida de interfaz en español e inglés | `dc892ce` | **6/6 PASS por idioma**, 720p y 21:9, texto 100/125/150 %, ambos perfiles. Se corrigió el cruce inicial del aviso de ataque con el reloj. |
| `guesttoggle`, `guesttoggle_sim` y `race` en localhost | `dc892ce` | **3/3 PASS**. Las sondas del invitado dieron 9/9 cada una, con confirmación a 68 y 610 ms, respectivamente. En la disputa hubo un solo titular simultáneo del cinturón. |
| EditMode | `dc892ce` | **75 PASS**, 1 omitida |
| PlayMode dirigido a HUD, entrada real, guía y prioridades de interacción | `dc892ce` | **40/40 PASS**. Los archivos versionados de Unity quedaron iguales en ambas tandas. |
| Regresión PlayMode completa, primera pasada | antes de `728da9f` | **169 PASS, 1 omitida, 1 FAIL** en la comprobación nueva del panel de energía. La comprobación pasó tras consultar la visibilidad efectiva del panel. |

Registros locales en `.local-setup/logs/unity/`, `.local-setup/logs/net/` y `.local-setup/shots/s18_final_*/`. Unity se ejecutó con una ruta corta al checkout tras detectar el límite de longitud durante la importación inicial de URP.

El [guion A/B y las hipótesis de la sesión 18](docs/PRUEBA_HUD_S18.md) reúnen la validación humana y la entrega a Claude.

## Estado vigente

**Build para probar** (local, no versionada): `Builds/UncleExtraction/UncleExtraction.exe`, con `VERSION.txt` (commit, cambios sin commitear, fecha, tipo), `LEEME.txt` (cómo jugar, controles) y `PRUEBA_HUMANA.txt` (guion corto de prueba con planilla). `Builds/UncleExtraction_Dev/` es la de desarrollo (simulador de red). Build de referencia anterior: `2fc2d5d` (respaldo local en `Builds/UncleExtraction_Backup_2fc2d5d/`).

**Qué se puede jugar:** Bar_LastRound solo o en cooperativo de 2 a 4 (internet por Relay con código de sala; red local por IP). Tío articulado de 11 cuerpos con poses de ataque; bar de noche con techo, luces cálidas, neones y calle; camioneta con carrocería propia; carrito con modelo propio; tío con ropa, materiales y gorro de fiesta; vaso señuelo, ayuda para levantarse, extracción con puertas. Interfaz del kit (menú, sala, HUD, pausa, opciones, resultados, errores), EN/es_UY, controles reasignables, opciones guardadas, mando en menús y juego. Sonido sintetizado y subtítulos del tío; sin voz ni música.

**Niveles de evidencia:** PM = PlayMode en el editor; EM = EditMode; AUTO = partida completa con entradas de jugador simuladas; NET = procesos de la build en **localhost**; VIS = auditoría y capturas reales de la build. Las pruebas humanas históricas tienen su propio [registro](docs/OBSERVACIONES_PRUEBAS.md).

### Sesión 17: correcciones de la auditoría, demo para compartir con resultados asociados

**Rama:** `s17-demo` (desde `s15-produccion`, auditada en `1ca19d7`; sin fusionar a `main`: lo decide Stefano). **Versión 0.17.0, protocolo de red 2** (una sola fuente: `GameVersion`; se ve en el menú, `VERSION.txt`, registros y admisión). La prueba humana anterior del dueño (demo s15) sigue como entrada propia en `docs/OBSERVACIONES_PRUEBAS.md`.

**Demo para compartir:** `Builds/UncleExtraction_Demo_s17.zip` (38,8 MB), build **de jugadores** de `b44386d` (solo menú y bar; sin sondas, autopiloto, capturas, laboratorios ni atajos de diagnóstico; `-ueScene`/`-ueAutopilot` se ignoran). SHA-256 `57d883e47b03dba0dac57be5c0d34c2daf352d1554e4d0308a73cda94c51362f` (también en `UncleExtraction_Demo_s17.zip.sha256`). Incluye `VERSION.txt`, `LEEME.txt`, `PRUEBA_HUMANA.txt` y `VOLVER_A_PROBAR.txt`. Generada y verificada con `tools/windows/make_demo_zip.ps1`: descomprimida en otra carpeta, llega al menú en 20 s sin excepciones y con argumentos de QA se queda en el menú (PASS). Demo anterior conservada: `UncleExtraction_Demo_s16.zip` y `Builds/UncleExtraction_Ref_s16/`. Builds de QA y desarrollo: `1bc221d` (mismo código de juego que la demo; la diferencia es solo el bloque `UE_PLAYER_BUILD`).

**Correcciones (bloque 1)**
- **Sin carrito en rondas seguidas:** el modo se reaplica en cada ronda y en todos los pares (visual, colisión y asa apagados; el anfitrión lo guarda y lo teletransporta por red). Volver a Estándar devuelve un carrito con física, en su lugar.
- **Torre de vasos** ligada al número de ronda replicado: se rearma en todos, una caída por ronda, eventos viejos descartados.
- **Compatibilidad de versión:** el protocolo de NGO queda fijo y la admisión compara versión/protocolo; el que entra con otra versión ve «Las versiones del juego no coinciden… actualizá y volvé a intentar desde la sala» y vuelve al menú (antes NGO cortaba sin motivo).
- **Nombres** por capacidad UTF-8 real (29 bytes, caracteres completos; tildes, emoji, `|` y `=` se conservan); mensajes codificados con escape. **Parte** estructurado (JSON por RPC, tope en bytes, traducido en cada cliente, solo el de la ronda actual) con un momento destacado real.

**Bloques 2–4**
- **Disputa por un agarre:** `race` exige evidencia del anfitrión atada a un intento (dos jugadores distintos, SocketBusy del segundo mientras el primero lo tiene, candidato local, IA restaurada). Nuevo `race_natural`: juego normal (IA activa, sin reubicar, 80 ms ±15 simulados) con métricas aparte. El autopiloto restaura `brainEnabled` y entradas al terminar, cancelar o reiniciar.
- **Pruebas que modificaban el proyecto:** causa encontrada — URP le quitaba `_EMISSION` al asset de la camisa la primera vez que se dibujaba en el editor (ningún script lo tocaba; confirmado con una sonda por cuadro y procesos de editor nuevos). Los `.mat` del tío ya no llevan la palabra clave; `NpcPresentation` la activa en copias en ejecución. `tools/windows/run_tests.ps1` compara los archivos versionados antes y después y **falla** si cambian.
- **Tandas aisladas:** bloqueo atómico (mutex con nombre; una tanda cancelada se recupera; solo libera el dueño) con `guard_selftest.ps1`; `net_smoke` ya no cierra procesos ajenos y usa el siguiente puerto libre. Error de herramienta encontrado: una variable de `race_natural` pisaba los argumentos de ventana (PowerShell no distingue mayúsculas) y el escenario siguiente arrancaba mal; corregido.
- **Perfil:** materiales de `BarProps`/`Challenges` cacheados por nombre (antes uno nuevo por reconstrucción); `AttackTelegraph` rehace sus mallas solo si cambia la forma y reutiliza arreglos (antes dos mallas nuevas por cuadro). Medido con el banco de vistas fijas (no con el Profiler de Unity).
- **Primer minuto:** el rótulo del punto apuntado dice con qué botón agarrar y, si está ocupado, quién lo tiene; las ayudas siguen al dispositivo en uso (mando ↔ teclado en vivo).
- **Campana de última ronda (4.1):** E en la punta de la barra; el tío mira al barman 2 s; si está aferrado no se suelta solo pero soltarle la mano cuesta el 40 % (jugada de a dos). Espera 20 s, dos por ronda (la segunda rinde la mitad), sonido y línea en todos, momento en el parte.
- **Desafío «Arranca en el karaoke» (4.2):** el tío empieza aferrado a la baranda de la tarima, al otro lado del salón (su «casa» pasa a ser la tarima); Estándar lo devuelve a la barra. En red, «Repetir configuración» vuelve a la sala con el mismo grupo y el anfitrión elige el desafío.

**No hecho en esta sesión:** arte del bloque 5 (malla nueva del tío, cuatro poses, manos que se cierran, materiales y encuadres nuevos, fondo del menú, líneas grabadas), secuencia audiovisual completa del agarre (bloque 3) y remate en el cierre de la camioneta (4.3), clip con sonido (sin grabador instalado: necesita permiso para ffmpeg/OBS).

#### Verificación (sesión 17; cada resultado con su build)

| Prueba | Código / build | Resultado |
| --- | --- | --- |
| EditMode | `475d746` (código) | **74/74**, 1 ignorada |
| PlayMode completa (con control de assets) | `475d746` (código) | **168/168**, 1 omitida; **archivos versionados sin cambios**. `lab_fail_m28` PASS (sin límite de FPS) |
| `lab_fail_m28` (StuckRecoveryTests_Lab) a 30/60/120 FPS | `1bc221d` (código) | **PASS** las tres (6/6 + 1 omitida cada una), assets sin cambios |
| Sin carrito ×3 → Estándar y gana con carrito; torre ×2; campana; karaoke ×2 → Estándar y gana arrastrando | `475d746` (código) | **4/4** (karaoke: gana en 31,8 s; Estándar tras Sin carrito: 43,4 s) |
| Autoprueba del bloqueo | — | **PASS** (4 simultáneos: 1 dueño, 3 rechazados; cancelado recuperado) |
| Red en localhost (20 escenarios: los 17 + `cycle`, `compat`, `race_natural`) | build `032702b` | **20/20 PASS** (`cycle` 9/9; `compat` en los dos sentidos contra la demo s16; `race` con disputa atada) |
| `race` repetido | builds `032702b` | **5 PASS** válidos; 5 corridas inválidas por el error de herramienta (anfitrión sin registro), corregido y repetido 2/2 PASS |
| `race_natural` ×12 | build dev `032702b` | **12/12 PASS** (nunca dos a la vez); disputa real en 5/12, aproximaciones fallidas en 6/12, ganador en 11/12 |
| Sesión larga 4 jugadores × 10 rondas (`long4`) | build `032702b` | **PASS**: 10/10 victorias, 0 excepciones, memoria +1/0/0/0 MB |
| `long` (1 jugador, 8 rondas) / `perf4` | build `032702b` | **PASS** (+1 MB; 4 procesos en una PC: p99 11,1 ms cada uno) |
| Auditoría de interfaz ES y EN (5 aspectos × 100/125/150 %) | build `1bc221d` | **15/15** por idioma, 0 problemas |
| Banco de vistas fijas, Alta, 1920×1080 (×2) | `1bc221d` vs build S16 (`c7a05c7`+) | con vsync 123 fps, p99 11,1 ms (igual); sin vsync 175 vs 174 fps, p99 7,2–7,5 vs 7,4–7,9 ms |
| Banco, Baja, sin vsync / Baja con `-Device 1` | `1bc221d` vs `1ca19d7` | 180,0 vs 179,8 fps (p99 7,08 vs 7,14 ms) / 198,7 vs 198,1 fps; **el nombre de la GPU no se registró**: no consta que `-Device 1` haya sido la Iris Xe |
| Demo: ZIP, SHA-256, copia descomprimida | build de jugadores `b44386d` | **PASS** |
| Relay desde una PC / dos equipos / LAN / grupos nuevos / clip | — | **NOT_RUN** (otra PC, personas y permiso para grabador) |

Resultados: `.local-setup/logs/unity/tests_*_s17*.xml`, `.local-setup/logs/net/net_smoke_history.csv`, `.local-setup/logs/perf/bench_*_s17_*.csv`, `.local-setup/shots/ui_audit_s17_*/`.

**Para volver a probar:** `docs/VOLVER_A_PROBAR_S17.md`. **Prueba con dos grupos nuevos (preparada):** `docs/PRUEBA_GRUPOS_S17.md`.

### Sesión 16: prueba del dueño, pulido de materiales y malla, prueba `race`

**Prueba humana del dueño:** Stefano jugó la demo de la sesión 15 (`fd0bce5`) el 2026-11-18. **Los detalles por tarea quedan pendientes** de que los pase (`docs/OBSERVACIONES_PRUEBAS.md`).

**Qué cambia a la vista**
- **Tío:** la malla del kit se importa con normales calculadas (suavizado 60°): cabeza, nariz, panza, brazos y piernas se ven redondos; ropa, calzado y bordes siguen marcados (antes: todo facetado). Pasa por el mismo rig: agarres, poses, caídas, correa y extracción verificados en la regresión.
- **Materiales que se distinguen de cerca** (texturas propias generadas con mapas de normales): acero cepillado (carrito, herrajes de la puerta), pintura naranja gastada con desconchados y rayones (carrito), goma con grano (ruedas, empuñaduras), madera barnizada oscura (molduras, marco), además de la madera y la tela del kit.
- **Barra:** moldura barnizada en el canto, franja gastada donde se apoyan los codos y apoyapiés de bronce con soportes (sin colisión: no compite con la baranda que se agarra).
- **Puerta de salida:** marco de madera con cabezal; en la hoja, placa de empuje, placa de patada y bisagras de metal (se mueven con la hoja).
- Comparación: `docs/capturas/sesion16/antes_despues_y_primeros_planos.jpg` (vistas fijas de la sesión 15 contra la 16 y primeros planos con `tools/windows/closeup_shots.sh`).

**Prueba de red `race` (disputa por el mismo punto):** fallaba porque el tío se movía antes de la señal y uno de los dos terminaba apuntando a otro punto (no había disputa que medir). Ahora el tío queda quieto en el anfitrión durante la prueba, el anfitrión ubica a los dos jugadores frente al cinturón hasta la señal y el criterio es la evidencia del anfitrión: rechazo «SocketBusy» en el cinturón, nunca dos a la vez y un solo ganador. **5/5 PASS**, los dos apuntando al cinturón, y PASS en la regresión completa de `1ca19d7`.

#### Verificación (sesión 16)

| Prueba | Código / build | Resultado |
| --- | --- | --- |
| `race` ×5 | build de `race: host places both players…` | **5/5 PASS** (rechazos=1, ganadores=1, apuntaban 2/2) |
| EditMode | `1a5dd5a` (código) | **64/64**, 1 ignorada |
| PlayMode completa | `1a5dd5a` (código) | 162/165: 1 omitida; FAIL `lab_fail_m28` (conocido, solo sin límite de FPS) y FAIL `Hold_HandsIndependent…` → defecto de la prueba (los dos hombros a la misma altura cambiaban de orden y a veces elegía el que ya sostenía la otra mano); corregido en `1ca19d7`: RealInputTests **29/29** |
| Red en localhost (17 escenarios) | build `1ca19d7` | **16 PASS + `loaddrop` NOT_RUN** (el bot de carrito del invitado no llegó a cargar en 240 s); repetido ×2: **2/2 PASS**. `race` PASS |
| Primeros planos y vistas fijas | build `S16` | `docs/capturas/sesion16/` |

**Candidata: `1ca19d7`**, demo `Builds/UncleExtraction_Demo_s16.zip` (incluye `VOLVER_A_PROBAR.txt`). Referencia anterior: demo de la sesión 15 (`fd0bce5`).

**Observación de pruebas:** al ejecutar PlayMode, una prueba le quita a `Uncle_Shirt.mat` la palabra clave de emisión (el brillo de aviso); se restauró el archivo, la causa queda por revisar (solo editor).

**Para volver a probar (vos):** ver `docs/VOLVER_A_PROBAR_S16.md`.

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

1. **Vos (15 min):** jugar `Builds/UncleExtraction_Demo_s17.zip` con `VOLVER_A_PROBAR.txt` y contarme lo que viste (y, si podés, los detalles pendientes de tu prueba de la demo s15). Lo anoto en `docs/OBSERVACIONES_PRUEBAS.md` como prueba del dueño.
2. **Con otra PC y 2–4 personas:** `docs/PRUEBA_GRUPOS_S17.md` (grupo A: Relay entre dos PC; grupo B: 3–4 personas en redes distintas). Anotar localhost, LAN e internet por separado.
3. **Decidir** si fusionar `s17-demo` en `main` y si se puede instalar un grabador (ffmpeg u OBS) para el clip con sonido.
4. **Desarrollo siguiente (sin depender de nadie):** bloque 5 de arte (malla del tío con ojos/párpados/cuello, cuatro poses, manos que se cierran sobre cinturón/tela/baranda, materiales y cuatro encuadres fijos, fondo del menú), secuencia audiovisual completa del agarre y remate en el cierre de la camioneta.
