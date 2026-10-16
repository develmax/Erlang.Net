"""Detached Root-owned proposal. No action occurs until explicitly invoked with actual gate identities."""
from pathlib import Path
import argparse
import hashlib
import json

parser = argparse.ArgumentParser()
parser.add_argument('phase', choices=['scope', 'finalize'])
for name in ['full-guard-sha256', 'native-guard-sha256', 'main-source-freeze-sha256']:
    parser.add_argument('--' + name, required=True)
parser.add_argument('--english-entry', type=Path, required=True)
parser.add_argument('--runtime-note', type=Path, required=True)
args = parser.parse_args()
root = Path.cwd().resolve()
here = Path(__file__).resolve().parent
main = root / 'artifacts/root-part323-main2'
control = root / 'artifacts/root-part323-docs2'
output = root / 'docs/validation/part-323'
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
read = lambda path: json.loads(path.read_bytes())
encoded = lambda value: (json.dumps(value, indent=2) + '\n').encode('utf-8')


def exact(path, expected):
    data = path.read_bytes()
    assert len(expected) == 64 and sha(data) == expected.upper(), str(path)
    return data


for row in read(here / 'proposal-freeze.json')['Files']:
    assert len(exact(here / row['Path'], row['Sha256'])) == row['Bytes']
cfg = read(here / 'configuration.json')
static_rows = read(here / 'existing-copy-rows.json')['Rows']
for reference in cfg['ExistingCommittedProofReferences']:
    assert len(exact(root/reference['Path'],reference['Sha256']))==reference['Bytes']
for row in static_rows:
    assert len(exact(root / row['SourcePath'], row['Sha256'])) == row['Bytes']
for path in [args.english_entry.resolve(), args.runtime_note.resolve()]:
    assert path.is_relative_to(root) and path.is_file()
entry = args.english_entry.read_text(encoding='utf-8').strip()
note = args.runtime_note.read_text(encoding='utf-8').strip()
assert entry.startswith('## Part 323') and note.startswith('Part 323:')
assert '[FINAL_GATES_PENDING]' not in entry + note and 'EXPECTED_PENDING' not in entry + note
full_path = main / 'full-guard.json'
native_path = root / 'artifacts/root-part323-main2-windows-native1/guard.json'
exact(full_path, args.full_guard_sha256)
assert cfg['ActualFullGuardSha256'] is None or args.full_guard_sha256.upper()==cfg['ActualFullGuardSha256']
exact(native_path, args.native_guard_sha256)
assert cfg['ActualNativeGuardSha256'] is None or args.native_guard_sha256.upper()==cfg['ActualNativeGuardSha256']
full, native = read(full_path), read(native_path)
fp, closure_path = main / 'source-freeze.json', main / 'source-resource-closure.json'
exact(fp, args.main_source_freeze_sha256)
assert args.main_source_freeze_sha256.upper() == cfg['ActualMainSourceFreezeSha256']
exact(closure_path, cfg['RequiredFull']['SourceClosureSha256'])
source_envelope=read(fp)
assert source_envelope['AuthorFinalFreeze']['Sha256']==cfg['AuthorFinalFreezeSha256']
pre=source_envelope['PreviousSuccessfulMain1PreAttribute'];exact(root/pre['Path'],pre['Sha256'])
assert read(root/pre['Path'])['Status']=='PRESERVED_SUCCESSFUL_MAIN1_PRE_ATTRIBUTE_NOT_FAILURE'
assert (full['Passed'], full['Failed'], full['IntegrationResults'], full['Registry'], full['ErlangMfas']) == (6685, 0, 11, 509, 351)
assert full['Status'] == 'PASS_ACTUAL_MAIN323_6685_FULL_AND_ELEVEN_INTEGRATIONS'
assert full['FullExecutionContext'] == 'Outside sandbox'
assert full['SourceFreezeSha256'] == sha(fp.read_bytes()) and full['SourceClosureSha256'] == sha(closure_path.read_bytes())
assert native['Status'] == 'PASS_ACTUAL_MAIN323_POST_FULL_WINDOWS_LEGACY_QUERY_FIVE_PHASES_AND_REALLOCATION_THREE'
assert (native['FullPassed'], native['SourceFilesRehashed'], native['OriginalExecuted']) == (6685, 2349, False)
assert native['SourceFreezeSha256'] == sha(fp.read_bytes()) and native['ActualFullGuard']['Sha256'] == sha(full_path.read_bytes())
assert read(main / 'full-exit.json')['ExitCode'] == 0
assert read(main / 'full-exit.json')['Context'] == 'Outside sandbox'
source_rows = read(closure_path)['Files']
assert len(source_rows) == 2349
for row in source_rows:
    assert len(exact(root / row['Path'], row['Sha256'])) == row['Bytes']
