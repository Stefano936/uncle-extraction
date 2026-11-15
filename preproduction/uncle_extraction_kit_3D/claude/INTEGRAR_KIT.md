# Integrar el kit 3D en el proyecto UNCLE EXTRACTION

Continuá desde el proyecto y documentación existentes; no reinicies ni cambies el motor. Este kit agrega assets de prototipo a los pendientes del paquete original. Leé README.md del kit, data/asset_metadata.json y data/validation_report.json; después el GDD y la especificación técnica originales.

## Trabajo prioritario

1. Inspeccioná el proyecto y PROGRESS.md. Si aún no existe, completá M0 con Unity 6.3 LTS y versiones de paquetes verificadas.
2. Copiá los FBX necesarios a una carpeta de assets de prototipo conservando nombres. No uses los GLB como si fueran importables por Unity sin instalar nada.
3. Importá uncle_prototype.fbx como rig Generic. Revisá Idle_Prototype, Push_Prototype y Kick_Prototype. Desactivá root motion para esta prueba. Mantené accesibles los huesos y sockets; no optimices su jerarquía antes de crear las referencias necesarias.
4. Verificá en Unity altura cercana a 1,93 m, orientación, colores y sockets. Comprobá una referencia de un metro. Registrá ajustes reales de importación y no dupliques conversiones de escala.
5. Armá un prefab con malla y rig, y cuerpos/colisionadores simplificados según el GDD. El kit no trae un ragdoll funcional. Usá once cuerpos físicos como objetivo, con fuerzas y juntas limitadas. Si el primer ensayo necesita un cuerpo más simple, documentá la simplificación.
6. Integrá transport_cart.fbx y bar_counter.fbx en PhysicsLab. Los marcadores indican manillar, carga y anclajes. Creá los componentes de juego y colisiones que faltan; los nombres no ejecutan lógica automáticamente.
7. Implementá un empujón anunciado, un agarre y recuperación de caída. Las animaciones son poses aproximadas: alineá contacto y señales con el reloj autoritativo, no con supuestas pruebas hechas por el autor del kit.
8. Probá en editor y build si tenés herramientas. Actualizá PROGRESS.md con evidencia. Si no podés ejecutar Unity, entregá archivos e instrucciones exactas pendientes, sin declarar éxito de compilación o gameplay.

## Integración posterior del escenario

Usá source/Uncle_Extraction_Blockout.blend como referencia editable de distribución y bar_blockout_shell.fbx como estructura. Ubicá los props separados según asset_metadata.json o recreá la distribución con una herramienta de editor. No importes estructura y escena completa duplicando suelos y paredes.

La puerta tiene el origen en la bisagra. La abertura mide 1,5 m de diseño; verificar colisión útil real. La pared frontal baja facilita inspección, pero la escena jugable necesita resolver límites, altura y colisión final. La camioneta es fija y su zona Extraction_Volume es un marcador, no un trigger implementado.

## Reglas que permanecen

- Host autoritativo; misma limpieza de agarres, golpes y recuperación que en la documentación.
- La masa del tío no escala con jugadores. Resistencia y presión usan el balance original.
- El empujón y patada pueden derribar según umbral de diseño, con señales y pausa.
- No hay cámara, movimiento, bot, online o Steam dentro de un FBX.
- No convertir el kit de prototipo en arte final por omisión. Registrar su estado en el manifiesto original.
- El socket Grab_Belt_Alt es un segundo punto opcional de cintura; habilitarlo solo si el prefab y el balance lo contemplan.

## Entrega de esta integración

Lista de assets importados y ajustes; prefab y escena reproducibles; resultado de una prueba de agarre y empujón; problemas de rig/colisión observados; estado de pruebas; siguiente hito. No refactorices todo el juego para adaptar un placeholder.
