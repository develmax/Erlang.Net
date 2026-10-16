"""Root-only detached329 final composition. No SDK/VM/native/Main/Git writes."""
from pathlib import Path
import argparse,copy,hashlib,json,re,subprocess
R=Path.cwd().resolve();H=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());digest=lambda b:hashlib.sha256(b).hexdigest().upper()
def write(p,v):p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
def exact(p,s):assert re.fullmatch('[A-Fa-f0-9]{64}',s) and sha(p)==s.upper(),str(p)
def artifact(p):p=p.resolve();assert p.is_relative_to(R/'artifacts');return p
def checkrows(root,rows):
 for e in rows:
  p=(root/e['Path']).resolve();assert p.is_relative_to(root.resolve()) and p.stat().st_size==e['Bytes'] and sha(p)==e['Sha256'],e['Path']
def row(root,n):p=root/n;return dict(Path=n,Bytes=p.stat().st_size,Sha256=sha(p))
p=argparse.ArgumentParser();p.add_argument('--output',required=True,type=Path)
for role in ['green-source-freeze','green-guard','managed-guard','retained-original-guard','interpreted-guard','historical-guard','source-peer']:
 p.add_argument('--'+role,required=True,type=Path);p.add_argument('--'+role+'-sha256',required=True)
a=p.parse_args();out=artifact(a.output);assert not out.exists();C=out/'candidate';head='bb71b9ce04b21e4ec58ba1d62028f3a2c33c3612'
assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD']).decode().strip()==head
assert not subprocess.check_output(['git','--no-optional-locks','diff','--name-only']).strip() and not subprocess.check_output(['git','--no-optional-locks','diff','--cached','--name-only']).strip()
main=R/'artifacts/root-part328-main2/source-freeze.json';pc=main.parent/'source-resource-closure.json';exact(main,'BA9F82F848896139AC2D4BC81FCA724C7E0A9F145A1622B838A126B3D2228542');exact(pc,'B86A971D4922D1F20C97BACDF08A7A99534932CC8EA29FB2DACFEDC444084302');base=read(pc)['Files'];pm={x['Path']:x for x in base};assert len(pm)==2393;checkrows(R,base)
closed=R/'docs/changelog/0050.md';exact(closed,'4B0D3AE470E5D33449C10F9C2C4E4AF84A53DDCEC729E2E53C49F280114A0F5E')
# Independently bind full published parent blobs. Only declared text normalization may differ from raw Main snapshot.
names=list(pm);raw=subprocess.check_output(['git','--no-optional-locks','cat-file','--batch'],input=''.join(head+':'+n+'\n' for n in names).encode());off=0;canonical=[]
for n in names:
 end=raw.index(b'\n',off);hdr=raw[off:end].split();assert len(hdr)==3 and hdr[1]==b'blob';size=int(hdr[2]);off=end+1;b=raw[off:off+size];off+=size;assert raw[off:off+1]==b'\n';off+=1;working=(R/n).read_bytes()
 if b!=working:
  assert b'\0' not in working and b==working.replace(b'\r\n',b'\n'),n
  attr=subprocess.check_output(['git','--no-optional-locks','check-attr','text','--',n],text=True);assert not attr.rstrip().endswith(': unset'),n
  canonical.append(dict(Path=n,WorkingSha256=digest(working),PublishedSha256=digest(b),ComparisonMode='TextLF'))
assert off==len(raw)
gf=artifact(a.green_source_freeze);exact(gf,a.green_source_freeze_sha256);g=read(gf);gc=gf.parent/'formatted-source-resource-closure.json';exact(gc,g['SourceClosureSha256']);gm={x['Path']:x for x in read(gc)['Files']};sdk={x['Path']:x for x in g['Files']}
assert (g['SourcePaths'],g['CSharpPaths'],g['SourceClosurePaths'])==(7,7,2397) and len(gm)==2397 and len(sdk)==7 and all(n.endswith('.cs') for n in sdk)
expected={'src/Erlang.Compiler/Execution.cs','tests/Erlang.Tests/NonTailStackLifecycleHostChecks.cs','tests/Erlang.Tests/NonTailStackLifecycleHostNames.cs','tests/Erlang.Tests/Program.cs','tools/Erlang.Differential/CompiledModuleCases.cs','tools/Erlang.Differential/NonTailRecursionCaseNames.cs','tools/Erlang.Differential/NonTailRecursionCases.cs'};assert set(sdk)==expected
assert g['AllOwnedTokensByteExact'] is True and g['OutsideOwnedRawExact']==2390
assert (gf.parent/'tokens-before.json').read_bytes()==(gf.parent/'tokens-after.json').read_bytes() and len(read(gf.parent/'tokens-before.json'))==7
tokenpaths=read(gf.parent/'token-inputs.json');assert len(tokenpaths)==7 and all(Path(n).is_absolute() for n in tokenpaths)
assert {Path(n).resolve() for n in tokenpaths}=={(gf.parent/'candidate'/n).resolve() for n in sdk}
for n,x in sdk.items():assert x==gm[n],n
checkrows(gf.parent/'candidate',list(gm.values()))
for n,x in gm.items():
 if n not in sdk:assert x==pm[n],n