parents = {row['Path']: row for row in cfg['MetadataParentBindings']}


def parent_check(name):
    row = parents[name]
    assert len(exact(root / name, row['Sha256'])) == row['Bytes']


for name in cfg['MetadataFiles'] + ['CHANGELOG.md', 'docs/changelog/0049.md', 'docs/changelog/0048.md', 'docs/changelog/0047.md']:
    parent_check(name)
scope_path = root / 'docs/module-readiness-scope.json'
scope = read(scope_path)
runtime = next(row for row in scope['Components'] if row['Id'] == 'Runtime')
assert not output.exists()
assert (root / cfg['ActiveVolume']).is_file()
assert sha((root / 'docs/changelog/0048.md').read_bytes()) == cfg['Closed0048MustRemainExact']
if args.phase == 'scope':
    assert not control.exists()
    parent_check('docs/module-readiness-scope.json')
    assert scope['ComparisonTestReport'] == cfg['ScopePhase']['ComparisonTestReportBefore'] and note not in runtime['Implemented']
    before = scope_path.read_bytes()
    runtime['Implemented'].append(note)
    scope['ComparisonTestReport'] = cfg['ScopePhase']['ComparisonTestReportAfter']
    after = encoded(scope)
    control.mkdir()
    (control / 'scope-before.json').write_bytes(before)
    (control / 'runtime-note.txt').write_bytes((note + '\n').encode('utf-8'))
    (control / 'english-entry.md').write_bytes((entry + '\n').encode('utf-8'))
    scope_path.write_bytes(after)
    (control / 'scope-command.json').write_bytes(encoded({'BeforeSha256': sha(before), 'AfterSha256': sha(after),
        'FinalizerSha256': sha(Path(__file__).read_bytes()), 'NoteSha256': sha(note.encode('utf-8')),
        'EntrySha256': sha(entry.encode('utf-8')), 'ActualFullGuardSha256': sha(full_path.read_bytes()),
        'ActualNativeGuardSha256': sha(native_path.read_bytes())}))
    print('Scope323 applied; actual readiness CLI is required before finalize.')
    raise SystemExit(0)
command = read(control / 'scope-command.json')
assert command['BeforeSha256'] == parents['docs/module-readiness-scope.json']['Sha256'] == sha((control / 'scope-before.json').read_bytes())
assert command['AfterSha256'] == sha(scope_path.read_bytes()) and command['FinalizerSha256'] == sha(Path(__file__).read_bytes())
assert command['NoteSha256'] == sha(note.encode('utf-8')) and command['EntrySha256'] == sha(entry.encode('utf-8'))
assert runtime['Implemented'].count(note) == 1 and scope['ComparisonTestReport'] == cfg['ScopePhase']['ComparisonTestReportAfter']
assert read(main / 'readiness-exit.json')['ExitCode'] == 0
ready = read(root / 'docs/module-readiness.json')
for key, value in cfg['RequiredReadiness'].items():
    assert ready[key] == value, key
tests = read(root / 'artifacts/tests.json')
assert tests['Passed'] == 6685 and tests['Failed'] == 0 and all(row['Status'] == 'Passed' for row in tests['Tests'])
assert len(read(root / 'artifacts/integration-results.json')) == 11
assert sha((main / 'raw-supported-mfas.json').read_bytes()) == full['RawRegistrySha256']
registry = read(root / 'docs/supported-mfas.json')
assert len(registry) == 509 and sum(row['Module'] == 'erlang' for row in registry) == 351
copy_rows = list(static_rows)
for row in cfg['RequiredPendingCopyRows']:
    path = (root / row['SourcePath']).resolve()
    assert path.is_relative_to(root)
    data = path.read_bytes()
    copy_rows.append({**row, 'Bytes': len(data), 'Sha256': sha(data)})
changes = {}
volume = root / cfg['ActiveVolume']
combined = volume.read_bytes() + (b'\n' if volume.read_bytes().endswith(b'\n') else b'\n\n') + (entry + '\n').encode('utf-8')
assert len(combined) <= 32768 and len(combined.splitlines()) <= 400
for name in cfg['MetadataFiles']:
    data = (root / name).read_bytes()
    assert note.encode('utf-8') not in data
    if name == 'docs/ERLANG_COMPATIBILITY.md':
        for replacement in cfg['ExistingRowReplacements']:
            old = replacement['Before'].encode('utf-8')
            assert data.count(old) == 1
            data = data.replace(old, replacement['After'].encode('utf-8'))
    newline = b'\r\n' if b'\r\n' in data else b'\n'
    position = data.index(newline * 2) + 2 * len(newline)
    changes[root / name] = data[:position] + note.encode('utf-8') + newline * 2 + data[position:]
