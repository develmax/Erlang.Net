"""Root-only detached composition after actual GREEN and historical RED. No build/VM/Main/Git mutation."""
from pathlib import Path
import argparse,copy,hashlib,json,re,subprocess,zipfile

p=argparse.ArgumentParser()
p.add_argument('--published-parent',required=True)
p.add_argument('--output',required=True,type=Path)
for role in ('green-source-freeze','red-source-freeze','green-guard','red-guard','replaced-main1-freeze','failed-main1-capture'):
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
# Strict upgrade preflight: no reset, no Main writes, no old-full success inference.
mf=artifact(a.replaced_main1_freeze);exact(mf,a.replaced_main1_freeze_sha256)
assert h(mf.read_bytes())=='1C6D9F182962B2AA04831C4187255E43433EFFA9A336A20CCCB11AADA98C7E7F'
mc=mf.parent/'source-resource-closure.json';exact(mc,'A7859120DBD894D6AAD45FC17A133D83DC88FA3E2993B1C8ED07926CFB912424')
main1=read(mf);mainrows=read(mc)['Files'];assert len(mainrows)==2393;rehash(r,mainrows)
cp=artifact(a.failed_main1_capture);exact(cp,a.failed_main1_capture_sha256)
assert h(cp.read_bytes())=='C1252335323D98761C5B548C07F4ED498EECEF60E51087A0438C68D3AA8A0065'
failure=read(cp);assert (failure['Passed'],failure['Failed'],failure['ExitCode'])==(6733,5,1)
assert failure['SourceFreezeSha256']==h(mf.read_bytes()) and failure['RawSourceStillMain1Exact'] is True
rehash(cp.parent,failure['Files'])
assert (r/'docs/supported-mfas.json').read_bytes()==(cp.parent/'raw-supported-mfas.json').read_bytes()
assert (mf.parent/'registry-before-full.json').read_bytes()==(cp.parent/'registry-before-full.json').read_bytes()
assert not subprocess.check_output(['git','--no-optional-locks','diff','--cached','--name-only'],cwd=r).strip()
expected_dirty={x['Path'] for x in main1['Files'] if not x['Added']}
assert len(expected_dirty)==15
registry_name='docs/supported-mfas.json';registry_raw=(r/registry_name).read_bytes()
# Raw capture equality above remains mandatory. Git dirty state follows Git's own clean conversion.
filter_attr=subprocess.check_output(['git','--no-optional-locks','check-attr','filter','--',registry_name],cwd=r,text=True)
assert filter_attr.rstrip().rsplit(': ',1)[-1] in ('unspecified','unset'),filter_attr
registry_clean_oid=subprocess.check_output(['git','--no-optional-locks','hash-object','--path='+registry_name,'--stdin'],input=registry_raw,cwd=r).strip()
registry_raw_oid=subprocess.check_output(['git','--no-optional-locks','hash-object','--stdin'],input=registry_raw,cwd=r).strip()
registry_canonical=registry_raw
if registry_clean_oid!=registry_raw_oid:
 registry_canonical=registry_raw.replace(b'\r\n',b'\n')
 assert subprocess.check_output(['git','--no-optional-locks','hash-object','--stdin'],input=registry_canonical,cwd=r).strip()==registry_clean_oid
