from pathlib import Path
import hashlib
import importlib.util
import json
import re
import subprocess
import sys

sys.dont_write_bytecode = True
root = Path.cwd()
art = root / 'artifacts'
out = root / 'docs/validation/part-317'
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))
git = lambda *args: subprocess.check_output(['git', *args], cwd=root)
freeze_path = art / 'root-part317-main1/source-freeze.json'
freeze = read(freeze_path)
assert sha(freeze_path.read_bytes()) == sys.argv[1].upper()
assert git('rev-parse', 'HEAD').decode().strip() == freeze['ParentHead']
receipt = read(out / 'evidence-receipt.json')
index = read(out / 'evidence-index.json')
assert receipt['Files'] == index and receipt['SourceFreezeSha256'] == sha(freeze_path.read_bytes())
assert sha((out / 'evidence-index.json').read_bytes()) == receipt['EvidenceIndexSha256']
assert receipt['Passed'] == 6667 and receipt['Registry'] == 509 and receipt['RegistryDelta'] == 2
proofs = {entry['Destination'] for entry in index}
assert len(proofs) == len(index)
for entry in index:
    raw = (root / entry['Destination']).read_bytes()
    assert len(raw) == entry['Bytes'] and sha(raw) == entry['Sha256'], entry['Destination']
proofs |= {out.relative_to(root).as_posix() + '/' + name for name in ['evidence-index.json', 'evidence-receipt.json']}
assert proofs == {path.relative_to(root).as_posix() for path in out.rglob('*') if path.is_file()}
sources = {entry['Path'] for entry in freeze['Files']}
assert len(sources) == 38 and sum(path.endswith('.cs') for path in sources) == 33
for entry in freeze['Files']:
    raw = (root / entry['Path']).read_bytes()
    assert len(raw) == entry['Bytes'] and sha(raw) == entry['Sha256'], entry['Path']
metadata = {'AGENTS.md', 'CHANGELOG.md', 'docs/PROGRESS.md', 'docs/NEXT_STEPS.md', 'docs/COMPATIBILITY.md', 'docs/ERLANG_COMPATIBILITY.md', 'docs/DECISIONS.md', 'docs/OTP-PORTS.md', 'docs/changelog/0048.md', 'docs/module-readiness-scope.json', 'docs/module-readiness.json', 'docs/MODULE_READINESS.md', 'docs/supported-mfas.json'}
changed = set(git('diff', 'HEAD', '--name-only', '-z').decode().split('\0')) - {''}
assert changed <= sources | metadata | proofs, sorted(changed - sources - metadata - proofs)
assert not git('diff', '--cached', '--name-only'), 'Fresh explicit stage required'
assert sha((root / 'docs/changelog/0047.md').read_bytes()) == 'E4DDE5A0BA2BB6F8CDF6807391307E3AB708D9B8917D1A368108D91A3B27FF74'
active = (root / 'docs/changelog/0048.md').read_bytes()
assert len(active) <= 32768 and len(active.splitlines()) <= 400
seal_contracts = [
    ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json', '.github/workflows/nif-pending-native.yml', 'REVIEWED_SOURCE_PLAN_SHA256'),
    ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json', '.github/workflows/nif-reallocation-native.yml', 'REVIEWED_REALLOCATION_PLAN_SHA256')
]

def workflow_pin(raw, key):
    matches = re.findall(rb'(?m)^[ \t]+' + key.encode() + rb':[ \t]+([A-Fa-f0-9]{64})[ \t]*\r?$', raw)
    assert len(matches) == 1, key
    return matches[0].decode()

# Validate the cross-file seal before staging, not only normalized file rows.
for plan, workflow, key in seal_contracts:
    raw = (root / plan).read_bytes()
    assert b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf'), plan
    raw.decode('utf-8', errors='strict')
    assert sha(raw) == sha(raw.replace(b'\r\n', b'\n')) == workflow_pin((root / workflow).read_bytes(), key), plan
