"""Detached source composition only. Never builds, runs a VM, or writes Main/Git."""
from pathlib import Path
import argparse,copy,hashlib,json,re,subprocess

p=argparse.ArgumentParser()
p.add_argument('--published-parent',required=True)
p.add_argument('--output',required=True,type=Path)
for kind in ('green','red'):
 p.add_argument('--'+kind+'-guard',required=True,type=Path)
 p.add_argument('--'+kind+'-guard-sha256',required=True)
a=p.parse_args();r=Path.cwd().resolve();out=a.output.resolve();c=out/'candidate'
h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
def exact(path,digest):
 b=path.read_bytes();assert h(b)==digest.upper(),str(path);return b
def row(folder,name):
 b=(folder/name).read_bytes();return dict(Path=name,Bytes=len(b),Sha256=h(b))
def write(path,value):path.write_bytes((json.dumps(value,indent=2)+'\n').encode())
def rehash(folder,rows):
 for x in rows:assert row(folder,x['Path'])==x,x['Path']
assert a.published_parent=='b51f050c287f3b91346e1d3e30e86061708fb035'
assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD'],cwd=r,text=True).strip()==a.published_parent
for args in (['diff','--name-only'],['diff','--cached','--name-only']):assert not subprocess.check_output(['git','--no-optional-locks',*args],cwd=r).strip()
assert out.is_relative_to(r/'artifacts') and not out.exists()
parent=r/'artifacts/process-error-handler327-candidate1'
af=r/'artifacts/process-error-handler327-candidate4/source-freeze.json';exact(af,'4C01B791522C2EBA5EDF27707A41EB0D8D037CB9F7D7FFA5CE5FFDAACB334FFD')
parent_main=r/'artifacts/root-part326-main1/source-freeze.json';exact(parent_main,'371BBABFACB1B2F50D9FD08E40CC42D0DFE60A1B1FBDC0095CCE063F4BAEBBDA')
base=read(parent/'baseline-source-resource-closure.json')['Files'];pm={x['Path']:x for x in base};assert len(pm)==2370
rehash(r,base)
# Bind the actual published parent; retain raw working source separately from Git TextLF publication.
names=list(pm);payload=''.join(a.published_parent+':'+n+'\n' for n in names).encode()
batch=subprocess.check_output(['git','--no-optional-locks','cat-file','--batch'],input=payload,cwd=r);offset=0;canonical=[]
for name in names:
 end=batch.index(b'\n',offset);header=batch[offset:end].split();assert len(header)==3 and header[1]==b'blob';size=int(header[2]);b=batch[end+1:end+size+1];offset=end+size+2
 raw=(r/name).read_bytes()
 if b!=raw:
  assert b'\0' not in raw and b==raw.replace(b'\r\n',b'\n'),name
  attr=subprocess.check_output(['git','--no-optional-locks','check-attr','text','--',name],cwd=r,text=True);assert not attr.rstrip().endswith(': unset'),name
  canonical.append(dict(Path=name,WorkingSha256=h(raw),PublishedSha256=h(b),Mode='TextLF'))
assert offset==len(batch)
g=r/'artifacts/process-error-handler327-focus4';gf=g/'formatted-source-freeze.json';gc=g/'formatted-source-resource-closure.json'
exact(gf,'C0B49A59F4750D4CB98144BBCF6B230F7A290C2356D848ABE35FC57D42694EF8');exact(gc,'DF05958879FA97A8962578356332809109872D037F809DD751223BF796D8E64D')
green=read(gf);gm={x['Path']:x for x in read(gc)['Files']};sdk={x['Path']:x for x in green['Files']};assert len(gm)==2379 and len(sdk)==17
rehash(g/'candidate',list(gm.values()))
assert (g/'tokens-before.json').read_bytes()==(g/'tokens-after.json').read_bytes()
assert h((g/'tokens-before.json').read_bytes())=='934AF3C07C4350DAC287B34768BE6CDA5763AAC7DB3F69C9D40C45614A5A75C5' and len(read(g/'tokens-before.json'))==17
for name,x in gm.items():
 if name not in sdk:assert x==pm[name],name
