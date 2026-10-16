from pathlib import Path
import json

r = Path.cwd()
d = r / 'artifacts/proc-lib333-full-source'
g = json.loads((d / 'full-guard.json').read_bytes())
assert (g['Passed'], g['Failed'], g['Registry'], g['ProcLibMfas']) == (6871, 0, 528, 19)
assert json.loads((r/'artifacts/root-part333-promotion.json').read_bytes())['OwnedPaths'] == 18

def write(path, text):
    (r / path).write_bytes(text.encode())

def prepend(path, text):
    p = r / path
    heading, body = p.read_text(encoding='utf-8-sig').split('\n', 1)
    write(path, heading + '\n\n' + text + '\n\n' + body.lstrip('\n'))

summary = ('Part333 implements seven local proc_lib APIs: start_monitor/3,4,5, start/5, start_link/5 and spawn_opt/2,4. '
           'Actual6871/6871 and eleven integration/package outcomes pass in one uninterrupted isolated validation, retaining all6822 prior ordered names and all521 registry rows/custom Limits. '
           'Exact tested source is promoted without a redundant local full run. Registry528 across eight modules; proc_lib19/39, erlang351 unchanged. '
           'Published332 comparison is complete: original6001/6001 plus fd8, all5962 previous contracts and all39 new source/local/original/SDK chains match with zero strict ordinary variance; all five terminal typed scopes pass. '
           'New333 original comparison and current-head CI remain pending publication. Presence is not whole-module completion. Next: gen/sys local process protocols and the remaining proc_lib introspection/lifecycle contracts.')
for path in ['AGENTS.md','docs/PROGRESS.md','docs/COMPATIBILITY.md']:
    prepend(path, summary)
prepend('docs/NEXT_STEPS.md', 'Part333 next: publish and audit exact-head CI and original observations for monitored/option-bearing local startup. Continue the minimal gen/sys system protocol, state inspection and suspend/resume required by local behaviours; supply proc_lib introspection/lifecycle contracts where those source modules call them. Other valid OTP heap/scheduler tuning options and remote proc_lib overloads remain explicit gaps. Keep LICENSE.local.txt and unrelated caches out of commits.')
prepend('docs/ERLANG_COMPATIBILITY.md', 'Part333 leaves351 erlang registrations unchanged. A narrow existing spawn_opt option-error parser helper is shared with proc_lib so builtin wrapper arguments and badopt metadata follow the same path. No new scheduler/heap configuration or physical-resource parity is claimed. Existing contracts remain covered by6871 tests; new333 original observations remain pending.')
prepend('docs/semantic-differences.md', 'Part333: start_monitor ACK retains its original real monitor. NACK, early DOWN and timeout paths send the same consumed DOWN to the mailbox tail through actual erlang send. Option-bearing startup checks MFA guards before ordered lists:member/keyfind monitor rejection; wrong-arity monitor tuples retain case_clause. Existing option parsing, atomic links/monitors and spawn wrapper metadata are reused. Other native tuning options, remote overloads, full stack/tracing/scheduler/physical parity remain incomplete.')
prepend('docs/DECISIONS.md', 'Part333 — Share the existing startup wait core while retaining the old distinct four-argument helper and its contract. Ordinary startup removes the monitor; monitored startup retains it or requeues the original DOWN. Reuse actual lists functions for the ordered monitor-option precheck and existing ProcessSpawnOptions/SpawnWithOptions for creation. Expose only a narrow internal existing parser/error helper; no new actor framework, scheduler, production BEAM dependency or package. The full candidate binds six package resources before execution, avoiding the prior missing-README setup error.')
prepend('docs/dependency-decisions.md', 'Part333 — Source/license/reuse: pinned OTP29.1.1 proc_lib.erl supplies sync_start_monitor and VERIFY_NO_MONITOR_OPT contracts. Existing project C# process, monitor, mailbox, option parser, sequence descriptions and BCL mechanisms remain the implementation; existing licensing decisions apply and no dependency is added. A separate actor framework would duplicate ownership and alter mailbox/failure semantics. Native tuning options unsupported by the current parser remain unsupported, rather than reporting fabricated heap/scheduler effects.')

