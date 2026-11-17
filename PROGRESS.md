# Estado del proyecto

Última actualización: 2026-11-16, sesión 9. Detalle de sesiones anteriores (sin reescribir, con todos los FAIL): `docs/HISTORIAL.md`.

## Estado vigente

**Build para probar** (local, no versionada): `Builds/UncleExtraction/UncleExtraction.exe`, con `VERSION.txt` (commit, cambios sin commitear, fecha, tipo), `LEEME.txt` (cómo jugar, controles) y `PRUEBA_HUMANA.txt` (guion corto de prueba con planilla). `Builds/UncleExtraction_Dev/` es la de desarrollo (simulador de red). Build de referencia anterior: `378fa91`.

**Qué se puede jugar:** Bar_LastRound solo o en cooperativo de 2 a 4 (red local por IP; internet por Relay con código cuando el proyecto esté vinculado). Tío articulado de 11 cuerpos con poses de ataque; carrito y camioneta con piezas del kit; vaso señuelo, ayuda para levantarse, extracción con puertas. Interfaz del kit (menú, sala, HUD, pausa, opciones, resultados, errores), EN/es_UY, controles reasignables, opciones guardadas, mando en menús y juego. Sonido sintetizado y subtítulos del tío; sin voz ni música.

**Niveles de evidencia:** PM = PlayMode en el editor; EM = EditMode; AUTO = partida completa con entradas de jugador simuladas; NET = procesos de la build en **localhost** (no LAN ni internet); VIS = auditoría y capturas reales de la build. **Ninguna fila tiene prueba humana.**

### Resultados de la sesión 9 (por commit y build)

RESULTADOS_S9

### Pendientes reales (en orden)

1. **Prueba con personas** (`docs/PRUEBA_HUMANA.md`): objetivo sin explicación, controles, ataques, carrito y atascos, camioneta, perder/reintentar, sala con amigos. Registrar en `docs/OBSERVACIONES_PRUEBAS.md`. No bloquea el desarrollo.
2. **Internet:** vincular Unity Cloud (ver «Bloqueado») y verificar en este orden: Relay en una PC (`net_smoke.ps1 -Scenario relay`), dos equipos en la misma red por Relay, dos equipos en **redes distintas**.
3. **LAN real** con dos PC (conexión directa).
4. **Revisión humana de la interfaz:** legibilidad y jerarquía a 720p, 21:9 y texto al 150 % (la auditoría automática solo detecta textos fuera de pantalla, cortados o encimados); escribir el código de sala con mando (no hay teclado en pantalla).
5. **Audio de producción** (voz del tío, música, efectos grabados): necesita fuentes con licencia o actor; no bloquea la primera prueba con amigos.
6. Fallos intermitentes del autopiloto (ver abajo): revisar solo cuando cambie su código.
7. Steam e IL2CPP (bloqueados).

### Fallos intermitentes conocidos (se mantienen visibles)

- **Bot del modo carrito:** falló en 2 de 4 regresiones completas de la sesión 8 (asa contra la pared, corregido; no llegó a cargar en el laboratorio articulado, abierto). Es el punto más inestable de las pruebas automáticas, no una regla del juego.
- **`Uncle_Escapes_BackInsideThroughTheDoor` (articulado):** 1 de 3 corridas en la sesión 8 terminó con el tío en el umbral (z = −1,50 con la condición > −1,5) a los 15 s. ESCAPE_S9

## Bloqueado por cuentas, equipos o participantes

| Bloqueo | Paso mínimo |
| --- | --- |
| Internet (Relay) | **Tu cuenta de Unity ya tiene la organización, pero ningún proyecto de Unity Cloud.** Opción A (terminal, en la carpeta del repo): `unity projects link cloud UnityProject --cloud-org <tu organización>` (la única de la cuenta) crea el proyecto de Cloud y escribe el vínculo en `ProjectSettings.asset`; modifica tu cuenta, por eso no lo ejecuté sin tu confirmación. Opción B: Unity Hub → abrir el proyecto → Edit → Project Settings → Services → elegir la organización → *Create project ID*. Después, en cloud.unity.com → el proyecto → Multiplayer → **Relay** → habilitarlo (plan gratuito). Con eso, regenerar la build y correr `net_smoke.ps1 -Scenario relay`. |
| LAN | Otra PC en la misma red: `UncleExtraction.exe` → Jugar con amigos → Red local (IP) → Crear partida (aceptar el firewall) en una; Unirse por IP en la otra. |
| Prueba con personas | Jugar con `PRUEBA_HUMANA.txt`. No bloquea el desarrollo. |
| Steam | Cuenta de Steamworks, pago de Steam Direct y App ID. |
| IL2CPP | Visual Studio 2022 con «Desarrollo de juegos con C++». |

## Próxima acción exacta

PROXIMA_S9
