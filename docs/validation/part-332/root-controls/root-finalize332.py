from pathlib import Path
import hashlib
import json

r = Path.cwd()
out = r / 'docs/validation/part-332'
assert not out.exists()
h = lambda b: hashlib.sha256(b).hexdigest().upper()
rows = []

def copy(source, target, expected=None):
    source, target = r / source, r / target
    assert source.resolve().is_relative_to(r) and target.resolve().is_relative_to(out)
    assert source.is_file() and not target.exists()
    data = source.read_bytes()
    if expected:
        assert len(data) == expected['Bytes'] and h(data) == expected['Sha256'], str(source)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(data)
    rows.append({'SourcePath': source.relative_to(r).as_posix(), 'TargetPath': target.relative_to(r).as_posix(), 'Bytes': len(data), 'Sha256': h(data)})

prior_file = r / 'artifacts/part331-five-terminal-lean-copy-rows.json'
assert h(prior_file.read_bytes()) == 'CF17163AD287F18E16E02B011938FE294875AB79B418F480D2C65132668F2E29'
prior = json.loads(prior_file.read_bytes())
assert len(prior) == 702
for row in prior:
    assert row['TargetPath'].startswith('docs/validation/part-332/prior331/')
    copy(row['SourcePath'], row['TargetPath'], row)

allowed = {'.json', '.cs', '.py', '.ps1', '.yml', '.erl', '.txt', '.md', '.log', '.stdout', '.stderr', '.diff'}
trees = {
    'proc-lib332-actual1': 'historical-focused1',
    'proc-lib332-actual2': 'focused',
    'proc-lib332-integrated-source2': 'reviewed-source',
    'proc-lib332-integrated-source2-review': 'source-review',
    'proc-lib332-full-source': 'final-candidate',
    'proc-lib332-full-source-independent': 'composition-review',
    'proc-lib332-package-suffix-independent': 'package-review',
    'proc-lib332-promotion-source-review': 'promotion-review',
    'proc-lib332-native-proposal3': 'native-driver',
    'proc-lib332-native-proposal3-peer': 'native-driver-review',
    'proc-lib332-native-actual': 'final-native',
    'proc-lib332-native-actual-passive-review': 'native-review',
}
for source_dir, category in trees.items():
    base = r / 'artifacts' / source_dir
    assert base.is_dir(), source_dir
    for p in sorted(base.rglob('*')):
        if not p.is_file() or any(x in {'bin', 'obj', '__pycache__'} for x in p.relative_to(base).parts):
            continue
        if p.suffix.lower() not in allowed:
            continue
        copy(p.relative_to(r), out.relative_to(r) / category / p.relative_to(base))
for filename in ['root-promote332.py', 'root-document332.py', 'root-finalize332.py', 'root-part332-promotion.json', 'root-part332-shared-source-review.json', 'proc-lib332-full.py', 'proc-lib332-package-resume.py']:
    copy(Path('artifacts') / filename, out.relative_to(r) / 'root-controls' / filename)
for name in ['MODULE_READINESS.md', 'module-readiness.json']:
    data = (r / 'artifacts/root-part332-readiness' / name).read_bytes()
    (r / 'docs' / name).write_bytes(data)
    copy(Path('artifacts/root-part332-readiness') / name, out.relative_to(r) / 'readiness' / name)
copy(Path('docs/module-readiness-scope.json'), out.relative_to(r) / 'readiness/module-readiness-scope.json')
copy(Path('docs/supported-mfas.json'), out.relative_to(r) / 'readiness/supported-mfas.json')
(out / '.gitattributes').write_bytes(b'* binary\n')
index = out / 'evidence-index.json'
index.write_bytes((json.dumps(rows, indent=2) + '\n').encode())
receipt = {
    'Status': 'LOCAL332_CUMULATIVE_VALIDATION_AND_ORIGINAL331_COMPLETE',
    'Passed': 6822, 'Failed': 0, 'IntegrationOutcomes': 11,
    'InitialLocalPipelineExit': 1, 'PackageSuffixExit': 0,
    'UninterruptedLocalFullPass': False, 'RegisteredMfas': 521,
    'ProcLibMfas': 12, 'ProcLibDeclarations': 39, 'ErlangMfas': 351,
    'SourceClosurePaths': 2431, 'OwnedPaths': 25,
    'PreviousOrderedTests': 6775, 'PreviousRegistryRows': 515,
    'CurrentOriginalExecuted': False, 'CurrentCiVerified': False,
    'Prior331Original': {'Passed': 5962, 'Failed': 0, 'DescriptorCases': 8, 'NewProcLibSources': 21, 'StrictOrdinaryVariance': 0},
    'CopiedFiles': len(rows), 'CopiedBytes': sum(x['Bytes'] for x in rows),
    'EvidenceIndexSha256': h(index.read_bytes()),
    'NoRuntimeOrSdkBinariesCopied': True,
}
(out / 'evidence-receipt.json').write_bytes((json.dumps(receipt, indent=2) + '\n').encode())
assert len((r / 'docs/changelog/0052.md').read_bytes()) < 32768
print(json.dumps(receipt))
