from pathlib import Path
import hashlib
import importlib.util
import json
import subprocess
import sys

sys.dont_write_bytecode = True
root = Path.cwd()
art = root / 'artifacts'
out = root / 'docs/validation/part-313'
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
read = lambda path: json.loads(path.read_bytes())
git = lambda *args: subprocess.check_output(['git', *args], cwd=root)
freeze_path = art / 'root-part313-main1/source-freeze.json'
freeze = read(freeze_path)
assert sha(freeze_path.read_bytes()) == '94ECE1FA74682375688E4FF65B174EC4BC993CB572D50372145FDF5FEB34507B'
assert git('rev-parse', 'HEAD').decode().strip() == freeze['ParentHead']
receipt = read(out / 'evidence-receipt.json')
index = read(out / 'evidence-index.json')
assert receipt['Files'] == index and receipt['SourceFreezeSha256'] == sha(freeze_path.read_bytes())
assert sha((out / 'evidence-index.json').read_bytes()) == receipt['EvidenceIndexSha256']
assert receipt['Passed'] == 6640 and receipt['Registry'] == 505 and receipt['RegistryDelta'] == 0
proofs = {entry['Destination'] for entry in index}
assert len(proofs) == len(index) == 425
for entry in index:
    raw = (root / entry['Destination']).read_bytes()
    assert len(raw) == entry['Bytes'] and sha(raw) == entry['Sha256'], entry['Destination']
proofs |= {out.relative_to(root).as_posix() + '/' + name for name in
           ['evidence-index.json', 'evidence-receipt.json', 'final-bound-config.json']}
assert proofs == {p.relative_to(root).as_posix() for p in out.rglob('*') if p.is_file()}
sources = {entry['Path'] for entry in freeze['Files']}
for entry in freeze['Files']:
    raw = (root / entry['Path']).read_bytes()
    assert len(raw) == entry['Bytes'] and sha(raw) == entry['Sha256'], entry['Path']
metadata = {'AGENTS.md', 'CHANGELOG.md', 'docs/PROGRESS.md', 'docs/NEXT_STEPS.md',
            'docs/COMPATIBILITY.md', 'docs/ERLANG_COMPATIBILITY.md', 'docs/DECISIONS.md',
            'docs/OTP-PORTS.md', 'docs/changelog/0047.md', 'docs/module-readiness-scope.json',
            'docs/module-readiness.json', 'docs/MODULE_READINESS.md', 'docs/supported-mfas.json'}
changed = set(git('diff', 'HEAD', '--name-only', '-z').decode().split('\0')) - {''}
assert changed <= sources | metadata | proofs, sorted(changed - sources - metadata - proofs)
assert git('diff', '--cached', '--name-only'), 'Existing exact stage required'
assert sha((root / 'docs/changelog/0046.md').read_bytes()) == 'CAF1B30E0592EF5502A0642631256E3B4F4A5AD8201D96BBB234DA6F2B163361'
active = (root / 'docs/changelog/0047.md').read_bytes()
assert len(active) <= 32768 and len(active.splitlines()) <= 400
paths = sorted(sources | proofs | (metadata & changed))
assert all(not name.lower().startswith('license') and not name.startswith('artifacts/') for name in paths)
pathspec = art / 'root-part313-stage-pathspec'
assert pathspec.read_bytes() == b'\0'.join(path.encode() for path in paths) + b'\0'
actual = set(git('diff', '--cached', '--name-only', '-z').decode().split('\0')) - {''}
assert actual == set(paths), (actual - set(paths), set(paths) - actual)
spec = importlib.util.spec_from_file_location('bulk_git', art / 'root-part312-packaging-exec1/bulk-git.py')
bulk = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bulk)
staged, attributes = bulk.capture(paths, root)
plan_modes = {e['Path']: e['HashMode'] for e in read(root / 'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json')['Files']}
plan_modes['.github/workflows/nif-reallocation-native.yml'] = 'TextLF'
plan_modes['tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'] = 'TextLF'
records = []
for name in paths:
    raw = (root / name).read_bytes()
    blob = staged[name]
    attrs = attributes[name]
    exact = name in proofs or name.endswith(('.cs', '.beam', '.etf')) or attrs['text'] == 'unset' or attrs['binary'] == 'set'
    if name in sources and not exact:
        assert plan_modes.get(name) == 'TextLF', name
    assert (blob == raw if exact else blob == raw.replace(b'\r\n', b'\n')), name
    records.append({'Path': name, 'ComparisonMode': 'BinaryExact' if exact else 'TextLF',
                    'WorkingBytes': len(raw), 'WorkingSha256': sha(raw),
                    'StagedBytes': len(blob), 'StagedSha256': sha(blob), 'GitAttributes': attrs})
subprocess.run(['git', 'diff', '--cached', '--check'], check=True)
assert len(sources) == 45 and sum(name.endswith('.cs') for name in sources) == 19
guard = {'Status': 'PASS_EXACT_PART313_STAGE_DEFAULT_WHITESPACE_AND_DECLARED_MODES',
         'SourceFreezeSha256': sha(freeze_path.read_bytes()), 'StagedPaths': len(paths),
         'SourcePaths': 45, 'CSharpPaths': 19, 'ProofPaths': len(proofs),
         'ProofsBinaryExact': True, 'AllCSharpBinaryExact': True,
         'DefaultGitWhitespaceCheckPassed': True, 'BlankAtEofExceptionUsed': False,
         'LicenseLocalStaged': False, 'CacheStaged': False, 'Records': records}
target = art / 'root-part313-stage-index-guard.json'
assert not target.exists()
target.write_text(json.dumps(guard, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'StagePaths': len(paths), 'SourcePaths': 45, 'CSharpPaths': 19,
                  'ProofPaths': len(proofs), 'GuardSha256': sha(target.read_bytes())}))
