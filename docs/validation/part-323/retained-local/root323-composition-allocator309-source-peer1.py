"""Read-only final323 source composition audit; no target/build/VM execution."""
from pathlib import Path
import hashlib,json,re
r=Path.cwd().resolve();a=r/'artifacts';d=a/'root-part323-composed1';c=d/'candidate'
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
fp=d/'source-freeze.json';cl=d/'source-resource-closure.json'
assert sha(fp.read_bytes())=='C0D5852614A819F20152FFDD8894DA4B119D5E4B1898EB4933EF90519699D524'
assert sha(cl.read_bytes())=='B395DACAD8CEC82498A91B59342CC1217A66FE4F4ACA18AF1B8BC8EF63F6ABB7'
f=read(fp);rows=read(cl)['Files'];owned={x['Path']:x for x in f['Files']}
assert (f['SourcePaths'],f['CSharpPaths'],len(rows),f['ExpectedFullPassed'])==(30,13,2349,6685)
parentcl=a/'root-part322-main1/source-resource-closure.json'
assert sha(parentcl.read_bytes())=='4D4469862CACFE37DF2B0AFBF4A91BFB3A99BC7B04285192296E459E3FBF1042'
parent={x['Path']:x for x in read(parentcl)['Files']};assert len(parent)==2332
for x in rows:
    raw=(c/x['Path']).read_bytes();assert len(raw)==x['Bytes'] and sha(raw)==x['Sha256']
    if x['Path'] not in owned:assert x==parent[x['Path']]
for name,x in owned.items():
    assert x['Added']==(name not in parent) and x['ParentSha256']==(parent[name]['Sha256'] if name in parent else None)
wait=a/'distribution-control-wait323-focus1';tiny=a/'actual-minimal-bang321-sdk-focus1'
assert sha((wait/'formatted-source-freeze.json').read_bytes())==f['ActualCompiledControlWaitFreezeSha256']
assert sha((tiny/'formatted-source-freeze.json').read_bytes())==f['ActualCompiledTinyFreezeSha256']
for x in read(wait/'formatted-source-freeze.json')['Files']:
    if x['Path']=='tests/Erlang.Tests/Program.cs':continue
    assert (c/x['Path']).read_bytes()==(wait/'candidate'/x['Path']).read_bytes()
for x in read(tiny/'formatted-source-freeze.json')['Files']:
    if x['Path']=='tests/Erlang.Tests/ActualMinimalBeamGuestSendHostNames.cs':continue
    assert (c/x['Path']).read_bytes()==(tiny/'candidate'/x['Path']).read_bytes()
program='tests/Erlang.Tests/Program.cs';addition='Test(Erlang.Tests.ActualMinimalBeamGuestSendHostNames.Test, Erlang.Tests.ActualMinimalBeamGuestSendHostChecks.Run);\n'
text=(c/program).read_text();assert text.count(addition)==1 and text.replace(addition,'')==(wait/'candidate'/program).read_text()
names='tests/Erlang.Tests/ActualMinimalBeamGuestSendHostNames.cs';addition='    internal const string Test = "host_actual_original_minimal_beam_connected_bang";\n'
text=(c/names).read_text();assert text.count(addition)==1 and text.replace(addition,'')==(tiny/'candidate'/names).read_text()
newruntime={'src/Erlang.Runtime/ProcessContext.DistributionWait.cs'}
for p in f['Plans']:
    path=c/p['Path'];raw=path.read_bytes();assert sha(raw)==p['Sha256'] and b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf')
    plan=read(path);before=read(r/p['Path'])
    assert {k:v for k,v in plan.items() if k!='Files'}=={k:v for k,v in before.items() if k!='Files'}
    assert len(plan['Files'])==p['Inputs']
    assert {x['Path'] for x in plan['Files']}-{x['Path'] for x in before['Files']}==newruntime
    for x in plan['Files']:
        raw=(c/x['Path']).read_bytes();assert x['HashMode'] in ('BinaryExact','TextLF')
        if x['HashMode']=='TextLF':raw=raw.replace(b'\r\n',b'\n')
        assert len(raw)==x['Bytes'] and sha(raw)==x['Sha256']
    assert re.findall(r'^\s*'+p['Pin']+r': ([A-F0-9]{64})$',(c/p['Workflow']).read_text(),re.M)==[p['Sha256']]
assert sorted(x['Inputs'] for x in f['Plans'])==[1108,1133]
collector=a/'distribution-control-wait323-original-candidate2';source=c/'tools/Erlang.DistributionControlWaitObservations'
assert sha((collector/'source-freeze.json').read_bytes())==f['CollectorAuthorFreezeSha256']
assert sha((source/'source-plan.json').read_bytes())=='146C1DFA51058C54D8C72DAFEAAB24D82F5684B3EA5C9CB0CE1B35724D070705'
for x in read(source/'source-plan.json')['Sources']:
    raw=(source/x['Path']).read_bytes();assert len(raw)==x['Bytes'] and sha(raw)==x['Sha256']
    assert raw==(collector/'overlay/tools/Erlang.DistributionControlWaitObservations'/x['Path']).read_bytes()
assert sha((a/'distribution-control-wait323-original-allocator309-source-peer1/review.json').read_bytes())==f['CollectorSourcePeerSha256']
attrs=(c/'.gitattributes').read_bytes();assert (r/'.gitattributes').read_bytes() in attrs
assert b'docs/validation/part-323/** binary' in attrs.splitlines()
assert b'tools/Erlang.DistributionControlWaitObservations/source-plan.json text eol=lf' in attrs.splitlines()
beam=c/'tests/Erlang.Tests/Resources/Beam/guest_connected_bang322.beam';assert sha(beam.read_bytes())==f['ActualOriginalMinimalBeamSha256']
report=dict(Status='PASS_SOURCE_ONLY_FINAL323_COMPOSITION_PENDING_ROOT_FINAL_TARGET_AND_FULL',Passed=True,
    SourceFreezeSha256=sha(fp.read_bytes()),SourceClosureSha256=sha(cl.read_bytes()),SourcePaths=30,CSharpPaths=13,CompleteRowsRehashed=2349,
    PublishedParentHead=f['ParentHead'],ParentRows=2332,AllOutsideOwnedRowsExact=True,AllOwnedParentBindingsExact=True,
    SevenActualCompiledWaitRuntimeAndHostBodiesExact=True,GenuineBeamHostBodyResourceExact=True,
    PostFocusTwoTestOnlyChanges=['one Program registration','one semantic Names.Test constant'],
    NativePlanInputs=[1108,1133],AllNativeNonFilesPoliciesExactParent=True,EveryNativePlanInputAndWorkflowSealVerified=True,
    Original323PlanAndEightSourcesExactIndependentPeer=True,AttrsMergedPreservingPublished322=True,
    FinalCompositionTargetOrFullClaimed=False,Original323Executed=False,ReviewerSdkNativeVmMainGitMutation=False,
    ReviewSourceSha256=sha(Path(__file__).read_bytes()))
out=a/'root323-composition-allocator309-source-peer1.json';assert not out.exists()
out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(Sha256=sha(out.read_bytes()),Path=out.relative_to(r).as_posix())))
