from pathlib import Path
import hashlib, json

r = Path.cwd()
d = r / 'artifacts/root-part321-main1'
h = lambda b: hashlib.sha256(b).hexdigest().upper()
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
bind = lambda p: {'Path': p.relative_to(r).as_posix(), 'Bytes': p.stat().st_size, 'Sha256': h(p.read_bytes())}
target = d / 'first-full-failure-receipt.json'
assert not target.exists()
assert h((d / 'source-freeze.json').read_bytes()) == '872916DEFB7170CD988197A03EBB4B88859D682A7EFB42A784E7CEBEB1FF7D93'
assert read(d / 'full-exit.json')['ExitCode'] == 1
tests = read(d / 'first-full-results-snapshot/tests.json')
assert (tests['Passed'], tests['Failed']) == (6677, 1)
failed = [e for e in tests['Tests'] if e['Status'] != 'Passed']
assert len(failed) == 1 and failed[0]['Name'] == 'runtime/distribution-raw-signal-link-unlink-exit-owned-process-signals'
for e in read(d / 'source-resource-closure.json')['Files']:
    raw = (r / e['Path']).read_bytes()
    assert len(raw) == e['Bytes'] and h(raw) == e['Sha256']
assemblies = [r / n for n in ['tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll', 'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Runtime.dll', 'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll', 'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll', 'tools/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll', 'tools/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.Runtime.dll']]
receipt = {'Status': 'PRESERVED_ACTUAL_FIRST_MAIN321_FULL_FAILURE_6677_OF_6678', 'Passed': 6677, 'Failed': 1, 'FailedTests': failed, 'SourceFreeze': bind(d / 'source-freeze.json'), 'SourceClosure': bind(d / 'source-resource-closure.json'), 'SourceRowsRehashed': 2316, 'FullLog': bind(d / 'full-validation.log'), 'FullExit': bind(d / 'full-exit.json'), 'ImmutableTests': bind(d / 'first-full-results-snapshot/tests.json'), 'RawRegeneratedMfas': bind(d / 'first-full-results-snapshot/raw-supported-mfas.json'), 'CurrentPostFailureBuildAssets': [bind(p) for p in assemblies], 'IntegrationAndPackagesReached': False, 'FullNativeOrReadinessSuccessClaimed': False, 'CurrentRegeneratedManifestStillUnrepaired': True}
target.write_bytes((json.dumps(receipt, indent=2) + '\n').encode())
print(json.dumps({'ReceiptSha256': h(target.read_bytes()), 'FailurePreserved': True}))
