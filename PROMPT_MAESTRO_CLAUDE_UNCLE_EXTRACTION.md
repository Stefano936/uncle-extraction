# UNCLE EXTRACTION — Prompt maestro de implementación para Claude
Versión 1.0 · 15 de noviembre de 2026 · Preparado para Stefano Francolino

Actuá como desarrollador principal de UNCLE EXTRACTION, un juego indie 3D para Windows x64 con distribución futura en Steam. Tu tarea es construir el juego trabajando sobre los archivos reales del proyecto: programar, integrar los recursos entregados, crear escenas, ejecutar Unity cuando esté disponible, generar builds, probar, corregir y dejar todo reproducible.

Quiero ejecución concreta por etapas verificables. Empezá con la inspección y la primera implementación en esta misma sesión. Tu plan inicial debe ser breve y conducir al trabajo. El objetivo de esta primera etapa es un prototipo jugable; la publicación comercial se prepara más adelante, con las validaciones correspondientes.

## 1. Material entregado y estado real

Tenés dos paquetes de entrada:
- UNCLE_EXTRACTION_entrega_unificada_para_Claude.zip.
- UNCLE_EXTRACTION_materiales_y_texturas_v01.zip.

El primero contiene una carpeta raíz UNCLE_EXTRACTION_para_Claude con documentación, datos, kit 3D y kit UI. El segundo agrega uncle_extraction_kit_materiales, que debe quedar dentro de esa raíz, junto a las otras carpetas. Si ya están extraídos, trabajá sobre esas carpetas sin duplicarlas. Si hay un proyecto Unity existente en otro lugar autorizado, identificá su ubicación y continuá ahí.

La estructura de referencia es:
- 00_EMPEZAR_AQUI.md.
- uncle_extraction/: diseño, arquitectura, arte conceptual, plan, datos y prompts por hito.
- uncle_extraction_kit_3D/: FBX, GLB, escena editable de Blender, metadatos y guía.
- uncle_extraction_kit_UI/: pantallas de referencia, componentes, iconos, fuentes, textos y especificación.
- uncle_extraction_kit_materiales/: texturas, variantes visuales de modelos, recetas y fichas de interacción.

El material es preproducción. Se hicieron verificaciones de exportación en Blender y comprobaciones documentales y gráficas. No se entregó un juego Unity ejecutado ni una red probada. Los tests del catálogo siguen pendientes hasta que se ejecuten. Conservá esa distinción en todos tus informes.

Los prompts anteriores son contexto y tareas por hito; no ejecutes cada uno como si fuera un proyecto independiente. Este prompt coordina la implementación y agrega el complemento de materiales a la entrada unificada.

## 2. Cómo debés trabajar y qué herramientas comprobar

Inspeccioná primero la carpeta autorizada, instrucciones locales aplicables, estado de Git, cambios existentes y PROGRESS.md. No reinicies un proyecto por recibir este paquete ni sobrescribas trabajo anterior.

Comprobá sistema operativo, shell, acceso de escritura, Git, Unity Hub, ejecutable y versión de Unity, licencia activa, módulos de compilación Windows, herramientas C# y Blender si lo necesitás. Comprobá también qué acceso real tenés al editor y a la pantalla.

Usá terminal y herramientas de archivos para código, datos, builds, pruebas y logs. Usá herramientas de editor de Unity para generar escenas, prefabs y configuraciones reproducibles. Si disponés de una integración con Unity ya habilitada, verificala antes de depender de ella.

Si tenés control de escritorio habilitado y autorizado, utilizalo para abrir Unity, revisar el Inspector y la consola, entrar en Play y comprobar la presentación y los controles que puedas ejercer. El acceso a terminal por sí solo no demuestra acceso visual ni capacidad de jugar. Describí con precisión los límites observados.

No instales integraciones de terceros innecesarias ni cambies la configuración general de la computadora para compensar una limitación. Si falta una instalación, licencia o acceso imprescindible, explicá el paso mínimo que necesita hacer el usuario y seguí con las tareas independientes posibles.

Trabajá dentro del proyecto. Conservá las fuentes originales de los kits. Para integrar recursos, copiá los necesarios al proyecto y registrá su procedencia. No publiques ni hagas compras, altas de servicios de pago o aceptación de contratos por cuenta del usuario.

## 3. Lectura y precedencia

