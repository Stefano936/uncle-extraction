# Especificación de interfaz y experiencia - v0.1

Complemento del GDD v1.0 y del kit 3D. Fecha: 15 de noviembre de 2026. Las doce pantallas son propuestas estáticas. El juego y sus pruebas en Unity no están implementados en este paquete.

## 1. Qué queda definido

El objetivo es reducir decisiones de presentación durante la programación. El jugador debe entender qué sostiene, qué prepara el tío, cuándo puede moverse y cómo terminar la extracción. Los menús permiten entrar al juego, coordinar una sala y resolver errores con acciones claras. Las mecánicas, valores de balance y arquitectura del paquete original conservan prioridad.

Se entregan menú, lobby privado, opciones, cuatro estados de HUD, pausa online, victoria, derrota por tiempo, pérdida de host y tutorial. Las capturas están a 1920x1080, en inglés, con nombres y números ficticios. La versión es deliberadamente de diseño: las marcas de mockup no forman parte del producto final. Los fondos son renders de la geometría del kit 3D, sin simulación de gameplay.

El idioma inicial de los mockups apunta al público angloparlante del concepto. El catálogo aporta traducción es_UY para instrucciones y menús. Las voces del NPC y sus traducciones siguen el catálogo de diálogos original; no se reemplazan por mensajes de la interfaz.

## 2. Sistema visual y componentes

Colores de base: carbón, crema y verde azulado oscuro. El ámbar señala foco, atención o esfuerzo; coral con forma de advertencia señala peligro. El color nunca es el único indicador. Cada mano muestra icono, etiqueta y estado; cada golpe muestra postura del NPC y, según ajustes, una señal textual.

Los valores de tipografía, espaciado y radios están en design_tokens.json. La resolución de referencia es 1920x1080 y el margen principal 64 px. El tamaño de un botón de referencia es 72 px de alto. No copiar coordenadas de una imagen como si fueran coordenadas locales de Unity: convertirlas a anchors, pivots y contenedores adaptables.

Se propone un Canvas uGUI con Scale With Screen Size y mezcla ancho/alto inicial 0,5. Es una receta a comprobar con la versión instalada, no un prefab entregado. Los menús usan un contenedor centrado y los elementos del HUD se anclan por esquina o borde. En ultrawide, conservar un ancho máximo razonable para que las manos no queden a extremos incómodos.

Los once sprites no contienen texto. Incluyen panel, panel de peligro, estados de botón, keycap, barras y foco. El archivo sprite_import.json define tamaño, bordes y opciones propuestas. Usar sprites con transparencia; no generar colliders. Para Image Sliced, configurar el borde del sprite y verificar esquinas sin deformación. El foco de mando/teclado debe ser visible igual que hover de ratón.

Los iconos se entregan en vector y PNG de 64 px. Ese tamaño sirve a los elementos previstos a 1080p. Para presentaciones mayores, exportar SVG a resolución suficiente; no prometer una interfaz 4K verificada. Las fuentes TTF se incluyen sin modificar con su aviso de licencia. Crear un asset de fuente compatible en Unity y revisar acentos, signos, flechas y nombres admitidos.

## 3. Pantallas y recorrido

Menu permite jugar solo, abrir conexión con amigos, tutorial, opciones y salir. Play Solo configura un intento local. Play With Friends abre un modal reutilizable con Create Room y Join by Code. La elección precede al estado Connecting; la interfaz permite cancelar y descarta callbacks de intentos ya cancelados.

Lobby muestra únicamente miembros presentes, no exige readiness de espacios vacíos y admite de uno a cuatro. El anfitrión modifica dificultad y configuración antes de cargar. Un invitado ve esos valores como solo lectura. Start se habilita con configuración válida, miembros presentes listos y servicios necesarios disponibles. Su estado deshabilitado explica el motivo. El ejemplo muestra tres listos y Alex sin confirmar.

Al cambiar configuración, invalidar readiness y comunicar que hay que confirmar de nuevo. Durante carga no se permite empezar otro intento. La ruta de entrada a una ronda ya iniciada se rechaza con mensaje específico porque el alcance original no admite join in progress.

