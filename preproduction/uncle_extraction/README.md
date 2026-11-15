# UNCLE EXTRACTION - paquete de preproducción

Versión 1.0 · 15 de noviembre de 2026 · Preparado para Stefano Francolino.

Juego propuesto: de 1 a 4 amigos sacan de un bar a un NPC borracho que se resiste, pega, empuja, escapa y se aferra al entorno. Windows / Steam. Recomendación: Unity 6.3 LTS y C#, con versiones de paquetes por verificar.

## Empezar

1. Leer los PDF en orden 01 a 04 para entender diseño, tecnología, arte y producción.
2. Extraer esta carpeta en el entorno de Claude con acceso a archivos.
3. Pedirle que lea README.md, DECISIONES.md y claude/00_PROMPT_MAESTRO.md.
4. El prompt comienza M0 y M1: proyecto reproducible y laboratorio de agarre/empujón. No intenta fabricar todo el juego en una respuesta.
5. Continuar con los prompts de fases y PROGRESS.md. La red se prueba temprano.

## Contenido

- pdf/: cuatro documentos extensos, con índice y páginas numeradas.
- docs/: las mismas fuentes en Markdown, editables y buscables.
- art/: dos conceptos PNG, plano, HUD y diagrama de estados; prompts y alcance de cada imagen.
- data/: JSON de balance y CSV de bot, diálogos, assets pendientes, tareas y QA.
- claude/: prompt maestro, prompts por fase y plantilla de progreso.
- FUENTES.md: referencias primarias, antecedentes y fecha de consulta.
- DECISIONES.md: qué se mantiene y qué se deja como ampliación.
- PLAYTEST.md: formulario y registro de observación.
- tools/validate_spec.py: verificación estructural de catálogos sin dependencias externas.
- VALIDACION_DOCUMENTAL.json: qué se verificó y qué requiere implementación.
- MANIFEST.json: archivos empacados y hashes SHA-256 (excluye al propio manifiesto).

## Estado honesto

Es documentación de preproducción, no un proyecto Unity ni un juego ejecutable. Los parámetros son hipótesis sin playtest. Los CSV de assets listan trabajo pendiente; no acreditan archivos 3D, animaciones o audio que no existen. Las imágenes generadas son referencias conceptuales. Los diagramas son esquemas exactos de intención. Las pruebas de juego comienzan NOT_RUN.

Claude necesita herramientas para integrar y ejecutar. Si no dispone de Unity, debe preparar lo que pueda y dejar pasos de verificación reproducibles. El dueño gestiona cuentas, pagos y publicación. No hay garantía de viralidad ni afirmación de novedad absoluta.

El nombre es provisional. La versión inicial se concentra en Alcohol / Fiestero. Marihuana / Filósofo y Mezcla están especificados como ampliaciones. Las mentiras que cambian la realidad no forman parte del diseño elegido.