Leé primero:
1. 00_EMPEZAR_AQUI.md.
2. uncle_extraction/DECISIONES.md y README.md.
3. uncle_extraction/docs/01_GDD_diseno_del_juego.md.
4. uncle_extraction/docs/02_especificacion_tecnica.md.
5. uncle_extraction/data/game_balance.json y data/LEEME_DATOS.md.
6. uncle_extraction/docs/03_biblia_visual_y_sonora.md.
7. uncle_extraction/docs/04_plan_de_produccion_y_handoff.md.
8. uncle_extraction/claude/00_PROMPT_MAESTRO.md.
9. Los README e instrucciones claude/INTEGRAR_KIT.md, INTEGRAR_UI.md e INTEGRAR_MATERIALES.md de cada kit.
10. La especificación, metadatos y datos necesarios para el hito activo.

Consultá backlog.csv, npc_states.csv, qa_matrix.csv, dialogue_en_es.csv, asset_manifest.csv, PLAYTEST.md y los archivos de UI/materiales según la tarea. Usá Markdown y datos como fuentes de implementación; los PDF duplican su contenido para revisión humana. Leé FUENTES.md y verificá APIs en documentación oficial de las versiones instaladas.

Precedencia para decisiones de producto:
- Instrucciones explícitas actuales del usuario.
- Requisitos fijos y decisiones aceptadas.
- GDD y especificación técnica para comportamiento.
- Balance y contratos de datos para parámetros y fórmulas.
- Especificaciones de cada kit para integración y presentación.
- Mockups e imágenes para orientación visual.

Los valores iniciales son hipótesis ajustables. Una imagen no habilita una mecánica ni reemplaza una regla de juego. Registrá contradicciones materiales y la resolución. No reescribas todos los documentos para resolver diferencias menores.

## 4. Identidad y alcance que tenés que conservar

De uno a cuatro amigos intentan sacar de un bar a un adulto borracho controlado por el juego y acomodarlo en una camioneta estacionada. La cámara es en primera persona. Cada jugador tiene dos manos con acciones independientes.

El tío se resiste de manera activa: se aferra a muebles, intenta escapar o volver al bar, coopera por momentos, responde a distracciones, pega, empuja, patea y se sacude. Puede desplazar y derribar de verdad a los jugadores.

El cooperativo debe permitir repartir tareas: sostenerlo, liberar anclajes, distraerlo, usar el carrito y despejar el recorrido. No hace falta imponer clases fijas. En individual, las herramientas y las aperturas del bot permiten completar el objetivo sin exigir varias personas.

Dificultad elegida y cantidad de jugadores son ejes separados. Conservá los tres niveles y las combinaciones de uno a cuatro del diseño. La masa del tío no aumenta con la cantidad de jugadores; se ajustan resistencia, presión y ayudas según las fórmulas. No multipliques dos veces frecuencia o impacto.

El humor es adulto, absurdo y orientado al público angloparlante, con localización al español. El tío debe ser reconocible y gracioso, con reacciones asociadas a situaciones reales de juego. Conservá los diálogos entregados y evitá repetir la misma frase constantemente.

La primera versión se concentra en el perfil de alcohol, un bar, vereda y camioneta fija. Marihuana/Filósofo y Mezcla quedan para ampliaciones posteriores. También quedan fuera del primer alcance las mentiras que alteran la realidad, un jugador controlando al tío, PVP, armas, conducción, mundo abierto, pantalla dividida, migración de host e incorporación a una ronda ya iniciada. Voz por proximidad y captura automática de clips son posteriores.

## 5. Libertad para implementar y mejorar

Podés decidir autónomamente organización de clases, componentes, herramientas internas, nombres nuevos, pruebas, pequeños ajustes de layout, detalles de presentación y soluciones reversibles dentro del alcance.

Podés ajustar fuerzas, amortiguación, alcance, cámara, tiempos y curvas de dificultad si hay un problema observado. Registrá el valor anterior, el nuevo, el motivo y la prueba. Conservá una única fuente de parámetros para que los números no diverjan entre scripts.

Los kits 3D y UI son bases de prototipo. Podés adaptar mallas, animaciones y presentación cuando ayude a la legibilidad o a la interacción. Preservá identificadores y referencias necesarias o documentá su migración. Priorizá una interacción clara y controlable.

Para un cambio sustancial de motor, cámara, topología de red o alcance, presentá el problema concreto y la propuesta antes de reescribir. Los valores tentativos pueden cambiar con pruebas; las decisiones estructurales necesitan una justificación explícita.

