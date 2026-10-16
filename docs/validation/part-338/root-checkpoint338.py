from pathlib import Path
import argparse
import hashlib
import json
import shutil
import subprocess

p = argparse.ArgumentParser()
p.add_argument('--full-dir', required=True)
p.add_argument('--guard-sha256', required=True)
args = p.parse_args()
r = Path.cwd()
a = r / 'artifacts'
c = a / 'proc-lib338-candidate'
d = r / args.full_dir
b = r / 'docs/validation/part-338'
j = lambda path: json.loads(path.read_bytes())
h = lambda raw: hashlib.sha256(raw).hexdigest().upper()
head = 'add206c03c801cbd68c51d23dbbdb8e36f2d581f'
assert subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip() == head
assert not b.exists()
assert h((d / 'completion-guard.json').read_bytes()) == args.guard_sha256.upper()
g = j(d / 'completion-guard.json')
f = j(d / 'source-freeze.json')
tests = j(d / 'tests.json')
oldtests = j(r / 'docs/validation/part-337/final-candidate/tests.json')
assert f['ParentSha'] == head and g['SourceFreezeSha256'] == h((d / 'source-freeze.json').read_bytes())
assert tests['Passed'] >= 7105 and tests['Failed'] == 0
assert g['Passed'] == tests['Passed'] and g['Failed'] == 0 and g['Registry'] == 576 and g['ProcLibMfas'] == 33
names = [x['Name'] for x in tests['Tests']]
oldnames = [x['Name'] for x in oldtests['Tests']]
assert len(names) == len(set(names)) == tests['Passed'] and len(oldnames) == 7017
assert [n for n in names if n in set(oldnames)] == oldnames
assert all(x['Status'] == 'Passed' for x in tests['Tests'])
exit = j(d / 'full-exit.json')
assert exit['ExitCode'] == 1 and exit['Settled'] and not exit['TimedOut']
assert g['SingleUninterruptedFullPass'] is False and g['InitialExit'] == 1 and g['ContinuationExit'] == 0
integrations = j(d / 'integration-results.json')
assert len(integrations) == 11
owned = {x['Path']: x for x in f['Files']}
assert len(owned) == len(f['Files'])
closure = j(d / 'source-resource-closure.json')['Files']
line_endings = j(a / 'root-338-unowned-line-endings2.json')
assert line_endings['Parent'] == head and line_endings['MainEdited'] is False
ending_rows = {x['Path']: x for x in line_endings['DifferingPaths']}
assert len(ending_rows) == 105 and not any(p.endswith('.cs') for p in ending_rows)
for manifest in ['source-resource-closure.json', 'package-resources.json']:
    for row in j(d / manifest)['Files']:
        raw = (c / row['Path']).read_bytes()
        assert len(raw) == row['Bytes'] and h(raw) == row['Sha256'], row['Path']
        if row['Path'] not in owned:
            current = (r / row['Path']).read_bytes()
            if current != raw:
                ending = ending_rows[row['Path']]
                assert h(current) == ending['WorkingTreeSha256'] and h(raw) == ending['TestedSha256']
                assert current.replace(b'\r\n', b'\n') == raw.replace(b'\r\n', b'\n')
            else:
                assert row['Path'] not in ending_rows
