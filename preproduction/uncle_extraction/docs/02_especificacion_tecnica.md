# UNCLE EXTRACTION
## 01. Plataforma y decisiones de tecnología

**Especificación técnica propuesta | Versión 1.0 | 15 de noviembre de 2026**

La plataforma objetivo inicial es Windows x64, distribuido en Steam. Se recomienda Unity 6.3 LTS, C#, plantilla Universal 3D y URP. Es una decisión de este proyecto, no una afirmación de que ese motor sea el mejor para todo juego. La razón es reunir físicas 3D, herramientas de escenas, animación, entrada y un ecosistema de C# en una ruta conocida. La página oficial consultada identifica 6.3 como LTS con soporte hasta diciembre de 2027 [S1](https://unity.com/releases/unity-6/support). Al comenzar, seleccionar el parche estable compatible, registrarlo y no actualizarlo silenciosamente.

Paquetes base: Input System, Netcode for GameObjects (NGO), Unity Transport, Test Framework y, para conexión por internet, Multiplayer Services SDK. Usar versiones publicadas compatibles con el editor elegido. La documentación consultada presenta MPS como la ruta unificada de sesiones y marca obsoletos los SDK independientes de Lobby, Relay y Matchmaker [S3, S4]. No copiar tutoriales antiguos sin contrastar las APIs instaladas.

