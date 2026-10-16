"""Root-only explicit scope/finalize entry points; no Git, build, SDK, VM or network dispatch."""
from pathlib import Path
import argparse
import re
from gates327 import ROOT, HERE, sha, read, encoded, exact, arguments, observed, readiness, copy_rows

parser = argparse.ArgumentParser()
parser.add_argument('phase', choices=['scope', 'finalize'])
arguments(parser)
parser.add_argument('--runtime-note', required=True, type=Path)
parser.add_argument('--english-entry', type=Path)
parser.add_argument('--readiness-sha256')
parser.add_argument('--readiness-exit-sha256')
args = parser.parse_args()
cfg, fp, closure, fullpath, nativepath, static = observed(args)
output = ROOT / 'docs/validation/part-327'
control = ROOT / 'artifacts/root-part327-docs1'
assert not output.exists()
assert len(exact(ROOT / cfg['NewVolume'], cfg['Active0050']['Sha256'])) == cfg['Active0050']['Bytes']
assert args.runtime_note.resolve().is_relative_to(ROOT / 'artifacts')
note = args.runtime_note.read_text(encoding='utf-8').strip()
assert note == (HERE / 'runtime-note-template.txt').read_text(encoding='utf-8').strip()
before = {item['Path']: item for item in cfg['MetadataBefore']}
for name in cfg['MetadataFiles'] + ['CHANGELOG.md', 'docs/changelog/0050.md', 'docs/changelog/0049.md']:
    item = before[name]
    assert len(exact(ROOT / name, item['Sha256'])) == item['Bytes']
scope_path = ROOT / 'docs/module-readiness-scope.json'
scope = read(scope_path)
runtime = next(item for item in scope['Components'] if item['Id'] == 'Runtime')
if args.phase == 'scope':
    assert not control.exists()
    item = before['docs/module-readiness-scope.json']
    assert len(exact(scope_path, item['Sha256'])) == item['Bytes']
    assert scope['ComparisonTestReport'] == cfg['ComparisonTestReportBefore']
    assert note not in runtime['Implemented']
    raw = scope_path.read_bytes()
    runtime['Implemented'].append(note)
    scope['ComparisonTestReport'] = cfg['ComparisonTestReportAfter']
    after = encoded(scope)
    # All actual full/native/source/static-copy/metadata checks settled before this one Main scope write.
    control.mkdir()
    (control / 'scope-before.json').write_bytes(raw)
    (control / 'runtime-note.txt').write_bytes((note + '\n').encode('utf-8'))
    scope_path.write_bytes(after)
    (control / 'scope-command.json').write_bytes(encoded({
        'ConfigurationSha256': sha((HERE / 'configuration.json').read_bytes()),
        'GatesSha256': sha((HERE / 'gates327.py').read_bytes()), 'BeforeSha256': sha(raw), 'AfterSha256': sha(after), 'FinalizerSha256': sha(Path(__file__).read_bytes()),
        'RuntimeNoteSha256': sha(note.encode('utf-8')), 'MainFreezeSha256': sha(fp.read_bytes()),
        'FullGuardSha256': sha(fullpath.read_bytes()), 'NativeGuardSha256': sha(nativepath.read_bytes())}))
    print('Scope327 applied. Run the actual readiness CLI before final texts/finalize.')
    raise SystemExit(0)
