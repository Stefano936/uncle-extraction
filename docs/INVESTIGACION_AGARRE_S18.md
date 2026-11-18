# Investigación de agarre y caída · sesión 18

Referencia de juego `82c28e123adde35455ba0a6d8064a75e6823c3c7`. Se mantuvo Detallado durante las pruebas de velocidad y puntería. Cada variante tiene su propio cambio de juego y comparte las seis sondas PlayMode del commit `8f7f33c`. La referencia y las tres variantes pasaron 6/6 en Unity 6000.3.28f1, con el anfitrión local en el puerto 7890.

| Hipótesis | Referencia | Ajuste aislado y resultado |
| --- | --- | --- |
| Frenazo al agarrar | 4,200 → 2,800 m/s en 6 pasos físicos (~100 ms). En el primer paso ya va a 3,933 m/s. | Rampa del límite de velocidad de 180 ms. Primer paso a 4,070 m/s y 2,800 m/s en 11 pasos (~183 ms). |
| Selección de puntos | Un barrido de −6° a +6° cambia de punto una vez. En 12 muestras alternadas a ±0,35° cerca del empate hubo 11 cambios. | Margen de 1,0 en el puntaje entre dos puntos válidos y libres. Hubo 0 cambios en esas 12 muestras. El barrido regresó la misma secuencia y el clic confirmó el punto derecho. |
| Cámara al caer | Pasa de 1,62 m de pie o 1,05 m agachado a 0,45 m. El giro llega a 65° sin reducción y a 12° con reducción. | Límite de inclinación normal de 40°. Se midieron 40° tanto de pie como agachado. La altura, la inclinación con reducción y el giro cerca de 100 ms se mantuvieron dentro de la variación observada. |

## Ramas

- [Rampa de velocidad](https://github.com/Stefano936/uncle-extraction/commit/40a78b0940ff381acc7f5a75c4a499130f975688).
- [Estabilidad de puntos](https://github.com/Stefano936/uncle-extraction/commit/07db5939ed2b19edfeb83ee583ab46da7b604abd).
- [Inclinación al caer](https://github.com/Stefano936/uncle-extraction/commit/bfa4f8d3aae5fa0b04edbfd71c550fdd69107269).

## Cómo se midió

- **Velocidad.** Tramo recto con avance continuo y agarre confirmado sobre un punto de prueba. Se desactivó la reacción del resorte solo durante la sonda para medir el límite del motor. Se contó hasta alcanzar 2,85 m/s o menos con pasos físicos de 1/60 s. Falta evaluar el efecto de la rampa con peso real y al soltar y volver a agarrar.
- **Puntería.** Dos puntos fijos, libres y a 1,2 m, con el jugador quieto. Se barrió un grado por muestra y luego se alternó la mirada alrededor del empate. Un clic del Input System comprobó la confirmación del punto seleccionado. Falta comparar el comportamiento con obstáculos y la selección entre anfitrión e invitado con latencia.
- **Cámara.** El mismo impacto por agotamiento de compostura, sin empuje ni agarre, de pie y agachado, con y sin reducción de movimiento. Se midieron altura y máxima inclinación durante los primeros 0,55 s. La comodidad de la transición queda para la prueba humana.

Para repetir cada variante desde su rama, ejecutar `tools/windows/run_tests.ps1 -Platform PlayMode -Filter S18Probe`. La referencia de la sonda se reproduce con el commit `8f7f33c`, que agrega instrumentación de prueba al SHA del HUD. Los resultados de estas ramas corresponden a escenarios controlados.

