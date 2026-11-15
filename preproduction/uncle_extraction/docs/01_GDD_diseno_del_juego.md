# UNCLE EXTRACTION
## 01. Visión, promesa y estado del proyecto

**Documento de diseño del juego (GDD) | Versión 1.0 | 15 de noviembre de 2026**

UNCLE EXTRACTION es un nombre de trabajo. El juego es una comedia física en primera persona para 1 a 4 jugadores en PC: un grupo de amigos tiene que sacar de un bar a un adulto completamente borracho y acomodarlo en una camioneta estacionada. El personaje se resiste, se aferra al mobiliario, negocia, escapa, pega y empuja. La dificultad y la cantidad de jugadores modifican su capacidad de resistencia y la presión de la partida.

La promesa al jugador es: «Tus amigos, un borracho que no se quiere ir y veinte metros que se sienten como una expedición». La frase promocional provisional en inglés es: «The party is over. He disagrees». El humor nace del contacto entre reglas comprensibles y decisiones torpes: agarrar una pierna, bloquear una mano, mover una silla y cerrar una puerta en el momento correcto.

El público inicial son grupos que buscan una sesión breve para reírse y producir anécdotas. La dirección de humor apunta al inglés coloquial de un bar estadounidense ficticio: orgullo absurdo, malas excusas, insultos y actuaciones exageradas. El personaje principal debe resultar memorable y querible incluso cuando arruina el plan. La estética será deliberadamente desprolija, pero las respuestas de los controles y las señales de ataque deben ser claras.

Este paquete es una especificación de preproducción, no un juego implementado. Las cifras de balance, rendimiento, duración y pruebas son objetivos o hipótesis iniciales. Ninguna se presenta como medida real. Los conceptos visuales incluidos son referencias de intención; no son modelos 3D, capturas de gameplay ni assets finales.

La premisa general tiene antecedentes. Se identificaron Holice Uncle, Bring Dad Home y Drunk Dad vs Family; ver FUENTES.md. No se afirma novedad absoluta, disponibilidad comercial del nombre ni probabilidad cuantificable de éxito. La diferenciación propuesta reside en el sistema de extracción corporal, la resistencia contextual del bot, el combate cómico legible y las soluciones con objetos dentro del bar.

**Decisiones fijadas por esta versión:** objetivo de extracción; NPC controlado por el juego; modo individual y cooperativo online de hasta cuatro; golpes del bot; escalado por tamaño de grupo; dificultad seleccionable; estética estilizada; humor adulto; primer escenario compacto. Las mentiras que alteran la realidad, sugeridas durante la ideación, quedan fuera de esta versión. No hay poderes sobrenaturales.

## 02. Pilares, límites y vocabulario

**Pilar 1: resistencia con intención.** El tío no es una bolsa de peso ni un enemigo que haya que matar. Quiere seguir en el bar. Busca una cerveza, volver a su asiento, abrazar a un conocido o demostrar que camina solo. Sus acciones siempre deben poder explicarse por una intención visible.

**Pilar 2: cooperación que cambia el resultado.** Dos jugadores coordinados pueden liberar una mano y tirar durante una apertura; cuatro pueden distribuir transporte, control del entorno y distracción. Nadie elige una clase obligatoria. Los roles aparecen durante la situación y se intercambian sin menús.

**Pilar 3: caos con reglas.** El bot anuncia los ataques. Las agarradas tienen límites. Los objetos reaccionan de manera consistente. Perder una posición debe enseñar algo: «nos pateó porque nos pusimos delante de las piernas», en lugar de «el juego decidió que perdíamos».

**Pilar 4: humor que se juega.** Una frase puede anunciar una patada, distraer al grupo o justificar que el tío se aferre a una columna. Los diálogos acompañan acciones. El chiste sigue funcionando si se juega sin voces porque la postura, el sonido y los subtítulos comunican la situación.

**Pilar 5: entrar rápido, volver a intentar rápido.** El objetivo inicial es llegar al primer contacto con el bot en menos de 60 segundos desde que empieza el escenario, y reiniciar un intento desde resultados en menos de 15 segundos en el equipo de referencia. Son objetivos de experiencia, sujetos a medición.

| Término | Definición operativa |
| --- | --- |
| Tío / sujeto | NPC adulto que debe ser extraído. No presupone parentesco real con todos los jugadores. |
| Agarre | Vínculo validado entre una mano del jugador y un punto del NPC u objeto. |
| Anclaje | Objeto fijo al que se aferra el NPC para frenar el transporte. |
| Apertura | Intervalo breve de recuperación en el que conviene trasladarlo. |
| Compostura | Capacidad del jugador de permanecer en pie; no representa salud médica. |
| Fatiga del tío | Recurso del bot que limita ráfagas de resistencia y habilita pausas. |
| Extracción | NPC dentro del volumen válido de la camioneta y cierre confirmado. |
| Muestra jugable | Un escenario pequeño que demuestra la calidad pretendida. |

