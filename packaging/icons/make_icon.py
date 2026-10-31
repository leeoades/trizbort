from PIL import Image, ImageDraw
S=1024; SS=4; W=S*SS
im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im)
def r(v): return int(v*SS)
# tile
m=r(40); d.rounded_rectangle([m,m,W-m,W-m],radius=r(200),fill=(38,70,96,255))
line=(236,226,196,255); room=(248,242,222,255); edge=(20,38,54,255)
lw=r(34)
def conn(a,b): d.line([r(a[0]),r(a[1]),r(b[0]),r(b[1])],fill=line,width=lw)
def rm(x,y,w,h):
    d.rounded_rectangle([r(x),r(y),r(x+w),r(y+h)],radius=r(26),fill=room,outline=edge,width=r(16))
# rooms: big TL, small TR, small BL, big BR
TL=(170,190,300,250); TR=(630,215,210,200); BL=(190,600,210,200); BR=(560,560,300,270)
# connections (drawn first)
conn((470,315),(630,315))            # TL - TR
conn((320,440),(295,600))            # TL - BL
conn((400,700),(560,700))            # BL - BR
conn((735,415),(735,560))            # TR - BR
for x,y,w,h in (TL,TR,BL,BR): rm(x,y,w,h)
im=im.resize((S,S),Image.LANCZOS)
im.save('trizbort-recast-1024.png')
sizes=[16,24,32,48,64,128,256]
im.save('trizbort-recast.ico',sizes=[(s,s) for s in sizes])
im.save('trizbort-recast.icns')
for s in (256,512): im.resize((s,s),Image.LANCZOS).save(f'trizbort-recast-{s}.png')
im.resize((32,32),Image.LANCZOS).resize((256,256),Image.NEAREST).save('preview32.png')
