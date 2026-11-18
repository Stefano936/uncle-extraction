# Sesión 18 · HUD contextual y feedback de agarre

Comparar **Contextual** y **Detallado** en el mismo ejecutable de Windows. En partida, abrir **Pausa → Opciones → HUD**, elegir el perfil, pulsar **Aplicar** y volver al juego. Contextual es el valor inicial cuando no hay una elección guardada. Usar la demo de jugadores s17 (0.17.0, protocolo 2, `b44386d`) como referencia externa.

## Compilación y QA en Windows

Usar el editor **6000.3.28f1**. Para estas pruebas, generar la build Mono habitual con `UncleExtraction.EditorTools.BuildTools.BuildWindows`, desde el proyecto `UnityProject`. Para `guesttoggle_sim`, usar además la build de desarrollo (`-ueDev`).

```powershell
$env:UE_TEST_FPS = '60'
powershell -ExecutionPolicy Bypass -File tools/windows/run_tests.ps1 -Tag s18
powershell -ExecutionPolicy Bypass -File tools/windows/ui_audit.ps1 -Lang es_UY -Out .local-setup/shots/s18_es
powershell -ExecutionPolicy Bypass -File tools/windows/ui_audit.ps1 -Lang en -Out .local-setup/shots/s18_en
powershell -ExecutionPolicy Bypass -File tools/windows/net_smoke.ps1 -Scenario guesttoggle,guesttoggle_sim,race -Scene Bar_LastRound
```

La auditoría incluye las vistas de apuntado de ambos perfiles. Los registros quedan en `.local-setup/`.

## Prueba manual A/B · pendiente

1. Empezar una ronda con la guía nueva. Comparar qué se lee al entrar, después de unos segundos sin actuar y al apuntar al tío. Comprobar que Contextual tiene mira y reloj discreto (sin reloj en ronda sin límite), sin tarjetas de manos ni rótulo 3D de agarre. Detallado debe mostrar las tarjetas, la tensión y el rótulo. Probar un punto lejos, uno alcanzable y un punto de capacidad uno que ya tenga otra persona. Mantener la mira sobre la misma restricción y cambiar de motivo. Confirmar que aparece el nombre del titular cuando está disponible.
2. Alternar teclado/ratón y mando durante la ronda y reasignar el agarre izquierdo. Verificar la instrucción de Mantener o Alternar, en español e inglés y al 150 %, especialmente a 720p y 21:9. Comprobar con la guía activa que sigue orientando al jugador. Probar ambos perfiles antes y después de cerrar y volver a abrir el juego.
3. Probar una y dos manos con ambos modos de agarre. Buscar el cierre parcial al intentar (más fácil como invitado con latencia), el cierre firme al confirmar y un contacto por mano. Probar la soltada voluntaria (también con Q o B), una pérdida por distancia o golpe y una desaparición cuyo motivo el invitado no pueda determinar. Escuchar la diferencia entre soltada, resbalón y roce neutro. Probar tensión alta sin perder el agarre y esfuerzo hasta agotar la energía. La barra común debe aparecer cuando aporta contexto y ocultarse al recuperarse. Repetir con Reducción de movimiento.
4. Cancelar un intento con la otra pulsación, pausa, pérdida de foco, caída o reinicio. Probar también el rechazo sobre un punto que acaba de ocupar otra persona. Seguir un aviso de ataque con subtítulos activados y ayudar a un compañero caído. Soltar la mano anclada y, desde afuera, intentar cerrar la camioneta con alguien o algo en la abertura. El aviso de cierre debe identificar el obstáculo o señalar su posición. Comparar la claridad de estas acciones durante la partida y registrar localhost, LAN y Relay por separado.

## Hipótesis para después del A/B

| Hipótesis | Cómo observarla por separado |
| --- | --- |
| La velocidad objetivo baja de 4,2 a 2,8 m/s al sostener algo. | Mantener un solo perfil y recorrer el mismo tramo recto sin agarre y con agarre confirmado; anotar cuándo se percibe el cambio de ritmo frente al momento exacto del contacto. |
| El candidato se recalcula cada cuadro por ángulo y distancia. | Con los pies quietos y dos puntos cercanos, barrer despacio la mira entre ambos. En Detallado, anotar cuándo alterna el rótulo y qué punto se confirma si se pulsa entre los dos. |
| La cámara cambia mucho entre los estados de movimiento y de caída. | Repetir el mismo impacto sin agarrar, primero de pie y luego agachado. Observar altura, giro y tiempo de lectura del aviso en la transición, con reducción de movimiento activada y desactivada. |
