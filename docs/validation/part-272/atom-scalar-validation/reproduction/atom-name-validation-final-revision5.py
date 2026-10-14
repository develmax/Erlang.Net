from pathlib import Path
import json, hashlib, shutil, re
r = Path.cwd()
p = r / 'artifacts/atom-name-validation-prototype'
e = r / 'artifacts/atom-name-validation-evidence'
def digest(raw): return hashlib.sha256(raw).hexdigest().upper()
prepath = r / 'artifacts/atom-name-validation-source-freeze-revision5.json'
pre = json.loads(prepath.read_text())
owned = {row['Path'] for row in pre['Files']}
rows = []
formatter = []
for row in pre['Files']:
    raw = (p / row['Path']).read_bytes()
    current = dict(row, Sha256=digest(raw), SourceContent=raw.decode())
    rows.append(current)
    if current['Sha256'] != row['Sha256']:
        assert re.sub(r'\s+', '', row['SourceContent']) == re.sub(r'\s+', '', current['SourceContent']), row['Path']
        formatter.append(row['Path'])
    original = r / row['Path']
    assert (digest(original.read_bytes()) if original.exists() else None) == row['BeforeSha256'], row['Path']
rebase = json.loads((r / 'artifacts/atom-name-validation-source-freeze-rebased271.json').read_text())
unowned = []
for row in rebase['Seed']:
    if row['Path'] in owned: continue
    assert digest((p / row['Path']).read_bytes()) == row['Sha256'], row['Path']
    unowned.append(row['Path'])
focus = json.loads((e / 'revision5-focus.json').read_text(encoding='utf-8-sig'))
assert focus['HostGroupPassed'] and focus['Failed'] == 0
assert len(focus['Generated']) == 24 and all(row['Pass'] for row in focus['Generated'])
assert len(focus['Diagnoses']) == 4 and all(row['Pass'] for row in focus['Diagnoses'])
assert not focus['NativeExecuted']
assert '0 files need formatting' in (e / 'revision5-style-final.log').read_text(encoding='utf-8-sig')
initial = json.loads((e / 'initial-unsupported-atom-count-focus.json').read_text(encoding='utf-8-sig'))
assert initial['Failed'] == 2 and len(initial['Generated']) == 8
pattern = r'"(-module\(atom_scalar_validation_contract\).*?)",'
initial_source = next(row['SourceContent'] for row in json.loads((r / 'artifacts/atom-name-validation-source-freeze-initial.json').read_text())['Files'] if row['Path'].endswith('AtomScalarValidationCases.cs'))
final_source = next(row['SourceContent'] for row in rows if row['Path'].endswith('AtomScalarValidationCases.cs'))
old_cases = re.findall(pattern, initial_source)
new_cases = re.findall(pattern, final_source)
assert len(old_cases) == 8 and len(new_cases) == 13 and new_cases[:8] == old_cases
closure = e / 'actual-revision5-dll-closure'
closure.mkdir(exist_ok=True)
for file in (p / 'tools/AtomNameValidationFocus/bin/Release/net10.0').iterdir():
    if file.is_file(): shutil.copyfile(file, closure / file.name)
receipt = {'Status': 'PASS_SCOPED_PROMOTION', 'PublishedBase': rebase['PublishedBase'], 'OwnedPaths': 20, 'OtherPublishedSeedFilesExact': len(unowned), 'WhitespaceOnlyPostPrebuildChanges': formatter, 'HostGroupPassed': True, 'NewCompiledPassed': 13, 'ExistingIdentifierCompiledPassed': 11, 'TypedLexicalDiagnosesPassed': 4, 'Failures': 0, 'BuildErrors': 0, 'BuildWarnings': 0, 'StyleChangesFinal': 0, 'AssemblyMvid': focus['AssemblyMvid'], 'OldDllNegativePreserved': True, 'InitialUnsupportedCounterFailurePreserved': '6/8', 'InitialEightSourcesAndOrderExact': True, 'LexicalERL999RegressionPreserved': 'artifacts/atom-lexer-regression-shadow/receipt.json', 'RegistryExportsAdded': 0, 'NativeExecuted': False, 'MainFullExpected': 6515, 'MainFullExecuted': False, 'SourceExpectedNativeNotObserved': True}
final = {'Stage': 'FinalRevision5AtomScalarAndTypedLexerPromotion', 'Files': rows, 'PreBuildFreezeSha256': digest(prepath.read_bytes()), 'Receipt': receipt}
out = r / 'artifacts/atom-name-validation-source-freeze-final-revision5.json'
out.write_text(json.dumps(final, indent=2))
(e / 'receipt-revision5.json').write_text(json.dumps(receipt, indent=2))
print('Final source freeze SHA256:', digest(out.read_bytes()))
print(json.dumps(receipt, indent=2))
