from pathlib import Path
import hashlib,json,re,subprocess
r=Path(__file__).resolve().parents[1];a=r/'artifacts';d=a/'root-part329-composed1';c=d/'candidate'
j=lambda p:json.loads(p.read_bytes());h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();digest=lambda b:hashlib.sha256(b).hexdigest().upper()
fp=d/'source-freeze.json';cl=d/'source-resource-closure.json'
assert h(fp)=='7B2458C3A03F991CC0A3D25C0119AF644CDBE9F362A9B8B583C6A9BF06C073B1'
assert h(cl)=='911C51D20E8D98B19CE85B40947C4A6D4CBBF245BDF38452C0ABA7ED56C65C64'
f=j(fp);rows=j(cl)['Files'];owned={x['Path']:x for x in f['Files']};assert len(rows)==2397 and len(owned)==12
parent=a/'root-part328-main2/source-resource-closure.json';assert h(parent)==f['ParentSourceClosureSha256'];pm={x['Path']:x for x in j(parent)['Files']};assert len(pm)==2393
for x in rows:
    q=c/x['Path'];assert q.stat().st_size==x['Bytes'] and h(q)==x['Sha256']
    if x['Path'] not in owned:assert x==pm[x['Path']]
for n,x in owned.items():
    assert h(c/n)==x['Sha256'] and (n not in pm)==x['Added'] and x['ParentSha256']==(pm[n]['Sha256'] if n in pm else None)
for n,x in pm.items():assert h(r/n)==x['Sha256'] and (r/n).stat().st_size==x['Bytes']
gf=a/'non-tail-stack329-root-focus5/formatted-source-freeze.json';g=j(gf);assert h(gf)==f['GreenSourceFreezeSha256'];assert len(g['Files'])==7
for x in g['Files']:assert (c/x['Path']).read_bytes()==(gf.parent/'candidate'/x['Path']).read_bytes()
assert len([n for n in owned if n.endswith('.cs')])==7
assert (gf.parent/'tokens-before.json').read_bytes()==(gf.parent/'tokens-after.json').read_bytes()
assert len(j(gf.parent/'tokens-before.json'))==7
bindings=[]
def bind(p):bindings.append(dict(Path=p.relative_to(r).as_posix(),Bytes=p.stat().st_size,Sha256=h(p)))
for x in f['ActualProofs']:
    p=r/x['Path'];assert h(p)==x['Sha256'];v=j(p);assert v['Passed'] is True and v['Status'].startswith('PASS');bind(p)
    for b in v.get('Bindings',[]):
        q=r/b['Path'];assert q.stat().st_size==b['Bytes'] and h(q)==b['Sha256']
expectedchanges=[{'src/Erlang.Compiler/Execution.cs'},{'.gitattributes','.github/workflows/nif-pending-native.yml','src/Erlang.Compiler/Execution.cs','tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'}]
for i,spec in enumerate(f['Plans']):
    p=c/spec['Path'];data=p.read_bytes();assert b'\r' not in data and not data.startswith(b'\xef\xbb\xbf')
    assert h(p)==spec['Sha256']==digest(data.replace(b'\r\n',b'\n'))
    v=j(p);old=j(r/spec['Path']);assert {k:v[k] for k in v if k!='Files'}=={k:old[k] for k in old if k!='Files'}
    vm={x['Path']:x for x in v['Files']};om={x['Path']:x for x in old['Files']};assert vm.keys()==om.keys() and len(vm)==spec['Inputs']==[1120,1145][i]
    changed={n for n in vm if vm[n]!=om[n]};assert changed==expectedchanges[i]==set(spec['ChangedInputRows'])
    for n,x in vm.items():
        assert x['HashMode']==om[n]['HashMode']
        raw=(c/n).read_bytes();raw=raw.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else raw
        assert len(raw)==x['Bytes'] and digest(raw)==x['Sha256']
    workflow=(c/spec['Workflow']).read_text();assert re.search(re.escape(spec['Pin'])+r':\s*'+spec['Sha256'],workflow)
    oldwf=(r/spec['Workflow']).read_text();assert workflow.replace(spec['Sha256'],h(r/spec['Path']))==oldwf
    bind(p)
attrs=(c/'.gitattributes').read_bytes();before=(r/'.gitattributes').read_bytes();assert attrs==before+(b'' if before.endswith(b'\n') else b'\n')+b'docs/validation/part-329/** binary\n'
closed=r/'docs/changelog/0050.md';assert h(closed)==f['Closed0050Sha256']=='4B0D3AE470E5D33449C10F9C2C4E4AF84A53DDCEC729E2E53C49F280114A0F5E'
assert 'docs/changelog/0050.md' not in {x['Path'] for x in rows}
# Read-only actual Git model: each parent file is either exact or declared canonical LF.
requests=b''.join(('bb71b9ce04b21e4ec58ba1d62028f3a2c33c3612:'+n+'\n').encode() for n in pm)
raw=subprocess.check_output(['git','--no-optional-locks','cat-file','--batch'],input=requests,cwd=r);offset=0;canonical=[]
for n in pm:
    end=raw.index(b'\n',offset);header=raw[offset:end].split();size=int(header[2]);start=end+1;blob=raw[start:start+size];offset=start+size+1;work=(r/n).read_bytes()
    if work!=blob:
        assert work.replace(b'\r\n',b'\n')==blob
        canonical.append(dict(Path=n,WorkingSha256=digest(work),PublishedSha256=digest(blob),ComparisonMode='TextLF'))
assert canonical==f['CanonicalPublishedParentRows']
for p in (fp,cl,gf,gf.parent/'formatted-source-resource-closure.json',gf.parent/'tokens-before.json',gf.parent/'tokens-after.json',closed,parent):bind(p)
out=a/'root-part329-final-putdata-source-peer1.json';assert not out.exists()
value=dict(Status='PASS_INDEPENDENT_ACTUAL_FINAL329_SOURCE_COMPOSITION',Passed=True,AuthorFinalFreezeSha256=h(fp),FinalSourceFreezeSha256=h(fp),FinalSourceClosureSha256=h(cl),SourceClosureSha256=h(cl),SourceRowsRehashed=2397,OwnedPaths=12,CSharpPaths=7,OutsideOwnedParentRowsRawExact=2385,All2393RawParentFilesVerified=True,CanonicalPublishedParentRowsVerified=len(canonical),SevenCompiledFocus5SourcesRawExact=True,NativePlanInputs=[1120,1145],NativePlanPathSetsHashModesAndNonFilesExactParent=True,NativeChangedRowsExactApproved=True,CanonicalLfRawStagedModelAndWorkflowPinsEqual=True,Closed0050RawExact=True,ActualFocusProofsAndAllBindingsReplayed=True,GeneralStackSafetyClaimed=False,ReviewerExecutedSdkVmBuildNativeNetworkMainGitMutation=False,Bindings=bindings)
out.write_bytes((json.dumps(value,indent=2)+'\n').encode());print(out,h(out))