assert len(j(d / 'package-resources.json')['Files']) == 6
old = j(r / 'docs/supported-mfas.json')
new = j(d / 'supported-mfas.json')
key = lambda x: (x['Module'], x['Function'], x['Arity'])
newrows = {key(x): x for x in new}
assert len(old) == 540 and len(newrows) == len(new) == 576
assert all(newrows[key(x)] == x for x in old)
delta = set(newrows) - set(map(key, old))
assert len(delta) == 36
assert {x for x in delta if x[0] == 'proc_lib'} == {('proc_lib', 'init_p', 3), ('proc_lib', 'init_p', 5), ('proc_lib', 'hibernate', 3), ('proc_lib', 'wake_up', 3)}
assert {m: sum(x[0] == m for x in delta) for m in ['logger', 'application', 'error_logger']} == dict(logger=26, application=4, error_logger=2)
ci = j(a / 'part337-remote-new-review/ci/independent-ci-guard.json')
assert ci['Passed'] == 7017 and ci['Head'] == head and ci['PublishedBlobsRehashed'] == 1783
original = j(a / 'part337-remote-new-review/ordinary-semantic-independent-guard.json')
assert original['Head'] == head and original['Passed'] == 6171 and original['Failed'] == 0 and original['FdPassed'] == 8
assert original['AllPrevious6114OrderedContractsVerified'] and original['NewOriginalCases'] == 57
for path, row in owned.items():
    raw = (c / path).read_bytes()
    assert len(raw) == row['Bytes'] and h(raw) == row['Sha256'], path
    (r / path).parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(c / path, r / path)
for row in closure:
    current = (r / row['Path']).read_bytes()
    if row['Path'] in ending_rows:
        assert current.replace(b'\r\n', b'\n') == (c / row['Path']).read_bytes().replace(b'\r\n', b'\n')
    else:
        assert h(current) == row['Sha256'], row['Path']

def write(path, text):
    (r / path).write_text(text, encoding='utf-8', newline='\n')

def prepend(path, text):
    first, rest = (r / path).read_text(encoding='utf-8').split('\n', 1)
    write(path, first + '\n\n' + text + '\n\n' + rest.lstrip('\n'))

summary = (f'Part338 implements proc_lib init_p/3,5, hibernate/3 and wake_up/3, shared initialization and real crash logger dependencies. '
           f'Actual frozen-source {tests["Passed"]}/{tests["Passed"]} tests; a separate exact readiness/integration suffix completes11 outcomes after restoring one omitted immutable comparison report. The initial exit1 is preserved; no uninterrupted full-pass claim. All7017 previous ordered tests and540 registry rows remain exact. '
           'Registry576 across12 modules; proc_lib33/39. Add logger26, application4 and error_logger2 source-guided exports with real per-runtime configuration, guest callback delivery, standard streams and iterative term limiting. '
           'Local crash reports preserve data and field order but expose heap_size/stack_size as undefined because public CLR process_info deliberately lacks BEAM physical metrics. No physical values are fabricated; the two explicit platform alternatives are recorded. '
           'Published337 CI7017/11 and separate original6171/fd8 are independently complete, including all prior6114 ordered contracts and57 new formatter cases. The known map stdout-order variance remains a separate strict refusal. '
           'Six node proc_lib overloads, remote RPC label queries, full logger background/overload/configuration/formatting, physical scheduling/resources and performance parity remain unfinished. Current338 original verification awaits publication. API presence is not semantic completion.')
for path in ['AGENTS.md', 'docs/PROGRESS.md', 'docs/COMPATIBILITY.md', 'docs/PROC_LIB_COMPATIBILITY.md', 'docs/ERLANG_COMPATIBILITY.md', 'docs/semantic-differences.md']:
    prepend(path, summary)
