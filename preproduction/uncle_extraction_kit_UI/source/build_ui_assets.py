"""Create exact editable UI graphics and mockups. Pillow + standard Python.
This generates design files, not a Unity game or interactive frontend.
Run: python source/build_ui_assets.py
"""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import math,json,html,shutil
ROOT=Path(__file__).resolve().parents[1]
COL={'ink':'#10171D','panel':'#19252D','primary':'#205F5D','teal':'#3CA6A0','cream':'#F3E7CF','muted':'#B7C3BE','amber':'#E9A84A','coral':'#F07861','disabled':'#344048','danger':'#442827'}
FONT=ROOT/'assets/fonts/DejaVuSans.ttf';BOLD=ROOT/'assets/fonts/DejaVuSans-Bold.ttf'
def font(size,bold=False):return ImageFont.truetype(str(BOLD if bold else FONT),size)
def rgba(color,alpha=255):return tuple(int(color.lstrip('#')[i:i+2],16) for i in (0,2,4))+(int(alpha),)

class Canvas:
 def __init__(self,w,h,bg=None):
  self.w,self.h=w,h;self.parts=[];self.text_boxes=[]
  if bg:
   self.image=Image.open(ROOT/'assets/backgrounds'/bg).convert('RGBA');assert self.image.size==(w,h)
   self.parts.append(f'<image href="../assets/backgrounds/{bg}" x="0" y="0" width="{w}" height="{h}"/>')
  else:self.image=Image.new('RGBA',(w,h),(0,0,0,0))
 def layer(self):
  im=Image.new('RGBA',(self.w,self.h));return im,ImageDraw.Draw(im)
 def finish(self,layer):self.image=Image.alpha_composite(self.image,layer)
 def rect(self,x,y,w,h,fill=None,r=0,stroke=None,sw=2,alpha=255):
  im,d=self.layer();d.rounded_rectangle((x,y,x+w,y+h),radius=r,fill=rgba(fill,alpha) if fill else None,outline=rgba(stroke) if stroke else None,width=sw);self.finish(im)
  self.parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill or "none"}" fill-opacity="{alpha/255}" stroke="{stroke or "none"}" stroke-width="{sw}"/>')
 def line(self,points,color=None,width=4):
  color=color or COL['cream'];im,d=self.layer();d.line(points,fill=rgba(color),width=width,joint='curve');self.finish(im)
  pts=' '.join(f'{x},{y}' for x,y in points);self.parts.append(f'<polyline points="{pts}" fill="none" stroke="{color}" stroke-width="{width}" stroke-linecap="round" stroke-linejoin="round"/>')
 def polygon(self,points,fill=None,stroke=None,width=4):
  im,d=self.layer();d.polygon(points,fill=rgba(fill) if fill else None)
  if stroke:d.line(points+[points[0]],fill=rgba(stroke),width=width,joint='curve')
  self.finish(im);pts=' '.join(f'{x},{y}' for x,y in points);self.parts.append(f'<polygon points="{pts}" fill="{fill or "none"}" stroke="{stroke or "none"}" stroke-width="{width}"/>')
 def circle(self,x,y,r,fill=None,stroke=None,width=4):
  im,d=self.layer();d.ellipse((x-r,y-r,x+r,y+r),fill=rgba(fill) if fill else None,outline=rgba(stroke) if stroke else None,width=width);self.finish(im)
  self.parts.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill or "none"}" stroke="{stroke or "none"}" stroke-width="{width}"/>')
 def text(self,x,y,s,size=28,bold=False,color=None,max_width=None):
  color=color or COL['cream'];f=font(size,bold)
  if max_width is not None:assert f.getlength(s)<=max_width,(s,f.getlength(s),max_width)
  im,d=self.layer();d.text((x,y),s,font=f,fill=rgba(color));bbox=d.textbbox((x,y),s,font=f);self.finish(im)
  self.text_boxes.append({'text':s,'x0':bbox[0],'y0':bbox[1],'x1':bbox[2],'y1':bbox[3]})
  weight='700' if bold else '400';self.parts.append(f'<text x="{x}" y="{y+size*.94:.2f}" font-family="DejaVu Sans,sans-serif" font-size="{size}" font-weight="{weight}" fill="{color}">{html.escape(s)}</text>')
 def wrap(self,x,y,s,width,size=28,bold=False,color=None,leading=None):
  leading=leading or round(size*1.40);words=s.split();line='';lines=[]
  for word in words:
   candidate=(line+' '+word).strip()
   if font(size,bold).getlength(candidate)>width and line:lines.append(line);line=word
   else:line=candidate
  if line:lines.append(line)
  for i,line in enumerate(lines):self.text(x,y+i*leading,line,size,bold,color,width)
  return len(lines)*leading
 def icon(self,name,x,y,size=44):
  p=ROOT/'assets/icons_png'/f'{name}.png';im=Image.open(p).resize((size,size),Image.Resampling.LANCZOS);self.image.alpha_composite(im,(x,y))
  self.parts.append(f'<image href="../assets/icons_png/{name}.png" x="{x}" y="{y}" width="{size}" height="{size}"/>')
 def save(self,png,svg):
  self.image.save(png)
  css="@font-face{font-family:'DejaVu Sans';src:url('../assets/fonts/DejaVuSans.ttf')}@font-face{font-family:'DejaVu Sans';font-weight:700;src:url('../assets/fonts/DejaVuSans-Bold.ttf')}"
  css=css if self.text_boxes else ''
  Path(svg).write_text(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{self.w}" height="{self.h}" viewBox="0 0 {self.w} {self.h}"><style>{css}</style>'+''.join(self.parts)+'</svg>',encoding='utf-8')

def make_icons():
 for name in ['hand_left','hand_right','grip','release','warning','push','kick','shake','energy','composure','clock','ready','disconnect','door','van','cart','settings','speaker','retry','help']:
  c=Canvas(64,64);co=COL['cream']
  if name.startswith('hand') or name in ['grip','release']:
   c.line([(18,56),(14,39),(9,32),(12,28),(21,36),(21,14),(26,14),(26,31),(27,8),(32,8),(32,31),(33,11),(38,11),(38,32),(40,18),(45,18),(45,42),(40,56),(18,56)],co,4)
   if name=='hand_right':c.image=c.image.transpose(Image.Transpose.FLIP_LEFT_RIGHT);c.parts=['<g transform="translate(64 0) scale(-1 1)">'+''.join(c.parts)+'</g>']
   if name=='release':c.line([(45,8),(57,8),(57,20)],co,4)
   if name=='grip':c.rect(20,26,26,20,COL['panel'],6,co,3)
  elif name=='warning':c.polygon([(32,7),(59,55),(5,55)],stroke=co);c.line([(32,23),(32,37)],co,5);c.circle(32,46,2,co)
  elif name=='push':
   for x in [10,34]:c.rect(x,20,17,29,None,5,co,4);c.line([(x+8,7),(x+8,15)],co,3)
   c.line([(20,12),(31,4),(44,12)],co,4)
  elif name=='kick':c.line([(19,6),(31,26),(29,39),(47,43),(55,49),(52,56),(22,52),(17,42),(20,27),(8,15)],co,5)
  elif name=='shake':c.circle(32,32,11,stroke=co);c.line([(3,21),(9,14),(14,21)],co,3);c.line([(3,43),(9,50),(14,43)],co,3);c.line([(50,21),(56,14),(61,21)],co,3);c.line([(50,43),(56,50),(61,43)],co,3)
  elif name=='energy':c.polygon([(35,5),(12,36),(29,36),(24,59),(53,25),(36,25)],fill=co)
  elif name=='composure':c.polygon([(32,6),(55,15),(51,40),(32,58),(13,40),(9,15)],stroke=co);c.line([(23,31),(30,38),(44,23)],co,4)
  elif name=='clock':c.circle(32,32,24,stroke=co);c.line([(32,15),(32,32),(44,38)],co,4)
  elif name=='ready':c.line([(10,33),(26,49),(55,15)],co,6)
  elif name=='disconnect':c.line([(27,11),(19,11),(9,22),(9,33),(18,40)],co,5);c.line([(37,53),(45,53),(55,42),(55,31),(46,24)],co,5);c.line([(8,56),(56,8)],co,4)
  elif name=='door':c.rect(13,5,38,54,None,2,co,4);c.line([(23,14),(23,53),(43,59)],co,3);c.circle(39,34,2,co)
  elif name=='van':c.rect(5,19,54,29,None,6,co,4);c.line([(42,20),(42,39),(59,39)],co,3);c.circle(18,50,7,stroke=co);c.circle(47,50,7,stroke=co)
  elif name=='cart':c.line([(7,12),(17,12),(22,43),(52,43),(57,25),(20,25)],co,4);c.circle(27,54,5,stroke=co);c.circle(48,54,5,stroke=co)
  elif name=='settings':
   c.circle(32,32,17,stroke=co);c.circle(32,32,6,stroke=co)
   for i in range(8):
    a=i*math.pi/4;c.line([(32+21*math.cos(a),32+21*math.sin(a)),(32+28*math.cos(a),32+28*math.sin(a))],co,5)
  elif name=='speaker':c.polygon([(9,25),(20,25),(35,12),(35,52),(20,39),(9,39)],stroke=co);c.line([(44,19),(52,28),(52,37),(44,46)],co,4)
  elif name=='retry':c.line([(16,20),(16,8),(5,20),(16,20),(25,11),(39,11),(52,21),(55,35),(46,49),(31,54),(17,47)],co,4)
  elif name=='help':c.circle(32,32,25,stroke=co);c.line([(23,23),(26,17),(37,17),(43,23),(40,29),(32,34),(32,39)],co,4);c.circle(32,48,2,co)
  c.save(ROOT/'assets/icons_png'/f'{name}.png',ROOT/'assets/icons_svg'/f'{name}.svg')

def make_sprites():
 specs=[('panel_dark',256,256,COL['panel'],COL['teal'],20,24),('panel_danger',256,128,COL['danger'],COL['coral'],16,20),
 ('button_primary',256,96,COL['primary'],None,12,16),('button_hover',256,96,COL['primary'],COL['amber'],12,16),
 ('button_pressed',256,96,COL['ink'],COL['teal'],12,16),('button_disabled',256,96,COL['disabled'],None,12,16),
 ('keycap',96,96,COL['cream'],COL['muted'],12,16),('bar_track',256,32,COL['disabled'],None,12,12),
 ('bar_fill_teal',256,32,COL['teal'],None,12,12),('bar_fill_amber',256,32,COL['amber'],None,12,12),
 ('focus_ring',256,96,None,COL['amber'],12,16)]
 data=[]
 for name,w,h,fill,stroke,r,border in specs:
  c=Canvas(w,h);c.rect(4,4,w-8,h-8,fill,r,stroke,sw=4,alpha=245);c.image.save(ROOT/'assets/sprites'/f'{name}.png')
  data.append({'id':name,'file':f'assets/sprites/{name}.png','size_px':[w,h],'border_lbrt_px':[border]*4,'texture_type':'Sprite (2D and UI)','sprite_mode':'Single','mesh_type':'Full Rect','sRGB':True,'alpha_is_transparency':True,'mipmaps':False,'filter':'Bilinear','compression':'None proposed; profile before shipping','ppu':100,'ui_image_type':'Sliced','text_baked_in':False})
 (ROOT/'data/sprite_import.json').write_text(json.dumps({'status':'DESIGN_RECIPE_NOT_UNITY_IMPORTED','sprites':data},indent=2))

def base(title=None,darken=150,bg='bar_idle.png'):
 c=Canvas(1920,1080,bg);c.rect(0,0,1920,1080,COL['ink'],alpha=darken)
 c.text(64,24,'UNCLE EXTRACTION',24,True)
 c.rect(1370,22,486,42,COL['ink'],8,alpha=235);c.text(1390,29,'DESIGN MOCKUP / NO GAMEPLAY',21,True,COL['amber'])
 c.text(64,1030,'UI DESIGN v0.1  |  Illustrative values  |  English layout',19,False,COL['muted'])
 if title:c.text(64,92,title,54,True)
 return c
def panel(c,x,y,w,h):c.rect(x,y,w,h,COL['panel'],18,stroke=COL['teal'],sw=1,alpha=245)
def button(c,x,y,w,label,primary=True,disabled=False,focus=False):
 fill=COL['disabled'] if disabled else COL['primary'] if primary else COL['panel']
 c.rect(x,y,w,72,fill,12,COL['amber'] if focus else None,3)
 color=COL['muted'] if disabled else COL['cream'];c.text(x+25,y+18,label,27,True,color,w-50)
def chip(c,x,y,label,color=None,w=None):
 w=w or int(font(22,True).getlength(label))+32;c.rect(x,y,w,42,color or COL['primary'],9);c.text(x+16,y+7,label,22,True,max_width=w-30)
def keycap(c,x,y,label,w=68):c.rect(x,y,w,43,COL['cream'],8);c.text(x+12,y+8,label,20,True,COL['ink'],w-20)
def bar(c,x,y,w,fraction,color=None,h=14):
 c.rect(x,y,w,h,COL['disabled'],h//2);c.rect(x,y,max(1,int(w*fraction)),h,color or COL['teal'],h//2)
def hand(c,x,label,key,state,detail,color=None):
 panel(c,x,865,360,135);c.icon('hand_left' if label.startswith('LEFT') else 'hand_right',x+20,890,47)
 c.text(x+83,881,label,20,True,COL['muted']);c.text(x+83,916,state,28,True,color or COL['cream']);c.text(x+83,958,detail,20,False,COL['muted'],255)
 keycap(c,x+275,880,key,w=74)
def hud(state):
 c=base(darken=32,bg='bar_windup.png' if state=='windup' else 'bar_idle.png')
 panel(c,64,96,470,118);c.text(90,116,'GET HIM OUT',33,True);c.text(90,164,'Get him into the van.',24)
 panel(c,1630,96,226,100);c.icon('clock',1650,121,39);c.text(1710,118,'08:42',36,True)
 c.circle(960,540,8,stroke=COL['ink'],width=5);c.circle(960,540,5,stroke=COL['cream'],width=2)
 left='HOLDING' if state in ['grab','windup'] else 'FREE';right='FREE'
 if state=='down':left=right='RELEASED'
 hand(c,64,'LEFT HAND','LMB',left,'Left forearm' if left=='HOLDING' else 'Getting up' if state=='down' else 'Hold to grab')
 hand(c,1496,'RIGHT HAND','RMB',right,'Hold to grab' if state!='down' else 'Getting up')
 panel(c,595,905,730,95);c.icon('energy',615,923,33);c.text(662,917,'ENERGY',21,True,COL['muted']);bar(c,662,956,385,.72)
 
 if state in ['grab','windup']:
  keycap(c,1100,925,'SHIFT',w=112);c.text(1226,933,'Effort',19)
 else:c.text(1100,933,'RECOVERING' if state=='down' else 'Grip first',20,False,COL['muted'])
 if state=='idle':
  panel(c,744,615,435,84);keycap(c,763,635,'LMB',77);c.text(858,630,'Aim at a handle',24,True)
 elif state=='grab':
  panel(c,580,696,760,106);c.text(610,716,'PULL TOGETHER',26,True,COL['amber']);c.text(610,760,'Q release all  ·  Watch his free hand.',24)
 elif state=='windup':
  c.rect(1280,370,570,154,COL['danger'],18,COL['coral'],3,alpha=245);c.icon('push',1305,399,54);c.text(1385,395,'PUSH INCOMING',31,True);c.text(1385,449,'Step aside or let go.',26)
  c.circle(1080,520,52,stroke=COL['coral'],width=5)
  panel(c,545,730,830,85);c.text(574,750,'[preparing a push]  Back up.',28,True)
 else:
  c.rect(530,380,860,270,COL['panel'],20,COL['amber'],3,alpha=245);c.icon('composure',570,415,60)
  c.text(660,410,'FLOOR INSPECTION',43,True);c.text(575,495,'Getting up automatically...',29)
  bar(c,575,555,770,.48,COL['amber'],20);c.text(575,598,'You will regain control shortly.',24,False,COL['muted'])
 return c

def main_screen():
 c=base(darken=155);c.text(64,116,'UNCLE',112,True);c.text(64,245,'EXTRACTION',112,True)
 c.text(69,393,'The party is over. He disagrees.',30,False,COL['amber'])
 for i,label in enumerate(['PLAY SOLO','PLAY WITH FRIENDS','HOW TO PLAY','SETTINGS','QUIT']):button(c,72,485+i*92,565,label,primary=i<2,focus=i==0)
 chip(c,1320,700,'1-4 PLAYERS',w=310);c.wrap(1320,768,'Bad decisions. Better with friends.',440,36,True)
 return c
def lobby_screen():
 c=base('GET THE CREW TOGETHER',darken=215);panel(c,64,200,1090,710);panel(c,1185,200,671,710)
 c.text(96,226,'YOUR CREW',29,True);chip(c,850,222,'PRIVATE',w=255)
 for i,(name,desc,ready,color) in enumerate([('YOU','Host',True,COL['teal']),('SAM','Joined',True,COL['amber']),('ALEX','Joined',False,COL['coral']),('JAMIE','Joined',True,'#8267B0')]):
  y=296+i*133;c.rect(96,y,1020,110,COL['ink'],12);c.circle(139,y+54,19,fill=color);c.text(183,y+18,name,30,True);c.text(184,y+64,desc,23,False,COL['muted']);
  if i==0:
   c.rect(816,y+20,275,70,COL['primary'],10,COL['amber'],3);c.text(842,y+38,'READY',25,True);c.icon('ready',1035,y+38,32)
  else:c.text(848,y+35,'READY' if ready else 'NOT READY',25,True,COL['cream'] if ready else COL['amber'])
 c.text(1218,232,'THE LAST ROUND',35,True);c.text(1218,294,'Mode: Alcohol / Partygoer',24)
 c.text(1218,361,'DIFFICULTY',23,True,COL['muted'])
 for j,label in enumerate(['RELAXED','NORMAL','HARD']):
  x=1218+j*198;c.rect(x,410,180,62,COL['primary'] if j==1 else COL['ink'],10,COL['teal'] if j==1 else None,3);c.text(x+17,427,label,23,True)
 c.wrap(1218,510,'He fights to stay. Work together to get him out.',570,28)
 chip(c,1218,636,'4 PLAYERS  /  10 MIN',w=560)
 button(c,1218,734,570,'START EXTRACTION',disabled=True);c.text(1220,831,'Waiting for Alex to get ready.',23,False,COL['amber'])
 button(c,64,936,280,'LEAVE LOBBY',False);button(c,1185,936,671,'COPY INVITE CODE',False)
 return c
def settings_screen():
 c=base('SETTINGS',darken=220);panel(c,64,205,838,670);panel(c,932,205,924,670)
 c.text(96,237,'AUDIO & LANGUAGE',31,True)
 for i,(label,val) in enumerate([('Master volume',.8),('Music',.5),('NPC voice',.9),('Gameplay cues',1)]):
  y=323+i*105;c.text(98,y,label,27);c.text(770,y,f'{int(val*100)}%',24,True);bar(c,98,y+50,725,val)
 c.text(98,771,'Language',27);chip(c,510,759,'ENGLISH',w=340)
 c.text(968,237,'COMFORT & ACCESSIBILITY',31,True)
 for i,(label,value) in enumerate([('Descriptive attack subtitles','ON'),('Camera shake','OFF'),('Head bob','OFF'),('Toggle grab','ON'),('Profanity filter','OFF')]):
  y=325+i*92;c.text(970,y,label,27);chip(c,1695,y-2,value,w=113)
 c.text(970,798,'Text size: 100%  /  adjustable',25,False,COL['muted'])
 button(c,64,930,280,'BACK',False);button(c,1296,930,560,'APPLY CHANGES',focus=True)
 return c
def pause_screen():
 c=base('TAKE A BREATHER',darken=190);panel(c,560,230,800,700)
 chip(c,600,270,'ONLINE MATCH CONTINUES',w=720)
 c.wrap(600,343,'Your friends and the uncle are still moving.',690,30)
 for i,label in enumerate(['RESUME','SETTINGS','LEAVE MATCH']):button(c,600,463+i*126,720,label,i==0,focus=i==0)
 return c
def result_screen(win=True):
 c=base(darken=215);title='UNCLE EXTRACTED' if win else 'CLOSING TIME'
 c.text(64,116,title,78,True);c.text(68,221,'He is in the van. Somehow.' if win else 'The ride left. He did not.',32,False,COL['amber'])
 panel(c,64,328,1792,460)
 for i,(label,value) in enumerate([('EXTRACTION TIME','04:38' if win else '--:--'),('TEAM FALLS','7' if win else '12'),('ESCAPES','2' if win else '4')]):
  x=112+i*575;c.text(x,377,label,24,True,COL['muted']);c.text(x,440,value,84,True)
 c.text(112,603,'HUMAN DOORSTOP' if win else 'NEXT TRY',30,True,COL['amber'])
 c.wrap(112,664,'Kept the exit open for the crew.' if win else 'Wait for his recovery, clear the route and pull together.',1580,27)
 button(c,64,858,570,'RETRY SAME SETUP',focus=True);button(c,671,858,570,'NEW SETUP');button(c,1280,858,576,'BACK TO LOBBY',False)
 return c
def error_screen():
 c=base(darken=225);panel(c,460,270,1000,540);c.icon('disconnect',925,308,64)
 c.text(553,415,'THE HOST LEFT',52,True);c.wrap(555,503,'This match has ended. You can return to the menu and start another one.',810,29)
 button(c,555,670,810,'BACK TO MENU',focus=True)
 return c
def tutorial_screen():
 c=base('GET HIM OUT. TOGETHER.',darken=220)
 for i,(icon,title,body,keys) in enumerate([('grip','01 / GRAB','Use either hand to grab a forearm, ankle or clothing handle.','LMB  /  RMB'),('warning','02 / WATCH','He telegraphs every attack. Step aside or release before he hits.','SPACE  /  Q'),('cart','03 / MOVE','Pull during recovery. Use the cart and clear a route to the van.','WASD  /  E')]):
  x=64+i*607;panel(c,x,250,577,600);c.icon(icon,x+40,294,72);c.text(x+40,408,title,36,True);c.wrap(x+40,494,body,490,30);chip(c,x+40,730,keys,w=490)
 button(c,64,925,330,'BACK',False);c.text(665,945,'Practice solo before joining your crew.',28,False,COL['muted'])
 return c

def main():
 assert FONT.is_file() and BOLD.is_file(),'Copy bundled fonts before running.'
 make_icons();make_sprites()
 screens=[('01_main_menu',main_screen()),('02_private_lobby',lobby_screen()),('03_settings',settings_screen()),
 ('04_hud_free',hud('idle')),('05_hud_grab',hud('grab')),('06_hud_windup',hud('windup')),('07_hud_fallen',hud('down')),
 ('08_pause_online',pause_screen()),('09_result_success',result_screen(True)),('10_result_timeout',result_screen(False)),
 ('11_host_disconnected',error_screen()),('12_tutorial',tutorial_screen())]
 manifest=[]
 for name,c in screens:
  c.save(ROOT/'screens'/f'{name}.png',ROOT/'screens'/f'{name}.svg')
  assert all(0<=b['x0']<=b['x1']<=1920 and 0<=b['y0']<=b['y1']<=1080 for b in c.text_boxes),name
  manifest.append({'id':name,'png':f'screens/{name}.png','svg':f'screens/{name}.svg','resolution':[1920,1080],'status':'STATIC_DESIGN_NOT_GAMEPLAY','text_bounds':c.text_boxes})
 (ROOT/'data/screen_manifest.json').write_text(json.dumps({'screens':manifest},ensure_ascii=False,indent=2))
 thumbs=Image.new('RGB',(1920,1080),COL['ink']);d=ImageDraw.Draw(thumbs)
 for i,(name,c) in enumerate(screens):
  x=i%4*480;y=i//4*360;im=c.image.convert('RGB').resize((468,263),Image.Resampling.LANCZOS);thumbs.paste(im,(x+6,y+40));d.text((x+14,y+9),name,font=font(20,True),fill=COL['cream'])
 thumbs.save(ROOT/'screens/00_contact_sheet.png')
 print('UI_ASSETS_CREATED',len(screens),'screens; 20 icons; 11 sprites')
if __name__=='__main__':main()
