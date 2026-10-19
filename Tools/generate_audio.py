#!/usr/bin/env python3
"""Original deterministic synthesis. No samples, recordings or external melodies.
Requires numpy and ffmpeg. All generated audio is dedicated to CC0-1.0.
"""
from pathlib import Path
import numpy as np
import wave, subprocess, tempfile
ROOT=Path(__file__).resolve().parents[1]/'KleinesSpiel/Assets/Audio'
SR=22050
rng=np.random.default_rng(721)
def tone(freq, duration, voice='bell'):
 t=np.arange(int(SR*duration))/SR
 if voice=='pad':
  v=sum(np.sin(2*np.pi*freq*h*t)/(h*h) for h in [1,2,3,4])
  e=np.minimum(t/.3,1)*np.minimum((duration-t)/.6,1)
 elif voice=='pluck':
  v=np.sin(2*np.pi*freq*t)+.35*np.sin(2*np.pi*freq*2*t)+.15*np.sin(2*np.pi*freq*3*t)
  e=(1-np.exp(-t*100))*np.exp(-t*4/duration)
 elif voice=='drum':
  v=np.sin(2*np.pi*(freq*t+40*.04*(1-np.exp(-t/.04))))+.18*rng.uniform(-1,1,len(t))*np.exp(-t*25)
  e=(1-np.exp(-t*200))*np.exp(-t*14)
 else:
  v=np.sin(2*np.pi*freq*t)+.4*np.sin(2*np.pi*freq*2.006*t)*np.exp(-t*4)+.14*np.sin(2*np.pi*freq*3*t)
  e=(1-np.exp(-t*140))*np.exp(-t*3/duration)
 return (v*e).astype(np.float32)
def save(name,data,ogg=False):
 data=np.asarray(data,dtype=np.float32)
 data=data/(max(float(np.max(np.abs(data))),.01))*0.62
 pcm=(data*32767).astype('<i2')
 with tempfile.NamedTemporaryFile(suffix='.wav') as f:
  with wave.open(f.name,'wb') as w:
   w.setnchannels(2 if data.ndim==2 else 1);w.setsampwidth(2);w.setframerate(SR);w.writeframes(pcm.tobytes())
  if ogg:
   subprocess.run(['ffmpeg','-y','-loglevel','error','-i',f.name,'-c:a','libvorbis','-q:a','4',str(ROOT/(name+'.ogg'))],check=True)
  else: (ROOT/(name+'.wav')).write_bytes(Path(f.name).read_bytes())
