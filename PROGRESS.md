# Estado del proyecto

Última actualización: 2026-11-15 (sesión de preparación en Cowork).

## Hito actual

M0 — entorno y creación de `UnityProject/`. Bloqueado por falta de Unity en la PC de desarrollo (ver abajo). Luego M1 — PhysicsLab.

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

Las verificaciones de juego siguen sin ejecutarse:

| Verificación | Estado |
| --- | --- |
| Importación en Unity / compilación C# | NOT_RUN — sin editor ni proyecto. |
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

1. Instalar Unity Hub y Unity 6.3 LTS (parche más reciente) con el módulo **Windows Build Support (IL2CPP)**; iniciar sesión y activar la licencia Personal.
2. Vincular GitHub con los comandos de arriba.
3. Abrir esta carpeta en Claude Code y ejecutar M0 + M1 según `CLAUDE.md` (mensaje en `00_EMPEZAR_AQUI.md`).

## Al cerrar una sesión

Anotar comportamiento implementado, archivos, commit, comandos y pruebas ejecutadas, resultados, bloqueos y siguiente acción.
