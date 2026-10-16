from pathlib import Path
import json
import hashlib

r = Path.cwd()
d = r / 'artifacts/proc-lib332-full-source'
h = lambda b: hashlib.sha256(b).hexdigest().upper()
guard = json.loads((d / 'full-guard.json').read_bytes())
assert h((d / 'full-guard.json').read_bytes()) == 'FBB9A95750DD49DA34C8FF76B4CE10D2D1EC640C98A32555D9D4667BFDC899E7'
assert json.loads((r / 'artifacts/root-part332-promotion.json').read_bytes())['SourceClosurePaths'] == 2431
assert guard['Passed'] == 6822 and guard['Registry'] == 521

def write(path, text):
    (r / path).write_bytes(text.encode('utf-8'))

def prepend(path, text):
    p = r / path
    original = p.read_text(encoding='utf-8-sig')
    heading, body = original.split('\n', 1)
    write(path, heading + '\n\n' + text.strip() + '\n\n' + body.lstrip('\n'))

checkpoint = ('Part332 adds six local proc_lib startup/failure APIs:521 registered MFAs across8 modules, proc_lib12/39 and erlang351 unchanged. '
              'Actual6822/6822 tests retain all6775 previous ordered names; all515 previous registry rows and custom Limits remain exact. '
              'Eleven integration/package outcomes pass cumulatively: the initial isolated pipeline failed at first pack because README.md was absent; '
              'the exact remaining package suffix passed after copying six unchanged tracked package resources. Tests were not rerun. '
              'The tested2431 source/resource bytes were promoted exactly; this is not an uninterrupted local full-script pass. Original332 and current-head CI remain pending publication. '
              'Published331 now passes all five terminal typed scopes: original5962/fd8 with zero ordinary strict variance and all5941 prior contracts retained, CI6775/11 and separate native/reallocation/distribution scopes. '
              'The first six proc_lib APIs therefore have fresh original observations; the six new332 APIs remain pending. Bounded329 recursion/cancellation completion remains established by the published331 prior329/prior330 proofs. Next: monitored/option-bearing proc_lib startup and minimal gen/sys support.')
prepend('AGENTS.md', checkpoint)
prepend('docs/PROGRESS.md', checkpoint)
prepend('docs/NEXT_STEPS.md', 'Part332 next execution: publish the exact tested startup/receive slice, audit current-head CI and original332 observations, then implement proc_lib start_monitor and option-bearing startup before gen/sys system messages, state inspection and suspend/resume. Preserve arbitrary acknowledgement returns, real monitor ownership, failure ordering and selective mailbox contents. Follow DEPENDENCY_PRIORITIES.md; no whole-module or performance completion claim.')
prepend('docs/COMPATIBILITY.md', checkpoint + '\n\nThe shared Erlang receive route evaluates the timeout expression before selection and validates its value at a genuine no-match wait boundary. The host TimeSpan route remains unchanged. Existing SDK zero-timeout snapshot scheduling is retained; exact VM scheduling under continuous arrivals and complete exception stack parity remain unverified.')
prepend('docs/ERLANG_COMPATIBILITY.md', 'Part332 changes no erlang registrations (351 present). It repairs shared source receive timeout ordering needed by proc_lib synchronous startup. Bounded source/host tests pass; original332 comparison remains pending. All erlang exports being registered does not establish whole-module, scheduler, resource-layout or performance compatibility.')
prepend('docs/semantic-differences.md', 'Part332: matching queued messages precede invalid timeout VALUE rejection; the timeout EXPRESSION still executes once before selection, including its side effects/errors. Startup uses actual ack/nack/DOWN and removes a link before timeout kill. Existing zero-timeout snapshot policy is retained without claiming exact VM concurrent-arrival scheduling. Crash logging, complete stack parity and gen/sys integration remain incomplete; original332 is pending.')
prepend('docs/DECISIONS.md', 'Part332 — Reuse the existing CLR process/monitor/mailbox and selective receive loop. Public proc_lib acknowledgements return arbitrary terms; do not reuse gen_server return classification. Defer Erlang timeout value conversion within the existing mailbox wait boundary, preserving cancellation and typed host TimeSpan calls. No external framework or production BEAM dependency. The isolated packaging omission was repaired by executing only the uncompleted exact package suffix; retain the original failure and distinguish cumulative validation from a single full-script success.')
prepend('docs/dependency-decisions.md', 'Part332 — Local proc_lib startup and receive ordering. Reviewed pinned OTP29.1.1 proc_lib.erl and VM receive instructions. Reuse the project\'s existing C# ProcessRuntime, monitor/link machinery, immutable terms, Mailbox and BCL timing/cancellation; their source and licensing decisions remain unchanged. A new actor framework would require adapting its mailbox and failure semantics and is unnecessary for this slice. No new third-party component or license header is introduced. Source review and bounded tests do not establish native scheduling, full module compatibility or performance.')

