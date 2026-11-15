"""Build the readable visual atlas; requires ReportLab and Pillow."""
from pathlib import Path
import json
from PIL import Image as PIL, ImageDraw, ImageFont
from reportlab.platypus import SimpleDocTemplate,Paragraph,Spacer,PageBreak,Image,Table,TableStyle
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.pagesizes import A4,landscape
from reportlab.lib import colors
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
ROOT=Path(__file__).resolve().parents[1]
for n,p in [('UI',ROOT/'assets/fonts/DejaVuSans.ttf'),('UIB',ROOT/'assets/fonts/DejaVuSans-Bold.ttf')]:pdfmetrics.registerFont(TTFont(n,str(p)))
pdfmetrics.registerFontFamily('UI',normal='UI',bold='UIB',italic='UI',boldItalic='UIB')
board=PIL.new('RGB',(1560,500),'#10171D');draw=ImageDraw.Draw(board);f=ImageFont.truetype(str(ROOT/'assets/fonts/DejaVuSans.ttf'),24)
for i,(label,color) in enumerate([('INK','#10171D'),('PANEL','#19252D'),('PRIMARY','#205F5D'),('TEXT','#F3E7CF'),('FOCUS','#E9A84A'),('DANGER','#F07861')]):
 x=30+i*255;draw.rounded_rectangle((x,22,x+224,100),14,fill=color,outline='#B7C3BE',width=2);draw.text((x,115),label,font=f,fill='#F3E7CF')
