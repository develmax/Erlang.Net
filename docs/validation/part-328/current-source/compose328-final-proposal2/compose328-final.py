"""Root-only detached composition after actual GREEN and historical RED. No build/VM/Main/Git mutation."""
from pathlib import Path
import argparse,copy,hashlib,json,re,subprocess

p=argparse.ArgumentParser()
p.add_argument('--published-parent',required=True)
p.add_argument('--output',required=True,type=Path)
for role in ('green-source-freeze','red-source-freeze','green-guard','red-guard'):
 p.add_argument('--'+role,required=True,type=Path)
 p.add_argument('--'+role+'-sha256',required=True)
a=p.parse_args();r=Path.cwd().resolve();out=a.output.resolve();c=out/'candidate'
h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
def exact(path,digest):
 assert re.fullmatch('[A-Fa-f0-9]{64}',digest)
 b=path.read_bytes();assert h(b)==digest.upper(),str(path);return b
def row(folder,name):
 b=(folder/name).read_bytes();return dict(Path=name,Bytes=len(b),Sha256=h(b))
def write(path,value):path.write_bytes((json.dumps(value,indent=2)+'\n').encode())
def rehash(folder,rows):
 for x in rows:assert row(folder,x['Path'])=={k:x[k] for k in ('Path','Bytes','Sha256')},x['Path']
def artifact(path):
 path=path.resolve();assert path.is_relative_to(r/'artifacts');return path
assert a.published_parent=='4bc4c00b713c1cc0bbf32a1c74add45174bbd10a'
assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD'],cwd=r,text=True).strip()==a.published_parent
for args in (['diff','--name-only'],['diff','--cached','--name-only']):assert not subprocess.check_output(['git','--no-optional-locks',*args],cwd=r).strip()
assert out.is_relative_to(r/'artifacts') and not out.exists()
author=r/'artifacts/undefined-lambda328-candidate2/source-freeze.json';exact(author,'28E773857A6D8B465A3C79EA4FC30AD7AE4646549947F33058ED6DC5C94FFA9B');av=read(author)
sourcepeer=r/'artifacts/undefined-lambda328-candidate2-public-admission-putdata-peer.json';exact(sourcepeer,'E9093B145A479950E86E0ABD68BB002B45E1C1A586144AF8E1F3085EF7707342');sv=read(sourcepeer);assert sv['CandidateFreezeSha256']==h(author.read_bytes()) and sv['DiagnosticSourceClear'] is True
parent_main=r/'artifacts/root-part327-main1/source-freeze.json';exact(parent_main,'380AF06807C90F6891C3C834A66573536310F23118954512E6A538E3E4BD8AD3')
parentclosure=r/'artifacts/root-part327-main1/source-resource-closure.json';exact(parentclosure,'57275A459C17E8A6F991CE0FAD8C3FDE16F3DF0D6B132CF8A9C53D0EC9491C3F');base=read(parentclosure)['Files'];pm={x['Path']:x for x in base};assert len(pm)==2379
rehash(r,base)
# Keep raw worktree and canonical published identities distinct, accepting only declared TextLF conversions.
names=list(pm);payload=''.join(a.published_parent+':'+n+'\n' for n in names).encode();batch=subprocess.check_output(['git','--no-optional-locks','cat-file','--batch'],input=payload,cwd=r);offset=0;canonical=[]
for name in names:
 end=batch.index(b'\n',offset);header=batch[offset:end].split();assert len(header)==3 and header[1]==b'blob';size=int(header[2]);offset=end+1;b=batch[offset:offset+size];offset+=size;assert batch[offset:offset+1]==b'\n';offset+=1
 raw=(r/name).read_bytes()
 if b!=raw:
  assert b'\0' not in raw and b==raw.replace(b'\r\n',b'\n'),name
  attr=subprocess.check_output(['git','--no-optional-locks','check-attr','text','--',name],cwd=r,text=True);assert not attr.rstrip().endswith(': unset'),name
  canonical.append(dict(Path=name,WorkingSha256=h(raw),PublishedSha256=h(b),Mode='TextLF'))
