# UNCLE EXTRACTION
## 01. Cómo usar este paquete con Claude

**Plan de producción, validación y entrega a desarrollo | Versión 1.0 | 15 de noviembre de 2026**

Este paquete convierte una conversación de ideación en una propuesta implementable por etapas. Incluye decisiones de diseño, arquitectura sugerida, arte conceptual, datos iniciales y pruebas. No contiene un ejecutable, un proyecto Unity ni modelos 3D terminados. El siguiente paso es que Claude trabaje en un entorno con acceso a archivos y, de ser posible, a Unity y a la ejecución de builds.

Orden recomendado de lectura: README.md; docs/01_GDD_diseno_del_juego.md; docs/02_especificacion_tecnica.md; docs/03_biblia_visual_y_sonora.md; este plan; DECISIONES.md; data/game_balance.json; claude/00_PROMPT_MAESTRO.md. Los PDF ofrecen lectura cómoda; los Markdown y catálogos facilitan buscar, modificar y programar.

Descomprimir el ZIP completo en una carpeta de trabajo. Si Claude no puede abrir ZIP, darle los Markdown y datos extraídos. No pegar los cuatro PDF en una única instrucción gigantesca esperando que todos los detalles se conserven. El prompt maestro indica lectura inicial, comprobación del entorno y primer hito concreto. Los prompts de fases permiten retomar después sin reconstruir todo de memoria.

Claude debe comenzar verificando qué herramientas tiene realmente. Si puede editar archivos pero no ejecutar Unity, puede generar código y herramientas de editor, pero debe separar «escrito» de «compilado» y «probado». Entregar instrucciones exactas para la ejecución humana pendiente. No puede reportar una prueba de red real si solo abrió dos ventanas de editor.

El dueño del proyecto conserva decisiones de cuentas, aceptación de términos, compras y publicación. Las acciones reversibles de diseño, código y pruebas avanzan dentro del alcance sin pedir permiso cada pocas líneas. Un bloqueo de credenciales no detiene el laboratorio físico ni justifica inventar claves.

La prioridad es que el primer empujón y el primer agarre sean claros y graciosos. Un menú precioso, decenas de insultos y más mapas no compensan una simulación inestable. El plan obliga a demostrar el núcleo antes de fabricar todo el contenido.

## 02. M0: entorno reproducible y decisiones cerradas

**Resultado:** proyecto Unity que abre, compila y genera una build Windows vacía con controles básicos de entrada. Este hito no requiere servicios online, Steam ni arte final. No se promete compatibilidad de paquetes antes de instalar y comprobar.

Tareas: inventariar sistema operativo, editor y herramientas; seleccionar Unity 6.3 LTS con parche estable; crear Universal 3D; instalar paquetes base compatibles; registrar versiones; configurar escenas Bootstrap y PhysicsLab; establecer Git y exclusiones de Unity; añadir README de arranque; crear primera build.

Entregables: ProjectVersion.txt, manifest.json, packages-lock.json, DEPENDENCIES.md, scripts de build si el entorno permite, escena de laboratorio y registro de evidencia. La build debe indicar número de versión y no depender de una ruta absoluta del equipo de desarrollo.

Criterio de salida: un clon limpio abre en el editor fijado, no tiene errores de compilación y puede generar el ejecutable. Si no hay editor disponible, el hito queda parcialmente preparado y su prueba permanece pendiente; se continúa con tareas independientes que no dependan de darlo por aprobado.

Se cierran decisiones de motor, cámara y topología. Un cambio requiere una nota con problema observado, alternativa, coste de migración y prueba. La preferencia del asistente por otro framework no es evidencia suficiente. No sumar DOTS, mundos dedicados, microservicios o un backend propio sin una necesidad demostrada.

## 03. M1 y M2: núcleo físico y primer cliente remoto

**M1, laboratorio individual:** cápsula del jugador, dos manos, muñeco resistente y un ataque. Construir la transacción de agarre con limpieza explícita. Primero probar un cuerpo simple; después añadir segmentos. Implementar señal de empujón, contacto único, pérdida de compostura, caída y recuperación. Añadir un carrito funcional y un volumen de extracción de prueba.

Evidencias M1: video corto o registro visual del agarre; prueba de fuerza en direcciones opuestas; caída sin cámara incontrolable; liberación al reiniciar; una victoria repetible. Criterio de salida: diez intentos consecutivos sin uniones huérfanas, explosiones físicas ni bloqueo de control. La calidad de la diversión se anota aunque la geometría sea gris.

**M2, dos instancias:** integrar NGO y Unity Transport en topología de host. El host decide NPC, agarres, ataque y extracción. El cliente ve la marioneta y envía inputs. Se prueban solicitudes simultáneas de un socket, una caída, soltar durante golpe y desconexión con manos ocupadas.

