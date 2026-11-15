# Prompt maestro para iniciar la implementación

Actuá como desarrollador principal de un juego indie 3D para Windows/Steam llamado provisionalmente UNCLE EXTRACTION. Trabajá en la carpeta adjunta y construí el proyecto por hitos verificables. Quiero resultados ejecutables, documentación actualizada y evidencia honesta de lo que pudiste probar.

Primero leé README.md, DECISIONES.md, los cuatro archivos de docs/, FUENTES.md y data/game_balance.json. Después inspeccioná tus herramientas reales: acceso a archivos, Unity, shell, builds, Git, modelado y red. No asumas que disponés de editor o que una API existe por su nombre. Usá la documentación oficial de las versiones instaladas.

## Identidad que debés conservar

De uno a cuatro amigos intentan sacar de un bar a un NPC adulto borracho y acomodarlo en una camioneta estacionada. El bot se resiste, se aferra a cosas, negocia, se escapa, pega, patea y empuja; puede tirar jugadores al piso. La cooperación distribuye transporte, liberación de manos, distracción y ruta. Individual es viable con carrito y recuperación propia. La dificultad elegida y el tamaño del grupo son ejes distintos. Los ataques se anticipan, tienen un contacto por objetivo y dejan aperturas. Humor adulto en inglés y estética desprolija estilizada. No hay mentiras que alteran la realidad ni un quinto jugador que controle al bot.

## Base técnica recomendada

Unity 6.3 LTS con parche estable por fijar, C#, URP, Input System, NGO y Unity Transport. Autoridad de host sobre física, NPC, agarres, ataques y victoria. MPS Sessions/Relay para internet en M4. Steam se integra después mediante adaptador verificado. No confundir lobby, invitaciones y transporte. Registrar editor y dependencias exactas una vez comprobadas. No cambiar de motor o red sin un problema observado, comparación breve y registro en DECISIONES.md.

## Primer trabajo: M0 y M1

1. Resumí las decisiones fijas y cualquier contradicción material, sin pedirme resolver detalles menores que puedas decidir razonablemente.
2. Inspeccioná el entorno y creá o adaptá un proyecto reproducible. Si existe trabajo previo, conservá sus cambios y leé PROGRESS.md antes de tocarlo.
3. Construí PhysicsLab con movimiento y cámara estable, dos manos, sockets validados, fuerzas acotadas, liberación segura, un NPC físico simplificado, un empujón anunciado, compostura/caída/levantada, carrito y extracción de prueba.
4. Generá escenas y referencias con herramientas de editor reproducibles cuando resulte útil. No inventes prefabs que solo existen como nombres de archivos ni YAML Unity escrito sin importación verificada.
5. Ejecutá pruebas disponibles y documentá resultados reales. Compilar, probar manualmente y ejecutar dos builds son evidencias distintas.
6. Entregá instrucciones concretas para abrir y probar, lista de archivos relevantes, limitaciones y siguiente hito. Actualizá PROGRESS.md y DEPENDENCIES.md.

Avanzá autónomamente en acciones reversibles del alcance. Continuá con el siguiente hito cuando los criterios anteriores estén comprobados y el entorno lo permita; los hitos son controles de calidad, no una obligación de pedirme permiso cada vez. Si una cuenta o herramienta externa bloquea algo, seguí con tareas independientes y describí el paso humano exacto pendiente. No inventes claves, compras, licencias ni aprobaciones de plataforma.

## Reglas de calidad

- No fabricás resultados, screenshots de gameplay ni tests aprobados que no ejecutaste.
- Los números son punto de partida; cambiarlos exige motivo y actualización del balance.
- No escalás masa con N ni multiplicás frecuencia/impacto dos veces.
- Todos los agarres se limpian al soltar, caer, desconectar, reiniciar y ganar.
- El bot no puede encadenar golpes infinitos ni girar instantáneamente hacia quien esquivó.
- Priorizá el segundo cliente temprano. No produzcas todo el arte antes de demostrar física y red.
- Los conceptos PNG no son modelos con rig. Usá placeholders explícitos y registrá assets/licencias.
- No introduzcas llamadas a un modelo de lenguaje durante la partida, servicios innecesarios ni una campaña gigante.
- No declares el juego listo para Steam sin build, internet real, plataforma, QA y material de tienda comprobados.

Empezá ahora con inspección, decisiones y la primera implementación viable. Si no podés ejecutar Unity, prepará código/herramientas que puedas producir y marcá de forma inequívoca qué falta importar, compilar y probar. No me devuelvas solamente otro plan general.
