# Estado del proyecto

Última actualización: 2026-11-15 (Claude Code local, sesión 3: integración Git, M0/M1 compilado, autopiloto e intentos individuales automáticos).

## Hito actual

M0 + M1 — PhysicsLab **compila y pasa las pruebas automáticas, incluidos intentos individuales completos de arrastre en Normal y Difícil, en tiempo real y con el cerebro del tío encendido. La build de Windows completa sola un intento de arrastre con el autopiloto.** Abierto: el modo carrito se traba en la puerta (problema conocido). Falta jugarlo a mano: no hubo control de pantalla en la sesión.

## Niveles de verificación (2026-11-15, sesión 3)

| Nivel | Estado | Evidencia |
| --- | --- | --- |
| Código escrito | OK | `UnityProject/Assets/Game/`. Cambios de C# en esta sesión: `LabInput.Simulated`, `LabAutopilot` (nuevo), levantada del tío en `NpcBrain`, correa en `TransportCart`, panel del HUD, `BuildTools` (`runInBackground`) y `SoloAttemptTests`. |
| Compilación C# | OK, 0 errores, 0 warnings | Unity 6000.3.28f1 batch; `.local-setup/logs/unity/01b_bootstrap.log` (local, no versionado). |
| Escena PhysicsLab | Generada | `PhysicsLabBuilder.BuildFromCommandLine` → `Assets/Game/Scenes/PhysicsLab.unity`, 10 sockets. |
| Pruebas EditMode | 9/9 PASS | `23_editmode.xml`. |
| Pruebas PlayMode | 6 PASS + 1 ignorada, 2 corridas | `22_playmode_{1,2}.xml`. Las 4 originales (cerebro apagado, extracción con teletransporte) + `SoloAttemptTests` en **tiempo real** (`timeScale` 1). |
| Intento individual de arrastre (automático) | PASS: Normal 28,7 y 28,9 s; Difícil 43,5 y 30,3 s | `LabAutopilot` juega solo con entradas de jugador (mirar, moverse, clics, E, Q, Espacio, agacharse; sin `Strap()`, `Secure()` ni teletransportes). El tío se aferra, forcejea, ataca, cae y se agota; el bot lo saca por la puerta y lo entra de costado, tomado de los tobillos. |
| Intento individual con carrito (automático) | FALLA en tiempo real (ignorado con motivo) | Asegura al tío con E (correa) y tira del carrito, pero se traba al salir por la puerta: el carrito deriva a la derecha y la cabeza del tío, que sobresale, engancha el marco. 0/3 en la prueba en tiempo real y 0/9 en la build. Con `timeScale` 4 pasaba 3/3, pero era un falso positivo por control más grueso; por eso las pruebas pasaron a tiempo real. |
| Build Windows (Mono, x64) | OK | `Builds/PhysicsLab/UncleExtraction_PhysicsLab.exe` (~92 MB, no versionado), 0 errores (`24_build.log`). `runInBackground` activado. |
| Build: intento automático de arrastre | PASS 3/3 tras activar `runInBackground` (13,7, 87,8 y 104,7 s) | `UncleExtraction_PhysicsLab.exe -ueAutopilot drag -ueQuitOnEnd` en ventana de 1280×720; sale con código 0 si la ronda termina en éxito (`25_build_autopilot_drag_*.log`). Antes, 1 de 5 corridas se quedó congelada: sin foco, Unity pausaba el juego. |
| Arranque de la build | OK | 20 s en ventana 1280×720: D3D11 (RTX 3070 Ti), PhysX 4.1.2, Input System inicializado, sin excepciones (`06_build_smoke.log`). |
| Jugado a mano | NOT_RUN | Sin herramientas de control de pantalla en la sesión. Sensación, legibilidad de ataques y diversión sin comprobar. |
| IL2CPP | NOT_RUN | Módulo instalado; falta Visual Studio con C++. |
| Red | NOT_RUN | M2. |

## Comandos verificados (Git Bash, desde `UnityProject/`)

```
U="C:/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Unity.exe"
"$U" -batchmode -nographics -projectPath . -logFile <log> -executeMethod UncleExtraction.EditorTools.ProjectBootstrap.Run
"$U" -batchmode -nographics -quit -projectPath . -logFile <log> -executeMethod UncleExtraction.EditorTools.PhysicsLabBuilder.BuildFromCommandLine
"$U" -batchmode -nographics -projectPath . -logFile <log> -runTests -testPlatform EditMode -testResults <xml>
"$U" -batchmode -nographics -projectPath . -logFile <log> -runTests -testPlatform PlayMode -testResults <xml>
"$U" -batchmode -nographics -quit -projectPath . -logFile <log> -executeMethod UncleExtraction.EditorTools.BuildTools.BuildWindows -ueBackend Mono -ueOut ../Builds/PhysicsLab
```

