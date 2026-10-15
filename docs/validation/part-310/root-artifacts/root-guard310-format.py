from pathlib import Path
import hashlib,json
r=Path.cwd();d=r/'artifacts/root-part310-combined-isolated';c=d/'candidate';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
before=d/'tokens-before.json';after=d/'tokens-after.json';b=read(before);a=read(after)
assert b==a and len(a)==20 and all(x['Tokens'] for x in a)
files=[]
for row in a:
 p=Path(row['Path']);assert p.is_relative_to(c);data=p.read_bytes();files.append({'Path':p.relative_to(c).as_posix(),'Bytes':len(data),'Sha256':sha(data)})
out=d/'format-token-guard.json';assert not out.exists();out.write_text(json.dumps({'Status':'TWENTY_CSHARP_TOKEN_STREAMS_EXACT_AFTER_FORMAT','CSharpPaths':20,'BeforeSha256':sha(before.read_bytes()),'AfterSha256':sha(after.read_bytes()),'Files':files},indent=2)+'\n',encoding='utf-8');print(json.dumps({'CSharp':20,'TokensExact':True,'GuardSha256':sha(out.read_bytes())}))
