# UNCLE EXTRACTION — instrucciones del repositorio

## Objetivo y estado

Juego 3D en primera persona de comedia física, cooperativo de 1 a 4 jugadores (Unity 6.3 LTS, C#, Windows; Steam más adelante). Hay que sacar de un bar a un tío borracho controlado por el juego y llevarlo a una camioneta; el tío se resiste, se aferra, escapa, golpea y derriba. Estado real y siguiente acción: `PROGRESS.md`.

## Continuidad (instrucción vigente de Stefano, 2026-11-15)

- Actuar como desarrollador principal hasta una versión completa, jugable y verificable del alcance inicial. Implementación continua: al cerrar un hito, pasar al siguiente pendiente sin esperar «continuá».
- Prioridades: M2 red (host autoritativo, NGO + UTP) → interacciones y recorrido completos (vaso señuelo, distracciones, ayuda para levantarse, extracción real dentro de la camioneta con puertas, carga del carrito válida, ragdoll reducido) → internet (Sessions/Relay) y flujo completo de menús → presentación (URP, kits de modelos/UI/materiales, animación, audio, EN/es_UY, controles remapeables) → QA, rendimiento, Steam y build reproducible.
- Autonomía para cambios reversibles de código, escenas, prefabs, configuración, recursos y balance. Cuentas, licencias, pagos, publicación, otros equipos o participantes: registrar el bloqueo y seguir con lo independiente.
- Mantener siempre una build funcional de referencia. No ajustar el juego solo para que gane el autopiloto. No desactivar pruebas ni debilitar condiciones para ocultar fallos.
- Registrar PASS / FAIL / NOT_RUN / BLOCKED con evidencia; separar localhost, LAN e internet; un autopiloto exitoso no prueba diversión ni controles cómodos.
- Commits descriptivos y push al repo privado tras cada avance coherente; sin force push. Versionar código, escenas, prefabs, `.meta`, `Packages/` y `ProjectSettings/`; excluir cachés, `Library/`, builds, logs y credenciales.
- Cerca del límite de contexto: dejar en `PROGRESS.md` un punto de continuación preciso y subir los cambios.

## Arranque de cada sesión

1. `git status`, `git log`, `PROGRESS.md` (estado y «Próxima acción exacta»), `docs/DECISIONES_IMPLEMENTACION.md`, `DEPENDENCIES.md`.
2. Leer solo la documentación del hito activo (tabla de abajo). Markdown y PDF duplican contenido.
3. Implementar, verificar en proporción al cambio, actualizar `PROGRESS.md` y subir.

## Entorno y comandos verificados

- Unity 6000.3.28f1 en `C:\Program Files\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe`, licencia Personal activa, módulo Windows IL2CPP instalado (sin Visual Studio C++ todavía). Unity Hub y Unity CLI (`unity`) instalados; `gh` autenticado.
- Paquetes fijados en `UnityProject/Packages/manifest.json` (ver `DEPENDENCIES.md`).
- Batch desde `UnityProject/` (Git Bash): `"$U" -batchmode -nographics -projectPath . -logFile <log> ...` con:
  - escena: `-quit -executeMethod UncleExtraction.EditorTools.PhysicsLabBuilder.BuildFromCommandLine` (regenera PhysicsLab, prefab del jugador y hashes de red);
  - pruebas: `-runTests -testPlatform EditMode|PlayMode -testResults <xml>`;
  - build: `-quit -executeMethod UncleExtraction.EditorTools.BuildTools.BuildWindows -ueBackend Mono -ueOut ../Builds/UncleExtraction` (`-ueDev -ueOut ../Builds/UncleExtraction_Dev` para la de desarrollo con simulador de red).
- Build: `Builds/UncleExtraction/UncleExtraction.exe` (escenas Frontend, Bar_LastRound, PhysicsLab, PhysicsLab_Articulated; `VERSION.txt` con commit, cambios sin commitear, fecha y tipo, generado por `BuildTools`). Argumentos de prueba: `-ueScene <escena>`, `-ueHost [-ueListen 127.0.0.1] [-uePort 7777]`, `-ueClient <ip>`, `-ueAutopilot drag|cart|race`, `-ueAutopilotDelay s`, `-ueQuitOnEnd`, `-ueRepeat N`, `-ueMenuAtEnd`, `-ueRoundTime s`, `-ueDropAt strap|closing`, `-ueNoClose`, `-ueNetSim ms jitter %` (solo build dev), `-ueShot archivo.png@s` (captura; repetible), `-ueShotMenu archivo.png@s`, `-ueForceAttack slap|push|kick|shake@s` (anfitrión; para capturar avisos), `-ueRelay` (anfitrión por Relay), `-ueJoinCode CÓDIGO` (invitado por Relay), `-ueProfile nombre` (identidad anónima distinta por proceso), `-ueDifficulty Relaxed|Normal|Hard`, `-ueExpectPlayers N`, `-uePerf s` (rendimiento), `-ueUiAudit carpeta` y `-ueTextScale 1.5` (auditoría de interfaz), `-ueDebug` (líneas técnicas en release).
- Red en dos procesos: `powershell -ExecutionPolicy Bypass -File tools\windows\net_smoke.ps1 -Scenario all -Scene Bar_LastRound` (localhost, no LAN ni internet; código 0 solo si todos los escenarios dan PASS). `-Scenario qa`: dificultades × grupos, sesión larga, caída brusca del anfitrión, rendimiento (largo). `-Scenario relay`: Relay en una PC (BLOCKED sin proyecto de Unity Cloud).
- Interfaz: `powershell -ExecutionPolicy Bypass -File tools\windows\ui_audit.ps1 -Out <carpeta>` (5 aspectos × texto 100/125/150 %; capturas y textos fuera de pantalla, recortados, encimados o tapados).
- Logs locales (no versionados): `.local-setup/logs/unity/`, `.local-setup/logs/net/`.

## Reglas centrales

- Primera persona y dos manos independientes. Individual viable; cooperación funcional en grupos.
- Dificultad y cantidad de jugadores son ejes separados. La masa del tío no escala.
- NPC programado con ataques anticipables, recuperación y fatiga. Sin IA conversacional en partida.
- El anfitrión decide física, agarres, ataques, impactos, escalado y resultados; los clientes envían intenciones. Individual = anfitrión en loopback. Sin migración de anfitrión.
- Alcance inicial: alcohol, un bar, vereda y camioneta fija. Respetar exclusiones de `preproduction/uncle_extraction/DECISIONES.md`.
- Justificar por escrito cambios de motor, cámara, topología o alcance antes de rehacer.

## Mapa de lectura

| Tarea | Fuentes bajo `preproduction/` |
| --- | --- |
| M0 y M1 | `uncle_extraction/claude/01_M0_M1_laboratorio.md`; GDD y especificación técnica 1–5; `data/game_balance.json`. |
| M2 y red | `uncle_extraction/claude/02_M2_red_temprana.md`; especificación técnica 5–8; QA de red. |
| Hitos posteriores | Prompt del hito en `uncle_extraction/claude/` y su documentación. |
| Modelos | `uncle_extraction_kit_3D/README.md`, `claude/INTEGRAR_KIT.md` y metadatos. |
| UI | `uncle_extraction_kit_UI/claude/INTEGRAR_UI.md`, `UI_SPEC.md`. |
| Materiales | `uncle_extraction_kit_materiales/claude/INTEGRAR_MATERIALES.md`. |

Prompt original: `PROMPT_MAESTRO_CLAUDE_UNCLE_EXTRACTION.md` (consultar por secciones; puede estar desactualizado frente a lo ejecutado). Prioridad: instrucciones actuales del usuario > requisitos fijados > GDD/especificación > datos > kits y mockups. Conservar `.meta`; no inventar GUID; mantener las fuentes originales en `preproduction/`.
