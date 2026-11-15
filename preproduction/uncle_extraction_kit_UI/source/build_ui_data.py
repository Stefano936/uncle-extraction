"""Write UI design catalogs, not runtime game code."""
from pathlib import Path
import json,csv,hashlib
ROOT=Path(__file__).resolve().parents[1]
def write(name,s): (ROOT/name).write_text(s.strip()+'\n',encoding='utf8')
def csvfile(name,fields,rows):
 with (ROOT/name).open('w',encoding='utf-8-sig',newline='') as f:
  w=csv.DictWriter(f,fieldnames=fields);w.writeheader()
  for r in rows:w.writerow(dict(zip(fields,r)))

strings=[]
def loc(key,en,es,scope='common',width=650,size=27,lines=1):strings.append({'key':key,'en':en,'es_UY':es,'scope':scope,'max_width_reference_px':width,'font_reference_px':size,'max_lines':lines})
for key,en,es in [
('play_solo','PLAY SOLO','JUGAR SOLO'),('play_friends','PLAY WITH FRIENDS','JUGAR CON AMIGOS'),('how_to','HOW TO PLAY','CÓMO JUGAR'),('settings','SETTINGS','OPCIONES'),('quit','QUIT','SALIR'),('back','BACK','VOLVER'),('resume','RESUME','CONTINUAR'),('cancel','CANCEL','CANCELAR'),('apply','APPLY CHANGES','APLICAR CAMBIOS'),('leave_match','LEAVE MATCH','SALIR DE LA PARTIDA'),('leave_lobby','LEAVE LOBBY','SALIR DEL LOBBY'),('back_menu','BACK TO MENU','VOLVER AL MENÚ'),('retry_same','RETRY SAME SETUP','REPETIR CONFIGURACIÓN'),('new_setup','NEW SETUP','NUEVA CONFIGURACIÓN'),('back_lobby','BACK TO LOBBY','VOLVER AL LOBBY'),('start','START EXTRACTION','INICIAR EXTRACCIÓN'),('copy_code','COPY INVITE CODE','COPIAR CÓDIGO'),('create','CREATE ROOM','CREAR SALA'),('join','JOIN BY CODE','UNIRSE CON CÓDIGO'),('ready','READY','LISTO'),('not_ready','NOT READY','SIN CONFIRMAR'),('wait_host','WAITING FOR HOST','ESPERANDO AL ANFITRIÓN')]:loc('button.'+key,en,es,width=515)
for key,en,es in [
('tagline','The party is over. He disagrees.','La fiesta terminó. Él no está de acuerdo.'),('crew','GET THE CREW TOGETHER','JUNTÁ AL GRUPO'),('your_crew','YOUR CREW','TU GRUPO'),('private','PRIVATE','PRIVADA'),('host','Host','Anfitrión'),('joined','Joined','Conectado'),('you','YOU','VOS'),('difficulty','DIFFICULTY','DIFICULTAD'),('relaxed','RELAXED','RELAJADO'),('normal','NORMAL','NORMAL'),('hard','HARD','DIFÍCIL'),('variant','Mode: Alcohol / Partygoer','Modo: Alcohol / Fiestero'),('mode_hint','He fights to stay. Work together to get him out.','Se resiste a irse. Coordínense para sacarlo.'),('waiting_player','Waiting for {player} to get ready.','Esperando que {player} confirme.'),('players_one','{count} PLAYER','{count} JUGADOR'),('players_other','{count} PLAYERS','{count} JUGADORES'),('minutes','{minutes} MIN','{minutes} MIN'),('untimed','NO TIMER','SIN RELOJ'),('code','INVITE CODE','CÓDIGO DE SALA'),('copied','Invite code copied.','Código copiado.'),('connecting','Connecting...','Conectando...'),('creating','Creating room...','Creando sala...'),('loading','Loading the bar...','Cargando el bar...')]:loc('lobby.'+key,en,es,'lobby',570,27,2)
for key,en,es in [
('audio_language','AUDIO & LANGUAGE','AUDIO E IDIOMA'),('master','Master volume','Volumen general'),('music','Music','Música'),('voice','NPC voice','Voz del personaje'),('cues','Gameplay cues','Señales de juego'),('language','Language','Idioma'),('english','ENGLISH','INGLÉS'),('spanish','SPANISH','ESPAÑOL'),('comfort','COMFORT & ACCESSIBILITY','COMODIDAD Y ACCESIBILIDAD'),('attack_subtitles','Descriptive attack subtitles','Subtítulos de ataques'),('shake','Camera shake','Sacudida de cámara'),('head_bob','Head bob','Balanceo al caminar'),('toggle_grab','Toggle grab','Alternar agarre'),('profanity','Profanity filter','Suavizar insultos'),('text_size','Text size: {percent}%','Texto: {percent}%'),('on','ON','SÍ'),('off','OFF','NO'),('unsaved','Apply your changes before leaving?','¿Aplicar los cambios antes de salir?'),('discard','DISCARD CHANGES','DESCARTAR CAMBIOS')]:loc('settings.'+key,en,es,'settings',680,27,2)
for key,en,es in [
('objective','GET HIM OUT','SACALO DEL BAR'),('objective_detail','Get him into the van.','Subilo a la camioneta.'),('left','LEFT HAND','MANO IZQ.'),('right','RIGHT HAND','MANO DER.'),('free','FREE','LIBRE'),('pending','REQUESTING','PIDIENDO'),('holding','HOLDING','SUJETANDO'),('strained','STRAINED','EN TENSIÓN'),('released','RELEASED','SOLTÓ'),('unavailable','UNAVAILABLE','NO DISPONIBLE'),('hold_grab','Hold to grab','Mantené para agarrar'),('press_grab','Press to grab','Pulsá para agarrar'),('press_release','Press to release','Pulsá para soltar'),('aim','Aim at a handle','Apuntá a un agarre'),('forearm_left','Left forearm','Antebrazo izquierdo'),('grip_first','Grip first','Agarrá primero'),('energy','ENERGY','ENERGÍA'),('composure','COMPOSURE','COMPOSTURA'),('effort','Effort','Esfuerzo'),('pull','PULL TOGETHER','TIREN JUNTOS'),('release_tip','{release} release all · Watch his free hand.','{release} soltá todo · Ojo con su mano libre.'),('push','PUSH INCOMING','VA A EMPUJAR'),('slap','SLAP INCOMING','VA A DAR UN MANOTAZO'),('kick','KICK INCOMING','VA A PATEAR'),('shake','HE IS ABOUT TO SHAKE','VA A SACUDIRSE'),('evade','Step aside or let go.','Hacete a un lado o soltalo.'),('preparing_push','[preparing a push]','[prepara un empujón]'),('preparing_slap','[preparing a slap]','[prepara un manotazo]'),('preparing_kick','[preparing a kick]','[prepara una patada]'),('preparing_shake','[preparing a shake]','[prepara una sacudida]'),('fall_title','FLOOR INSPECTION','INSPECCIÓN DEL PISO'),('getting_up','Getting up automatically...','Te estás levantando...'),('control_return','You will regain control shortly.','Vas a recuperar el control en breve.'),('recovering','RECOVERING','RECUPERANDO'),('grip_busy','That grip is already taken.','Ese agarre está ocupado.'),('too_far','Move closer to grab.','Acercate para agarrar.'),('exhausted','He is catching his breath. Move now!','Está recuperando el aire. ¡Muevan ahora!'),('door_blocked','Clear the van door.','Despejá la puerta de la camioneta.')]:loc('hud.'+key,en,es,'hud',650,27,2)
for key,en,es in [
('pause','TAKE A BREATHER','TOMATE UN RESPIRO'),('online_continues','ONLINE MATCH CONTINUES','LA PARTIDA ONLINE CONTINÚA'),('online_detail','Your friends and the uncle are still moving.','Tus amigos y el tío siguen moviéndose.'),('host_leave','Leaving ends the match for everyone.','Si salís, termina la partida para todos.'),('client_leave','Leave this match and return to the menu?','¿Salir de esta partida y volver al menú?'),('success','UNCLE EXTRACTED','TÍO EXTRAÍDO'),('success_detail','He is in the van. Somehow.','Está en la camioneta. De alguna manera.'),('timeout','CLOSING TIME','HORA DE CERRAR'),('timeout_detail','The ride left. He did not.','La camioneta se fue. Él no.'),('time','EXTRACTION TIME','TIEMPO DE EXTRACCIÓN'),('falls','TEAM FALLS','CAÍDAS DEL GRUPO'),('escapes','ESCAPES','FUGAS'),('award','HUMAN DOORSTOP','TOPE DE PUERTA HUMANO'),('award_detail','Kept the exit open for the crew.','Mantuvo la salida abierta para el grupo.'),('next_try','NEXT TRY','PRÓXIMO INTENTO'),('hint','Wait for his recovery, clear the route and pull together.','Esperen su recuperación, despejen la ruta y tiren juntos.')]:loc('round.'+key,en,es,'round',1580,29,2)
for key,en,es in [
('host_left','THE HOST LEFT','EL ANFITRIÓN SALIÓ'),('host_left_detail','This match has ended. You can return to the menu and start another one.','La partida terminó. Podés volver al menú y empezar otra.'),('invalid','That invite code is not valid.','Ese código de sala no es válido.'),('full','This room is full.','La sala está llena.'),('version','Your game versions do not match.','Las versiones del juego no coinciden.'),('timeout','The connection timed out. Try joining again.','La conexión tardó demasiado. Intentá unirte de nuevo.'),('service','Online is unavailable right now. You can still play solo.','El modo online no está disponible ahora. Podés jugar solo.'),('not_started','No online match was started.','No se inició ninguna partida online.'),('already_started','That match has already started. Join the next round.','Esa partida ya empezó. Unite en la próxima ronda.'),('copy_failed','Could not copy the code. You can select it manually.','No se pudo copiar. Podés seleccionar el código manualmente.')]:loc('error.'+key,en,es,'connection',810,29,3)
for key,en,es in [
('title','GET HIM OUT. TOGETHER.','SÁQUENLO. JUNTOS.'),('grab','01 / GRAB','01 / AGARRÁ'),('watch','02 / WATCH','02 / OBSERVÁ'),('move','03 / MOVE','03 / MOVÉ'),('grab_detail','Use either hand to grab a forearm, ankle or clothing handle.','Usá cualquier mano para agarrar antebrazo, tobillo o ropa.'),('watch_detail','He telegraphs every attack. Step aside or release before he hits.','Avisa antes de atacar. Hacete a un lado o soltá antes del golpe.'),('move_detail','Pull during recovery. Use the cart and clear a route to the van.','Tiren durante su recuperación. Usen el carrito y despejen la ruta.'),('practice','Practice solo before joining your crew.','Practicá solo antes de sumarte al grupo.')]:loc('tutorial.'+key,en,es,'tutorial',490,30,5)
write('data/localization_en_es.json',json.dumps({'schema':'ui.localization.v0.1','locale_notes':'English mockups; es_UY strings provided. Runtime layout in both languages must still be verified in Unity. Tokens in braces are runtime values, not C# interpolation instructions.','strings':strings},ensure_ascii=False,indent=2))

