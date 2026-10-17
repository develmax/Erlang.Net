from pathlib import Path
import hashlib, json, shutil, subprocess

r = Path.cwd()
p = r / 'artifacts/six-predicate-published351-contract-proposal'
o = r / 'artifacts/root-six352-readiness-candidate'
o.mkdir(exist_ok=False)
root = o / 'repository'
docs = root / 'docs'
docs.mkdir(parents=True)
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest().upper()
load = lambda path: json.loads(path.read_bytes())
ledger = load(p / 'ledger-candidate.json')
assert load(r / 'artifacts/root-six352-preflight.json')['Other581EntriesExact']
for name in ['otp-inventory.json', 'supported-mfas.json', 'module-readiness-scope.json']:
    shutil.copyfile(r / 'docs' / name, docs / name)
shutil.copyfile(r / 'reference-baseline.json', root / 'reference-baseline.json')
shutil.copyfile(p / 'ledger-candidate.json', docs / 'mfa-compatibility.json')
proof = {row['TargetPath']: r / row['SourcePath'] for row in load(p / 'proof-copy-manifest.json')['Files']}
for entry in ledger['Entries']:
    for ev in entry['Evidence']:
        key = 'docs/' + ev['Path']
        source = proof.get(key, r / key)
        dst = docs / ev['Path']
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, dst)
        assert sha(dst) == ev['Sha256']
for row in ledger['VerifiedSourceFiles']:
    dst = root / row['Path']
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(r / row['Path'], dst)
    assert sha(dst) == row['Sha256']
prior = load(docs / 'module-readiness-scope.json')['ComparisonTestReport']
dst = (docs / prior).resolve()
assert dst.is_relative_to(root.resolve())
dst.parent.mkdir(parents=True, exist_ok=True)
shutil.copyfile(r / 'docs' / prior, dst)
tool = r / 'artifacts/spawn-priority351-full-corrected-candidate2/tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll'
assert sha(tool) == 'A3F059073C5E7C2D176520FB7CD78DB5D66C8BC8972F3FC079E77255D57B441E'
tests = r / 'artifacts/spawn-priority351-full-actual2/tests.json'
assert sha(tests) == '0381A6247B5F400D2A75060B6D24EE82E82C7B01FFE93A8B92E28179B372FFA8'
command = ['dotnet', str(tool), 'readiness', str(docs), str(tests), str(o / 'generated')]
with (o / 'stdout.txt').open('xb') as stdout, (o / 'stderr.txt').open('xb') as stderr:
    result = subprocess.run(command, cwd=r, stdout=stdout, stderr=stderr, timeout=60)
receipt = {'Command': command, 'ExitCode': result.returncode, 'ToolSha256': sha(tool),
    'LedgerSha256': sha(docs / 'mfa-compatibility.json'), 'NewSdkBuild': False, 'MainChanged': False}
(o / 'exit.json').write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
assert result.returncode == 0
actual = load(o / 'generated/module-readiness.json')
assert (actual['VerifiedCompatibleMfas'], actual['KnownDifferenceMfas'], actual['NeedsVerificationMfas'], actual['LocalPassedTests'], actual['RegisteredMfas']) == (16,24,547,7297,587)
md = (o / 'generated/API_COMPATIBILITY.md').read_text(encoding='utf-8')
assert md.count('| 100% compatible |') == 16
assert md.count('| Known differences |') == 24
assert md.count('| Needs verification for 100% compatibility |') == 547
(o / 'guard.json').write_text(json.dumps({'Status': 'ACTUAL_TESTED_TOOL_CANDIDATE_GENERATION_PASS',
    'Counts': [16,24,547], 'InputFullTests': 7297, 'NewTestsOrSdkBuild': False,
    'MainChanged': False, 'Generated': [{'Path': f.name, 'Sha256': sha(f)} for f in sorted((o / 'generated').iterdir())]}, indent=2) + '\n', encoding='utf-8')
print(json.dumps(receipt))
