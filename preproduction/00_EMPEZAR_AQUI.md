# UNCLE EXTRACTION - empezar por acá

Entrega consolidada de preproducción · 15 de noviembre de 2026 · Preparada para Stefano Francolino.

Este ZIP reúne diseño, recursos 3D de prototipo e interfaz. No contiene un proyecto Unity ni un juego ejecutable. El objetivo es darle a Claude decisiones claras y archivos reales para construir y comprobar el núcleo jugable.

## Para Claude

Actuá como desarrollador principal del proyecto. Conservá la fantasía central: de uno a cuatro amigos extraen de un bar a un NPC borracho que se resiste, pega, patea, empuja, escapa y se aferra al entorno. Individual es viable; grupos requieren coordinación. Dificultad elegida y cantidad de jugadores son ejes separados. La masa no escala. El tío tiene señales, fatiga y recuperaciones; no cadenas infinitas de golpes. La camioneta está estacionada.

Primero inspeccioná las herramientas y el trabajo existente. Si ya hay proyecto o PROGRESS.md, continuá desde ese estado. No reinicies por recibir este paquete.

## Carpetas

| Carpeta | Contenido | Estado real |
| --- | --- | --- |
| uncle_extraction/ | GDD, arquitectura, arte conceptual, plan, datos y prompts | Diseño de preproducción; 57 páginas PDF y fuentes Markdown. |
| uncle_extraction_kit_3D/ | Ocho assets en FBX/GLB, escena Blender, rig y acciones | Exportados y reimportados en Blender; Unity y físicas pendientes. |
| uncle_extraction_kit_UI/ | Doce pantallas, componentes, textos, estados y bindings | Diseño y recursos; interfaz interactiva pendiente. |

Los archivos originales se preservan dentro de sus carpetas. Para programar, leé Markdown y datos: los PDF son versiones cómodas para revisión, no hace falta leer ambas versiones duplicadas de todo.

## Orden de lectura

1. uncle_extraction/DECISIONES.md.
2. uncle_extraction/docs/01_GDD_diseno_del_juego.md.
3. uncle_extraction/docs/02_especificacion_tecnica.md y data/game_balance.json de esa misma carpeta.
4. uncle_extraction/docs/03_biblia_visual_y_sonora.md y docs/04_plan_de_produccion_y_handoff.md.
5. uncle_extraction/claude/00_PROMPT_MAESTRO.md.
6. uncle_extraction_kit_3D/README.md y claude/INTEGRAR_KIT.md.
7. uncle_extraction_kit_UI/README.md, UI_SPEC.md y claude/INTEGRAR_UI.md.

Si aparece una contradicción, respetar requisitos del usuario y decisiones fijadas. Balance original manda sobre cifras de mockups; GDD manda sobre interpretación de un asset; UI_SPEC define presentación, no nuevos poderes del bot. Registrar ajustes materiales sin reescribir todo por preferencia.

## Primer trabajo concreto

Base recomendada: Unity 6.3 LTS, C#, URP y red autoritativa de host. Comprobar parche y paquetes en M0. Crear un laboratorio con cápsula de jugador, cámara estable, agarres y un empujón anunciado. Usar el FBX del tío y el carrito como placeholders; no modelar todo desde cero. Configurar física y límites reales: el FBX no trae ragdoll funcional.

Añadir un HUD pequeño conectado a manos y ataque reales. Probar individual y después dos builds con host y cliente temprano. Completar recorrido y menús cuando el núcleo sea estable. Las pantallas del kit UI orientan implementación, no se importan como una única textura que simule una interfaz.

Mantener M0 a M8 como controles de calidad. Avanzar con tareas reversibles autorizadas, resolver detalles internos razonables y registrar decisiones. Si no hay Unity, preparar lo viable y devolver pasos exactos pendientes. No inventar compilación, pruebas, cuentas, licencias, pagos ni publicación.

## Evidencia y límites

- El kit 3D pasó 16 reimportaciones en Blender, con huesos, sockets y acciones comprobados.
- El kit UI pasó verificaciones de recursos y catálogos; el atlas tiene 14 páginas.
- Hay 72 casos de QA del juego y 36 específicos de UI; sus estados iniciales son NOT_RUN.
- No se ejecutó Unity ni una partida de este juego. No hay prueba de red, Steam o diversión real todavía.
- Los conceptos y modelos son preproducción. Arte final, rig afinado, audio, implementación y playtests siguen siendo trabajo del proyecto.
- Marihuana / Filósofo y Mezcla permanecen ampliaciones posteriores, no contenido que se deba anunciar como disponible.

Actualizar PROGRESS.md con archivos, versiones, pruebas realmente ejecutadas, limitaciones y siguiente objetivo. Las credenciales, compras y publicación corresponden al dueño de las cuentas. La entrega siguiente debe ser un resultado concreto ejecutable o una preparación verificable con pendientes honestos, no otro plan genérico.
