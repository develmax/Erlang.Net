"""Root-only source, actual validation and evidence gates; read-only until caller mutates."""
from pathlib import Path
import hashlib,json,subprocess
ROOT=Path.cwd().resolve();OUT=ROOT/'docs/validation/part-307';CLOSED45='9EB82D1D39B01ABB618DFD3698D656E0ADE6F3511506604F33038C9A3B381CB6'
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(Path(p).read_bytes());git=lambda *x:subprocess.check_output(['git',*x],cwd=ROOT)
def safe(name):
 p=(ROOT/name).resolve();assert p.is_relative_to(ROOT)
 assert not {'.git','bin','obj','__pycache__','cache','.cache'}.intersection(p.relative_to(ROOT).parts)
 assert not any(x.lower().startswith('license') for x in p.relative_to(ROOT).parts)
 return p
def pinned(pin):
 p=safe(pin['Path']);data=p.read_bytes();assert len(data)==pin['Bytes'] and sha(data)==pin['Sha256'],str(p);return p
def gates(path):
 c=read(path);assert c['Part']==307 and c['ParentHead']=='ae4cd999e20e461f7f5917ec346ade31416e2874'
 assert git('rev-parse','HEAD').decode().strip()==c['ParentHead']
 f=read(pinned(c['SourceFreeze']));assert f['ParentHead']==c['ParentHead'] and f['SourcePaths']==len(f['Files'])==12 and f['CSharpPaths']==8 and f['TokensExactForCSharp']
 assert c['SourceFreeze']['Sha256']=='9C6CE588B09C182AF582252BB377CB3E0DC4650F78444BD1A49D6371AA78964D'
 assert c['NativeSourcePlan']['Sha256']=='FD758C0CBD4B6AAF29FB3B3A82BDF97473FE1848E2079BB3B3B3C95F0176B299'
 assert len({x['Path'] for x in f['Files']})==12 and sum(x['Path'].endswith('.cs') for x in f['Files'])==8
 for row in f['Files']:pinned(row)
 for row in read(ROOT/'artifacts/nif-actual-beam-caller310-validated1/promotion-map.json')['Paths']:
  actual=safe(row['Path']).read_bytes();assert len(actual)==row['ResultBytes'] and sha(actual)==row['ResultSha256']
 assert f['ReviewedSourceInputs']==1013 and f['ReviewedSourcePlanSha256']==c['NativeSourcePlan']['Sha256']
 assert f['AddedHostNames']==[] and not f['PublicMfaAdded'] and not f['PublicLoadNifActivated'] and f['ExtendedExistingHost']=='runtime/direct-source-load-nif-exact-caller-capability'
 assert read(pinned(c['FullExit']))['ExitCode']==0 and b'Validation passed' in pinned(c['FullLog']).read_bytes()
 t=read(pinned(c['Tests']));old=read(pinned(c['PreviousTests']));assert (t['Passed'],t['Failed'])==(6627,0) and old['Passed']==6627
 names=[x['Name'] for x in t['Tests']];assert names==[x['Name'] for x in old['Tests']] and len(names)==len(set(names))==6627 and f['ExtendedExistingHost'] in names and all(x['Status']=='Passed' for x in t['Tests'])
 assert c['AddedNames']==[] and read(pinned(c['Integration']))==read(pinned(c['PreviousIntegration'])) and len(read(pinned(c['Integration'])))==11
 for label,assertions in c['GuardAssertions'].items():
  guard=read(pinned(c[label]))
  for key,value in assertions.items():assert guard[key]==value,(label,key)
 for label in ['FullGuard','NativeGuard']:
  guard=read(pinned(c[label]));assert guard['SourceHashesExact'] and guard['SourceFreezeSha256']==c['SourceFreeze']['Sha256']
 assert read(pinned(c['NativeGuard']))['ReviewedSourcePlanSha256']==c['NativeSourcePlan']['Sha256']
 plan=read(pinned(c['NativeSourcePlan']));assert len(plan['Files'])==1013
 for row in plan['Files']:
  data=safe(row['Path']).read_bytes()
  if row['HashMode']=='TextLF':data=data.replace(b'\r\n',b'\n')
  else:assert row['HashMode']=='BinaryExact'
  assert len(data)==row['Bytes'] and sha(data)==row['Sha256'],row['Path']
 raw=read(pinned(c['RawRegistry']));derived=read(pinned(c['DerivedRegistry']));previous=json.loads(git('show',c['ParentHead']+':docs/supported-mfas.json'))
 assert len(raw)==len(derived)==504 and derived==previous and sum(x['Module']=='erlang' for x in derived)==346
 for before,after in zip(raw,derived):
  assert before==after or (before['Module']=='erlang' and before['Function']=='process_info' and before['Arity']==1 and {k:v for k,v in before.items() if k!='Limits'}=={k:v for k,v in after.items() if k!='Limits'})
 registry=read(pinned(c['RegistryGuard']));assert registry['RawSha256']==c['RawRegistry']['Sha256'] and registry['RestoredSha256']==c['DerivedRegistry']['Sha256']
 optional=read(pinned(c['OptionalIoGuard']));assert optional['FullTestsSha256']==c['Tests']['Sha256']
 for row in optional['Sources']:assert sha(safe(row['Path']).read_bytes())==row['Sha256']
 assert sha((ROOT/'docs/changelog/0045.md').read_bytes())==CLOSED45
 volume=pinned(c['ActiveChangelogBefore']).read_bytes();assert len(volume)<=32768 and len(volume.splitlines())<=400
 index=read(pinned(c['EvidenceIndex']));destinations=set()
 for row in index:
  pinned({'Path':row['SourcePath'],'Bytes':row['Bytes'],'Sha256':row['Sha256']});dest=safe(row['Destination']);assert dest.is_relative_to(OUT) and dest not in destinations;destinations.add(dest)
 for row in read(ROOT/'artifacts/nif-actual-beam-caller310-validated1/validation-freeze.json')['Files']:
  source='artifacts/nif-actual-beam-caller310-validated1/'+row['Path'];assert any(x['SourcePath']==source and x['Bytes']==row['Bytes'] and x['Sha256']==row['Sha256'] for x in index),source
 for kind in c['Verified306ProofKinds']:
  for row in read(ROOT/f'artifacts/pack306-{kind}-detached-staging-manifest.json'):
   assert any(all(x[k]==row[k] for k in ['SourcePath','Destination','Bytes','Sha256']) for x in index)
 assert c['Verified306ProofKinds']==['ci','native','original']
 assert c['Allocator19Verified']
 allocator_rows=read(pinned(c['Allocator19Manifest']))['Files'];assert len(allocator_rows)==51
 for row in allocator_rows:assert any(all(x[k]==row[k] for k in ['SourcePath','Destination','Bytes','Sha256']) for x in index)
 assert not c['PublicLoadNifActivated'] and not c['WholeCompatibilityComplete'] and not c['FreshOriginal307Verified']
 return c,f,index
