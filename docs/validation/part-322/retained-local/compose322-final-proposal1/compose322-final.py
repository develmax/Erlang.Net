"""Root-executable source composition only; no SDK, VM, native or Git invocation."""
from pathlib import Path
import argparse, copy, hashlib, json, re

p=argparse.ArgumentParser()
for key in ('green-freeze','green-closure','green-guard','red-guard'):
    p.add_argument('--'+key,required=True,type=Path)
    p.add_argument('--'+key+'-sha256',required=True)
p.add_argument('--output',required=True,type=Path)
a=p.parse_args(); r=Path.cwd().resolve(); out=a.output.resolve(); c=out/'candidate'
sha=lambda z:hashlib.sha256(z).hexdigest().upper()
read=lambda p:json.loads(p.read_bytes())
write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode('utf-8'))
def exact(p,h):
    z=p.read_bytes(); assert sha(z)==h.upper(),str(p); return z
def row(root,name):
    z=(root/name).read_bytes();return dict(Path=name,Bytes=len(z),Sha256=sha(z))
def rows(folder,name,digest,count):
    exact(folder/name,digest); result=read(folder/name)['Files']
    assert len(result)==count and len({x['Path'] for x in result})==count
    for x in result:
        assert row(folder/'candidate',x['Path'])==x,x['Path']
    return {x['Path']:x for x in result}
assert out.is_relative_to(r/'artifacts') and not out.exists()
for key in ('green_freeze','green_closure','green_guard','red_guard'):
    exact(getattr(a,key),getattr(a,key+'_sha256'))
assert a.green_freeze_sha256.upper()=='4370C19D8D454424956D3EFEA314269624B976C557352554C1AFAFA2E332629C'
assert a.green_closure_sha256.upper()=='0AAC713323A387F424034A35CE78665161F13BCA5DC2B5C9BF9009EA6412C566'
for file in (a.green_guard,a.red_guard):
    guard=read(file)
    assert guard['Passed'] is True and guard['GreenGroups']==13 and guard['HistoricalRedCases']==2
    assert (guard['GreenSourceRows'],guard['HistoricalRedSourceRows'])==(2314,2309)
    assert guard['SameTwoFormattedHostFilesRawExact'] is True
    for binding in guard['Bindings']: exact(r/binding['Path'],binding['Sha256'])
parent=r/'artifacts/root-part321-composed6'
parenthead='a1a7fe4b8ef81e3cefdd39bc9717ab6855376b4a'
exact(parent/'source-freeze.json','F4A0BB9A8365170872829FC460F67465D9785B51F2E8C32D42DCDE0078E933AB')
exact(r/'artifacts/root-part321-main2/source-freeze.json','2B1A7264ECF3A7C891C30B95555E18D5C07ED85F1CF8131882AD807BB4CC2DE3')
parentmap=rows(parent,'source-resource-closure.json','FC9F58B6E32711ED8A1D7157D288F4CD1CBB04AC3B3EAD4556F46584A8DE220C',2316)
core=r/'artifacts/root-part321-composed5'
coremap=rows(core,'focus-source-resource-closure.json','DAFC935D62C41457400553BE6AD2608F47952E5BB265A2BB2577531C82091450',2307)
metadata={'.gitattributes','.github/workflows/distribution-signal-observations.yml',
    '.github/workflows/nif-pending-native.yml','.github/workflows/nif-reallocation-native.yml',
    'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json',
    'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'}
