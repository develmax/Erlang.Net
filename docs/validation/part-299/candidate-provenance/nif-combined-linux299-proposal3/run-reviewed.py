import argparse
import hashlib
import importlib.util
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
ORIGINAL_PLAN = 'EAC7F22640E3233268FD81EC22568A08666BAD5A190A81748DC1A49A1D5B20C0'
FIXTURE = '7B6AF591C635DED2FE686A9E8671ACD58F9C8A6A080D47C61420F844FF33F074'
COLLECTOR = '9B42C2B6FCF1A91B98FDA90D986C42044C40ABC98C020A3290C06F7DDD439C01'
ORIGINAL = 'tools/Erlang.NifPendingVarargsObservations/original'
TOOL = 'tools/Erlang.NifNativeBackend'
SUCCESS = [
    'Managed backend checks passed; native ABI and original OTP NOT executed.',
    'Internal trusted native fixture checks passed; general NIF ABI NOT implemented.',
    'Bounded native ABI cursor/pending checks passed; original OTP NOT executed by this harness.',
]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def record(path, base, mode='BinaryExact'):
    data = path.read_bytes()
    if mode == 'TextLF':
        data = data.replace(b'\r\n', b'\n')
    elif mode != 'BinaryExact':
        raise RuntimeError('Unknown explicit source hash mode.')
    return {'Path': str(path.relative_to(base)), 'Bytes': len(data), 'Sha256': hashlib.sha256(data).hexdigest().upper(), 'HashMode': mode}


def write(path, value):
    if path.exists():
        raise RuntimeError('Refusing to overwrite evidence: ' + str(path))
    path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')


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


