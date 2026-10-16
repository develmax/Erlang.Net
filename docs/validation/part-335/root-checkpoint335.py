from pathlib import Path
import hashlib
import json
import shutil
import subprocess

r=Path.cwd();d=r/'artifacts/proc-lib335-full-source';c=r/'artifacts/proc-lib335-candidate';b=r/'docs/validation/part-335'
j=lambda p:json.loads(p.read_bytes());h=lambda value:hashlib.sha256(value).hexdigest().upper()
f=j(d/'source-freeze.json');g=j(d/'full-guard.json')
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()==f['ParentHead']=='8f27347c3137172276b3292753cc7a4595746e2f'
assert h((d/'full-guard.json').read_bytes())=='49ECBCA9645EA27C5F267542A95571A66A41171697DFB2CC99C4C19F9B40054B'
assert (g['Passed'],g['Failed'],g['Registry'],g['ProcLibMfas'])==(6952,0,536,25)
assert g['PreviousOrderedNamesUnchanged'] and g['PreviousOrderedNames']==6918 and len(g['IntegrationResults'])==11
assert j(d/'full-exit.json')['ExitCode']==0 and j(d/'full-exit.json')['Settled'] and not j(d/'full-exit.json')['TimedOut']
assert g['SourceFreezeSha256']==h((d/'source-freeze.json').read_bytes())
owned={row['Path']:row for row in f['Files']};assert len(owned)==24 and f['SourceClosurePaths']==2460
for manifest in ['source-resource-closure.json','package-resources.json']:
    for row in j(d/manifest)['Files']:
        data=(c/row['Path']).read_bytes();assert len(data)==row['Bytes'] and h(data)==row['Sha256'],row['Path']
        if row['Path'] not in owned:assert (r/row['Path']).read_bytes()==data,row['Path']
key=lambda row:(row['Module'],row['Function'],row['Arity'])
old=j(r/'docs/supported-mfas.json');new=j(d/'supported-mfas.json');rows={key(row):row for row in new}
assert len(old)==532 and len(new)==len(rows)==536 and all(rows[key(row)]==row for row in old)
assert set(rows)-set(map(key,old))=={('proc_lib','stop',1),('proc_lib','stop',3),('sys','terminate',2),('sys','terminate',3)}
old_names=[row['Name'] for row in j(r/'docs/validation/part-334/final-candidate/tests.json')['Tests']]
assert len(old_names)==6918 and [row['Name'] for row in j(d/'tests.json')['Tests'] if row['Name'] in set(old_names)]==old_names
for path,row in owned.items():
    assert h((c/path).read_bytes())==row['Sha256'];(r/path).parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(c/path,r/path)
for row in j(d/'source-resource-closure.json')['Files']:assert h((r/row['Path']).read_bytes())==row['Sha256']

def write(path,text): (r/path).write_bytes(text.encode('utf-8'))
def prepend(path,text):
    first,rest=(r/path).read_text(encoding='utf-8-sig').split('\n',1);write(path,first+'\n\n'+text+'\n\n'+rest.lstrip('\n'))

summary=('Part335 adds proc_lib stop/1,3 and sys terminate/2,3 using the real system request/reply, aliases and DOWN protocol. '
    'Actual6952/6952 tests and eleven integration/package outcomes pass in one complete isolated pipeline; all6918 previous ordered names and532 registry rows/custom Limits remain exact. '
    'Registry536 across nine modules: proc_lib25/39, sys2, erlang351 unchanged. Focused29 compiled,4 direct and1 bare-runtime HOST checks pass. '
    'Gen_server/supervisor acknowledge before termination callbacks or child shutdown; the host stop helper consumes ACK and waits for DOWN under one budget. '
    'Published334 CI is verified6918/11; original334 remains running. Published333 five typed terminal scopes are complete6042/fd8/CI6871, with strict text refusals retained separately. '
    'Original335 and current-head CI await publication. Fourteen proc_lib exports and wider gen/sys/logger, connected-node, tuning, physical and performance contracts remain incomplete.')
