# Decisiones de implementación

Las decisiones de diseño originales están en `preproduction/uncle_extraction/DECISIONES.md`. Este registro complementa ese documento.

## 2026-11-15 — Organización del repositorio

- Mantener los cuatro paquetes fuente bajo `preproduction/`, preservando sus archivos.
- Conservar el prompt maestro completo en la raíz.
- Usar `CLAUDE.md` como entrada breve y consultar detalles por hito.
- Reservar `UnityProject/` para el proyecto real que se creará en M0.
- Entregar contenido descomprimido; los ZIP de transporte no se agregan al repositorio.
- Mantener los binarios actuales en Git normal para este primer import. El archivo individual más grande entregado es menor de 20 MB. Si el arte empieza a crecer o cambia seguido, evaluar Git LFS antes de incorporar nuevos archivos grandes y revisar su configuración real.

Motivo: facilitar lectura selectiva, continuidad y primer import sin obligar a configurar servicios adicionales antes del laboratorio.

## 2026-11-15 — Repositorio local y Git LFS

- Problema: ninguna herramienta de la sesión pudo autenticarse contra el repositorio privado (403), así que no se pudo clonar.
- Decisión: inicializar Git en `uncle-extraction/` (la carpeta ya extraída, verificada contra `ENTREGA_SHA256.json`) y unir después la historia remota con `git merge --allow-unrelated-histories` (pasos en `PROGRESS.md`). Alternativa descartada: copiar a un clon nuevo, que requería credenciales no disponibles.
- `.gitignore`: agregados generados extra de Unity/IDE (`.utmp/`, `*.pdb`, `*.unitypackage`, Addressables en StreamingAssets, `.vscode/`, etc.). `.gitattributes`: normalización LF para `.mat`, `.anim`, `.controller`, `.inputactions`; `preproduction/**` sigue `-text` (10 fuentes tienen CRLF y deben quedar intactas).
- LFS: no por ahora (máx. 19,3 MB, total ~115 MB, por debajo del límite de 100 MB por archivo de GitHub). Reevaluar antes de agregar audio, texturas 2K+, `.blend` que cambien seguido o cualquier archivo >50 MB; en ese caso migrar `*.psd, *.wav, *.blend, *.fbx` de `UnityProject/` a LFS antes de que crezca el historial.

## 2026-11-15 — M1: simplificaciones del primer PhysicsLab (reversibles)

- Tío = un Rigidbody central de 110 kg con sockets en partes visuales (la especificación 04 lo permite para el primer prototipo). El ragdoll de 11 cuerpos queda para después de medir el laboratorio.
- La caída del jugador = bloqueo de control + cámara inclinada, sin ragdoll del jugador (especificación 03).
- Tras levantarse, la compostura vuelve a ≥60 (el GDD no lo fija). Ajustar en playtest.
- Carrito: rotaciones X/Z congeladas para estabilidad; la correa (E) funciona con el tío **cerca** de la caja y lo acerca con un resorte limitado.
- Entrada leída directamente de dispositivos (Input System si está instalado; si no, Input Manager clásico). Acciones remapeables en M6.
- HUD con IMGUI para no depender de paquetes de UI; el kit de UI se integra en M6.
- Render: la escena usa el pipeline activo. **URP pendiente** (crear su asset sin el editor abierto es frágil); al pasar a URP, reconstruir la escena desde el menú.
- Build del laboratorio con Mono: IL2CPP necesita Visual Studio con C++. Se prueba IL2CPP cuando esté.
- Capas de colisión: todavía no. Las manos son virtuales (sin colisionador), así que no se realimentan con el cuerpo agarrado.
- Pendiente para cerrar M1: distracción con vaso señuelo, ayuda de compañero al levantarse, superposición con sólidos y puerta en la extracción.

## 2026-11-15 — M0/M1: paquetes fijados y levantada del tío

- Problema: `ProjectBootstrap` no podía ejecutarse en un proyecto nuevo porque los asmdef de pruebas no compilaban sin Test Framework. Decisión: fijar Input System 1.20.1, Test Framework 1.6.0 y uGUI 2.0.0 directamente en `Packages/manifest.json` (se versiona). Se quitaron paquetes por defecto no usados (purchasing, analytics, collab-proxy, 2D, XR helpers).
- Problema: en el intento individual automático, el tío llegaba a la zona varias veces pero no se aseguraba. Al soltarlo, si llevaba ≥2,5 s en el piso se levantaba al instante y se iba caminando antes de los 1,5 s de conteo. Decisión: para levantarse necesita 2,5 s en el piso **y** 2,5 s sin agarres (`NpcBrain.Decide`). Antes: solo 2,5 s en el piso. Evidencia: antes, 1 de 2 corridas de la batería fallaba (el tío salía de la zona 3 veces); después, 3/3 PASS en ~20 s. Reversible; revisar en el playtest si hace al tío demasiado dócil.
- Entrada simulada: `LabInput.Simulated[]` se suma a la entrada real para que las pruebas usen los mismos caminos que un jugador (E, Q, Espacio, agacharse).

## 2026-11-15 — M1: correa del carrito, autopiloto y pruebas en tiempo real

- Correa: antes era una unión con límite lineal y rotación libre; el tío quedaba en cualquier orientación y, atravesado (1,9 m), no pasaba la puerta de 1,5 m. Decisión: al asegurar, la correa lo acuesta a lo largo de la caja (cabeza hacia el frente, boca arriba) y bloquea la rotación relativa; se desactiva la colisión tío-carrito, porque el prototipo se superpone con las barandas. Alternativa pendiente: ragdoll o pose encogida. Reversible.
- `LabAutopilot`: el bot de pruebas pasó a ser un componente de la escena, para que pruebas y build compartan código y el usuario pueda mirarlo. Usa solo entradas de jugador.
- Pruebas de intento completo en tiempo real: con `timeScale` 4 el carrito pasaba 3/3, pero en la build fallaba 0/9 (el bot decide por frame y a 4x controla más grueso). Se usa `timeScale` 1 y la prueba del carrito queda `[Ignore]` con motivo hasta resolver la puerta.
- `runInBackground = true`: sin foco, la build se pausaba (una prueba de humo se congeló a los 2,8 s). También es necesario para el anfitrión de M2.

## Plantilla para nuevas decisiones

Fecha, problema observado, decisión, alternativas relevantes, impacto y evidencia. Para balance, registrar valores anteriores y nuevos y el resultado de la prueba.