assert offset==len(batch)
gf=artifact(a.green_source_freeze);exact(gf,a.green_source_freeze_sha256);g=gf.parent;gc=g/'formatted-source-resource-closure.json';green=read(gf);exact(gc,green['SourceClosureSha256'])
assert green['AuthorFreezeSha256']==h(author.read_bytes())
assert (green['SourcePaths'],green['CSharpPaths'],green['SourceClosurePaths'])==(24,24,2393)
gm={x['Path']:x for x in read(gc)['Files']};sdk={x['Path']:x for x in green['Files']};assert len(gm)==2393 and len(sdk)==24 and set(sdk)=={x['Path'] for x in av['Files']}
assert all(x.endswith('.cs') for x in sdk);rehash(g/'candidate',list(gm.values()))
assert green['AllOwnedTokensByteExact'] is True and green['OutsideOwnedRawExact']==2369
assert (g/'tokens-before.json').read_bytes()==(g/'tokens-after.json').read_bytes() and len(read(g/'tokens-before.json'))==24
assert {Path(x).resolve() for x in read(g/'token-inputs.json')}=={(g/'candidate'/x).resolve() for x in sdk}
for name,x in gm.items():
 if name not in sdk:assert x==pm[name],name
rf=artifact(a.red_source_freeze);exact(rf,a.red_source_freeze_sha256);red=rf.parent;rv=read(rf);rc=red/'source-resource-closure.json';exact(rc,rv['SourceClosureSha256']);rm={x['Path']:x for x in read(rc)['Files']};assert len(rm)==2387;rehash(red/'candidate',list(rm.values()))
# Same ten nonproduction helpers including Program; every historical production file remains published327.
helpers={name for name in sdk if not name.startswith('src/')};assert len(helpers)==10
assert set(rm)==set(pm)|{name for name in helpers if name not in pm}
for name,x in rm.items():assert x==(gm[name] if name in helpers else pm[name]),name
proofs=[]
for kind,path,digest in [('green',a.green_guard,a.green_guard_sha256),('red',a.red_guard,a.red_guard_sha256)]:
 path=artifact(path);exact(path,digest);v=read(path);assert v['Passed'] is True and v['TargetBuildPassed'] is True and str(v['Status']).startswith('PASS_ACTUAL328_')
 if kind=='green':
  assert v['GreenSourceFreezeSha256']==h(gf.read_bytes()) and v['SourceClosureSha256']==h(gc.read_bytes())
  assert (v['GeneratedCasesPassed'],v['HostGroupsPassed'],v['SourceClosureFiles'])==(8,6,2393)
 else:
  assert v['RedSourceFreezeSha256']==h(rf.read_bytes()) and v['SourceClosureSha256']==h(rc.read_bytes())
  for key in ('HistoricalProductionExactPublished327','SameFormattedTenNonProductionHelpersRaw','OwnedProcessSettled','SinglePrimaryException','NoTimeoutOrCleanupAggregate'):assert v[key] is True,key
  assert v['ActualFailure']=='public_restored_local_fun_undef'
 bindings=v['Bindings'];assert bindings and len({str((r/x['Path']).resolve()).casefold() for x in bindings})==len(bindings)
 for x in bindings:
  bp=artifact(r/x['Path']);data=exact(bp,x['Sha256']);assert len(data)==x['Bytes']
 proofs.append(dict(Path=path.relative_to(r).as_posix(),Sha256=h(path.read_bytes()),Role=kind))
# No output mutation until every current source/actual-result/publication precondition above has passed.
c.mkdir(parents=True)
for name in gm:
 target=c/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((g/'candidate'/name).read_bytes())
