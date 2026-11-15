"""Resource/catalog checks. No Unity or gameplay tests are executed here."""
from pathlib import Path
from PIL import Image,ImageFont
import json,re,csv,xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]
def read(name):return json.loads((ROOT/name).read_text(encoding='utf8'))
def lum(h):
 v=[int(h[i:i+2],16)/255 for i in (1,3,5)];v=[x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4 for x in v];return .2126*v[0]+.7152*v[1]+.0722*v[2]
def contrast(a,b):a,b=sorted([lum(a),lum(b)]);return (b+.05)/(a+.05)
def wrapped_count(s,f,width):
 words=s.split();count=1;line=''
 for word in words:
  candidate=(line+' '+word).strip()
  if f.getlength(candidate)>width and line:count+=1;line=word
  else:line=candidate
  assert f.getlength(word)<=width,word
 return count
def main():
 manifest=read('data/screen_manifest.json');assert len(manifest['screens'])==12
 for s in manifest['screens']:
  im=Image.open(ROOT/s['png']);assert im.size==(1920,1080)
  assert all(0<=b['x0']<=b['x1']<=1920 and 0<=b['y0']<=b['y1']<=1080 for b in s['text_bounds'])
 for p in ROOT.rglob('*.svg'):
  tree=ET.parse(p)
  for node in tree.iter():
   href=node.attrib.get('href')
   if href:assert (p.parent/href).resolve().is_file(),(p,href)
  for path in re.findall(r"url\(['\"]?(.*?)['\"]?\)",p.read_text()):assert (p.parent/path).resolve().is_file(),(p,path)
 icons=list((ROOT/'assets/icons_png').glob('*.png'));assert len(icons)==20
 for p in icons:
  im=Image.open(p);assert im.size==(64,64) and im.mode=='RGBA';assert im.getpixel((0,0))[3]==0
 sprites=read('data/sprite_import.json')['sprites'];assert len(sprites)==11
 for s in sprites:
  im=Image.open(ROOT/s['file']);assert list(im.size)==s['size_px'] and im.mode=='RGBA'
  left,bottom,right,top=s['border_lbrt_px'];assert left+right<im.width and top+bottom<im.height
  assert not s['text_baked_in'] and im.getpixel((0,0))[3]==0
 local=read('data/localization_en_es.json')['strings'];assert len({r['key'] for r in local})==len(local)
 samples={'player':'Alex','count':'4','minutes':'10','percent':'150','release':'Q'};fit=[]
 for r in local:
  assert set(re.findall(r'\{(.*?)\}',r['en']))==set(re.findall(r'\{(.*?)\}',r['es_UY'])),r['key']
  f=ImageFont.truetype(str(ROOT/'assets/fonts/DejaVuSans.ttf'),r['font_reference_px'])
  for lang in ['en','es_UY']:
   text=r[lang].format(**samples);lines=wrapped_count(text,f,r['max_width_reference_px'])
   assert lines<=r['max_lines'],(r['key'],lang,lines,r['max_lines'])
   fit.append({'key':r['key'],'locale':lang,'reference_lines':lines})
 with (ROOT/'data/ui_acceptance.csv').open(encoding='utf-8-sig',newline='') as f:qa=list(csv.DictReader(f))
 assert len(qa)==36 and all(r['status']=='NOT_RUN' for r in qa)
 tokens=read('data/design_tokens.json');co=tokens['colors'];pairs=[('text','primary'),('text','panel'),('muted_text','panel'),('focus','ink'),('danger_text','danger_panel')]
 ratios={a+'/'+b:round(contrast(co[a],co[b]),2) for a,b in pairs};assert all(v>=4.5 for v in ratios.values()),ratios
 result={'status':'RESOURCE_CHECKS_PASSED','screens':12,'icons':20,'sprites':11,'localized_keys':len(local),'nominal_text_fits_checked':len(fit),'svg_xml_and_linked_assets':'PASS','transparent_assets':'PASS','text_bounds_reference_only':'PASS','color_pair_nominal_contrast':ratios,'font_notice_present':(ROOT/'assets/fonts/FONT_LICENSE.txt').is_file(),'not_tested':['Unity import and SDF font generation','Runtime UI, controller navigation and accessibility','Network state handling in the real game','Actual text layout at 720p/ultrawide/150%','Gameplay or playtests'],'acceptance_cases_not_run':len(qa),'note':'Nominal fit uses provided sample values and reference budgets. This is not proof of final runtime layout or WCAG compliance.'}
 assert result['font_notice_present']
 (ROOT/'data/validation_report.json').write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n')
 print(json.dumps(result,indent=2,ensure_ascii=False))
if __name__=='__main__':main()