Quedan fuera del lanzamiento base propuesto: mundo abierto, conducción de la camioneta, combate con armas, muerte del NPC, PVP, control del borracho por un quinto jugador, economía de compras reales, pantalla dividida, matchmaking competitivo y generación de diálogos en vivo. La voz de proximidad es una mejora posterior; el primer prototipo usa comunicación externa o marcadores.

## 03. Bucle de partida y condiciones de cierre

La sesión comienza en un lobby privado. El anfitrión selecciona dificultad, semilla y variante habilitada. Cada participante confirma que está listo. El número efectivo de jugadores se fija al cargar el escenario. Se muestran tres instrucciones: agarrar, soltar y retroceder ante la señal de ataque. La primera habitación enseña esas acciones dentro del juego.

**Bucle de cinco pasos:** observar la intención del tío; preparar una ruta; establecer agarres; aprovechar una apertura para transportarlo; resolver la nueva resistencia. La repetición introduce cambios de postura, objetos y ubicación, no una sucesión de botones rápidos obligatorios.

El recorrido tiene tres etapas reconocibles. Dentro del bar, el reto es separarlo de anclajes y conducirlo entre mesas. En el umbral, hay que orientar el cuerpo y evitar que una extremidad se enganche. En la vereda, hay más espacio pero también oportunidades de fuga; finalmente hay que alinearlo con la camioneta.

**Victoria:** el centro del torso y los puntos de validación de hombros y pelvis permanecen dentro del volumen de carga durante 1,5 segundos; ninguna parte principal atraviesa un obstáculo sólido; la puerta está libre; un jugador mantiene la acción de cierre durante 1 segundo. El bot entra entonces en Secured, se liberan agarres y se reproduce el cierre. La comprobación es autoritativa y solo se ejecuta una vez por intento. El área de carga usa un vehículo de pasajeros, no un maletero pequeño.

**Derrota:** en los modos con reloj, el tiempo llega a cero. La ficción es que termina la ventana de recogida y el grupo abandona el intento. En modo relajado no hay derrota por reloj. Las caídas de jugadores son contratiempos recuperables y no eliminaciones permanentes. El tío no muere y no se pierde por una barra de salud oculta.

Si todo el grupo cae, el bot recibe una pausa de ataques y los jugadores recuperan el control. El reloj continúa en Normal y Difícil. En Relajado se detiene durante la recuperación colectiva. Un fallo de colisiones no se cuenta como fracaso: el sistema rescata el objeto afectado al último punto seguro, informa brevemente y registra el incidente.

El resultado muestra tiempo, distancia recuperada, caídas, fugas del tío y un máximo de tres reconocimientos cómicos. No da una nota moral por consumir sustancias. Se puede repetir la misma semilla para mejorar la ejecución o pedir otra configuración.

## 04. Cámara, movimiento y controles

Se propone primera persona para favorecer la cercanía física y las conversaciones entre amigos. El jugador ve sus manos, el punto de agarre y suficiente cuerpo de los demás para entender la maniobra. La cámara permanece estabilizada durante la mayor parte de la acción. Una caída puede inclinarla de forma limitada; no debe heredar cada giro del ragdoll.

| Acción | Teclado y ratón, propuesta | Mando, propuesta |
| --- | --- | --- |
| Mover / mirar | WASD / ratón | Stick izquierdo / derecho |
| Agarrar o mantener mano izquierda | Botón izquierdo | Gatillo izquierdo |
| Agarrar o mantener mano derecha | Botón derecho | Gatillo derecho |
| Interacción contextual | E | Botón sur |
| Soltar todo / cancelar acción | Q | Botón este |
| Esfuerzo breve | Shift, mientras sostiene | Botón de stick izquierdo |
| Agacharse | Ctrl | Botón de stick derecho |
| Paso evasivo | Espacio, con dirección | Botón oeste |
| Marcador / rueda breve | F | Cruceta arriba |
| Menú y opciones | Esc | Menú |

La implementación usa acciones remapeables. Los nombres de botones en pantalla provienen del dispositivo activo. El modo alternativo de agarre permite alternar en lugar de mantener. Mantener dos manos ocupadas reduce la velocidad y bloquea interacciones incompatibles. No existe un minijuego de pulsar repetidamente para resistir.

Valores iniciales: caminar 4,2 m/s sin carga; movimiento con agarre limitado a 2,8 m/s; agachado 2 m/s; paso evasivo de 1,1 m con 1,2 s de recarga. El paso usa barrido de colisiones y no atraviesa mesas ni al NPC. El esfuerzo dura hasta 1,25 s, consume energía y aumenta temporalmente la fuerza de agarre; no convierte la marcha en una carrera que arrastre el cuerpo a través de la geometría.

El alcance de inicio de agarre es 1,4 m medidos desde la posición de la mano virtual validada. El retículo muestra la extremidad o el objeto seleccionable y un contorno fino. Un intento fuera de alcance responde con un sonido suave y una etiqueta breve. No se confirma visualmente un agarre permanente antes de la respuesta del host.