data = (root / 'CHANGELOG.md').read_bytes()
old = cfg['ChangelogLabelBefore'].encode('utf-8')
assert data.count(old) == 1
changes[root / 'CHANGELOG.md'] = data.replace(old, cfg['ChangelogLabelAfter'].encode('utf-8'))
changes[volume] = combined
copies, destinations = [], set()
for row in copy_rows:
    source, destination = (root / row['SourcePath']).resolve(), (root / row['Destination']).resolve()
    assert source.is_relative_to(root) and destination.is_relative_to(output) and destination not in destinations
    assert destination not in {output / 'evidence-index.json', output / 'evidence-receipt.json'}
    assert source.suffix.lower() not in {'.dll', '.zip', '.tar', '.gz'}
    data = exact(source, row['Sha256'])
    assert len(data) == row['Bytes'] and len(data) < 12 * 1024 * 1024
    destinations.add(destination)
    copies.append((destination, data))
# Validate relative proof links against the declared final namespace before any write.
import re
for link in re.findall(r'\[[^\]]+\]\(([^)]+)\)', entry):
    if link.startswith('../validation/part-323/'):
        target = (root / 'docs/changelog' / link).resolve()
        assert target in destinations or target in {output / 'evidence-index.json', output / 'evidence-receipt.json'}, link
# No evidence or metadata writes occur before every actual gate, source, parent and copy byte is settled.
output.mkdir(parents=True)
for destination, data in copies:
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(data)
for path, data in changes.items():
    path.write_bytes(data)
(output / 'evidence-index.json').write_bytes(encoded(copy_rows))
receipt = {'Status':'PASS_ACTUAL_MAIN323_FULL_NATIVE_READINESS_CALLER_OWNED_WAIT_AND_GENUINE_BEAM_SCOPE',
    'Passed':6685,'Failed':0,'IntegrationResults':11,'Registry':509,'PreviousRegistry':509,'RegistryDelta':0,
    'ErlangPresence':351,'SourcePaths':30,'CSharpPaths':13,'SourceClosurePaths':2349,
    'SourceFreezeSha256':sha(fp.read_bytes()),'SourceClosureSha256':sha(closure_path.read_bytes()),
    'ActualFullGuardSha256':sha(full_path.read_bytes()),'ActualNativeGuardSha256':sha(native_path.read_bytes()),
    'EvidenceIndexSha256':sha((output/'evidence-index.json').read_bytes()),'FinalizerSha256':sha(Path(__file__).read_bytes()),
    'Original323Executed':False,'Current323RemotePending':True,'Published322FiveWorkflowsPendingAtCheckpoint':True,'Previous321CiPassed':True,
    'Previous321FourTerminalWorkflowsPassed':True,'Previous321OrdinaryPending':True,'LiveSdkPeerOrNativeWireEqualityClaimed':False,
    'ApiPresenceIsNotSemanticCompletion':True,'Closed0047Sha256':sha((root/'docs/changelog/0047.md').read_bytes()),
    'Closed0048Sha256':sha((root/'docs/changelog/0048.md').read_bytes()),'ActiveVolume':cfg['ActiveVolume'],
    'GreenFocusGroups':17,'HistoricalSemanticRedCases':4,'QueryTimeoutAndMissingPublicDownDistinct':True,
    'ActualGenuineOriginalBeamSdkPrepareSupportsInvokePassed':True,'Original323SyntheticControls':69,'Original323PinnedObjects':14,'Source325LinkGapsRetained':True,'PreviousMain1PreAttributeFullNativePassedNotFailure':True,'AttributeRepairAndDerivedOwnSealOnly':True,
    'Files':copy_rows}
(output / 'evidence-receipt.json').write_bytes(encoded(receipt))
source_paths = [row['Path'] for row in read(fp)['Files']]
stage = cfg['ExplicitStageFilesRequired'] + source_paths + [row['Destination'] for row in copy_rows] + [
    'docs/validation/part-323/evidence-index.json', 'docs/validation/part-323/evidence-receipt.json']
(control / 'explicit-stage-files.json').write_bytes(encoded({'Files': list(dict.fromkeys(stage)), 'GitExecuted': False}))
print(json.dumps({'CopiedProofRows': len(copy_rows), 'VolumeBytes': len(combined),
                  'EvidenceReceiptSha256': sha((output / 'evidence-receipt.json').read_bytes())}))