Evidencias M2: host y cliente en builds separadas; versiones de protocolo; captura de ambos puntos de vista; latencia y pérdida configuradas para la prueba. Aclarar si la conexión fue loopback, LAN o internet. M2 no se presenta como online listo para vender.

Criterio de salida: ambas instancias coinciden en quién sostiene cada punto, quién recibió el golpe y cuándo termina la ronda. El control de un cliente remoto debe ser utilizable; si solo funciona para el host, investigar presentación y arquitectura antes de continuar.

Estas fases son deliberadamente tempranas porque concentran el riesgo técnico. Si fallan, simplificar cuerpos, props y poses conservando la fantasía de extracción. No ocultar el fallo detrás de animaciones o cambiar al juego entero a una cinemática.

## 04. M3 y M4: muestra completa y conexión por internet

**M3, The Last Round:** construir barra, mesas, salida, vereda y camioneta en blockout. Completar cuatro ataques, fatiga, anclajes, una distracción y falsa cooperación. Incorporar reloj, tres niveles, individual y escalado a cuatro. El final exige colocación válida y cierre de puerta. Integrar resultados y reinicio.

Se ejecutan las doce combinaciones de uno a cuatro jugadores y tres niveles. En individual se usa carrito y auto-recuperación; con cuatro se exige coordinación funcional. No hay puerta que requiera obligatoriamente cuatro manos. Las desconexiones limpian vínculos y recalculan presión sin quitar tiempo.

Evidencias M3: una partida completa ganada, una perdida por tiempo, una individual y una de cuatro con roles intercambiables. Registrar eventos y fallos físicos. Criterio de salida: el núcleo es comprensible y hay razones para repetirlo, además de estabilidad técnica.

**M4, online real:** configurar proyecto de servicio bajo cuenta del dueño; integrar MPS Sessions con Relay; gestionar crear, unir por código, listo, carga, salida y errores. Mantener el proveedor local para ensayos sin servicios. El coste del proveedor se observa con tráfico real y alertas antes de abrir pruebas amplias.

Probar dos equipos en redes distintas sin reenvío manual de puertos. Probar invitaciones o códigos inválidos, sala llena, versión diferente, timeout de carga y cierre del host. La primera versión no tiene migración de host ni entrada a ronda en curso. El menú debe decirlo con comportamiento concreto, no dejar una pantalla congelada.

Evidencias M4: builds y cuentas usadas, tipo de conexión, resultados de latencia, errores manejados y captura de estadísticas de consumo que no exponga secretos. Criterio de salida: un grupo puede entrar y completar una ronda sin intervención del desarrollador.

## 05. M5 y M6: diversión, arte y contenido adicional

**M5, validación de diversión:** probar con grupos que no participaron del diseño. Empezar con seis grupos como ronda exploratoria si es viable, incluyendo experiencia de una, dos y cuatro personas. Usar el formulario incluido y observar sin rescatar cada confusión con instrucciones verbales. La muestra es pequeña; sus resultados orientan ajustes y no demuestran viralidad.

Medir legibilidad, control perdido, tiempo estancado y estrategias descubiertas. Identificar qué momentos los participantes recuerdan. Criterio de salida: los jugadores entienden el objetivo, reconocen contraacciones y varios quieren repetir espontáneamente. Si el problema es sensación de azar, ajustar señales y recuperación antes de sumar frases.

**M6, muestra con presentación final:** fijar modelo del tío, un rig probado, ropa de amigos, bar estilizado, HUD, primeros efectos y selección de voces. Reemplazar placeholders de forma incremental y repetir las pruebas físicas. La muestra final mantiene las mismas reglas que el laboratorio, no una versión distinta montada solo para un tráiler.

El manifiesto de assets guía producción. Priorizar NPC, manos, carrito, barra, puerta, camioneta y señales. Las decenas de props decorativos se hacen después. Cada asset tiene revisión de escala, pivote, colisión, origen y presupuesto. Si se usa generación de imágenes, conservar referencia y distinguir concepto de contenido incluido en la build.

La variante Marihuana / Filósofo se inicia después de estabilizar esta muestra, o se retrasa fuera del lanzamiento si compite con calidad del núcleo. Necesita comportamientos y estrategia diferentes, no solo un filtro y diez líneas. La variante Mezcla y voz de proximidad permanecen P2 hasta estimar y validar su coste.

Criterio de salida M6: partida completa con la dirección visual buscada, señales aún legibles, mezcla de audio controlada, mando y localización funcionales. Una imagen de portada o un video editado no reemplazan esa evidencia.

## 06. M7 y M8: Steam, calidad y lanzamiento