La sensibilidad, inversión vertical, campo de visión, vibración, sacudida, balanceo de cámara y desenfoque se configuran por separado. Valores de campo de visión inicial de 85 grados verticales deben validarse contra la implementación real del motor; la interfaz debe indicar la convención usada. No se requiere cámara borrosa para comunicar intoxicación.

## 05. Sistema de agarre, transporte y objetos

Cada jugador dispone de dos manos y puede usar ambas sobre el NPC o reservar una para un objeto. Los puntos válidos del tío son antebrazos, tobillos, dos asas de ropa en hombros y un asa de cinturón en pelvis. Una mano ocupa un punto; una segunda mano puede usar otro punto del mismo segmento cuando el prefab lo permite. El máximo global del prototipo es ocho agarres de jugador.

El transporte se representa con restricciones físicas suaves y limitadas. El jugador no teletransporta al NPC a la cámara. Al retroceder, cada mano ejerce fuerza hacia un objetivo cercano al cuerpo del jugador; las restricciones ceden o se liberan si se separan demasiado. Agarrar alto ayuda a controlar el torso; agarrar tobillos facilita arrastrar pero deja manos del bot libres. Esta diferencia debe emerger de posturas y reglas simples.

Las fuerzas alineadas ayudan; tirar en direcciones opuestas genera tensión y desperdicia esfuerzo. No se premia tener más agarres si nadie abre una puerta o libera el anclaje. La interfaz muestra tensión en la mano concreta, no una barra gigante que tape el cuerpo. La energía del jugador limita el esfuerzo sostenido; al agotarse se pierde el aumento de fuerza, no se suelta instantáneamente sin aviso.

**Desanclar al NPC:** si su mano sostiene una barra fija, un compañero puede interactuar con esa mano durante 0,9 s sin recibir un golpe para liberarla. El progreso se interrumpe si se aleja o cae. Otra solución es colocar una distracción válida visible y esperar que la suelte. Si el objeto anclado es móvil, se puede mover junto al NPC con mayor dificultad. Los anclajes fijos nunca se arrancan por acumulación ilimitada de fuerzas.

**Objetos de la muestra jugable:** silla liviana, mesa fija, puerta con bisagra y topes, carrito de transporte y vaso señuelo. Los objetos ligeros no golpean con daño médico; pueden afectar compostura si superan un umbral de impacto. Se limita la energía de esos impactos y la cantidad de objetos físicos activos. La mayoría de las botellas del fondo son decoración sin simulación.

El carrito permite apoyar al tío y empujarlo, con un punto de retención que él puede intentar liberar tras una señal. Es la herramienta principal del modo individual. En cooperativo también sirve, pero un NPC descansado puede desestabilizarlo. Las ruedas del prototipo pueden resolverse con un cuerpo y fricción controlada; no se exige suspensión completa.

Soltar una mano, caerse, desconectarse o terminar la partida destruye su vínculo de forma segura. Cada liberación tiene un motivo registrable. Ninguna acción deja una articulación unida a un jugador inexistente.

## 06. Bot: personalidad, percepción y decisiones

El bot combina una máquina de estados con una puntuación sencilla de intenciones. La decisión pertenece al anfitrión. Cada 0,2 s considera los estímulos cercanos: agarres presentes, anclajes visibles, cerveza señuelo, asiento favorito, salida, fatiga y ataques recientes. La animación no decide por sí sola si un golpe ocurrió.

Su personalidad base es un adulto fanfarrón y obstinado: cree que conoce al dueño, asegura que ya pidió un taxi y recuerda una hazaña cada vez que alguien intenta levantarlo. Su objetivo operativo es permanecer en el bar o volver a una zona favorita. No persigue jugadores por todo el mapa para lastimarlos cuando nadie está intentando moverlo.

| Estado | Conducta y salida principal |
| --- | --- |
| Loiter | Sentado o de pie junto a su interés; pasa a Protest si lo agarran. |
| Protest | Habla, cambia el peso y avisa de resistencia; elige Anchor, Struggle o Windup. |
| Anchor | Se aferra a un punto válido; sale al liberarlo, distraerlo o agotarse. |
| Struggle | Resistencia física moderada con una duración máxima. |
| Windup | Prepara un ataque elegido y fija dirección; no cambia de golpe al final. |
| Attack | Ventana activa corta; un contacto válido por objetivo. |
| Recover | Baja resistencia; no encadena otro ataque inmediatamente. |
| Distracted | Atiende un señuelo durante una ventana acotada. |
| Escape | Camina hacia su zona si queda libre; no corre más rápido que los jugadores. |
| Stumble | Pierde equilibrio y pasa a postura física recuperable. |
| Secured | Estado terminal de éxito; cancela toda resistencia. |

Una jerarquía resuelve conflictos: Secured y rescate técnico tienen precedencia; un ataque ya iniciado continúa salvo interrupción definida; Recover impide ataques; después se valoran distracción, anclaje, escape y protesta. El cuerpo puede estar cargado y el cerebro en Windup; postura e intención son variables distintas.

