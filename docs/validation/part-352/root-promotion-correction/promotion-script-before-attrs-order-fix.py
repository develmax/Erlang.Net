from pathlib import Path
import hashlib, json, subprocess, sys

r = Path.cwd()
plan_path = r / sys.argv[1]
expected_plan = sys.argv[2]
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
load = lambda p: json.loads(p.read_bytes())
assert len(expected_plan) == 64 and sha(plan_path.read_bytes()) == expected_plan
out = r / 'artifacts/root-part352-promotion'
assert not out.exists()
head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
assert head == '547a2dd266586589a90c7883dac88d09a5a1de20'
subprocess.run(['git', 'diff', '--exit-code'], check=True, stdout=subprocess.DEVNULL)
subprocess.run(['git', 'diff', '--cached', '--exit-code'], check=True, stdout=subprocess.DEVNULL)
peer_path = r / 'artifacts/six-predicate-published351-independent/report.json'
assert sha(peer_path.read_bytes()) == '0D0959976B95EAA3CE8A5CB6FDDAF8FB521EE23CD0AE59A3ABCE3C633ED5BD3C'
peer = load(peer_path)
assert peer['Status'].startswith('PASS_INDEPENDENT_SIX_COMPLETE_SUPPORTED_UNARY_CONTRACT')
assert not peer['MissingWholeContractOrKnownCurrentSemanticGapFound']
assert load(r / 'artifacts/root-six352-readiness-candidate/guard.json')['Counts'] == [16,24,547]
plan = load(plan_path)
assert plan['ParentHead'] == head
files = {}
for row in plan['Files']:
    key = row['TargetPath']
    assert key not in files and key != 'LICENSE.local.txt'
    dst = r / key
    assert dst.resolve().is_relative_to(r.resolve())
    data = (r / row['SourcePath']).read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256']
    if row['ParentSha256'] is not None:
        assert sha(dst.read_bytes()) == row['ParentSha256']
    else:
        assert not dst.exists()
    files[key] = data
assert all(not name.startswith('src/') and not name.endswith('.cs') for name in files if not name.startswith('docs/validation/'))
assert {name for name in files if name.startswith('tools/')} == {'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'}
native_key = 'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'
old_native = load(r / native_key)
new_native = json.loads(files[native_key])
assert {k: v for k, v in old_native.items() if k != 'Files'} == {k: v for k, v in new_native.items() if k != 'Files'}
assert [a['Path'] for a, b in zip(old_native['Files'], new_native['Files']) if a != b] == ['.gitattributes']
assert len(new_native['Files']) == len(old_native['Files']) == 1287
for plan_key in [native_key, 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json']:
    native = json.loads(files[plan_key] if plan_key in files else (r / plan_key).read_bytes())
    for row in native['Files']:
        key = row['Path']
        data = files[key] if key in files else (r / key).read_bytes()
        if row.get('HashMode') == 'TextLF':
            data = data.replace(b'\r\n', b'\n')
        assert len(data) == row['Bytes'] and sha(data) == row['Sha256'], key
workflow_key = '.github/workflows/nif-reallocation-native.yml'
old_workflow = (r / workflow_key).read_bytes()
assert files[workflow_key] == old_workflow.replace(sha((r / native_key).read_bytes()).encode(), sha(files[native_key]).encode())
proposal = r / 'artifacts/six-predicate-published351-contract-proposal'
data = (proposal / 'ledger-candidate.json').read_bytes()
assert sha(data) == peer['CandidateLedgerSha256']
files['docs/mfa-compatibility.json'] = data
attrs = (r / '.gitattributes').read_bytes()
for line in ['docs/validation/part-352/** binary', 'docs/validation/complete-six-predicate-audit/** binary']:
    if line.encode() not in attrs:
        attrs = attrs.rstrip(b'\r\n') + b'\r\n\r\n' + line.encode() + b'\r\n'
files['.gitattributes'] = attrs
if not any(value == peer_path.read_bytes() for value in files.values()):
    files['docs/validation/part-352/independent-contract-review.json'] = peer_path.read_bytes()
changelog = files['docs/changelog/0055.md']
assert changelog.startswith((r / 'docs/changelog/0055.md').read_bytes())
assert len(changelog) <= 32768 and len(changelog.splitlines()) <= 400
protected = sha((r / 'LICENSE.local.txt').read_bytes())
out.mkdir()
for key, data in files.items():
    dst = r / key
    dst.parent.mkdir(parents=True, exist_ok=True)
    dst.write_bytes(data)
tool = r / 'artifacts/spawn-priority351-full-corrected-candidate2/tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll'
assert sha(tool.read_bytes()) == 'A3F059073C5E7C2D176520FB7CD78DB5D66C8BC8972F3FC079E77255D57B441E'
command = ['dotnet', str(tool), 'readiness', str(r / 'docs'), str(r / 'artifacts/spawn-priority351-full-actual2/tests.json'), str(out / 'generated')]
with (out / 'stdout.txt').open('xb') as stdout, (out / 'stderr.txt').open('xb') as stderr:
    result = subprocess.run(command, cwd=r, stdout=stdout, stderr=stderr, timeout=60)
(out / 'exit.json').write_text(json.dumps({'Command': command, 'ExitCode': result.returncode,
    'ToolSha256': sha(tool.read_bytes()), 'NewSdkBuild': False}, indent=2) + '\n', encoding='utf-8')
assert result.returncode == 0
v = load(out / 'generated/module-readiness.json')
assert (v['VerifiedCompatibleMfas'],v['KnownDifferenceMfas'],v['NeedsVerificationMfas'],v['LocalPassedTests'],v['RegisteredMfas']) == (16,24,547,7297,587)
for name in ['module-readiness.json', 'MODULE_READINESS.md', 'API_COMPATIBILITY.md']:
    data = (out / 'generated' / name).read_bytes()
    assert data == (r / 'artifacts/root-six352-readiness-candidate/generated' / name).read_bytes()
    if name.endswith('.md'):
        data = data.rstrip(b'\r\n') + b'\n'
    files['docs/' + name] = data
    (r / 'docs' / name).write_bytes(data)
for name in ['exit.json', 'stdout.txt', 'stderr.txt']:
    key = 'docs/validation/part-352/root-readiness/' + name
    data = (out / name).read_bytes()
    files[key] = data
    dst = r / key
    dst.parent.mkdir(parents=True, exist_ok=True)
    dst.write_bytes(data)
assert sha((r / 'LICENSE.local.txt').read_bytes()) == protected
assert not subprocess.check_output(['git', 'diff', '--name-only', '--', 'src', 'tools/*.cs', 'tools/**/*.cs'])
rows = [{'Path': key, 'Sha256': sha(data), 'Bytes': len(data)} for key, data in sorted(files.items())]
(out / 'promotion.json').write_text(json.dumps({'Status': 'ACTUAL352_SIX_CONTRACT_PROMOTION_AND_CURRENT_MAIN_READINESS_PASS',
    'Parent': head, 'PlanSha256': expected_plan, 'IndependentReportSha256': sha(peer_path.read_bytes()),
    'Counts': [16,24,547], 'Files': rows, 'RuntimeChanged': False, 'ProtectedLicenseUnchanged': True}, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Files': len(rows), 'Counts': [16,24,547], 'ActualReadinessExitCode': result.returncode, 'RuntimeChanged': False}))