for i,p in enumerate(sorted((ROOT/'assets/icons_png').glob('*.png'))):
 x=35+(i%10)*152;y=176+(i//10)*145;im=PIL.open(p).resize((66,66),PIL.Resampling.LANCZOS);board.paste(im,(x,y),im);draw.text((x,y+78),p.stem[:11],font=ImageFont.truetype(str(ROOT/'assets/fonts/DejaVuSans.ttf'),18),fill='#B7C3BE')
board.save(ROOT/'assets/component_board.png')
W,H=landscape(A4);M=36;CW=W-2*M
S={
 'title':ParagraphStyle('title',fontName='UIB',fontSize=24,leading=29,textColor=colors.HexColor('#205F5D'),spaceAfter=9),
 'subtitle':ParagraphStyle('subtitle',fontName='UIB',fontSize=15,leading=19,textColor=colors.HexColor('#205F5D'),spaceAfter=8),
 'body':ParagraphStyle('body',fontName='UI',fontSize=10,leading=14.2,textColor=colors.HexColor('#19252D'),spaceAfter=8),
 'small':ParagraphStyle('small',fontName='UI',fontSize=8.5,leading=11.8,textColor=colors.HexColor('#59656B'),spaceAfter=6),
 'cell':ParagraphStyle('cell',fontName='UI',fontSize=9,leading=12,textColor=colors.HexColor('#19252D')),
 'head':ParagraphStyle('head',fontName='UIB',fontSize=9,leading=12,textColor=colors.white)}
story=[]
def p(s,style='body'):story.append(Paragraph(s,S[style]))
def image(path,maxheight=430):
 w,h=PIL.open(path).size;ratio=min(CW/w,maxheight/h);story.append(Image(str(path),width=w*ratio,height=h*ratio));story.append(Spacer(1,6))
def table(rows,widths):
 t=Table([[Paragraph(x,S['head' if i==0 else 'cell']) for x in row] for i,row in enumerate(rows)],colWidths=[v*CW for v in widths],repeatRows=1)
 t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),colors.HexColor('#205F5D')),('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.HexColor('#EFF3EF'),colors.white]),('VALIGN',(0,0),(-1,-1),'TOP'),('TOPPADDING',(0,0),(-1,-1),7),('BOTTOMPADDING',(0,0),(-1,-1),7)]));story.append(t);story.append(Spacer(1,10))
def footer(c,d):
 c.setFillColor(colors.HexColor('#205F5D'));c.setFont('UIB',7);c.drawString(M,H-22,'UNCLE EXTRACTION / ATLAS DE INTERFAZ')
 c.setStrokeColor(colors.HexColor('#C9D2CE'));c.line(M,29,W-M,29);c.setFont('UI',7);c.setFillColor(colors.HexColor('#59656B'));c.drawString(M,17,'Diseño v0.1 · 15 NOV 2026 · Mockups, no gameplay implementado');c.drawRightString(W-M,17,str(d.page))

p('Interfaz y experiencia para implementar','title')
p('Doce pantallas, componentes reutilizables y reglas de presentación. Complemento del GDD y del kit 3D; el motor todavía debe implementar y probar la interfaz.')
image(ROOT/'assets/component_board.png',245)
table([['Entregable','Uso'],['PNG + SVG de cada pantalla','Referencia visual y edición; no importar la pantalla entera como una textura de UI.'],['20 iconos + 11 sprites sin texto','Construir componentes, estados de botón, paneles y señales.'],['Catálogos EN / es_UY, estados y bindings','Conectar mensajes y controles a datos reales, respetando autoridad.']],[.31,.69])
p('Los fondos son renders de la geometría del kit 3D. Relojes, nombres, premios y resultados son ejemplos. Los archivos SVG enlazan imágenes y fuentes relativas; extraer la carpeta completa.','small')
story.append(PageBreak())
p('Reglas que la implementación debe conservar','title')
table([['Situación','Respuesta de la interfaz'],['Agarre pedido','Pending hasta concesión válida; no mostrar Holding por anticipación.'],['Ataque del tío','Señal legible por tipo e instancia; subtítulos descriptivos aunque no haya voces.'],['Jugador caído','Recuperación automática; manos liberadas y sin pistas de esfuerzo activo.'],['Pausa online','El mundo continúa; cancelar inputs locales y limpiar acciones incompatibles.'],['Lobby','El anfitrión configura; iniciar requiere miembros presentes listos.'],['Fin o desconexión','Salir de overlays obsoletos; conservar un resultado terminal ya confirmado.']],[.26,.74])
p('Importar piezas, no screenshots','subtitle')
p('Usar PNG transparentes y bordes 9-slice de sprite_import.json. Crear el asset de fuente que corresponda a Unity instalado. Los textos viven en localization_en_es.json; los glyphs siguen controles remapeados. Comprobar 720p, ultrawide y texto grande en el motor.')
p('Lectura para Claude','subtitle')
p('README.md → UI_SPEC.md → data/design_tokens.json → ui_bindings.csv → ui_state_flow.csv → claude/INTEGRAR_UI.md. Las 36 pruebas de aceptación comienzan sin ejecutar. UI_SPEC.md contiene decisiones y fuentes primarias.')
story.append(PageBreak())

descriptions=[
('01_main_menu','01 / Menú principal','Foco inicial: Play Solo. Con amigos abre Create / Join. Las opciones esenciales conservan nombres comprensibles.'),
('02_private_lobby','02 / Lobby privado','Vista del host. Alex no confirmó: Start está deshabilitado y explica por qué. Dificultad y reloj se derivan del setup real.'),
('03_settings','03 / Opciones','Borrador local con previsualización. Apply guarda; Back con cambios ofrece aplicar o descartar. No cambia a los demás jugadores.'),
('04_hud_free','04 / HUD: manos libres','Objetivo, reloj cuando exista y manos independientes. La pista de esfuerzo espera un agarre; el texto de botón sigue el binding.'),
('05_hud_grab','05 / HUD: agarre confirmado','Holding proviene del registro autoritativo. Soltar una mano no modifica la otra. Las ayudas no reemplazan señales del NPC.'),
('06_hud_windup','06 / HUD: preparación de empujón','Presentar tipo y señal sin afirmar que ya hubo impacto. El marcador se proyectará desde el miembro real; aquí su posición es ilustrativa.'),
('07_hud_fallen','07 / HUD: caída y recuperación','Sin pantalla de muerte. Se sueltan agarres, se ocultan acciones incompatibles y se recupera control según el estado real.'),
('08_pause_online','08 / Pausa online','La partida sigue. Resume vuelve al estado actual; un resultado terminal recibido durante pausa tiene prioridad.'),
('09_result_success','09 / Extracción completada','Estadísticas de ejemplo. Mostrar premios solo con eventos registrados. En invitados, los controles del host no se habilitan.'),
('10_result_timeout','10 / Tiempo agotado','No hubo extracción: no inventar un tiempo de éxito. Reintentar requiere preparación de la siguiente ronda y readiness válido.'),
('11_host_disconnected','11 / El anfitrión se desconectó','No prometer migración. Ofrecer una salida concreta. Si ya se confirmó resultado, conservarlo y deshabilitar acciones de la sesión.'),
('12_tutorial','12 / Aprendizaje breve','Tres decisiones jugables: agarrar, anticipar y mover durante recuperación. Los controles se actualizan al remapear o cambiar dispositivo.')]
for idx,(stem,title,desc) in enumerate(descriptions):
 p(title,'subtitle');image(ROOT/'screens'/f'{stem}.png',429);p(desc,'small')
 if idx<len(descriptions)-1:story.append(PageBreak())
doc=SimpleDocTemplate(str(ROOT/'ATLAS_INTERFAZ.pdf'),pagesize=(W,H),leftMargin=M,rightMargin=M,topMargin=40,bottomMargin=36,title='UNCLE EXTRACTION - Atlas de interfaz',author='Preparado para Stefano Francolino')
doc.build(story,onFirstPage=footer,onLaterPages=footer)
print('ATLAS_BUILT')