El NPC recuerda si una distracción funcionó recientemente. El mismo vaso no puede bloquearlo de manera infinita. Las frases tienen enfriamiento y selección sin repetición inmediata. Un estado con duración máxima siempre tiene salida de seguridad; por ejemplo, Struggle termina en Recover aunque los jugadores sigan agarrados.

La aleatoriedad decide entre varias opciones válidas con semilla de partida. No genera ataques imposibles: una patada necesita piernas con espacio, un manotazo necesita un brazo disponible y un empujón requiere postura apoyada. Una configuración de depuración muestra intención, objetivo, fatiga y razones de rechazo.

## 07. Golpes, empujones y recuperación de jugadores

El bot puede pegar de verdad dentro de las reglas del juego: sus ataques producen impulso, rompen agarres y pueden tirar a los jugadores. El estilo visual es slapstick: cuerpos de goma, expresiones exageradas y golpes contundentes sin sangre. La dificultad surge de perder posición y coordinación.

| Ataque | Preparación base | Ventana activa | Recuperación | Respuesta del jugador |
| --- | --- | --- | --- | --- |
| Manotazo | 0,9 s | 0,18 s | 1,5 s | Salir del arco o controlar ese brazo. |
| Empujón frontal | 1,1 s | 0,22 s | 1,8 s | Separarse de su frente y tirar desde un lado. |
| Patada | 1,2 s | 0,2 s | 2 s | Soltar el tobillo marcado o colocarse lateralmente. |
| Sacudida corporal | 1,4 s | 0,3 s | 2,3 s | Soltar a tiempo y volver durante la apertura. |

Cada preparación tiene una postura inequívoca, un sonido corto y una señal visual accesible. El manotazo levanta el hombro y la mano; el empujón apoya pies y lleva las palmas hacia atrás; la patada flexiona una rodilla; la sacudida incluye una inhalación teatral. Una vez fijada la dirección, el bot puede girar un máximo limitado hasta el inicio del contacto. Nunca sigue instantáneamente a un jugador que ya esquivó.

El manotazo afecta un arco cercano. El empujón usa un cono. La patada ocupa una zona estrecha. La sacudida puede afectar a quienes sigan sujetando puntos del NPC; se limita a dos jugadores impactados en Normal. En Difícil puede afectar más, pero requiere una señal clara y conserva la recuperación garantizada.

La compostura inicial es 100. Un manotazo resta 35, un empujón 50, una patada 60 y una sacudida 45 antes de modificadores. Al llegar a cero, el jugador cae y suelta todo. Además, un impacto cuyo cambio de velocidad de diseño alcance 2,5 m/s provoca una caída directa: con los valores iniciales, el empujón y la patada en Normal pueden tirarlo aunque conserve compostura. Los impactos menores producen desplazamiento y tropiezo breve. Esta regla garantiza derribos reales sin depender de acumular golpes antes de la regeneración. La caída dura como máximo 2,2 s antes de iniciar levantada; la invulnerabilidad tras levantarse dura 1,5 s. La compostura se recupera fuera de golpes después de 2,5 s, a 20 puntos por segundo. Una ayuda de compañero reduce el tiempo de levantada, pero nunca es obligatoria en individual.

Un mismo ataque solo puede afectar una vez a cada jugador. No hay golpes que se repliquen porque la mano atraviesa dos colliders del mismo cuerpo. Durante recuperación colectiva se impide una nueva ráfaga. Los ataques gastan fatiga del tío; cuando esta se agota, la apertura es obligatoria y comunicada.

Los jugadores pueden apartar una mano o empujar al NPC con interacción contextual durante una apertura, pero no cuentan con un sistema de puñetazos para dejarlo inconsciente. La resolución principal sigue siendo extraer, distraer y coordinar.

## 08. De uno a cuatro: escalado y dificultad

La cantidad de jugadores y el nivel elegido son dos ejes separados. El juego aumenta la resistencia total con más personas, pero conserva legibilidad, límites de fuerza y oportunidades de avance. El archivo data/game_balance.json es la fuente numérica para el prototipo; estas tablas explican la intención.

| Jugadores efectivos | Índice de resistencia R | Intervalo base entre ataques | Ayuda y tareas |
| --- | --- | --- | --- |
| 1 | 1,0 | 7 s | Carrito disponible, fatiga del tío mayor por acción, anclaje simple. |
| 2 | 1,55 | 6,2 s | Dos agarres coordinados y un anclaje a la vez. |
| 3 | 2,0 | 5,6 s | Más oportunidades de anclaje y presión de ruta. |
| 4 | 2,4 | 5 s | Resistencia alta, transporte y apoyo simultáneo recomendados. |

