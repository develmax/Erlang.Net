"""Compare one real Windows NIF image in pinned OTP and both managed loaders."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def save(path, value):
    path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--provider-admission', type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--runtime-version', default='10.0.11')
    parser.add_argument('--check-golden', action='store_true')
    args = parser.parse_args()
    root, tool, out = args.repository.resolve(), Path(__file__).resolve().parent, args.output.resolve()
    assert os.name == 'nt' and tool == root/'tools/Native/Erlang.NifMapCreation501Observations'
    assert out.is_relative_to(root/'artifacts/native-map501') and not out.exists()
    plan_path = tool/'source-plan.json'
    assert sha(plan_path) == args.plan_sha256.upper()
    plan = json.loads(plan_path.read_bytes())
    assert plan['PinnedCommit'] == PIN and sha(tool/'build-native.py') == plan['BuildScriptSha256']
    if 'CollectorSha256' in plan:
        assert sha(__file__) == plan['CollectorSha256']
    helpers = root/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts'
    owned, inventory = load('owned_map501', helpers/'owned.py'), load('inventory_map501', helpers/'bounded_inventory.py')
    provider = json.loads(args.provider_admission.read_bytes())
    assert provider['SourceCommit'] == PIN and provider['ReleaseTag'] == 'OTP-29.1.1'
    installed = Path(provider['InstalledOtpRoot']).resolve()
    manifest = Path(provider['ExecutionManifestPath'])
    assert sha(manifest) == provider['ExecutionManifestSha256']
    expected = {row['Path'].replace('\\', '/'): row['Sha256'] for row in json.loads(manifest.read_bytes())}
    assert len(expected) == 6427 and inventory.inventory(installed) == expected
    out.mkdir(parents=True)
    failures, phases = [], {}
    env = dict(os.environ)
    env['ERL_CRASH_DUMP'] = 'NUL'
    for name in ['ERL_FLAGS', 'ERL_AFLAGS', 'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS', 'ERLC_EMULATOR']:
        env.pop(name, None)

    def phase(name, command, directory, cwd=None):
        directory.mkdir(exist_ok=True)
        try:
            receipt = owned.owned(command, directory, name, 180, env, str(cwd or directory))
            phases[name] = receipt
            return True
        except Exception as error:
            failures.append(name + ': ' + repr(error))
            return False

    try:
        prepared = phase('build', [sys.executable, '-B', str(tool/'build-native.py'), '--plan', str(plan_path),
            '--approved-plan-sha256', args.plan_sha256, '--provider-admission', str(args.provider_admission.resolve()),
            '--output', str(out/'build')], out/'preparation', root)
        if prepared:
            folder = out/'build'
            receipt_path = folder/'build-receipt.json'
            build = json.loads(receipt_path.read_bytes())
            assert build['Status'] == 'MAP_CREATION501_WINDOWS_BUILD_COMPLETE'
            assert build['CompileExitCode'] == build['LinkExitCode'] == 0
            assert build['ExpectedExports'] == ['nif_init'] and not build['ExecutionApproved']
            assert build['CompiledSourceSha256'] == plan['NativeSourceSha256']
            for row in build['Files']:
                assert Path(row['Path']).name == row['Path']
                assert sha(folder/row['Path']) == row['Sha256'] and (folder/row['Path']).stat().st_size == row['Bytes']
            library = folder/'map_creation501.dll'
            admission_path = out/'execution-admission.json'
            admission = dict(Status='PARENT_REVIEWED_MAP_CREATION501_ADMISSION', BuildReceiptPath=str(receipt_path),
                BuildReceiptSha256=sha(receipt_path), PlanPath=str(plan_path), PlanSha256=sha(plan_path),
                LibraryPath=str(library), LibrarySha256=sha(library),
                NativeSourcePath=str(root/plan['NativeSourcePath']), NativeSourceSha256=plan['NativeSourceSha256'],
                ErlangSourcePath=str(root/plan['ErlangSourcePath']), ErlangSourceSha256=plan['ErlangSourceSha256'])
            save(admission_path, admission)
            original = out/'original'
            original.mkdir()
            shutil.copyfile(root/plan['ErlangSourcePath'], original/'map_creation501.erl')
            stem = library.with_suffix('').as_posix()
            expression = ('{ok,map_creation501,_}=compile:file("map_creation501.erl",[return_errors,return_warnings]),'
                'ok=map_creation501:load("' + stem + '"),'
                'ok=prim_file:write_file("returned.etf",term_to_binary(map_creation501:run(),[{minor_version,2}])),halt(0).')
            phase('original', [str(installed/'bin/erl.exe'), '+S', '1:1', '+A', '1', '-noshell', '-noinput',
                '-boot', 'no_dot_erlang', '-pa', str(original), '-eval', expression], original)
            host = root/'tools/Native/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll'
            for route in ['ordinary', 'reviewed']:
                # Independent routes run even when original acquisition or the other route failed.
                phase(route, ['dotnet', 'exec', '--fx-version', args.runtime_version, str(host), '--map-creation501',
                    str(admission_path), sha(admission_path), str(out/route), route], out/(route+'-process'), root)
            if not failures:
                raw = (original/'returned.etf').read_bytes()
                for route in ['ordinary', 'reviewed']:
                    assert (out/route/'returned.etf').read_bytes() == raw, route
                    done = json.loads((out/route/'completion.json').read_bytes())
                    assert done['ReviewedPolicyInvocationCount'] == (1 if route == 'reviewed' else 0)
                    for key in ['EnvironmentBaselineRestored', 'BinaryOwnershipBaselineRestored', 'NativeModuleAbsentBeforeLoad',
                        'NativeModulePresentDuringInvocation', 'NativeModuleAbsentAfterRetirementAndRuntimeDisposal']:
                        assert done[key], (route, key)
                if args.check_golden:
                    assert sha(tool/'original-values.etf') == plan['OriginalGoldenSha256']
                    assert raw == (tool/'original-values.etf').read_bytes()
    except Exception as error:
        failures.append(repr(error))
    finally:
        after = inventory.inventory(installed) == expected
        if not after:
            failures.append('Original provider changed')
        save(out/'completion.json', dict(Passed=not failures, Pin=PIN, Platform='windows-x64',
            ProviderFiles=6427, ProviderBeforeEqual=True, ProviderAfterEqual=after,
            SourcePlanSha256=sha(plan_path), GoldenChecked=args.check_golden,
            WholeRawLoaderComparisons=2 if not failures else 0, NormalizedFields=0,
            NativeLibraryExecuted=bool(phases.get('original')), Phases=phases, Errors=failures,
            BroaderMapMutationIterationClaimed=False, LinuxNativeExecutionClaimed=False))
    if failures:
        raise RuntimeError('; '.join(failures))
    print(json.dumps({'Passed': True, 'WholeRawLoaderComparisons': 2, 'ProviderFiles': 6427}))


if __name__ == '__main__':
    main()