assert {k for k in coremap if coremap[k]!=parentmap.get(k)}==metadata
assert len(set(parentmap)-set(coremap))==9
assert all(k.startswith('tools/Erlang.GuestConnectedLinkObservations/') for k in set(parentmap)-set(coremap))
green=read(a.green_freeze);g=a.green_freeze.resolve().parent/'candidate'
greenmap=rows(a.green_closure.resolve().parent,a.green_closure.name,a.green_closure_sha256,2314)
owned={x['Path'] for x in green['Files']}
assert len(owned)==10 and all(k.endswith('.cs') for k in owned)
assert all(greenmap[k]==coremap[k] for k in set(greenmap)-owned)
assert set(greenmap)-set(coremap)==owned-set(coremap)
for x in green['Files']:assert row(g,x['Path'])=={k:x[k] for k in ('Path','Bytes','Sha256')}
collector=r/'artifacts/connected-guest-exit322-original-candidate2';af=collector/'source-freeze.json'
exact(af,'1F7CD50EAC19C3CD5AB066D36B67151F763573D38240BE5A7E8E100B3246B628')
peer=r/'artifacts/connected-guest-exit322-original-allocator309-source-peer1/review.json'
exact(peer,'DCE3705DC8FE8E28101C2FDA99186E495F6E2E5274CC27A0327B19930F5BC0BE')
assert str(read(peer)['Status']).startswith('PASS')
tool=read(af);assert len(tool['Files'])==11
for x in tool['Files']:
    assert row(collector/'overlay',x['Path'])=={k:x[k] for k in ('Path','Bytes','Sha256')}
    if x['Path']=='.gitattributes':continue
    assert x.get('ParentSha256')==(parentmap[x['Path']]['Sha256'] if x['Path'] in parentmap else None),x['Path']
# Only after all input preflights: create a fresh detached candidate from immutable published-source authority.
c.mkdir(parents=True)
for name in parentmap:
    d=c/name;d.parent.mkdir(parents=True,exist_ok=True);d.write_bytes((parent/'candidate'/name).read_bytes())
for name in owned:
    d=c/name;d.parent.mkdir(parents=True,exist_ok=True);d.write_bytes((g/name).read_bytes())
keys=set(parentmap)|owned
for x in tool['Files']:
    name=x['Path'];owned.add(name);keys.add(name)
    if name=='.gitattributes':continue
    d=c/name;d.parent.mkdir(parents=True,exist_ok=True);d.write_bytes((collector/'overlay'/name).read_bytes())
attrs=c/'.gitattributes';beforeattrs=attrs.read_bytes()
assert b'docs/validation/part-321/** binary' in beforeattrs.splitlines()
addedattrs=[b'tools/Erlang.GuestConnectedExitObservations/source-plan.json text eol=lf',b'docs/validation/part-322/** binary']
assert all(x not in beforeattrs.splitlines() for x in addedattrs)
attrs.write_bytes(beforeattrs+(b'' if beforeattrs.endswith(b'\n') else b'\n')+b'\n'.join(addedattrs)+b'\n')
newruntime={k for k in owned-set(parentmap) if k.startswith('src/')}
assert newruntime=={'src/Erlang.Runtime/ProcessRuntime.GuestExitSignals.cs',
    'src/Erlang.Runtime/RuntimeDistributionPending.GuestExitSignals.cs',
    'src/Erlang.Serialization/DistributionExplicitPidExitLayout.cs',
    'src/Erlang.Serialization/DistributionPassThroughCodec.ExplicitPidExit.cs'}
plans=[]
contracts=[('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1103,1107),
    ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1128,1132)]
for name,workflow,pin,oldcount,count in contracts:
    before=read(c/name);updated=copy.deepcopy(before);prior={x['Path']:x for x in before['Files']}
    assert len(prior)==oldcount
    names=set(prior)|newruntime;assert len(names)==count and workflow not in names
    records=[]
    for path in sorted(names):
        mode=prior[path]['HashMode'] if path in prior else 'TextLF';assert mode in ('TextLF','BinaryExact')
        z=(c/path).read_bytes();z=z.replace(b'\r\n',b'\n') if mode=='TextLF' else z
        records.append(dict(Path=path,Bytes=len(z),Sha256=sha(z),HashMode=mode))
    updated['Files']=records
    assert {k:v for k,v in before.items() if k!='Files'}=={k:v for k,v in updated.items() if k!='Files'}
    write(c/name,updated);digest=sha((c/name).read_bytes());assert b'\r' not in (c/name).read_bytes()
    raw,n=re.subn(rb'(?m)^([ \t]*'+pin.encode()+rb': )[A-F0-9]{64}[ \t]*\r?$',lambda m:m[1]+digest.encode(),(c/workflow).read_bytes())
    assert n==1;(c/workflow).write_bytes(raw)
    owned|={name,workflow};plans.append(dict(Path=name,Inputs=count,Sha256=digest,Workflow=workflow,Pin=pin,AllNonFilesImportAbiPolicyFieldsUnchanged=True))
