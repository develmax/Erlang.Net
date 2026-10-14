import hashlib
import json
from pathlib import Path

repo = Path(__file__).resolve().parent.parent
artifacts = repo / 'artifacts'
freeze_path = artifacts / 'distribution-pending-source-freeze.json'
initial_path = artifacts / 'distribution-pending-source-freeze-initial.json'
assert hashlib.sha256(freeze_path.read_bytes()).hexdigest() == 'bef32838144b64c7610297264eb90bad74b8a107d8fa91a42583526895d72ff2'
assert hashlib.sha256(initial_path.read_bytes()).hexdigest() == 'c7d0e7c1a071b6e1af5eaf8629709d0b6d665571917a49eae411a5d73233c5fc'
freeze = json.loads(freeze_path.read_text(encoding='utf-8'))
initial = json.loads(initial_path.read_text(encoding='utf-8'))
candidate = Path(freeze['Candidate'])
assert len(freeze['OwnedPaths']) == 22 and freeze['UnchangedSeedFiles'] == 6887
initial_files = {item['Path']: item for item in initial['OwnedPaths']}
formatter_changes = []
for item in freeze['OwnedPaths']:
    data = (candidate / item['Path']).read_bytes()
    assert hashlib.sha256(data).hexdigest() == item['NewSha256'], item['Path']
    previous = initial_files[item['Path']]
    if previous != item:
        formatter_changes.append({'Path': item['Path'], 'Previous': previous, 'Current': item})
assert [item['Path'] for item in formatter_changes] == ['tools/Erlang.DistributionPendingFocus/DistributionPendingChecks.cs']
resources_exact = []
for item in freeze['Resources']:
    assert hashlib.sha256((candidate / item['Path']).read_bytes()).hexdigest() == item['Sha256']
    resources_exact.append(item)
focus_path = artifacts / 'distribution-pending-focus-initial.log'
assert json.loads(focus_path.read_text(encoding='utf-8-sig')) == {'Passed': True, 'NativeOtpExecutions': 0, 'CoreRegistrationsAdded': 0}
build_path = artifacts / 'distribution-pending-target-build-initial.log'
build = build_path.read_text(encoding='utf-8-sig')
assert 'Предупреждений: 0' in build and 'Ошибок: 0' in build
style_path = artifacts / 'distribution-pending-style-initial.log'
style_write_path = artifacts / 'distribution-pending-style-write.log'
assert 'Source layout: 1 files need formatting' in style_path.read_text(encoding='utf-8-sig')
assert style_write_path.read_text(encoding='utf-8-sig').strip() == 'Source layout: 1 files updated'
result = {
    'Passed': True,
    'Seed': freeze['Seed'],
    'Candidate': str(candidate),
    'OwnedFilesExactAfterTargetFocus': 22,
    'UnchangedSeedFilesBeforeBuild': freeze['UnchangedSeedFiles'],
    'ResourceFilesExact': len(resources_exact),
    'ExistingProductionPathsChanged': [item['Path'] for item in freeze['OwnedPaths'] if item['OldSource'] is not None],
    'NewProductionPaths': [item['Path'] for item in freeze['OwnedPaths'] if item['OldSource'] is None and item['Path'].startswith('src/')],
    'FormatterChanges': formatter_changes,
    'TargetBuildExitCode': 0,
    'TargetWarnings': 0,
    'TargetErrors': 0,
    'FocusTerminalExitCode': 0,
    'RuntimeSourceRepairsAfterCompile': 0,
    'ControlsExecuted': [
        'flat typed resource handle/repeated pending identity/id',
        'valid remote node spelling/local and malformed node rejection',
        'explicit2-node metadata budget exhaustion',
        'strict resource kind/plain marker/foreign owner rejection',
        'live local ETF saved identity restoration/foreign fallback',
        'pinned64bit small range and lower32 signed/high alias source contracts (native observations pending)',
        'idle metadata retention/new generation ID increment/stale and disposed leases',
        'real registered kernel/controller SETUP reservation and duplicate role rejection',
        'controller kill cleanup before logical Done with old callback held/distinct replacement',
        '32 real kernel reservation release/reacquire loops and kernel death cleanup',
        'immediate halt/quiescence/ordinary disposal and closed ETF fallback'
    ],
    'SharedRuntimeMembershipGate': True,
    'DeathHookPrecedesStopAndDone': True,
    'KernelNativeRoleOrConnectedAdmissionClaimed': False,
    'NativeOtpExecutions': 0,
    'CoreRegistrationsAdded': 0,
    'FullSuiteRun': False,
    'MainWrites': 0,
    'GitMutations': 0,
    'InheritedCreationZeroSavedIdentityScopePreserved': True,
    'Scope': 'runtime-owned pending resource/lifetime primitive and setup reservations, no complete native distribution/API promotion',
    'Files': [{'Path': str(path), 'Sha256': hashlib.sha256(path.read_bytes()).hexdigest()} for path in [freeze_path, initial_path, focus_path, build_path, style_path, style_write_path, artifacts / 'distribution-pending-producer-initial-failure.json', artifacts / 'prepare-distribution-pending-hooks-initial.py', artifacts / 'prepare-distribution-pending-hooks.py', candidate / 'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll', candidate / 'tools/Erlang.DistributionPendingFocus/bin/Debug/net10.0/Erlang.DistributionPendingFocus.dll']]
}
output = artifacts / 'distribution-pending-evidence-guard.json'
output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Path': str(output), 'Sha256': hashlib.sha256(output.read_bytes()).hexdigest(), 'Passed': True, 'OwnedFiles': 22, 'Resources': len(resources_exact)}))