tokens={'schema':'ui.design_tokens.v0.1','status':'DESIGN_SPEC_NOT_UNITY_IMPLEMENTED','reference_resolution_px':[1920,1080],'safe_margin_px':64,'spacing_px':[4,8,12,16,24,32,48,64],'colors':{'ink':'#10171D','panel':'#19252D','primary':'#205F5D','accent':'#3CA6A0','text':'#F3E7CF','muted_text':'#B7C3BE','focus':'#E9A84A','danger_text':'#F07861','disabled':'#344048','danger_panel':'#442827'},'typography_px':{'display':112,'screen_title':54,'section_title':31,'button':27,'body':27,'hud_label':20,'timer':36,'design_watermark':19},'font_source':'assets/fonts/DejaVuSans.ttf','font_bold_source':'assets/fonts/DejaVuSans-Bold.ttf','button_height_reference_px':72,'corners_px':{'panel':18,'button':12,'chip':9},'canvas_recipe':{'system':'uGUI proposed','render_mode':'Screen Space Overlay proposed','scale_mode':'Scale With Screen Size','reference_resolution':[1920,1080],'match_width_height':0.5,'native_game_engine_import_tested':False},'accessibility':{'default_subtitles':True,'descriptive_cues':True,'camera_shake_optional':True,'head_bob_optional':True,'grab_toggle_supported':True,'color_is_not_only_signal':True,'large_text_layouts_to_test':[1.0,1.25,1.5],'resolutions_to_test':[[1280,720],[1920,1080],[2560,1080]],'icons_4k_note':'64px PNGs are supplied; use SVG sources for larger crisp exports before claiming 4K presentation.'},'layout_convention':'Mockup screen positions are top-left pixels. Convert to actual RectTransform anchors and pivots; do not copy them blindly as Unity local coordinates.'}
write('data/design_tokens.json',json.dumps(tokens,ensure_ascii=False,indent=2))

