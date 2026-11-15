# Catálogos de implementación

- game_balance.json: configuración propuesta, unidades y fórmulas. No validada jugando.
- npc_states.csv: 11 estados principales. El diagrama gráfico es una vista parcial para lectura.
- dialogue_en_es.csv: 60 líneas originales propuestas, inglés/español y alternativas de lenguaje. Audio NOT_RECORDED.
- asset_manifest.csv: 79 elementos; únicamente los cinco conceptos/esquemas indicados están entregados. El resto está pendiente.
- backlog.csv: 48 tareas con dependencias por ID. S/M/L son tamaño relativo, no estimación de días.
- qa_matrix.csv: 72 pruebas sin ejecutar. Completar build, fecha y evidencia al realizarlas.

CSV en UTF-8 con BOM para facilitar apertura en herramientas de escritorio. Usar un parser CSV real; no separar por comas manualmente. Importar a assets del motor o convertir a estructuras verificadas. Ningún CSV hace que el juego funcione sin implementación.

Convención de unidades: metros, segundos, kilogramos, newtons y radianes/segundo donde se especifica. Un factor adimensional no reemplaza una masa. La semilla configura selección de contenido; no promete determinismo de PhysX.
