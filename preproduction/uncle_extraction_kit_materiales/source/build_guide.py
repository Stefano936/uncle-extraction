"""Render the material/interaction guide. ReportLab + Pillow."""
from pathlib import Path
from PIL import Image as PIL
from reportlab.platypus import SimpleDocTemplate,Paragraph,Spacer,PageBreak,Image,Table,TableStyle
from reportlab.lib.pagesizes import A4,landscape
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib import colors
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
ROOT=Path(__file__).resolve().parents[1]
FONTS=ROOT.parent/'uncle_extraction_kit_UI/assets/fonts'
if not FONTS.is_dir():FONTS=Path('/usr/share/fonts/truetype/dejavu')
for n,p in [('M',FONTS/'DejaVuSans.ttf'),('MB',FONTS/'DejaVuSans-Bold.ttf')]:pdfmetrics.registerFont(TTFont(n,str(p)))
pdfmetrics.registerFontFamily('M',normal='M',bold='MB',italic='M',boldItalic='MB')
W,H=landscape(A4);M=36;CW=W-2*M
S={
 'title':ParagraphStyle('title',fontName='MB',fontSize=25,leading=30,textColor=colors.HexColor('#205F5D'),spaceAfter=10),
 'h':ParagraphStyle('h',fontName='MB',fontSize=14,leading=19,textColor=colors.HexColor('#205F5D'),spaceAfter=9,spaceBefore=8),
 'body':ParagraphStyle('body',fontName='M',fontSize=10,leading=14.5,textColor=colors.HexColor('#19252D'),spaceAfter=9),
 'small':ParagraphStyle('small',fontName='M',fontSize=8.2,leading=11.5,textColor=colors.HexColor('#59656B'),spaceAfter=7),
 'cell':ParagraphStyle('cell',fontName='M',fontSize=9,leading=12.5,textColor=colors.HexColor('#19252D')),
 'head':ParagraphStyle('head',fontName='MB',fontSize=9,leading=12.5,textColor=colors.white)}
story=[]
def p(s,style='body'):story.append(Paragraph(s,S[style]))
def image(path,h):
 w,ih=PIL.open(path).size;ratio=min(CW/w,h/ih);story.append(Image(str(path),width=w*ratio,height=ih*ratio));story.append(Spacer(1,7))
