# Cómo probar UNCLE EXTRACTION (prototipo)

## Abrir

1. Doble clic en `Builds\UncleExtraction\UncleExtraction.exe` (se genera en la PC de desarrollo; no está en Git).
2. Menú: **Jugar solo** → tocar **Dificultad** hasta la deseada → **Iniciar extracción**. El **Laboratorio** es la escena de diagnóstico. El escenario principal usa el tío de 11 cuerpos (ragdoll reducido, malla del kit). **Laboratorio** conserva el tío anterior de primitivas (un solo cuerpo) para comparar, y **Laboratorio: tío articulado (prueba)** es el laboratorio con el tío nuevo.
3. Cooperativo en la misma PC o LAN: en una ventana **Jugar con amigos → Crear partida en la red local**; en otra **Unirse por IP** con la IP del anfitrión (`127.0.0.1` en la misma PC). Windows puede pedir permiso de firewall al anfitrión. Primero se abre la **sala**: cada jugador pulsa **LISTO** y el anfitrión, que elige la dificultad, pulsa **INICIAR EXTRACCIÓN**. No se puede entrar con la ronda ya empezada; al reintentar se vuelve a la sala. Las salas por internet (Relay) todavía no están disponibles.
4. **Opciones** (en el menú o en la pausa):
   - idioma (español rioplatense o inglés), volumen, sensibilidad del mouse, campo de visión, eje Y invertido, subtítulos de ataques y tamaño del texto;
   - **controles**: elegir una acción y pulsar la tecla nueva. Esc cancela. Si la tecla ya estaba en uso, las dos acciones se intercambian;
   - **Aplicar cambios** guarda; **Volver** con cambios pregunta si aplicar o descartar.

## Objetivo

Sacar al tío del bar, subirlo por la rampa a la camioneta (arrastrándolo o en el carrito), salir y **mantener E 1 s desde afuera** para cerrar las puertas. Se gana cuando las puertas quedan cerradas con el tío bien adentro y la abertura libre. Se pierde si se acaba el tiempo.

## Controles

Teclas por defecto (se pueden cambiar en Opciones → Controles; el HUD y los avisos muestran la tecla asignada):

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

- Antes de cada golpe hay tres avisos, que no dependen solo del color:
  - el tío toma la pose de preparación (brazo arriba, brazos atrás, rodilla levantada o brazos abiertos);
  - en el piso aparece la zona que va a barrer el golpe, que se llena hasta el impacto;
  - arriba se lee el aviso («VA A PATEAR»).

  Hay que alejarse, esquivar o soltarlo.
- Al apuntar a un punto, un anillo indica si se puede agarrar (sólido), si está ocupado (grueso) o si hay que acercarse (punteado). La línea de cada mano se engrosa y parpadea cuando está por soltarse.
- Si la camioneta no cierra, un rombo marca lo que molesta («Le queda un pie afuera: empujalo adentro»). Se puede empujar el pie o entrar por el costado y tirar de los dos hombros hacia la cabina.
- Si el carrito se tuerce en la puerta del bar, alcanza con ponerse en el eje de la puerta y tirar derecho del asa. La puerta tiene una manija que se puede agarrar para sostenerla abierta.
- Con una sola mano no alcanza para arrastrarlo caído; usá las dos o el carrito.
- El vaso de cerveza (sobre una mesa) distrae al tío si lo llevás y lo apoyás a su vista: suelta la barra unos segundos.
- Tomarlo de los tobillos ayuda a entrarlo «con los pies primero»; si las piernas quedan afuera, no se puede cerrar.

## Qué anotar

Qué se sintió bien o mal, dónde te trabaste, si los ataques se leen a tiempo, si el carrito o el arrastre son demasiado fáciles o difíciles, y cualquier error con la hora aproximada. El registro del juego está en `%USERPROFILE%\AppData\LocalLow\Stefano936\Uncle Extraction\Player.log`.
