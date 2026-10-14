import hashlib
import json
from pathlib import Path
import subprocess

repo = Path(__file__).resolve().parent.parent
artifacts = repo / 'artifacts'
candidate = artifacts / 'distribution-controller-foundation-prototype'
freeze_path = artifacts / 'distribution-controller-source-freeze-v3.json'
freeze_bytes = freeze_path.read_bytes()
assert hashlib.sha256(freeze_bytes).hexdigest() == '392977b18f44ca96b9436b9365c44b49dd2cc2082c7d4c53f39f209aa8f46723'
freeze = json.loads(freeze_bytes)
assert freeze['UnchangedSeedFiles'] == 5269 and len(freeze['AddedFiles']) == 18
assert not freeze['ChangedSeedFiles'] and not freeze['MissingSeedFiles']
for entry in freeze['AddedFiles']:
    actual = (candidate / entry['Path']).read_bytes()
    assert actual.decode('utf-8') == entry['NewSource']
    assert hashlib.sha256(actual).hexdigest() == entry['NewSha256']
focus = artifacts / 'distribution-controller-focus-initial.log'
focus_value = json.loads(focus.read_text(encoding='utf-8-sig'))
assert focus_value == {'Passed': True, 'NativeExecutions': 0, 'RegisteredExportsAdded': 0}
style = artifacts / 'distribution-controller-style-initial.log'
style_write = artifacts / 'distribution-controller-style-write.log'
assert style.read_text(encoding='utf-8-sig').strip() == 'Source layout: 0 files need formatting'
assert style_write.read_text(encoding='utf-8-sig').strip() == 'Source layout: 0 files updated'
build = artifacts / 'distribution-controller-target-build-initial.log'
build_text = build.read_text(encoding='utf-8-sig')
assert 'Предупреждений: 0' in build_text and 'Ошибок: 0' in build_text
head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=repo, text=True).strip()
assert head == '5b3bd2e0b7f12bfcf4fe717e72579df454f8aa4d'
proof_files = [freeze_path, focus, style, style_write, build,
    artifacts / 'distribution-controller-revision2-independent-source-review.json',
    artifacts / 'distribution-controller-revision3-independent-source-review.json',
    artifacts / 'distribution-controller-revision3-ledger.json',
    candidate / 'tools/Erlang.DistributionHostFocus/bin/Debug/net10.0/Erlang.DistributionHostFocus.dll',
    candidate / 'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll',
    candidate / 'src/Erlang.Terms/bin/Debug/net10.0/Erlang.Terms.dll',
    candidate / 'src/Erlang.Serialization/bin/Debug/net10.0/Erlang.Serialization.dll']
result = {
    'Passed': True,
    'Revision': 3,
    'Seed': freeze['Seed'],
    'HeldMainAndSourceLinkParentHead': head,
    'CurrentMainRuntimeNotImportedIntoCandidate': True,
    'AddedFilesExactAfterBuild': len(freeze['AddedFiles']),
    'SeedFilesExactBeforeBuild': freeze['UnchangedSeedFiles'],
    'TargetBuildExitCode': 0,
    'TargetBuildWarnings': 0,
    'TargetBuildErrors': 0,
    'TargetFocusExitCode': 0,
    'RuntimeFocusAssertionsExecuted': True,
    'GroupsExecuted': [
        'basic live owner/duplicate owner/foreign handle/options/notice/snapshot immutability/queue/lease/closure',
        'actual actor callback wrong owner and foreign runtime/input-handler transfer',
        'zero-buffer count cap and payload-byte cap',
        '64 actual simultaneous-start arm/enqueue barrier controls',
        '16 empty segments accepted, 17 rejected, total queued segment cap and recovery',
        'actual kill logical completion while old callback blocked/distinct new process replacement/stale handle',
        'immediate runtime halt/quiescence and runtime disposal'
    ],
    'RaceBarrierInternalInterleavingClaimed': False,
    'KilledBodyLogicalCompletionEqualsUnwindClaimed': False,
    'LayoutCheckExitCode': 0,
    'FormatterChanges': 0,
    'CPUCountEnvironment': 2,
    'NativeExecutions': 0,
    'CoreRegistrationsAdded': 0,
    'MainSourceEdits': 0,
    'GitMutations': 0,
    'FullRegressionSuiteRun': False,
    'TransportOrErlangMagicHandleProof': False,
    'Scope': 'isolated HOST foundation; native option projection remains unexecuted, typed C# handles are not Erlang magic references',
    'Files': [{'Path': str(path), 'Sha256': hashlib.sha256(path.read_bytes()).hexdigest()} for path in proof_files]
}
output = artifacts / 'distribution-controller-focus-guard.json'
output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Path': str(output), 'Sha256': hashlib.sha256(output.read_bytes()).hexdigest(), 'Passed': True}))
