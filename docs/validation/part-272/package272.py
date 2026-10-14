from pathlib import Path
import json, hashlib, shutil
r = Path.cwd(); a = r / 'artifacts'; d = r / 'docs/validation/part-272'
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest().upper()
tests = json.loads((a / 'tests.json').read_bytes())
assert tests['Passed'] == 6515 and tests['Failed'] == 0
assert 'Validation passed' in (a / 'part272-main-full.log').read_text(encoding='utf-8-sig')
assert not d.exists(), 'Never replace an earlier archive'
native = a / 'part272-native271-and-packet-correction-staging'
manifest = json.loads((native / 'receipt.json').read_bytes())
for row in manifest['Files']:
    source = native / row['Path']
    assert sha(source) == row['Sha256']
    if row['Path'].startswith('docs/'):
        target = r / row['Path']; target.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(source, target)
atom = a / 'part272-atom-evidence-staging/docs/validation/part-272/atom-scalar-validation'
manifest = json.loads((atom / 'package-manifest.json').read_bytes())
for row in manifest['Files']:
    assert sha(atom / row['Path']) == row['Sha256'], row['Path']
shutil.copytree(atom, d / 'atom-scalar-validation')
for name in ['tests.json', 'integration-results.json', 'part272-main-full.log', 'part272-main-import-guard.json', 'import272.py', 'guard272-names.py', 'part272-test-name-preservation-guard.json', 'part272-halt-postfull-guard.ps1', 'part272-halt-postfull-guard.json', 'part272-halt-postfull-guard.log', 'part272-main-packet-controls.ps1', 'part272-main-packet-controls.py', 'part272-main-packet-controls.log', 'part272-main-packet-target.log', 'reference-checkpoint272.ps1', 'reference-checkpoint272-source-guard.py', 'publish-checkpoint272.ps1']:
    shutil.copyfile(a / name, d / name)
shutil.copytree(a / 'part272-main-packet-controls', d / 'actual-main-packet-controls')
halt = json.loads((a / 'part272-halt-postfull-guard.json').read_text(encoding='utf-8-sig'))
assert halt['TotalSdkChildExecutions'] == 59
for index, row in enumerate(halt['RawProof']):
    origin = Path(row['Directory']); target = d / 'raw-halt' / (str(index).zfill(3) + '-' + row['Name'] + '-backend' + str(row['Backend'])); target.mkdir(parents=True)
    for name in ['process-raw-evidence.json', 'process-stdout.raw', 'process-stderr.raw', 'mfa-result.json']:
        if (origin / name).exists(): shutil.copyfile(origin / name, target / name)
summary = 'Part272: Main6515/full and59 halt/package controls; all old6497 names/order preserved after13 compiled atom contracts, four typed lexical diagnostics and one HOST group. Strict Unicode scalar atom admission and255-scalar limit prevent invalid-name pool pollution; real atom_count query and pinned erl_scan illegal atom/var diagnostics. Twenty production/test paths plus five corrected packet-observer paths; actual Main collector target and12 pre-write refusal controls. Original271 packet setnode badarg/controller_timeout preserved independently from complete32 mixed-map captures and CI6497/full. Native mixed retained/rebuilt map divergence contradicts prior canonicalization hypothesis; preserve original behavior. Corrected pending18 packet and new ordinary5802/fd8 original executions pending publication. Registry493/erlang335 of35195.4% API presence only; whole module remains partial.'
for name in ['AGENTS.md', 'docs/PROGRESS.md']:
    p = r / name; text = p.read_text(encoding='utf-8-sig'); at = text.index('\n\n') + 2; p.write_bytes((text[:at] + summary + '\n\n' + text[at:]).encode())
p = r / 'docs/module-readiness-scope.json'; scope = json.loads(p.read_bytes()); scope['ComparisonTestReport'] = 'validation/part-272/external-part-271/part-271-validation/tests.json'
assert (r / 'docs' / scope['ComparisonTestReport']).is_file()
for component in scope['Components']:
    if component['Project'] in ['src/Erlang.Terms', 'src/Erlang.Compiler', 'src/Erlang.Runtime']: component['Implemented'].append(summary)
p.write_bytes((json.dumps(scope, indent=2, ensure_ascii=False) + '\n').encode())
entry = (a / 'part272-changelog-entry.md').read_text().rstrip() + '\n'
volume = r / 'docs/changelog/0040.md'; combined = volume.read_text().rstrip() + '\n\n' + entry
index = r / 'CHANGELOG.md'; text = index.read_text()
if len(combined.encode()) > 32768 or len(combined.splitlines()) > 400:
    volume = r / 'docs/changelog/0041.md'; assert not volume.exists(); combined = '# Work changelog — volume 0041\n\n' + entry
    text = text.replace('— parts268–271 (active)', '— parts268–271 (closed)') + '\n- [0041.md](docs/changelog/0041.md) — part272 (active)\n'
else: text = text.replace('— parts268–271 (active)', '— parts268–272 (active)')
volume.write_bytes(combined.encode()); index.write_bytes(text.encode())
p = r / '.gitattributes'; text = p.read_text().rstrip(); line = 'docs/validation/part-272/** binary'
if line not in text.splitlines(): text += '\n' + line
p.write_bytes((text + '\n').encode())
receipt = {'Status': 'PASS_ActualMainScopedEvidence', 'ActualMainPassed': 6515, 'Old6497NamesAndOrderExact': True, 'SourceImports': 25, 'HaltChildren': 59, 'Ci271Passed': 6497, 'Packet271Passed': 0, 'Packet271Failed': 1, 'Mixed271CaptureComplete': True, 'NewNativeExecuted': False, 'WholeModuleComplete': False}
receipt['Files'] = [{'Path': p.relative_to(d).as_posix(), 'Length': p.stat().st_size, 'Sha256': sha(p)} for p in sorted(d.rglob('*')) if p.is_file()]
(d / 'evidence-receipt.json').write_bytes((json.dumps(receipt, indent=2) + '\n').encode())
print('Packaged guarded source and raw evidence; regenerate readiness and stage explicitly next')
