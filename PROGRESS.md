# Estado del proyecto

Última actualización: 2026-11-15 (Cowork, sesión 2: entorno + código M1).

## Hito actual

M0 + M1 — código de PhysicsLab **escrito, sin compilar ni ejecutar**. Falta que Stefano ejecute `tools/windows/setup_entorno.cmd` (instalaciones, GitHub, Unity, licencia) y luego `tools/windows/unity_m0_m1.cmd`.

## Código M1 implementado (no compilado todavía)

`UnityProject/Assets/Game/` (asmdefs Runtime, Editor, Tests):

- Jugador: `PlayerMotor` (cápsula dinámica 80 kg, 4,2/2,8/2 m/s, paso evasivo con barrido, agacharse con chequeo de espacio, compostura, energía, tropiezo, caída 2,2 s, levantada con búsqueda de lugar libre, invulnerabilidad 1,5 s) y `PlayerHands` (dos manos independientes, alcance 1,4 m con línea de visión, esfuerzo, Q suelta todo, E interactúa).
- Agarre: `GrabRegistry` (único registro; ocupación por socket; límite global 8; liberación idempotente con motivo) y `GrabSpring` (una sola fuerza `clamp(k·e − c·v, Fmax)`, 420/560 N por mano, 700/950 N por jugador, aviso 1,6 m, corte a 2,2 m tras 0,15 s).
- Tío: `NpcBody` (cuerpo central de 110 kg, equilibrio con torque limitado, cae si se inclina >45°, se levanta si nadie lo agarra), `NpcBrain` (Loiter/Protest/Anchor/Struggle/Windup/Attack/Recover/ForcedRecovery/Escape/Carried/Secured; 4 ataques con preparación ≥0,8 s, dirección bloqueada, un impacto por objetivo; anclaje a la barra liberable manteniendo E 0,9 s; presupuesto de resistencia y apertura forzada) y `NpcPresentation` (color, pose e indicador de cada ataque).
- Carrito con asa para dos manos y correa (E); el tío puede intentar soltarse tras una señal. `ExtractionZone` (torso, pelvis y hombros dentro durante 1,5 s). `RoundDirector` (dificultad, N, temporizador, reinicio limpio, prueba de 10 reinicios).
- Depuración: HUD IMGUI (Tab), ataques forzados, 0/1/2/4 jugadores virtuales que tiran, sliders k/c/equilibrio, derribarse, simular desconexión.
- Editor: `ProjectBootstrap` (instala Input System, Test Framework y uGUI con la versión que elija Unity), `PhysicsLabBuilder` (genera la escena), `BuildTools` (build Windows; Mono por defecto).
- Pruebas: 9 EditMode (registro y balance) y 4 PlayMode (8 agarres + 10 reinicios sin huérfanos, arrastre, empujón que derriba y recuperación, extracción).

Decisiones y simplificaciones: ver `docs/DECISIONES_IMPLEMENTACION.md` (2026-11-15, M1).

## Ubicación y versionado

- Carpeta de trabajo: `C:\Users\stuff\Downloads\UNCLE_EXTRACTION_REPOSITORIO_Y_CLAUDE_v1\uncle-extraction` (raíz del repositorio; no hay carpetas `uncle-extraction` anidadas).
- Git inicializado localmente en rama `main` con un commit de la entrega completa. **Remoto todavía no vinculado**: el repositorio privado `Stefano936/uncle-extraction` ya tiene un commit con un README inicial y debe unirse con los pasos de la sección "Vincular GitHub".
- Sin Git LFS por ahora: el archivo más grande pesa 19,3 MB (`ATLAS_INTERFAZ.pdf`), todo el contenido ~115 MB. Ver `docs/DECISIONES_IMPLEMENTACION.md`.

## Estado comprobado (2026-11-15)

| Comprobación | Resultado | Evidencia |
| --- | --- | --- |
| Acceso a Google Drive | OK | Carpeta de entrega listada; contiene la entrega ya descomprimida (no hay ZIP en Drive). |
| Acceso a la carpeta local | OK | Lectura/escritura en la carpeta de arriba. |
| Integridad local | OK | 214/214 archivos de `ENTREGA_SHA256.json` presentes con SHA-256 idéntico; único extra: el propio manifiesto. |
| Acceso a GitHub | BLOQUEADO | Ni el entorno en la nube ni la VM local tienen credenciales/ruta al repo privado (403). No se pudo clonar ni comparar el remoto. |
| Validador de catálogos (`preproduction/uncle_extraction/tools/validate_spec.py`) | OK | 11 estados, 60 líneas de diálogo, 79 assets, 48 tareas, 72 casos QA. No es prueba de juego. |
| Unity instalado | NO DETECTADO | No hay `Unity`/`Unity Hub` en `C:\Program Files` ni carpeta `UnityHub` en AppData. Sí hay Git para Windows, .NET, JetBrains, VS Code. |
| GitHub CLI | NO DETECTADO | Sin carpeta `GitHub CLI` en Program Files. |
| Acceso de Claude a la PC (Cowork) | PARCIAL | Lectura/escritura de esta carpeta desde una VM Linux; sin shell de Windows. Control de pantalla disponible pero sin escritura en terminales ni UAC. Por eso las instalaciones van en scripts que ejecuta Stefano. |