for plan in plans:
    for x in read(c/plan['Path'])['Files']:
        z=(c/x['Path']).read_bytes();z=z.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else z
        assert len(z)==x['Bytes'] and sha(z)==x['Sha256'],x['Path']
originalplan=c/'tools/Erlang.GuestConnectedExitObservations/source-plan.json'
exact(originalplan,'F40BA9404BF5BE58BE58108365AB85C6B24E9528DA870355FDFE0126DBD3576B')
assert b'\r' not in originalplan.read_bytes()
for x in read(originalplan)['Sources']:
    z=(originalplan.parent/x['Path']).read_bytes();assert len(z)==x['Bytes'] and sha(z)==x['Sha256']
for x in green['Files']:assert row(c,x['Path'])=={k:x[k] for k in ('Path','Bytes','Sha256')}
assert (len(owned),sum(k.endswith('.cs') for k in owned),len(keys))==(25,10,2332)
finalrows=[row(c,k) for k in sorted(keys)]
for x in finalrows:
    if x['Path'] not in owned:assert x==parentmap[x['Path']],x['Path']
assert all(row(c,k)==parentmap[k] for k in parentmap if k.startswith('tools/Erlang.GuestConnectedLinkObservations/'))
write(out/'source-resource-closure.json',dict(Files=finalrows))
files=[]
for name in sorted(owned):
    x=row(c,name);x.update(Added=name not in parentmap,ParentSha256=parentmap[name]['Sha256'] if name in parentmap else None);files.append(x)
freeze=dict(Status='SOURCE_ONLY_FINAL322_PUBLIC_CONNECTED_EXIT_AND_ORIGINAL_COLLECTOR_PENDING_PEER_FULL_NATIVE_ORIGINAL',
    ParentHead=parenthead,ParentSourceFreezeSha256='2B1A7264ECF3A7C891C30B95555E18D5C07ED85F1CF8131882AD807BB4CC2DE3',
    ParentSourceClosureSha256='FC9F58B6E32711ED8A1D7157D288F4CD1CBB04AC3B3EAD4556F46584A8DE220C',
    SourcePaths=len(owned),CSharpPaths=10,SourceClosurePaths=len(keys),CandidateDirectory=str(c),
    FinalSourceClosureSha256=sha((out/'source-resource-closure.json').read_bytes()),Files=files,Plans=plans,
    ActualCompiledGreenFreeze=dict(Path=str(a.green_freeze.resolve()),Sha256=a.green_freeze_sha256.upper()),
    ActualCompiledGreenClosureSha256=a.green_closure_sha256.upper(),ActualGreenGuardSha256=a.green_guard_sha256.upper(),
    ActualControlledRedGuardSha256=a.red_guard_sha256.upper(),CollectorAuthorFreezeSha256=sha(af.read_bytes()),
    CollectorPlanSha256=sha(originalplan.read_bytes()),CollectorSourcePeerSha256=sha(peer.read_bytes()),
    AllTenActualCompiledSourceFilesRawExact=True,AllNonOwnedPublishedParentRowsRawExact=True,
    All321CollectorAndFixedLinkHostBytesPreserved=True,Source323Excluded=True,OriginalExecutionClaimed=False,
    ComposerExecutedSdkVmNativeBuildOrMainGitMutation=False,ComposerSha256=sha(Path(__file__).read_bytes()))
write(out/'source-freeze.json',freeze)
print(json.dumps(dict(FreezeSha256=sha((out/'source-freeze.json').read_bytes()),ClosureSha256=freeze['FinalSourceClosureSha256'],Owned=len(owned),CS=10,Closure=len(keys),Plans=plans)))