for path in ['AGENTS.md','docs/PROGRESS.md','docs/COMPATIBILITY.md']:prepend(path,summary)
prepend('docs/NEXT_STEPS.md','Part335: publish cooperative stop/system termination and compare29 new scenarios with pinned OTP. Finish pending original334 and current335 CI. Continue faithful report formatting, then initializer/hibernate/crash-report and node overloads. Keep unverified formatter/lifecycle drafts isolated and uncounted; no direct kill replacement or generic full-module/performance claim. LICENSE.local.txt and unrelated caches remain uncommitted.')
prepend('docs/ERLANG_COMPATIBILITY.md','Part335 keeps351 erlang registrations. Sys/proc_lib reuse existing real monitor aliases, sends and selective receives; no primitive or physical accounting implementation is changed. All6918 prior tests remain in6952 passing tests. Published334 CI6918/11 is verified; original334 and new335 comparisons remain separate pending scopes.')
prepend('docs/semantic-differences.md','Part335: proc_lib stop creates its outer monitor before sys dispatch, catches exit only and shares one monotonic timeout budget through the final real DOWN. A different exit before ACK retains the sys wrapper; after ACK its DOWN reason is returned directly as an exit. Local infinite gen calls preserve raw noconnection while finite/remote alias calls translate it to nodedown. Invalid-timeout error-class paths retain the source outer-monitor behavior. System ACK precedes terminate callback/child shutdown; aliases suppress late replies. Broader sys/gen protocol and live connected-peer equivalence remain incomplete.')
prepend('docs/DECISIONS.md','Part335 — Port proc_lib stop and required sys/gen call/reply logic directly over existing CLR process, alias, monitor and receive primitives. The host GenServer.Stop helper stays usable in a bare runtime without module-registration side effects; it consumes ACK and awaits DOWN under one Stopwatch budget. Preserve source error/catch boundaries and matching-node registry failures. Avoid a separate actor or message-bus dependency.')
prepend('docs/dependency-decisions.md','Part335 — Pinned OTP29.1.1 proc_lib.erl stop, sys.erl system messages and gen.erl call/reply/name resolution guide the adaptation. Existing project/runtime/BCL primitives supply actual monitors, aliases, sends, ownership and cancellation. No third-party framework, production BEAM dependency or new license header is added; existing notices and license decisions remain. Global registry implementation and complete system protocol are separate dependencies.')
prepend('docs/PROC_LIB_COMPATIBILITY.md',summary+'\n\nNew stop/1,3 use sys termination rather than kill, preserve exit-wrapper and timeout boundaries, and wait for the real DOWN. Source-guided before/after ACK, missing/dead/self/named/via targets, late alias replies, actual gen_server/supervisor shutdown and bare C# host use are permanently tested. Original335 remains pending.')
write('docs/SYS_COMPATIBILITY.md','''# System protocol foundation

Reference: OTP29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5 sys.erl and gen.erl.
Part335 registers terminate/2,3 only. Status: **Partially compatible**.

Requests use the real monitor/reply protocol, finite-call aliases and noconnect.
Timeout cleanup revokes aliases before checking an already queued reply.
Local infinite calls retain the source plain-monitor optimization. Name/via/global
lookup follows source dispatch where those registry modules are available;
the global registry itself is not implemented by this slice.

Gen_server/supervisor send ACK before callbacks or child shutdown. terminate may
return after ACK while the process still lives; proc_lib stop additionally waits
for its matching real DOWN using the remaining original time budget.

Actual6952 tests and11 integrations pass, including29 compiled scenarios,4 direct
API contracts and1 bare-runtime HOST regression. Original335 comparison is pending.
Suspend/resume, state/status/code change/debugging, full gen/sys protocol, connected
remote-peer behavior and physical/scheduler/performance parity remain incomplete.
''')
scope=j(r/'docs/module-readiness-scope.json');scope['ComparisonTestReport']='validation/part-334/final-candidate/tests.json'
component=next(row for row in scope['Components'] if row['Id']=='proc_lib adapters')
component['Implemented'].append('Part335: cooperative stop/1,3 plus sys terminate/2,3 and ACK-before-callback/child shutdown; actual6952/11,29 compiled+4direct+1bare HOST. Registry536/proc_lib25/39/sys2; original335 pending. Published334 CI6918/11 verified; original334 pending.')
component['Remaining']=['Fourteen initializer/hibernate, node-overload and report-formatting exports; crash logger integration','Connected remote RPC/numeric PID channels and unrestricted native tuning','Complete gen/sys, stack/tracing/scheduler/physical and performance parity']
scope['Components'].append({'Id':'sys system protocol','Project':'src/Erlang.Otp.Behaviours','Status':'Partially compatible','Implemented':['Part335 terminate/2,3; actual monitor/alias/reply protocol and ACK-before-termination; matching proc_lib stop shares original DOWN deadline.6952/11 local full; original335 pending.'],'Remaining':['Suspend/resume, status/state, code changes, debugging and complete gen/sys callbacks','Global registry and genuine connected remote-peer/physical/scheduler/performance contracts'],'TestPrefixes':['mfa/sys:','oracle/compiled-module/proc-lib-stop-','host_proc_lib_stop_bare_runtime_ack_consumed']})
if 'sys' not in scope['FocusModules']:scope['FocusModules'].append('sys')
write('docs/module-readiness-scope.json',json.dumps(scope,indent=2)+'\n')
shutil.copyfile(d/'supported-mfas.json',r/'docs/supported-mfas.json');shutil.copyfile(d/'tests.json',r/'artifacts/tests.json')
write('docs/DEPENDENCY_PRIORITIES.md',(r/'docs/DEPENDENCY_PRIORITIES.md').read_text(encoding='utf-8')+'\nPart335:proc_lib25/39 and sys terminate2/3 delivered;6952/11 local. Continue report formatting and crash logging, initializer/hibernate and node overloads. Full gen/sys and connected spawning remain dependencies; raw termination is not a substitute for their protocols.\n')

b.mkdir(parents=True,exist_ok=True);(b/'.gitattributes').write_bytes(b'* binary\n')
for source in d.iterdir():
    if source.is_file() and source.suffix in ['.json','.log']:
        target=b/'final-candidate'/source.name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
