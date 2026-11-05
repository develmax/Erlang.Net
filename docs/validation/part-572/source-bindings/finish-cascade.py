import json,hashlib,re
from pathlib import Path
R=Path.cwd();A=R/'docs/validation/part-572/source-bindings';audit=json.loads((A/'audit.json').read_text());sha=lambda b:hashlib.sha256(b).hexdigest().upper()
names=set(audit['InitialAllowlist'])|{n for n in audit['ChangedManifests'] if n.startswith('tools/') and n.endswith('.json')}
paths=[R/n for n in sorted(names)];original={};changes=[]
def base(row,p):
 return p.parent if row.get('Root') in ('Packet','Tool') or row.get('Scope')=='Tool' else R
def walk(x,p):
 if isinstance(x,list):
  for y in x:walk(y,p)
 elif isinstance(x,dict):
  if isinstance(x.get('Path'),str) and isinstance(x.get('Sha256'),str):
   f=base(x,p)/x['Path']
   if not f.exists() and not x.get('Root') and not x.get('Scope'):f=p.parent/x['Path']
   if f.is_file() and f.resolve()!=p.resolve():
    b=f.read_bytes()
    if x.get('HashMode')=='TextLF':b=b.replace(b'\r\n',b'\n')
    if x['Sha256']!=sha(b):
     changes.append(dict(Manifest=p.relative_to(R).as_posix(),Path=f.relative_to(R).as_posix(),Before=x['Sha256'],After=sha(b)));x['Sha256']=sha(b)
     for k in ('Bytes','SealedBytes'):
      if k in x:x[k]=len(b)
  for k,y in x.items():
   if k not in ('HistoricalSourceSnapshots','SourceEquivalence'):walk(y,p)
prev={p:sha(p.read_bytes()) for p in paths}
for n in range(15):
 mapping={}
 for p in paths:
  b=p.read_bytes();v=json.loads(b);walk(v,p);after=(json.dumps(v,indent=2)+'\n').encode()
  if v!=json.loads(b):original.setdefault(p,b);p.write_bytes(after)
  h=sha(p.read_bytes())
  if h!=prev[p]:mapping[prev[p]]=h;prev[p]=h
 if not mapping:break
 for p in (R/'.github/workflows').glob('*.yml'):
  b=p.read_bytes();after=b
  for old,new in mapping.items():after=after.replace(old.encode(),new.encode())
  if after!=b:original.setdefault(p,b);p.write_bytes(after)
else:raise ValueError('Cycle in source consumers')
# Explicit integrated workflow consumer of its final plan.
p=R/'.github/workflows/nif-upgrade-integrated.yml';b=p.read_bytes();h=sha((R/'tools/Native/Erlang.NifIntegratedObservations/inputs.json').read_bytes());after,count=re.subn(rb'(      INTEGRATED_PLAN: )[0-9A-F]{64}',lambda m:m[1]+h.encode(),b);assert count==1
if after!=b:original.setdefault(p,b);p.write_bytes(after)
for p,b in original.items():
 dest=A/(sha(b)+p.suffix);dest.write_bytes(b) if not dest.exists() else None;audit['ArchivedOriginals'].setdefault(p.relative_to(R).as_posix(),dest.relative_to(R).as_posix())
audit['Changes']+=changes;audit['ConsumerFinalizationIterations']=n;audit['ChangedManifests']=sorted(set(audit['ChangedManifests'])|{p.relative_to(R).as_posix() for p in original});(A/'audit.json').write_text(json.dumps(audit,indent=2)+'\n');print('Finalized consumer cascade',n)
