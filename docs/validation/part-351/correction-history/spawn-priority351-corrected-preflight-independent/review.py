from pathlib import Path
import ast
import collections
import hashlib
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent / 'report.json'
SEED = ROOT / 'artifacts/spawn-priority351-full-source4'
CANDIDATE = ROOT / 'artifacts/spawn-priority351-full-corrected-candidate2'
PARENT = 'add05fc61d8c6f5f024ffcdad1ea19aaea4e3725'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def read(path):
    return json.loads(path.read_bytes())

def verify(rows, root=CANDIDATE):
    assert len({row['Path'] for row in rows}) == len(rows)
    for row in rows:
        file = root / row['Path']
        assert file.stat().st_size == row['Bytes'] and sha(file) == row['Sha256'], row['Path']

source = read(SEED / 'source-freeze.json')
closure = read(SEED / 'source-resource-closure.json')
config = read(SEED / 'configuration.json')
assert sha(SEED / 'source-freeze.json') == 'C5FC54F85ED13C883C7C985EA3F88495E7E4EC6EFE515DDEB3129E23A42D9B1F'
assert sha(SEED / 'source-resource-closure.json') == 'B3897680F2AA4222E258D350D1FC163EC673E205B59D2E341D8FC0A531E36C6F'
assert sha(SEED / 'configuration.json') == 'B12ECD96F854DD20B3AC513D456ADEB42D32A9D2AD4DA878A44D6D92F0744A9B'
assert len(source['Files']) == 49 and len(closure['Files']) == 3006
verify(source['Files'])
verify(closure['Files'])
assert 'docs/supported-mfas.json' not in {row['Path'] for row in closure['Files'] + source['Files']}
assert source['ParentHead'] == config['ParentHead'] == subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip() == PARENT
assert config['ReadyForExecution'] is True
assert config['CandidateRelativePath'] == 'artifacts/spawn-priority351-full-corrected-candidate2'
assert config['SeedRelativePath'] == 'artifacts/spawn-priority351-full-source4'
assert not (ROOT / config['OutputRelativePath']).exists()
for path, key in [('source-freeze.json', 'SourceFreezeSha256'), ('source-resource-closure.json', 'SourceClosureSha256'), ('previous-tests.json', 'PreviousTestsSha256'), ('registry-before.json', 'RegistryBeforeSha256'), ('package-resources.json', 'PackageResourcesSha256'), ('readiness-inputs.json', 'ReadinessInputsSha256'), ('added-tests.json', 'AddedTestsSha256')]:
    assert sha(SEED / path) == config[key]

# The reviewed seven correction files are exact, including independent HOST changes.
for proposal in ['spawn-priority351-host-corrections-proposal1', 'spawn-priority351-port-retired-proposal']:
    directory = ROOT / 'artifacts' / proposal
    manifest = read(directory / 'source-freeze.json')
    verify(manifest['Files'])
    for row in manifest['Files']:
        assert sha(CANDIDATE / row['Path']) == sha(directory / 'source' / row['Path'])
