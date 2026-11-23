# Sesión 21 · tío de cuerpo completo, luz con volumen y mangas del jugador

**Base:** `main` en `4f7802d` (código de juego = `a793e8c`, PR #5–#9). **Referencia jugable conservada:** `Builds/UncleExtraction_a793e8c_Windows_x64.zip` (SHA-256 `f7b7d35e…a0`) y la build QA `Builds/UncleExtraction_Ref_a793e8c/`. **Corte integrado S21:** `5da0616` (rama `claude/s21-playtest`); builds nuevas en `Builds/S21/`.

## Defectos de la referencia, por cuánto se notan jugando

1. **El cuerpo del tío** (lo más visible): torso y camisa como placas, brazos de tubo, bermuda como caja, zapatos como ladrillos; la cabeza redonda de S20 apoyada sobre ese cuerpo sin continuidad.
2. **Antebrazos del jugador**: cápsulas casi negras, siempre en pantalla.
3. **Carrito y camioneta**: tubos finos, caja con interior plano; calle con fachadas planas.
4. **Volumen y contacto**: sin oclusión ambiental, una sola cascada de sombras; muebles y personajes «flotan».
5. Piso todavía con mucho patrón; paredes y techo planos y oscuros.

Capturas de referencia con las mismas cámaras: `tools/windows/art_shots.ps1` (16 vistas: bar, aproximación, puerta, rampa, carga de la camioneta, carrito, vereda, tío de frente, perfil, espalda y primeros planos). Clip de referencia: `tools/windows/clip.ps1` (cuadros de juego real a WebP).

## Qué cambió

### Tío de cuerpo completo (UE-14) · PR personaje

- **Modelado en Blender 5.3** por script, con fuente editable: `tools/art/blender/build_uncle_s21.py` → `art_source/uncle_s21/uncle_s21.blend` y `.fbx`. Instalado con permiso de Stefano (winget, Blender Foundation).
- **Cuerpo continuo** de cuello a zapatos (modificador Piel + subdivisión): panza, cadera ancha, hombros con trapecio, brazos y piernas con volumen, rodillas, tobillos, pies; **manos con cinco dedos** (palma, cuatro dedos en tres tramos, pulgar).
- **Ropa** por zonas de material: camisa abierta de manga corta con arrugas sobre la panza, camiseta en la abertura, bermuda con pliegues, medias, zapatos. **Piezas con volumen** ajustadas a la superficie: cinturón con hebilla (a la altura de los puntos de agarre «cinturón»), ruedo de la camisa, tapetas, cuello, puños de manga, borde de las medias y suelas.
- **Mismo esqueleto del kit** (mismos nombres y pose de reposo): Unity monta la malla nueva sobre los huesos del rig físico existente (`ArticulatedUncleBuilder.AttachBodyS21`). **No cambian** colliders, joints, masas, los 8 puntos de agarre, los clips de ataque ni `FaceSign`. Pesos por región (ninguna mano toma peso de un muslo).
- **Cabeza:** la cabeza de S20 (cara, expresiones, bigote, gorrito) se conserva y se ajusta a una esfera guía del modelo nuevo: radio 0,29 → **0,21** (proporciones más creíbles, sigue siendo exagerada).
- Materiales propios (`Uncle_S21_*`) con la trama de tela a escala de la ropa; cuero para cinturón y zapatos, metal para la hebilla.
- `-ueOldBody` (build QA) vuelve al cuerpo del kit para comparar.

### Luz y sombras (UE-16) · PR render

- **Oclusión ambiental en pantalla (SSAO)** en el renderer URP, agregada por `UrpSetup` (configuración reproducible). En calidad Baja se apaga en ejecución.
- **Sombras:** dos cascadas, sombras suaves en calidad alta, mapas de 4096 para las luces adicionales (focos de la barra, farol): patas, barra, tío y carrito apoyan en el piso.

### Primera persona (UE-12) · PR render

- **Mangas del jugador:** campera de jean gastada con trama y relieve, y manga arremangada (antes una cápsula casi negra).

## Lo que NO se hizo en S21 (sigue pendiente, en orden de impacto)

- **Animación y movimiento del tío (UE-15):** respiración, cambio de apoyo, anticipación de la resistencia y pérdida de equilibrio. La pose de reposo sigue siendo la del rig físico (brazos en «A»): de lejos se ve rígido.
- **Detalle del modelo:** los bordes entre materiales (camiseta/camisa) se ven escalonados de cerca; el cuello de la camisa queda en parte dentro del cuello; el pelo de la herradura sigue siendo de mechones redondos.
- **Manos del jugador por superficie** (cinturón, brazo, baranda, manija, carrito) y dedos articulados.
- **Entorno:** barra y muebles con más perfil, puerta con vidrio y herrajes nuevos, fachada y fondo de la calle, carrito con tubos y ruedas trabajados, carrocería e interior de carga de la camioneta. **Sin cambios en S21.**
- **Remate del cierre de la camioneta (UE-13):** Mateo tiene el audio en `mateo/ue-13-cierre` (sin PR).
- Sensación de control (aceleración, frenado con carga, selección de puntos): sin cambios; las variantes S18 siguen aparte.

## Cómo reproducir

```
"C:\Program Files\Blender Foundation\Blender 5.3\blender.exe" -b --python tools\art\blender\build_uncle_s21.py -- UnityProject\Assets\Game\Art\Kit3D\uncle_prototype.fbx art_source\uncle_s21\uncle_s21.fbx art_source\uncle_s21\uncle_s21.blend
copy art_source\uncle_s21\uncle_s21.fbx UnityProject\Assets\Game\Art\Kit3D\uncle_s21.fbx
```
Luego `PhysicsLabBuilder.BuildFromCommandLine`. La regeneración en `5da0616` dio escenas estructuralmente idénticas (mismos tipos de componente, scripts y nombres; solo cambia el orden de los ids).

Licencias: todo lo nuevo es propio (modelo generado por script, texturas propias de sesiones anteriores). Blender es software libre (GPL); el modelo exportado no queda sujeto a esa licencia.

## Verificación del corte `5da0616`

| Prueba | Topología | Resultado |
| --- | --- | --- |
| EditMode completo | editor | **76 PASS**, 1 omitida |
| PlayMode completo | editor | **175 PASS, 2 FAIL**, 1 omitida; archivos versionados sin cambios. Resultado original en `.local-setup/qa/s21_playmode_full_ORIGINAL.xml` |
| ↳ `Narrow_OneGapBarricade_DraggingThroughTheGapWins` | editor, aislada ×6 (3 cuerpo nuevo, 3 `-ueOldBody`) | **6/6 PASS**: falla intermitente del bot en la tanda completa (ya registrada como intermitencia del bot) |
| ↳ `Van_FootInDoorway_IsExplained_AndPlayerTucksItIn` | editor, ×6 A/B + 30 y 60 FPS fijos | **FAIL 8/8**, también con el cuerpo viejo y a FPS fijos. Bisección: pasa en `e2889af` (hito 1) y en `claude/s21-render`; falla desde `71a1105`. Entre ambos commits la física de la escena es **idéntica** (masas, colliders, joints y transforms comparados): solo cambia el **orden de los ids** al regenerar, que cambia el orden en que PhysX crea los cuerpos. Con ese orden, el pie vuelve a quedar en la abertura y tampoco alcanza la maniobra alternativa (entrar por el costado y tirar del hombro). No se debilitó la prueba; queda como fragilidad real del rescate del pie (UE-18) |
| Red localhost, 20 escenarios | 2–4 procesos en una PC, QA y dev `5da0616` | **20/20 PASS** |
| Auditoría de interfaz ES/EN (5 formatos × 100/125/150 %, dos perfiles de HUD) | build QA | **30/30** |
| Regeneración de escenas | editor | estructuralmente idéntica (solo cambia el orden de ids) |

### Rendimiento (banco de vistas fijas, misma PC, 1920 × 1080; GPU no informada por el banco)

| Configuración | S21 `5da0616` | Referencia `a793e8c` |
| --- | --- | --- |
| Alta, sin vsync | 131 fps · p95 11,3 ms · p99 12,6 ms · CPU 3,0 ms · GPU 5,5 ms | 185 fps · p95 5,6 ms · p99 6,0 ms · CPU 2,0 ms · GPU 1,9 ms |
| Alta, con vsync | 112 fps · p99 13,9 ms | 125 fps · p99 8,3 ms |
| Baja, sin vsync | 173 fps · p99 7,1 ms | — |
| 2560 × 1440 | **no medible**: la pantalla es 1080p y la ventana dio los mismos números que a 1080p | — |

La imagen nueva cuesta unos **3,6 ms de GPU por cuadro** en Alta (SSAO y sombras más finas). Sigue por encima de 110 fps, pero con vsync en una pantalla de 120 Hz algunos cuadros se pasan del intervalo. Primer ajuste si molesta: mapas de luces adicionales a 2048.
