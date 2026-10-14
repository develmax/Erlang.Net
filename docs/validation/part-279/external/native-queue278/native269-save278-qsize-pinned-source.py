from pathlib import Path
import subprocess,hashlib
pin='ad05823719d77c8faee87348ea39513d4e2f99c5';d=Path('artifacts/native269-queue278-pinned-qsize-source');d.mkdir(exist_ok=True)
for name in ['dist.c','dist.h','erl_driver.h','erl_binary.h']:
 p='erts/emulator/beam/'+name
 if name=='erl_driver.h':p='erts/emulator/beam/erl_driver.h'
 try:b=subprocess.check_output(['git','-C','../otp','cat-file','blob',pin+':'+p],stderr=subprocess.PIPE)
 except subprocess.CalledProcessError as e:print(name,'not found at',p);continue
 (d/name).write_bytes(b);print(name,len(b),hashlib.sha256(b).hexdigest().upper())