oldsource = read(ROOT / 'artifacts/spawn-priority351-full-source2/source-freeze.json')['Files']
oldrows = {row['Path']: row for row in oldsource}
newrows = {row['Path']: row for row in source['Files']}
added_owned = set(newrows) - set(oldrows)
assert added_owned == {'src/Erlang.Runtime/RuntimePorts.cs', 'tests/Erlang.Tests/HaltLifecycleHostChecks.cs', 'tests/Erlang.Tests/HaltLifecycleHostNames.cs', 'tests/Erlang.Tests/PriorityPermissionHostChecks.cs', 'tests/Erlang.Tests/RuntimeActorDispatchHostChecks.cs', 'tests/Erlang.Tests/RuntimeActorDispatchHostParameters.cs'}
allowed_changed = {'src/Erlang.Runtime/ModuleRegistry.ProcessScheduling.cs', 'docs/mfa-compatibility.json', 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json', 'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json', '.github/workflows/nif-pending-native.yml', '.github/workflows/nif-reallocation-native.yml'}
assert {path for path in oldrows if oldrows[path]['Sha256'] != newrows[path]['Sha256']} == allowed_changed
unowned = read(SEED / 'unowned-main-raw.json')
assert unowned['ParentHead'] == PARENT and unowned['RawVariances'] == []
verify(unowned['Files'])
verify(unowned['Files'], ROOT)
package = read(SEED / 'package-resources.json')['Files']
assert len(package) == 6
verify(package)
verify(package, ROOT)
inputs = read(SEED / 'readiness-inputs.json')['Files']
assert len(inputs) == 37
verify(inputs)

ledger = read(CANDIDATE / 'docs/mfa-compatibility.json')
oldledger = read(ROOT / 'docs/mfa-compatibility.json')
assert len(ledger['Entries']) == len(oldledger['Entries']) == 587
assert dict(collections.Counter(row['Status'] for row in ledger['Entries'])) == config['CompatibilityCounts'] == {'Verified compatible': 10, 'Known differences': 24, 'Needs verification': 553}
bindings = ledger['VerifiedSourceFiles']
assert len(bindings) == len({row['Path'] for row in bindings}) == 74
for row in bindings:
    assert row['Path'].startswith('src/') and sha(CANDIDATE / row['Path']) == row['Sha256']
evidence = {}
changed_entries = []
for before, after in zip(oldledger['Entries'], ledger['Entries'], strict=True):
    key = tuple(before[k] for k in ['Module', 'Function', 'Arity'])
    assert key == tuple(after[k] for k in ['Module', 'Function', 'Arity'])
    if before != after:
        changed_entries.append(key)
        assert after['Evidence'][:len(before['Evidence'])] == before['Evidence']
        if before['Status'] == 'Verified compatible':
            assert {k:v for k,v in before.items() if k != 'Evidence'} == {k:v for k,v in after.items() if k != 'Evidence'}
        elif key[:2] == ('erlang', 'spawn_opt'):
            assert before['Status'] == after['Status'] == 'Known differences'
        else:
            assert key == ('erlang', 'statistics', 1) and before['Status'] == 'Needs verification' and after['Status'] == 'Known differences'
    for row in after['Evidence']:
        assert sha(CANDIDATE / 'docs' / row['Path']) == row['Sha256']
        if row['Path'] in evidence:
            assert evidence[row['Path']] == row['Sha256']
        evidence[row['Path']] = row['Sha256']
assert len(changed_entries) == 15 and len(evidence) == 31
scope = read(CANDIDATE / 'docs/module-readiness-scope.json')
assert scope['ComparisonTestReport'] == 'validation/part-350/full/tests.json'
base_inputs = {'reference-baseline.json', 'docs/mfa-compatibility.json', 'docs/supported-mfas.json', 'docs/module-readiness-scope.json', 'docs/otp-inventory.json', 'docs/' + scope['ComparisonTestReport']}
assert {row['Path'] for row in inputs} == base_inputs | {'docs/' + path for path in evidence}
before = read(SEED / 'registry-before.json')
original = read(ROOT / 'docs/supported-mfas.json')
assert len(before) == len(original) == 587 and sha(CANDIDATE / 'docs/supported-mfas.json') == sha(SEED / 'registry-before.json') == 'EE3B3C9540C151698CA086E738BCD7E37C83F2836C9CBA79CC08169521B50A89'
registry_changes = []
for a, b in zip(original, before, strict=True):
    assert {k:v for k,v in a.items() if k != 'Limits'} == {k:v for k,v in b.items() if k != 'Limits'}
    if a['Limits'] != b['Limits']:
        registry_changes.append(tuple(a[k] for k in ['Module', 'Function', 'Arity']))
assert set(registry_changes) == {('erlang', 'spawn_opt', n) for n in [2,3,4,5]} | {('erlang', 'process_flag', 2), ('erlang', 'process_info', 1), ('erlang', 'process_info', 2)}
previous = read(SEED / 'previous-tests.json')
assert previous['Passed'] == len(previous['Tests']) == 7286 and previous['Failed'] == 0 and all(row['Status'] == 'Passed' for row in previous['Tests'])
assert sha(SEED / 'previous-tests.json') == sha(CANDIDATE / 'docs' / scope['ComparisonTestReport'])
def constants(path):
    return {k:json.loads(v) for k,v in re.findall(r'(?:public|internal) const string (\w+)\s*=\s*("(?:[^"\\]|\\.)*")\s*;', path.read_text())}
program = (CANDIDATE / 'tests/Erlang.Tests/Program.cs').read_text()
hostnames = constants(CANDIDATE / 'tests/Erlang.Tests/SpawnPriorityTestNames.cs')
casenames = constants(CANDIDATE / 'tools/Erlang.Differential/SpawnPriorityCaseNames.cs')
catalog = (CANDIDATE / 'tools/Erlang.Differential/SpawnPriorityCases.cs').read_text()
hosts = [hostnames[key] for key in re.findall(r'Test\(\s*Erlang.Tests.SpawnPriorityTestNames\.(\w+)\s*,', program)]
compiled = ['oracle/compiled-module/' + casenames[key] for key in re.findall(r'SpawnPriorityCaseNames\.(\w+)\s*,', catalog)]
assert len(hosts) == 8 and len(compiled) == 3
assert program.index('foreach (var fixture in Erlang.Differential.CompiledModuleCases.All)') < program.index('Test(\n    Erlang.Tests.SpawnPriorityTestNames.')
added = read(SEED / 'added-tests.json')
assert added['SourceFreezeSha256'] == sha(SEED / 'source-freeze.json') and added['Names'] == compiled + hosts
assert len(set(added['Names'])) == 11 and set(added['Names']).isdisjoint(row['Name'] for row in previous['Tests'])
plans = []
for path, workflow, variable, count in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json', '.github/workflows/nif-pending-native.yml', 'REVIEWED_SOURCE_PLAN_SHA256', 1262), ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json', '.github/workflows/nif-reallocation-native.yml', 'REVIEWED_REALLOCATION_PLAN_SHA256', 1287)]:
    raw = (CANDIDATE / path).read_bytes()
    assert b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf')
    plan = json.loads(raw)
    oldplan = read(ROOT / path)
    assert {k:v for k,v in plan.items() if k != 'Files'} == {k:v for k,v in oldplan.items() if k != 'Files'}
    rows = {row['Path']:row for row in plan['Files']}
    oldrows = {row['Path']:row for row in oldplan['Files']}
    assert len(plan['Files']) == len(rows) == count and set(oldrows) <= set(rows) and len(set(rows) - set(oldrows)) == 12
    for row in plan['Files']:
        raw = (CANDIDATE / row['Path']).read_bytes()
        assert row['HashMode'] in ['TextLF', 'BinaryExact']
        if row['HashMode'] == 'TextLF':
            raw = raw.replace(b'\r\n', b'\n')
        assert len(raw) == row['Bytes'] and hashlib.sha256(raw).hexdigest().upper() == row['Sha256']
        if row['Path'] in oldrows:
            assert row['HashMode'] == oldrows[row['Path']]['HashMode']
    assert re.findall(variable.encode() + rb': ([0-9A-F]{64})', (CANDIDATE / workflow).read_bytes()) == [sha(CANDIDATE / path).encode()]
    plans.append({'Path':path, 'Rows':count, 'Sha256':sha(CANDIDATE / path)})