ready = readiness(args, fp.parent)
command = read(control / 'scope-command.json')
assert command['ConfigurationSha256'] == sha((HERE / 'configuration.json').read_bytes())
assert command['GatesSha256'] == sha((HERE / 'gates327.py').read_bytes())
assert command['BeforeSha256'] == before['docs/module-readiness-scope.json']['Sha256'] == sha((control / 'scope-before.json').read_bytes())
assert command['AfterSha256'] == sha(scope_path.read_bytes()) and command['FinalizerSha256'] == sha(Path(__file__).read_bytes())
assert command['RuntimeNoteSha256'] == sha(note.encode('utf-8'))
assert command['MainFreezeSha256'] == sha(fp.read_bytes()) and command['FullGuardSha256'] == sha(fullpath.read_bytes())
assert command['NativeGuardSha256'] == sha(nativepath.read_bytes())
assert runtime['Implemented'].count(note) == 1 and scope['ComparisonTestReport'] == cfg['ComparisonTestReportAfter']
ready_runtime = next(item for item in ready['Components'] if item['Id'] == 'Runtime')
assert ready_runtime['Implemented'].count(note) == 1
assert args.english_entry is not None and args.english_entry.resolve().is_relative_to(ROOT / 'artifacts')
entry = args.english_entry.read_text(encoding='utf-8').strip()
assert entry.startswith('## Part 327') and '### Actual final validation' in entry
assert '### Final gates — pending' not in entry and 'This draft is not final publication authority' not in entry
texts = read(args.english_entry.parent / 'text-bindings.json')
assert texts['Phase'] == 'final' and texts['MainFreezeSha256'] == sha(fp.read_bytes())
assert texts['FullGuardSha256'] == sha(fullpath.read_bytes()) and texts['NativeGuardSha256'] == sha(nativepath.read_bytes())
assert texts['ReadinessSha256'].upper() == args.readiness_sha256.upper()
assert texts['EnglishEntrySha256'] == sha(args.english_entry.read_bytes())
assert texts['PreparerSha256'] == sha((HERE / 'prepare-final-texts.py').read_bytes())
rows = copy_rows(cfg, fp, fullpath, nativepath, static)
for path, target in [(args.english_entry, 'final-author/english-entry.md'),
                     (args.runtime_note, 'final-author/runtime-note.txt'),
                     (args.english_entry.parent / 'text-bindings.json', 'final-author/text-bindings.json')]:
    raw = path.read_bytes()
    rows.append({'SourcePath': path.resolve().relative_to(ROOT).as_posix(),
                 'TargetPath': 'docs/validation/part-327/' + target, 'Bytes': len(raw), 'Sha256': sha(raw)})
copies, destinations = [], set()
for item in rows:
    source, destination = (ROOT / item['SourcePath']).resolve(), (ROOT / item['TargetPath']).resolve()
    assert source.is_relative_to(ROOT) and destination.is_relative_to(output) and destination not in destinations
    assert destination not in {output / 'evidence-index.json', output / 'evidence-receipt.json'}
    assert source.suffix.lower() not in {'.dll', '.zip', '.tar', '.gz'}
    raw = exact(source, item['Sha256'])
    assert len(raw) == item['Bytes'] and len(raw) < 12 * 1024 * 1024
    destinations.add(destination)
    copies.append((destination, raw))
for link in re.findall(r'\[[^\]]+\]\(([^)]+)\)', entry):
    if link.startswith('../validation/part-327/'):
        target = (ROOT / 'docs/changelog' / link).resolve()
        assert target in destinations or target == output / 'evidence-receipt.json', link
entry_bytes = (entry + '\n').encode('utf-8')
assert len(entry_bytes) <= 32768 and len(entry_bytes.splitlines()) <= 400
active = exact(ROOT / cfg['NewVolume'], cfg['Active0050']['Sha256'])
combined = active.rstrip() + b'\n\n' + entry_bytes
assert len(combined) <= 32768 and len(combined.splitlines()) <= 400
changes = {ROOT / cfg['NewVolume']: combined}
for name in cfg['MetadataFiles']:
    raw = (ROOT / name).read_bytes()
    assert note.encode('utf-8') not in raw
    if name == 'docs/ERLANG_COMPATIBILITY.md':
        for old, new in zip(cfg['CompatibilityRowsBefore'], cfg['CompatibilityRowsAfter']):
            assert raw.count(old.encode('utf-8')) == 1
            raw = raw.replace(old.encode('utf-8'), new.encode('utf-8'))
    newline = b'\r\n' if b'\r\n' in raw else b'\n'
    position = raw.index(newline * 2) + 2 * len(newline)
    changes[ROOT / name] = raw[:position] + note.encode('utf-8') + newline * 2 + raw[position:]