Las verificaciones de juego siguen sin ejecutarse:

| Verificación | Estado |
| --- | --- |
| Importación en Unity / compilación C# | NOT_RUN — código escrito; solo revisión manual (sin compilador disponible en la sesión). |
| Laboratorio en Play / build Windows | NOT_RUN — pendiente M1. |
| Host y cliente / internet | NOT_RUN — pendiente M2 / M4. |
| Playtest de diversión | NOT_RUN. |

## Faltantes y contradicciones detectadas

- **Unity 6.3 LTS no instalado** (bloquea M0). Ver `DEPENDENCIES.md`.
- `preproduction/uncle_extraction/data/asset_manifest.csv` marca todos los modelos como `pending/TBD`, pero el kit 3D entregó prototipos de 8 de ellos (bar shell, barra, puerta, camioneta, banqueta, mesa, carrito, tío). El README del kit pide registrar «prototipo entregado / producción pendiente». No se editó la fuente original; actualizar el estado al importar en Unity.
- Sin modelo todavía (usar primitivas en PhysicsLab): cuerpo compartido del jugador, brazos en primera persona, marco de puerta, vaso señuelo, vereda. Audio: todo pendiente (P1, no bloquea M1).
- Texturas: solo Base Color, 1254 px (los prompts pedían 2048). Sin normal/roughness/AO.
- Todo el arte entregado es **prototipo/placeholder**; el arte conceptual (`preproduction/uncle_extraction/art/`) es solo referencia de dirección final. Los GLB no son importables por Unity sin paquete extra: usar FBX.
- El kit no trae ragdoll funcional ni triggers: `Extraction_Volume` y sockets son marcadores.
- `00_EMPEZAR_AQUI.md` y `docs/GUIA_GITHUB.md` suponen clonar primero y copiar; en la práctica se inicializó Git en la carpeta extraída. Para vincular, seguir la sección siguiente.
- `ENTREGA_SHA256.json` es una foto del empaquetado: `.gitignore`, `.gitattributes`, `PROGRESS.md`, `DEPENDENCIES.md` y `docs/DECISIONES_IMPLEMENTACION.md` cambiaron después a propósito.

## Vincular GitHub (lo hace Stefano, una sola vez)

En PowerShell o Git Bash, dentro de la carpeta de trabajo:

```bash
git remote add origin https://github.com/Stefano936/uncle-extraction.git
git fetch origin
git merge origin/main --allow-unrelated-histories -X ours -m "Merge initial GitHub README"
git push -u origin main
```

`-X ours` conserva el README ampliado local si choca con el README inicial. Revisar `git log --oneline` antes del push. Sin force push. Si el autor del commit inicial no es el deseado: `git commit --amend --reset-author --no-edit` antes del push.

## Próxima acción exacta

1. Stefano: doble clic en `tools\windows\setup_entorno.cmd` (instala GitHub CLI, Unity Hub, Unity CLI y Unity 6.3 LTS + IL2CPP; inicia sesión en GitHub y Unity en el navegador; hace `fetch` sin mezclar). Activar la licencia Personal en Unity Hub.
2. Claude: leer `.local-setup/logs/`, integrar `origin/main` resolviendo conflictos uno por uno, commit. Stefano: `tools\windows\subir_cambios.cmd` (push sin force).
3. Stefano: `tools\windows\unity_m0_m1.cmd`. Claude: leer `.local-setup/logs/unity/`, corregir errores de compilación/pruebas, fijar versiones en `DEPENDENCIES.md`, versionar `ProjectSettings/` y `Packages/`.
4. Probar a mano: abrir `UnityProject` en Unity Hub, escena `Assets/Game/Scenes/PhysicsLab.unity`, Play.

Si Cowork no alcanza (por ejemplo, para iterar rápido sobre errores de compilación), abrir esta misma carpeta en Claude Code y pedir: «Leé CLAUDE.md y PROGRESS.md y continuá desde la Próxima acción exacta».

## Al cerrar una sesión

Anotar comportamiento implementado, archivos, commit, comandos y pruebas ejecutadas, resultados, bloqueos y siguiente acción.