R es un índice de juego que multiplica el límite de resistencia activa y el presupuesto de fatiga. No multiplica la masa física ni cada fuerza del sistema. La masa del NPC permanece estable para que no cambie el comportamiento de las colisiones al entrar otro jugador. La frecuencia de ataques se ajusta por su propia tabla; no se vuelve a escalar con R.

| Nivel | Resistencia extra | Factor de intervalo | Tiempo base | Señal de ataque |
| --- | --- | --- | --- | --- |
| Relajado | 0,75 | 1,25 | Sin reloj | 1,25 veces la duración base |
| Normal | 1,0 | 1,0 | 600 s | Duración base |
| Difícil | 1,2 | 0,9 | 480 s | 0,9 veces, mínimo 0,8 s |

El impulso de ataque depende del nivel con factores 0,8, 1 y 1,1, pero no del número de jugadores. La pérdida de compostura usa esos mismos factores. El reloj recibe 45 s extra en individual, 20 s con dos y ninguno con tres o cuatro, cuando existe reloj. El intervalo es un mínimo entre comienzos de ataques y además exige haber terminado la recuperación; no habilita solapamientos.

**Ejemplo:** cuatro jugadores en Normal usan R=2,4, un mínimo de 5 s entre ataques y 600 s de reloj. Un jugador en Normal usa R=1, carrito, mínimo de 7 s y 645 s de reloj. No se exige fuerza equivalente a cuatro jugadores para ganar solo. En Difícil con cuatro, R efectivo es 2,88 y el intervalo mínimo 4,5 s.

No se requiere que todos mantengan una extremidad para abrir una puerta o completar una barra. La cooperación debe surgir de ventajas funcionales. La prueba decisiva del escalado es que cuatro personas descoordinadas tengan problemas y cuatro coordinadas avancen claramente mejor; individual debe seguir siendo ganable con práctica y herramientas.

Si alguien se desconecta, se limpian sus agarres y, tras 5 s, se recalcula R y el intervalo con los conectados. No se acorta el reloj ni se elimina tiempo acumulado. Si se reduce el grupo a uno y faltan herramientas, aparece un carrito en un punto seguro. Las entradas nuevas esperan al lobby en la versión base; no hay incorporación en mitad del intento.

## 09. Fatiga, distracciones y posibilidades de estrategia

El tío dispone de una reserva de fatiga usada como presupuesto de resistencia. En implementación se llama resistanceBudget para evitar confundir «más fatiga» con «más energía». Su valor máximo base es 100 por R efectivo. Un manotazo cuesta 20, empujón 25, patada 30 y sacudida 35. Resistir con un anclaje cuesta 8 puntos por segundo. Esos costes aumentan proporcionalmente con R efectivo, por lo que el número aproximado de acciones antes de la pausa no se dispara con el grupo.

Llegar al 15% del presupuesto inicia Recover cuando termina la acción actual. La apertura forzada dura 3,5 s en Normal, 4,5 s en Relajado y 3 s en Difícil. Durante esa pausa se restaura hasta el 70% del máximo y se limita la resistencia activa al 20% de su techo. Las pausas breves propias de un ataque no restauran todo el presupuesto; así se distingue recuperación de animación de descanso por agotamiento.

La distracción es un intercambio entre preparación y movimiento. Un vaso señuelo tiene un punto de interés, no un efecto de «dormir» al NPC. Si el bot lo ve y no está atacando, puede atenderlo durante 2,5 s. Tiene 12 s de enfriamiento por tipo y rendimiento reducido si se repite tres veces. Una distracción garantiza una oportunidad acotada, nunca una secuencia infinita sin resistencia.

Otros señuelos posteriores son una canción en la rockola, una campera que cree suya y una caja de snacks. Cada objeto declara qué personalidad lo prefiere. El jugador no administra sustancias reales ni prepara dosis. Las variantes son reglas ficticias de comedia, no simulaciones clínicas.

Tres estrategias deben ser viables en el primer bar: esperar el golpe y tirar durante recuperación; usar el carrito mientras otro libera anclajes; distraer y tomar la ruta más despejada. No se pretende que cualquier objeto resuelva todo. Una silla puede obstruir, servir de apoyo o ser movida, pero no se convierte automáticamente en herramienta de inmovilización.

Las acciones repetidas se balancean con pequeñas restricciones visibles: el tío protesta ante el mismo truco, el objeto necesita reposicionarse o la ruta exige cambiar de orientación. No se penaliza al grupo con trampas ocultas por descubrir una técnica eficiente. Si una táctica reduce el juego a mantener un botón durante todo el recorrido, se ajusta el sistema y se vuelve a probar.

## 10. Escenario inicial: The Last Round

Nombre provisional del bar: The Last Round. Planta interior de aproximadamente 18 por 12 m, más una vereda de 14 por 5 m. La escala es una hipótesis de blockout. El recorrido principal útil ronda 22 a 30 m; la ruta exacta debe medirse en Unity. La camioneta está estacionada y no necesita conductor.

