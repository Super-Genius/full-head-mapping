import numpy as np
import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch
from pathlib import Path

OUT = Path(__file__).parent / 'figures'
OUT.mkdir(parents=True, exist_ok=True)

def box(ax, xy, w, h, text, fc='#edf2f7', ec='#334155', fs=9):
    p = FancyBboxPatch(xy, w, h, boxstyle='round,pad=0.02,rounding_size=0.02', facecolor=fc, edgecolor=ec, linewidth=1.2)
    ax.add_patch(p)
    ax.text(xy[0]+w/2, xy[1]+h/2, text, ha='center', va='center', fontsize=fs)
    return p

def arrow(ax, a, b):
    ax.add_patch(FancyArrowPatch(a, b, arrowstyle='-|>', mutation_scale=12, color='#475569', linewidth=1.2))

# Figure 1: historical pipeline
fig, ax = plt.subplots(figsize=(11, 4.3))
ax.set_xlim(0, 11); ax.set_ylim(0, 4.3); ax.axis('off')
box(ax,(0.2,2.7),1.6,0.8,'Orthogonal photos\nfront / left / right')
box(ax,(2.2,2.7),1.7,0.8,'Planar UVW\nprojections')
box(ax,(4.3,2.7),1.7,0.8,'Triangle normal\n· view direction')
box(ax,(6.4,2.7),1.7,0.8,'Select best\nsource view')
box(ax,(8.5,2.7),1.9,0.8,'Destination\ncylindrical UV')
for a,b in [((1.8,3.1),(2.2,3.1)),((3.9,3.1),(4.3,3.1)),((6.0,3.1),(6.4,3.1)),((8.1,3.1),(8.5,3.1))]: arrow(ax,a,b)
box(ax,(4.0,0.7),2.3,0.9,'Hard source boundaries\nvisible seams',fc='#fff7ed',ec='#9a3412')
box(ax,(7.0,0.7),2.8,0.9,'Gaussian + Laplacian pyramids\nBurt–Adelson spline blend',fc='#ecfdf5',ec='#166534')
arrow(ax,(7.3,2.7),(5.2,1.6)); arrow(ax,(6.3,1.15),(7.0,1.15)); arrow(ax,(8.4,1.6),(9.2,2.7))
ax.text(5.5,4.0,'Historical Full Head Mapping workflow (reconstructed from the 2002 description)',ha='center',fontsize=12,weight='bold')
fig.tight_layout(); fig.savefig(OUT/'historical_pipeline.png',dpi=220,bbox_inches='tight'); plt.close(fig)

# Figure 2: modern pipeline
fig, ax = plt.subplots(figsize=(12, 5.0))
ax.set_xlim(0, 12); ax.set_ylim(0, 5); ax.axis('off')
box(ax,(0.3,3.2),1.5,0.8,'Multi-view\nphotos')
box(ax,(2.2,3.2),1.7,0.8,'Camera alignment\nmanual or landmarks')
box(ax,(4.3,3.2),1.5,0.8,'UV-mapped\nhead mesh')
box(ax,(6.2,3.2),1.7,0.8,'Per-texel\nprojection')
box(ax,(8.3,3.2),1.6,0.8,'Confidence\nweights')
box(ax,(10.3,3.2),1.4,0.8,'Multiband\nblend')
for a,b in [((1.8,3.6),(2.2,3.6)),((3.9,3.6),(4.3,3.6)),((5.8,3.6),(6.2,3.6)),((7.9,3.6),(8.3,3.6)),((9.9,3.6),(10.3,3.6))]: arrow(ax,a,b)
box(ax,(2.0,0.8),2.3,0.9,'Optional geometry provider\nFLAME / DECA / Meshy / scan',fc='#f5f3ff',ec='#6d28d9')
box(ax,(5.0,0.8),2.1,0.9,'Observed-region\nprovenance map',fc='#eff6ff',ec='#1d4ed8')
box(ax,(7.8,0.8),2.0,0.9,'Optional fill only\nin unseen regions',fc='#fff7ed',ec='#9a3412')
box(ax,(10.3,0.8),1.4,0.9,'Unity-ready\ntextures',fc='#ecfdf5',ec='#166534')
arrow(ax,(3.15,1.7),(5.0,3.2)); arrow(ax,(11.0,3.2),(6.0,1.7)); arrow(ax,(7.1,1.25),(7.8,1.25)); arrow(ax,(9.8,1.25),(10.3,1.25))
ax.text(6,4.6,'Modernized pipeline: deterministic projection first, optional AI second',ha='center',fontsize=12,weight='bold')
fig.tight_layout(); fig.savefig(OUT/'modern_pipeline.png',dpi=220,bbox_inches='tight'); plt.close(fig)

# Figure 3: weighting curves
angles = np.linspace(0, 90, 361)
c = np.cos(np.deg2rad(angles))
fig, ax = plt.subplots(figsize=(7.6,4.6))
for g in [0.5,1,2,4]:
    ax.plot(angles, np.maximum(c,0)**g, label=fr'$\gamma={g}$')
ax.set_xlabel('Angle between surface normal and camera direction (degrees)')
ax.set_ylabel('Angular confidence')
ax.set_title(r'View weighting term $\max(0,N\cdot V)^\gamma$')
ax.grid(alpha=.25); ax.legend(ncol=2); ax.set_xlim(0,90); ax.set_ylim(0,1.03)
fig.tight_layout(); fig.savefig(OUT/'view_weighting.png',dpi=220,bbox_inches='tight'); plt.close(fig)

