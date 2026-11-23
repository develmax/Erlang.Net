import hashlib,json,sys,zipfile
from pathlib import Path
s,p,n=map(Path,sys.argv[1:])
def read(f): return json.loads(f.read_bytes())
def sha(b): return hashlib.sha256(b).hexdigest()
a=read(p/'source-hashes.json')
b=read(s/'source-hashes.json')
t=read(s/'hydration-receipt.json')
expect=a|t['CandidateChangedPaths']
formatted={x['Path']:x['Sha256'].lower() for x in read(s/'source-identities.json')}
registration='tests/Erlang.Tests/Harness/Program.cs'
assert {k for k in b if b[k]!=expect.get(k)}=={registration}
assert formatted[registration]==b[registration]
assert sha((s/'layout'/registration).read_bytes())==b[registration]
expect[registration]=formatted[registration]
assert expect==b
for x in t.get('CandidateRemovedPaths',{}): expect.pop(x)
with zipfile.ZipFile(s/'source.zip') as z:
 assert set(z.namelist())==set(b) and len(z.namelist())==len(b)
 for k,v in b.items():
  vals=(v,expect.get(k),sha(z.read(k)),sha((n/k).read_bytes()))
  assert len(set(vals))==1,(k,vals)
print(len(b))

