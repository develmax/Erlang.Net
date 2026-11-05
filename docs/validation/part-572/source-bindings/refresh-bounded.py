import hashlib,json,subprocess
from pathlib import Path
R=Path.cwd(); A=R/'docs/validation/part-572/source-bindings'; A.mkdir(parents=True,exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
initial=[
'tools/Native/Erlang.NifMapFamily504Observations/source-plan.json',
'tools/Native/Erlang.NifMapMaterialized505Observations/source-plan.json',
'tools/Native/Erlang.NifMapReentry505Observations/source-plan.json',
'tools/Runtime/Erlang.OwnedGuest506Observations/source-plan.json',
'tools/Native/Erlang.NifVariadic526Observations/source-plan.json',
'tools/Native/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json',
'tools/Native/Erlang.NifReallocationObservations/SOURCE-PLAN.json',
'tools/Native/Erlang.NifPrivateData557Observations/source-plan.json',
'tools/Native/Erlang.NifResource529Observations/source-plan.json',
*[f'tools/Runtime/{n}/inputs.json' for n in ['Erlang.Dictionary537Observations','Erlang.MatchSaved634Observations','Erlang.EtsDispatch540Observations','Erlang.MonitorOriginal645Observations','Erlang.BeamFunction542Observations']]]
paths={R/n for n in initial}; original={};events=[];additions=[]
new_sources=subprocess.check_output(['git','diff','--name-only','--diff-filter=A','origin/main...HEAD','--','src']).decode().splitlines()
callers={'src/Runtime/Erlang.Terms/Collections/Maps/MapTerm.cs','src/Runtime/Erlang.Terms/Collections/Maps/TermMapOrdering.cs','src/Runtime/Erlang.Runtime.Core/Primitives/Maps/HamtIndexOwner429.cs','src/Otp/Managed/Erlang.Otp.Stdlib/Collections/Maps/MapOperations.cs'}
def save(p,v):
 b=(json.dumps(v,indent=2)+'\n').encode()
 if b!=p.read_bytes():
  original.setdefault(p,p.read_bytes());p.write_bytes(b)
def target(row,owner):
 base=owner.parent if row.get('Root') in ('Packet','Tool') or row.get('Scope')=='Tool' else R
 if row.get('Root') not in (None,'Packet','Tool','Repository'):raise ValueError('Unknown root '+str(row))
 p=base/row['Path']
 if not p.is_file() and not row.get('Root') and not row.get('Scope'):p=owner.parent/row['Path']
 return p.resolve()
def update_row(row,owner):
 p=target(row,owner)
 if p==owner or not p.is_file():return
 b=p.read_bytes();mode=row.get('HashMode','Raw');assert mode in ('Raw','TextLF','BinaryExact'),mode
 if mode=='TextLF':b=b.replace(b'\r\n',b'\n')
 h=sha(b)
 if h!=row['Sha256']:
  events.append(dict(Manifest=owner.relative_to(R).as_posix(),Path=p.relative_to(R).as_posix(),Before=row['Sha256'],After=h));row['Sha256']=h
  for k in ('Bytes','SealedBytes'):
   if k in row:row[k]=len(b)
def walk(x,owner):
 if isinstance(x,list):
  for y in x:walk(y,owner)
 elif isinstance(x,dict):
  if isinstance(x.get('Path'),str) and isinstance(x.get('Sha256'),str):update_row(x,owner)
  for k,y in x.items():
   if k not in ('HistoricalSourceSnapshots','SourceEquivalence'):walk(y,owner)
def membership(v):
 for key in ('Files','Dependencies','Inputs','Rows','SourceBindings'):
  rows=v.get(key)
  if not isinstance(rows,list):continue
  bound={x.get('Path') for x in rows if isinstance(x,dict)}
  if not callers.intersection(bound):continue
  sample=next(x for x in rows if isinstance(x,dict) and x.get('Path') in callers)
  for name in new_sources:
   if name in bound:continue
   item={k:y for k,y in sample.items() if k in ('HashMode','Scope','Root','Bytes','SealedBytes')};item.update(Path=name,Sha256=sha((R/name).read_bytes()))
   if 'Scope' in item:item['Scope']='Repository'
   if 'Root' in item:item['Root']='Repository'
   for k in ('Bytes','SealedBytes'):
    if k in item:item[k]=(R/name).stat().st_size
   rows.append(item);bound.add(name);additions.append(name)
for p in list(paths):
 v=json.loads(p.read_bytes())
 if 'SourceFreezePath' in v:
  old=R/v['SourceFreezePath'];original.setdefault(old,old.read_bytes());f=json.loads(old.read_bytes());f['Scope']='MR104 current source identities only; no execution or compatibility promotion';f['Rows']=[dict(x) for x in f['Rows']];membership(f);walk(f['Rows'],old)
  new=A/(p.parent.name+'-current-source.json');new.write_bytes((json.dumps(f,indent=2)+'\n').encode());v['SourceFreezePath']=new.relative_to(R).as_posix();v['SourceFreezeSha256']=sha(new.read_bytes());v['SourceFreezeRows']=len(f['Rows']);save(p,v)
tracked=[R/n for n in subprocess.check_output(['git','ls-files','tools']).decode().splitlines() if n.endswith('.json') and any(s in Path(n).name.lower() for s in ('inputs','manifest','source-plan'))]
for iteration in range(18):
 mapping={};changed=False
 for p in sorted(paths):
  old=p.read_bytes();v=json.loads(old);membership(v);walk(v,p);save(p,v)
  if old!=p.read_bytes():mapping[sha(old)]=sha(p.read_bytes());changed=True
 if not mapping and not changed:break
 for p in tracked:
  if not p.is_file():continue
  v=json.loads(p.read_bytes());before=json.dumps(v,sort_keys=True)
  def consumer(x):
   if isinstance(x,list):
    for y in x:consumer(y)
   elif isinstance(x,dict):
    if isinstance(x.get('Path'),str) and isinstance(x.get('Sha256'),str) and target(x,p) in paths:update_row(x,p)
    for k,y in list(x.items()):
     if k in ('N1BuildPlanSha256','RuntimeBuildManifestSha256','OriginalPlanSha256','BuildPlanSha256','NativeBuildPlanSha256') and isinstance(y,str) and y in mapping:x[k]=mapping[y]
     elif k not in ('HistoricalSourceSnapshots','SourceEquivalence'):consumer(y)
  consumer(v)
  if json.dumps(v,sort_keys=True)!=before:paths.add(p);save(p,v)
 for p in (R/'.github/workflows').glob('*.yml'):
  b=p.read_bytes();after=b
  for old,new in mapping.items():after=after.replace(old.encode(),new.encode())
  if b!=after:original.setdefault(p,b);p.write_bytes(after)
else:raise ValueError('Dependency cascade did not converge')
for relative,key in [('docs/mfa-compatibility.json','VerifiedSourceFiles'),('docs/native-infrastructure.json','Entries')]:
 p=R/relative;v=json.loads(p.read_bytes());before_entries=json.dumps(v.get('Entries'),sort_keys=True)
 if key=='VerifiedSourceFiles':
  for row in v[key]:update_row(row,p)
 else:
  for entry in v['Entries']:
   for row in entry['SourceBindings']:update_row(row,p)
 # Raw source bindings only: classifications/evidence/scopes are not rewritten.
 if key=='VerifiedSourceFiles':assert json.dumps(v['Entries'],sort_keys=True)==before_entries
 save(p,v)
archives={}
for p,b in original.items():
 dest=A/(sha(b)+p.suffix);dest.write_bytes(b) if not dest.exists() else None;archives[p.relative_to(R).as_posix()]=dest.relative_to(R).as_posix()
(A/'audit.json').write_text(json.dumps(dict(Status='SOURCE_BINDINGS_ONLY_NOT_EXECUTION_OR_PROMOTION',InitialAllowlist=initial,ChangedManifests=sorted(p.relative_to(R).as_posix() for p in original),ArchivedOriginals=archives,AddedSourceFiles=sorted(set(additions)),Changes=events,Iterations=iteration,WholeContractsClosed=0),indent=2)+'\n')
print('Changed',len(original),'files; iterations',iteration)
