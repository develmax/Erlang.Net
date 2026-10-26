"""Original-only controlled dirty-NIF physical markers; no managed NIF admission."""
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
    parser.add_argument('--check-golden', action='store_true')
    args = parser.parse_args()
    root, tool, out = args.repository.resolve(), Path(__file__).resolve().parent, args.output.resolve()
    assert os.name == 'nt' and tool == root/'tools/Native/Erlang.ProcessRetirement502OriginalObservations'
    assert out.is_relative_to(root/'artifacts/process-retirement502') and not out.exists()
    plan_path = tool/'source-plan.json'
    assert sha(plan_path) == args.plan_sha256.upper()
    plan = json.loads(plan_path.read_bytes())
    assert plan['PinnedCommit'] == PIN
    assert sha(tool/'build-native.py') == plan['BuildScriptSha256'] and sha(__file__) == plan['CollectorSha256']
    for key in ['NativeSource', 'ErlangSource']:
        path = (root/plan[key+'Path']).resolve()
        assert path.is_relative_to(tool) and sha(path) == plan[key+'Sha256']
    assert sha(tool/'original-values.etf') == plan['OriginalGoldenSha256']
    assert sha(tool/'original-cleanup.etf') == plan['OriginalCleanupSha256']
    for row in plan['Dependencies']:
        dependency = (root/row['Path']).resolve()
        assert dependency.is_relative_to(root) and sha(dependency) == row['Sha256']
    helpers = root/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts'
    owned = load('owned_retirement502', helpers/'owned.py')
    inventory = load('inventory_retirement502', helpers/'bounded_inventory.py')
    decoder = load('decoder_retirement502', root/'tools/Distribution/Erlang.GuestConnectedSendObservations/passive_etf.py')
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
    env['PROBE_RUNTIME_ROOT'] = str(installed)
    for name in ['ERL_FLAGS', 'ERL_AFLAGS', 'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS', 'ERLC_EMULATOR']:
        env.pop(name, None)

    def phase(name, command, directory, timeout, cwd=None):
        directory.mkdir(exist_ok=True)
        try:
            receipt = owned.owned(command, directory, name, timeout, env, str(cwd or directory))
            phases[name] = receipt
            return True
        except Exception as error:
            failures.append(name + ': ' + repr(error))
            return False

    native_executed = False
    golden_checked = False
    whole_protocol_verified = False
    try:
        prepared = phase('build', [sys.executable, '-B', str(tool/'build-native.py'), '--plan', str(plan_path),
            '--approved-plan-sha256', args.plan_sha256, '--provider-admission', str(args.provider_admission.resolve()),
            '--output', str(out/'build')], out/'preparation', 180, root)
        if prepared:
            folder = out/'build'
            receipt_path = folder/'build-receipt.json'
            build = json.loads(receipt_path.read_bytes())
            assert build['Status'] == 'RETIREMENT_PROBE502_WINDOWS_BUILD_COMPLETE'
            assert build['PinnedCommit'] == PIN and build['PlanSha256'] == sha(plan_path)
            assert build['CompileExitCode'] == build['LinkExitCode'] == 0
            assert build['ExpectedExports'] == ['nif_init'] and not build['ExecutionApproved']
            assert build['CompiledSourceSha256'] == build['NativeSourceSha256'] == plan['NativeSourceSha256']
            assert build['CopiedErlangSourceSha256'] == build['ErlangSourceSha256'] == plan['ErlangSourceSha256']
            for row in build['Files']:
                assert Path(row['Path']).name == row['Path']
                assert sha(folder/row['Path']) == row['Sha256'] and (folder/row['Path']).stat().st_size == row['Bytes']
            library = folder/'retirement_probe502.dll'
            assert sha(library) == build['LibrarySha256']
            original = out/'original'
            original.mkdir()
            shutil.copyfile(root/plan['ErlangSourcePath'], original/'retirement_probe502.erl')
            assert sha(original/'retirement_probe502.erl') == plan['ErlangSourceSha256']
            env['PROBE_DLL_STEM'] = str(library.with_suffix(''))
            expression = ('W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
                'try Expected=filename:absname(os:getenv("PROBE_RUNTIME_ROOT")),Expected=filename:absname(code:root_dir()),'
                '8=erlang:system_info(wordsize),W("metadata.etf",{Expected,erlang:system_info(version),erlang:system_info(wordsize),erlang:system_info(system_architecture)}),'
                'C=compile:file("retirement_probe502.erl",[return_errors,return_warnings]),W("compile.etf",C),{ok,retirement_probe502,_}=C,'
                '{module,retirement_probe502}=code:load_abs("retirement_probe502"),ok=retirement_probe502:load(os:getenv("PROBE_DLL_STEM")),'
                'R=retirement_probe502:run(),W("returned.etf",R),'
                'D=code:delete(retirement_probe502),P=code:purge(retirement_probe502),L=code:is_loaded(retirement_probe502),'
                'W("cleanup.etf",{D,P,L}),true=D,false=P,false=L,halt(0) '
                'catch Class:Reason:Stack->W("raised.etf",{Class,Reason,Stack}),halt(1) end.')
            native_executed = phase('original', [str(installed/'bin/erl.exe'), '+S', '2:2', '+SDcpu', '1', '+SDio', '1', '+A', '1',
                '-noshell', '-noinput', '-boot', 'no_dot_erlang', '-pa', str(original), '-eval', expression], original, 30)
            if native_executed:
                atom = lambda value: ('atom', value)
                integer = lambda value: ('integer', value)
                tup = lambda *values: ('tuple', tuple(values))
                expected_protocol = tup(tup(atom('before_release'), tup(integer(1), integer(0), integer(-1)), atom('true'),
                    atom('false'), atom('false'), atom('undefined'), atom('killed'), tup(integer(1), integer(0), integer(-1))),
                    tup(atom('after_release'), tup(integer(1), integer(1), integer(0))))
                raw = (original/'returned.etf').read_bytes()
                assert decoder.decode_exact(raw) == expected_protocol
                cleanup = (original/'cleanup.etf').read_bytes()
                assert decoder.decode_exact(cleanup) == tup(atom('true'), atom('false'), atom('false'))
                metadata = decoder.decode_exact((original/'metadata.etf').read_bytes())
                assert metadata[0] == 'tuple' and len(metadata[1]) == 4 and metadata[1][2] == integer(8)
                whole_protocol_verified = True
                if args.check_golden:
                    assert raw == (tool/'original-values.etf').read_bytes()
                    assert cleanup == (tool/'original-cleanup.etf').read_bytes()
                    golden_checked = True
                assert sha(library) == build['LibrarySha256']
    except Exception as error:
        failures.append(repr(error))
    finally:
        after = inventory.inventory(installed) == expected
        if not after:
            failures.append('Original provider changed')
        save(out/'completion.json', dict(Passed=not failures, Pin=PIN, Platform='windows-x64',
            ProviderFiles=6427, ProviderBeforeEqual=True, ProviderAfterEqual=after,
            SourcePlanSha256=sha(plan_path), GoldenChecked=golden_checked,
            WholeRawProtocolVerified=whole_protocol_verified, NormalizedFields=0,
            NativeLibraryExecuted=native_executed, PhysicalMarkersFromActualCNotDirtyLookup=True,
            PreReleaseBodyEntered=1 if whole_protocol_verified else None,
            PreReleaseBodyDone=0 if whole_protocol_verified else None,
            PreReleaseNativeWaitResult=-1 if whole_protocol_verified else None,
            LogicalDeathBeforePhysicalCompletion=whole_protocol_verified,
            Phases=phases, Errors=failures, ManagedNativeAdmissionExecuted=False,
            LinuxNativeExecutionClaimed=False))
    if failures:
        raise RuntimeError('; '.join(failures))
    print(json.dumps({'Passed': True, 'WholeRawProtocolVerified': True, 'GoldenChecked': golden_checked, 'ProviderFiles': 6427}))


if __name__ == '__main__':
    main()