def main():
    sys.dont_write_bytecode = True
    parser = argparse.ArgumentParser()
    parser.add_argument('phase', choices=['native-build', 'sdk'])
    parser.add_argument('--repository', type=pathlib.Path, required=True)
    parser.add_argument('--otp-source', type=pathlib.Path, required=True)
    parser.add_argument('--plan', type=pathlib.Path, required=True)
    parser.add_argument('--approved-plan-sha256', required=True)
    parser.add_argument('--output', type=pathlib.Path, required=True)
    parser.add_argument('--approved-build-receipt-sha256')
    args = parser.parse_args()
    if sys.platform != 'linux' or os.uname().machine != 'x86_64':
        raise RuntimeError('Reviewed backend requires Linux x86_64.')
    root, otp, output = args.repository.resolve(), args.otp_source.resolve(), args.output.resolve()
    head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
    if not os.environ.get('GITHUB_SHA') or head != os.environ['GITHUB_SHA']:
        raise RuntimeError('Exact workflow checkout SHA binding required.')
    if sha(args.plan) != args.approved_plan_sha256.upper():
        raise RuntimeError('Reviewed source plan hash mismatch.')
    plan = json.loads(args.plan.read_bytes())
    if plan['PinnedOtpCommit'] != PIN or plan['NativeOrSdkExecuted'] is not False:
        raise RuntimeError('Unexpected source plan scope.')
    if len(plan['Files']) != len({item['Path'] for item in plan['Files']}) or reviewed_inputs(root, args.plan) != {item['Path'] for item in plan['Files']}:
        raise RuntimeError('Reviewed source input path set changed; reseal/review before execution.')
    for item in plan['Files']:
        path = (root / item['Path']).resolve()
        if not path.is_relative_to(root) or record(path, root, item['HashMode']) != item:
            raise RuntimeError('Reviewed source bytes changed: ' + item['Path'])
    proposal = root / ORIGINAL
    if sha(proposal / 'source-plan.json') != ORIGINAL_PLAN or sha(proposal / 'collect.py') != COLLECTOR:
        raise RuntimeError('Frozen original collector/plan changed.')
    if sha(root / 'tools/Erlang.NifFixtureHost/native/fixture.c') != FIXTURE:
        raise RuntimeError('Trusted old SDK fixture changed.')
    spec = importlib.util.spec_from_file_location('reviewed_original_collector', proposal / 'collect.py')
    collector = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(collector)
    environment = dict(os.environ)
    for name in ('ERL_AFLAGS', 'ERL_FLAGS', 'ERL_ZFLAGS'):
        environment.pop(name, None)
    environment['ERLC_USE_SERVER'] = 'false'
    if subprocess.check_output(['git', '-C', str(otp), 'rev-parse', 'HEAD'], text=True).strip() != PIN:
        raise RuntimeError('OTP source head mismatch.')
    if subprocess.check_output(['git', '-C', str(otp), 'status', '--porcelain', '--untracked-files=no'], text=True).strip():
        raise RuntimeError('Tracked original OTP source changed.')
    include = otp / 'observer-install/lib/erlang/usr/include'
    headers = json.loads((proposal / 'source-plan.json').read_bytes())['Headers']
    for header in headers:
        actual = (include / header['Name']).read_bytes()
        original = subprocess.check_output(['git', '-C', str(otp), 'show', PIN + ':' + header['GitPath']])
        if actual != original or sha(include / header['Name']) != header['Sha256']:
            raise RuntimeError('Actual installed public header mismatch.')
    if not (include / 'erl_int_sizes_config.h').is_file():
        raise RuntimeError('Real configured OTP size header required.')
    if args.phase == 'native-build':
        if output.exists():
            raise RuntimeError('Native build requires fresh output.')
        output.mkdir(parents=True)
        def execute(command, name, timeout=90):
            return collector.execute(command, root, environment, output / name, timeout)
        tools = {name: pathlib.Path(shutil.which(name) or '') for name in ('gcc', 'readelf', 'nm', 'dotnet')}
        if any(not path.is_file() for path in tools.values()):
            raise RuntimeError('Existing gcc, readelf, nm and dotnet required; nothing is installed.')
        write(output / 'source-binding.json', {'Head': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip(), 'PlanSha256': sha(args.plan), 'Files': plan['Files'], 'ConfiguredHeaders': [record(path, include) for path in sorted(include.glob('*.h'))], 'Tools': {name: {'Path': str(path), 'Sha256': sha(path.resolve())} for name, path in tools.items()}})
        execute([str(tools['gcc']), '--version'], 'gcc-version')
        sdk = execute([str(tools['dotnet']), '--version'], 'dotnet-version')
        if sdk.stdout.strip() != b'10.0.400':
            raise RuntimeError('SDK version must equal committed global.json pin 10.0.400.')
        native = root / TOOL / 'native'
        flags = ['-shared', '-fPIC', '-O2', '-std=c11', '-Wall', '-Wextra', '-Werror', '-I', str(include)]
        execute([str(tools['gcc']), *flags, '-fvisibility=hidden', str(native / 'nif_bridge_exports.c'), str(native / 'nif_bridge_varargs.c'), '-o', str(output / 'nif_bridge.so')], 'compile-veneer')
        execute([str(tools['gcc']), *flags, '-fvisibility=hidden', str(native / 'nif_bridge_abi_probe.c'), '-o', str(output / 'nif_bridge_abi_probe.so')], 'compile-probe')
        execute([str(tools['gcc']), *flags, str(root / 'tools/Erlang.NifFixtureHost/native/fixture.c'), '-o', str(output / 'fixture.so')], 'compile-trusted-fixture')
        for library in ('fixture.so', 'nif_bridge.so', 'nif_bridge_abi_probe.so'):
            prefix = library.replace('.', '-')
            execute([str(tools['readelf']), '-h', '-Ws', '-d', str(output / library)], prefix + '-readelf')
            execute([str(tools['nm']), '-D', '--defined-only', str(output / library)], prefix + '-defined')
            execute([str(tools['nm']), '-D', '--undefined-only', str(output / library)], prefix + '-undefined')
        expected_exports = {
            'fixture.so': {'nif_init', 'memory_allocator_fixture_unload_count'},
            'nif_bridge.so': {'enif_priv_data', 'enif_alloc', 'enif_free', 'enif_get_int', 'enif_make_badarg', 'enif_make_int', 'enif_make_string', 'enif_is_exception', 'enif_has_pending_exception', 'enif_raise_exception', 'enif_make_tuple', 'enif_make_list', 'nif_bridge_register', 'nif_bridge_read_term'},
            'nif_bridge_abi_probe.so': {'nif_bridge_probe_tuple', 'nif_bridge_probe_list', 'nif_bridge_probe_empty_tuple', 'nif_bridge_probe_empty_list', 'nif_bridge_probe_pending'},
        }
        for library, expected in expected_exports.items():
            actual = {line.split()[-1] for line in (output / (library.replace('.', '-') + '-defined.stdout')).read_text().splitlines() if line.split()}
            if actual != expected:
                raise RuntimeError('Defined dynamic exports differ: ' + library)
        execute([str(tools['dotnet']), 'build', str(root / TOOL / 'Erlang.NifNativeBackend.csproj'), '-c', 'Release', '-o', str(output / 'managed'), '--nologo', '-m:1', '-nodeReuse:false'], 'build-sdk', 600)
        binaries = [output / name for name in ('fixture.so', 'nif_bridge.so', 'nif_bridge_abi_probe.so')]
        binaries += sorted(path for path in (output / 'managed').iterdir() if path.is_file())
        write(output / 'build-receipt.json', {'Status': 'BUILT_NOT_EXECUTED', 'PlanSha256': sha(args.plan), 'OriginalCompared': False, 'Files': [record(path, output) for path in binaries]})
    else:
        receipt = output / 'build-receipt.json'
        if not args.approved_build_receipt_sha256 or sha(receipt) != args.approved_build_receipt_sha256.upper():
            raise RuntimeError('Separate recorded build receipt hash required before native SDK execution.')
        receipt_data = json.loads(receipt.read_bytes())
        if receipt_data['PlanSha256'] != sha(args.plan):
            raise RuntimeError('Build source binding changed.')
        for item in receipt_data['Files']:
            path = (output / item['Path']).resolve()
            if not path.is_relative_to(output) or record(path, output) != item:
                raise RuntimeError('Recorded built artifact changed.')
        prefix = output / 'sdk-six-argument'
        if any(prefix.with_suffix(suffix).exists() for suffix in ('.stdout', '.stderr', '.process.json')):
            raise RuntimeError('SDK execution requires fresh evidence names.')
        arguments = ['dotnet', str(output / 'managed/Erlang.NifNativeBackend.dll')]
        for name in ('fixture.so', 'nif_bridge.so', 'nif_bridge_abi_probe.so'):
            library = output / name
            arguments.extend([str(library), sha(library)])
        result = collector.execute(arguments, root, environment, prefix, 120)
        lines = result.stdout.decode('utf-8').splitlines()
        route_pass = 'Trusted native generated-source entry routes passed; BEAM/old-image/general ABI NOT established.'
        hash_prefix = 'Native generated source SHA256: '
        if len(lines) != 5 or lines[:2] != SUCCESS[:2] or lines[3:] != [route_pass, SUCCESS[2]] or not lines[2].startswith(hash_prefix) or not re.fullmatch('[0-9A-F]{64}', lines[2][len(hash_prefix):]) or result.stderr:
            raise RuntimeError('SDK six-argument success contract differs; raw evidence retained.')
        write(output / 'sdk-receipt.json', {'Status': 'BOUNDED_TRUSTED_SDK_NATIVE_AND_COMPILED_SOURCE_CHECKS_PASS', 'BuildReceiptSha256': sha(receipt), 'GeneratedCSharpSha256': lines[2][len(hash_prefix):], 'GeneratedCSharpHashIndependentlyCompared': False, 'Original13Compared': False, 'GeneralNifAbiImplemented': False, 'Capture': [record(prefix.with_suffix(suffix), output) for suffix in ('.stdout', '.stderr', '.process.json')]})


if __name__ == '__main__':
    main()