# Synthetic UV atlas and two view observations to illustrate blending.
w,h=720,360
x=np.linspace(-1,1,w); y=np.linspace(-1,1,h); X,Y=np.meshgrid(x,y)
mask=(X/0.92)**2+(Y/0.93)**2 < 1
base=np.zeros((h,w,3),float)
base[:]=[0.09,0.08,0.075]
skin=np.stack([
    0.57+0.08*np.cos(2.0*X)+0.03*np.sin(7*Y),
    0.36+0.05*np.cos(1.5*X),
    0.29+0.04*np.sin(4*X+2*Y)],axis=2)
# stylized features
features=np.zeros((h,w))
for cx in (-0.28,0.28):
    features += np.exp(-(((X-cx)/0.085)**2+((Y-0.15)/0.045)**2)*3)
features += .6*np.exp(-((X/0.035)**2+((Y+0.02)/0.22)**2)*2)
features += .8*np.exp(-((X/0.25)**2+((Y+0.35)/0.045)**2)*4)
skin -= features[...,None]*np.array([0.22,0.18,0.16])
base[mask]=np.clip(skin[mask],0,1)
left=base.copy(); right=base.copy()
left[...,0]*=1.08; left[...,1]*=.98; left[...,2]*=.95
right[...,0]*=.92; right[...,1]*=1.05; right[...,2]*=1.08
# Each synthetic camera becomes poor toward the opposite side.
wl=np.clip((0.35-X)/1.25,0,1)*mask
wr=np.clip((X+0.35)/1.25,0,1)*mask
# hard best view
hard=np.where((wl>=wr)[...,None],left,right); hard[~mask]=base[~mask]
weighted=(left*wl[...,None]+right*wr[...,None])/(wl[...,None]+wr[...,None]+1e-7); weighted[~mask]=base[~mask]

# compact pyramid blender for demo
from scipy.ndimage import gaussian_filter, zoom

def pyr_down(a):
    if a.ndim==3:
        return gaussian_filter(a,(1,1,0))[::2,::2]
    return gaussian_filter(a,1)[::2,::2]
def resize(a, shape):
    if a.ndim==3:
        z=(shape[0]/a.shape[0], shape[1]/a.shape[1], 1)
    else: z=(shape[0]/a.shape[0],shape[1]/a.shape[1])
    b=zoom(a,z,order=1)
    return b[:shape[0],:shape[1]]

def multiband(imgs, weights, levels=6):
    gps=[]; lps=[]
    for im,wt in zip(imgs,weights):
        gi=[im]; gw=[wt]
        for _ in range(levels-1):
            gi.append(pyr_down(gi[-1])); gw.append(pyr_down(gw[-1]))
        lp=[]
        for j in range(levels-1):
            lp.append(gi[j]-resize(gi[j+1],gi[j].shape[:2]))
        lp.append(gi[-1]); lps.append(lp); gps.append(gw)
    out=[]
    for level in range(levels):
        num=np.zeros_like(lps[0][level]); den=np.zeros(lps[0][level].shape[:2])
        for i in range(len(imgs)):
            m=gps[i][level]
            num += lps[i][level]*m[...,None]; den += m
        out.append(num/(den[...,None]+1e-7))
    cur=out[-1]
    for j in range(levels-2,-1,-1): cur=out[j]+resize(cur,out[j].shape[:2])
    return np.clip(cur,0,1)
mb=multiband([left,right],[wl,wr],6); mb[~mask]=base[~mask]

fig,axs=plt.subplots(2,3,figsize=(12,6.2))
items=[(left,'Synthetic left view'),(right,'Synthetic right view'),(np.stack([wl]*3,2),'Left confidence'),(hard,'Hard best-view selection'),(weighted,'Weighted blend'),(mb,'Multiband blend')]
for ax,(im,t) in zip(axs.ravel(),items):
    ax.imshow(np.clip(im,0,1)); ax.set_title(t,fontsize=10); ax.axis('off')
fig.suptitle('Controlled illustration of seam formation and blending (synthetic data)',fontsize=12,weight='bold')
fig.tight_layout(); fig.savefig(OUT/'synthetic_blending_demo.png',dpi=220,bbox_inches='tight'); plt.close(fig)

# Figure 5: provenance concept
fig,ax=plt.subplots(figsize=(9,3.4))
prov=np.zeros((180,520,3)); xx=np.linspace(-1,1,520); yy=np.linspace(-1,1,180); XX,YY=np.meshgrid(xx,yy); m=(XX/.95)**2+(YY/.9)**2<1
leftdom=(XX<-.12)&m; rightdom=(XX>.12)&m; mix=(~leftdom)&(~rightdom)&m
prov[leftdom]=[0.18,0.55,0.90]; prov[rightdom]=[0.95,0.43,0.20]; prov[mix]=[0.55,0.35,0.75]
ax.imshow(prov); ax.axis('off'); ax.set_title('Provenance concept: blue = left view, orange = right view, purple = overlap; black = unseen')
fig.tight_layout(); fig.savefig(OUT/'provenance_concept.png',dpi=220,bbox_inches='tight'); plt.close(fig)