raw = (ROOT / 'CHANGELOG.md').read_bytes()
old = cfg['ChangelogIndexBefore'].encode('utf-8')
assert raw.count(old) == 1
newline = b'\r\n' if b'\r\n' in raw else b'\n'
replacement = cfg['ChangelogIndexAfter'].encode('utf-8').replace(b'\n', newline)
changes[ROOT / 'CHANGELOG.md'] = raw.replace(old, replacement)
# No evidence or metadata finalization writes occur before all actual gates, parent bytes and copies settle.
output.mkdir(parents=True)
for destination, raw in copies:
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(raw)
for path, raw in changes.items():
    path.write_bytes(raw)
(output / 'evidence-index.json').write_bytes(encoded(rows))
receipt = {
    'Status': 'PASS_ACTUAL_MAIN327_REGISTERED_ERROR_HANDLER_FULL_NATIVE_READINESS',
    'Passed': 6729, 'Failed': 0, 'IntegrationResults': 11, 'All6708PreviousOrderedNamesPreserved': True,
    'Registry': 509, 'PreviousRegistry': 509, 'RegistryDelta': 0, 'ErlangPresence': 351,
    'SourcePaths': 22, 'CSharpPaths': 17, 'SourceClosurePaths': 2379,
    'SourceFreezeSha256': sha(fp.read_bytes()), 'SourceClosureSha256': sha(closure.read_bytes()),
    'ActualFullGuardSha256': sha(fullpath.read_bytes()), 'ActualNativeGuardSha256': sha(nativepath.read_bytes()),
    'ActualReadinessSha256': args.readiness_sha256.upper(),
    'EvidenceIndexSha256': sha((output / 'evidence-index.json').read_bytes()),
    'FinalizerSha256': sha(Path(__file__).read_bytes()),
    'HistoricalSemanticRed': 'Same formatted helpers on exact326 production reject valid process_flag(error_handler, atom) with one badarg',
    'ActualFocusedSdkCases': 20, 'ActualFocusedHostSelectors': 5,
    'HistoricalOwnedProcessSettledWithoutTimeoutOrAggregate': True,
    'PriorDiagnosticNestedOrderSemanticRedRetained': True,
    'PriorDiagnosticFixtureSyntaxAndReceiptPreconditionsSeparate': True,
    'Current327OriginalExecuted': False, 'PlannedOriginalOrdinaryNotObserved': 5925, 'PlannedFd': 8,
    'Previous325ObservedOrdinary': 5891, 'Previous326ObservedOrdinary': 5905,
    'Previous326FourOtherTypedTerminalScopesPreserved': True,
    'StrictMapStdoutAndExitDiagnosticReprRefusalsRetainedSeparately': True,
    'NativeSdkChecksAndSeparateOriginal13NotDifferentialEquality': True,
    'HistoricalRawNormalization': False, 'ApiPresenceIsNotSemanticCompletion': True,
    'BeforeFirstHandlerStackParityDefaultAutoloadUndefinedLambdaAndRawBeamRemainPartial': True,
    'Closed0049Sha256': sha((ROOT / 'docs/changelog/0049.md').read_bytes()), 'ActiveVolume': cfg['NewVolume'],
    'Files': rows}
(output / 'evidence-receipt.json').write_bytes(encoded(receipt))
for closed in cfg['ClosedVolumes']:
    assert sha((ROOT / closed['Path']).read_bytes()) == closed['Sha256']
stage = cfg['MetadataStageFiles'] + [item['Path'] for item in read(fp)['Files']] + [item['TargetPath'] for item in rows]
stage += ['docs/validation/part-327/evidence-index.json', 'docs/validation/part-327/evidence-receipt.json']
(control / 'explicit-stage-files.json').write_bytes(encoded({'Files': list(dict.fromkeys(stage)), 'GitExecuted': False}))
print(encoded({'CopiedProofRows': len(rows), 'ActiveVolumeBytes': len(combined),
               'ReceiptSha256': sha((output / 'evidence-receipt.json').read_bytes())}).decode())