write('docs/PROC_LIB_COMPATIBILITY.md', '''# proc_lib local process foundation

Reference: [OTP29.1.1 proc_lib.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/proc_lib.erl).

Part332 registers12 of39 declarations. Status: **Partially compatible**.
There are521 registered APIs across eight modules; erlang remains351.
API presence does not establish complete semantic compatibility.

| APIs | Implemented local contract |
|---|---|
| spawn/1,3; spawn_link/1,3 | Existing atomic process creation, creator ancestry, caller/child guard boundaries and initial-call metadata |
| init_ack/1,2 | Parent from ancestry or explicit destination; arbitrary acknowledgement return term |
| start/3,4; start_link/3,4 | Real monitor, optional atomic link, arbitrary ACK return, real DOWN after NACK, unlink-before-timeout kill and selective EXIT cleanup |
| init_fail/2,3 | Send NACK before error/exit/throw or explicit-stack raise; preserve parent lookup and malformed-exception failure boundaries |

Creator ancestry uses its registered name or PID and preserves list-shaped tails,
including improper tails. Native process_info(initial_call) describes the
proc_lib:init_p wrapper; '$initial_call' describes the user call or source-defined
gen behaviour translation. These identities remain separate.

Shared receive now evaluates a timeout expression once before selection but
converts its value only when a genuine no-match wait is required. Matching queued
ACK/NACK/DOWN therefore precede invalid timeout-value rejection. No dummy receive
or false timeout event is added. Exact integer zero retains the SDK snapshot
policy; identical VM scheduling under continuous concurrent arrivals is unclaimed.

Actual tests:6822/6822, retaining all6775 previous ordered names. This slice adds
31 compiled startup scenarios, eight compiled receive scenarios, six direct API
contracts and two HOST groups containing three real queued startup controls and
ten interpreted controls. Prior21 proc_lib scenarios and nine initial-call
translation controls remain. All515 previous registry rows/custom Limits are exact.

All eleven integration/package outcomes pass cumulatively. The first isolated
tools/validate.ps1 stopped at first pack with NU5019 because README.md was absent.
Six unchanged tracked package resources were supplied and the exact uncompleted
package suffix passed; tests were not rerun. The initial failure is retained.
All2431 tested source/resource bytes match their promoted Main counterparts.
Original332 and current-head CI remain pending publication.

Published331 original observations now pass5962/5962 plus descriptor8, including
all21 first-slice proc_lib scenarios and all5941 previous contracts. Ordinary
strict raw variance is zero. Its other four terminal typed scopes also pass;
the distribution strict text-representation refusal remains separate from its
six successful semantic groups and is retained without normalization.

Not implemented: start_monitor, option-bearing and remote overloads, crash logger
integration, complete stack parity and gen/sys system messages. No performance
improvement or whole-module completion is inferred. See [priorities](DEPENDENCY_PRIORITIES.md)
and [actual cumulative validation](validation/part-332/final-candidate/full-guard.json).
''')