Las primeras fases no requieren Steamworks, servicios cloud ni assets pagos. Un modo individual local y host/cliente directo permiten validar el núcleo. Para internet, la ruta recomendada es host de jugador con Relay mediante MPS. Relay conecta; no ejecuta la simulación. El anfitrión sigue siendo la autoridad. El uso de servicios genera costes que deben medirse antes de publicar [S4](https://docs.unity.com/en-us/mps-sdk/mps-billing).

Steamworks.NET es una opción de integración C# mantenida fuera de Valve [S5](https://github.com/rlabrecque/Steamworks.NET). Se usa para funciones de plataforma como overlay, logros e invitaciones. No se lo confunde con un transporte de NGO: por sí solo no convierte Unity Transport en Steam Networking. La integración de lobby/invitación y la conexión de datos son tareas distintas.

| Componente | Selección inicial | Evidencia pendiente |
| --- | --- | --- |
| Editor | Unity 6.3 LTS, parche por fijar en M0 | Proyecto abre y build vacía ejecuta. |
| Lenguaje / render | C# / URP | Build Windows con escena reproducible. |
| Física | PhysX a través de Unity | Laboratorio estable con 8 agarres. |
| Red de estado | NGO, topología host autoritativo | Dos builds con latencia simulada. |
| Conexión online | MPS Sessions + Relay | Dos máquinas en redes distintas. |
| Plataforma | Steamworks.NET, etapa de integración | Compatibilidad, licencia y cuentas reales. |
| Modelado | Blender o herramienta compatible | Exportación de un rig de prueba. |

No están verificados números de versión de paquetes ni compatibilidad conjunta en una instalación real. M0 convierte estas decisiones en un registro reproducible: ProjectVersion.txt, manifest.json, packages-lock.json y DEPENDENCIES.md. No se adjunta aquí un proyecto Unity ficticio ni archivos YAML de escenas escritos a ciegas.

## 02. Arquitectura de componentes y propiedad

La arquitectura separa reglas, presentación y conexión. Un objeto de juego tiene un estado autoritativo y una representación visual; esa división permite usar el mismo núcleo en individual y cooperativo. Evitar un único script que mezcle menús, IA, agarradas, audio y RPC.

| Módulo | Responsabilidad | Dueño de la decisión |
| --- | --- | --- |
| SessionCoordinator | Crear, unir, preparar, cerrar sesión | Host y proveedor de sesión |
| RoundDirector | Reloj, etapas, éxito, derrota y reinicio | Host |
| PlayerMotor | Movimiento, compostura y recuperación | Host; vista local interpolada |
| GrabAuthority | Solicitudes, cupos, restricciones y liberación | Host |
| NpcBrain | Intenciones y máquina de estados | Host |
| NpcBody | Posturas, articulaciones y resistencia limitada | Host |
| AttackResolver | Preparación, contacto único e impulso | Host |
| DistractionService | Visibilidad, cooldown y resultado | Host |
| ExtractionZone | Validación de carga y cierre | Host |
| Presentation | Animación, UI, audio y efectos | Cada cliente según eventos |
| ProfileStore | Opciones y cosméticos locales | Instancia local |
| Diagnostics | Métricas, trazas y reproducción de semillas | Host y logs locales |

Contratos de aplicación propuestos, no APIs de Unity: IAuthorityContext informa si puede decidir; IConnectionBootstrap expone iniciar individual, host, unir y cerrar; IGrabTarget enumera sockets válidos; IAttackQuery devuelve objetivos de una forma barrida; IGameClock evita mezclar tiempo de render y simulación; IEventSink recibe sucesos para UI, audio y pruebas. Claude puede simplificar estas interfaces si el proyecto queda más claro, conservando responsabilidades.

Datos configurables viven en ScriptableObjects o estructuras serializables de solo lectura durante el intento. data/game_balance.json sirve como semilla importable y contrato de valores. No tiene efecto en un juego hasta que se implemente el importador o se creen los assets equivalentes. Los identificadores de perfiles, ataques, diálogos y objetos deben permanecer estables.

Escenas propuestas: Bootstrap, Frontend, PhysicsLab y Bar_LastRound. Bootstrap conserva servicios; las escenas de juego contienen datos y referencias propias. El reinicio destruye la ronda anterior, limpia vínculos y vuelve a una semilla declarada. Se valida que no sobrevivan gestores duplicados con DontDestroyOnLoad.

## 03. Laboratorio físico y controlador del jugador

Carpetas futuras del proyecto: Assets/Game/Core, Gameplay, AI, Physics, Networking, Presentation, UI, Audio, Data, Editor y Tests. Mantener prefabs por sistema y las herramientas de generación en Editor. Los scripts de editor pueden construir el blockout reproduciblemente; no ejecutar herramientas destructivas sobre escenas del usuario sin conservar su versión.

Usar escala de 1 unidad = 1 metro, gravedad de 9,81 m/s² hacia abajo y paso de física fijo de 1/60 s como punto inicial. La red transmite entradas a 30 Hz y snapshots críticos a 20 Hz; la tasa de render es independiente. Estas cifras deben fijarse en configuración y comprobarse con el profiler, no repartirse como números mágicos.

El jugador usa una cápsula dinámica con rotación bloqueada para el estado de pie, altura aproximada de 1,8 m, radio de 0,32 m y masa de referencia de 80 kg. La fuerza de movimiento busca una velocidad objetivo mediante aceleración limitada. No escribir transform.position para mover un Rigidbody dinámico en cada frame. Resolver el suelo con comprobación específica y pendientes limitadas.

Un impacto cambia velocidad e inicia un estado de tropiezo o caída. El prototipo puede representar la caída con cápsula y una malla animada que cae; un ragdoll completo de cada jugador es mejora posterior si mantiene rendimiento y control de cámara. La cámara se desacopla de la rotación física y regresa con interpolación. Es más valioso controlar una caída que simular cada hueso desde el inicio.

Los objetivos de mano se calculan en espacio del jugador con distancias máximas. El cliente envía dirección de mirada y acciones; el host reconstruye el alcance desde la posición autoritativa. No acepta una coordenada arbitraria de mano que permita agarrar a 20 m. Una preselección local puede resaltar el candidato; la unión física solo se confirma tras validación.

Capas de colisión mínimas: WorldStatic, WorldDynamic, PlayerBody, NpcBody, InteractionQuery, AttackQuery y Cosmetic. Las manos visuales y decoración no colisionan con el cuerpo propio. Durante un agarre se excluyen únicamente pares que produzcan realimentación inevitable entre mano y segmento; no desactivar todas las colisiones entre jugadores y NPC, porque eso destruiría la mecánica de posicionamiento.

El paso evasivo usa comprobación de espacio antes de aplicar velocidad. Agacharse prueba espacio libre antes de volver a pie. La recuperación de una caída busca una cápsula válida cercana; si no la encuentra, prueba puntos seguros de radio limitado y deja una traza. No reaparece dentro de la camioneta para regalar victoria.

PhysicsLab contiene suelo, pared, puerta, obstáculo bajo, rampa, carrito y un muñeco de prueba. Expone botones para 1/2/4 fuerzas de agarre, caída, reconexión simulada y ataque. El laboratorio debe existir como escena ejecutable; un script compilado sin referencias no cuenta como prueba física.

## 04. Cuerpo del NPC y agarres estables

El prototipo comienza con un cuerpo central y segmentos simplificados. La siguiente versión utiliza un ragdoll reducido de once cuerpos: pelvis, torso, cabeza, dos brazos superiores, dos antebrazos, dos muslos y dos pantorrillas. Manos y pies son puntos de contacto o visuales. Su masa total inicial es 110 kg y no cambia con el número de jugadores. La distribución de masas se ajusta para estabilidad; no asignar una cabeza más pesada que el torso.

Las articulaciones limitan rangos anatómicos amplios y cómicos sin permitir estiramiento infinito. Se recomienda investigar ConfigurableJoint en un prototipo aislado. Las fuerzas de postura buscan una pose objetivo con límite de fuerza y torque. El NPC tiene modos Upright, Carried, Dragged y Fallen. El cerebro selecciona intención; el cuerpo convierte esa intención en objetivos compatibles con su postura.

La raíz de red coincide con pelvis o un objeto raíz explícito que siga la pelvis. El host simula todos los cuerpos del NPC. Los clientes presentan una marioneta cinemática; no vuelven a resolver las mismas articulaciones. Para la raíz se puede usar NetworkTransform con NetworkRigidbody. Los huesos secundarios usan snapshots propios y no necesitan once NetworkObjects independientes. La documentación de NGO describe cuerpos no autoritativos cinemáticos y diferencias de eventos de colisión [S2](https://mp-docs.dl.it.unity3d.com/netcode/current/advanced-topics/physics/); por eso los impactos de juego se resuelven en el host.

Para un agarre, el host valida socket y crea una restricción entre segmento y objetivo de mano. Punto inicial de fuerza por mano: 420 N continuos, 560 N durante esfuerzo, con límite global por jugador. El sistema no debe sumar ocho muelles de fuerza ilimitada. Saturar fuerza, amortiguar y restringir velocidad son parte del diseño, no parches opcionales al final.

Modelo conceptual: F = clampMagnitude(k * errorPos - c * errorVel, Fmax). El término de velocidad usa unidades m/s y el error de posición metros. k y c se ajustan en PhysicsLab; no se fijan como cifras físicas universales. ConfigurableJoint o un controlador equivalente puede realizar esta función. Aplicar una sola implementación autoritativa, no muelle manual y joint fuerte a la vez.

La separación de agarre se avisa desde 1,6 m y se libera al superar 2,2 m durante 0,15 s. La referencia es objetivo de mano contra socket, no distancia entre cámaras. Fijar un máximo de velocidad lineal de seguridad de 8 m/s y angular de 12 rad/s para cuerpos críticos; los límites se consideran salvaguardas iniciales y se registran si se alcanzan repetidamente.

Nunca cambiar todo el ragdoll de dinámico a cinemático para cada fase de animación sin preservar velocidades y pose. Un cambio de postura debe ensayarse sin red y luego con red. La recuperación técnica guarda el último conjunto de poses seguras y suelta agarres antes de recolocar.

## 05. Transacción de agarre y resolución de carreras

Una solicitud de agarre contiene requestId creciente por jugador, handId, targetNetworkId, socketId y tick de entrada. El host obtiene la identidad del remitente del canal de red; no confía en un playerId que venga libremente en el payload. Verifica que el jugador esté activo, no caído ni en menú incompatible, que la mano esté libre y que el objetivo exista.

Después valida distancia, línea de visión con geometría relevante, disponibilidad del socket y compatibilidad de estado. Si dos jugadores reclaman el mismo punto en el mismo tick, una sola transacción gana según el orden autoritativo; la otra recibe SocketBusy. No se crean dos joints y después se intenta resolver el conflicto visualmente.

La respuesta incluye requestId, resultado, grabId y estado de mano. Un cliente ignora respuestas anteriores al último estado confirmado. Los inputs de mantener y soltar se reenvían como estado con secuencia; perder un paquete de liberación no puede mantener a alguien agarrado eternamente. Un timeout de inputs de 0,5 s cancela esfuerzo y, si persiste 1 s, libera manos de ese jugador.

Liberar es idempotente: liberar dos veces el mismo grabId no falla ni borra el de otro. Se ejecuta al soltar, caer, alejarse, desaparecer el objetivo, desconectarse, cargar otra escena, iniciar Secured o reiniciar. Cada joint pertenece a un registro y tiene limpieza explícita. Se registran total de agarres y motivo de liberación para detectar fugas.

El NPC usa un registro separado de anclajes. Una mano del NPC puede ocupar un anclaje de mundo; no compite por el mismo identificador que una mano del jugador. La acción de desanclar requiere un objetivo de mano del NPC visible, dentro de alcance y una interacción mantenida. Esa acción tiene su propio progreso y validación, evitando usar «cualquier E sobre el tío».

En la red, los vínculos se transmiten como eventos fiables y también como estado actual en un snapshot de ronda. Si un evento se pierde por una transición de escena, el snapshot puede reconstruir la presentación. No se envían referencias directas a componentes Unity; se usan IDs de red y sockets deterministas.

El prototipo valida que un jugador pueda sujetar dos segmentos, dos jugadores puedan cruzar sus fuerzas y una desconexión deje cero vínculos huérfanos. La misma validación debe pasar después de diez reinicios, no solo en el primer arranque.

## 06. Cerebro, navegación y ataques

El cerebro evalúa a 5 Hz, mientras las acciones temporizadas avanzan con el reloj autoritativo de simulación. Cada estado tiene condiciones de entrada, salida, duración mínima, máxima, acciones de limpieza e interrupciones permitidas. data/npc_states.csv contiene la matriz base. Los eventos terminales de ronda cancelan cualquier acción pendiente.

El bot calcula puntuaciones de intención para anclar, protestar, distraerse, escapar y atacar. Se usan distancias normalizadas, disponibilidad de miembros, preferencias y fatiga. Una pequeña variación por semilla rompe empates. No se introduce una llamada remota a un modelo de lenguaje. El comportamiento debe ser reproducible y funcionar offline.

La navegación del NPC cuando está libre puede usar un NavMesh para obtener ruta, con el desplazamiento final aplicado al cuerpo físico. No permitir que NavMeshAgent y Rigidbody escriban simultáneamente la posición. Al cargarlo, se suspende el seguimiento de ruta. Al soltarlo, se reevalúa suelo y posición navegable antes de reactivar la marcha.

Cada ataque tiene attackInstanceId, tipo, tick de preparación, tick de inicio, dirección fijada, conjunto de objetivos impactados y tick final. En Windup se publica la señal de forma fiable. El host ejecuta consultas barridas por la zona activa para evitar perder contactos rápidos. No se depende solo de que una animación dispare OnCollisionEnter.

Validaciones previas: el miembro requerido está utilizable, la postura permite el movimiento, hay un objetivo válido, terminó la recuperación y se cumple el intervalo mínimo. En ausencia de ataque posible se pasa a protesta o fatiga; no se inventa un golpe invisible para mantener la frecuencia.

Un impacto reduce compostura, aplica un impulso limitado con componente vertical acotada, rompe agarres definidos y produce un evento. Los números de impulso son parámetros artísticos; empezar con cambios de velocidad objetivo de 2 a 3,2 m/s según ataque y validar con masas reales. No mezclar ForceMode.Force durante un frame con un parámetro calibrado como Impulse. Si el delta de velocidad configurado del ataque alcanza 2,5 m/s después del factor de nivel, iniciar caída directa aunque la compostura no llegue a cero; los demás contactos pueden desplazar y producir un tropiezo breve. Usar el parámetro de ataque, no una velocidad incidental del cuerpo, para decidir este derribo.

Tras un impacto, la inmunidad temporal evita cadenas. El ataque no puede volver a afectar al mismo objetivo con otro collider. La recuperación del bot es ininterrumpible por la selección de una nueva intención agresiva. Si hay una caída colectiva, un flag de gracia impide nuevos ataques hasta que exista al menos un jugador levantado y transcurra la ventana mínima configurada.

La animación puede anticipar y seguir la acción, pero el host decide su tiempo. Un cliente que recibe tarde un evento reproduce la fracción restante de preparación y corrección suave; no vuelve a infligir daño ni reinicia un ataque completo.

## 07. Red, autoridad y sincronización

La topología base es listen server: el anfitrión también juega y simula reglas, NPC, cuerpos dinámicos, agarres, ataques y extracción. Los clientes envían intención de entrada, no decisiones finales. El modo individual usa la misma autoridad de juego con conexión local y sin iniciar servicios externos. El tráfico local debe limitarse a loopback cuando no se haya seleccionado jugar con amigos.

El prototipo admite conexión directa solo para pruebas locales/LAN. No se la presenta como online terminado ni se obliga a usuarios finales a abrir puertos. El hito de internet configura MPS Sessions y Relay, maneja autenticación necesaria del servicio y comprueba dos redes diferentes. El SDK unifica sesión y conexión, pero la simulación física y sus snapshots siguen siendo responsabilidad del juego [S3](https://docs.unity.com/en-us/mps-sdk/sessions-overview).

Estado replicado de ronda: versión de protocolo y contenido, semilla, dificultad, número efectivo, fase, reloj, NPC state y tick. Por jugador: pose, compostura, energía, estado de control y manos. Por NPC: raíz, postura, intención, presupuesto normalizado y huesos visibles. Por objeto dinámico: ID, pose, velocidades cuando sean útiles y estado de reposo.

Usar mensajes fiables para inicio/fin, vínculos, inventario funcional y eventos de ataque. Usar snapshots no fiables secuenciados para poses frecuentes. El cliente interpola con un pequeño buffer. Extrapola de forma limitada si falta una muestra y luego corrige; no mantiene un cuerpo atravesando una pared por segundos para ocultar latencia.

Movimiento local: la primera implementación prioriza autoridad y presentación suavizada. La predicción y reconciliación del jugador se investigan si la latencia vuelve el control inaceptable. No se promete que NGO dé automáticamente predicción física. Las manos pueden tener respuesta visual local, pero los agarres y golpes se confirman por host. Se prueban ambos roles: el anfitrión no debe ser el único que siente buen control.

No se implementa migración de host en la primera versión. Si se va el host, la partida termina con mensaje y se vuelve al menú; los demás conservan sus opciones. Que un servicio pueda cambiar propietario de sesión no reconstruye por sí solo el mundo físico. Documentar esta limitación y no activar etiquetas que sugieran continuidad garantizada.

Se bloquean nuevas incorporaciones durante ronda. Los jugadores que pierden conexión regresan al lobby para el intento siguiente. Si se desea reconexión en el futuro, requiere IDs persistentes, snapshot completo, limpieza y pruebas propias. No se deja a medias un sistema que duplica personajes.

## 08. Rendimiento, ancho de banda y fallos físicos

Objetivo inicial de evaluación: 60 fps a 1080p en calidad media en un PC de referencia que se elegirá y registrará antes de medir. No se publican todavía requisitos mínimos. Se debe perfilar host con cuatro jugadores, porque carga física, IA y red recaen en la misma máquina. El presupuesto propuesto es 16,7 ms por frame, con objetivo de física por debajo de 5 ms en la escena de prueba; son metas, no resultados.

Límites iniciales: un NPC crítico de once cuerpos, cuatro cápsulas de jugador, hasta 50 props móviles, ocho agarres, dos anclajes potenciales pero uno activo en el primer escenario y máximo dos luces dinámicas con sombras en una zona. El resto usa iluminación horneada o luces sin sombras. Los objetos dormidos reducen frecuencia de replicación.

Estimación de planificación para el NPC: raíz de 12 bytes de posición, 8 de rotación cuantizada y once rotaciones locales de 8 bytes suman 108 bytes; con cabecera y estados, reservar aproximadamente 140 por snapshot. A 20 Hz son 2,8 KB/s por cliente antes de sobrecarga real. Esto no es un formato implementado ni una medición. Datos de poses secundarias pueden ajustarse tras probar el rig.

Los props pueden dominar el tráfico: 50 objetos a 32 bytes y 10 Hz representan 16 KB/s por cliente antes de sobrecarga. Reducir frecuencia en reposo, enviar solo lo relevante y no replicar cada botella decorativa. Un techo exploratorio de 40 KB/s de salida por cliente y 120 KB/s desde host hacia tres clientes sirve como alarma temprana, no como requisito garantizado de conexión.

Evitar asignaciones por frame en consultas físicas y formateo de UI. Reutilizar buffers para ataques y proximidad. El audio de múltiples choques agrupa eventos y limita voces. Un prop que rebota 30 veces no produce 30 RPC fiables ni 30 sonidos superpuestos.

Watchdog: detectar posiciones no finitas, velocidad fuera de límites repetida, caída bajo el mapa y ausencia de progreso por atasco técnico. La recuperación suelta vínculos afectados, cancela ataques y coloca en una pose segura del recorrido previo. No recuperar por «jugadores jugando mal»; solo por condiciones técnicas verificables. Un atasco jugable se resuelve con las herramientas normales o reinicio.

Registrar snapshot de diagnóstico sin audio personal: semilla, builds, estados, conteo de joints, razón del rescate y posición. Los logs se guardan localmente con rotación por tamaño. No subirlos a un servicio externo por defecto.

## 09. Datos, balance y persistencia

El JSON de balance utiliza schema_version y bloques explícitos para unidades. La aplicación valida campos obligatorios, rangos y referencias antes de comenzar una ronda. Un archivo inválido debe impedir la configuración modificada y mostrar una explicación en desarrollo; en release se usa una configuración empaquetada válida. No aceptar valores remotos arbitrarios enviados por el cliente.

Fórmulas principales: R_efectivo = resistencia_por_jugadores[N] * factor_resistencia_del_nivel. El techo de fuerza activa y el presupuesto máximo usan ese R. Costes del presupuesto también usan R, preservando un número comparable de acciones. El intervalo mínimo = intervalo_por_jugadores[N] * factor_intervalo_del_nivel. El daño de compostura y el impulso usan el factor de ataque del nivel, sin factor N. La preparación = max(0,8 s, preparación_base * factor_señal_del_nivel).

El reloj inicial = tiempo_base_del_nivel + ayuda_por_jugadores[N], salvo modo sin reloj. Tras una desconexión solo se concede ayuda adicional si corresponde al nuevo tamaño; nunca se resta tiempo. La recomputación es monótona para reloj y gradual para resistencia. Esta regla evita que salir y entrar sea una estrategia: no se permite entrar en mitad de ronda y la ayuda solo se aplica una vez por transición de tamaño descendente.

La semilla determina distribución autorizada y sorteos del bot en un flujo separado de audio decorativo. Reproducibilidad aquí significa misma configuración e inputs pueden ayudar a depurar decisiones; PhysX no se presenta como determinista entre plataformas ni como sistema lockstep. Repetir semilla no garantiza idénticas colisiones.

Persistencia local: opciones, dispositivo preferido, tutorial visto y desbloqueos cosméticos. Incluir save_version, migración de esquema, escritura atómica a archivo temporal y copia de seguridad. Ante archivo corrupto, conservarlo con extensión de respaldo, restablecer valores y avisar. No perder progreso por un volumen de audio fuera de rango.

No guardar partidas físicas a mitad de extracción en la primera versión. Una ronda es corta y reiniciable. Steam Cloud se evalúa para preferencias y cosméticos después de definir conflictos entre equipos; la sincronización no sustituye la migración de formato.

Los CSV incluidos son catálogos de diseño. Importarlos requiere parser que preserve comillas, comas y UTF-8, o conversión controlada a ScriptableObjects. Las claves de diálogo no dependen del texto para que una traducción no cambie referencias.

## 10. Interfaces de usuario, sonido y assets

La UI se implementa con una solución coherente dentro del proyecto. Se recomienda uGUI para HUD y menús por simplicidad de integración, sin mezclar dos frameworks sin una razón. Usar escalado por resolución, navegación por mando, orden de foco y safe areas. Los textos deben soportar español e inglés desde el primer menú funcional.

El AudioDirector recibe eventos de juego con tipo, ID, posición y prioridad. Agrupa choques, corta o aplaza frases de baja prioridad y garantiza que una señal de ataque siga siendo audible. Las voces del bot son archivos preparados, no generación en vivo. El CSV determina selección, subtítulos y enfriamiento; el mezclador controla música, voces, efectos y señales por separado.

Los modelos se exportan en FBX o formato compatible validado, con escala aplicada, eje correcto y materiales sencillos de URP. No confiar en una imagen generada como si contuviera una malla con rig. Los conceptos del paquete sirven para modelar, comprar assets adecuados o guiar generación asistida que después requiere retopología, rig, colisiones y revisión.

Las animaciones se importan en clips separados con eventos cosméticos. Los tiempos de gameplay pertenecen al ataque, no a un evento de animación frágil. Una herramienta de revisión muestra cada clip junto a la ventana activa para comprobar que la mano visible no golpee antes o después de la zona real.

Los materiales se comparten cuando sea razonable. Usar atlas pequeño para utilería, LOD o mallas simples, colisionadores primitivos y una lista de qué objetos son interactivos. No añadir MeshCollider convexo de alta complejidad a todo el bar. Los objetos fijos de fondo pueden compartir geometría y materiales.

Los assets tienen ID, origen, licencia, fecha, modificaciones y estado. El manifiesto no acredita que todos existan: la columna delivered diferencia conceptos entregados de tareas pendientes. Los archivos comerciales o voces necesitan derechos verificables antes del empaquetado público. Eso forma parte de producir contenido reutilizable, no del bot.

## 11. Conexión con Steam y operación online

Steam es la tienda y plataforma de distribución, no el motor. La build Windows debe funcionar fuera del editor. La integración comienza con un adaptador IPlatformServices que permite un proveedor local de desarrollo y uno Steam. El juego sigue pudiendo ensayar sus físicas si Steam no se inicia.

La ruta de conexión elegida mantiene MPS/Relay para datos y usa Steam para invitaciones o presencia. Unirse desde una invitación debe llevar a una sesión válida del servicio; no basta con abrir una lobby de Steam. El prototipo online puede usar un código de sala. La integración de invitación añade la asociación entre sesión y lobby/presencia y valida si la ronda está disponible.

Datos de lobby y códigos no son autoridad de gameplay. Validar protocolo, contenido, cupos y estado antes de admitir. Si se vinculan identidades Steam a perfiles de servicio, implementar autenticación soportada y comprobaciones correspondientes según documentación vigente; no confiar en un SteamID escrito por el cliente. La primera etapa de prueba privada no pretende ser un sistema antitrampas competitivo.

Credenciales de administrador, claves de publisher y secretos de backend no se empaquetan en la build ni se suben al repositorio. La configuración pública de un proyecto de servicio se trata según su documentación; no se presenta como equivalente a una clave secreta. El dueño del proyecto realiza acceso, pagos y aceptación de términos en su cuenta.

La distribución usa ramas privadas para probar actualizaciones, un número de build visible y logs identificables. Pruebas mínimas: invitación aceptada desde menú, aplicación cerrada y lobby lleno; salida de un cliente; salida del host; overlay; mando; ausencia de Steam en modo local de desarrollo y error claro en build comercial si la plataforma es necesaria.

La voz de proximidad queda en P2. Steam Voice captura y comprime voz, pero el juego debe gestionar transmisión, reproducción y atenuación [S10](https://partner.steamgames.com/doc/features/voice). No es un botón de activar proximidad. Si se implementa, añadir pulsar para hablar, selección de entrada, silencio por usuario y pruebas de reconexión. El juego nunca exige micrófono para completar una ronda.

## 12. Verificación técnica y criterios de aceptación

Separar EditMode para reglas puras, PlayMode para escenas y pruebas de builds para red real. Los tests no deben repetir constantes sin verificar conducta. Casos de reglas: escalado, un impacto por objetivo, idempotencia de soltar, presupuesto de fatiga, reloj tras desconexión y migración de configuración. Casos físicos: ocho fuerzas, puerta, carrito y recuperación segura.

Cada hito produce un registro con fecha, commit o hash, editor, paquetes, comando o pasos y resultado. Si solo se compila, escribir «compiló»; si se probó manualmente, describir qué se vio; si no se dispone del editor o de dos equipos, declarar «pendiente». No convertir código escrito en evidencia de ejecución.

Matriz de red: individual, host más uno, host más dos y host más tres; latencias de ensayo 0, 80 y 150 ms RTT; pérdida 0% y 2% en sesiones controladas. Son escenarios de prueba, no umbrales de soporte prometidos. Se observa legibilidad de señales, consistencia de agarres, orden de eventos y recuperación. Revisar a anfitrión y clientes por separado.

Casos obligatorios de carrera: agarrar el mismo socket a la vez, soltar durante golpe, desconectar con dos manos ocupadas, ganar cuando llega un ataque, reiniciar en Windup y cortar conexión durante carga. Ninguno debe dejar referencias inválidas, golpes después de victoria o una pantalla sin salida.

Prueba de resistencia: diez reinicios consecutivos y una sesión de treinta minutos del escenario completo con registro de memoria, joints y objetos. El objetivo es ausencia de crecimiento continuo y de errores no controlados; una caída de fps se investiga con perfil, no se tapa bajando resolución sin análisis.

Antes de un playtest externo, la build debe permitir crear partida, unirse, ganar, perder, volver a lobby y salir sin el editor. Antes de Steam, se agregan las pruebas de plataforma y las doce combinaciones de N y dificultad. data/qa_matrix.csv organiza estos casos y deja resultados como NOT_RUN hasta que alguien los ejecute.

## 13. Riesgos, alternativas y referencias

**Riesgo principal: ragdoll y red.** Mitigación: cuerpo simplificado, autoridad única, límites de fuerza y snapshots de marioneta. Si once cuerpos fallan, reducir a pelvis/torso y cuatro segmentos amplios conservando sockets y ataques. La decisión se basa en un experimento de PhysicsLab; no se compra un controlador complejo antes de medir la necesidad.

**Riesgo de respuesta en cliente.** Mitigación: comprobar dos builds desde M2, suavizado y señal suficiente. Si se necesita predicción, diseñarla como tarea propia con pruebas de reconciliación. Un cambio de framework de red requiere comparación breve y un experimento, no reescribir el juego por preferencia personal.

**Riesgo de coste online.** Mitigación: registrar bytes y horas, consultar tarifas reales y usar alertas del proveedor. No se fija aquí un coste mensual porque depende de servicio, regiones, concurrencia y consumo. Una alternativa futura de Steam Networking necesita transporte compatible verificado y su propio hito; no es intercambiable sin trabajo.

**Riesgo de assets y calidad.** Mitigación: bloquear siluetas, trabajar con blockout, probar un rig antes de producir todas las animaciones y reservar pruebas humanas. Claude puede producir código, importadores y herramientas, pero su capacidad depende del acceso a Unity, ejecución de builds, archivos y pruebas. No se garantiza que una sola instrucción genere un juego vendible completo.

Fuentes primarias consultadas el 15 de noviembre de 2026; listado completo y enlaces en FUENTES.md:

| ID | Fuente | Uso limitado en esta especificación |
| --- | --- | --- |
| S1 | Unity 6 release support | Selección de versión LTS. |
| S2 | Unity Multiplayer: Physics | Modelo autoritativo y cuerpos cinemáticos. |
| S3 | Unity MPS: Sessions overview | Sesiones y conexión. |
| S4 | Unity MPS: Billing and subservices | SDK unificado y costes por servicios. |
| S5 | Repositorio Steamworks.NET | Wrapper C# y licencia declarada. |
| S10 | Steam Voice | Alcance de captura y compresión. |

Todo lo demás en este documento describe decisiones y objetivos de ingeniería del proyecto. Los nombres de contratos son ilustrativos y no deben copiarse como si fueran métodos existentes del SDK.
