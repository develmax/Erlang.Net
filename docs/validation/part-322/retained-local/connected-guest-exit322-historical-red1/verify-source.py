from pathlib import Path
import json,hashlib
r=Path.cwd();d=Path(__file__).resolve().parent;sha=lambda z:hashlib.sha256(z).hexdigest().upper();f=json.loads((d/'source-freeze.json').read_bytes());cl=d/'source-resource-closure.json';assert sha(cl.read_bytes())==f['FinalSourceClosureSha256'];rows=json.loads(cl.read_bytes())['Files'];assert len(rows)==2309
for x in rows:
 z=(d/'candidate'/x['Path']).read_bytes();assert len(z)==x['Bytes'] and sha(z)==x['Sha256'],x['Path']
binding=json.loads((d/'runner-binding.json').read_bytes());assert sha((r/binding['Path']).read_bytes())==binding['Sha256']
print(json.dumps({'Passed':True,'SourceRows':2309,'NoSdkExecuted':True}))