def table(rows,widths):
 t=Table([[Paragraph(c,S['head' if i==0 else 'cell']) for c in row] for i,row in enumerate(rows)],colWidths=[x*CW for x in widths],repeatRows=1)
 t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),colors.HexColor('#205F5D')),('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.HexColor('#EFF3EF'),colors.white]),('VALIGN',(0,0),(-1,-1),'TOP'),('TOPPADDING',(0,0),(-1,-1),7),('BOTTOMPADDING',(0,0),(-1,-1),7),('LEFTPADDING',(0,0),(-1,-1),8),('RIGHTPADDING',(0,0),(-1,-1),8)]));story.append(t);story.append(Spacer(1,11))
def footer(c,d):
 c.setFillColor(colors.HexColor('#205F5D'));c.setFont('MB',7);c.drawString(M,H-22,'UNCLE EXTRACTION / MATERIALES E INTERACCIÓN')
 c.setStrokeColor(colors.HexColor('#C9D2CE'));c.line(M,29,W-M,29);c.setFont('M',7);c.setFillColor(colors.HexColor('#59656B'));c.drawString(M,17,'Complemento v0.1 · 15 NOV 2026 · Lookdev de prototipo; Unity pendiente');c.drawRightString(W-M,17,str(d.page))

p('Texturas aplicadas al prototipo','title')
p('Cuatro mapas de color, una escena de materiales y fichas de interacción para que Claude integre el arte con reglas claras.')
image(ROOT/'previews/01_bar_materials.png',338)
p('Render real de la copia Blender entregada. Se mantuvieron los modelos y se prepararon materiales y UV; no hay gameplay, físicas ni interfaz ejecutándose en esta imagen.','small')
p('El kit se suma a la entrega unificada anterior. El GDD y el balance siguen definiendo cómo se juega; los materiales definen cómo se ven las superficies.')
story.append(PageBreak())

p('Los cuatro mapas de color','title')
cells=[]
for filename,label in [('01_wood_basecolor.png','MADERA · tabla/barra · 1 m'),('02_floor_basecolor.png','PISO · damero de 4 x 4 · 2 m'),('03_wall_basecolor.png','PARED · taupe gastado · 2 m'),('04_fabric_basecolor.png','TELA · camisa bordó · 0,5 m')]:
 cells.append([Paragraph(label,S['h']),Image(str(ROOT/'textures'/filename),width=176,height=176)])
rows=[[cells[0],cells[1]],[cells[2],cells[3]]];grid=Table(rows,colWidths=[CW/2,CW/2]);grid.setStyle(TableStyle([('VALIGN',(0,0),(-1,-1),'TOP'),('BOTTOMPADDING',(0,0),(-1,-1),5)]));story.append(grid)
p('Archivos originales RGB de 1254 x 1254 px, sin editar tras su generación. Se entregan Base Color; no mapas normal, height, roughness, AO ni metallic. Los prompts y la procedencia están incluidos.','small')
story.append(PageBreak())

p('Repetición y escala de material','title')
image(ROOT/'previews/03_material_repeat_review.png',350)
p('Cada superficie muestra dos repeticiones por eje. Madera, pared y tela usan Mirror; el piso usa Repeat para conservar la alternancia del damero. Esta prueba visual no certifica periodicidad perfecta de los píxeles de origen.')
p('Las variantes FBX ya codifican metros por repetición en UV0: comenzar con Tiling 1,1. Revisar patrón y distancia en Unity antes de afinar densidad o compresión.','small')
story.append(PageBreak())

p('Camisa y lectura del personaje','title')
image(ROOT/'previews/02_uncle_fabric.png',340)
p('La tela cambia la camisa; piel, ojos, sandalias y otros detalles mantienen color plano. El render usa la malla y el rig originales. La legibilidad de manos y postura tiene prioridad sobre agregar microdetalle.')
p('Los pesos y las UV son de prototipo. Revisar costuras y deformación durante las animaciones. Usar mipmaps y evaluar la trama a distancia; no asignar la imagen de color al slot de normal.','small')
story.append(PageBreak())

p('Receta inicial para Unity / URP','title')
table([['Superficie','Metallic','Smoothness','Wrap','Dato de escala'],['Madera','0','0,22','Mirror','1 m por imagen'],['Baldosas','0','0,18','Repeat','2 m por imagen; 0,5 m por baldosa'],['Pared','0','0,05','Mirror','2 m por imagen'],['Tela','0','0,10','Mirror','0,5 m por imagen']],[.22,.12,.16,.15,.35])
p('<b>Base:</b> URP/Lit, Opaque, textura de color sRGB y tinte blanco. Los valores son propuestas de lookdev, no mediciones físicas. Smoothness es escalar; no está escondida en un alpha que estos RGB no contienen.')
p('<b>Importación:</b> mipmaps y filtrado propuestos en texture_import.json. El tamaño fuente real es 1254 px; comprobar resolución y compresión finales. Elegir un límite de 2048 no convierte la fuente en 2K.')
p('<b>Variantes:</b> ocho FBX con un canal UV principal. Mantener models y textures como carpetas hermanas o remapear. Importar las variantes junto a los assets existentes; conservar scripts, sockets y física del prefab.')
p('<b>Escena:</b> Material_Lookdev.blend usa rutas relativas. scene_material_review.glb lleva imágenes incorporadas y sirve para revisión visual; no se presupone un importador GLB en Unity vacío.')
story.append(PageBreak())

p('Qué se puede hacer con cada objeto','title')
table([['Objeto','Comportamiento de diseño','Preparación pendiente'],['Tío','Sujeto de extracción; masa constante; golpes y resistencia.','Conectar rig y cuerpos; siete sockets base y uno opcional.'],['Taburete','Obstáculo móvil; puede ofrecer un anclaje alcanzable al NPC.','Añadir y probar sockets; masa inicial propuesta 6 kg.'],['Mesa','Obstáculo fijo en primera muestra.','Collider estático; sin prompt de arrastrar.'],['Barra','Anclaje fijo del NPC, uno activo inicialmente.','Vincular marcadores y acción de liberar mano.'],['Carrito','Transporte individual/cooperativo; ruedas visuales al inicio.','Cuerpo estabilizado, agarre y carga; 18 kg es una hipótesis.'],['Puerta','Apertura por bisagra y acción contextual.','Colisión, límites y masa; 12 kg es una hipótesis.'],['Camioneta','Fija; carga, validación de pose y cierre.','Trigger y verificación autoritativa; no conducción.'],['Mundo','Suelo y límites estáticos.','Completar pared rebajada de inspección y colisión.']],[.16,.44,.40])
p('Las once fichas incluyen además vaso, karaoke y snacks, expresamente pendientes de modelo/sistema. Una ficha no significa que el objeto ya esté implementado.','small')
p('Un material visual no cambia masa, fricción ni permisos de agarre. La mano, el contorno y los mensajes solo aparecen cuando existe una acción real y válida.','small')
story.append(PageBreak())

p('Integración, evidencia y próximo paso','title')
p('Se generaron los cuatro mapas y se aplicaron a una copia de la escena. Se verificaron archivos, enlaces relativos y ocho reimportaciones FBX, incluyendo UV0 y preservación del rig, pesos, sockets y tres acciones del tío. Los reportes contienen la evidencia exacta; no equivalen a una prueba de Unity o gameplay.')
p('Para Claude','h')
p('Leer README.md → docs/ESPECIFICACION_MATERIALES.md → texture_import.json → interaction_manifest.json → claude/INTEGRAR_MATERIALES.md. Usar las variantes para preparar arte sin reescribir la simulación. Registrar cambios de importación, referencias preservadas y pruebas efectivamente ejecutadas.')
p('Qué se debe comprobar en el motor','h')
p('Apariencia a distancia; lectura de ataques; compresión y mipmaps; materiales URP; colisión y agarres; marcadores y cierre de camioneta. Los veinte casos de QA del kit empiezan sin ejecutar. Las masas nuevas son hipótesis; el balance previo tiene prioridad.')
p('Fuentes técnicas','h')
p('<link href="https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/lit-shader" color="#205F5D">Unity 6.3: URP/Lit</link> · <link href="https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/textureimportersettings/srgbtexture" color="#205F5D">sRGB</link> · <link href="https://docs.unity3d.com/cn/6000.0/ScriptReference/TextureWrapMode.html" color="#205F5D">Wrap modes</link> · <link href="https://docs.unity3d.com/es/current/ScriptReference/TextureImporterNPOTScale.html" color="#205F5D">NPOT</link>. Consulta: 15 de noviembre de 2026. La especificación editable conserva las URLs completas.','small')
p('Las imágenes de superficies se generaron con la herramienta integrada de imágenes; los renders muestran los recursos aplicados en Blender. Si el contenido queda en la build final, mantener el inventario de procedencia previsto en la documentación de publicación.','small')

doc=SimpleDocTemplate(str(ROOT/'GUIA_MATERIALES_E_INTERACCION.pdf'),pagesize=(W,H),leftMargin=M,rightMargin=M,topMargin=40,bottomMargin=36,title='UNCLE EXTRACTION - Materiales e interacción',author='Preparado para Stefano Francolino')
doc.build(story,onFirstPage=footer,onLaterPages=footer)
print('MATERIAL_GUIDE_BUILT')