prepend('docs/DECISIONS.md', 'Part338 — Port real proc_lib initialization and crash orchestration over existing runtime processes. Add actual logger/application/error_logger dependencies rather than test-only handlers. Preserve unavailable physical fields explicitly as undefined in the private crash-report bridge; public process_info support and other report assertions stay unchanged. Iterative source prechecking retains immutable object identity without an Equals shortcut.')
prepend('docs/dependency-decisions.md', 'Part338 — Reuse process monitors, dictionaries, mailbox queries, runtime standard streams and immutable term/map iterators. Port only the pinned logger/environment/limiter dependency paths required by real crash reports; introduce no new package or actor framework. Physical per-process BEAM allocation is unavailable in CLR and is never simulated with guessed counts. Keep existing third-party notices and the owner-local license proposal uncommitted.')
prepend('docs/NEXT_STEPS.md', 'Part338: publish the passed initializer/crash-report slice and dispatch the enlarged original comparison once. Complete six node-qualified proc_lib overloads with actual connected spawning, TT token and monitor/link/alias semantics; then complete remote RPC process labels. Preserve raw failed fixtures and the explicit two-field physical-report divergence. Full logger and whole-module compatibility remain partial.')
prepend('docs/DEPENDENCY_PRIORITIES.md', 'Part338 — proc_lib33/39: prioritize six real node spawning overloads and their connected spawn/TT/monitor/link/alias dependencies, followed by actual erpc/rpc label queries. Broader logger overload handling and physical/runtime equivalence remain separate measured work.')
scope = j(r / 'docs/module-readiness-scope.json')
scope['ComparisonTestReport'] = 'validation/part-337/final-candidate/tests.json'
component = next(x for x in scope['Components'] if x['Id'] == 'proc_lib adapters')
component['Implemented'].append(summary)
component['Remaining'] = ['Six node spawning overloads and real remote RPC process-label queries', 'Full crash stack, remote-neighbour, logger background/configuration/overload/formatting parity', 'BEAM physical heap/stack/scheduler/resources and performance parity']
write('docs/module-readiness-scope.json', json.dumps(scope, indent=2) + '\n')
shutil.copyfile(d / 'supported-mfas.json', r / 'docs/supported-mfas.json')
shutil.copyfile(d / 'tests.json', a / 'tests.json')
b.mkdir(parents=True)
(b / '.gitattributes').write_bytes(b'* binary\n')
for source in d.iterdir():
    if source.is_file() and source.suffix in ['.json', '.log']:
        target = b / 'final-candidate' / source.name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
entry = (a / 'part338-changelog-draft.md').read_text(encoding='utf-8')
entry = entry.replace('draft; replace this status only after the frozen candidate passes validation.', 'completed frozen local implementation and validation; current original comparison awaits publication.')
for token, value in dict(PASSED=tests['Passed'], FAILED=0, INTEGRATIONS=11, REGISTRY=576, PROC_LIB=33).items():
    entry = entry.replace('{{' + token + '}}', str(value))
assert '{{' not in entry
index = (r / 'CHANGELOG.md').read_text(encoding='utf-8')
combined = (r / 'docs/changelog/0052.md').read_text(encoding='utf-8') + '\n' + entry
if len(combined.encode()) <= 32768 and len(combined.splitlines()) <= 400:
    volume = 'docs/changelog/0052.md'
    write(volume, combined)
    index = index.replace('parts332–337 (active)', 'parts332–338 (active)')
else:
    volume = 'docs/changelog/0053.md'
    assert not (r / volume).exists()
    write(volume, '# Detailed changelog — volume 0053\n\n' + entry)
    index = index.replace('parts332–337 (active)', 'parts332–337 (closed)')
    index += '\n- [0053.md](docs/changelog/0053.md) — part338 (active)\n'
assert (r / volume).stat().st_size <= 32768
assert len((r / volume).read_text(encoding='utf-8').splitlines()) <= 400
write('CHANGELOG.md', index)
receipt = dict(Status='EXACT338_RUNTIME_PROMOTED_PENDING_FINAL_READINESS_AND_STAGE', ParentHead=head,
               Passed=tests['Passed'], Registry=576, ProcLib=33, OwnedPaths=len(owned),
               SourceClosure=len(closure), PackageResources=6, FullGuardSha256=args.guard_sha256.upper(),
               CurrentHeadCiVerified=False, CurrentHeadOriginalVerified=False, ChangelogVolume=volume)
receipt.update(OwnedSource44PromotedRawExact=True, AllCSharpRawExact=True,
               UnownedArchiveLineEndingVariances=105, WholeClosureRawIdenticalToMain=False)
(a / 'root-part338-promotion.json').write_text(json.dumps(receipt, indent=2) + '\n')
print(json.dumps(receipt))
