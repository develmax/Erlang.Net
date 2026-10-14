from pathlib import Path
import subprocess,json,hashlib
p=Path('artifacts/native269-queue278-pinned-qsize-source');pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
for n in ['external.c','erl_node_tables.h','erl_drv_nif.h']:(p/n).write_bytes(subprocess.check_output(['git','-C','../otp','cat-file','blob',pin+':erts/emulator/beam/'+n]))
rows=[{'Path':str(f).replace('\\','/'),'Bytes':f.stat().st_size,'Sha256':hashlib.sha256(f.read_bytes()).hexdigest().upper()} for f in sorted(p.iterdir())]
Path('artifacts/native269-queue278-qsize-source-bindings.json').write_bytes((json.dumps({'Pin':pin,'Files':rows,'NativeRerun':False,'SdkParityClaim':False},indent=2)+'\n').encode())
