# Materiales e interacción - especificación v0.1

Complemento de preproducción del 15 de noviembre de 2026. El GDD y game_balance.json originales continúan siendo autoridad para reglas y números. Este documento define superficies y cómo comunicar las posibilidades de los objetos.

## 1. Dirección visual

El bar debe sentirse barato, gastado y reconocible. Madera con vetas amplias, piso de baldosas crema y verde apagado, pared taupe manchada y camisa bordó mate. La suciedad agrega carácter sin tapar manos, cara, señales de golpe o salida. Se mantiene geometría simple y materiales coherentes con el kit 3D.

La textura no sustituye la silueta. El bigote, las manos y la camisa ayudan a identificar al NPC. Los bordes del entorno evitan competir con el contorno de interacción. En vista de juego, el movimiento importante debe leerse incluso con música apagada y efectos reducidos.

No se texturiza todo por obligación: piel, ojos, sandalias, vinilo y pintura pueden conservar colores planos con materiales mates. Aplicar tablones a patas delgadas produce cortes absurdos; por eso la madera con juntas se reserva a barra y tableros.

## 2. Texturas entregadas y parámetros

| Material | Archivo | Repetición física codificada en UV | Muestreo | Smoothness propuesto |
| --- | --- | --- | --- | --- |
| Madera | 01_wood_basecolor.png | 1 m | Mirror | 0,22 |
| Baldosas | 02_floor_basecolor.png | 2 m; cuatro baldosas por lado | Repeat | 0,18 |
| Pared | 03_wall_basecolor.png | 2 m | Mirror | 0,05 |
| Tela | 04_fabric_basecolor.png | 0,5 m | Mirror | 0,10 |

Los cuatro archivos son RGB y miden 1254 x 1254 px. Se entregan sin modificación de píxeles posterior a la generación. Los prompts solicitaron una resolución y repetición ideales; los datos de importación registran la salida real. No hay garantía automática de bordes idénticos ni de ausencia de repetición visible.

Los materiales Blender usan la textura como color, metallic cero y rugosidad escalar. Para la receta equivalente de Unity se propone URP/Lit, Opaque y Workflow Metallic, con multiplicador de color blanco. En este caso smoothness se deriva de 1 menos la rugosidad usada, como decisión de lookdev; no es una conversión medida de un material físico real.

No existe un mapa roughness oculto en estos PNG ni datos de smoothness en un alpha que no tienen. No usar RGB de color como normal. Un set PBR más completo puede producirse cuando la dirección y el rendimiento justifiquen el detalle; este complemento no afirma entregarlo.

## 3. Importar a Unity sin romper la escala visual

Usar las variantes FBX si se desea reproducir este lookdev. Las coordenadas preparadas quedan en UV0 y expresan metros por repetición. Por eso el material usa Tiling 1,1. Multiplicar de nuevo por nueve para el piso haría que el patrón aparezca nueve veces más pequeño de lo previsto.

Base Color se importa como textura de color sRGB. Los datos lineales de otras clases de mapas necesitan su configuración específica, si se agregan en el futuro. La documentación oficial distingue el muestreo de color de los datos no coloreados [M2, M3].

Activar mipmaps y revisar filtrado para evitar vibración de la trama de tela a distancia. El catálogo propone Trilinear, límite 2048 y escalado NPOT ToNearest para inspección; comprobar el resultado real. No declarar que se entregaron mapas 2K por elegir un límite máximo de 2048 en el importador. CPU Read/Write se deja desactivado salvo una necesidad concreta.

La documentación de Unity presenta Mirror como repetición reflejada en cada frontera entera y Repeat como repetición periódica [M4]. Se usa Repeat en las baldosas para mantener alternancia del damero. La selección de Mirror en otros materiales es una solución de muestreo y puede producir simetrías reconocibles; observarla a escala de juego, no solo en una muestra aislada.

Las referencias de imagen en FBX son relativas. Mantener models y textures como carpetas hermanas o remapear explícitamente. Un material importado de FBX puede requerir adaptación a URP; el kit incluye la receta, no un .mat de Unity inventado. Mantener accesibles huesos, sockets y acciones del personaje.

## 4. Preparación y límites de las UV

Las UV se generan por proyección del plano principal de cada cara en espacio local del asset. Las zonas de color plano conservan coordenadas previas cuando existen. Se elimina la ambigüedad de canal al colocar MaterialUV como único canal de la variante exportada. Esto no toca pesos de rig ni añade física.

La camisa usa superficies simples y todavía necesita una revisión de costuras y deformación durante movimiento. Las aristas de un unwrap por caras pueden cambiar orientación de la trama. La prioridad del prototipo es comprobar que la dirección del color y la lectura funcionan, antes de una topología y pintura final más costosas.