paths = sorted(sources | proofs | (metadata & changed))
assert all(not path.lower().startswith('license') and not path.startswith('artifacts/') for path in paths)
pathspec = art / 'root-part317-stage-pathspec'
assert not pathspec.exists()
pathspec.write_bytes(b'\0'.join(path.encode() for path in paths) + b'\0')
attempt = art / 'root-part317-stage-attempt.json'
assert not attempt.exists()
write = lambda path, value: path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')
write(attempt, {'Head': freeze['ParentHead'], 'Paths': len(paths), 'PathspecSha256': sha(pathspec.read_bytes()), 'SourceFreezeSha256': sha(freeze_path.read_bytes()), 'ScriptSha256': sha(Path(__file__).read_bytes()), 'GitAddAttemptRecordedBeforeExecution': True})
subprocess.run(['git', 'add', '--pathspec-from-file=' + str(pathspec), '--pathspec-file-nul'], check=True)
actual = set(git('diff', '--cached', '--name-only', '-z').decode().split('\0')) - {''}
assert actual == set(paths), (actual - set(paths), set(paths) - actual)
spec = importlib.util.spec_from_file_location('bulk_git', art / 'root-part312-packaging-exec1/bulk-git.py')
bulk = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bulk)
staged, attributes = bulk.capture(paths, root)
seal_records = []
plan_paths = [contract[0] for contract in seal_contracts]
eol_raw = subprocess.check_output(['git', 'check-attr', '--cached', '-z', 'eol', '--', *plan_paths], cwd=root).decode().split('\0')
assert eol_raw.pop() == '' and len(eol_raw) == 6
eol = {}
for offset in range(0, len(eol_raw), 3):
    name, key, value = eol_raw[offset:offset + 3]
    assert key == 'eol' and name not in eol
    eol[name] = value
for plan, workflow, key in seal_contracts:
    raw = (root / plan).read_bytes()
    blob = staged[plan]
    pin = workflow_pin(staged[workflow], key)
    assert eol[plan] == 'lf' and blob == raw and b'\r' not in blob, plan
    assert sha(raw) == sha(blob) == pin, plan
    seal_records.append({'Plan': plan, 'Workflow': workflow, 'EnvironmentKey': key, 'WorktreeSha256': sha(raw), 'ActualStagedBlobSha256': sha(blob), 'ActualStagedWorkflowPin': pin, 'GitEolAttribute': eol[plan], 'RawAndCanonicalAndPublishedBytesEqual': True})
plan_modes = {row['Path']: row['HashMode'] for row in read(root / 'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json')['Files']}
plan_modes['.github/workflows/differential.yml'] = 'TextLF' # Separate observation workflow, outside native compile-input plan.
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
    records.append({'Path': name, 'ComparisonMode': 'BinaryExact' if exact else 'TextLF', 'WorkingBytes': len(raw), 'WorkingSha256': sha(raw), 'StagedBytes': len(blob), 'StagedSha256': sha(blob), 'GitAttributes': attrs})
subprocess.run(['git', 'diff', '--cached', '--check'], check=True)
guard = {'Status': 'PASS_EXACT_PART317_STAGE_DEFAULT_WHITESPACE_AND_DECLARED_MODES', 'SourceFreezeSha256': sha(freeze_path.read_bytes()), 'StagedPaths': len(paths), 'SourcePaths': 38, 'CSharpPaths': 33, 'ProofPaths': len(proofs), 'ProofsBinaryExact': True, 'AllCSharpBinaryExact': True, 'DefaultGitWhitespaceCheckPassed': True, 'BlankAtEofExceptionUsed': False, 'LicenseLocalStaged': False, 'CacheStaged': False, 'Records': records}
guard['ActualStagedRawSourcePlanSeals'] = seal_records
target = art / 'root-part317-stage-index-guard.json'
assert not target.exists()
write(target, guard)
print(json.dumps({'StagePaths': len(paths), 'SourcePaths': 38, 'CSharpPaths': 33, 'ProofPaths': len(proofs), 'GuardSha256': sha(target.read_bytes())}))