states=[
('Menu','local','play_solo','SetupSolo','No external online service needed for local mode'),
('Menu','local','play_friends','ConnectionChoice','Create room or join code; reusable modal'),
('ConnectionChoice','local+session','create_or_join','Connecting','Display busy and allow cancel; ignore stale callbacks'),
('Connecting','session','success','Lobby','Validate version, slots and round availability'),
('Connecting','session','failure','ConnectionError','Concrete reason; retry only when reason is retryable'),
('Lobby','host+local ready','all_present_ready_and_host_start','Loading','Host commits setup; clients cannot start'),
('Loading','host','scene_ready','Playing.Free','All required actors loaded or controlled timeout'),
('Playing.Free','host for grip','grab_request','Playing.Pending','Do not show Holding before grant'),
('Playing.Pending','host','grant_or_reject','Playing.Holding|Playing.Free','Pair grant with requestId; discard stale replies'),
('Playing.Holding','host','tension_threshold','Playing.Strained','Color plus icon and text; no audio spam'),
('Playing.Holding|Playing.Strained','host','release_or_fall','Playing.Free|Playing.Fallen','Apply cleanup confirmation; no restoring old grab automatically'),
('Playing.Any','host','attack_windup','Playing.Telegraph','Overlay on base gameplay state; cancel after authoritative end'),
('Playing.Any','host','forced_recovery','Playing.RecoveryCue','Show opportunity; do not hide next critical cue'),
('Playing.Any','host','knockdown','Playing.Fallen','Release hands; auto-recovery; no death screen'),
('Playing.Fallen','host','standup_complete','Playing.Free','Short control-return cue; grace from game rules'),
('Playing.Any','local','pause','PauseSolo|PauseOnline','Solo pauses local sim; online does not'),
('PauseOnline','local','resume','Playing.Current','Round may have advanced; do not resume a stale state'),
('Playing.Any|PauseOnline','host','round_terminal','ResultSuccess|ResultTimeout','Terminal authority wins over transient overlays'),
('ResultSuccess|ResultTimeout','host','retry_or_new_setup','Lobby','Ready resets; same seed only for retry'),
('Playing.Any|Lobby|Loading','session','host_lost','ConnectionError','No migration promised; explicit return path'),
('ResultSuccess|ResultTimeout','session','host_lost_after_terminal_confirmed','ResultSuccess|ResultTimeout','Keep confirmed result; disable session commands and offer menu'),
('SetupSolo','local authority','confirm_setup','Loading','One-player config; no online service dependency'),
('PauseSolo','local','resume','Playing.Current','Resume local simulation; new input press required'),
('ConnectionError','local','back_menu','Menu','Cleanup old attempts and present an enabled exit'),
('Connecting','local','cancel','Menu','Cancel provider attempt and ignore late completion'),
('ResultSuccess|ResultTimeout','local','exit_menu','Menu','Leave session safely without rewriting confirmed result'),
('AnyLocalMenu','local','open_settings','Settings','Remember prior context and draft options'),
('Settings','local','apply','PreviousContext','Persist only accepted values; cancel restores stored draft'),
('Settings','local','back_with_changes','UnsavedModal','Apply or discard; keep existing session if any')]
csvfile('data/ui_state_flow.csv',['state','authority','event','next','rule'],states)