Las áreas son barra, mesas, esquina de karaoke, pasillo de salida, umbral, vereda y zona de carga. El arte distingue cada zona con silueta, iluminación y un hito. La ruta nunca depende de leer un cartel en inglés. Desde el punto inicial se ve la salida y, al acercarse, la camioneta.

| Zona | Problema principal | Oportunidad de solución |
| --- | --- | --- |
| Barra | Mano del tío aferrada a una superficie fija | Liberar mano o presentar señuelo. |
| Mesas | Cuerpo ancho y mobiliario móvil | Reordenar sillas y girar el transporte. |
| Karaoke | Tentación que puede provocar desvío | Desactivar música o aprovechar distracción. |
| Umbral | Orientación de hombros y piernas | Un jugador estabiliza y otro abre. |
| Vereda | Espacio para una fuga corta | Interceptar con carrito y volver a agarrar. |
| Camioneta | Alineación y puerta libre | Apoyar, liberar agarres y cerrar. |

Las puertas del recorrido principal tienen un ancho inicial de 1,5 m y altura de 2,2 m. Las zonas de giro reservan al menos un círculo de 2,4 m. Los números son restricciones de prototipo, no normas arquitectónicas. Se ajustan al tamaño definitivo del cuerpo. Los marcos usan colisionadores simples con biseles invisibles razonables para evitar enganches técnicos.

La variación procedural solo selecciona entre puntos autorizados: asiento inicial, dos distribuciones de sillas, un señuelo y una preferencia del bot. No se genera un bar entero al azar. Cada semilla garantiza camino navegable, carrito accesible y zona de carga sin obstrucción permanente.

El plano incluido en la biblia visual es un esquema de diseño, no un plano de implementación a escala final. Las pruebas de cámara, colisiones y cuatro cuerpos determinan las medidas definitivas. La utilería decorativa se deja fuera de las áreas de circulación para que el juego siga siendo legible.

## 11. Variantes de intoxicación y personalidad

La primera muestra implementa únicamente Alcohol / Fiestero. Esa restricción mantiene el foco en el núcleo. Las variantes de marihuana y mezcla forman parte de la visión, con prioridad posterior a una muestra online estable. Todas representan caricaturas ficticias; sus animaciones y reglas no pretenden describir efectos universales de sustancias reales.

**Alcohol / Fiestero:** intenta volver a la barra, exige otra ronda, inicia baile y hace demostraciones de equilibrio. Perfil activo: protestas, manotazos, empujones y breves fugas. El objetivo visual es una confianza desmedida que contradice su coordinación. El jugador aprende a anticipar la mano que busca un anclaje.

**Marihuana / Filósofo:** se interesa por snacks, se sienta en lugares inconvenientes y trata ciertos objetos como compañeros de conversación. Su desafío se diseña alrededor de pausas obstinadas, cambios de destino y anclajes prolongados. Puede defender su espacio con empujones cómicos si lo levantan. El ritmo distinto debe ofrecer una partida nueva, no solo una voz diferente o filtros verdes.

**Mezcla / Noche imposible:** alterna entre dos perfiles mediante señales y ventanas mínimas de duración. Se desbloquea como variante avanzada cuando ambas bases estén probadas. Nunca combina todos los ataques a la vez ni omite recuperaciones por «estar más pasado». La complejidad tiene un presupuesto igual que el resto del bot.

Las personalidades son modificadores pequeños e independientes: sentimental, competitivo y desconfiado. Sentimental busca despedirse de un objeto o abrazar a un cliente fijo; competitivo prepara demostraciones que abren ventanas de fatiga; desconfiado pide recuperar una prenda antes de colaborar. Una personalidad puede cambiar pesos de intención y diálogos, sin crear un sistema nuevo completo.

Cada perfil debe declarar acciones permitidas, pesos de selección, señuelos preferidos, duración mínima de estados, frases y animaciones necesarias. No se habilita un perfil sin sus señales, contraacciones y pruebas para los cuatro tamaños de grupo. La selección del lobby informa qué variantes están realmente disponibles.

Los consumos del jugador quedan fuera del diseño inicial. No hay minijuego para fabricar drogas, instrucciones reales ni una economía de compra de sustancias. El foco sigue siendo la extracción y la comedia de cuidar a un amigo difícil.

## 12. Humor, diálogos y escenas compartibles

La escritura usa una voz estable. El tío se atribuye importancia que no tiene, interpreta ayuda como servicio premium y culpa a la arquitectura de sus caídas. Puede putear e insultar a los jugadores, pero necesita variedad de ritmo: algunas líneas breves, algunas excusas y silencios donde el accidente visual sea el remate.

| Situación | Ejemplo de línea original propuesta |
| --- | --- |
| Lo cargan entre varios | «Finally. Fucking Uber.» |
| Se aferra a una columna | «This is my emotional support pillar.» |
| Pierde la sandalia | «Leave me. Save the fucking sandal.» |
| Se aproxima a la puerta | «I fit here in 1998.» |
| Se cae solo | «Who installed the floor like that?» |
| Lo dejan en un carrito | «Business class. About damn time.» |
| Prepara empujón | «Back up. I am about to be reasonable.» |
| Consiguen sacarlo | «Great night. Same time tomorrow?» |

