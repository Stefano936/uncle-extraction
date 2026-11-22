# Plan de pruebas con grupos nuevos y de difusión (borrador)

Vos manejás cuentas, contactos y publicación. Esto deja listo qué medir y qué mandar.

## 1. Prueba sin instrucciones (grupos nuevos)

**Regla:** nadie del equipo explica los controles durante el primer minuto. Se usa el ZIP aprobado en UE-05, con el mismo SHA en todos los equipos, y se observa. Las preguntas se hacen **antes** de contar qué esperábamos que les gustara.

| Qué registrar | Cómo | Meta de esta etapa |
| --- | --- | --- |
| ¿Entiende la misión sin ayuda? | Observación; primera frase que dice | Sí, en el primer minuto |
| Tiempo hasta el primer agarre | Cronómetro desde que aparece en el bar (el parte también registra «lo agarró primero» con su tiempo) | < 60 s |
| Dónde se estanca | Notas con hora (mano en la barra, carrito en la puerta, cierre desde afuera) | Ningún atasco > 2 min sin salida |
| Primer cierre | Tiempo y cómo llegó (carrito o arrastrando) | Gana la primera ronda en Normal, solo o en grupo |
| ¿Eligen otra ronda? | Sin sugerirlo: ¿apretan «Otra ronda» solos? | ≥ 1 ronda extra voluntaria por grupo |
| ¿Qué le contarías a un amigo? | Pregunta abierta al final | Que mencionen un momento concreto (no «estuvo bueno») |

Preguntas (en este orden):
1. ¿Qué había que hacer? (sin mirar la pantalla)
2. ¿Qué momento le contarías a un amigo?
3. ¿Qué te frustró?
4. ¿Volverías a jugar con este grupo? ¿Ahora o otro día?
5. Recién ahora: ¿te diste cuenta de …? (guía, marcador, sujetar el brazo que levanta, desafíos)

Usar la [planilla de grupos](../PRUEBA_GRUPOS_S17.md) con el SHA actual y la [guía de la candidata](../VOLVER_A_PROBAR_CANDIDATA.md) para las comprobaciones técnicas. Guardar las frases textuales en [observaciones](../OBSERVACIONES_PRUEBAS.md).

## 2. Métricas propias de esta etapa

| Métrica | Fuente | Estado |
| --- | --- | --- |
| Personas que inician la demo / la terminan (ganan una ronda) | Planilla de cada prueba; más adelante, telemetría opcional (no implementada) | Sin datos: pendiente de pruebas |
| Grupos que repiten por voluntad propia | Observación | Sin datos |
| Conversión a lista de deseados | Steamworks (cuando exista la página) | Sin página |
| Referencia pública | One More Lawn: «Muy positivas», 94 % de 618 reseñas a ~6 días de su lanzamiento (12-nov-2026) | Es su resultado, no el nuestro; se compara a ventanas equivalentes cuando tengamos datos |

Las cifras comerciales futuras son **metas**, no resultados.

## 3. Difusión con creadores chicos de cooperativo (borradores)

**A quién:** canales de 1k–50k suscriptores que juegan cooperativos de comedia física en grupo (perfil, no nombres: elegís vos).

**Mensaje (ES):**
> Hola, [nombre]. Hacemos UNCLE EXTRACTION, una comedia física cooperativa de 1 a 4: es la hora del cierre y hay que sacar del bar a un tío que no se quiere ir. Tenemos una demo privada de una ronda completa (15–20 min con amigos). ¿Te interesa probarla con tu grupo? No pedimos nada a cambio; si la mostrás, nos encantaría ver qué momento les quedó.

**Message (EN):**
> Hi [name] — we're making UNCLE EXTRACTION, a 1–4 player physical-comedy co-op: it's closing time and you have to get an uncle who won't leave out of the bar. We have a private demo with one full round (15–20 min with friends). Want to try it with your group? No strings attached; if you show it, we'd love to know which moment stuck.

**Qué adjuntar:** enlace privado al ZIP aprobado, SHA-256, guía de instalación y materiales aprobados. Pedir que el grupo use el mismo ZIP en cada PC y consulte antes de reenviar. Acordar con el equipo el permiso para transmitir o compartir imágenes antes de enviar las invitaciones.

## 4. Entrega privada y reportes

Antes de una difusión a creadores, invitar a dos grupos que nunca hayan jugado. Quien coordine cada grupo recibe el enlace y responde por el mismo canal. El equipo copia las incidencias reproducibles a Trello con el SHA en la descripción. Si hay una sola PC, tomar datos del primer minuto y de una ronda solo. Si falta un mando, tomar datos de teclado y ratón y dejar la prueba de mando pendiente.

| Grupo | SHA | Descargas confirmadas | Instalaciones que abren | Rondas iniciadas | Rondas completas | Rondas repetidas por elección | Incidencias |
| --- | --- | --- | --- | --- | --- | --- | --- |
| | | | | | | | |

**Reporte técnico.** Adjuntar un fragmento del log, el commit de `VERSION.txt`, checksum, Windows, CPU/GPU, controles, topología (solo, LAN o Relay), pasos, resultado esperado, observado y frecuencia. Quitar códigos de sala y datos personales antes de compartir el log.

**Feedback de juego.** Anotar cantidad de jugadores, dificultad, HUD, tiempo al primer agarre, qué entendieron, dónde se trabaron, resultado de la ronda y si eligieron repetir.

## 5. Corrección y reversión

1. Guardar el ZIP aprobado y la entrega anterior, cada uno con su `VERSION.txt`, SHA completo, checksum y evidencia. Para una entrega Steam, agregar App ID, rama, Build ID y manifiestos del depot.
2. Clasificar incidencias por impacto y frecuencia. Priorizar bloqueos de arranque, conexión, victoria o pérdida de progreso. Reproducir contra el SHA reportado y abrir una rama de corrección desde ese corte.
3. Si cambian los binarios que se distribuyen, asignar una versión de juego nueva. Aumentar el protocolo si se cambia de forma incompatible la serialización, el estado replicado o los RPC. Comprobar el rechazo localizado en ambos sentidos entre la build anterior y la nueva.
4. Generar un ZIP nuevo, verificar el checksum y ejecutar las pruebas dirigidas, arranque en copia limpia y una ronda completa. Si afecta red, repetir también la topología afectada.
5. Para volver atrás, recuperar la copia archivada, verificar su checksum y ejecutar desde una carpeta limpia. En Steam, seleccionar el Build ID anterior en la rama privada y comprobar desde una instalación de prueba. Registrar motivo, SHA, verificación y fecha.

La recuperación real del ZIP anterior y el cambio de build en Steam quedan pendientes hasta disponer de los artefactos y de la rama privada.
