from pathlib import Path
import hashlib
import json
import shutil
import subprocess

r = Path.cwd()
d = r / 'artifacts/proc-lib334-full-source'
c = r / 'artifacts/proc-lib334-candidate'
j = lambda path: json.loads(path.read_bytes())
h = lambda value: hashlib.sha256(value).hexdigest().upper()
f = j(d / 'source-freeze.json')
g = j(d / 'full-guard.json')
assert subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip() == f['ParentHead'] == 'e9725c4b2eef68014aeb70afd0abcf9f646065fd'
assert (g['Passed'], g['Failed'], g['Registry'], g['ProcLibMfas']) == (6918, 0, 532, 23)
assert g['PreviousOrderedNamesUnchanged'] and g['PreviousOrderedNames'] == 6871
expected = {'Readiness':'Passed','PackagedReadiness':'Passed','Build':'Passed','Tests':'Passed','OracleUnavailableReport':'Expected failure recorded','HybridOutput':'Hello World','Incremental':'Passed','DisabledPreprocessing':'Expected failure','CleanRebuild':'Passed','PackageConsumer':'Passed','LocalTool':'Passed'}
assert g['IntegrationResults'] == expected
exit_result = j(d / 'full-exit.json')
assert exit_result['ExitCode'] == 0 and exit_result['Settled'] and not exit_result['TimedOut']
assert g['SourceFreezeSha256'] == h((d / 'source-freeze.json').read_bytes())
assert g['SourceClosureSha256'] == h((d / 'source-resource-closure.json').read_bytes())
owned = {row['Path']: row for row in f['Files']}
assert len(owned) == f['OwnedPaths'] == 15 and f['SourceClosurePaths'] == 2446
closure = j(d / 'source-resource-closure.json')['Files']
for row in closure:
    value = (c / row['Path']).read_bytes()
    assert len(value) == row['Bytes'] and h(value) == row['Sha256'], row['Path']
    if row['Path'] not in owned:
        assert (r / row['Path']).read_bytes() == value, row['Path']
for row in j(d / 'package-resources.json')['Files']:
    value = (r / row['Path']).read_bytes()
    assert h(value) == row['Sha256'] and len(value) == row['Bytes'], row['Path']
old_registry = j(r / 'docs/supported-mfas.json')
new_registry = j(d / 'supported-mfas.json')
key = lambda row: (row['Module'], row['Function'], row['Arity'])
new_rows = {key(row): row for row in new_registry}
assert len(old_registry) == 528 and len(new_registry) == len(new_rows) == 532
assert all(new_rows[key(row)] == row for row in old_registry)
assert set(new_rows) - set(map(key, old_registry)) == {('proc_lib', name, 1) for name in ['initial_call', 'translate_initial_call', 'set_label', 'get_label']}
old_tests = j(r / 'docs/validation/part-333/final-candidate/tests.json')['Tests']
new_tests = j(d / 'tests.json')['Tests']
old_names = [row['Name'] for row in old_tests]
old_set = set(old_names)
assert [row['Name'] for row in new_tests if row['Name'] in old_set] == old_names
for path, row in owned.items():
    source = c / path
    assert h(source.read_bytes()) == row['Sha256']
    target = r / path
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, target)
for row in closure:
    assert h((r / row['Path']).read_bytes()) == row['Sha256'], row['Path']

def write(path, text):
    (r / path).write_bytes(text.encode('utf-8'))

def prepend(path, text):
    head, body = (r / path).read_text(encoding='utf-8-sig').split('\n', 1)
    write(path, head + '\n\n' + text + '\n\n' + body.lstrip('\n'))

summary = ('Part334 implements proc_lib initial_call/1, translate_initial_call/1, set_label/1 and get_label/1. '
    'Actual6918/6918 and eleven integration/package checks pass in one complete isolated pipeline; all6871 prior ordered tests and528 registry rows/custom Limits are preserved. '
    'Registry532 across eight modules; proc_lib23/39, erlang351 unchanged. Forty-three new compiled scenarios and four direct API checks pass. '
    'Published333 is fully audited: CI6871/11, original6042/6042 plusfd8, all6001 previous and41 new contracts match semantically; all five typed terminal scopes pass. '
    'One predeclared map stdout-order variance and the separate distribution strict representation refusal remain raw and explicit. '
    'Original334 and current-head CI await publication. API presence is not semantic completion. Remaining16 proc_lib exports cover lifecycle, node overloads and report formatting, with gen/sys/logger and connected-node dependencies still incomplete.')
for path in ['AGENTS.md', 'docs/PROGRESS.md', 'docs/COMPATIBILITY.md']:
    prepend(path, summary)
