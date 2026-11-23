# Sesión 20 · arte del tío, materiales y auditoría de la candidata

**Referencia:** código de `54b5246bc9e7f3d793ea6ec9faeb0a1c74840eef` (build de jugadores y ZIP `UncleExtraction_Demo_54b5246_Windows_x64.zip`, SHA-256 `9167da7f…bbe`, verificado en `Documents\Codex\…\outputs`). `main` estaba en `456a4fc1ea5127bc7cc6c71d02b0208ae624b68f`; entre ambos solo cambiaron documentación y un script (`git diff 54b5246 456a4fc -- UnityProject` vacío), así que las capturas de referencia salen de una build QA de `456a4fc` (`Builds/UncleExtraction_Ref_456a4fc`).
**Build para jugar:** `ac1934be071b7ea9c1169b2a26f6d2b3f1bf2e45` (rama `claude/s20-playtest`, integra los tres PR). La regresión completa corrió sobre `b40897d`; `ac1934b` solo agrega la corrección de la placa del rótulo (`GrabFeedback.cs`), con sus pruebas repetidas (ver abajo). No incluye el PR #5 de Mateo (mando para códigos Relay), fusionado en `main` después del corte.

## Qué vas a notar

- **El tío tiene cara.** Cabeza nueva generada en código: cráneo con papada y mofletes, nariz roja de borracho, orejas, ojos con iris y párpados, cejas pobladas, bigote de morsa, pelo canoso en herradura y peinado de costado, boca con mandíbula. Sigue al jugador más cercano con la mirada.
- **Cambia de cara según lo que hace:** borracho (párpados pesados, parpadeo lento), resistiendo (entrecierra los ojos, frunce las cejas), **antes de pegar** (abre grandes los ojos, levanta las cejas y abre la boca: se ve venir), pegando (grita), aturdido (ojos que giran), distraído o con la campana (ilusión), asegurado en la camioneta (se duerme).
- **Piso más calmo:** el damero sigue, con la mitad de contraste; ya no compite con el tío. Paredes un poco más claras.
- **Menú:** el fondo es una captura del juego con el tío de frente desde atrás de la barra.
- **Rótulo del punto de agarre (HUD Detallado):** placa oscura detrás del texto; se lee sobre la ropa iluminada.
- **Si el anfitrión rechaza tu agarre** (ocupado o fuera de alcance), además del texto suena un golpe seco y bajo.

Comparativas con las mismas posiciones de cámara, FOV, 1920 × 1080 y calidad (`tools/windows/art_shots.ps1`):
[Alta](capturas/sesion20/antes_despues_alta.jpg) · [Baja](capturas/sesion20/antes_despues_baja.jpg) · [Menú Alta](capturas/sesion20/menu_antes_despues_alta.jpg) · [Menú Baja](capturas/sesion20/menu_antes_despues_baja.jpg) · [Expresiones (reposo, tres cuartos, anticipación, perfil)](capturas/sesion20/tio_expresiones_v3.jpg) · [Fondo del menú nuevo](capturas/sesion20/menu_fondo_nuevo.jpg). Las tiras finales salen de la build QA de `b40897d`.

## Dirección artística (decisión)

- **Silueta:** se probaron dos direcciones en el mismo generador: *Fiesta* (calvo con peinado de costado, herradura canosa y el gorrito de cumpleaños que ya era su marca) y *Boina* (gorra plana). Quedó **Fiesta**: el gorro es lo único que se reconoce desde la otra punta del salón y cuenta el chiste («la fiesta terminó»). *Boina* queda disponible con `-ueHeadVariant Boina` (build QA) para comparar.
- **Cara para leer a distancia de agarre:** rasgos grandes y de alto contraste (nariz roja, bigote oscuro, ojos claros con iris marrón), formas redondeadas, sin texturas finas que parpadeen.
- **Ambiente:** se conserva S19 (madera, baldosa, revoque, neón); se bajó lo que se veía demasiado fuerte en partida (contraste del piso) en vez de sumar detalle.

## Método (reproducible, sin modelos externos)