write('docs/PROC_LIB_COMPATIBILITY.md', '''# proc_lib local process foundation

Reference: [OTP29.1.1 proc_lib.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/proc_lib.erl).

Part333 registers19 of39 declarations. Status: **Partially compatible**.
Registry528 across eight modules; erlang351 unchanged. API presence does not
establish complete compatibility or a performance improvement.

| APIs | Implemented local contract |
|---|---|
| spawn/1,3; spawn_link/1,3 | Existing atomic creation, ancestry, caller/child guard boundaries and initial-call metadata |
| init_ack/1,2 | Ancestry or explicit parent; arbitrary acknowledgement return term |
| start/3,4,5; start_link/3,4,5 | Real monitor, optional atomic link, arbitrary ACK return, real DOWN after NACK, unlink-before-timeout kill and selective EXIT cleanup |
| start_monitor/3,4,5 | Returns {Ret,Ref}; ACK keeps the original monitor; failure/death/timeout requeues the actual consumed DOWN at the mailbox tail |
| spawn_opt/2,4 | Fun or MFA wrapper metadata and ancestry; existing option parser, atomic relations and PID or {PID,Ref} return |
| init_fail/2,3 | NACK before error/exit/throw or explicit-stack raise |

Startup's user monitor options are rejected through the source's two ordered
lists scans after MFA guards and before parent lookup. A monitor atom or two-tuple
raises badarg; a matching monitor tuple of another arity raises case_clause.
Options otherwise inherit the current runtime's link/monitor relation flags and
async_dist support. Public spawn_opt can use supported monitor tags/aliases;
startup's own monitor remains controlled by its protocol. Other valid native
heap/scheduler tuning options remain unsupported. This is not unrestricted OTP
spawn-option compatibility.

Creator ancestry uses its registered name or PID and retains list-shaped tails.
Native initial_call describes proc_lib:init_p, while '$initial_call' describes
the user call or source-defined gen translation. The shared receive route keeps
timeout expression evaluation before selection and defers value validation until
a genuine no-match wait. Existing SDK zero-timeout snapshot scheduling remains;
exact VM scheduling under continuous arrivals is unclaimed.

Actual6871/6871 and eleven integration/package checks pass in one isolated full
run. All6822 previous ordered tests and521 registry rows/custom Limits remain.
This slice adds41 compiled scenarios, seven direct API contracts and one HOST
group containing three deterministic real queued ACK/NACK/DOWN controls. Two
pre-full test-source syntax errors (reserved maybe atom and catch stack pattern)
were corrected without production changes or weakened assertions; failed receipts
remain preserved. Six tracked package resources were bound before validation.
Exact source bytes are promoted; current-head CI and original333 remain pending.

Published332 passes original6001/6001 plus fd8 and all five terminal typed scopes.
All39 added startup/receive scenarios have matching published source, local actual,
original and SDK observations; strict ordinary variance is zero. Historical
isolated packaging failure and successful suffix remain distinct from its later
uninterrupted CI pass. Distribution representation differences remain separately
recorded without normalization.

Remaining: remote overloads, unsupported tuning options, introspection/lifecycle
helpers, crash logging, complete stack/tracing parity and gen/sys system protocol.
See [priorities](DEPENDENCY_PRIORITIES.md) and
[full validation](validation/part-333/final-candidate/full-guard.json).
''')

scope = json.loads((r/'docs/module-readiness-scope.json').read_bytes())
scope['ComparisonTestReport'] = 'validation/part-332/final-candidate/tests.json'
component = next(x for x in scope['Components'] if x['Id']=='proc_lib adapters')
component['Implemented'].extend([
    'Part332 completion: original6001/6001+fd8, all5962 prior contracts and39 new source/local/original/SDK chains match; strict ordinary variance0; all five terminal typed scopes pass.',
    'Part333: seven local monitored/option APIs; original monitor retention, real DOWN tail requeue, ordered monitor precheck and existing atomic option parser. Actual6871/6871 and eleven integrations/packages;19/39 API presence. Original333 pending.'
])
component['Remaining'] = ['Remote overloads and unsupported native tuning options','gen/sys integration and required introspection/lifecycle helpers','Crash logger, complete stack/tracing/scheduler/physical parity']
write('docs/module-readiness-scope.json', json.dumps(scope, indent=2)+'\n')
(r/'docs/supported-mfas.json').write_bytes((d/'supported-mfas.json').read_bytes())
(r/'artifacts/tests.json').write_bytes((d/'tests.json').read_bytes())
priorities = r/'docs/DEPENDENCY_PRIORITIES.md'
write('docs/DEPENDENCY_PRIORITIES.md', priorities.read_text(encoding='utf-8')+'\nPart333 checkpoint:19/39 proc_lib APIs, including the local monitored and option-bearing startup family. Actual6871/11 local validation; original333 pending. Priority now moves to minimal gen/sys protocols with required proc_lib introspection/lifecycle support. Native tuning options and remote overloads remain explicit gaps.\n')

entry = (r/'artifacts/part333-changelog-draft.md').read_text(encoding='utf-8')
entry = entry.replace('**Validation:** fill only from actual source-reviewed candidate, full integration, readiness and publication results before committing this entry.', '**Validation:** source-reviewed actual candidate passes6871/6871 and eleven integrations/packages in one uninterrupted full run. All6822 previous ordered tests and all521 registry rows/custom Limits remain exact; seven new registrations give528 total/proc_lib19/39. Focused41 compiled scenarios, seven direct contracts and three real HOST controls pass. Two test-source syntax failures were preserved and corrected without production changes or assertion weakening. Source closure2439 and six prebound package resources are verified;18 owned paths are promoted exactly without repeating the local full solely for directory changes. Native plans1130/1155 preserve all nonFiles/native ABI policies. Current-head CI and original333 remain pending publication. [Actual full guard](../validation/part-333/final-candidate/full-guard.json).')
entry += '\n\n**Next:** audit exact-head original/CI observations, then continue gen/sys and required proc_lib introspection/lifecycle contracts. Full module, unsupported tuning, remote and performance compatibility remain incomplete.\n'
log = r/'docs/changelog/0052.md'
combined = log.read_text(encoding='utf-8')+'\n\n'+entry
assert len(combined.encode()) < 32768 and len(combined.splitlines()) < 400
write('docs/changelog/0052.md', combined)
index = (r/'CHANGELOG.md').read_text(encoding='utf-8')
assert index.count('— part332 (active)') == 1
write('CHANGELOG.md', index.replace('— part332 (active)','— parts332–333 (active)'))
print(json.dumps({'Passed':6871,'Registry':528,'ProcLib':19,'Changelog':'0052.md','BaselineTests':6822}))
