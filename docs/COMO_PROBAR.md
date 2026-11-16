# Cómo probar UNCLE EXTRACTION (prototipo)

## Abrir

1. Doble clic en `Builds\UncleExtraction\UncleExtraction.exe` (se genera en la PC de desarrollo; no está en Git).
2. Menú: **Jugar solo** → elegir dificultad → **Empezar: The Last Round**. El **Laboratorio** es la escena de diagnóstico. **Laboratorio: tío articulado (prueba)** es el mismo laboratorio con el tío de 11 cuerpos (ragdoll reducido, malla del kit); está en prueba y todavía no reemplaza al tío del escenario principal.
3. Cooperativo en la misma PC o LAN: en una ventana **Jugar con amigos → Crear partida**; en otra **Unirse** con la IP del anfitrión (`127.0.0.1` en la misma PC). Windows puede pedir permiso de firewall al anfitrión.

## Objetivo

Sacar al tío del bar, subirlo por la rampa a la camioneta (arrastrándolo o en el carrito), salir y **mantener E 1 s desde afuera** para cerrar las puertas. Se gana cuando las puertas quedan cerradas con el tío bien adentro y la abertura libre. Se pierde si se acaba el tiempo.

## Controles

| Acción | Tecla |
| --- | --- |
| Moverse / mirar | WASD / mouse |
| Mano izquierda / derecha (mantener) | clic izquierdo / derecho |
| Esfuerzo (más fuerza un rato) | Shift |
| Soltar todo | Q |
| Interactuar (mantener o tocar): soltarle la mano de la barra, cargar y asegurar en el carrito, cerrar la camioneta, ayudar a un compañero caído | E |
| Agacharse / paso evasivo | Ctrl / Espacio |
| Pausa, opciones, volver al menú | Esc |
| Panel de depuración (mostrar el cursor) | Tab |
| Reintentar | R |

## Consejos

- Las señales de ataque (color, pose y texto con cuenta regresiva) avisan antes de cada golpe: alejarse o esquivar.
- Con una sola mano no alcanza para arrastrarlo caído; usá las dos o el carrito.
- El vaso de cerveza (sobre una mesa) distrae al tío si lo llevás y lo apoyás a su vista: suelta la barra unos segundos.
- Tomarlo de los tobillos ayuda a entrarlo «con los pies primero»; si las piernas quedan afuera, no se puede cerrar.

## Qué anotar

Qué se sintió bien o mal, dónde te trabaste, si los ataques se leen a tiempo, si el carrito o el arrastre son demasiado fáciles o difíciles, y cualquier error con la hora aproximada. El registro del juego está en `%USERPROFILE%\AppData\LocalLow\Stefano936\Uncle Extraction\Player.log`.
