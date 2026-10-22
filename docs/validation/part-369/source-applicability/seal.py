from pathlib import Path
import hashlib,json
base=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
files=[p for p in sorted(base.rglob('*')) if p.is_file() and p.name!='source-freeze.json']
p=base/'source-freeze.json';p.write_text(json.dumps({'Status':'conditional source audit; final Root gates pending','Files':[{'Path':f.relative_to(base).as_posix(),'Sha256':sha(f),'Bytes':f.stat().st_size} for f in files]},indent=2)+'\n',encoding='utf-8',newline='\n')
print(sha(p))