bindings=[
('hud.left_hand','GrabRegistry.left','host confirmed','enum Free/Pending/Holding/Strained','No confirmation by animation alone'),
('hud.right_hand','GrabRegistry.right','host confirmed','same','Independent hand state and socket label'),
('hud.energy','Player.energy','host with optional visual prediction','0..100','Smooth bar; never drives game force'),
('hud.composure','Player.composure','host','0..100','Show when hit or low; no medical health label'),
('hud.timer','Round.remainingSeconds','host tick','mm:ss or hidden when untimed','Client cannot extend round by UI clock'),
('hud.telegraph','Attack.instanceId/type/timing','host event','overlay','Cancel old instance; critical subtitle priority'),
('hud.fall','Player.controlState','host','fallen/recovering/up','Hide grab hints while unavailable'),
('hud.effort','hands + Player.controlState','local visual and host state','enabled only when holding and able','No effort hint while fallen or free'),
('hud.world_marker','projected socket or attacking limb','local projection of host actor','screen position or edge indication','Mockup coordinates are illustrative, not fixed runtime positions'),
('lobby.start','allReady + isLocalHost + validSetup','host','enabled/disabled','Disabled reason text; no start while loading'),
('lobby.difficulty','Session.setup.difficulty','host','relaxed/normal/hard','Read-only for guests'),
('lobby.ready','Local readiness then host confirmation','local request + host state','ready/not ready','Reset after setup change or completed round'),
('lobby.copy','Session.inviteCode','session provider','string','Copy local clipboard; provide visible fallback on error'),
('result.stats','Round.finalStats','host terminal snapshot','time/falls/escapes','No invented awards; dash when no extraction time'),
('result.retry','isLocalHost and sessionAlive','host','enabled/disabled','Guests wait or leave; host cannot force an unready load'),
('pause.context','Session.mode','local+session','solo/online','Message describes real behavior'),
('settings.audio','ProfileStore draft','local','0..100','Preview safely; Apply persists; Cancel restores'),
('settings.language','ProfileStore locale','local','en/es_UY','Use keys and binding glyphs; no literal LMB in translated sentence'),
('settings.profanity','ProfileStore filter','local','on/off','Use dialogue clean columns, not substring replacement'),
('connection.error','Session error reason','session','enum/message key','Do not report every problem as internet failure'),
('menu.play_solo','LocalBootstrap','local','command','Does not require Steam/Relay for development local provider')]
csvfile('data/ui_bindings.csv',['control_id','source','authority','value','rule'],bindings)