Continuá con acciones reversibles ya incluidas sin pedirme aprobación por cada archivo o hito. Respetá las autorizaciones y límites de las herramientas. Si hay un bloqueo externo real, pedí solo la intervención necesaria y mantené el resto del trabajo en marcha.

## 6. Base técnica y reproducción

La base propuesta en el paquete es Unity 6.3 LTS, C#, URP, Input System, Netcode for GameObjects y Unity Transport. Verificá disponibilidad y compatibilidad reales antes de fijar el parche y los paquetes. Si ya existe un proyecto funcional con versiones fijadas, preservalo mientras no haya un problema demostrado.

Usá red autoritativa de anfitrión. Para internet, la ruta prevista es Multiplayer Services Sessions y Relay; la plataforma Steam se incorpora después mediante un adaptador compatible. Lobby, invitaciones y transporte son responsabilidades distintas.

Registrá editor y dependencias exactas en DEPENDENCIES.md, ProjectVersion.txt, manifest.json y packages-lock.json según corresponda. No actualices silenciosamente las dependencias ni copies APIs de tutoriales incompatibles.

Si no existe un proyecto, creá uno en una carpeta claramente separada, por ejemplo UnityProject/, conservando los kits originales. Incluí un .gitignore apropiado para Unity; preservá Assets con sus .meta, Packages y ProjectSettings. No versionés Library, Temp, Logs ni credenciales. Si hay Git, trabajá en una rama local adecuada sin descartar cambios previos; la publicación remota queda pendiente de autorización.

Separá movimiento, agarres, cuerpo del tío, decisiones del bot, ataques, ronda, conexión y presentación. Usá las responsabilidades propuestas en la especificación sin crear una arquitectura innecesariamente grande. UI y audio presentan estado real y envían solicitudes al sistema correspondiente.

Generá escenas, prefabs y referencias mediante Unity y herramientas de editor. No inventes GUID, .meta o YAML de escenas sin importación comprobada. Un script que genera una escena todavía tiene que ejecutarse y verificarse para afirmar que esa escena existe y funciona.

## 7. Reglas del núcleo físico

Usá escala coherente de metros y segundos. Partí del balance entregado, incluido el paso físico inicial a 60 Hz. Conservá una cámara estable desacoplada de la rotación violenta del cuerpo.

Implementá dos manos con selección, solicitud, validación, agarre, esfuerzo y liberación. Verificá alcance, línea de interacción, disponibilidad del socket, estado del jugador y límites de fuerza. Una selección visual no equivale a agarre confirmado.

Las restricciones deben tener fuerza, amortiguación y separación acotadas. Evitá aplicar simultáneamente dos soluciones que dupliquen la fuerza. Comenzá con un cuerpo simplificado del tío si acelera la prueba y evolucioná al ragdoll reducido previsto cuando haya estabilidad.

Limpiá agarres y anclajes al soltar, caer, desconectarse, destruir un objetivo, reiniciar o finalizar la ronda. Evitá fuerzas que sigan actuando sobre manos inexistentes, estiramiento infinito y estados sin salida.

Implementá compostura, tropiezo, caída y recuperación con espacio libre comprobado. El empujón y la patada pueden causar derribo directo según el umbral de diseño. El jugador debe recuperar el control y el bot debe respetar las ventanas de protección.

Los cuatro ataques previstos son slap, push, kick y shake. Cada uno tiene preparación legible, ventana activa, un impacto por objetivo y recuperación obligatoria. Respetá postura, extremidades disponibles, alcance y bloqueo de dirección. El bot no debe girar instantáneamente para anular una esquiva ni atacar sin pausa.

El bot se implementa con estados y reglas programadas. Separá intención de ejecución física. Conservá fatiga y presupuesto de resistencia para producir aperturas aprovechables. No hace falta un modelo de lenguaje ni un servicio de voz en vivo durante la partida.

La extracción debe comprobar los puntos requeridos del cuerpo, pose válida, solapes y cierre durante los tiempos definidos. Tocarse con el trigger no alcanza para ganar. La camioneta permanece estacionada.

## 8. Red temprana

Diseñá la propiedad de los estados desde el laboratorio e integrá un segundo cliente antes de completar todo el contenido.

El anfitrión decide física, sockets, ataques, impactos, bot, temporizador, escalado y resultado. El cliente envía intenciones validadas. No aceptes posiciones arbitrarias o una victoria declarada por un cliente.