**M7, integración de plataforma:** preparar cuenta Steamworks, app, builds privadas y adaptación de funciones Steam. La tarifa oficial consultada es USD 100 o equivalente por aplicación; es recuperable bajo las condiciones de ingresos indicadas por Valve [S6](https://partner.steamgames.com/doc/gettingstarted/appfee). No es el coste total de desarrollar, vender ni operar el juego. El dueño realiza pago y datos de cuenta.

Conectar overlay, presencia/invitaciones según la solución elegida y logros básicos si agregan valor. Probar con cuentas y builds reales. Los textos de tienda anuncian solo contenido disponible. Las capturas muestran gameplay de la build; las ilustraciones de este paquete no se presentan como capturas [S7](https://partner.steamgames.com/doc/store/review_process). Revisar los tamaños vigentes de cápsulas y assets en plantillas oficiales [S8](https://partner.steamgames.com/doc/store/assets/standard).

Completar encuesta de contenido según lenguaje, violencia caricaturesca y sustancias efectivamente presentes. Revisar el apartado de contenido generado con IA para arte, voces, narrativa o traducción que realmente se incluyan [S9](https://partner.steamgames.com/doc/gettingstarted/contentsurvey). El bot programado mediante estados no requiere un modelo generativo en ejecución.

**M8, candidato a lanzamiento:** ejecutar matriz QA, pruebas largas, reinstalación, guardado corrupto, cambios de idioma, mando, mezcla de audio, caídas de red y rendimiento en equipos de referencia. Definir requisitos mínimos mediante medición. Corregir bloqueos antes de ampliar contenido.

Valve revisa tanto la página como la build. La guía consultada indica tiempos típicos de 3 a 5 días hábiles y recomienda planificar al menos 7 para revisión; no es una garantía de aprobación ni de fecha [S7](https://partner.steamgames.com/doc/store/review_process). Comprobar también requisitos vigentes de onboarding y Coming Soon al planificar el calendario. No fijar lanzamiento hasta que el proyecto cumpla sus condiciones reales.

Criterio de salida: build reproducible, sin bloqueos conocidos, online probado, configuración persistente, fuentes/licencias registradas y material de tienda fiel a la experiencia. Preparar una rama de corrección, respaldo de versión publicada y forma de recibir reportes. El mantenimiento posterior forma parte del trabajo; no termina al subir un ZIP a Steam.

## 07. Backlog, estimación y presupuesto

data/backlog.csv descompone los hitos en tareas con dependencias y tamaño relativo S/M/L. Son tamaños de planificación, no horas garantizadas. Un proyecto que empieza sin experiencia en Unity necesita aprender herramientas y realizar pruebas humanas. No se asigna una fecha total ficticia a partir de la cantidad de prompts que se puedan enviar a Claude.

Planificar por evidencia: al terminar M1 se estima física; al terminar M2 se estima red; al terminar M5 se decide cuánto contenido merece producción. Si se necesita un calendario, usar la velocidad observada de esas tareas y reservar margen de integración, QA y publicación. Comprar assets puede ahorrar modelado, pero también añade integración y revisión de licencias.

| Partida de gasto | Tratamiento recomendado |
| --- | --- |
| Steam Direct | Tarifa oficial por app; verificar moneda e impuestos al pagar. |
| Motor | Revisar elegibilidad y términos actuales del plan seleccionado. |
| Online | Medir horas y tráfico; consultar tarifas y límites del proveedor. |
| Modelos y animación | Cotizar lo que realmente falte después del rig de prueba. |
| Audio y voces | Cotizar selección concreta de líneas, derechos y revisiones. |
| QA | Reservar equipos, participantes y tiempo de corrección. |
| Marketing | Crear clips de gameplay real y probar interés antes de escalar gasto. |
| Herramientas de IA | Considerar límites y costes del entorno que se use. |

No se incluyen cotizaciones de freelancers ni un presupuesto global inventado. Para comparar un asset pago con producción propia, listar coste, derechos, adaptación, compatibilidad y riesgo de rehacerlo. Las compras quedan a cargo del dueño con un resultado revisable.

La hipótesis inicial de precio a probar es USD 4,99 a 7,99 para una experiencia compacta. Es una decisión comercial exploratoria, no un precio recomendado por Steam ni una predicción de ventas. Se decide después de conocer contenido, calidad, duración y respuesta de playtests. No se utiliza cantidad de reseñas de otro juego para inferir ingresos propios.

## 08. Pruebas, criterios de corte y seguimiento

Todos los casos de data/qa_matrix.csv comienzan NOT_RUN. El responsable registra build, equipo, fecha, resultado y evidencia. Un caso automatizado que pasa en EditMode no reemplaza la revisión visual de una caída o una prueba con dos redes reales.

Bloqueos de lanzamiento: no poder ganar en alguna configuración anunciada; uniones huérfanas; ataques invisibles o duplicados; control perdido permanente; cliente que no puede jugar; error de conexión sin salida; guardado que impide iniciar; diferencias entre funciones anunciadas y build. Los fallos cosméticos se priorizan por visibilidad sin convertirse automáticamente en bloqueos.

Un registro de riesgos usa probabilidad estimada, impacto, señal temprana, mitigación y dueño. Riesgos iniciales: estabilidad de joints, respuesta del cliente, diversión repetida, sobreproducción de contenido, coste online, coherencia del arte y dependencia de servicios. Revisarlo al terminar cada hito.

Cada sesión de Claude actualiza PROGRESS.md con hito actual, archivos cambiados, validación ejecutada, fallos, tareas siguientes y decisiones. Mantener una lista corta de pendientes técnicos reales. No mezclar «me gustaría agregar» con «falta para que funcione». Conservar DECISIONES.md como autoridad para alcance.

Si el núcleo no funciona, reducir complejidad sin perder su identidad. Ejemplos: ragdoll simplificado, menos props móviles, una sola ruta, dos ataques primero. Si el núcleo funciona pero el humor se agota, estudiar personalidad, estrategias y variación contextual antes de agregar una campaña enorme.

La señal comercial más útil en esta etapa es interés por jugar lo que ya existe. Un clip atractivo puede atraer curiosidad, pero debe llevar a una demo representativa. No se fijan tasas de wishlist ni objetivos de viralidad sin una campaña y datos propios. El trabajo se evalúa por decisiones y pruebas, no por promesas.

## 09. Protocolo de trabajo para Claude

El prompt maestro define rol, alcance y lectura. Claude debe sintetizar decisiones fijadas, detectar contradicciones materiales y arrancar el primer trabajo viable. Puede resolver detalles menores mediante supuestos escritos; no tiene que pedir confirmación por cada nombre de clase. Una dependencia externa pendiente se reporta mientras continúa lo independiente.

Reglas para implementación: mantener un proyecto ejecutable; cambios incrementales; APIs verificadas en versión instalada; herramientas de editor reproducibles; datos separados; cero secretos; limpieza de escena y red; pruebas con evidencia; mensajes de error concretos. No fabricar un proyecto entero con nombres de archivos que nunca fueron importados por Unity.

La entrega de un hito incluye: problema resuelto, archivos relevantes, cómo abrirlo, cómo probarlo, qué se ejecutó, qué está pendiente y siguiente objetivo. Evitar afirmar «listo para Steam» mientras falten internet, arte, cuentas, controladores o QA. El dueño debe poder reproducir una acción sin reconstruir una conversación larga.

Los prompts de fases no ordenan reescribir lo ya probado. Cada uno comienza leyendo PROGRESS.md y la configuración real. Si el contexto se corta, continuar desde ese estado. Un asistente nuevo debe encontrar proyecto, versiones y evidencia, no depender de memoria del chat.

Las capacidades de Claude varían con el entorno. Con acceso solo a chat puede producir instrucciones y código que alguien integra; con acceso a archivos puede construir estructura; con Unity y ejecución puede comprobar compilación y escenas; con equipos y cuentas puede probar online y plataforma. Este paquete prepara el trabajo para esos escenarios sin prometer automatización de los pasos humanos.

## 10. Entregables y primera acción concreta

El ZIP incluye cuatro documentos en PDF y Markdown, dos conceptos visuales, planos y wireframes, balance JSON, tablas CSV de estados/diálogos/assets/tareas/pruebas, fuentes, registro de decisiones, formulario de playtest y prompts de implementación. README.md indica cómo recorrerlos. MANIFEST.json permite comprobar nombres y hashes de los archivos empacados.

La primera acción es entregar a Claude la carpeta extraída y el contenido de claude/00_PROMPT_MAESTRO.md. Ese prompt inicia M0 y M1 con un laboratorio de agarre, resistencia y empujón. Si el editor no está disponible, Claude prepara lo que pueda y devuelve los pasos exactos de verificación pendientes; no simula haber probado.

Al completar el laboratorio, el siguiente cuello de botella es el segundo cliente. La dirección visual ya tiene intención definida, pero la producción masiva de assets espera esa prueba. Se conserva así una secuencia útil: mecánica, red, recorrido, diversión, presentación y publicación.

El objetivo final es una experiencia compacta que invite a decir «tenemos que probarlo entre cuatro». La documentación deja esa intención clara y proporciona criterios para comprobarla. La ejecución y los playtests decidirán la forma final del producto.