cases=[
('Menu','Start solo without online services','Local game remains reachable'),('Menu','Keyboard and gamepad focus','Initial Play Solo; visible focus; no focus loss'),
('Lobby','One through four present','Correct count; no empty-slot readiness requirement'),('Lobby','Host with one not ready','Start disabled with exact reason'),('Lobby','Guest changes difficulty','Read-only; no authority escalation'),('Lobby','Setup changes after ready','Readiness invalidated visibly'),('Lobby','Invite code copy failure','Selectable value and clear fallback'),
('Connection','Cancel join then late success','Stale callback ignored; no ghost lobby'),('Connection','Invalid/full/version/running cases','Specific distinct message keys'),('Connection','Service down','Offer menu and solo; no spinner forever'),
('HUD','Pending request denied','No false Holding; hand returns Free'),('HUD','Late grant after release','Stale reply ignored by requestId'),('HUD','One hand releases','Other state remains correct'),('HUD','Tension threshold oscillates','Hysteresis or debounce; no flashing spam'),('HUD','Windup then round ends','Terminal clears telegraph and input prompts'),('HUD','Voice muted','Descriptive signal still readable'),('HUD','No grasp or fallen','Effort prompt hidden/disabled'),('HUD','Relaxed difficulty','Timer hidden; no invented 00:00 timeout'),
('Fall','Control is lost briefly','Auto-recovery; no revive requirement solo'),('Fall','Returns to standing','Current hand state; no old attachment restored'),
('Pause','Online open menu','Simulation continues; local inputs canceled'),('Pause','Solo open menu','Simulation pauses according to local mode'),('Pause','Host selects leave','Confirmation explains everyone will leave match'),('Pause','Terminal result while paused','Result replaces obsolete pause screen'),
('Settings','Draft then Back','Apply/discard; cancel restores previous values'),('Settings','Apply and restart','Accepted options persist'),('Settings','Spanish 150% text','No essential clipping; scroll if needed'),('Settings','Rebind controls','Prompts update from bindings, not hardcoded strings'),
('Results','Guest retry','Host action unavailable; clear wait state'),('Results','Host lost after confirmed result','Keep stats; show safe menu exit'),('Results','No qualifying award event','No award invented; omit slot'),('Results','Timeout','No fake extraction time; use dash'),
('Layout','1280x720 and ultrawide','Critical content inside safe area; center HUD not stretched'),('Layout','Names too long or markup-like','Truncate safely; no rich text injection'),('Layout','Non-color cues','State recognizable from shape and wording'),('Import','Sprites font and 9-slice','Import verified in Unity before marked complete')]
csvfile('data/ui_acceptance.csv',['id','area','scenario','expected','status','build','evidence'],[(f'UI_QA_{i:03}',a,b,c,'NOT_RUN','','') for i,(a,b,c) in enumerate(cases,1)])