- No hay Blender en la PC. La cabeza se genera en `Scripts/AI/UncleHead.cs` con parámetros editables (`UncleHead.Style`: proporciones, papada, menton, mofletes, tamaño de nariz y ojos, bigote, colores). Sin aleatoriedad: la misma malla en cada arranque y en cada par.
- `ArticulatedUncleBuilder` mide la cabeza del kit (vértices con peso del hueso `head`, solo legible en el editor) y guarda el ajuste en el componente; en ejecución la cabeza del kit se colapsa dentro de la nueva.
- Texturas del bar: mismo generador S19 (`tools/art/generate_s19_surfaces.py`, semillas fijas); cambian solo los colores base del piso y del revoque. Mismos archivos y GUID; los mapas de normales salieron idénticos.
- Recursos propios: no se usó ningún modelo, textura ni sonido de terceros en esta sesión.

## Contrato con el juego (UE-14/UE-15)

| Comprobación | Resultado |
| --- | --- |
| Colliders, cuerpos, puntos de agarre en la cabeza nueva | **0 / 0 / 0** (`Session20HeadTests`) |
| Puntos de agarre del tío | los mismos 8: tobillos, cinturón (frente y atrás), antebrazos, hombros |
| Escenas regeneradas contra `main` | tipos de componente iguales salvo **+1 `UncleHead`** en `Bar_LastRound` y `PhysicsLab_Articulated`; rigidbodies, colliders, joints, luces y scripts idénticos |
| Expresión al preparar un golpe | pasa de «borracho» a «anticipa» y abre los ojos (prueba) |
| Cara anterior para comparar | `-ueOldFace` (build QA) |

## Estados de agarre (UE-12)

| Estado | Señal | Dónde | Prueba |
| --- | --- | --- | --- |
| Apuntado válido | anillo sólido + rótulo con botón (Detallado) / pista cerca de la mira (Contextual) | `GrabFeedback` | `RealInputTests`, auditoría UI |
| Intento | sonido `grab_try`, la mano se cierra a medias | `PlayerHands`, `HandRig` | `RealInputTests` |
| Confirmación (anfitrión) | sonido `grab` una vez por mano, mano cerrada, línea de tensión | `GameAudio`, `HandRig` | `RealInputTests`, `race` |
| Tensión alta | línea más gruesa que parpadea, `strain` | `GrabFeedback`, `GameAudio` | — |
| Rechazo: ocupado / fuera de alcance | texto + **sonido seco nuevo** (`release_neutral` bajo), una vez por respuesta | `PlayerHands.RecordFeedback` | `race` (rechazo `SocketBusy`) |
| Ocupado por otro (antes de intentar) | anillo grueso + «lo tiene X» | `GrabFeedback` | — |
| Soltada pedida / pérdida | `release` vs `slip` distintos | `GameAudio` | `RealInputTests` |
| Causa incierta | `release_neutral` | `GameAudio` | — |
| Disputa | un solo titular, el otro recibe rechazo | anfitrión | `race`, `race_natural` |

## Auditoría de lo fusionado

| Hallazgo | Severidad | Evidencia / reproducción | Estado |
| --- | --- | --- | --- |
| Regenerar escenas (`PhysicsLabBuilder.BuildFromCommandLine`) **pisa los catálogos de idioma** con la fuente del kit: borra la línea de la campana (DLG_061) y el mensaje completo de versión distinta | Alta (cualquier regeneración futura rompía textos del juego) | `UiKitImporter` copiaba `localization_en_es.json` y `dialogue_en_es.csv` si diferían. Reproducido al regenerar en esta sesión | **Corregido** en `claude/s20-auditoria` (copia solo si falta) + prueba `Catalogs_KeepGameOwnedLines` |
| Rótulo 3D de Detallado con poco contraste sobre ropa iluminada (registrado en 54b5246) | Media | capturas de la auditoría 720p/150 % | Primer intento **FAIL** en `b40897d`: el cuerpo del tío tapaba la placa ([captura](capturas/sesion20/rotulo_detallado_FAIL_b40897d.png)). **Corregido** en `ac1934b` con el sombreador del texto ([captura](capturas/sesion20/rotulo_detallado_PASS_ac1934b.png)) |
| El rechazo del anfitrión no se oía | Media (UE-12) | intento sobre un punto ocupado: solo texto | **Corregido** (sonido seco, una vez) |
| Regenerar escenas reescribe `_MainTex`/`_Color` de 7 materiales `Finish` | Baja (ruido de diff, sin efecto visual) | diff de la regeneración | Aceptado (queda estable tras regenerar) |
| PR #2 materiales S19: colliders, bisagra, luces | — | comparación estructural de escena | Sin cambios físicos |
| Cierre nativo intermitente al terminar `latejoin` (0xc0000005 en 54b5246) | Abierto | `latejoin` PASS en `b40897d`, sin rastro de crash en los registros (una corrida; no prueba la causa) | Guion: repetir `latejoin` ×10 y guardar `exit code` y log por proceso |

