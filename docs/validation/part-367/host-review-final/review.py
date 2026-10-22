import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def write(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')

prior = json.loads((ROOT / 'artifacts/queue-integration367-source-review2/proposed-mfa-compatibility.json').read_text())
assert len(prior['VerifiedSourceFiles']) == 357
for row in prior['VerifiedSourceFiles']:
    assert sha(ROOT / row['Path']) == row['Sha256'], row['Path']
names = [
    'tests/Erlang.Tests/QueueCoreReverseEntryHostChecks.cs',
    'tests/Erlang.Tests/QueueCoreReverseEntryHostNames.cs',
    'tests/Erlang.Tests/QueueCooperativeHostChecks.cs',
    'tests/Erlang.Tests/QueueCooperativeHostNames.cs',
    'src/Erlang.Runtime/ModuleRegistry.cs',
    'src/Erlang.Runtime/ModuleRegistry.ProcessScheduling.cs',
    'src/Erlang.Runtime/ProcessRuntime.ExternalTerms.cs',
    'src/Erlang.Runtime/RuntimeExceptionCapture.cs',
    'src/Erlang.Runtime/QueueCoreModule.cs',
    'src/Erlang.Runtime/QueueCoreExecution.cs',
    'src/Erlang.Runtime/QueueCoreLinear.cs',
    'src/Erlang.Runtime/QueueCoreReverse.cs',
    'src/Erlang.Runtime/ProcessContext.ListTraversal.cs',
    'src/Erlang.Runtime/ProcessContext.cs',
]
bindings = [{'Path': name, 'Bytes': (ROOT / name).stat().st_size, 'Sha256': sha(ROOT / name)} for name in names]
write('source-bindings.json', bindings)
write('review.json', {
    'Status': 'SOURCE_REVIEW_HOST_ACCESS_REPAIR_PRESERVES_ACTUAL_BOUNDARIES',
    'Production357SourceHashesStillExact': True,
    'FrozenAudit2SealSha256': sha(ROOT / 'artifacts/queue-integration367-source-review2/seal.json'),
    'Repair': 'Two HOST files replace inaccessible internal API calls with public ReduceAsync/public registry dispatch and reflection of an existing getter.',
    'BudgetAdmission': [
        'Reflection reads only RemainingListTraversalReductions via GetValue. There is no SetValue, field mutation or synthetic budget injection.',
        'The actor-owned loop calls public ReduceAsync until the getter returns1. Existing modulo2000 ChargeAsync logic reaches1 from any valid remaining slice. No competing actor mutates this context.',
        'Expected suffix/accumulator construction after that read uses ordinary host operations, not actor reductions. Public queue:to_list/1 receives tuple(original,Nil).',
        'Public ModuleRegistry.Call dispatches the registered queue body in the already-owned actor turn. Observation, tracing, wrapper and admission paths do not charge reductions; QueueCoreExecution.Require is shallow and does not charge. First reverse entry therefore still sees actual remaining1.',
        'ChargeAsync(0) inside reverse validates controls without decrementing that budget. Normal public dispatch supplies the same registered source wrapper/error boundary used by guest queue calls.',
    ],
    'Boundaries': [
        {'ImproperPrefix':39, 'CommittedBeforeFailure':0},
        {'ImproperPrefix':40, 'CommittedBeforeFailure':0},
        {'ImproperPrefix':41, 'CommittedBeforeFailure':40},
        {'ImproperPrefix':81, 'CommittedBeforeFailure':40},
        {'ImproperPrefix':80041, 'CommittedBeforeFailure':80040},
    ],
    'Assertions': 'All five cases independently aggregate errors; assert badarg, top lists:reverse, two effective args, same suffix object and exact accumulated values. Wrong successful return fails. No exception is converted to PASS without those checks.',
    'BoundaryReason': 'At40 cells the malformed lookahead endpoint is already visible and fails before copy. At41 first40 commit, then next full2000-reduction entry sees improper41-residue endpoint before copying it. At80041 a second80000-cell chunk commits, then residual1 fails.',
    'KnownLimits': 'Reflection member renaming fails the test rather than passing; this static review does not replace actual focus3 build/run. No claim that native and own VM use identical entry budgets/heap paths.',
    'RuntimeChanges':0, 'MainWrites':0, 'BuildOrSdkOrNativeOrCiExecution':False,
})
(OUT / 'README.md').write_text('''# Queue reverse HOST access repair review\n\nThe repaired two HOST files preserve the five actual-budget1 boundary assertions while using public runtime dispatch. Reflection only reads the existing actor-owned getter; public ReduceAsync advances the real budget. The public queue:to_list path has no reduction charge before entering reverse. All357 production hashes remain exact against the sealed source-review2 proposal.\n\nThe initial CS1061/CS0122 test compilation failure remains genuine prior evidence. This supplemental review does not change that receipt or the frozen source proposal/audit. Actual repaired build and focus results belong to Root; this review performs no build or runtime execution.\n''', encoding='utf-8')
write('seal.json', {'Files':[{'Path':p.name,'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(OUT.iterdir()) if p.is_file() and p.name != 'seal.json']})
print(json.dumps({'SealSha256':sha(OUT/'seal.json'), 'ReviewSha256':sha(OUT/'review.json'), 'BindingsSha256':sha(OUT/'source-bindings.json')}))
