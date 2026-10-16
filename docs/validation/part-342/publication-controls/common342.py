"""Draft passive342 delivery contracts. No build/network; execution requires reviewed actual input hashes."""
from pathlib import Path
import hashlib,json
PARENT='7b43e00608c41303c1b03a25ebd5385d12517ee5'
INTEGRATIONS={'Readiness':'Passed','PackagedReadiness':'Passed','Build':'Passed','Tests':'Passed','OracleUnavailableReport':'Expected failure recorded','HybridOutput':'Hello World','Incremental':'Passed','DisabledPreprocessing':'Expected failure','CleanRebuild':'Passed','PackageConsumer':'Passed','LocalTool':'Passed'}
def read(p):return json.loads(Path(p).read_bytes())
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def save(p,v):
 with Path(p).open('x',encoding='utf-8',newline='\n') as f:json.dump(v,f,indent=2);f.write('\n')
def checked(root,name):
 assert isinstance(name,str) and not Path(name).is_absolute() and ':' not in name and '\\' not in name and all(v not in ('','.','..') for v in name.split('/')),name
 p=(root/name).resolve(strict=True);assert p.is_relative_to(root) and p.is_file(),name;return p
def rows(path):
 data=read(path);return data['Files'] if isinstance(data,dict) else data
def exact(root,path):
 seen=set()
 for r in rows(path):
  assert r['Path'] not in seen;seen.add(r['Path']);p=checked(root,r['Path']);assert p.stat().st_size==r['Bytes'] and sha(p)==r['Sha256'],r['Path']
 return seen
def key(r):return r['Module'],r['Function'],r['Arity']
def ledger(root,baseline):
 l=read(root/'docs/mfa-compatibility.json');entries=l['Entries'];assert len(entries)==len({key(x) for x in entries})==587
 assert {s:sum(x['Status']==s for x in entries) for s in {x['Status'] for x in entries}}==baseline['CompatibilityCounts']
 assert l['Tag']=='OTP-29.1.1' and l['Commit']=='ad05823719d77c8faee87348ea39513d4e2f99c5'
 for r in l['VerifiedSourceFiles']:assert sha(checked(root,r['Path']))==r['Sha256']
 return l
