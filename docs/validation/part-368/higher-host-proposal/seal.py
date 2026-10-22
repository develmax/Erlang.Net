import hashlib
import json
from pathlib import Path

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def write(name,value):
    (OUT/name).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')

files=[]
for path in sorted((OUT/'source').rglob('*.cs')):
    destination=path.relative_to(OUT/'source').as_posix()
    assert not (ROOT/destination).exists(),destination
    files.append({'Source':path.relative_to(ROOT).as_posix(),'Destination':destination,'BaseSha256':None,'Bytes':path.stat().st_size,'Sha256':sha(path)})
assert len(files)==4
dependencies=[
 'tests/Erlang.Tests/RuntimeNodeFoundationHostActor.cs',
 'tests/Erlang.Tests/RuntimeNodeFoundationHostActorParameters.cs',
 'tests/Erlang.Tests/Program.cs',
 'src/Erlang.Runtime/QueueHigherFilters.cs',
 'src/Erlang.Runtime/QueueHigherDelete.cs',
 'src/Erlang.Runtime/QueueHigherExecution.cs',
 'src/Erlang.Runtime/QueueHigherTrace.cs',
 'src/Erlang.Runtime/QueueHigherHistory.cs',
 'src/Erlang.Runtime/QueueCoreLinear.cs',
 'src/Erlang.Runtime/QueueCoreReverse.cs',
 'src/Erlang.Runtime/ProcessContext.cs',
 'src/Erlang.Runtime/ProcessSchedulingLimits.cs',
]
write('copy-plan.json',{'Status':'NEW_TEST_SOURCE_ONLY_NOT_BUILT_OR_RUN','Files':files,
 'Dependencies':[{'Path':path,'Sha256':sha(ROOT/path)} for path in dependencies],
 'RootOnlyRegistration':'Test(Erlang.Tests.QueueHigherCooperativeHostNames.Test, Erlang.Tests.QueueHigherCooperativeHostChecks.Run);',
 'NewHostNames':1,'RuntimeChanges':0,'ExistingCoreTestsChanged':0})
write('coverage.json',{
 'Status':'SOURCE_ONLY_PHASE_DESIGN_ACTUAL_EXECUTION_PENDING',
 'InputCells':80017,'Scenarios':8,'NormalAndCancellationBoundaries':16,
 'Phases':[
  {'Scenario':'FilterFrontRebuild','Checkpoint':'callbacks==Cells and actual continuation queued','SourceReason':'Front cannot enter output reconstruction until all callbacks finish. Cells80017 leaves many prefix entries; the first queued yield after final callback is in the iterative pending-output loop, before balancing.'},
  {'Scenario':'FilterRearUnwind','Checkpoint':'0<callbacks<Cells and actual continuation queued','SourceReason':'Rear callbacks start only after complete stack collection. Pause is inside reverse-order unwind, not the earlier input scan.'},
  {'Scenario':'DeleteFrontRebuild','Checkpoint':'last front callback matches, callbacks==Cells, actual continuation queued','SourceReason':'Only the last item matches, leaving Cells-1 prefix entries. Following real yield is in prefix reconstruction.'},
  {'Scenario':'DeleteRearRebuild','Checkpoint':'first reverse-order callback matches, callbacks==1, actual continuation queued','SourceReason':'First unwind match suppresses later callbacks and rebuilds Cells-1 pending items; first following real yield is reconstruction.'},
  {'Scenario':'DeleteFrontNoMatch','Checkpoint':'0<callbacks<Cells and actual continuation queued','SourceReason':'Accurately labelled scan control; no-match path does not rebuild and must return original queue object.'},
  {'Scenario':'DeleteRearNoMatch','Checkpoint':'0<callbacks<Cells and actual continuation queued','SourceReason':'Accurately labelled rear unwind control after complete collection; no-match path must return original queue object.'},
  {'Scenario':'Split','Checkpoint':'first actual continuation queued','SourceReason':'Large front-only split must yield during real source length/move work. Not labelled later reconstruction.'},
  {'Scenario':'Join','Checkpoint':'first actual continuation queued','SourceReason':'Large rear-only left plus front-only right forces a real reverse40cells/reduction trap. Not labelled later reconstruction.'},
 ],
 'Pump':'Channel stores only actual SynchronizationContext.Post continuations from production awaits. Resume one real continuation at a time, restore actual synchronization context, stop only with queued continuation plus phasepredicate; operation completing early or never reaching phase fails. No artificial Task.Yield/Delay or synthetic asynchronous callback.',
 'Observers':'Independent public runtime actor observes process reductions and counter remains stable while actual continuation withheld. Kill sent by real runtime.Exit; resumed operation must cancel, worker completion killed, finally signal observed, pid dead, no later callbacks, runtime still responds to queue:new.',
 'NormalOracle':'Exact callback counts; filter list length/payloads/persistence; delete countCells-1 for genuine match and same original queue object for no-match; split exact halves; join exact2*Cells contents; input retained.',
 'Cleanup':'Independent cleanup actor kills incomplete worker, drains actual captured continuations with bounded waits, awaits actual worker completion and finally signal. All16 independent failures aggregate.',
 'KnownLimits':'Source-specific phase labels bind exact current loop placement in dependency hashes. A future implementation may complete with fewer physical reductions; retain failure rather than weakening the checkpoint. These controls do not assert native timing, allocation or GC equivalence.',
 'NoMainWritesBuildSdkNativeCiNetwork':True})
(OUT/'README.md').write_text('''# Queue higher-operation later-phase HOST proposal\n\nFour new test classes/files and one HOST registration add16 independent normal/cancellation controls. Current core test files, Program and runtime remain untouched. This proposal is source-only and has not been built or executed. Root owns style, integration and actual validation.\n\nThe real continuation pump stops after public callback counters establish a specific source phase and a real production continuation is queued. Front filter and last-front-match delete have completed their callback scan before pausing during reconstruction. Rear filter starts callbacks only after full collection; first-rear-match delete has entered reconstruction with further callbacks suppressed. Separate no-match controls are deliberately labelled scan/unwind: no-match returns the original queue rather than reconstructing it. Large split/join test genuine first-yield paths without claiming a later phase.\n\nEach scenario runs normal and kill. Normal outputs and exact callback counts are checked independently; no-match requires the same original queue object. Cancellation requires actual killed completion, canceled operation, finally/physical unwind, no further callbacks, dead process and a still usable runtime. The pump runs actual captured callbacks, creates no fake async callback, sleeps or GC assertions, and uses bounded waits. All16 outcomes aggregate.\n\nRegister once:\n\n```csharp\nTest(Erlang.Tests.QueueHigherCooperativeHostNames.Test, Erlang.Tests.QueueHigherCooperativeHostChecks.Run);\n```\n\nAll four destinations must be absent. The copy plan binds current runtime loop locations and actor helper source assumptions; a changed implementation requires phase applicability review. Existing runtime source/guards need no refresh for this test-only packet.\n''',encoding='utf-8')
write('source-seal.json',{'Files':[{'Path':p.relative_to(OUT).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(OUT.rglob('*')) if p.is_file() and p.name!='source-seal.json']})
print(json.dumps({'CopyPlanSha256':sha(OUT/'copy-plan.json'),'SealSha256':sha(OUT/'source-seal.json'),'CoverageSha256':sha(OUT/'coverage.json')}))