`tools/windows/unity_m0_m1.cmd` hace lo mismo (busca `6000.3.*`).

## Cambios de esta sesión

- Historias unidas: los 4 commits de Cowork (Descargas) + commit inicial de GitHub, merge `21021fc` (único conflicto `README.md`: se conservó el ampliado). Sin force push; la carpeta de Descargas queda intacta como respaldo.
- Herramientas instaladas (winget oficial): Unity Hub 3.22.6, Unity CLI 1.0.0, editor 6000.3.28f1 + Windows Build Support (IL2CPP). Licencia Unity Personal activa (la activó Stefano). `gh` ya estaba autenticado.
- Corrección: `ProjectBootstrap` no podía correr porque los asmdef de pruebas no compilaban sin Test Framework (huevo y gallina). Se fijaron los paquetes directamente en `Packages/manifest.json`. Se quitaron paquetes por defecto no usados (purchasing, analytics, collab-proxy, 2D, XR helpers) y el `Assets/Resources/BillingMode.json` que dejó purchasing.
- `.gitattributes`: LF para json/asmdef/physicMaterial/ProjectVersion de Unity.
- Commit `bcc8f9b` (proyecto Unity, escena, settings, .meta).
- Intento individual automático (`Tests/PlayMode/SoloAttemptTests.cs`) y `LabInput.Simulated` (botones simulados que se suman a la entrada real, para pruebas y un futuro autopiloto).
- `LabAutopilot` (`Scripts/Debug/`): jugador automático para pruebas y demostraciones. En la build: Tab → panel → «Autopiloto: Arrastrar / Carrito / Parar», o por línea de comandos `-ueAutopilot drag|cart [-ueQuitOnEnd]`.
- Ajuste de juego: al asegurar con E, la correa acuesta al tío a lo largo del carrito y bloquea su rotación (antes giraba libre, quedaba atravesado y no pasaba la puerta).
- `PlayerSettings.runInBackground = true` (en `BuildTools`).
- Ajuste de juego: el tío caído necesita 2,5 s sin agarres para levantarse (antes se levantaba al instante al soltarlo y se iba de la zona antes del conteo de 1,5 s). Ver `docs/DECISIONES_IMPLEMENTACION.md`.

## Hallazgos del intento automático (para el playtest)

- Con una mano (420 N) no se puede arrastrar al tío caído: la fricción ronda los 460 N. Con dos manos (tope 700 N) sí. Puede ser intencional (incentiva usar las dos manos o el carrito), pero hay que comunicarlo.
- La puerta de 1,5 m es un cuello de botella real: el tío acostado se engancha en el marco si entra en diagonal (GDD: «orientar el cuerpo en el umbral»).
- Si se lo arrastra de los hombros contra la camioneta, el torso queda bajo el jugador y fuera de la zona. Funciona entrarlo de costado, tomado de los tobillos. La zona (2,2 × 1,6 m) delante de una camioneta sólida es exigente; en el juego final el tío va **dentro** de la camioneta (GDD).
- En tiempo real, el bot gana arrastrando en 14–105 s (Normal) y 30–64 s (Difícil), con 1 a 12 ataques. La varianza es alta y podría ser fácil para un jugador hábil: medirlo a mano antes de tocar el balance.
- Carrito + puerta: el tío (1,9 m) sobresale del carrito (1,4 m) y engancha el marco de 1,5 m si el carrito no sale centrado y recto. Puede ser bueno como desafío («orientar el cuerpo en el umbral»), pero hoy el bot no lo resuelve. Opciones: un carrito más largo, el tío más encogido en la caja, o un bot que alinee mejor antes de la puerta.

## Código M1 implementado

`UnityProject/Assets/Game/` (asmdefs Runtime, Editor, Tests):