runner = SEED / 'run-full.py'
assert sha(runner) == '96E5E39E3EB055A4AA1D55ABC373BC8C01DEC8F4C13462AB34FB638C28DCCD74'
runner_source = runner.read_text()
ast.parse(runner_source)
assert "assert sha(seed/'added-tests.json')==config['AddedTestsSha256']" in runner_source
assert runner_source.index("assert sha(seed/'added-tests.json')") < runner_source.index('o.mkdir(exist_ok=False)')
assert "peer['SourceFreezeSha256']==source and peer['SourceClosureSha256']==closure" in runner_source
assert "assert [n for n in names if n in oldset]==previous" in runner_source
assert "[n for n in names if n not in oldset]==added['Names']" in runner_source
assert sha(ROOT / 'artifacts/spawn-priority351-corrections-focused-independent/report.json') == 'DD3DC38152E23BD01952131A12E1996FA98F2FE7EEADC0C69FAECDD180B55ACF'
result = {'Status':'PASS_INDEPENDENT_CORRECTED351_SEED4_SOURCE_PREFLIGHT', 'SourceFreezeSha256':sha(SEED / 'source-freeze.json'), 'SourceClosureSha256':sha(SEED / 'source-resource-closure.json'), 'ConfigurationSha256':sha(SEED / 'configuration.json'), 'RunnerSha256':sha(runner), 'ParentHead':PARENT, 'OwnedPaths':49, 'OwnedCSharp':sum(row['Path'].endswith('.cs') for row in source['Files']), 'StableClosureRows':3006, 'ReadinessInputs':37, 'PackageResources':6, 'RegistryRows':587, 'IntentionalRegistryLimitsChanges':registry_changes, 'CurrentRuntimeSourceBindings':74, 'AllLedgerEvidence31RawExact':True, 'CompatibilityCounts':config['CompatibilityCounts'], 'PreviousTests':7286, 'NewTestNamesActualSourceOrder':added['Names'], 'ExpectedFullTestsArithmeticOnly':7297, 'Plans':plans, 'Focused59IndependentPassed':True, 'Scope':'Source/input preflight only; failed full1 remains immutable. New full success not claimed. Root must independently inspect settled full0/test7297/integrations11/packages2/children and restore intended587customLimits after preserving generated registry.', 'SdkExecuted':False, 'MainModified':False, 'NetworkUsed':False}
assert not OUT.exists()
OUT.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(sha(OUT))
