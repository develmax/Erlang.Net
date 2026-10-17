import hashlib
import json
import pathlib
import re

root = pathlib.Path(__file__).resolve().parents[2]
out = pathlib.Path(__file__).resolve().parent
proposal = root / 'artifacts/size-bif354-permanent-test-proposal3'
view = root / 'artifacts/size-bif354-owned-style-view3'


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


freeze_raw = (proposal / 'source-freeze.json').read_bytes()
freeze = json.loads(freeze_raw)
for row in freeze['Files']:
    data = (proposal / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256'], row['Path']
inputs = json.loads((proposal / 'style-inputs.json').read_bytes())
token_raw = (root / 'artifacts/root-size354-style3-token-identity.json').read_bytes()
token = json.loads(token_raw)
rows = {row['Path']: row for row in token['Files']}
assert len(rows) == len(inputs['Files']) == 50
current = []
for row in inputs['Files']:
    source = (root / row['SourcePath']).read_bytes()
    after = (view / row['Path']).read_bytes()
    assert sha(source) == row['Sha256'] == rows[row['Path']]['BeforeSha256']
    assert sha(after) == rows[row['Path']]['AfterSha256']
    assert source == after
    current.append({'Path': row['Path'], 'Bytes': len(after), 'Sha256': sha(after)})
fixture_proof_raw = (root / 'artifacts/root-size354-styled3-fixtures.json').read_bytes()
fixture_proof = json.loads(fixture_proof_raw)
assert fixture_proof['FixtureCases'] == 23
assert {row['Path']: row for row in fixture_proof['Files']} == {row['Path']: row for row in current}
fixture_raw = (proposal / 'fixtures.json').read_bytes()
fixture = json.loads(fixture_raw)
sources = (view / 'tools/Erlang.Differential/SizeErrorCaseSources.cs').read_bytes().decode()
seen = {}
for match in re.finditer(r'public const string (\w+) = """\r?\n(.*?)(^[ \t]*)""";', sources, re.M | re.S):
    name, body, indent = match.groups()
    assert body.endswith('\n')
    body = body[:-2] if body.endswith('\r\n') else body[:-1]
    seen[name] = ''.join(line[len(indent):] if line.startswith(indent) else line for line in body.splitlines(True)).encode()
assert len(seen) == len(fixture['Cases']) == 23
for row in fixture['Cases']:
    key = ''.join(word.title() for word in row['Module'].split('_'))
    assert sha(seen[key]) == row['Sha256']

def lf(path):
    return path.read_bytes().decode().replace('\r\n', '\n')


program = lf(view / 'tests/Erlang.Tests/Program.cs')
baseline_program = lf(root / 'tests/Erlang.Tests/Program.cs')
old_filter = 'foreach (var fixture in Erlang.Differential.CompiledModuleCases.All.Where(fixture => !Erlang.Differential.SchedulerAffinityCases.Modules.Contains(fixture)))'
new_filter = old_filter[:-2] + ' && !Erlang.Differential.SizeErrorCases.Modules.Contains(fixture)))'
assert program.count(new_filter) == 1
new_tail = 'foreach (var fixture in Erlang.Differential.SizeErrorCases.Modules)'
tail_start = program.index(new_tail)
tail_end = program.index('int failed = 0;', tail_start)
restored = (program[:tail_start] + program[tail_end:]).replace(new_filter, old_filter)
assert restored == baseline_program
old_start = baseline_program.index(old_filter)
old_body_start = baseline_program.index('\n{', old_start)
old_body_end = baseline_program.index('\n}\n', old_body_start) + len('\n}\n')
new_body_start = program.index('\n{', tail_start)
new_body_end = program.index('\n}\n', new_body_start) + len('\n}\n')
assert program[new_body_start:new_body_end] == baseline_program[old_body_start:old_body_end]
assert program[new_body_end:tail_end].count('Test(') == 6
catalog = lf(view / 'tools/Erlang.Differential/CompiledModuleCases.cs')
baseline_catalog = lf(root / 'tools/Erlang.Differential/CompiledModuleCases.cs')
assert catalog.replace('.. SchedulerAffinityCases.Modules,\n        .. SizeErrorCases.Modules', '.. SchedulerAffinityCases.Modules') == baseline_catalog
load_path = 'src/Erlang.Runtime/ModuleRegistry.SourceLoadNif.cs'
load = (view / load_path).read_bytes()
assert sha(load) == 'ACB87E84B75D1A6A4D55860F4E847331D2CC30FE7FF47913FB5D6A155799467A'
assert load == (root / 'artifacts/default-load-nif354-filename-proposal/source' / load_path).read_bytes()
assert b'SourceLoadNifFilename.IsValidUtf8(arguments[0])' in load
for name in ('SourceLoadNifFilename.cs', 'SourceLoadNifFilenameParameters.cs'):
    path = 'src/Erlang.Runtime/' + name
    assert (view / path).read_bytes() == (root / 'artifacts/default-load-nif354-filename-proposal/source' / path).read_bytes()
names = lf(view / 'tests/Erlang.Tests/CommonPreloadedReinstallHostNames.cs')
checks = lf(view / 'tests/Erlang.Tests/CommonPreloadedReinstallHostChecks.cs')
assert 'EntriesProperty = "Entries"' in names and 'Item2Field = "Item2"' in names
assert 'Property(old, "Entries")' not in checks and 'GetField("Item2")' not in checks

ledger_raw = (root / 'docs/mfa-compatibility.json').read_bytes()
ledger = json.loads(ledger_raw)
guards = {row['Path']: row for row in ledger['VerifiedSourceFiles']}
assert len(guards) == 89
for row in guards.values():
    assert sha((root / row['Path']).read_bytes()) == row['Sha256']
runtime = {row['Path']: row for row in current if row['Path'].startswith('src/')}
assert len(runtime) == 32
union = []
for path in sorted(set(guards) | set(runtime)):
    data = (view / path).read_bytes() if path in runtime else (root / path).read_bytes()
    union.append({'Path': path, 'Sha256': sha(data)})
assert len(union) == 114
changed_old_guards = [path for path in sorted(guards) if path in runtime and runtime[path]['Sha256'] != guards[path]['Sha256']]
review = {
    'Status': 'PASS_FINAL_STYLED_PERMANENT3_SOURCE_AND_SIXTEEN_VERIFIED_SCOPE_PRESERVATION',
    'SourceFreezeSha256': sha(freeze_raw),
    'SourceView': str(view.relative_to(root)).replace('\\', '/'),
    'OwnedFiles': current,
    'RuntimeFiles': 32,
    'TestFiles': 18,
    'RoslynTokenProofSha256': sha(token_raw),
    'ZeroAdditionalStyledChanges': True,
    'FixtureProofSha256': sha(fixture_proof_raw),
    'FixtureManifestSha256': sha(fixture_raw),
    'FixtureBytesExact': 23,
    'InitialCompositionRefusalSha256': sha((out / 'styled-initial-report.json').read_bytes()),
    'ClosedInitialFindings': [
        'Default-only LoadNif invalid filename validation restored byte-exact reviewed proposal/actual focus6 ACB87E84; configured hook and indirect route unchanged.',
        'Early compiled loop excludes new SizeErrorCases; identical previous compile/run/stream/caller policy body appends23 after entire existing registration tail, followed by6 new HOST registrations. Removing declared two edits restores baseline Program exactly modulo stored EOL, no old assertion or name mutation.',
        'Entries and Item2 reflection strings extracted into domain Names constants.',
    ],
    'OldOrderedNamePrefixSourcePreserved': True,
    'AddedCompiledCases': 23,
    'AddedHostGroups': 6,
    'ActualFinalCountPending': True,
    'GuardUnion': {'Prior': 89, 'OwnedRuntime': 32, 'Overlap': 7, 'New': 25, 'Total': 114},
    'ChangedPriorGuardPaths': changed_old_guards,
    'VerifiedSourceFiles': union,
    'PriorLedgerSha256': sha(ledger_raw),
    'VerifiedMfasUnchanged': [{'Module': row['Module'], 'Function': row['Function'], 'Arity': row['Arity']} for row in ledger['Entries'] if row['Status'] == 'Verified compatible'],
    'SixteenScopePreservation': [
        'All16 existing verified APIs are total predicates or self/node identity on supported terms; no implementation branch, successful result or term model change.',
        'CoreModules changes only four invalid size error branches; none of the16 verified primitive handlers is edited.',
        'Execution/Evaluate origin catch handles ErlangException only; successful primitive dispatch/results and guard/matchspec classification unchanged.',
        'ModuleRegistry actual body/image selection, owned admission and lease/tracing preserved; new capture boundaries only observe failures. Reinstall correction changes current error metadata lifetime, not successful body identity.',
        'Context capture uses flow-local frame chain, selected-HOST provenance/arguments only, and atomic BacktraceDepth; ordinary successful total APIs do not enter these failure branches.',
        'ProcessRuntime/SequenceTrace changes isolated to backtrace-depth state/new flag key; existing self/node identity sources unchanged.',
        'Default filename validation affects only direct load_nif invalid input path; existing verified16 handlers and native filename backend claims unchanged.',
        'All previous89 dependency files remain guarded, seven current hashes refreshed and25 exact owned runtime dependencies conservatively added.',
    ],
    'ScopeLimits': [
        'No new Verified API or whole stack/native loader/performance compatibility claim.',
        'Full admission additionally requires actual corrected permanent focused build/new reinstall+lambda controls and final metadata/evidence/package/closure/native-plan preflight.',
        'Final candidate must copy these exact32 source bytes; any later semantic/style change requires narrow binding review.',
    ],
    'ReviewerAuthorship': 'Independent reviewer of common-source and permanent composition; authored separate defaultfilename proposal and three additional portable controls, explicitly disclosed; those source components were separately reviewed by Root/Cleanup.',
    'SdkExecutedByReviewer': False,
    'NetworkExecuted': False,
    'MainMutation': False,
    'CompatibilityPromotion': False,
}
assert len(review['VerifiedMfasUnchanged']) == 16
graph = out / 'final-styled3-source-guards.json'
graph.write_bytes((json.dumps({'Status': 'PASS_EXACT114_STYLED3_SOURCE_GUARD_GRAPH_NO_LEDGER_WRITE', 'VerifiedSourceFiles': union}, indent=2) + '\n').encode())
review['GuardGraphSha256'] = sha(graph.read_bytes())
target = out / 'final-styled3-report.json'
target.write_bytes((json.dumps(review, ensure_ascii=False, indent=2) + '\n').encode())
print(json.dumps({'Status': review['Status'], 'Sha256': sha(target.read_bytes()), 'GuardGraphSha256': review['GuardGraphSha256'], 'ChangedPriorGuards': changed_old_guards}))
