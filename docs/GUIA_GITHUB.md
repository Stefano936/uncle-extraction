# Cómo agregar esta entrega a GitHub

Repositorio: https://github.com/Stefano936/uncle-extraction

Al revisarlo para preparar esta entrega era privado y solo contenía un README. Esta entrega no cambia el repositorio remoto por sí sola. Descargar o extraer un ZIP tampoco lo sube: hay que copiar los archivos a una copia local del repositorio, hacer commit y push.

## Opción visual: GitHub Desktop

1. Clonar el repositorio existente `Stefano936/uncle-extraction` en GitHub Desktop, usando tu cuenta. No crear otro repositorio.
2. Descargar y extraer `UNCLE_EXTRACTION_REPOSITORIO_Y_CLAUDE_v1.zip` fuera de la copia local.
3. Abrir la carpeta `uncle-extraction` que está dentro del ZIP. Copiar **todo su contenido**, incluidos `.gitignore` y `.gitattributes`, dentro de la carpeta local clonada. No copiar la carpeta entera como subcarpeta. No modificar ni copiar una carpeta `.git`.
4. El README ampliado sustituye el README inicial. Si ya agregaste avances desde la revisión, comparar archivos y conservarlos antes de reemplazar nada.
5. En GitHub Desktop revisar los cambios: deben aparecer documentación, modelos, texturas, UI y las instrucciones. No deberían aparecer los ZIP de transporte, cachés de Unity ni credenciales.
6. Hacer commit con el mensaje `Add preproduction kits and Claude handoff` y luego push al remoto. Para este repositorio inicial puede ser sobre `main`; si ya hay trabajo de desarrollo, usar una rama de preparación y revisarla antes de integrarla.
7. Abrir GitHub y comprobar que `CLAUDE.md`, `PROGRESS.md` y `preproduction/` estén en la raíz y que los modelos/texturas existan.
8. Abrir la carpeta local en VS Code. Iniciar Claude Code apuntando a esa misma carpeta y usar el mensaje de `00_EMPEZAR_AQUI.md`.

## Alternativa por terminal

Desde la carpeta en la que quieras guardar el proyecto:

```bash
git clone https://github.com/Stefano936/uncle-extraction.git
cd uncle-extraction
```

Copiar ahora el contenido del ZIP extraído a esta carpeta, como se explica arriba. Después:

```bash
git status --short
git diff -- README.md
git add .
git diff --cached --stat
git commit -m "Add preproduction kits and Claude handoff"
git push origin main
```

Revisar lo que se va a incluir antes del commit. Los comandos suponen que estás en la copia recién clonada, en `main`, y que esta primera carga es el único cambio local. Si ya hay otros cambios, seleccionar los archivos de esta entrega en vez de usar `git add .` indiscriminadamente. No usar force push ni borrar trabajo para resolver un error de sincronización. La autenticación se hace con tu cuenta, sin pegar claves en los archivos.

## Qué se agrega y qué se genera después

Se agrega ahora el contenido descomprimido de diseño y kits, el prompt maestro, la entrada breve, el estado y la configuración de Git. No se agrega una segunda copia de los ZIP originales.

`UnityProject/` no existe todavía: Claude debe crearlo en M0. Después se versionarán `Assets/` y sus `.meta`, `Packages/` y `ProjectSettings/`. Los ejecutables y cachés locales quedan excluidos por el `.gitignore`.

Este primer import conserva los binarios en Git normal. No hay filtros LFS ni archivos que dependan de una descarga LFS para abrirse. Si incorporás mucho arte nuevo o versiones frecuentes de Blender y texturas, conviene evaluar LFS antes de aumentar el historial.

## Mensaje para Claude

> Trabajá sobre este repositorio. Leé CLAUDE.md y PROGRESS.md, inspeccioná el estado real y continuá con M0 y M1. Consultá el prompt maestro y las fuentes del hito según el mapa de lectura. Implementá archivos reales y ejecutá las comprobaciones disponibles. Actualizá PROGRESS.md y distinguí lo implementado de lo probado. Si falta acceso a Unity, indicá el paso concreto necesario y avanzá con las tareas independientes.