red=r/'artifacts/process-error-handler327-historical-red1';rf=red/'source-freeze.json';rc=red/'source-resource-closure.json'
exact(rf,'DC4F5F53A49628E74271A54EF266ACB46D57A3153CE52D6EE0C8CC38D333796E');rv=read(rf);exact(rc,rv['SourceClosureSha256']);rm={x['Path']:x for x in read(rc)['Files']};assert len(rm)==2374
rehash(red/'candidate',list(rm.values()));helpers=set(rv['AppliedSameFormattedPaths']);assert len(helpers)==6
for name,x in rm.items():assert x==(gm[name] if name in helpers else pm[name]),name
sourcepeer=r/'artifacts/process-error-handler327-putdata-candidate4-source-peer.json';exact(sourcepeer,'839D3610F6C128BE33DCE9AFBC11ADEDF2F2B5E78EF232DE690900A0C8341F23')
formatpeer=r/'artifacts/root327-actual-focus4-managed-putdata-peer.json';exact(formatpeer,'CCDB7E58FE6D791F0970F28164FC9B5C84578A616F65C36BC456D9B7681DE596')
proofs=[]
for kind,path,digest in [('green',a.green_guard,a.green_guard_sha256),('red',a.red_guard,a.red_guard_sha256)]:
 path=path.resolve();assert path.is_relative_to(r/'artifacts');exact(path,digest);v=read(path);assert v['Passed'] is True
 assert v['TargetBuildPassed'] is True
 if kind=='green':
  assert v['Status'].startswith('PASS_ACTUAL327_')
  assert v['GreenSourceFreezeSha256']==h(gf.read_bytes()) and v['SourceClosureSha256']==h(gc.read_bytes())
  assert (v['GeneratedCasesPassed'],v['HostGroupsPassed'],v['SourceClosureFiles'])==(20,5,2379)
 else:
  assert v['Status'].startswith('PASS_ACTUAL327_')
  assert v['RedSourceFreezeSha256']==h(rf.read_bytes()) and v['SourceClosureSha256']==h(rc.read_bytes())
  for key in ['HistoricalProductionExactPublished326','SameFormattedSixHelpersRaw','OwnedProcessSettled']:assert v[key] is True,key
  assert v['ActualFailure']=='public_process_flag_error_handler_badarg'
 bindings=v['Bindings'];assert bindings and len({str((r/x['Path']).resolve()).casefold() for x in bindings})==len(bindings)
 for x in bindings:
  bp=(r/x['Path']).resolve();assert bp.is_relative_to(r/'artifacts');data=exact(bp,x['Sha256']);assert len(data)==x['Bytes']
 proofs.append(dict(Path=path.relative_to(r).as_posix(),Sha256=h(path.read_bytes()),Role=kind))
# All approvals/proofs/source bytes are checked before the first output mutation.
c.mkdir(parents=True)
for name in gm:
 target=c/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((g/'candidate'/name).read_bytes())
