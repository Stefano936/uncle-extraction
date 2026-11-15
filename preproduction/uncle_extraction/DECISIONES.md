# Registro de decisiones - v1.0

## Requisitos del usuario

- Comedia bizarra, caótica, con humor adulto y frases en inglés.
- Extraer del bar a un adulto controlado por el juego que no se quiere ir.
- El NPC pega, empuja y tira a los jugadores cuando intentan moverlo.
- De uno a cuatro jugadores, con dificultad elegida y escalado por tamaño de grupo.
- Cooperación funcional en grupos: transportar, liberar anclajes, distraer y despejar ruta.
- Preparar documentación extensa, PDFs y material útil para implementación con Claude.
- Objetivo de distribución: Steam.

## Decisiones de esta propuesta

- Cámara en primera persona; hasta cuatro online; individual viable; no pantalla dividida inicial.
- Unity 6.3 LTS, C#, URP; parche y paquetes fijados tras prueba M0.
- Host autoritativo; NGO y Unity Transport; MPS Sessions/Relay para internet.
- Steamworks.NET como posible adaptador de plataforma, sujeto a compatibilidad; no se supone transporte automático.
- Un bar y una camioneta estacionada; no conducción ni mundo abierto.
- Dos manos por jugador; sockets, fuerzas acotadas, carrito y anclajes contextuales.
- Cuatro ataques con señales y recuperación; sin muerte ni sistema de dejar inconsciente al NPC.
- Dificultad y N son ejes separados. La masa no escala; sí resistencia y presión.
- No migración de host ni incorporación a ronda en curso inicialmente.
- Voces preparadas; no modelo de lenguaje en vivo; sin reconocimiento de voz obligatorio.

## Posterior al núcleo

- Marihuana / Filósofo después de muestra estable; Mezcla después de ambas variantes.
- Personalidades pequeñas y escenarios nuevos solo si agregan decisiones.
- Proximidad de voz, captura automática de clips y rankings públicos son P2.

## Descartado o no incluido

- Mentiras que alteran la realidad: idea exploratoria no elegida por el usuario.
- Un quinto jugador controlando al tío, PVP, armas, conducción y monetización de poder.
- Prometer alta probabilidad de éxito o afirmar que nadie hizo una premisa similar.

## Pendientes que no bloquean el laboratorio

- Nombre comercial y revisión de disponibilidad.
- Precio, presupuesto, calendario, requisitos mínimos y contenido final de lanzamiento.
- Proveedor de voces/modelos, licencias finales y configuración de cuentas.
- Ajustes de balance tras pruebas; los valores adjuntos no están validados.

## Protocolo de cambios

Registrar fecha, problema observado, decisión, impacto y evidencia. Detalles internos reversibles pueden resolverse sin consultar. Un cambio de motor, topología, cámara o alcance debe justificarse antes de una reescritura. No actualizar dependencias silenciosamente.
