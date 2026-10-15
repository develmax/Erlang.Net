import argparse
import json
import os
import pathlib
import shutil
import sys
from reviewed_common import bound_helpers, capture, clean_environment, exact_head, files_receipt, sha, source_record, verified_plan, verify_otp, write_fresh

sys.dont_write_bytecode = True


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('phase', choices=['native-build', 'sdk'])
    for name in ('repository', 'otp-source', 'otp-include', 'plan', 'output'):
        parser.add_argument('--' + name, type=pathlib.Path, required=True)
    for name in ('expected-head', 'approved-plan-sha256'):
        parser.add_argument('--' + name, required=True)
    parser.add_argument('--approved-build-receipt-sha256')
    args = parser.parse_args()
    if sys.platform != 'linux' or os.uname().machine != 'x86_64':
        raise RuntimeError('This reviewed build/SDK runner is Linux x64 only')
    root, otp, include, output = [path.resolve() for path in (args.repository, args.otp_source, args.otp_include, args.output)]
    plan = verified_plan(root, args.plan.resolve(), args.approved_plan_sha256)
    head = exact_head(root, args.expected_head)
    verify_otp(otp, include, plan)
    helper = bound_helpers(root, plan)['bounded_process']
    env = clean_environment()
    env['DOTNET_PROCESSOR_COUNT'] = '2'
    env['DOTNET_CLI_USE_MSBUILD_SERVER'] = '0'
    env['MSBUILDDISABLENODEREUSE'] = '1'
    if args.phase == 'native-build':
        if output.exists():
            raise RuntimeError('Fresh native build output required')
        output.mkdir(parents=True)
        def run(command, name, timeout=90):
            return capture(helper, command, root, env, output / name.replace('.', '-'), timeout)
        gcc, nm, readelf, dotnet = [shutil.which(name) for name in ('gcc', 'nm', 'readelf', 'dotnet')]
        if not all((gcc, nm, readelf, dotnet)):
            raise RuntimeError('Existing gcc/nm/readelf/dotnet required; nothing installed')
        if run([dotnet, '--version'], 'sdk-version').stdout.strip().decode() != plan['DotNetSdk']:
            raise RuntimeError('Pinned SDK required')
        run([gcc, '--version'], 'compiler-version')
        native = root / 'tools/Erlang.NifReallocationObservations/native'
        old = root / 'tools/Erlang.NifNativeBackend/native'
        flags = ['-shared', '-fPIC', '-O2', '-std=c11', '-Wall', '-Wextra', '-Werror', '-I', str(include)]
        recipes = {
            'reallocation_fixture.so': [native / 'reallocation_fixture.c'],
            'reallocation_extension.so': [native / 'nif_reallocation_bridge.c'],
            'legacy_fixture.so': [root / 'tools/Erlang.NifFixtureHost/native/fixture.c'],
            'legacy_bridge.so': [old / 'nif_bridge_exports.c', old / 'nif_bridge_varargs.c']
        }
        for name, sources in recipes.items():
            run([gcc, *flags, '-fvisibility=hidden', *sources, '-o', output / name], name + '-compile')
            definitions = run([nm, '-D', '--defined-only', output / name], name + '-defined')
            if {line.split()[-1] for line in definitions.stdout.decode().splitlines() if line.split()} != set(plan['NativeExports'][name]):
                raise RuntimeError('Exact native exports differ: ' + name)
            imports = run([nm, '-D', '--undefined-only', output / name], name + '-undefined')
            actual_imports = {line.split()[-1].split('@')[0] for line in imports.stdout.decode().splitlines() if line.split()}
            if not actual_imports.issubset(set(plan['NativeImports'][name])):
                raise RuntimeError('Unexpected native imports: ' + name)
            run([readelf, '-h', '-Ws', '-d', output / name], name + '-readelf')
        run([dotnet, 'build', root / 'tools/Erlang.NifNativeBackend/Erlang.NifNativeBackend.csproj', '-c', 'Release', '-o', output / 'managed', '--nologo', '-m:2', '-nodeReuse:false', '-p:UseSharedCompilation=false', '-p:RestoreDisableParallel=true'], 'build-managed', 600)
        # Source rehash after compiler processes; only the reviewed source set is accepted.
        for item in plan['Files']:
            if source_record(root / item['Path'], root, item['HashMode']) != item:
                raise RuntimeError('Source changed during build')
        write_fresh(output / 'build-receipt.json', {'Status': 'BUILT_NOT_EXECUTED', 'Head': head,
            'PlanSha256': sha(args.plan.read_bytes()), 'SdkNativeExecuted': False, 'Files': files_receipt(output)})
    else:
        receipt = output / 'build-receipt.json'
        if not args.approved_build_receipt_sha256 or sha(receipt.read_bytes()) != args.approved_build_receipt_sha256.upper():
            raise RuntimeError('Independent approval of exact built binary receipt required')
        build = json.loads(receipt.read_bytes())
        if build['Head'] != head or build['PlanSha256'] != sha(args.plan.read_bytes()) or build['SdkNativeExecuted'] is not False:
            raise RuntimeError('Build provenance mismatch')
        for item in build['Files']:
            path = (output / item['Path']).resolve()
            if not path.is_relative_to(output) or path.stat().st_size != item['Bytes'] or sha(path.read_bytes()) != item['Sha256']:
                raise RuntimeError('Reviewed built artifact changed')
        if (output / 'sdk.stdout').exists() or (output / 'sdk-receipt.json').exists():
            raise RuntimeError('SDK native execution is once-only per fresh build evidence')
        fixture, legacy, bridge, extension = [output / name for name in ('reallocation_fixture.so', 'legacy_fixture.so', 'legacy_bridge.so', 'reallocation_extension.so')]
        dotnet = shutil.which('dotnet')
        if not dotnet:
            raise RuntimeError('Existing dotnet required')
        command = [dotnet, output / 'managed/Erlang.NifNativeBackend.dll', '--reallocation-probe', fixture, sha(fixture.read_bytes()), legacy, sha(legacy.read_bytes()), bridge, sha(bridge.read_bytes()), extension, sha(extension.read_bytes())]
        write_fresh(output / 'sdk-attempt.json', {'Head': head, 'BuildReceiptSha256': sha(receipt.read_bytes()), 'Arguments': [str(arg) for arg in command], 'AutomaticRetryAllowed': False})
        capture(helper, command, root, env, output / 'sdk', 90)
        write_fresh(output / 'sdk-receipt.json', {'Complete': True, 'Head': head, 'PlanSha256': sha(args.plan.read_bytes()), 'BuildReceiptSha256': sha(receipt.read_bytes()), 'OriginalCompared': False, 'Files': files_receipt(output)})


if __name__ == '__main__':
    main()
