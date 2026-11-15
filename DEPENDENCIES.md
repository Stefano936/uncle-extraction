# Entorno y dependencias

Estas son propuestas del diseño, no instalaciones comprobadas.

| Componente | Propuesta | Estado |
| --- | --- | --- |
| Editor | Unity 6.3 LTS; comprobar disponibilidad y parche compatible en M0. | Sin fijar. |
| Lenguaje | C# compatible con el editor seleccionado. | Sin comprobar. |
| Render | URP compatible con el editor. | Sin fijar. |
| Entrada | Unity Input System. | Sin fijar. |
| Red | Netcode for GameObjects y Unity Transport. | Sin fijar. |
| Internet | Multiplayer Services Sessions / Relay, durante M4. | Sin configurar. |
| Steam | Adaptador compatible; Steamworks.NET es una opción del diseño. | Posterior al núcleo. |
| Arte | FBX preferente; GLB y Blender como fuentes/referencia. | Recursos entregados. |

En M0 completar sistema operativo, ruta del editor, versión exacta, licencia comprobada sin copiar datos sensibles, módulos de build y versiones de paquetes.

Versionar `UnityProject/ProjectSettings/ProjectVersion.txt`, `UnityProject/Packages/manifest.json` y `UnityProject/Packages/packages-lock.json` cuando existan. Registrar comandos de apertura, pruebas y build después de ejecutarlos con éxito. No inventar una versión o comando comprobado por completar esta tabla.

## Entorno detectado (2026-11-15, sin abrir el editor)

- PC `msi`, Windows x64. Detectados por nombre de carpeta: Git para Windows, .NET (`dotnet`), JetBrains, VS Code, Docker, WSL.
- **Unity Hub / Editor: no detectados** en `C:\Program Files` ni en AppData. Puede estar en otro disco; confirmarlo al iniciar M0.
- Git LFS: no disponible en la VM de Cowork; no comprobado en Windows. No se usa todavía.
