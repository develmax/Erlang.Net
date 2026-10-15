import argparse
import hashlib
import json
import pathlib
import subprocess

def reviewed_inputs(root, plan_path):
    paths = set(subprocess.check_output(['git', 'ls-files', '-z', '--', 'src', 'global.json', 'Directory.*', 'NuGet.Config', 'nuget.config'], cwd=root).decode().split('\0')) - {''}
    paths = {path for path in paths if pathlib.Path(path).suffix in ('.cs', '.csproj', '.props', '.targets', '.json', '.Config', '.config')}
    paths.update(path.name for pattern in ('Directory.*', 'global.json', 'NuGet.Config', 'nuget.config') for path in root.glob(pattern) if path.is_file() and path.suffix in ('.props', '.targets', '.json', '.Config', '.config'))
    for directory in ('src', 'tools/Erlang.NifNativeBackend', 'tools/Erlang.NifPendingVarargsObservations'):
        paths.update(str(path.relative_to(root)).replace('\\', '/') for path in (root / directory).rglob('*') if path.is_file() and not {'bin', 'obj', '__pycache__'}.intersection(path.parts) and path.resolve() != plan_path.resolve() and (directory != 'src' or path.suffix in ('.cs', '.csproj', '.props', '.targets')))
    paths.add('tools/Erlang.NifFixtureHost/native/fixture.c')
    paths.add('tools/Erlang.BeamFixtures/verify-metadata-mutations.py')
    for name in ('GeneratedModuleCompiler.cs', 'GeneratedModuleArtifact.cs', 'GeneratedModuleReferences.cs', 'GeneratedModuleMetadata.cs', 'OracleDiagnostics.cs'):
        paths.add('tools/Erlang.Differential/' + name)
    return paths


parser = argparse.ArgumentParser(description='Source-only sealing after final reviewed source promotion; does not execute native code.')
parser.add_argument('--repository', type=pathlib.Path, required=True)
parser.add_argument('--output', type=pathlib.Path, required=True)
args = parser.parse_args()
root = args.repository.resolve()
if args.output.exists():
    raise RuntimeError('Fresh seal path required.')
paths = reviewed_inputs(root, args.output)
if not any(path.startswith('tools/Erlang.NifNativeBackend/') for path in paths):
    raise RuntimeError('Final native backend must be promoted before source sealing.')
records = []
for name in sorted(paths):
    raw = '/original/' in name or '/native/' in name or name == 'tools/Erlang.BeamFixtures/verify-metadata-mutations.py'
    data = (root / name).read_bytes()
    mode = 'BinaryExact' if raw else 'TextLF'
    if not raw:
        data = data.replace(b'\r\n', b'\n')
    records.append({'Path': name, 'Bytes': len(data), 'Sha256': hashlib.sha256(data).hexdigest().upper(), 'HashMode': mode})
receipt = {'Status': 'REVIEWED_SOURCE_ONLY_NATIVE_NOT_EXECUTED', 'PinnedOtpCommit': 'ad05823719d77c8faee87348ea39513d4e2f99c5', 'NativeOrSdkExecuted': False, 'Files': records}
args.output.write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
print(hashlib.sha256(args.output.read_bytes()).hexdigest().upper())