El cliente presenta e interpola el cuerpo remoto; no vuelve a simular independientemente el mismo ragdoll. Usá pose reducida y snapshots según lo propuesto, ajustándolos con mediciones.

Probá dos solicitudes simultáneas al mismo socket, liberación durante un ataque, caída, desconexión mientras se agarra, entrada obsoleta, reinicio y salida del host. Una confirmación pendiente debe distinguirse de un agarre aceptado.

Dos procesos en la misma computadora, dos equipos en LAN y dos equipos en redes distintas son pruebas diferentes. Registrá qué escenario ejecutaste. No declares internet verificado con localhost.

Si faltan cuentas de servicios, completá individual y conexión directa posibles. Prepará el adaptador y los pasos de configuración sin inventar credenciales. Al perder al host, mostrales a los clientes una salida clara; no anuncies migración de host si no está implementada.

## 9. Integración de los recursos

### Kit 3D
Preferí FBX para la integración prevista. GLB y Blender son formatos de referencia o requieren importadores comprobados. El tío incluye rig y animaciones de prototipo; los archivos no incorporan por sí mismos controlador, ragdoll, lógica ni red.

Comprobá escala, orientación, jerarquía, sockets y animaciones en Unity. La guía especifica rig Generic y root motion desactivado para la primera prueba. Evitá optimizar huesos antes de resolver sus referencias.

Usá el carrito, barra y geometría entregados. No dupliques suelos y paredes al combinar el shell con una escena completa. Conservá el pivote de bisagra de la puerta y verificá el ancho útil con colisiones reales.

### Materiales
Leé ESPECIFICACION_MATERIALES.md, texture_import.json, material_application.json e interaction_manifest.json del kit. Las cuatro texturas son mapas de color; no se entregó un conjunto PBR completo.

Creá materiales compatibles con el URP instalado y revisá sRGB, repetición, mipmaps, resolución y aspecto. Las variantes usan UV preparadas para iniciar el tiling en 1,1; comprobá la escala antes de multiplicar la densidad otra vez.

Si sustituís una malla para aplicar texturas, preservá prefabs, scripts, cuerpos, colisionadores, sockets y referencias de red. La roughness de un material visual no define fricción física.

Las fichas distinguen objetos entregados de objetos por producir. No supongas que vaso, karaoke o snacks tienen modelo por aparecer en un catálogo. La mesa permanece fija y la camioneta no se conduce.

### Interfaz y audio
Construí componentes interactivos a partir de iconos, paneles, fuentes, tokens, bindings y localización. Las imágenes de pantallas completas son referencia; no simules toda la interfaz con una captura estática.

Primero integrá un HUD mínimo de ambas manos, energía y aviso de ataque, conectado a estados reales. Después, menús, lobby, opciones, pausa, resultados y errores.

Conservá Pending frente a Holding, autoridad de los controles y limpieza de prompts al caer o terminar. Abrir pausa online no detiene al grupo.

Usá claves de localización EN y es_UY, respetá licencias de fuentes y verificá encaje y navegación. Audio y voces que falten deben quedar identificados como pendientes o placeholders. Conservá procedencia y licencias de cada recurso incorporado.

## 10. Hitos y criterios de avance

Mantené el orden M0 a M8 del paquete. Cada hito es un control de calidad, no una pausa para pedir permiso.

M0 — Entorno reproducible: localizar herramientas, fijar versiones compatibles, abrir el proyecto y ejecutar una escena mínima/build vacía si el entorno lo permite.

M1 — PhysicsLab: jugador y cámara, ambas manos, tío físico simple, agarre y liberación, empujón anunciado, caída, levantada, carrito y extracción de prueba. Completar un intento individual y diez reinicios sin agarres huérfanos.

M2 — Red temprana: host y cliente directo en procesos/builds separados. Verificar estados coincidentes y casos de sockets, impactos y desconexiones. Registrar condiciones de latencia realmente ensayadas.

M3 — Bar y bot: recorrido completo, cuatro ataques, anclajes, distracción, fatiga, fuga, victoria, derrota y reintento. Revisar las doce combinaciones de jugadores y dificultad, indicando cuáles requieren participantes o equipos aún no disponibles.

M4 — Internet: integrar Sessions/Relay, preparación del lobby, cupos, versión, carga y errores. Verificar dos equipos en redes diferentes cuando haya acceso y cuentas. Registrar tráfico observado y configuración pendiente.

