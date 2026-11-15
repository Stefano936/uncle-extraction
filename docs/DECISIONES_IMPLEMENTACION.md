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

## Plantilla para nuevas decisiones

Fecha, problema observado, decisión, alternativas relevantes, impacto y evidencia. Para balance, registrar valores anteriores y nuevos y el resultado de la prueba.