- Jugador: `PlayerMotor` (cápsula dinámica 80 kg, 4,2/2,8/2 m/s, paso evasivo con barrido, agacharse con chequeo de espacio, compostura, energía, tropiezo, caída 2,2 s, levantada con búsqueda de lugar libre, invulnerabilidad 1,5 s) y `PlayerHands` (dos manos independientes, alcance 1,4 m con línea de visión, esfuerzo, Q suelta todo, E interactúa).
- Agarre: `GrabRegistry` (único registro; ocupación por socket; límite global 8; liberación idempotente con motivo) y `GrabSpring` (una sola fuerza `clamp(k·e − c·v, Fmax)`, 420/560 N por mano, 700/950 N por jugador, aviso 1,6 m, corte a 2,2 m tras 0,15 s).
- Tío: `NpcBody` (cuerpo central de 110 kg, equilibrio con torque limitado, cae si se inclina >45°, se levanta si nadie lo agarra), `NpcBrain` (Loiter/Protest/Anchor/Struggle/Windup/Attack/Recover/ForcedRecovery/Escape/Carried/Secured; 4 ataques con preparación ≥0,8 s, dirección bloqueada, un impacto por objetivo; anclaje a la barra liberable manteniendo E 0,9 s; presupuesto de resistencia y apertura forzada) y `NpcPresentation` (color, pose e indicador de cada ataque).
- Carrito con asa para dos manos y correa (E); el tío puede intentar soltarse tras una señal. `ExtractionZone` (torso, pelvis y hombros dentro durante 1,5 s). `RoundDirector` (dificultad, N, temporizador, reinicio limpio, prueba de 10 reinicios).
- Depuración: HUD IMGUI (Tab), ataques forzados, 0/1/2/4 jugadores virtuales que tiran, sliders k/c/equilibrio, derribarse, simular desconexión.
- Editor: `ProjectBootstrap` (instala Input System, Test Framework y uGUI con la versión que elija Unity), `PhysicsLabBuilder` (genera la escena), `BuildTools` (build Windows; Mono por defecto).
- Pruebas: 9 EditMode (registro y balance) y 4 PlayMode (8 agarres + 10 reinicios sin huérfanos, arrastre, empujón que derriba y recuperación, extracción).

Decisiones y simplificaciones: ver `docs/DECISIONES_IMPLEMENTACION.md` (2026-11-15, M1).

## Faltantes y contradicciones detectadas

- `preproduction/uncle_extraction/data/asset_manifest.csv` marca todos los modelos como `pending/TBD`, pero el kit 3D entregó prototipos de 8 de ellos (bar shell, barra, puerta, camioneta, banqueta, mesa, carrito, tío). El README del kit pide registrar «prototipo entregado / producción pendiente». No se editó la fuente original; actualizar el estado al importar en Unity.
- Sin modelo todavía (usar primitivas en PhysicsLab): cuerpo compartido del jugador, brazos en primera persona, marco de puerta, vaso señuelo, vereda. Audio: todo pendiente (P1, no bloquea M1).
- Texturas: solo Base Color, 1254 px (los prompts pedían 2048). Sin normal/roughness/AO.
- Todo el arte entregado es **prototipo/placeholder**; el arte conceptual (`preproduction/uncle_extraction/art/`) es solo referencia de dirección final. Los GLB no son importables por Unity sin paquete extra: usar FBX.
- El kit no trae ragdoll funcional ni triggers: `Extraction_Volume` y sockets son marcadores.
- `ENTREGA_SHA256.json` es una foto del empaquetado: `.gitignore`, `.gitattributes`, `PROGRESS.md`, `DEPENDENCIES.md` y `docs/DECISIONES_IMPLEMENTACION.md` cambiaron después a propósito.
- `00_EMPEZAR_AQUI.md` y `docs/GUIA_GITHUB.md` describen la vinculación con GitHub, ya realizada (merge `21021fc`); quedan como historia.

## Ubicación y versionado

- Carpeta principal: `C:\Users\stuff\OneDrive\Escritorio\uncle-extraction`; `origin` = `Stefano936/uncle-extraction` (privado), rama `main`.
- OneDrive sincroniza `UnityProject/Library` (ignorado por Git, pero pesado). Recomendado excluirlo de OneDrive, o mover el clon fuera de OneDrive si aparecen bloqueos de archivos.
- Sin Git LFS (ver `docs/DECISIONES_IMPLEMENTACION.md`).

## Próxima acción exacta

1. Stefano (o Claude con control de pantalla): abrir `Builds/PhysicsLab/UncleExtraction_PhysicsLab.exe`. Se puede mirar primero al autopiloto (Tab → «Arrastrar») y después jugar un intento completo: agarrar con clic izquierdo/derecho, arrastrar al tío, esquivar un ataque anunciado, caer y levantarse, cargarlo en el carrito (E para la correa) y llevarlo a la zona verde. Anotar qué no funciona o no se siente bien.
2. Claude: destrabar el modo carrito en la puerta (problema conocido; probar primero el ajuste del carrito o del tío en la caja) y volver a activar `SoloAttempt_Cart_Normal_Succeeds`.
3. Pendientes de M1 según decisiones: vaso señuelo, ayuda de compañero, superposición con sólidos y puerta en la extracción; URP.
4. Instalar Visual Studio 2022 con «Desarrollo de juegos con C++» y probar la build IL2CPP.

## Al cerrar una sesión

Anotar comportamiento implementado, archivos, commit, comandos y pruebas ejecutadas, resultados, bloqueos y siguiente acción.