for path in owned:
    target=b/'source'/path;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(c/path,target)
for name in ['proc-lib335-actual1','proc-lib335-actual2','proc-lib335-actual3','proc-lib335-actual4','proc-lib335-actual5','proc-lib335-actual6']:
    for source in (r/'artifacts'/name).glob('*'):
        if source.is_file() and source.suffix in ['.json','.log']:
            target=b/name/source.name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
for name in ['root-part335-composition-review.json','root-review335-composition.py']:
    shutil.copyfile(r/'artifacts'/name,b/name)
ci=r/'artifacts/part334-remote-new-review/ci'
for source in ci.rglob('*'):
    if source.is_file() and source.suffix!='.zip':
        target=b/'prior334-ci'/source.relative_to(ci);target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
assert j(ci/'independent-ci-guard.json')['Passed']==6918
entry='''
## Part335 — 2026-10-16 — Cooperative proc_lib stop and system termination

**Part status:** local implementation completed; fresh original comparison pending publication.
**Objective and scope:** add proc_lib stop/1,3 and sys terminate/2,3 with real system requests, acknowledgements, aliases, DOWN and one timeout budget.

### Changes

- Add separate stop/system protocol files using existing process/monitor/send/receive primitives. Name/via/global dispatch preserves matching-node failure conversion where registry modules are available.
- Gen_server and supervisor acknowledge before termination callbacks or child shutdown. The bare-runtime C# stop helper consumes ACK and still waits for real DOWN under one total deadline; it does not initialize module registries as a side effect.
- Add29 permanent original-comparable compiled scenarios,4 direct API contracts and1 bare-runtime HOST regression. Cover timeout before/after ACK, different exit wrappers, missing/self/dead/name/via targets, late alias replies, mailbox order and real callback/child shutdown ordering.

### Decisions and compatibility

Pinned OTP29.1.1 proc_lib.erl/sys.erl/gen.erl guide the C# port. Local infinite calls preserve raw noconnection; finite/remote alias calls translate it to nodedown. A different exit before ACK retains the sys wrapper; after ACK the final DOWN reason exits directly. The invalid-timeout error-class monitor behavior is preserved. No kill shortcut, new framework/package, production BEAM dependency or license header is introduced. Complete sys/gen, global registry, live connected peers, native tuning, physical resources and performance equivalence remain incomplete.

### Validation

- Actual focused29+4+1 pass after source-directed corrections. First two build failures (local-name collision and inaccessible test-only catalog) are retained. The before-ACK fixture was corrected to the exact original sys wrapper, and a supervisor worker fixture now explicitly traps exits so the original terminate callback path is reachable; assertions were not weakened. A source review additionally corrected the local-infinity noconnection boundary and added two distinct regression cases.
- One complete isolated `pwsh -NoLogo -NoProfile -File tools/validate.ps1` run passes6952/6952 and11 integration/package outcomes, settled exit0 without timeout. All6918 previous ordered tests and532 registry rows/custom Limits remain exact. Source2460, package6 and promoted24 paths remain byte-bound. [Full guard](../validation/part-335/final-candidate/full-guard.json).
- Native plan1142/1167 source inputs and workflow seals are updated with all nonFiles policies unchanged; no redundant native rerun is claimed. Readiness is regenerated against immutable334:536 APIs across9 modules, proc_lib25/39, sys2, erlang351 unchanged. Presence is not complete compatibility.
- Historical published334 CI is independently verified6918/11 and792 committed blobs. Its original comparison remains running. Fresh335 CI/original are pending publication; no result is inferred from an earlier commit.

### Handoff

Publish this validated slice and compare29 new cases with pinned OTP. Continue all14 remaining proc_lib exports: report formatting, initializer/hibernate/crash reporting and node overloads. Unverified formatter source remains isolated and uncounted. Progress, compatibility, decisions, machine readability and next tasks are updated; the owner-local license and unrelated caches remain uncommitted.
'''
log='docs/changelog/0052.md';value=(r/log).read_text(encoding='utf-8')+'\n'+entry
assert '## Part335 ' not in (r/log).read_text(encoding='utf-8') and len(value.encode())<32768 and len(value.splitlines())<400
write(log,value);index=(r/'CHANGELOG.md').read_text(encoding='utf-8');assert 'parts332–334 (active)' in index;write('CHANGELOG.md',index.replace('parts332–334 (active)','parts332–335 (active)'))
receipt={'Status':'SOURCE_EXACT335_PROMOTED_AFTER_COMPLETE_FULL','Parent':f['ParentHead'],'Passed':6952,'Registry':536,'ProcLib':25,'Sys':2,'Modules':9,'OwnedPaths':24,'SourceClosureRows':2460,'PackageResources':6,'FullGuardSha256':h((d/'full-guard.json').read_bytes()),'Original335Executed':False}
(r/'artifacts/root-part335-promotion.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');shutil.copyfile(r/'artifacts/root-part335-promotion.json',b/'promotion.json')
print(json.dumps(receipt))