prepend('docs/NEXT_STEPS.md', 'Part334: publish and compare the43 new introspection/label scenarios against pinned OTP. Continue all remaining proc_lib exports: real sys termination and DOWN budget, init_p/wake_up/hibernate plus crash reports, node overloads and faithful report formatting. Keep unverified drafts isolated; never count absent backends or disconnected-only spawning as full compatibility. LICENSE.local.txt and unrelated caches remain uncommitted.')
prepend('docs/ERLANG_COMPATIBILITY.md', 'Part334 leaves351 erlang registrations unchanged. The four new proc_lib exports reuse actual process_info, dictionary, integer/atom conversion and process identifiers. No new erlang semantics, physical metrics or performance result is claimed. Published333 original6042/fd8 and CI6871/11 are complete, with strict textual refusals separate from semantic success.')
prepend('docs/semantic-differences.md', 'Part334: proc_lib initial-call queries preserve specific dictionary-item precedence over dictionary-list fallback; initial_call makes Argument__1..N atoms and translate_initial_call retains the native default. Labels are arbitrary real dictionary values, return ok, do not inherit into children, and unavailable/invalid targets return undefined through the source exception boundary. Connected remote RPC and nonlocal numeric PID-channel parsing remain incomplete; lifecycle/crash reports, report formatting and unrestricted native tuning are not completed by these four APIs.')
prepend('docs/DECISIONS.md', 'Part334 — Reuse existing process_info dictionary queries, lists:keyfind, process dictionary put/get, PID text parsing and atom admission. Dummy arguments are built cooperatively with existing reductions/cancellation. Keep wrong-arity/malformed query behavior and the source precedence rather than flattening all query forms. No actor framework, production BEAM dependency, package or synthetic process state is added.')
prepend('docs/dependency-decisions.md', 'Part334 — Pinned OTP29.1.1 proc_lib.erl and c.erl guide raw_initial_call, initial_call, translate_initial_call and process labels. Existing C# runtime/BCL facilities are reused; separate actor or logging libraries would not supply these source contracts. Existing OTP notices and the repository license decision remain unchanged; no license headers or new third-party dependency are introduced.')
prepend('docs/PROC_LIB_COMPATIBILITY.md', summary + '\n\nNew APIs: initial_call/1 and translate_initial_call/1 query live PIDs, numeric PID tuples and source-shaped process-info lists. set_label/1 stores any term in the actual process dictionary; get_label/1 reads self or another process without inventing label inheritance. Remote RPC and nonlocal numeric PID channels remain explicitly incomplete. Local labels/introspection are validated by43 compiled scenarios plus4 direct contracts; original334 is pending.')
scope = j(r / 'docs/module-readiness-scope.json')
scope['ComparisonTestReport'] = 'validation/part-333/final-candidate/tests.json'
component = next(row for row in scope['Components'] if row['Id'] == 'proc_lib adapters')
component['Implemented'].extend([
    'Part333 completion: original6042/6042+fd8 and CI6871/11; all6001 previous and41 new contracts match. Allfive typed scopes pass; one predeclared ordinary map stdout-order variance and distribution strict repr refusal remain separately retained without normalization.',
    'Part334: initial_call/1, translate_initial_call/1 and arbitrary process labels set_label/1,get_label/1; actual6918/6918 plus11 integrations,43 compiled scenarios and4 direct contracts. Registry532/proc_lib23/39. Original334 pending.'
])
component['Remaining'] = ['Sixteen lifecycle, node-overload and report-formatting exports; gen/sys/logger integration', 'Connected remote RPC and numeric PID channels; unrestricted native tuning options', 'Complete stack/tracing/scheduler/physical and performance parity']
write('docs/module-readiness-scope.json', json.dumps(scope, indent=2) + '\n')
shutil.copyfile(d / 'supported-mfas.json', r / 'docs/supported-mfas.json')
shutil.copyfile(d / 'tests.json', r / 'artifacts/tests.json')
priorities = 'docs/DEPENDENCY_PRIORITIES.md'
write(priorities, (r / priorities).read_text(encoding='utf-8') + '\nPart334:23/39 proc_lib APIs and real process labels; actual6918/11. Finish cooperative sys stop and lifecycle/crash reporting, node overloads and exact report formatting. Source-only drafts are unregistered; full proc_lib remains incomplete.\n')

base = r / 'docs/validation/part-334'
for name in ['source-freeze.json', 'source-resource-closure.json', 'package-resources.json', 'readiness-inputs.json', 'registry-before.json', 'tests.json', 'supported-mfas.json', 'raw-supported-mfas.json', 'integration-results.json', 'full-guard.json', 'full-attempt.json', 'full-exit.json', 'full-validation.log']:
    source = d / name
    if source.exists():
        target = base / 'final-candidate' / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