El botón Retry conserva semilla y configuración pero vuelve a una preparación con readiness limpio. New Setup abre el lobby para configurar otra semilla. Back to Lobby vuelve sin comenzar una ronda automáticamente. Las pantallas de resultados ilustran la vista del host; invitados tienen acciones de inicio deshabilitadas y una explicación de espera, además de la salida al menú.

Tutorial comunica tres decisiones: agarrar, observar preparación y mover durante recuperación. Puede consultarse sin iniciar una partida online a espaldas del grupo. Si existe práctica local, se abre por un flujo explícito. El texto no ordena un botón que haya sido remapeado: usa el glyph de la acción actual.

## 4. Contrato de las manos y el HUD

Estados de mano: Free, Pending, Holding, Strained, Released y Unavailable. Pending es una solicitud enviada, no una unión física confirmada. Solo una concesión válida del host cambia a Holding. Cada respuesta se empareja con requestId; una respuesta vieja no restaura un agarre que ya se soltó. Los dos paneles son independientes.

Strained añade advertencia visual y textual. Se recomienda histéresis o un pequeño debounce visual para evitar parpadeo cuando la tensión oscila; esto no altera los umbrales físicos del balance. Released es una reacción breve después de perder el vínculo, y vuelve a Free o Unavailable según el estado del jugador.

La pista de esfuerzo aparece cuando existe agarre y el jugador tiene control. En Free dice que primero debe agarrar. En Fallen se reemplaza por estado de recuperación. Una opción de agarre alternable cambia los verbos: Press to grab y Press to release en lugar de Hold to grab. La captura de opciones muestra un borrador con Toggle Grab activado; las demás capturas ilustran el modo de mantener.

La energía se presenta con suavizado visual sin alterar valores de simulación. La compostura puede aparecer al recibir golpes o estar baja; se evita una barra permanente innecesaria. El reloj se toma de ticks autoritativos y desaparece en Relajado. No se muestra 00:00 como una derrota cuando el modo no usa reloj.

El marcador de socket o miembro atacante se proyecta desde el actor real. Las posiciones del círculo y el callout en los mockups son ilustrativas, no coordenadas fijas del futuro juego. El texto crítico permanece en zona segura si el punto queda fuera de pantalla. La selección y las colisiones siguen las reglas de alcance y autoridad del GDD.

## 5. Golpes, caídas y prioridad de señales

El host anuncia Attack.instanceId, tipo y tiempos. El HUD muestra un mensaje correspondiente: manotazo, empujón, patada o sacudida. Una preparación no anuncia daño ya ocurrido. El marcador se cancela cuando termina o se cancela esa instancia; una copia tardía no revive un ataque anterior.

Orden de prioridad: error de sesión bloqueante, resultado terminal, caída/control, ataque crítico, oportunidad de recuperación, ayuda contextual y diálogo ordinario. La señal de ataque puede coexistir con la caída si aporta información útil, pero no oculta recuperación ni salida. Al terminar la ronda se limpian todos los prompts de gameplay.

Desactivar voces no elimina señales. Descriptive Attack Subtitles ofrece texto separado del diálogo del NPC. La postura y el sonido de ataque se mantienen aunque un tutorial contextual se haya ocultado. Las pistas de aprendizaje se reducen después de varios usos exitosos; las señales críticas no desaparecen por haber jugado mucho.

Fallen no es una pantalla de muerte. Se muestran recuperación automática y progreso disponible desde el estado real, sin exigir ayuda de otro jugador en individual. El tiempo máximo de control perdido y la gracia de levantada usan el balance original; la UI no los reinicia al abrir un menú. Al levantarse se presentan manos actuales y se exige nueva entrada para un nuevo agarre.

## 6. Pausa, opciones y contexto de entrada

En individual, pausa detiene la simulación local según el diseño original. Online, pausa abre opciones propias y el mundo continúa. El mensaje al jugador explica ese efecto sin hablar de topología o migración. Al abrir un menú incompatible, cancelar esfuerzo e inputs de juego y solicitar liberación de manos de forma segura; no dejar teclas mantenidas por pérdida de foco.

Los contextos UI y Gameplay son exclusivos al despachar una acción. El botón East puede cancelar en UI y soltar en gameplay, pero no ejecutar ambas cosas al cerrar el panel. Al retomar, reconstruir desde estado actual y esperar una nueva pulsación. Si la ronda terminó durante la pausa, ir a resultados en lugar de volver al HUD anterior.

