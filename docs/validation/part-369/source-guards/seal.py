from pathlib import Path
import hashlib,json
base=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
files=[p for p in sorted(base.rglob('*')) if p.is_file() and p.name!='source-freeze.json']
p=base/'source-freeze.json';p.write_text(json.dumps({'Status':'reviewed guard successor; Root applies, final empirical gates remain pending','Files':[{'Path':q.relative_to(base).as_posix(),'Sha256':sha(q),'Bytes':q.stat().st_size} for q in files]},indent=2)+'\n',encoding='utf-8',newline='\n')
print('freeze',sha(p));print('guards',sha(base/'guarded-files-proposal.json'));print('review',sha(base/'review.json'))