priorities = r / 'docs/DEPENDENCY_PRIORITIES.md'
write('docs/DEPENDENCY_PRIORITIES.md', priorities.read_text(encoding='utf-8') + '\nPart332 checkpoint:12 local proc_lib APIs are present after adding synchronous start/start_link and init_fail. Actual6822 tests and eleven cumulative integration/package outcomes pass; original332 remains pending. Continue monitored/option-bearing startup, then gen/sys. Presence is not complete compatibility.\n')

scope = json.loads((r / 'docs/module-readiness-scope.json').read_bytes())
scope['ComparisonTestReport'] = 'validation/part-331/final-main/tests.json'
component = next(x for x in scope['Components'] if x['Id'] == 'proc_lib adapters')
component['Implemented'].append('Part331 completion: all21 compiled local spawn/spawn_link/init_ack scenarios match pinned original OTP; complete5962/5962+fd8, all5941 prior contracts retained, zero strict ordinary variance. CI6775/11 and all other terminal typed scopes pass.')
component['Implemented'].append('Part332: start/3,4,start_link/3,4,init_fail/2,3 using real ACK/NACK/DOWN, unlink-before-timeout kill and shared deferred timeout value validation. Actual6822/6822 and eleven cumulative integration/package outcomes; promoted2431 source bytes exact. Original332 pending.')
component['Remaining'] = ['start_monitor, option-bearing and remote overloads', 'gen/sys integration, crash logger, complete error-stack parity and proc_lib-specific original tracing observations']
write('docs/module-readiness-scope.json', json.dumps(scope, indent=2) + '\n')
(r / 'docs/supported-mfas.json').write_bytes((d / 'supported-mfas.json').read_bytes())
(r / 'artifacts/tests.json').write_bytes((d / 'tests.json').read_bytes())

draft = (r / 'artifacts/root-part332-entry-draft.md').read_text(encoding='utf-8')
draft = draft.replace('implementation under validation; fill terminal results before publication.', 'implemented and cumulatively verified locally; original332 pending publication.')
draft = draft.replace('Terminal full, promotion, readiness and original observations: TO_BE_FILLED_FROM_ACTUAL_REPORTS.', 'Actual6822/6822, preserving all6775 ordered names; registry521 with all515 prior rows/custom Limits exact, proc_lib12/39, erlang351 unchanged. All eleven outcomes pass cumulatively. Initial isolated tools/validate failed at first pack NU5019 (missing README); after six unchanged tracked package resources were supplied, the exact remaining suffix passed. No test rerun and no uninterrupted full-script success claim. [Actual guard](../validation/part-332/final-candidate/full-guard.json). The exact25 owned paths and all2431 tested source/resource bytes match after promotion. Current-head CI and original332 remain pending publication.')
assert 'TO_BE_FILLED' not in draft
prior_note = ('### Part331 post-publication completion — 2026-10-16\n\n'
              'Published6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d passes all five terminal workflows and independent typed reviews: CI6775/11; original5962/5962 plus fd8, preserving all5941 prior contracts and adding21 matching proc_lib scenarios with zero strict ordinary variance; separate native8/3/original13, reallocation3 paired and distribution semantic4/7/7/8/2/2. The separate distribution strict representation refusal is retained without normalization. The first six proc_lib APIs have original observations; the new332 slice is separate and pending. See prior331 evidence under part332.\n\n')
write('docs/changelog/0052.md', '# Detailed work changelog — volume0052\n\n' + prior_note + draft)
index = (r / 'CHANGELOG.md').read_text(encoding='utf-8')
old = '- [0051.md](docs/changelog/0051.md) — parts328–331 (active)'
assert index.count(old) == 1
write('CHANGELOG.md', index.replace(old, '- [0051.md](docs/changelog/0051.md) — parts328–331 (closed)\n- [0052.md](docs/changelog/0052.md) — part332 (active)'))
print(json.dumps({'Registry': 521, 'Passed': 6822, 'Comparison': scope['ComparisonTestReport'], 'Changelog': '0052.md'}))
