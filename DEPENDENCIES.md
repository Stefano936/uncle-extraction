# Entorno y dependencias

## Vigente (comprobado en la PC de desarrollo)

| Componente | Versión | Evidencia |
| --- | --- | --- |
| SO / GPU | Windows 11 Home 10.0.26200 x64 / RTX 3070 Ti Laptop | Registros de la build. |
| Unity Editor | **6000.3.28f1** (Unity 6.3 LTS) en `C:\Program Files\Unity\Hub\Editor\6000.3.28f1` | `UnityProject/ProjectSettings/ProjectVersion.txt`. |
| Módulos | Windows Mono (incluido) y Windows Build Support IL2CPP | IL2CPP sin probar: falta Visual Studio con C++. |
| Unity Hub / Unity CLI | 3.22.6 / 1.0.0 (winget `Unity.UnityHub`, `Unity.CLI`) | Instalados con winget. |
| Licencia | Unity Personal activa | `unity license`. |
| Input System | 1.20.1 (manejo de entrada: ambos) | `UnityProject/Packages/manifest.json`. |
| Test Framework | 1.6.0 (NUnit 2.0.5) | Ídem. |
| uGUI | 2.0.0 | Ídem. |
| Netcode for GameObjects | 2.14.0 | Ídem. |
| Unity Transport | 2.7.5 | Ídem. |
| Multiplayer Tools | 2.2.12 (simulador de red en editor y builds de desarrollo) | Ídem. |
| Render | URP `com.unity.render-pipelines.universal` 17.3.0 (incluido en el editor 6000.3.28f1; trae core 17.3.0, shadergraph 17.3.0 y universal-config 17.0.3) | Asset y renderer creados por código (`UrpSetup`, `Assets/Game/Settings/`), sombras a 40 m, MSAA 2x. Verificado en la build con capturas (`-ueShot`). |
| Multiplayer Services (Sessions/Relay) | Sin instalar | Requiere un proyecto de Unity Cloud (bloqueo de cuenta). |
| Steamworks.NET | Sin instalar | Requiere cuenta de Steamworks y App ID. |

`packages-lock.json` fija las dependencias transitivas. No actualizar el editor ni los paquetes sin registrarlo aquí.

## Histórico

- Antes de M0, en la sesión de Cowork, no había Unity instalado y no se había comprobado ninguna versión. Las propuestas originales (URP, NGO, MPS, Steamworks.NET) están en la especificación técnica de `preproduction/`.
- El 2026-11-15 se instalaron Unity Hub, Unity CLI y el editor con winget y se fijaron los paquetes base. NGO y Transport se agregaron en M2, y Multiplayer Tools en M3.