owned=set(sdk);attrs='.gitattributes';raw=(c/attrs).read_bytes();line=b'docs/validation/part-328/** binary';assert line not in raw.splitlines();(c/attrs).write_bytes(raw+(b'' if raw.endswith(b'\n') else b'\n')+line+b'\n');owned.add(attrs)
newproduction={x['Path'] for x in av['Files'] if x['Added'] and x['Path'].startswith('src/')};assert len(newproduction)==6 and all(x.startswith('src/Erlang.Runtime/') and x.endswith('.cs') for x in newproduction)
plans=[]
for name,wf,pin,parent_count,parent_digest in [
 ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1114,'D1A3A9533A9A4537E7C7B596318E690083677B154B8A912B5AC36A0672137344'),
 ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1139,'0096F13F945A7494F704C518EDFCEC766DF38167B8EE76898F53FA3077E118D9')]:
 exact(c/name,parent_digest);prior=read(c/name);updated=copy.deepcopy(prior);records=[];assert len(prior['Files'])==parent_count
 oldrows={x['Path']:x for x in prior['Files']};assert len(oldrows)==parent_count and not(set(oldrows)&newproduction)
 assert {n for n in gm if n.startswith('src/') and n.endswith('.cs')} - set(oldrows)==newproduction
 for n in newproduction:oldrows[n]=dict(Path=n,HashMode='TextLF')
 for n,x in sorted(oldrows.items()):
  b=(c/n).read_bytes();assert x['HashMode'] in ('TextLF','BinaryExact');b=b.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else b;records.append(dict(Path=n,Bytes=len(b),Sha256=h(b),HashMode=x['HashMode']))
 updated['Files']=records;assert {k:v for k,v in updated.items() if k!='Files'}=={k:v for k,v in prior.items() if k!='Files'}
 assert len(records)==parent_count+len(newproduction) and {x['Path'] for x in records}=={x['Path'] for x in prior['Files']}|newproduction
 write(c/name,updated);digest=h((c/name).read_bytes());assert b'\r' not in (c/name).read_bytes() and not(c/name).read_bytes().startswith(b'\xef\xbb\xbf')
 b,n=re.subn(rb'(?m)^([ \t]*'+pin.encode()+rb': )[A-F0-9]{64}[ \t]*\r?$',lambda m:m[1]+digest.encode(),(c/wf).read_bytes());assert n==1;(c/wf).write_bytes(b)
 owned|={name,wf};plans.append(dict(Path=name,Inputs=len(records),Sha256=digest,Workflow=wf,Pin=pin,AllNonFilesImportAbiPolicyFieldsUnchanged=True,InputPathSetOnlyApprovedSixAdded=True,NewProductionInputs=sorted(newproduction)))
for plan in plans:
 for x in read(c/plan['Path'])['Files']:
  b=(c/x['Path']).read_bytes();b=b.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else b;assert len(b)==x['Bytes'] and h(b)==x['Sha256'],x['Path']
assert len(owned)==29 and sum(n.endswith('.cs') for n in owned)==24
for name in sdk:assert row(c,name)==sdk[name]
rows=[row(c,n) for n in sorted(gm)]
for x in rows:
 if x['Path'] not in owned:assert x==pm[x['Path']]
write(out/'source-resource-closure.json',dict(Files=rows));files=[]
for name in sorted(owned):
 x=row(c,name);x.update(Added=name not in pm,ParentSha256=pm[name]['Sha256'] if name in pm else None);files.append(x)
write(out/'promotion-map.json',files)
value=dict(Status='SOURCE_ONLY_FINAL328_PENDING_INDEPENDENT_FINAL_REVIEW',ParentHead=a.published_parent,ParentSourceFreezeSha256=h(parent_main.read_bytes()),ParentSourceClosureSha256=h(parentclosure.read_bytes()),CandidateDirectory=str(c),SourcePaths=len(owned),CSharpPaths=24,SourceClosurePaths=len(rows),FinalSourceClosureSha256=h((out/'source-resource-closure.json').read_bytes()),Files=files,Plans=plans,ActualGreenAndHistoricalRedGuards=proofs,GreenSourceFreezeSha256=h(gf.read_bytes()),RedSourceFreezeSha256=h(rf.read_bytes()),AllTwentyFourCompiledGreenCSharpRawExact=True,AllOutsideOwnedPublishedParentRowsRawExact=True,AllNativeNonFilesFieldsExactParent=True,NativeInputPathSetsOnlySixApprovedProductionAdded=True,Docs328BinaryAttributeAdded=True,PublicMfaDelta=0,OrdinaryCasesAppended=8,HostRowsAdded=1,ExpectedFullPassed=6738,Original328Executed=False,ComposerExecutedSdkNativeVm=False,ComposerSha256=h(Path(__file__).read_bytes()),CanonicalPublishedParentRows=canonical)
write(out/'source-freeze.json',value);print(json.dumps(dict(FreezeSha256=h((out/'source-freeze.json').read_bytes()),ClosureSha256=value['FinalSourceClosureSha256'],SourcePaths=len(owned),CSharpPaths=24,SourceClosurePaths=len(rows),Plans=plans)))
