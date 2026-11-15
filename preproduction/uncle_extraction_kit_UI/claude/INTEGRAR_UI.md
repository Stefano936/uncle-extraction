# Integrar interfaz y experiencia en UNCLE EXTRACTION

Continuá desde el proyecto existente, sin rehacer los hitos completados. Leé README.md y UI_SPEC.md de este kit, data/design_tokens.json, data/ui_bindings.csv y data/ui_state_flow.csv. Conservá GDD y balance originales como autoridad de gameplay.

1. Verificá la versión real de Unity, uGUI y la solución de texto. Este kit propone uGUI, Canvas Scaler y fuente SDF compatible; no presupone nombres de API no comprobados.
2. Importá sprites PNG como UI. Usá los bordes de data/sprite_import.json y Image Sliced cuando corresponda. No inventes archivos .meta ni GUID.
3. Creá la fuente con los TTF incluidos y preservá FONT_LICENSE.txt. No conviertas el texto de cada botón en una imagen; usá claves de data/localization_en_es.json y acciones de entrada dinámicas.
4. Construí primero un HUD de manos, energía y señal de empujón conectado a estado real. Pending no es Holding. Caída y resultado limpian prompts y acciones obsoletos.
5. Construí menú, conexión, lobby y resultados según autoridad de cada control. Los clientes no cambian dificultad ni fuerzan inicio. Online no se pausa al abrir el menú. No mostrés host migration que no existe.
6. No traslades las marcas de mockup, números ilustrativos, nombres ficticios ni ejemplos de premios como datos reales. Las capturas de screens/ no son screenshots de gameplay para Steam.
7. Verificá EN/es_UY, textos grandes, teclado/mando, 720p y ultrawide. Los 64px PNG de iconos sirven al tamaño previsto; exportá los SVG a más resolución si el proyecto lo necesita.
8. Registrá imports, archivos, evidencia y limitaciones en PROGRESS.md. Si no podés ejecutar Unity, prepará lo posible y marcá las pruebas pendientes con instrucciones reproducibles.

No rehagas física, movimiento o red para adaptar una pantalla. Los componentes de UI presentan estado y emiten solicitudes al sistema correspondiente. Avanzá con tareas reversibles del alcance; un servicio externo pendiente no impide construir el HUD local.

Entrega mínima de la primera integración: prefab de HUD, fuente y sprites importados, bindings reales de ambas manos y ataque, dos idiomas y prueba de agarre/caída si el entorno lo permite. Después completar menús y pruebas del catálogo.