for path in owned:
    target = base / 'source' / path
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(c / path, target)
for directory in ['proc-lib334-source-independent', 'proc-lib334-full-source-independent']:
    for source in (r / 'artifacts' / directory).glob('*.json'):
        target = base / directory / source.name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)

entry = '''
## Part333 completion — 2026-10-16 — Pinned original comparison

**Part status:** completed within the published slice. Commit e9725c4b2eef68014aeb70afd0abcf9f646065fd passes all five exact-head terminal workflows and independent typed audits. CI passes6871 tests and11 integrations. Original OTP29.1.1 passes6042/6042 plus8 descriptor checks; all6001 prior contracts and41 new monitored/option startup scenarios match source/local/original/SDK chains.

One previously declared map stdout-order variance retains its strict raw-equality refusal; semantic comparison passes separately. Distribution retains its fresh strict representation refusal independently of six successful typed groups. No raw normalization, physical equality or separate original13/SDK differential claim is made. Source/archive/API digests and failed reader precondition are retained. [Summary](../validation/part-334/prior333/all-five-terminal-independent-summary.json).

## Part334 — 2026-10-16 — Initial-call introspection and process labels

**Part status:** completed local implementation; fresh original comparison pending publication.
**Objective and scope:** implement initial_call/1, translate_initial_call/1, set_label/1 and get_label/1 using actual process state and source-shaped query behavior.

### Changes

- Add separate ProcLibIntrospection, names and cached-atom files; register four exports. Specific dictionary-item queries take precedence over dictionary-list fallback. Initial-call dummy arguments retain original naming/order and atom admission.
- Store arbitrary process labels in the real process dictionary; self/other/dead/invalid queries preserve the source return and exception boundaries. Children do not inherit labels.
- Add43 permanent compiled scenarios and4 direct API contracts covering live/dead/self targets, numeric PID round trips, malformed/duplicate query precedence, dummy arities, arbitrary labels and dictionary mutation.

### Decisions and compatibility

Pinned OTP29.1.1 proc_lib.erl/c.erl guide the C# adaptation. Existing process_info, dictionary, lists, PID text and BCL/runtime facilities are reused. No package/framework/production BEAM dependency or license header is introduced; existing notices remain. Connected remote RPC, nonlocal numeric PID channels and all16 remaining lifecycle/node/report exports remain incomplete. Unrestricted native tuning, physical resources and performance equality are not claimed.

### Validation

- SDK focused target: all43 sources parse before execution,43 compiled scenarios and4 direct API tests pass on the first attempt; source/style checks pass with zero build warnings/errors.
- `pwsh -NoLogo -NoProfile -File tools/validate.ps1` in the isolated candidate:6918/6918 tests and11 integration/package outcomes pass in one settled exit0 pipeline. All6871 prior ordered tests and528 registry rows/custom Limits remain exact. Source2446 and six prebound package resources match the promoted15 paths. [Full guard](../validation/part-334/final-candidate/full-guard.json).
- Native plan inputs1133/1158 and workflow seals are source-checked. No redundant new native execution or local full rerun after byte-exact promotion is claimed. Fresh334 CI/original outcomes remain unrun before publication; published333 evidence is historical and independently archived.
- Readiness is regenerated with the current report against immutable333:532 registered APIs, previous528/delta4, proc_lib23/39; erlang351 and eight implemented API modules remain unchanged. Presence is not full compatibility.

### Handoff

Publish this validated slice and compare its43 new cases with original OTP. Continue all remaining proc_lib lifecycle, cooperative sys termination, connected-node and faithful report-formatting work. Source-only drafts stay isolated and uncounted. Current progress/compatibility/decisions/readiness/next tasks are updated; owner-local license and unrelated caches remain uncommitted.
'''
log_path = r / 'docs/changelog/0052.md'
combined = log_path.read_text(encoding='utf-8') + '\n' + entry
assert len(combined.encode()) < 32768 and len(combined.splitlines()) < 400
assert '## Part334 ' not in log_path.read_text(encoding='utf-8')
write('docs/changelog/0052.md', combined)
index = (r / 'CHANGELOG.md').read_text(encoding='utf-8')
assert 'parts332–333 (active)' in index
write('CHANGELOG.md', index.replace('parts332–333 (active)', 'parts332–334 (active)'))
receipt = {'Status':'SOURCE_EXACT334_PROMOTED_AFTER_COMPLETE_FULL', 'Parent':f['ParentHead'], 'Passed':6918, 'Registry':532, 'ProcLib':23, 'OwnedPaths':15, 'SourceClosurePaths':2446, 'PackageResources':6, 'FullGuardSha256':h((d/'full-guard.json').read_bytes()), 'Original334Executed':False}
(r / 'artifacts/root-part334-promotion.json').write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
print(json.dumps(receipt))