registry_published_oid=subprocess.check_output(['git','--no-optional-locks','rev-parse',a.published_parent+':'+registry_name],cwd=r).strip()
registry_dirty=registry_clean_oid!=registry_published_oid
if registry_dirty:expected_dirty.add(registry_name)
assert len(expected_dirty)==15+int(registry_dirty)
registry_model=dict(RawSha256=h(registry_raw),CanonicalSha256=h(registry_canonical),GitCleanObjectId=registry_clean_oid.decode(),PublishedObjectId=registry_published_oid.decode(),CanonicalDiffersPublished=registry_dirty,RawCaptureEqualityStillRequired=True,ExpectedTrackedDirtyCount=len(expected_dirty))
actual_dirty=set(subprocess.check_output(['git','--no-optional-locks','diff','--name-only'],cwd=r,text=True).splitlines())
assert actual_dirty==expected_dirty,(actual_dirty,expected_dirty)
tracked=set(subprocess.check_output(['git','--no-optional-locks','ls-files'],cwd=r,text=True).splitlines())
assert expected_dirty<=tracked
upgrade=dict(ReplacedMain1FreezeSha256=h(mf.read_bytes()),FailedMain1CaptureSha256=h(cp.read_bytes()),Current2393InputsExactFailedMain1=True,CurrentRegistryExactFailureCapture=True,CachedIndexEmpty=True,ExactTrackedDirtyPaths=sorted(actual_dirty),RegistryCanonicalGitModel=registry_model,NoMainWrites=True)
assert out.is_relative_to(r/'artifacts') and not out.exists()
author=r/'artifacts/undefined-lambda328-candidate3-final/source-freeze.json';exact(author,'995C277357FA67A34D9016A7BC89CC133374234234F15D16E3E87E09C9FDFAE4');av=read(author)
sourcepeer=r/'artifacts/undefined-lambda328-candidate3-allocator309-source-peer1.json';exact(sourcepeer,'098F2B58CF9AE4F67FA73BE85E19B509E944932AB06FA5E997E72DCAD9812EE7');sv=read(sourcepeer);assert h(author.read_bytes()) in json.dumps(sv) and str(sv['Status']).startswith('PASS')
parent_main=r/'artifacts/root-part327-main1/source-freeze.json';exact(parent_main,'380AF06807C90F6891C3C834A66573536310F23118954512E6A538E3E4BD8AD3')
parentclosure=r/'artifacts/root-part327-main1/source-resource-closure.json';exact(parentclosure,'57275A459C17E8A6F991CE0FAD8C3FDE16F3DF0D6B132CF8A9C53D0EC9491C3F');base=read(parentclosure)['Files'];pm={x['Path']:x for x in base};assert len(pm)==2379
baselinezip=r/'artifacts/undefined-lambda328-candidate1/baseline-source-resources.zip';baseline=zipfile.ZipFile(baselinezip)
for x in base:
 b=baseline.read(x['Path']);assert len(b)==x['Bytes'] and h(b)==x['Sha256'],x['Path']
# Keep raw worktree and canonical published identities distinct, accepting only declared TextLF conversions.
names=list(pm);payload=''.join(a.published_parent+':'+n+'\n' for n in names).encode();batch=subprocess.check_output(['git','--no-optional-locks','cat-file','--batch'],input=payload,cwd=r);offset=0;canonical=[]
for name in names:
 end=batch.index(b'\n',offset);header=batch[offset:end].split();assert len(header)==3 and header[1]==b'blob';size=int(header[2]);offset=end+1;b=batch[offset:offset+size];offset+=size;assert batch[offset:offset+1]==b'\n';offset+=1
 raw=baseline.read(name)
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
  assert (v['GeneratedCasesPassed'],v['HostGroupsPassed'],v['SourceClosureFiles'])==(8,12,2393)
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
newproduction={x['Path'] for x in av['Files'] if x['Path'] not in pm and x['Path'].startswith('src/')};assert len(newproduction)==6 and all(x.startswith('src/Erlang.Runtime/') and x.endswith('.cs') for x in newproduction)
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
value['FailedMain1UpgradePreflight']=upgrade
value['SourceFactoryDeferredLegacyHostConstructionRestored']=True
write(out/'source-freeze.json',value);print(json.dumps(dict(FreezeSha256=h((out/'source-freeze.json').read_bytes()),ClosureSha256=value['FinalSourceClosureSha256'],SourcePaths=len(owned),CSharpPaths=24,SourceClosurePaths=len(rows),Plans=plans)))
