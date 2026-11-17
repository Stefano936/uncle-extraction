# Prueba con personas (guion corto, 25–35 min)

Sirve para lo que las pruebas automáticas no miden: si se entiende, si los controles son cómodos y si es divertido. Un autopiloto que gana no responde nada de esto.

## Antes de empezar (quien coordina)

- Build: `Builds\UncleExtraction\UncleExtraction.exe`. Anotá el commit que dice `VERSION.txt` (junto al ejecutable).
- **No expliques el objetivo ni los controles.** Dejá que la persona abra el juego y lea lo que el juego muestra. Si se traba más de 2 minutos, ayudá con una sola frase y anotá qué tuviste que decir: es un hallazgo.
- Pedile que piense en voz alta. Anotá lo que dice, no lo que vos creés que quiso decir.
- Para la parte con amigos hacen falta 2 a 4 PC. En la misma red: **Jugar con amigos → Red local (IP)**. Por internet: **Crear sala por internet** solo funciona cuando el proyecto esté vinculado a Unity Cloud (ver `PROGRESS.md`).

## Tareas

Marcá cada tarea: **Sí** (sola), **Con ayuda** (qué frase), **No**. Anotá el tiempo aproximado.

| # | Tarea (lo que se le pide a la persona) | Qué observar |
| --- | --- | --- |
| 1 | «Abrí el juego y empezá una partida solo.» | ¿Encuentra **Jugar solo → Iniciar extracción**? ¿Entiende el objetivo con el HUD (sacar al tío y cerrar la camioneta) sin preguntar? |
| 2 | «Agarrá al tío con las dos manos.» | ¿Descubre clic izquierdo/derecho? ¿Lee el anillo de la mira (sólido, grueso, punteado)? ¿Entiende «ocupado» o «muy lejos»? |
| 3 | «Hacé que se suelte de la barra.» | ¿Encuentra la manija/mano anclada y la tecla E? ¿Prueba el vaso de cerveza? |
| 4 | «Evitá que te pegue.» (jugar 2–3 min) | Por cada ataque (cachetada, empujón, patada, sacudida): ¿lo vio venir? ¿Supo qué hacer (alejarse, esquivar, soltar)? ¿Le pareció injusto? |
| 5 | «Llevalo en el carrito.» | ¿Entiende cargarlo y atarlo (E)? Si el carrito se traba en la puerta: ¿lo destraba (empujar adentro, alinearse, tirar derecho)? ¿Cuánto tarda? |
| 6 | «Metelo en la camioneta y cerrala.» | ¿Lee el rombo que marca qué molesta? ¿Entiende «mantené E desde afuera»? |
| 7 | «Perdé a propósito (esperá) y reintentá.» | ¿Entiende la pantalla de resultados y encuentra **Reintentar**? |
| 8 | «Cambiá el idioma, el tamaño del texto y una tecla; cerrá el juego y volvé a abrirlo.» | ¿Se guardaron? ¿Algo quedó cortado o encimado con texto grande? |
| 9 | Con amigos: «Uno crea la sala y los otros se unen; todos listos y empiecen.» | ¿Encuentran crear/unirse? ¿Entienden LISTO y quién inicia? ¿El motivo de «no se puede iniciar» se entiende? |
| 10 | Con amigos: jugar una ronda completa. | ¿Se coordinan? ¿Se nota la latencia? ¿Algún tirón, teletransporte o desincronización? |

## Preguntas al final (1 = nada, 5 = mucho)

| Pregunta | 1–5 | Comentario |
| --- | --- | --- |
| ¿Se entendía qué había que hacer? | | |
| ¿Los controles se sentían cómodos? | | |
| ¿Los ataques se veían venir a tiempo? | | |
| ¿Fue divertido? ¿Jugarías otra ronda? | | |
| ¿Fue demasiado fácil o difícil? (en qué dificultad) | | |
| ¿Qué fue lo más frustrante? | | |
| ¿Qué fue lo más gracioso? | | |

## Datos de la sesión

| Campo | Valor |
| --- | --- |
| Fecha y hora | |
| Commit de la build (`VERSION.txt`) | |
| PC (CPU, GPU, resolución, monitor) | |
| Mando o teclado y mouse | |
| Jugadores y cómo se conectaron (misma PC, LAN, internet) | |
| Dificultad | |
| Resultado de cada ronda (ganó/perdió, tiempo) | |
| Errores vistos (hora aproximada) | |

El registro técnico queda en `%USERPROFILE%\AppData\LocalLow\Stefano936\Uncle Extraction\Player.log`: copialo junto con la planilla. Las observaciones se pasan a `docs/OBSERVACIONES_PRUEBAS.md` tal como se dijeron.
