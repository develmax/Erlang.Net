"""Passive actual final target/focus review; no SDK/VM/native execution."""
from pathlib import Path
import hashlib,json
r=Path.cwd().resolve();a=r/'artifacts';d=a/'root-part323-composed1';c=d/'candidate'
h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
sourcepeer=a/'root323-composition-allocator309-source-peer1.json'
assert h(sourcepeer.read_bytes())=='CA997251EF349E5FD4FC8CFD635D1F0142C59D9D3F653E2A8ED5659CE36D8A20'
f=read(d/'source-freeze.json');cl=d/'source-resource-closure.json'
assert h((d/'source-freeze.json').read_bytes())=='C0D5852614A819F20152FFDD8894DA4B119D5E4B1898EB4933EF90519699D524'
assert h(cl.read_bytes())=='B395DACAD8CEC82498A91B59342CC1217A66FE4F4ACA18AF1B8BC8EF63F6ABB7'
for x in read(cl)['Files']:
    z=(c/x['Path']).read_bytes();assert len(z)==x['Bytes'] and h(z)==x['Sha256']
assert read(d/'target-build-exit.json')['ExitCode']==0
log=(d/'target-build.log').read_text(encoding='utf-8-sig')
assert 'Предупреждений: 0' in log and 'Ошибок: 0' in log
assert read(d/'final-layout-result.json')==dict(StyleExitCode=0,SdkWhitespaceExitCode=0,OwnedCSharp=13)
focus=read(d/'actual-final-focus.json')
assert focus['Status']=='PASS' and len(focus['Cases'])==17 and all(x['Status']=='PASS' for x in focus['Cases'])
assert len({x['Name'] for x in focus['Cases']})==17
assert focus['Cases'][-1]['Name']=='ActualMinimalBeamGuestSendHostChecks'
identities=focus['Identities'];assert {x['Name'] for x in identities}=={'Erlang.Tests','Erlang.Runtime','Erlang.Compiler'}
for x in identities:
    p=Path(x['Path']);assert p.is_relative_to(c) and h(p.read_bytes())==x['Sha256']
bindings=[]
for p in [d/'source-freeze.json',cl,d/'actual-final-focus.json',d/'target-build-exit.json',d/'target-build.log',d/'final-layout-result.json',d/'final-style-check.log',d/'final-sdk-whitespace.log',sourcepeer]:
    bindings.append(dict(Path=p.relative_to(r).as_posix(),Bytes=p.stat().st_size,Sha256=h(p.read_bytes())))
report=dict(Status='PASS_PASSIVE_FINAL323_COMPOSITION_ACTUAL_TARGET_AND_SEVENTEEN_GREEN_GROUPS',Passed=True,
    SourceFreezeSha256=h((d/'source-freeze.json').read_bytes()),SourceClosureSha256=h(cl.read_bytes()),
    SourcePaths=30,CSharpPaths=13,SourceRowsRehashed=2349,ExpectedFullPassed=6685,
    TargetWarnings=0,TargetErrors=0,FinalLayoutChecksPassed=True,ActualFocusGroups=17,
    ActualAssemblies=identities,ActualFocusNames=[x['Name'] for x in focus['Cases']],
    PostFocusProgramAndNamesRegistrationChangesActuallyCompiledAndPassed=True,
    GenuineOriginalCompiledMinimalBeamPrepareSupportsInvokePassed=True,
    Original323Executed=False,MainFullOrNativeClaimed=False,
    AllCompositionParentPlanWorkflowAndTwoTestDeltaChecksPassed=True,
    SourceCompositionPeerSha256=h(sourcepeer.read_bytes()),Bindings=bindings,
    SdkBuildNativeVmMainGitMutationByReviewer=False,ReviewScriptSha256=h(Path(__file__).read_bytes()))
target=a/'root323-final-composition-allocator309-actual-peer2.json';assert not target.exists()
target.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(Path=target.relative_to(r).as_posix(),Sha256=h(target.read_bytes()))))
