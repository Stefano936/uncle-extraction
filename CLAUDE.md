# UNCLE EXTRACTION — instrucciones del repositorio

## Objetivo y estado

Construir un juego 3D de comedia física para Windows y futura distribución en Steam. De 1 a 4 jugadores sacan de un bar a un NPC borracho que se resiste y puede derribarlos. La entrega inicial es preproducción; comprobar el estado real antes de continuar.

## Arranque de cada sesión

1. Inspeccionar instrucciones aplicables, `git status`, `PROGRESS.md` y cambios existentes. Preservar trabajo ajeno.
2. Leer `preproduction/uncle_extraction/DECISIONES.md` y `docs/DECISIONES_IMPLEMENTACION.md`, y elegir una tarea concreta del hito activo.
3. Consultar solo los documentos relevantes; no cargar todos los PDF, imágenes, JSON o el prompt completo en cada sesión. Markdown y PDF duplican contenido.
4. Implementar, verificar lo posible y actualizar `PROGRESS.md` con evidencia y siguiente acción.

## Reglas centrales

- Primera persona y dos manos independientes. Individual viable; cooperación funcional en grupos.
- Dificultad y cantidad de jugadores son ejes separados. La masa del tío no escala.
- NPC programado con ataques anticipables, recuperación y fatiga. Puede empujar, pegar, patear y derribar. Sin servicio de IA conversacional durante la partida.
- Física y resultado autoritativos del anfitrión; probar un segundo cliente temprano.
- Alcance inicial: alcohol, un bar, vereda y camioneta fija. Respetar exclusiones de DECISIONES.md.
- Autonomía para cambios internos reversibles y ajustes de balance con evidencia. Justificar cambios de motor, cámara, topología o alcance antes de reescribir.

## Mapa de lectura

| Tarea | Fuentes bajo `preproduction/` |
| --- | --- |
| M0 y M1 | `uncle_extraction/claude/01_M0_M1_laboratorio.md`; secciones pertinentes del GDD, especificación técnica y `data/game_balance.json`. |
| M2 y red | `uncle_extraction/claude/02_M2_red_temprana.md`; especificación técnica y QA de autoridad, sockets y desconexión. |
| Hitos posteriores | Prompt correspondiente en `uncle_extraction/claude/` y documentación asociada. |
| Modelos | `uncle_extraction_kit_3D/README.md`, `claude/INTEGRAR_KIT.md` y metadatos. |
| UI | `uncle_extraction_kit_UI/claude/INTEGRAR_UI.md`, `UI_SPEC.md` y datos necesarios. |
| Materiales | `uncle_extraction_kit_materiales/claude/INTEGRAR_MATERIALES.md`, especificación y datos necesarios. |

El prompt detallado original está en `PROMPT_MAESTRO_CLAUDE_UNCLE_EXTRACTION.md`. Consultar sus secciones pertinentes. `00_EMPEZAR_AQUI.md` adapta sus rutas y política de lectura al repositorio. Prioridad: instrucciones actuales del usuario, requisitos fijados, GDD/especificación, datos, especificaciones de kits y mockups.

## Ejecución y evidencia

Crear `UnityProject/` si no existe. Comprobar editor, licencia, módulos y paquetes; fijar versiones en `DEPENDENCIES.md` y archivos del proyecto. No hay comandos de build verificados todavía: registrarlos en M0 cuando funcionen.

Conservar `.meta`; excluir cachés, builds y credenciales. No inventar GUID ni presentar generadores sin ejecutar como escenas comprobadas. Mantener fuentes originales en `preproduction/`.

Distinguir revisión estática, compilación, Play, build y pruebas de red. No marcar QA como PASS sin ejecución. Si falta Unity, cuentas o participantes, registrar el bloqueo y avanzar con tareas independientes. No publicar el juego ni contratar servicios por esta instrucción.