def music(name,root,bpm,theme,variant):
 beat=60/bpm;n=int(64*beat*SR);out=np.zeros((n,2),np.float32)
 def add(at,note,dur,gain,voice,pan=0):
  v=tone(440*2**((note-69)/12),dur*beat,voice)*gain
  i=int(at*beat*SR);idx=(np.arange(len(v))+i)%n
  out[idx,0]+=v*(.7-pan*.25);out[idx,1]+=v*(.7+pan*.25)
 chords=[0,5,3,7] if theme=='crypt' else [0,8,5,7]
 motifs=[[0,7,10,7,3,2,7,5],[0,3,7,12,10,7,5,2],[7,0,3,10,12,7,2,5],[0,12,7,3,10,5,7,2]]
 motif=motifs[variant%4]
 boss=variant>0
 for bar in range(16):
  chord=chords[(bar//2)%4];at=bar*4
  for interval in [0,7,12,15]:add(at,root+chord+interval,4,.11,'pad',interval/15-.5)
  add(at,root+chord-12,3.8,.24,'pad')
  for j in range(8 if boss else 4):
   step=.5 if boss else 1
   note=root+12+chord+motif[(j+bar+variant)%8]
   add(at+j*step,note,.7 if boss else 1.6,.12 if boss else .14,'pluck' if theme=='castle' else 'bell',(-1)**j*.6)
  for pulse in ([0,1.5,2,3] if boss else [0,2]):add(at+pulse,35+variant,.35,.17 if boss else .075,'drum')
  if bar%4==3:add(at+3,root+31,1,.08,'bell')
 # Circular echoes keep the authored 16-bar loop continuous.
 out+=np.roll(out,int(.375*beat*SR),axis=0)*.18+np.roll(out,int(.75*beat*SR),axis=0)*.09
 save(name,out,True)
def ambience(name, root, bpm, castle, variant):
 beat=60/bpm;n=int(64*beat*SR);out=np.zeros((n,2),np.float32)
 def add(at,note,dur,gain,voice,pan=0):
  v=tone(440*2**((note-69)/12),dur*beat,voice)*gain
  idx=(np.arange(len(v))+int(at*beat*SR))%n
  out[idx,0]+=v*(.7-pan*.2);out[idx,1]+=v*(.7+pan*.2)
 # Each theme uses its own instrumentation, harmony, density and phrasing.
 crypt_motifs=[[0,1,7,3,10,7,1,0],[0,7,6,3,1,3,7,0],[7,10,8,7,3,1,0,7]]
 castle_motifs=[[0,7,3,5,10,7,5,2],[7,3,0,2,5,3,7,10],[0,5,7,10,7,3,2,0]]
 motif=(castle_motifs if castle else crypt_motifs)[variant]
 progression=([0,5,7,0],[0,8,5,7],[0,3,5,7])[variant] if castle else ([0,1,0,6],[0,6,1,0],[0,8,6,1])[variant]
 for bar in range(16):
  at=bar*4;chord=progression[(bar//2)%4]
  add(at,root+chord-12,4,.18 if castle else .3,'pad')
  for interval in ([0,3,7,12] if castle else [0,7,13]):
   add(at,root+chord+interval,4,.105 if castle else .075,'pad',interval/12-.5)
  if castle:
   # Measured, minor-key court dance: plucked strings over bowed/organ-like pads.
   for j in range(4):add(at+j,root+12+motif[(bar+j+variant)%8],.85,.15,'pluck',(-1)**j*.5)
   add(at,33+variant,.4,.075,'drum')
   if bar%2==1:add(at+2,40,.25,.055,'drum')
  else:
   # Sparse low bells and unresolved intervals, cavernous decay and drone.
   if bar%2==0:add(at+1,root+12+motif[(bar//2+variant)%8],3,.11,'bell',-.6)
   add(at+3,root+motif[(bar+variant)%8],1.5,.07,'bell',.6)
   if bar%4==0:add(at,28,.6,.065,'drum')
 out+=np.roll(out,int(beat*SR*.75),axis=0)*.24+np.roll(out,int(beat*SR*1.5),axis=0)*.12
 save(name,out,True)

ROOT.mkdir(parents=True,exist_ok=True)
for args in [('crypt_rooms',38,64,False,0),('crypt_ossuary',41,70,False,1),('crypt_depths',36,60,False,2),
             ('castle_rooms',50,82,True,0),('castle_gallery',48,88,True,1),('castle_watch',52,94,True,2)]:
 ambience(*args)
for args in [('crypt_melee',43,112,'crypt',1),('crypt_archer',47,116,'crypt',2),('crypt_ritual',42,108,'crypt',3),
 ('castle_archer',50,124,'castle',2),('castle_warden',45,120,'castle',3),('castle_champion',48,128,'castle',1),('regent',38,136,'castle',4)]:music(*args)
for name,dur,f0,f1 in [('arrow',.13,1500,650),('enemy_cast',.18,330,180),('impact',.09,220,100),('melee',.13,160,65),('hurt',.18,290,170),('enemy_death',.22,180,70),('death',.6,340,125),('charge',.45,120,270)]:
 t=np.arange(int(dur*SR))/SR
 freq=f0+(f1-f0)*t/dur
 v=np.sin(2*np.pi*np.cumsum(freq)/SR)*.65
 if name in ['arrow','impact','melee']:v+=rng.uniform(-.5,.5,len(t))
 v*=np.minimum(t/.012,1)*np.maximum(0,1-t/dur)**2
 save(name,v)
# Rounded, low-energy staff pulse with no sharp high-frequency sweep.
t=np.arange(int(.16*SR))/SR
v=(np.sin(2*np.pi*(380*t-110*t*t))+.13*np.sin(2*np.pi*570*t))*np.sin(np.pi*t/.16)**2*np.exp(-t*9)
save('cast',v)
coin=np.zeros(int(.28*SR),np.float32)
for start,note in [(0,76),(.085,81)]:
 v=tone(440*2**((note-69)/12),.18,'bell')*.35;i=int(start*SR);coin[i:i+len(v)]+=v
save('coin',coin)
t=np.arange(int(.6*SR))/SR
noise=np.convolve(rng.uniform(-1,1,len(t)),np.ones(35)/35,mode='same')
v=(np.sin(2*np.pi*(100*t+70*t*t))+.25*np.sin(2*np.pi*220*t)+noise*.5)*np.sin(np.pi*t/.6)**2
save('summon',v)
t=np.arange(int(.75*SR))/SR
phase=2*np.pi*np.cumsum(76+8*np.sin(2*np.pi*4*t))/SR
v=(np.sin(phase)+.45*np.sin(2*phase)+.2*np.sin(3*phase))*(.65+.35*np.sin(2*np.pi*22*t))*np.sin(np.pi*t/.75)**2
save('zombie',v)
print('Created 13 original looping tracks and 12 short effects.')