M5 — Playtest y balance: preparar build, métricas y formulario. Ajustar con observaciones reales. Si no hay participantes, dejar el paquete listo para probar y el hito pendiente, sin inventar feedback.

M6 — Arte, audio y UI: completar presentación usando los kits, conservar claridad de señales y comprobar controles, localización y rendimiento.

M7 — Steam: preparar e integrar funciones reales de plataforma con cuentas y permisos disponibles. Mantener separación de transporte. Preparar material fiel a la build; publicación y pagos requieren instrucciones del dueño.

M8 — Candidato y QA: ejecutar la matriz aplicable, revisar rendimiento con cuatro jugadores, sesiones largas, errores, opciones, reinicios y distribución reproducible. Mantener visibles los bloqueos.

Si un hito requiere una cuenta, participantes o hardware ausentes, dejalo bloqueado con precisión. Podés preparar trabajo independiente de otros hitos sin declarar aprobado el control anterior ni ocultar sus dependencias.

## 11. Validación y evidencia

Cada cambio relevante debe tener una verificación proporcional. Priorizá pruebas de comportamiento: exclusividad y limpieza de sockets, contacto único por ataque, recuperación, escalado, confirmación autoritativa, extracción y reinicio. Usá EditMode, PlayMode y pruebas manuales donde cada una corresponda.

Registrá:
- Versión o commit al que corresponde la prueba.
- Escenario, comando o procedimiento ejecutado.
- Condiciones de la sesión y número de clientes reales.
- Resultado esperado y observado.
- Log, reporte o captura real cuando exista.
- Estado PASS, FAIL, NOT_RUN o BLOCKED y motivo.

Compilación C#, importación en Unity, ejecución en Play, build Windows, partida completa y conexión por internet son evidencias distintas. Conservá esa separación.

Si no podés ejecutar Unity, podés producir código y herramientas útiles y verificarlos hasta donde llegue tu entorno. Informá que importación, compilación Unity y gameplay siguen pendientes. No presentes una revisión estática como prueba del motor.

Si tenés el editor y módulos disponibles, ejecutá y corregí los errores que puedas resolver. Prepará comandos o scripts de apertura, pruebas y build con rutas detectadas. Esperá resultados y revisá logs; iniciar un comando no prueba su éxito. Separá pruebas gráficas de pruebas batch.

No inventes jugadores, resultados de QA, métricas, capturas de gameplay, compatibilidad de paquetes ni promesas de popularidad. La diversión necesita observación humana real.

## 12. Continuidad y entrega

Mantené PROGRESS.md con hito actual, tareas hechas, archivos relevantes, versiones, verificaciones, problemas, bloqueos y siguiente acción exacta. Usá el template entregado si ayuda.

Mantené DEPENDENCIES.md y un documento de decisiones de implementación con cambios relevantes y evidencia. Podés complementar CLAUDE.md si es útil, conservando instrucciones existentes y enlazando a las fuentes sin duplicar todo.

Dejá instrucciones sencillas para abrir Unity, ejecutar la escena correcta, usar los controles, reiniciar, generar una build y conectar dos instancias. Especificá ubicación de las salidas y limitaciones conocidas.

En cada entrega comunicá:
1. Qué comportamiento nuevo funciona y con qué evidencia.
2. Qué archivos modificaste y dónde ejecutarlo.
3. Qué pruebas pasaron, fallaron o siguen pendientes.
4. Qué limitaciones o pasos humanos bloquean algo.
5. Qué vas a continuar después.

Si se corta la sesión, dejá un punto de continuación reproducible. Una nueva sesión debe poder retomar con este prompt, PROGRESS.md, decisiones y archivos, sin depender de recordar todo el chat.

## 13. Empezá ahora

Localizá el material y el trabajo existente. Leé las fuentes de arranque. Resumí en pocas líneas el estado, las herramientas disponibles, decisiones fijas y primer objetivo. Después implementá M0 y el primer recorrido útil de M1, o continuá desde el último hito real si ya hay avances.

La primera meta concreta es poder abrir PhysicsLab, caminar hasta el tío, agarrarlo, moverlo, recibir un empujón anunciado, caer, recuperarse y completar una extracción de prueba. Mostrá un HUD conectado al estado y dejá el código preparado para M2.

Continuá hacia el segundo cliente cuando el laboratorio esté verificado y las herramientas lo permitan. Entregá archivos reales, verificaciones y pasos de ejecución. Quiero poder probar lo construido y darte feedback para la siguiente iteración.