La tabla de diálogos del paquete incluye eventos, prioridad, enfriamiento, texto inglés, subtítulo español y versión con insultos suavizados. Las señales de ataque no pueden quedar tapadas por un monólogo. La prioridad sugerida es ataque, información de extracción, reacción física, protesta y ambiente.

Reglas de reproducción: una sola frase del tío a la vez; separación mínima de 4 s entre líneas ordinarias; enfriamiento por línea de al menos 25 s; evitar las últimas cinco líneas. La caída de una silla no dispara un comentario cada vez que rebota. Los eventos se agrupan y tienen umbrales.

Las escenas compartibles se diseñan como combinaciones, no como cinemáticas obligatorias. Ejemplos: una mano anclada a la barra mientras tres jugadores retroceden; la puerta abierta del lado equivocado; un compañero que suelta para esquivar y deja al otro sosteniendo todo; la sandalia que cae justo cuando el tío parece colaborar. Los jugadores conservan control y pueden evitar el desastre.

El menú puede tener detalles absurdos, como subtítulo «Premium Bad Decisions», pero las opciones conservan nombres comprensibles. Audio, salir y accesibilidad no se esconden detrás de chistes. Un modo de lenguaje suavizado sustituye líneas y subtítulos; no cambia la dificultad. No se necesita reconocimiento de voz para que el bot responda.

Se recomienda revisión de inglés por un hablante competente antes de grabar voces. Las líneas aquí incluidas son propuestas de guion, no pruebas de que una audiencia vaya a reír. El playtest registra qué situaciones se recuerdan, si las frases ayudan y si se vuelven molestas.

## 13. Interfaz, accesibilidad y tutorial

El HUD prioritario muestra objetivo corto, reloj cuando exista, energía y estado de las dos manos. La compostura aparece al recibir un impacto o al estar baja. La intención del bot se comunica principalmente en el mundo: postura, sonido y contorno temporal del miembro atacante. La pantalla no necesita una lista de ocho barras.

Una mano libre tiene icono neutro; una mano ocupada muestra el tipo de agarre; la tensión se representa con una forma y una etiqueta breve. Los colores tienen equivalentes gráficos. Las señales importantes pueden aparecer también en subtítulos descriptivos: «[prepara patada]», separados de la traducción del diálogo.

El tutorial individual se integra en la misma sala: mover una silla, agarrar un saco con forma de maniquí y soltar ante una señal. Luego el tío ejecuta una protesta de baja intensidad. Se puede omitir y consultar desde pausa. En grupos, los marcadores permiten señalar mano, ruta y carrito sin voz.

Opciones mínimas: reasignación de teclas, sensibilidad independiente, alternar agarre, tamaño de texto, subtítulos y nombres de hablante, subtítulos de señales, mezcla de música/voces/efectos, reducción de sacudidas, desactivar balanceo, vibración y destellos intensos. La lectura de ataques debe funcionar con música y voces silenciadas.

En individual, pausa detiene la simulación local. En online, abrir opciones solo bloquea el control propio y avisa de que la partida continúa. El menú no permite seguir ejerciendo esfuerzo con inputs que quedaron sostenidos: se cancelan acciones al perder foco o abrir un panel incompatible.

Flujo de menús: inicio; jugar solo o con amigos; selección de modo; lobby con preparación; carga; partida; resultados; repetir o volver. Un error de conexión ofrece reintentar o regresar con una explicación concreta. No se promete que todo error sea «revisá tu internet».

La primera versión se prepara para interfaz en español e inglés. Las voces se priorizan en inglés. Todos los textos viven en claves de localización, no dentro de scripts. Se prueba expansión de texto del 30% para evitar botones rotos. El humor puede adaptarse; las instrucciones de juego mantienen precisión.

## 14. Rejugabilidad, progresión y contenido futuro

La rejugabilidad inicial combina mejor ejecución, variación de objetos y preferencias del bot. No se necesita grind para que el núcleo funcione. Los jugadores pueden comparar tiempos de la misma semilla y dificultad dentro del grupo. Una clasificación pública queda fuera hasta resolver integridad de datos y valor real para la comunidad.

La progresión propuesta es cosmética: gorros, colores de ropa, estampas ficticias y accesorios del tío que no alteran hitboxes ni estadísticas. Se desbloquean por hitos de extracción y situaciones divertidas, como recuperar una sandalia o completar sin carrito. No se vende poder ni se bloquea la herramienta individual detrás de progresión.

El lanzamiento compacto propuesto contiene un bar pulido, un NPC con personalidad base, cuatro ataques, tres niveles, modo de 1 a 4, ajustes, inglés/español y variantes de distribución validadas. La cantidad final se confirma tras la muestra; no se anuncia en tienda un número de mapas todavía inexistente. Un segundo escenario solo se prioriza si añade decisiones espaciales sustanciales.