### Ramas de Mateo (sin PR al revisar, 2026-11-22)

| Rama | Revisión |
| --- | --- |
| `mateo/ue-13-cierre` | Chirrido al empezar a cerrar y remate `win_sting` como aviso. Correcto; `Played` es por escena. Sin solape con esta sesión (no toqué `GameAudio`). |
| `mateo/ue-19-relay` | Token de intento: una respuesta tardía de Relay de un intento cancelado se descarta antes de configurar el transporte; el código de sala ya no queda en el log. Correcto a lectura; falta probar cancelar → reintentar con Relay real. |
| `mateo/trello-mando`, `ue-52-verificacion`, `demo-material-y-entrega`, `revision-s18` | Fuera del alcance de arte; sin conflicto de archivos con esta sesión. |

## Pruebas

Regresión completa sobre `b40897d`; repeticiones sobre `ac1934b` (corrección de la placa) en las últimas filas.

| Prueba | Topología | Resultado |
| --- | --- | --- |
| EditMode completo | editor | **76 PASS**, 1 omitida (77) |
| PlayMode completo con control de assets | editor | **175 PASS**, 1 omitida (176; la candidata tenía 173 + 2 nuevas de la cabeza). Archivos versionados sin cambios |
| Ataques legibles (manotazo, empujón, patada, sacudida) | editor, bar y dos laboratorios | 17/17 en cada uno (`AttackSignalTests*`) |
| Entrada real (manos independientes, Q, desenganche con progreso) | editor | 34/34 (`RealInputTests`) |
| Red localhost, 20 escenarios | 2–4 procesos en una PC, build QA y dev `b40897d` | **20/20 PASS** (incluye disputa `race` con un titular y rechazo atado al intento, `race_natural`, `latejoin` sin cierre anómalo, `compat` contra la demo s16) |
| Capturas comparables Alta/Baja | build QA `b40897d` vs QA `456a4fc` (= código 54b5246) | tiras en `capturas/sesion20/` |
| Auditoría de interfaz ES/EN, dos perfiles de HUD | build QA `b40897d` | 15/15 ES y 15/15 EN de disposición, pero **FAIL visual**: la placa del rótulo 3D no se veía (corregido en `ac1934b`) |
| **`ac1934b`** · PlayMode dirigido (entrada real, HUD, guía, prioridades, cabeza, S17) | editor | **45/45**, assets sin cambios |
| **`ac1934b`** · Auditoría de interfaz ES/EN (5 formatos × 100/125/150 %, dos perfiles de HUD) | build QA | **30/30**. Primera corrida en inglés: 14 PASS + 1 NOT_RUN (1080p 125 %): el proceso **se cerró al arrancar dentro del driver NVIDIA D3D11** (`nvwgf2umx`, `OpenAdapter10`) antes de cargar código del juego. Repetición 15/15, sin cierres. Log original en `.local-setup/qa/s20/` |
| **`ac1934b`** · Red localhost `coop`, `race`, `repeat`, `party4` | 2–4 procesos en una PC, QA y dev `ac1934b` | **4/4 PASS** |
| **`ac1934b`** · ZIP de jugadores extraído en otra carpeta | build de jugadores | **PASS**: menú en 19 s sin excepciones; con argumentos de QA queda en el menú sin herramientas |
| **`ac1934b`** · Ronda con el ejecutable de jugadores | anfitrión QA (bot) + invitado de jugadores | Conexión y sala **PASS** sin excepciones; la ronda **no arranca sin una persona**: la sala espera LISTO y la build de jugadores no tiene autopiloto (por diseño). Ronda completa: **NOT_RUN** (la juega Stefano) |
| LAN, Relay entre redes, PC sin Unity, mando físico, grupos nuevos | — | **NOT_RUN** (requieren otra PC o personas) |

## Cómo reproducir

```
powershell -ExecutionPolicy Bypass -File tools\windows\art_shots.ps1 -Build Builds\UncleExtraction -Out .local-setup\shots\x -Quality high
python tools\art\generate_s19_surfaces.py
```
Escena: `PhysicsLabBuilder.BuildFromCommandLine` (mide y guarda el ajuste de la cabeza). Comparar con la cara anterior: `UncleExtraction.exe -ueOldFace` (build QA).