La escena Blender original no se modifica. Material_Lookdev.blend es una copia con rutas relativas a estas texturas. Los ocho FBX tienen sufijo material_variant. Algunos mantienen pintura o vinilo plano; el sufijo indica la versión de lookdev, no que todos contengan un mapa de imagen.

El GLB de escena es una revisión visual con imágenes incorporadas. No reemplaza el flujo FBX del proyecto Unity ni agrega un importador GLB. No trae colliders, articulaciones de física ni comportamiento del NPC.

## 5. Lenguaje de interacción del entorno

Un objeto que el jugador puede agarrar recibe contorno fino e icono de mano al enfocarlo, solo cuando la acción es válida. Una mesa fija no muestra una mano que prometa movimiento. La barra puede ser anclaje del NPC y seguir siendo inmóvil para el jugador. La camioneta comunica carga y cierre con icono de puerta y progreso de mantener la acción.

La apariencia de un shader no decide fricción ni masa. Un piso con suciedad pintada no se vuelve resbaladizo por sí solo. La roughness de un material visual no es el coeficiente de fricción de un collider. Esa separación evita cambiar jugabilidad al mejorar el arte.

Los estados visuales corresponden a estado real: agarre concedido, anclaje activo, puerta obstruida o extracción confirmada. No dibujar todos los objetos con brillo permanente. Las reglas detalladas están en visual_interaction_rules.csv y se complementan con el kit UI.

## 6. Fichas de objetos y alcance

Hay ocho objetos con modelo existente y tres fichas de contenido pendiente. La masa del NPC permanece en 110 kg según el diseño. Las masas nuevas de taburete 6 kg, carrito 18 kg y puerta 12 kg son hipótesis sin validación física. No son valores certificados ni instrucciones para reemplazar una simulación ya afinada.

Taburete: obstáculo móvil con puntos de jugador y anclaje del NPC que deben añadirse y comprobarse; el modelo actual no los trae. Mesa: fija en la muestra inicial. Barra: anclajes ya marcados, con un solo anclaje activo inicialmente. Carrito: un cuerpo estabilizado puede resolver el primer ensayo, dejando ruedas visuales.

Puerta: origen en bisagra y apertura coherente; límites y masa se prueban. Camioneta: fija, con volumen y bisagras marcados; conectar el trigger y cierre reales. Mundo: resolver colisión y elevar/completar la pared rebajada de inspección antes de usarlo como escenario terminado.

Vaso señuelo, karaoke y snacks continúan pendientes como modelos y sistemas. Incluir su ficha no los hace disponibles en un lobby ni justifica anunciarlos. Marihuana / Filósofo y Mezcla siguen fuera del primer núcleo. El comportamiento del señuelo conserva tiempos y cooldown del GDD cuando se implemente.

## 7. Comprobación visual y técnica

Revisar bajo luz neutra y bajo la luz del bar. Comprobar cara y mano del bot frente a pared, claridad de la preparación de empujón y lectura del carrito sobre el damero. Si el piso compite con la acción, reducir contraste o repetición mediante un ajuste revisado del material, no cambiar la cámara para esconder el problema.

Observar repeticiones en superficies grandes y a distancia. El tablero muestra dos repeticiones por eje con el wrap elegido. Es una evidencia visual de esa receta, no una prueba matemática de periodicidad de los mapas. Los valores de diferencia de bordes del JSON son diagnósticos y no se utilizan como certificado de calidad.

Reimportar las variantes FBX y revisar geometría, UV0, rutas, rig, sockets y acciones. Después repetir en Unity las pruebas pertinentes de agarre, golpe y caída. Veinte casos adicionales de materiales e interacción empiezan NOT_RUN; los reportes de arte no equivalen a tests del juego.

## 8. Fuentes primarias

- [M1] Unity 6.3, URP/Lit: https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/lit-shader
- [M2] Unity 6.3, tipos de textura: https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/textures/textures-getting-started/texture-types
- [M3] Unity 6.3, sRGBTexture: https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/textureimportersettings/srgbtexture
- [M4] Unity, TextureWrapMode: https://docs.unity3d.com/cn/6000.0/ScriptReference/TextureWrapMode.html
- [M5] Unity, escalado NPOT: https://docs.unity3d.com/es/current/ScriptReference/TextureImporterNPOTScale.html

Consulta: 15 de noviembre de 2026. Verificar APIs y nombres contra la versión de Unity/URP efectivamente instalada. Los parámetros del catálogo son propuestas del proyecto.