Ideas posteriores: boliche con pista estrecha, boda con sillas y mantel, camping con terreno irregular y karaoke privado. Cada escenario necesita una nueva situación física y reutiliza los sistemas centrales. La conducción, campañas narrativas y generación procedural completa no se agregan como requisito para que el juego parezca grande.

La variante de marihuana se desarrolla después de la muestra, con pruebas propias. La mezcla queda detrás de ella. La voz de proximidad puede enriquecer la coordinación y las caídas, pero no debe bloquear el primer prototipo ni impedir jugar sin micrófono.

Los resultados incluyen reconocimientos pequeños: «Dedicated Sandal Recovery», «Human Doorstop» o «Most Unnecessary Confidence». Estos reconocimientos se calculan con eventos reales del intento; no se asignan estadísticas falsas para rellenar pantalla. La grabación automática de clips queda como investigación posterior, porque requiere almacenamiento, codificación y controles adicionales.

## 15. Validación del concepto y criterios de diversión

No hay base para prometer viralidad. La hipótesis es que una misión cotidiana, un bot expresivo y consecuencias físicas visibles favorecen momentos que los grupos quieran compartir. Esa hipótesis debe confrontarse con pruebas tempranas y no con el volumen de documentación.

El primer playtest útil usa una sala gris, un cuerpo legible y dos jugadores. Se observa si logran agarrar, identificar una señal de golpe, perder una posición y avanzar durante la recuperación. El observador evita explicar estrategias después de la introducción. Si necesitan instrucciones constantes, se corrigen señales y affordances antes de sumar contenido.

Objetivos exploratorios para una primera ronda: la mayoría entiende el objetivo en menos de un minuto; al menos cuatro de seis grupos pueden describir por qué recibieron su último golpe; al menos cuatro desean repetir una vez sin que se les pida. Estas cifras son umbrales de decisión internos con muestra pequeña, no evidencia estadística de demanda de mercado.

Se comparan individual, dos y cuatro. Registrar tiempo moviendo efectivamente al NPC, tiempo atascado, cantidad de agarres rotos, impactos por minuto y oportunidades de recuperación aprovechadas. Si un jugador pasa más tiempo en el piso que tomando decisiones, se baja control perdido antes de aumentar variedad.

Las métricas técnicas y las de diversión se separan. Una partida sin errores puede ser aburrida. Una partida graciosa con juntas explosivas puede no ser vendible. El corte hacia arte final exige ambas: estabilidad y un núcleo que el grupo quiera repetir.

El formulario incluido pide recordar el mejor momento, el peor, una decisión útil y una situación injusta. También pregunta qué creen que deberían hacer distinto la próxima vez. La respuesta deseada es una estrategia concreta, no «tener suerte». Se solicita precio percibido con contexto de contenido real, sin convertir una respuesta aislada en validación comercial.

## 16. Alcance priorizado y cierre de diseño

| Prioridad | Entregable | Condición de aceptación |
| --- | --- | --- |
| P0 | Movimiento, dos manos y agarres | Funciona individual y en host/cliente sin teletransporte. |
| P0 | Bot resistente con ataques | Señales, contacto único, fatiga y recuperación comprobados. |
| P0 | Bar, salida y camioneta | Se puede ganar, perder y reiniciar sin intervención del editor. |
| P0 | Escalado 1 a 4 y tres niveles | Todas las combinaciones tienen pruebas registradas. |
| P0 | Online privado funcional | Conexión por internet real probada antes de llamarlo online. |
| P0 | Opciones esenciales y resultados | Se conservan ajustes, se puede volver al lobby. |
| P1 | Arte, voces y humor ampliado | Reemplazan placeholders sin degradar señales ni físicas. |
| P1 | Perfil Marihuana / Filósofo | Comportamientos y contraacciones propios verificados. |
| P1 | Integración Steam y mando completo | Se prueba con cuentas, builds y dispositivos reales. |
| P2 | Mezcla, proximidad y más escenarios | Solo después de validar valor, coste y estabilidad. |

Para Steam, P0 es el núcleo jugable y P1 incluye trabajo de lanzamiento necesario; las etiquetas no significan que un prototipo P0 esté listo para vender. Los documentos técnicos y de producción definen los hitos y sus evidencias.

El diseño considera innegociables la extracción, el bot que pelea por quedarse, el control corporal y la posibilidad de jugar de uno a cuatro. Claude puede decidir nombres internos, patrones pequeños y detalles de implementación. Un cambio de motor, topología, cámara o alcance requiere una justificación escrita y la actualización del registro de decisiones antes de rehacer el proyecto.

La siguiente acción de desarrollo es construir el laboratorio de agarre y un empujón anunciado, comprobarlos con un segundo cliente y medir si el resultado es manejable. Solo después corresponde fabricar el bar completo. La documentación orienta ese trabajo; la prueba jugable decide si la promesa se cumple.
