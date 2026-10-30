from pathlib import Path
from PIL import Image
import numpy as np

ROOT = Path(r"C:\Học\ThucTap\Assets\Phu\Sprite asset\Velvet Animations")
GEN = Path(r"C:\Users\LENOVO\.codex\generated_images\01a11eb7-efe2-7500-9b62-e25dd0886fd4")
SIZE = (384, 480)

DANCE = [
"43e8008f-c24c-4715-9f69-2f537f0c3069","e60a1132-ac03-4b55-9921-d7e789d33e41","3a3d8d5b-c2b0-4e95-99f9-37c10b228883",
"1278db57-e3aa-47d1-b35e-3dc883cf577b","2c809d5a-09a9-4ceb-95fb-3b226fce216f","6890d559-af2f-4544-b86b-096d7101ece4",
"24969964-6e7d-4d9a-9e55-2e17a9b93814","fe5a87e3-639f-418e-b67d-f4afa73be785","a722920c-f1fa-4ed9-89ba-6b7923df992e",
"6e51a5c6-0d7f-408a-9ad8-03a615d6e594"]
IDLE = ["b187dbad-ebbc-4185-8fd3-198cbecba3ba","e81d968c-d329-4cde-9124-bde156c2ee72","689299d7-c506-4971-a106-b3511a614b72","02da0183-a7a1-41c7-934f-d9f58743d2d7"]
MISS = ["49ea5a46-b7da-43ad-a26a-1031e24f6898","876afc4f-4b6d-42fb-a466-8706ae5825ee","c63ff7ec-a0d7-409f-a59e-2a2d8e2c4bb6","5f39f2aa-7317-4158-be0d-1c612f13b50a"]

def normalize(im, margin=20):
    im=im.convert("RGBA"); box=im.getchannel("A").getbbox()
    if not box:return Image.new("RGBA",SIZE)
    im=im.crop(box); scale=min((SIZE[0]-2*margin)/im.width,(SIZE[1]-2*margin)/im.height)
    im=im.resize((max(1,round(im.width*scale)),max(1,round(im.height*scale))),Image.Resampling.LANCZOS)
    out=Image.new("RGBA",SIZE); out.alpha_composite(im,((SIZE[0]-im.width)//2,SIZE[1]-margin-im.height)); return out

def clean_edge_scraps(im):
    im=im.convert("RGBA"); a=im.getchannel("A"); px=a.load(); w,h=a.size
    seen=set(); comps=[]
    for y in range(h):
        for x in range(w):
            if px[x,y]<=8 or (x,y) in seen:continue
            q=[(x,y)]; seen.add((x,y)); comp=[]; edge=False
            while q:
                cx,cy=q.pop(); comp.append((cx,cy)); edge |= cx<=1 or cx>=w-2
                for nx,ny in ((cx-1,cy),(cx+1,cy),(cx,cy-1),(cx,cy+1)):
                    if 0<=nx<w and 0<=ny<h and px[nx,ny]>8 and (nx,ny) not in seen:
                        seen.add((nx,ny)); q.append((nx,ny))
            comps.append((comp,edge))
    if not comps:return im
    main=max(comps,key=lambda ce:len(ce[0]))[0]
    xs=[p[0] for p in main]; ys=[p[1] for p in main]
    box=(min(xs)-24,min(ys)-24,max(xs)+24,max(ys)+24); keep=set(main)
    for comp,edge in comps:
        if comp is main or len(comp)<18:continue
        cx=sum(x for x,y in comp)//len(comp); cy=sum(y for x,y in comp)//len(comp)
        if box[0]<=cx<=box[2] and box[1]<=cy<=box[3]:keep.update(comp)
    mask=Image.new("L",(w,h)); mp=mask.load()
    for p in keep:mp[p]=px[p]
    out=im.copy(); out.putalpha(mask); return out

def split3(code):
    im=Image.open(GEN/f"exec-{code}.png").convert("RGBA"); w,h=im.size
    alpha=np.asarray(im.getchannel("A"))>8; active=alpha.any(axis=0)
    runs=[]; start=None
    for x,on in enumerate(active):
        if on and start is None:start=x
        if start is not None and (not on or x==w-1):
            end=x if not on else x+1
            if end-start>80:runs.append((start,end))
            start=None
    if len(runs)!=3:
        runs=[(round(i*w/3),round((i+1)*w/3)) for i in range(3)]
    return [normalize(clean_edge_scraps(im.crop((max(0,l-6),0,min(w,r+6),h)))) for l,r in runs]

def tween(a,b,t):
    aa=np.asarray(a,dtype=np.float32)/255.; bb=np.asarray(b,dtype=np.float32)/255.; aA=aa[...,3:4]; bA=bb[...,3:4]
    outA=aA*(1-t)+bA*t; prem=aa[...,:3]*aA*(1-t)+bb[...,:3]*bA*t
    rgb=np.divide(prem,outA,out=np.zeros_like(prem),where=outA>1e-5)
    return Image.fromarray(np.clip(np.concatenate([rgb,outA],axis=2)*255,0,255).astype(np.uint8),"RGBA")

def sheet(frames,path,cols):
    out=Image.new("RGBA",(SIZE[0]*cols,SIZE[1]*((len(frames)+cols-1)//cols)))
    for i,f in enumerate(frames):out.alpha_composite(f,((i%cols)*SIZE[0],(i//cols)*SIZE[1]))
    out.save(path)

def save_set(name,frames,cols,ms):
    base=ROOT/name; d=base/"Frames"; d.mkdir(parents=True,exist_ok=True)
    for f in d.glob("*.png"):f.unlink()
    for i,f in enumerate(frames,1):f.save(d/f"{name.lower()}_{i:03d}.png")
    sheet(frames,base/f"velvet_{name.lower()}_{len(frames)}.png",cols)
    frames[0].save(base/f"velvet_{name.lower()}_{len(frames)}_preview.gif",save_all=True,append_images=frames[1:],duration=ms,loop=0,disposal=2,optimize=False)

keys=[]
for code in DANCE:keys.extend(split3(code))
kd=ROOT/"Dance"/"Key Poses"; kd.mkdir(parents=True,exist_ok=True)
for f in kd.glob("*.png"):f.unlink()
for i,f in enumerate(keys,1):f.save(kd/f"dance_key_{i:03d}.png")
sheet(keys,ROOT/"Dance"/"velvet_dance_keyposes_30.png",10)
dance=[]
for i,a in enumerate(keys):
    # Keep every frame fully opaque. Repeating each clean key pose preserves the
    # requested 120-frame clip without cross-fade ghosts or disappearing sprites.
    dance.extend([a.copy(),a.copy(),a.copy(),a.copy()])
save_set("Dance",dance,10,22)
idle=[]
for code in IDLE:idle.extend(split3(code))
save_set("Idle",idle,6,100)
miss=[]
for code in MISS:miss.extend(split3(code))
save_set("Miss",miss,6,90)
print(f"Dance={len(dance)} Keys={len(keys)} Idle={len(idle)} Miss={len(miss)}")
