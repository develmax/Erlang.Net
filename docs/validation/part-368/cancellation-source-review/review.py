import hashlib
import json
from pathlib import Path

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[1]

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def write(name,value):
    (OUT/name).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')

trace=ROOT/'src/Erlang.Runtime/QueueHigherTrace.cs'
host=ROOT/'tests/Erlang.Tests/QueueHigherCooperativeHostChecks.cs'
assert trace.read_text().index('await context.ChargeAsync(0);') < trace.read_text().index('using var history = new QueueHigherScope(context, pending);')
assert 'Cancellation.IsCancellationRequested' in host.read_text()
assert 'Volatile.Read(ref callbacksAfterCancellation) == 0' in host.read_text()
paths=[
 'src/Erlang.Runtime/QueueHigherTrace.cs','src/Erlang.Runtime/ProcessRuntime.cs',
 'src/Erlang.Runtime/ProcessContext.cs','tests/Erlang.Tests/QueueHigherCooperativeHostChecks.cs',
 'tests/Erlang.Tests/QueueHigherCooperativeHostNames.cs','tests/Erlang.Tests/QueueHigherYieldContext.cs',
 'tests/Erlang.Tests/QueueHigherCooperativeScenario.cs',
 'artifacts/queue-integration368/higher-cooperative1/report.json',
 'artifacts/queue-higher368-cooperative-proposal1/source-seal.json',
]
write('source-bindings.json',[{'Path':path,'Sha256':sha(ROOT/path),'Bytes':(ROOT/path).stat().st_size} for path in paths])
write('review.json',{
 'Status':'ACTUAL_HOST_EXPOSED_REAL_CANCELLATION_ENTRY_GAP_SOURCE_FIX_REVIEWED_EXECUTION_PENDING',
 'FailedActualCases':['FilterRearUnwind cancel','DeleteFrontNoMatch cancel','DeleteRearNoMatch cancel'],
 'CorrectionToPriorCommentary':'The earlier queued-signal explanation was wrong for this public API. ProcessRuntime.Exit calls Signal, which calls Terminate directly for kill; Terminate cancels the process token via Stop.Cancel. The original failed control is not dismissed as infrastructure or invalid-test-only.',
 'FailureMechanism':[
  'ChargeAsync at a real reduction yield resumes and performs its post-yield cancellation check before completing its awaitable.',
  'The consumer continuation can then remain queued in the actual test SynchronizationContext. A direct kill can cancel the worker after that completed check but before the consumer continuation resumes.',
  'Before the fix, QueueHigherTrace.Invoke had no callback-entry checkpoint. That resumed consumer could invoke a callback with an already-canceled process token before reaching the next traversal reduction.',
 ],
 'ReviewedFix':[
  'Await actual context.ChargeAsync(0) as the first statement of QueueHigherTrace.Invoke, before history/current generated-frame scopes and callback invocation.',
  'Zero charge performs genuine halt/cancellation/suspension admission without incrementing reductions. It closes the already-canceled consumer-entry window while retaining the native source reduction/own-entry protocols.',
  'Generated frames and callback exception capture remain after the checkpoint. Normal guest error frame fields and argument order are unchanged; OperationCanceledException remains actor control flow, not a synthetic guest stack.',
 ],
 'ReviewedHostRepair':'Each actual callback reads the public ProcessContext.Cancellation token at entry and counts callbacks entered after cancellation. Cancel outcomes require zero such callbacks plus canceled operation/killed completion/finally-unwind/deadPID. Counters may advance before token cancellation; request-time equality is not used as the sole oracle.',
 'SourceScopeLimit':'This is cooperative callback-entry admission, not asynchronous preemption of a callback already running or an atomic cross-thread guarantee against cancellation arriving after the checkpoint. The deterministic witness cancels while the actual continuation is held, then resumes it.',
 'PreservedEvidence':'Frozen proposal1 and first actual failed report remain unchanged. No failure reclassification or assertion weakening to green occurred in this review.',
 'RequiredGates':['Root reruns all16 repaired HOST boundaries against fixed Main DLLs.','Fresh final native replay after this production delta, including all whole-frame callback profiles.','Refresh QueueHigherTrace guard to exact styled final hash; retain all prior ledger classifications until actual gates pass.'],
 'NoMainWritesBuildSdkNativeCiNetwork':True,'NewClassifications':0,
})
(OUT/'README.md').write_text('''# Queue callback cancellation-entry correction\n\nThe first actual later-phase HOST found a real production gap. Public Exit uses direct Signal/Terminate and Stop.Cancel; the earlier queued-signal explanation was incorrect. A completed ReduceAsync cancellation check could precede the queued consumer continuation, allowing a callback to enter after that continuation was resumed with its token already canceled.\n\nThe reviewed fix adds genuine zero-charge cancellation/halt/suspension admission at QueueHigherTrace.Invoke entry, before callback frames and invocation. The repaired HOST counts actual callback entries with an already-canceled token and retains canceled/killed/physical-unwind requirements. This is cooperative entry admission, not preemption of an already-running callback.\n\nExact source and original failure-report hashes are bound. This review is source-only; final HOST execution, native replay and guard refresh remain Root gates. No Main edits, builds or runtime execution were performed by reviewer.\n''',encoding='utf-8')
write('seal.json',{'Files':[{'Path':p.name,'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='seal.json']})
print(json.dumps({'SealSha256':sha(OUT/'seal.json'),'ReviewSha256':sha(OUT/'review.json'),'TraceSha256':sha(trace)}))
