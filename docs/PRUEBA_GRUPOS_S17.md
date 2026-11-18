# Prueba con dos grupos nuevos (sesión 17, preparada; sin realizar)

Objetivo: medir con personas que **no vieron el juego** lo que las pruebas automáticas no miden: si el primer minuto se entiende, si agarrar se siente bien y si piden otra ronda. La prueba anterior del dueño (Stefano, demo s15) queda registrada aparte en `OBSERVACIONES_PRUEBAS.md`.

**Build:** `UncleExtraction_Demo_s17.zip` (anotar el SHA-256 de `UncleExtraction_Demo_s17.zip.sha256` y el commit de `VERSION.txt`). Todos con la misma demo.

## Grupos

| Grupo | Personas | Conexión | Qué cubre |
| --- | --- | --- | --- |
| A | 1 jugador solo, luego 2 | misma PC (solo) y **Relay entre dos PC** (código de sala) | primer minuto, solo y dúo |
| B | 3 o 4 | **Relay entre PC en redes distintas** (una por datos del celular) | grupo grande, latencia real, disputa por agarres |

Anotar por separado: misma PC (localhost), LAN y internet. Si no hay segunda PC, marcar internet como **NOT_RUN**.

## Guion (sin explicar nada; quien observa no ayuda salvo bloqueo de más de 2 min)

1. **Primer minuto (cronometrar):** desde «Iniciar extracción» hasta el **primer agarre** del tío. Meta: menos de 60 s. Anotar qué miró primero, si encontró al tío y la salida, si leyó el rótulo del botón.
2. **Agarre:** ¿se nota cuándo la mano se cierra, cuándo está por resbalar y cuándo soltó? ¿Entendió «ocupado: lo tiene X»?
3. **Barra:** ¿descubrió E para soltarle la mano? ¿Usó la campana? ¿La usaron de a dos?
4. **Ataques:** ¿leyó el aviso antes del golpe? ¿Supo levantarse o ayudar a otro?
5. **Camioneta:** ¿entendió que hay que cerrar desde afuera manteniendo E?
6. **Otra ronda:** al terminar, ¿pidió otra sin que se lo propongan? Con **Repetir configuración** el grupo vuelve a la sala sin código nuevo; probar el desafío **karaoke** en la segunda.
7. **Parte:** ¿leyó el momento destacado? ¿Le pareció justo o gracioso?
8. Pregunta final: «¿Qué le contarías a un amigo de este juego?» (literal).

## Planilla

| Grupo | Fecha | Build (commit) | Conexión | Primer agarre (s) | Se trabó en | Pidió otra ronda | Frase literal | Acción |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A |  |  |  |  |  |  |  |  |
| B |  |  |  |  |  |  |  |  |

Registro del juego: `%USERPROFILE%\AppData\LocalLow\Stefano936\Uncle Extraction\Player.log` (adjuntarlo si algo falla).

## Grabación

Pedir permiso para grabar pantalla con sonido (Xbox Game Bar: Win+Alt+R). Un clip de 20–40 s de juego real con sonido sirve como material del 4.3; no hay grabador instalado en la PC de desarrollo (instalar ffmpeg u OBS necesita permiso del dueño).
