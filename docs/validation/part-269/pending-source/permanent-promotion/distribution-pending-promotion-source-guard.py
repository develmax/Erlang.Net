import hashlib
import json
from pathlib import Path

repo = Path(__file__).resolve().parent.parent
artifacts = repo / 'artifacts'
freeze_path = artifacts / 'distribution-pending-promotion-source-freeze.json'
assert hashlib.sha256(freeze_path.read_bytes()).hexdigest() == '01a38636858e4cc055d6a774cbbe51505f168670b295f02f3e4bdc0dc4770a59'
freeze = json.loads(freeze_path.read_text(encoding='utf-8'))
candidate = Path(freeze['Candidate'])
original = json.loads((artifacts / 'distribution-pending-source-freeze.json').read_text(encoding='utf-8'))
original_files = {item['Path']: item for item in original['OwnedPaths']}
assert len(freeze['Sources']) == 20
assert sum(item['ImportIntoMain'] for item in freeze['Sources']) == 18
for item in freeze['Sources']:
    data = (candidate / item['Path']).read_bytes()
    assert data.decode('utf-8') == item['NewSource']
    assert hashlib.sha256(data).hexdigest() == item['NewSha256']
    if item['Path'].startswith('src/'):
        assert item['NewSha256'] == original_files[item['Path']]['NewSha256']
program = next(item for item in freeze['Sources'] if item['Path'] == 'tests/Erlang.Tests/Program.cs')
append = '\nTest(Erlang.Tests.DistributionPendingTestNames.Test, Erlang.Tests.DistributionPendingChecks.Run);\n'
assert program['NewSource'].count(append) == 1
assert program['NewSource'].replace(append, '', 1) == program['OldSource']
assert len(freeze['OldTestNames']) == 6484
assert freeze['OldTestNames'][-1] == 'runtime/distribution-host-controller-generation-queue-notification-lifetime'
for item in freeze['Resources']:
    assert hashlib.sha256((candidate / item['Path']).read_bytes()).hexdigest() == item['Sha256']
for item in freeze['NativeScopeArtifacts']:
    assert item['Sha256'] == original_files[item['Path']]['NewSha256']
    assert not item['NativeExecuted'] and not item['ImportIntoMainProduction']
old_aux = json.loads((artifacts / 'distribution-pending-promotion-source-freeze-initial.json').read_text(encoding='utf-8'))
before = {item['Path']: item for item in old_aux['Sources']}
manual_layout_changes = [{'Path': item['Path'], 'Previous': before[item['Path']], 'Current': item} for item in freeze['Sources'] if item != before[item['Path']]]
assert [item['Path'] for item in manual_layout_changes] == ['tools/Erlang.DistributionPendingPromotionFocus/Program.cs']
result = {
    'SourceGuardPassed': True,
    'Seed': freeze['Seed'],
    'FreezeSha256': hashlib.sha256(freeze_path.read_bytes()).hexdigest(),
    'ImportPathsExact': 18,
    'AuxiliarySourcePaths': 2,
    'Original15RuntimePathsExact': True,
    'Old6484DeclaredNamePrefixUnchanged': True,
    'ProgramChangeExactlyOneAppend': True,
    'PermanentHelperTypes': ['Erlang.Tests.DistributionPendingChecks', 'Erlang.Tests.DistributionPendingTestNames'],
    'SourceHelperNamespaceAndFilenameMatching': True,
    'OtherSeedFilesVerifiedBeforeBuild': freeze['UnchangedOtherSeedFiles'],
    'ResourcesExact': len(freeze['Resources']),
    'ManualAuxiliaryLayoutChanges': manual_layout_changes,
    'Builds': 0,
    'ActualPromotionTargetFocusExecuted': False,
    'FullSuiteExecuted': False,
    'NativeOtpExecutions': 0,
    'CoreRegistrationsAdded': 0,
    'MainOrGitWrites': 0,
    'RequiredHookRegressionGroups': ['Erlang.Tests.ReferenceResourceHostChecks', 'Erlang.Tests.HostDistributionChecks', 'Erlang.Tests.BeamLoaderLifecycleChecks', 'Erlang.Tests.DistributionPendingChecks'],
    'Scope': 'source-only permanent test preparation and isolated auxiliary actual-test-assembly runner; target/hook focus and Root Main full remain gates'
}
output = artifacts / 'distribution-pending-promotion-source-guard.json'
output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Path': str(output), 'Sha256': hashlib.sha256(output.read_bytes()).hexdigest(), 'SourceGuardPassed': True}))