owned=set(sdk)
attrs='.gitattributes';raw=(c/attrs).read_bytes();line=b'docs/validation/part-327/** binary';assert line not in raw.splitlines()
(c/attrs).write_bytes(raw+(b'' if raw.endswith(b'\n') else b'\n')+line+b'\n');owned.add(attrs)
newproduction={x['Path'] for x in read(af)['Files'] if x['Added'] and x['Path'].startswith('src/')}
assert len(newproduction)==5 and all(p.endswith('.cs') for p in newproduction)
plans=[]
for name,wf,pin,parent_count,parent_digest in [
 ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1109,'A673C88561EC5E31E1F69BE5795519A3AA658E013B6000910B03E192A520E6A5'),
 ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1134,'53500EFC145705B70364DE7BC19E60FBCE55C62C7C4C00CB66712B1B8F01F9E4')]:
 exact(c/name,parent_digest);prior=read(c/name);updated=copy.deepcopy(prior);records=[]
 assert len(prior['Files'])==parent_count and len({x['Path'] for x in prior['Files']})==parent_count
 oldrows={x['Path']:x for x in prior['Files']};assert not(set(oldrows)&newproduction)
 assert {n for n in gm if n.startswith('src/') and n.endswith('.cs')} - set(oldrows)==newproduction
 for n in newproduction:oldrows[n]=dict(Path=n,HashMode='TextLF')
 count=parent_count+len(newproduction)
 for n,x in sorted(oldrows.items()):
  b=(c/x['Path']).read_bytes();assert x['HashMode'] in ('TextLF','BinaryExact');b=b.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else b
  records.append(dict(Path=x['Path'],Bytes=len(b),Sha256=h(b),HashMode=x['HashMode']))
 updated['Files']=records;assert {k:v for k,v in updated.items() if k!='Files'}=={k:v for k,v in prior.items() if k!='Files'}
 assert len(records)==count and {x['Path'] for x in records}=={x['Path'] for x in prior['Files']}|newproduction
 write(c/name,updated);digest=h((c/name).read_bytes());assert b'\r' not in (c/name).read_bytes() and not(c/name).read_bytes().startswith(b'\xef\xbb\xbf')
 b,n=re.subn(rb'(?m)^([ \t]*'+pin.encode()+rb': )[A-F0-9]{64}[ \t]*\r?$',lambda m:m[1]+digest.encode(),(c/wf).read_bytes());assert n==1;(c/wf).write_bytes(b)
 owned|={name,wf};plans.append(dict(Path=name,Inputs=count,Sha256=digest,Workflow=wf,Pin=pin,AllNonFilesImportAbiPolicyFieldsUnchanged=True,InputPathSetOnlyApprovedFiveAdded=True,NewProductionInputs=sorted(newproduction)))
for plan in plans:
 for x in read(c/plan['Path'])['Files']:
  b=(c/x['Path']).read_bytes();b=b.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else b;assert len(b)==x['Bytes'] and h(b)==x['Sha256'],x['Path']
assert len(owned)==22 and sum(n.endswith('.cs') for n in owned)==17 and len(gm)==2379
for name in sdk:assert row(c,name)==sdk[name]
rows=[row(c,n) for n in sorted(gm)]
for x in rows:
 if x['Path'] not in owned:assert x==pm[x['Path']]
write(out/'source-resource-closure.json',dict(Files=rows));files=[]
for name in sorted(owned):
 x=row(c,name);x.update(Added=name not in pm,ParentSha256=pm[name]['Sha256'] if name in pm else None);files.append(x)
write(out/'promotion-map.json',files)
value=dict(Status='SOURCE_ONLY_FINAL327_PENDING_INDEPENDENT_FINAL_REVIEW',ParentHead=a.published_parent,ParentSourceFreezeSha256=h(parent_main.read_bytes()),ParentSourceClosureSha256='098DD51D477CBD68C004AAF7D45115B06A0F7566370B56A05B75CBCCB68D989F',CandidateDirectory=str(c),SourcePaths=len(owned),CSharpPaths=17,SourceClosurePaths=len(rows),FinalSourceClosureSha256=h((out/'source-resource-closure.json').read_bytes()),Files=files,Plans=plans,ActualGreenAndHistoricalRedGuards=proofs,GreenSourceFreezeSha256=h(gf.read_bytes()),RedSourceFreezeSha256=h(rf.read_bytes()),AllSeventeenCompiledGreenCSharpRawExact=True,AllOutsideOwnedPublishedParentRowsRawExact=True,AllNativeNonFilesFieldsExactParent=True,NativeInputPathSetsOnlyFiveApprovedProductionAdded=True,Docs327BinaryAttributeAdded=True,PublicMfaDelta=0,OrdinaryCasesAppended=20,HostRowsAdded=1,ExpectedFullPassed=6729,Original327Executed=False,ComposerExecutedSdkNativeVm=False,ComposerSha256=h(Path(__file__).read_bytes()),CanonicalPublishedParentRows=canonical)
write(out/'source-freeze.json',value);print(json.dumps(dict(FreezeSha256=h((out/'source-freeze.json').read_bytes()),ClosureSha256=value['FinalSourceClosureSha256'],SourcePaths=len(owned),CSharpPaths=17,SourceClosurePaths=len(rows),Plans=plans)))
