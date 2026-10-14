import hashlib
import json
from pathlib import Path

repo = Path(__file__).resolve().parent.parent
artifacts = repo / 'artifacts'
freeze_path = artifacts / 'distribution-pending-promotion-source-freeze.json'
assert hashlib.sha256(freeze_path.read_bytes()).hexdigest() == '01a38636858e4cc055d6a774cbbe51505f168670b295f02f3e4bdc0dc4770a59'
freeze = json.loads(freeze_path.read_text(encoding='utf-8'))
candidate = Path(freeze['Candidate'])
for item in freeze['Sources']:
    assert hashlib.sha256((candidate / item['Path']).read_bytes()).hexdigest() == item['NewSha256']
for item in freeze['Resources']:
    assert hashlib.sha256((candidate / item['Path']).read_bytes()).hexdigest() == item['Sha256']
focus_path = artifacts / 'distribution-pending-promotion-hook-focus-initial.log'
focus = json.loads(focus_path.read_text(encoding='utf-8-sig'))
controls = ['Erlang.Tests.ReferenceResourceHostChecks', 'Erlang.Tests.HostDistributionChecks', 'Erlang.Tests.BeamLoaderLifecycleChecks', 'Erlang.Tests.DistributionPendingChecks']
assert focus == {'Passed': True, 'NativeOtpExecutions': 0, 'CoreRegistrationsAdded': 0, 'Controls': [{'Name': name, 'Passed': True} for name in controls]}
audit_path = artifacts / 'distribution-pending-promotion-loaded-assemblies.jsonl'
loaded = [json.loads(line) for line in audit_path.read_text(encoding='utf-8-sig').splitlines()]
assert {item['Name'] for item in loaded} == {'Erlang.DistributionPendingPromotionFocus', 'Erlang.Tests', 'Erlang.Runtime'}
assert len(loaded) == 3 and len({item['ProcessId'] for item in loaded}) == 1
for item in loaded:
    path = Path(item['Location'])
    assert path.is_relative_to(candidate / 'tools/Erlang.DistributionPendingPromotionFocus/bin/Debug/net10.0')
    assert hashlib.sha256(path.read_bytes()).hexdigest().upper() == item['Sha256']
    assert len(item['Mvid']) == 36
    if item['Name'] == 'Erlang.Tests':
        original = candidate / 'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll'
        assert hashlib.sha256(original.read_bytes()).hexdigest().upper() == item['Sha256']
    if item['Name'] == 'Erlang.Runtime':
        original = candidate / 'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll'
        assert hashlib.sha256(original.read_bytes()).hexdigest().upper() == item['Sha256']
style_path = artifacts / 'distribution-pending-promotion-style-initial.log'
assert style_path.read_text(encoding='utf-8-sig').strip() == 'Source layout: 0 files need formatting'
build_path = artifacts / 'distribution-pending-promotion-target-build-initial.log'
build = build_path.read_text(encoding='utf-8-sig')
assert 'Предупреждений: 0' in build and 'Ошибок: 0' in build
observer = artifacts / 'distribution-pending-load-observer'
result = {
    'Passed': True,
    'Seed': freeze['Seed'],
    'SourcesExactAfterTargetFocus': 20,
    'ImportSourcesExact': 18,
    'Original15RuntimePathsExact': True,
    'ResourcesExact': len(freeze['Resources']),
    'BuildExitCode': 0,
    'BuildWarnings': 0,
    'BuildErrors': 0,
    'LayoutCheckExitCode': 0,
    'HookRegressionExitCode': 0,
    'ActualGroupInvocations': 4,
    'PerGroupBudgetSeconds': 30,
    'RepeatedGroupInvocationsClaimed': False,
    'Steering': 'Root clarified30 means per-group timeout, not30repetitions; exactlyone invocation each of four unchanged actual test groups',
    'ActualLoadedAssemblies': loaded,
    'ActualLoadedTestsDllEqualsBuiltTestsBytes': True,
    'ActualLoadedRuntimeDllEqualsBuiltRuntimeBytes': True,
    'ObservationMethod': 'artifact-only DOTNET_STARTUP_HOOKS AppDomain assembly-load observer records actual live assembly location/MVID and on-disk SHA256; environment restored after process terminal',
    'ObserverChangesCandidateSource': False,
    'Controls': focus['Controls'],
    'CleanupEvidenceScope': 'actual selected group assertions include resource closure, controller/kernel death while oldbody held, runtime halt quiescence and ordinary disposal; no claim arbitrary host callbacks are bounded',
    'SourceChangesDuringValidation': 0,
    'MainWritesOrGitMutations': 0,
    'FullSuiteRun': False,
    'MainFullPassedClaimed': False,
    'NativeOtpExecutions': 0,
    'CoreRegistrationsAdded': 0,
    'ConnectedNativeKernelAdmissionOrTransportClaimed': False,
    'Files': [{'Path': str(path), 'Sha256': hashlib.sha256(path.read_bytes()).hexdigest()} for path in [freeze_path, focus_path, audit_path, build_path, style_path, artifacts / 'distribution-pending-promotion-source-guard.json', artifacts / 'distribution-pending-promotion-source-freeze-initial.json', artifacts / 'distribution-pending-promotion-source-freeze-duplicate-initial-failure.json', artifacts / 'distribution-pending-promotion-producer-failure.json', observer / 'StartupHook.cs', observer / 'LoadObserverNames.cs', observer / 'LoadObserver.csproj', observer / 'bin/Debug/net10.0/LoadObserver.dll', artifacts / 'distribution-pending-load-observer-build.log']]
}
output = artifacts / 'distribution-pending-promotion-hook-guard.json'
output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Path': str(output), 'Sha256': hashlib.sha256(output.read_bytes()).hexdigest(), 'Passed': True, 'ActualGroups': 4}))
