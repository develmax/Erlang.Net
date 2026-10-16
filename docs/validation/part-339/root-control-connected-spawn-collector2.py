from pathlib import Path
from types import SimpleNamespace
import hashlib
import importlib.util
import json
import shutil
import subprocess
import sys

sys.dont_write_bytecode = True
r = Path.cwd()
a = r / 'artifacts'
proposal = a / 'connected-spawn-original-collector-proposal2'
out = a / 'root-connected-spawn-collector2-controls'
assert not out.exists()
out.mkdir()
mirror = out / 'mirror'
mirror.mkdir()
j = lambda p: json.loads(p.read_bytes())
freeze = j(proposal / 'source-freeze.json')
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
assert sha((proposal / 'source-freeze.json').read_bytes()) == '2080B5FDAE3F93B731B97779AE13BDF33983685CBB0EE85ABDE5AEB05F0997A9'
for row in freeze['Files']:
    src = proposal / 'source' / row['Path']
    assert len(src.read_bytes()) == row['Bytes'] and sha(src.read_bytes()) == row['Sha256']
    target = mirror / row['Path']
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(src, target)
tool = mirror / 'tools/Erlang.ConnectedSpawnOriginalObservations'
manifest = j(tool / 'source-manifest.json')
for row in manifest['ExternalDependencies']:
    raw = (r / row['Path']).read_bytes()
    assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256']
    target = mirror / row['Path']
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(raw)
spec = importlib.util.spec_from_file_location('root_collector_controls', tool / 'collect-original.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
head = subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip()
args = SimpleNamespace(repository=mirror, manifest_sha256=freeze['SourceManifestSha256'], expected_head=head)
checks = []
m.verify(args)
assert len(m.CASES) == len(set(m.CASES)) == 12
checks.append('exact-seven-file-source-and-external-dependency-closure')

def refused(name, action):
    try:
        action()
    except (ValueError, OSError):
        checks.append(name)
    else:
        raise AssertionError('Expected refusal: ' + name)

refused('wrong-manifest-digest', lambda: m.verify(SimpleNamespace(repository=mirror, manifest_sha256='0' * 64, expected_head=head)))
refused('wrong-checkout-head', lambda: m.verify(SimpleNamespace(repository=mirror, manifest_sha256=args.manifest_sha256, expected_head='0' * 40)))
for label, target in [('workflow-drift', mirror / manifest['Workflow']['Path']), ('external-dependency-drift', mirror / manifest['ExternalDependencies'][0]['Path']), ('fixture-source-drift', tool / 'connected_spawn_probe.erl')]:
    raw = target.read_bytes()
    target.write_bytes(raw + b'\n')
    refused(label, lambda: m.verify(args))
    target.write_bytes(raw)
extra = tool / 'unexpected.bin'
extra.write_bytes(b'extra')
refused('undeclared-tool-file', lambda: m.verify(args))
extra.unlink()
for label, name in [('relative-traversal', '../outside'), ('absolute-source', '/outside'), ('windows-drive-source', 'C:/outside'), ('windows-separator-source', 'folder\\outside')]:
    refused(label, lambda name=name: m.safe_file(tool, name))
m.verify(args)
checks.append('source-restored-exactly-after-all-controls')
report = dict(Status='PASS_ACTUAL_PASSIVE_COLLECTOR_SOURCE_CONTROLS', Controls=len(checks), Checks=checks,
              SourceFreezeSha256=sha((proposal / 'source-freeze.json').read_bytes()),
              SourceManifestSha256=args.manifest_sha256, SourceMirrorOnly=True, DeclaredScenarios=12,
              SdkExecuted=False, ErlangCompilerOrVmExecuted=False, NetworkExecuted=False, MainEdited=False)
(out / 'guard.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report))
