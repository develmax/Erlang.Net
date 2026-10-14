import hashlib
import json
from pathlib import Path

repo = Path(__file__).resolve().parent.parent
artifacts = repo / 'artifacts'
freeze_path = artifacts / 'distribution-controller-part267-source-freeze.json'
assert hashlib.sha256(freeze_path.read_bytes()).hexdigest() == '5ad6f09155f942188fb89c7d000cb3a64ec3791f90ff96d3875046d98622318b'
freeze = json.loads(freeze_path.read_text(encoding='utf-8'))
candidate = Path(freeze['Candidate'])
before_path = candidate / 'docs/validation/part-266/tests.json'
before = json.loads(before_path.read_text(encoding='utf-8'))
after_path = artifacts / 'distribution-controller-part267-full-initial-tests.json'
after = json.loads(after_path.read_text(encoding='utf-8-sig'))
assert before['Passed'] == 6483 and before['Failed'] == 0
assert after['Passed'] == 6468 and after['Failed'] == 16 and len(after['Tests']) == 6484
assert [item['Name'] for item in before['Tests']] == [item['Name'] for item in after['Tests'][:-1]]
assert after['Tests'][-1]['Name'] == 'runtime/distribution-host-controller-generation-queue-notification-lifetime'
assert after['Tests'][-1]['Status'] == 'Passed'
failures = [item for item in after['Tests'] if item['Status'] == 'Failed']
assert len(failures) == 16
assert all(item['Name'].startswith('oracle/compiled-module/erlang-port-native-option-') and 'Expected {ok,done}, got {error,error,eacces}' in item['Error'] for item in failures)
assert not (candidate / 'artifacts/integration-results.json').exists()
full_log_path = artifacts / 'distribution-controller-part267-full-initial.log'
full_log = full_log_path.read_text(encoding='utf-8-sig')
assert '6468/6484 passed' in full_log and 'Validation passed' not in full_log
focus_path = artifacts / 'distribution-controller-part267-focus.log'
assert json.loads(focus_path.read_text(encoding='utf-8-sig')) == {'Passed': True, 'NativeExecutions': 0, 'RegisteredExportsAdded': 0}
style_path = artifacts / 'distribution-controller-part267-style-initial.log'
assert style_path.read_text(encoding='utf-8-sig').strip() == 'Source layout: 0 files need formatting'
target_build_path = artifacts / 'distribution-controller-part267-target-build.log'
target_build = target_build_path.read_text(encoding='utf-8-sig')
assert 'Предупреждений: 0' in target_build and 'Ошибок: 0' in target_build
for item in freeze['OwnedPaths']:
    data = (candidate / item['Path']).read_bytes()
    assert hashlib.sha256(data).hexdigest() == item['NewSha256'], item['Path']
    assert data.decode('utf-8') == item['NewSource']
for item in freeze['Resources']:
    assert hashlib.sha256((candidate / item['Path']).read_bytes()).hexdigest() == item['Sha256']
supported_path = candidate / 'docs/supported-mfas.json'
assert hashlib.sha256(supported_path.read_bytes()).hexdigest() == freeze['BaselineSupportedMfasSha256']
supported = json.loads(supported_path.read_text(encoding='utf-8-sig'))
assert len(supported) == 493 and sum(item['Module'] == 'erlang' for item in supported) == 335
proof_files = [freeze_path, before_path, after_path, full_log_path, focus_path, style_path, target_build_path, supported_path,
    artifacts / 'distribution-controller-source-freeze-v3.json',
    artifacts / 'distribution-controller-focus-guard.json',
    candidate / 'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll',
    candidate / 'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll',
    candidate / 'tools/Erlang.DistributionPromotionFocus/bin/Debug/net10.0/Erlang.DistributionPromotionFocus.dll']
result = {
    'ScopedEvidenceGuardPassed': True,
    'FullValidationPassed': False,
    'FullIntegrationAndPackagesExecuted': False,
    'Seed': freeze['Seed'],
    'OwnedPathsExactAfterTargetAndFailedFull': len(freeze['OwnedPaths']),
    'UnchangedOtherSeedFilesBeforeBuild': freeze['UnchangedSeedFiles'],
    'TargetBuildExitCode': 0,
    'TargetBuildWarnings': 0,
    'TargetBuildErrors': 0,
    'TargetFocusExitCode': 0,
    'LayoutExitCode': 0,
    'SourceRepairsPerformed': 0,
    'FullValidationTerminalExitCode': 1,
    'FullPassed': after['Passed'],
    'FullFailed': after['Failed'],
    'Old6483NamePrefixExact': True,
    'Old6483StatusesExact': False,
    'AddedPermanentHostTest': after['Tests'][-1],
    'Failures': failures,
    'Win32PhysicalCauseProved': False,
    'FailureObservation': '16 unchanged native Windows port fixture cases returned mapped eacces; RuntimePorts maps many Win32 errors to this reason, so actual platform cause is not inferred',
    'FullRerunPerformed': False,
    'RootNextGate': 'import only14 owned source paths, then one actual Main full validation with Windows child-port execution outside sandbox; retain these failed observations',
    'ResourceFilesExactAfterFailedFull': len(freeze['Resources']),
    'DefaultMfas': len(supported),
    'ErlangMfas': sum(item['Module'] == 'erlang' for item in supported),
    'DefaultMfaSnapshotBytesUnchanged': True,
    'CoreRegistrationsAdded': 0,
    'OriginalRevision3RuntimeBytesExact': True,
    'ActualCachedNoticeIdentity64ControlsPassed': True,
    'RuntimeOwnedAtomicExitDisposalHooksImplemented': False,
    'NativeOtpExecutions': 0,
    'TransportOrErlangMagicHandlesImplemented': False,
    'MainSourceOrGitMutationsByThisAgent': 0,
    'Scope': 'target/focus passes on fresh source seed and faithful preservation of failed full run; no full or native distribution compatibility promotion',
    'Files': [{'Path': str(path), 'Sha256': hashlib.sha256(path.read_bytes()).hexdigest()} for path in proof_files]
}
output = artifacts / 'distribution-controller-part267-scoped-promotion-guard.json'
output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Path': str(output), 'Sha256': hashlib.sha256(output.read_bytes()).hexdigest(), 'ScopedEvidenceGuardPassed': True, 'FullValidationPassed': False}))