assert {n for n in gm if n not in pm}==expected-set(pm) and len(expected-set(pm))==4
# Future guards are mandatory exact actual receipt authorities; controlled historical failure is NOT a historical test pass.
proofs=[]
for role in ['green-guard','managed-guard','retained-original-guard','interpreted-guard','historical-guard','source-peer']:
 q=artifact(getattr(a,role.replace('-','_')));exact(q,getattr(a,role.replace('-','_')+'_sha256'));v=read(q);assert v.get('Passed') is True and str(v.get('Status','')).startswith('PASS'),role
 if role!='historical-guard':assert sha(gf) in json.dumps(v),role
 if role in ['green-guard','source-peer']:assert sha(gc) in json.dumps(v),role
 if role=='green-guard':assert v['HostGroupsPassed']==10 and v['GeneratedCasesPassed']==8 and v['SourceClosureFiles']==2397
 bindings=v.get('Bindings',[]);assert bindings or role=='source-peer',role
 for x in bindings:
  bp=artifact(R/x['Path']);exact(bp,x['Sha256']);assert 'Bytes' not in x or bp.stat().st_size==x['Bytes']
 proofs.append(dict(Role=role,Path=q.relative_to(R).as_posix(),Sha256=sha(q)))
# No filesystem output until every parent/source/future-actual proof check passed above.
C.mkdir(parents=True)
for n in gm:
 q=C/n;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes((gf.parent/'candidate'/n).read_bytes())
owned=set(sdk);attrs='.gitattributes';b=(C/attrs).read_bytes();line=b'docs/validation/part-329/** binary';assert line not in b.splitlines();(C/attrs).write_bytes(b+(b'' if b.endswith(b'\n') else b'\n')+line+b'\n');owned.add(attrs)
plans=[];execution='src/Erlang.Compiler/Execution.cs'
for name,wf,pin,count,oldhash,allowed in [
 ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1120,'97C12C85FF98EC3D98C33DD692F2F2474A14FA4150B4F39D0408434773FAD649',{execution}),
 ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1145,'F835320BE11A28B4C1BDA71C962CFA5A8789FE49C1404FA54CA1E326EE9D9229',{execution,attrs,'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml'})]:
 exact(C/name,oldhash);prior=read(C/name);updated=copy.deepcopy(prior);assert len(prior['Files'])==count and len({x['Path'] for x in prior['Files']})==count;records=[];changed=[]
 for x in prior['Files']:
  raw=(C/x['Path']).read_bytes();assert x['HashMode'] in ['TextLF','BinaryExact'];raw=raw.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else raw;y=dict(Path=x['Path'],Bytes=len(raw),Sha256=digest(raw),HashMode=x['HashMode']);records.append(y)
  if y!=x:changed.append(x['Path'])
 assert set(changed)==allowed,(name,changed,allowed);assert next(x for x in records if x['Path']==execution)['HashMode']=='TextLF'
 updated['Files']=records;assert {k:v for k,v in updated.items() if k!='Files'}=={k:v for k,v in prior.items() if k!='Files'};assert {x['Path'] for x in updated['Files']}=={x['Path'] for x in prior['Files']}
 write(C/name,updated);digest_plan=sha(C/name);assert b'\r' not in (C/name).read_bytes() and not (C/name).read_bytes().startswith(b'\xef\xbb\xbf')
 raw,n=re.subn(rb'(?m)^([ \t]*'+pin.encode()+rb': )[A-F0-9]{64}[ \t]*\r?$',lambda m:m[1]+digest_plan.encode(),(C/wf).read_bytes());assert n==1;(C/wf).write_bytes(raw);owned|={name,wf};plans.append(dict(Path=name,Inputs=count,Sha256=digest_plan,Workflow=wf,Pin=pin,InputPathSetExactParent=True,AllNonFilesImportAbiPolicyFieldsUnchanged=True,ChangedInputRows=changed))
for plan in plans:
 for x in read(C/plan['Path'])['Files']:
  raw=(C/x['Path']).read_bytes();raw=raw.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else raw;assert len(raw)==x['Bytes'] and digest(raw)==x['Sha256']
assert len(owned)==12 and sum(n.endswith('.cs') for n in owned)==7
for n in sdk:assert row(C,n)==sdk[n]
final=[row(C,n) for n in sorted(gm)]
for x in final:
 if x['Path'] not in owned:assert x==pm[x['Path']]
assert len(final)==2397 and len(final)-len(owned)==2385
write(out/'source-resource-closure.json',dict(Files=final));files=[]
for n in sorted(owned):
 x=row(C,n);x.update(Added=n not in pm,ParentSha256=pm[n]['Sha256'] if n in pm else None);files.append(x)
write(out/'promotion-map.json',files)
value=dict(Status='SOURCE_ONLY_FINAL329_PENDING_INDEPENDENT_FINAL_REVIEW',ParentHead=head,ParentSourceFreezeSha256=sha(main),ParentSourceClosureSha256=sha(pc),CandidateDirectory=str(C),SourcePaths=12,CSharpPaths=7,SourceClosurePaths=2397,FinalSourceClosureSha256=sha(out/'source-resource-closure.json'),Files=files,Plans=plans,ActualProofs=proofs,GreenSourceFreezeSha256=sha(gf),GreenSourceClosureSha256=sha(gc),SevenCompiledGreenCSharpRawExact=True,OutsideOwnedParentRowsRawExact=2385,AllNativeNonFilesAndPathSetsExactParent=True,OnlyProductionInputChanged=execution,Docs329BinaryAttributeAdded=True,PublicMfaDelta=0,OrdinaryCasesAppended=8,HostRowsAdded=1,ExpectedFullPassed=6747,HistoricalFailuresRemainSeparateNotTestsPassed=True,GeneralClrStackSafetyClaimed=False,Original329ExecutedByComposer=False,ComposerExecutedSdkNativeVm=False,ComposerSha256=sha(Path(__file__)),CanonicalPublishedParentRows=canonical,Closed0050Sha256=sha(closed))
write(out/'source-freeze.json',value);print(json.dumps(dict(FreezeSha256=sha(out/'source-freeze.json'),ClosureSha256=value['FinalSourceClosureSha256'],Plans=plans)))