Leave Match abre una confirmación breve. Para el host: Leaving ends the match for everyone. Para un cliente: Leave this match and return to the menu? Se explica una consecuencia real de perder la sesión. Cancelar vuelve a la pausa, sin restaurar agarres viejos.

Opciones usan un borrador local. Audio puede previsualizarse mientras se edita; Apply guarda lo aceptado y Back con cambios ofrece aplicar o descartar. Descartar restaura los valores guardados. Idioma y accesibilidad no cambian a otros jugadores. El filtro de insultos usa las columnas limpias del catálogo original.

El remapeo usa un modal: elegir acción, escuchar entrada, mostrar conflicto y permitir reasignación segura o cancelar. El cuadro no captura inputs del gameplay mientras escucha. Los prompts siempre se derivan de bindings actuales. No se entregan glyphs propietarios de consolas; usar un proveedor compatible o etiquetas genéricas verificadas.

## 7. Errores, resultados y confianza

Errores diferenciados: código inválido, sala llena, versión distinta, ronda iniciada, timeout y servicio no disponible. No atribuir todos a la conexión del usuario. El botón de reintento solo aparece cuando tiene sentido. La ruta de solo permanece disponible si el proveedor local funciona y falla únicamente el servicio online.

Si se pierde el host durante una ronda sin resultado confirmado, cerrar la sesión y ofrecer menú. Si ya llegó un resultado terminal confirmado, conservar sus estadísticas, deshabilitar operaciones de la sesión y ofrecer salida. Una elección de nuevo propietario en un servicio no promete reconstrucción del mundo físico.

Los resultados muestran únicamente eventos reales. En Timeout, tiempo de extracción es un guion porque no se completó. No inventar premios: Human Doorstop requiere un evento registrado de sostener despejada la puerta mientras pasa el grupo; si no existe, omitir el slot. Los números de las imágenes son fixtures de diseño, no datos de una partida real.

Los nombres de miembros son datos de presentación: limitar longitud, sanitizar etiquetas de rich text y truncar de forma legible. No usar el texto visible como identidad o autoridad. Los códigos de sala no se imprimen en logs públicos por defecto; el botón Copy actúa en el portapapeles local y presenta una alternativa seleccionable si falla.

## 8. Accesibilidad y validación

La interfaz usa texto y forma además de color, subtítulos escalables, señales descriptivas y reducción de movimiento. Probar 720p, 1080p, ultrawide y escalas de texto 100%, 125% y 150% en Unity. El layout puede crecer o permitir scroll; no achicar toda la tipografía para encajar traducciones.

Cada pantalla tiene foco inicial, orden de navegación y ruta de vuelta. El foco nunca se queda detrás de un modal. Un spinner tiene cancelación o salida controlada. Los estados deshabilitados explican razones y no dependen de hover para ser entendidos con mando.

Las verificaciones del paquete son de recursos: PNG, SVG, medidas, alpha, claves, referencias y límites de texto nominales. Las pruebas de ui_acceptance.csv comienzan NOT_RUN porque requieren el juego. Revisar UI, estados de red, latencia y entradas reales después de integrar.

## 9. Fuentes primarias y procedencia

- Unity: 9-slicing y sprites. https://docs.unity.com/en-us/engine/6000.3/manual/unity2d/sprite/9-slice/9-slicing
- Unity: Sprite Editor y bordes. https://docs.unity.com/en-us/engine/6000.3/manual/unity2d/sprite/sprite-editor-window-reference/reference
- Unity Learn: Canvas Scaler, anchors y UI. https://learn.unity.com/tutorial/working-with-ui-in-unity
- DejaVu: aviso completo conservado en assets/fonts/FONT_LICENSE.txt. No se modificaron los TTF distribuidos.

Consulta técnica: 15 de noviembre de 2026. Las recetas son decisiones del proyecto a comprobar con paquetes instalados. Los fondos se renderizaron en Blender desde el kit 3D; no son material de tienda. Los iconos y sprites se construyeron como gráficos precisos con código, sin copiar un sistema de interfaz de otro juego.
