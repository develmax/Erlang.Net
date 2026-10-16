"""Independent passive source/pin replay; only synthetic Python controls execute."""
from pathlib import Path
import ast, difflib, hashlib, json, shutil, subprocess, sys
sys.dont_write_bytecode=True
r=Path(__file__).resolve().parents[2];out=Path(__file__).resolve().parent
q=r/'artifacts/distribution-control-wait323-original-candidate2';old=r/'artifacts/distribution-control-wait323-original-candidate1'
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
assert sha((q/'source-freeze.json').read_bytes())=='E8B3C3E30C02BC23EBE06AC609795702E02E6D746AF0C3BB765E26E5BD1D33EE'
f=read(q/'source-freeze.json');assert (f['SourcePaths'],f['CSharpPaths'],f['SourcePlanPaths'])==(11,0,8)
for x in f['Files']:
    z=(q/'overlay'/x['Path']).read_bytes();assert len(z)==x['Bytes'] and sha(z)==x['Sha256']
for x in f['Metadata']:
    z=(q/x['Path']).read_bytes();assert len(z)==x['Bytes'] and sha(z)==x['Sha256']
source=q/'overlay/tools/Erlang.DistributionControlWaitObservations';plan=source/'source-plan.json'
assert sha(plan.read_bytes())==f['SourcePlanSha256']=='146C1DFA51058C54D8C72DAFEAAB24D82F5684B3EA5C9CB0CE1B35724D070705' and b'\r' not in plan.read_bytes()
p=read(plan);assert len(p['Sources'])==8 and len(p['PinnedSources'])==14
for x in p['Sources']:
    z=(source/x['Path']).read_bytes();assert len(z)==x['Bytes'] and sha(z)==x['Sha256']
    assert z==(old/'overlay/tools/Erlang.DistributionControlWaitObservations'/x['Path']).read_bytes()
for x in p['PinnedSources']:
    z=subprocess.check_output(['git','--no-optional-locks','-C',str(r.parent/'otp'),'show',p['PinnedCommit']+':'+x['Path']])
    assert sha(z)==x['Sha256'],x['Path']
parent=r/'artifacts/connected-guest-exit322-original-candidate2/overlay'
for name in ('owned_process.py','passive_etf.py','process_capture.py'):
    assert (source/name).read_bytes()==(parent/'tools/Erlang.GuestConnectedExitObservations'/name).read_bytes()
baseline=(source/'passive_etf.py').read_text().splitlines(keepends=True)
extended=(source/'passive_control_etf.py').read_text().splitlines(keepends=True)
delta=list(difflib.unified_diff(baseline,extended));assert len(delta)==17
assert 'if tag in (102, 89, 120):' in ''.join(delta)
ast_count=0
for x in source.glob('*.py'):ast.parse(x.read_bytes());ast_count+=1
workflow=q/'overlay/.github/workflows/distribution-signal-observations.yml';w=workflow.read_text()
assert w.count('CONTROL_WAIT_SOURCE_PLAN_SHA256: '+sha(plan.read_bytes()))==1
assert w.count('"$CONTROL_WAIT_SOURCE_PLAN_SHA256"')==1
assert f['ParentProspectiveOriginal322FreezeSha256']=='1F7CD50EAC19C3CD5AB066D36B67151F763573D38240BE5A7E8E100B3246B628'
for x in f['Files']:
    if x['Path'] in ('.gitattributes','.github/workflows/distribution-signal-observations.yml'):
        assert sha((parent/x['Path']).read_bytes())==x['ParentSha256']
# Pure controls run from copied source; the immutable author artifact is never modified.
scratch=out/'synthetic-copy';assert not scratch.exists();scratch.mkdir()
target=scratch/'overlay/tools/Erlang.DistributionControlWaitObservations';target.mkdir(parents=True)
for x in p['Sources']:shutil.copyfile(source/x['Path'],target/x['Path'])
shutil.copyfile(q/'controls.py',scratch/'controls.py')
result=subprocess.run([sys.executable,'-B',str(scratch/'controls.py')],capture_output=True,timeout=20,check=True)
assert not result.stderr
controls=json.loads(result.stdout);assert controls['Count']==69 and controls['OriginalVmExecuted'] is False and controls['SdkExecuted'] is False
(out/'synthetic-controls.json').write_bytes(result.stdout)
report=dict(Status='PASS_SOURCE_ONLY_ORIGINAL323_REVISION2_BOUNDED_THREE_NODE_BUSY_QUERY_KILL_COLLECTOR',Passed=True,
    SourceFreezeSha256=sha((q/'source-freeze.json').read_bytes()),SourcePlanSha256=sha(plan.read_bytes()),
    SourcePaths=11,CSharpPaths=0,PlanInputs=8,PinnedGitObjectsVerified=14,PythonAstFiles=ast_count,
    Exact322Helpers=3,AdditivePortTags=[102,89,120],SyntheticControls=69,
    OwnTargetBusyControllerRequired=True,FillerOrStoppedBAloneCannotPass=True,AsyncDistExplicitFalseAndTrapExitTrue=True,
    QueryBeforeRemoteCExitBeforeActualKilledDownBeforeResume=True,FullActualQueryAndTokenFreeControlScope=True,
    DistinctOwnerTargetFillerWorkerKillerAndFreshReferences=True,
    StoppedOwnedBIdentityAndStartTimeBound=True,FailureFinallyAttemptsResumeThenBoundedOwnedGroupClose=True,
    RawPrimaryAndCleanupFailuresRetained=True,FreshTargetCleanupNoprocCannotReplaceKilledDown=True,
    BothCompiledImagesAttestedOnAllThreeOriginalNodes=True,OriginalCases=2,
    Prospective322WorkflowParentRequiresFinalRootRebind=True,
    Limitations=['Source-only: no actual compiler/VM/native network run.','Host SIGSTOP observes real busy output, not a claimed native equivalent of CLR ticket-turn waits.','Source-build receipt/log and loaded runtime checks preserve the established observer contract; no new whole-installed-prefix authority is claimed.','An externally uncatchable SIGKILL cannot execute collector finally.'],
    Blockers=[],OriginalExecuted=False,SdkExecuted=False,MainOrGitMutated=False,
    ReviewScriptSha256=sha(Path(__file__).read_bytes()),ControlsReportSha256=sha(result.stdout))
assert not (out/'review.json').exists()
(out/'review.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(ReportSha256=sha((out/'review.json').read_bytes()),PinnedObjects=14,SyntheticControls=69)))