write('README.md','''
# UNCLE EXTRACTION - kit de interfaz y experiencia

Complemento de diseño v0.1 · 15 de noviembre de 2026. Usar junto con el paquete principal y el kit 3D. No modifica el alcance del juego ni trae una interfaz ya programada en Unity.

## Entrega

- 12 pantallas en PNG 1920x1080 y SVG editable con fondos enlazados.
- 20 iconos en SVG y PNG transparente; 11 sprites modulares sin texto, con receta de importación y bordes 9-slice.
- Dos fondos renderizados desde la geometría real del kit 3D.
- Fuentes DejaVu Sans y Bold sin modificar, con su aviso de licencia completo.
- Catálogos de traducción EN/es_UY, tokens de diseño, estados, bindings y pruebas.
- UI_SPEC.md, ATLAS_INTERFAZ.pdf, scripts para reproducir los recursos y un prompt de integración.

## Usar con Claude

Leer claude/INTEGRAR_UI.md. Los PNG/SVG de screens/ muestran intención visual; no se importan como una sola textura que pretenda ser la interfaz. Construir componentes con sprites, fuentes y textos localizados. Los valores de reloj, jugadores y resultados de las pantallas son ilustrativos.

El GDD, el balance y la arquitectura originales siguen siendo autoridad para mecánicas. Este kit detalla su presentación. El código de source/ genera recursos y documentación; no implementa movimiento, red ni UI interactiva del juego.

## Fuentes y SVG

Extraer la carpeta completa para que los SVG encuentren fondos, iconos y fuentes relativos. Algunos editores pueden pedir instalar las fuentes o reenlazar imágenes. Los PNG sirven como referencia universal. Para Unity, crear el asset de fuente que corresponda a la versión instalada y verificarlo; no se entrega un atlas SDF ficticio.

## Reproducir

python source/build_ui_assets.py
python source/build_ui_data.py

Requiere Python y Pillow. Para regenerar fondos, requiere Blender y la escena del kit 3D; source/render_backgrounds.py admite --scene RUTA. Los fondos ya están incluidos, por lo que no hace falta Blender para editar la interfaz o regenerar los PNG/SVG.

La exportación SVG conserva las piezas de interfaz como vectores y usa fondos raster enlazados. Las etiquetas de mockup y valores de muestra no se trasladan a la interfaz final.

## Estado de verificación

Se revisaron las pantallas e integridad de archivos, tamaños, transparencia y catálogos. La aplicación real en Unity, navegación con mando, red y layouts en ejecución siguen pendientes. Las pruebas de aceptación empiezan NOT_RUN. El reporte de validación distingue revisión de recursos de pruebas del juego.
''')
write('claude/INTEGRAR_UI.md','''
# Integrar interfaz y experiencia en UNCLE EXTRACTION

Continuá desde el proyecto existente, sin rehacer los hitos completados. Leé README.md y UI_SPEC.md de este kit, data/design_tokens.json, data/ui_bindings.csv y data/ui_state_flow.csv. Conservá GDD y balance originales como autoridad de gameplay.

1. Verificá la versión real de Unity, uGUI y la solución de texto. Este kit propone uGUI, Canvas Scaler y fuente SDF compatible; no presupone nombres de API no comprobados.
2. Importá sprites PNG como UI. Usá los bordes de data/sprite_import.json y Image Sliced cuando corresponda. No inventes archivos .meta ni GUID.
3. Creá la fuente con los TTF incluidos y preservá FONT_LICENSE.txt. No conviertas el texto de cada botón en una imagen; usá claves de data/localization_en_es.json y acciones de entrada dinámicas.
4. Construí primero un HUD de manos, energía y señal de empujón conectado a estado real. Pending no es Holding. Caída y resultado limpian prompts y acciones obsoletos.
5. Construí menú, conexión, lobby y resultados según autoridad de cada control. Los clientes no cambian dificultad ni fuerzan inicio. Online no se pausa al abrir el menú. No mostrés host migration que no existe.
6. No traslades las marcas de mockup, números ilustrativos, nombres ficticios ni ejemplos de premios como datos reales. Las capturas de screens/ no son screenshots de gameplay para Steam.
7. Verificá EN/es_UY, textos grandes, teclado/mando, 720p y ultrawide. Los 64px PNG de iconos sirven al tamaño previsto; exportá los SVG a más resolución si el proyecto lo necesita.
8. Registrá imports, archivos, evidencia y limitaciones en PROGRESS.md. Si no podés ejecutar Unity, prepará lo posible y marcá las pruebas pendientes con instrucciones reproducibles.

No rehagas física, movimiento o red para adaptar una pantalla. Los componentes de UI presentan estado y emiten solicitudes al sistema correspondiente. Avanzá con tareas reversibles del alcance; un servicio externo pendiente no impide construir el HUD local.

Entrega mínima de la primera integración: prefab de HUD, fuente y sprites importados, bindings reales de ambas manos y ataque, dos idiomas y prueba de agarre/caída si el entorno lo permite. Después completar menús y pruebas del catálogo.
''')
print(json.dumps({'localized_strings':len(strings),'flow_rows':len(states),'bindings':len(bindings),'acceptance_cases':len(cases)}))
