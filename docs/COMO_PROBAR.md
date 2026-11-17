# Cómo probar UNCLE EXTRACTION (prototipo)

## Abrir

1. Doble clic en `Builds\UncleExtraction\UncleExtraction.exe` (se genera en la PC de desarrollo; no está en Git).
2. Menú: **Jugar solo** → tocar **Dificultad** hasta la deseada → **Iniciar extracción**. La primera vez aparece una guía corta (Primeros pasos) que se puede omitir o repetir desde la pausa. Los laboratorios de diagnóstico solo aparecen con `-ueDebug` o en la build de desarrollo. El escenario principal usa el tío de 11 cuerpos (ragdoll reducido, malla del kit). **Laboratorio** conserva el tío anterior de primitivas (un solo cuerpo) para comparar, y **Laboratorio: tío articulado (prueba)** es el laboratorio con el tío nuevo.
3. **Jugar con amigos** (2 a 4 jugadores):
   - **Por internet:** el anfitrión elige **Crear sala por internet** y pasa el **código de sala** que aparece en la sala (botón **Copiar código**); los demás lo escriben y eligen **Unirse con código**. No hace falta abrir puertos ni conocer la IP.
   - **Red local (IP):** en una PC **Crear partida en la red local** (Windows puede pedir permiso de firewall); en las otras **Unirse por IP** con la IP del anfitrión (`127.0.0.1` en la misma PC).
   - Primero se abre la **sala**: cada jugador pulsa **LISTO** y el anfitrión, que elige la dificultad, pulsa **INICIAR EXTRACCIÓN**. No se puede entrar con la ronda ya empezada; al reintentar se vuelve a la sala. El nombre de jugador se elige en Opciones.
4. **Opciones** (en el menú o en la pausa):
   - idioma (español rioplatense o inglés), volumen general, de efectos y de avisos (ataques y marcas), sensibilidad del mouse, campo de visión, eje Y invertido, subtítulos de ataques, tamaño del texto, reducir inclinación de cámara, modo de agarre (mantener o alternar) y **gráficos** (Alta o Baja; con GPU integrada empieza en Baja);
   - **controles**: elegir una acción y pulsar la tecla nueva. Esc cancela. Si la tecla ya estaba en uso, las dos acciones se intercambian;
   - **Aplicar cambios** guarda; **Volver** con cambios pregunta si aplicar o descartar.

## Objetivo

Sacar al tío del bar, subirlo por la rampa a la camioneta (arrastrándolo o en el carrito), salir y **mantener E 1 s desde afuera** para cerrar las puertas. Se gana cuando las puertas quedan cerradas con el tío bien adentro y la abertura libre. Se pierde si se acaba el tiempo.

## Controles

Controles por defecto (teclado y mouse se cambian en Opciones → Controles; el HUD y los avisos muestran la tecla asignada):

| Acción | Teclado y mouse | Mando |
| --- | --- | --- |
| Moverse / mirar | WASD o flechas / mouse | stick izquierdo / stick derecho |
| Mano izquierda / derecha | clic izquierdo / derecho (mantener, o un clic agarra y otro suelta si elegís «Alternar» en Opciones → Agarre) | gatillo izquierdo / derecho |
| Esfuerzo (más fuerza un rato) | Shift | clic del stick izquierdo |
| Soltar todo | Q | B / círculo |
| Interactuar (mantener o tocar): soltarle la mano de la barra, cargar y asegurar en el carrito, cerrar la camioneta, ayudar a un compañero caído | E | A / cruz |
| Agacharse / paso evasivo | Ctrl / Espacio | clic del stick derecho / X / cuadrado |
| Marcar para el equipo (tío, carrito, camioneta o un punto; 6 s) | clic central | cruceta arriba |
| Pausa, opciones, volver al menú | Esc | Start / Options |
| Reintentar | botón de resultados o de la pausa (en red decide el anfitrión) | — |
| Panel de depuración (mostrar el cursor) | Tab | — |

En los menús, el mando navega con la cruceta o el stick, acepta con A/cruz y vuelve con B/círculo. El código de sala se escribe con teclado (no hay teclado en pantalla). Se reasignan el movimiento y las acciones principales de teclado y mouse (las flechas quedan siempre como alternativa; Esc y Tab están reservadas). Los cambios se guardan con **Aplicar**; **Volver** sin aplicar los descarta.

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

## Prueba guiada

`PRUEBA_HUMANA.txt` (junto al ejecutable) tiene un guion de 25–35 minutos con las tareas y una planilla.

## Qué anotar

Qué se sintió bien o mal, dónde te trabaste, si los ataques se leen a tiempo, si el carrito o el arrastre son demasiado fáciles o difíciles, y cualquier error con la hora aproximada. El registro del juego está en `%USERPROFILE%\AppData\LocalLow\Stefano936\Uncle Extraction\Player.log`.
