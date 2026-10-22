from pathlib import Path
import subprocess,hashlib,json
root=Path(__file__).resolve().parents[2]
base=Path(__file__).resolve().parent
auth=base/'authority';auth.mkdir(exist_ok=True)
paths=['copy.c','erl_message.c','erl_proc_sig_queue.c','external.c','io.c','erl_io_queue.c','erl_io_queue.h','erl_nif.c','erl_bits.c','erl_trace.c']
rows=[]
for name in paths:
    path='erts/emulator/beam/'+name
    result=subprocess.run(['git','-C',str(root.parent/'otp'),'show','ad05823719d77c8faee87348ea39513d4e2f99c5:'+path],capture_output=True,check=True)
    p=auth/name;p.write_bytes(result.stdout)
    rows.append({'Path':path,'Sha256':hashlib.sha256(p.read_bytes()).hexdigest().upper(),'Bytes':p.stat().st_size})
(base/'authority.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
