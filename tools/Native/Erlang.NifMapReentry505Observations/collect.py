"""Acquire and compare the pinned native flat-map reentry protocol using a real C fixture."""
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
    path.write_text(json.dumps(value, indent=2, default=lambda value: {'BinaryHex': value.hex()} if isinstance(value, bytes) else str(value)) + '\n', encoding='utf-8')


def dependencies(root, plan, original_only=False):
    verified, failures = [], []
    for row in plan['Dependencies']:
        if original_only and row.get('When') == 'managed':
            verified.append(dict(Path=row['Path'], RequiredInOriginalOnly=False, Verified=False))
            continue
        try:
            path = (root / row['Path']).resolve()
            if not path.is_relative_to(root):
                raise RuntimeError('Dependency escapes repository: ' + row['Path'])
            raw = path.read_bytes()
            mode = row['HashMode']
            if mode == 'Raw':
                planned = raw
            elif mode == 'TextLF':
                planned = raw.replace(b'\r\n', b'\n')
            else:
                raise RuntimeError('Unknown declared dependency hash mode: ' + mode)
            actual = hashlib.sha256(planned).hexdigest().upper()
            if actual != row['Sha256']:
                raise RuntimeError('Dependency hash mismatch: ' + row['Path'] + ' mode=' + mode +
                    ' expected=' + row['Sha256'] + ' actual=' + actual + ' raw=' + sha(path))
            verified.append(dict(Path=row['Path'], HashMode=mode, PlannedSha256=actual,
                ActualRawSha256=sha(path), ActualRawBytes=len(raw), Verified=True))
        except Exception as error:
            failures.append(repr(error))
    if failures:
        raise RuntimeError('Independent dependency checks failed: ' + '; '.join(failures))
    return verified


def managed_preflight(root, original_only):
    if original_only:
        return []
    host = root/'tools/Native/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll'
    required = [host, host.with_suffix('.deps.json'), host.with_suffix('.runtimeconfig.json')]
    failures = ['Missing managed host dependency: ' + str(path) for path in required if not path.is_file()]
    if not shutil.which('dotnet'):
        failures.append('dotnet executable is absent')
    program = root/'tools/Native/Erlang.NifNativeBackend/Program.cs'
    if not program.is_file() or 'MapReentry505CallbackEntry' not in program.read_text(encoding='utf-8-sig'):
        failures.append('Managed CLI has no MapReentry505 dispatch')
    if failures:
        raise RuntimeError('; '.join(failures))
    return [dict(Path=str(path), Sha256=sha(path), Bytes=path.stat().st_size)
        for path in sorted(host.parent.iterdir()) if path.is_file() and path.suffix in ('.dll', '.json')]


def compare_default_profile(decoder, original, managed):
    assert original == managed, 'Whole default reentry protocol differs'
    return dict(WholeRawBytesEqual=True, PostCaptureEtfNormalizationPerformed=False)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--provider-admission', type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--runtime-version', default='10.0.11')
    parser.add_argument('--check-golden', action='store_true')
    parser.add_argument('--original-only', action='store_true')
    args = parser.parse_args()
    root, tool, out = args.repository.resolve(), Path(__file__).resolve().parent, args.output.resolve()
    assert os.name == 'nt' and tool == root/'tools/Native/Erlang.NifMapReentry505Observations'
    assert out.is_relative_to(root/'artifacts/native-map505') and not out.exists()
    plan_path = tool/'source-plan.json'
    assert sha(plan_path) == args.plan_sha256.upper()
    plan = json.loads(plan_path.read_bytes())
    assert plan['PinnedCommit'] == PIN and sha(tool/'build-native.py') == plan['BuildScriptSha256']
    assert sha(__file__) == plan['CollectorSha256']
    assert sha(tool/'seal.py') == plan['SealerSha256']
    preparation_failures = []
    for key in ['NativeSource', 'ErlangSource']:
        try:
            source = (root/plan[key+'Path']).resolve()
            assert source.is_relative_to(tool) and sha(source) == plan[key+'Sha256']
        except Exception as error:
            preparation_failures.append(key + ': ' + repr(error))
    assert plan['SerializationProfiles']['CanonicalOptions'] == ['deterministic', {'minor_version': 2}]
    assert plan['SerializationProfiles']['DefaultOptions'] == [{'minor_version': 2}]
    dependency_proof, host_proof = [], []
    try:
        dependency_proof = dependencies(root, plan, args.original_only)
    except Exception as error:
        preparation_failures.append(repr(error))
    try:
        host_proof = managed_preflight(root, args.original_only)
    except Exception as error:
        preparation_failures.append(repr(error))
    if args.check_golden:
        assert 'OriginalGoldenSha256' in plan and sha(tool/'original-values.etf') == plan['OriginalGoldenSha256']
    host = root/'tools/Native/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll'
    if preparation_failures:
        out.mkdir(parents=True)
        save(out/'preflight.json', dict(Passed=False, Errors=preparation_failures,
            NativeExecutionStarted=False, ManagedExecutionStarted=False))
        raise RuntimeError('Preflight failed before native execution: ' + '; '.join(preparation_failures))
    helpers = root/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts'
    owned, inventory = load('owned_reentry505', helpers/'owned.py'), load('inventory_reentry505', helpers/'bounded_inventory.py')
    decoder = load('decoder_reentry505', root/'tools/Native/Erlang.NifMapFamily504Observations/diagnostic_etf.py')
    provider = json.loads(args.provider_admission.read_bytes())
    assert provider['SourceCommit'] == PIN and provider['ReleaseTag'] == 'OTP-29.1.1'
    installed = Path(provider['InstalledOtpRoot']).resolve()
    manifest = Path(provider['ExecutionManifestPath'])
    assert sha(manifest) == provider['ExecutionManifestSha256']
    expected = {row['Path'].replace('\\', '/'): row['Sha256'] for row in json.loads(manifest.read_bytes())}
    assert len(expected) == 6427 and inventory.inventory(installed) == expected
    header_failures = []
    for row in plan['Headers']:
        try:
            name = row['OutputName']
            assert Path(name).name == name
            assert sha(installed/'usr/include'/name) == row['Sha256']
        except Exception as error:
            header_failures.append(row['OutputName'] + ': ' + repr(error))
    if header_failures:
        raise RuntimeError('Independent header checks failed before native execution: ' + '; '.join(header_failures))
    out.mkdir(parents=True)
    save(out/'dependency-proof.json', dependency_proof)
    save(out/'managed-preparation-proof.json', host_proof)
    failures, phases = [], {}
    golden_checked, matched, original_executed = False, 0, False
    original_bytes = None
    default_comparisons, cleanup_proofs = {}, {}
    env = dict(os.environ)
    env['ERL_CRASH_DUMP'] = 'NUL'
    env['MAP_REENTRY505_RUNTIME_ROOT'] = str(installed)
    for name in ['ERL_FLAGS', 'ERL_AFLAGS', 'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS', 'ERLC_EMULATOR']:
        env.pop(name, None)

    def phase(name, command, directory, cwd=None):
        try:
            directory.mkdir(exist_ok=True)
            receipt = owned.owned(command, directory, name, 180, env, str(cwd or directory))
            phases[name] = receipt
            return True
        except Exception as error:
            receipt_path = directory/(name+'.exit.json')
            if receipt_path.is_file():
                try:
                    phases[name] = json.loads(receipt_path.read_bytes())
                except Exception as receipt_error:
                    failures.append(name + ' unreadable owned receipt: ' + repr(receipt_error))
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
            assert build['Status'] == 'MAP_REENTRY505_WINDOWS_BUILD_COMPLETE'
            assert build['CompileExitCode'] == build['LinkExitCode'] == 0
            assert build['ExpectedExports'] == ['nif_init'] and not build['ExecutionApproved']
            assert build['CompiledSourceSha256'] == build['NativeSourceSha256'] == plan['NativeSourceSha256']
            assert build['CopiedErlangSourceSha256'] == build['ErlangSourceSha256'] == plan['ErlangSourceSha256']
            for row in build['Files']:
                assert Path(row['Path']).name == row['Path']
                assert sha(folder/row['Path']) == row['Sha256'] and (folder/row['Path']).stat().st_size == row['Bytes']
            library = folder/'map_reentry505.dll'
            assert sha(library) == build['LibrarySha256']
            admission_path = out/'execution-admission.json'
            admission = dict(Status='PARENT_REVIEWED_MAP_REENTRY505_ADMISSION', BuildReceiptPath=str(receipt_path),
                BuildReceiptSha256=sha(receipt_path), PlanPath=str(plan_path), PlanSha256=sha(plan_path),
                LibraryPath=str(library), LibrarySha256=sha(library),
                NativeSourcePath=str(root/plan['NativeSourcePath']), NativeSourceSha256=plan['NativeSourceSha256'],
                ErlangSourcePath=str(root/plan['ErlangSourcePath']), ErlangSourceSha256=plan['ErlangSourceSha256'])
            save(admission_path, admission)
            original = out/'original'
            original.mkdir()
            shutil.copyfile(root/plan['ErlangSourcePath'], original/'map_reentry505.erl')
            stem = library.with_suffix('').as_posix()
            expression = ('W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
                'Expected=filename:absname(os:getenv("MAP_REENTRY505_RUNTIME_ROOT")),Expected=filename:absname(code:root_dir()),'
                '8=erlang:system_info(wordsize),W("metadata.etf",{Expected,erlang:system_info(version),erlang:system_info(wordsize),erlang:system_info(system_architecture)}),'
                'C=compile:file("map_reentry505.erl",[return_errors,return_warnings]),W("compile.etf",C),{ok,map_reentry505,_}=C,'
                'ok=map_reentry505:load("' + stem + '"),'
                'T=map_reentry505:run(),W("returned-default.etf",T),'
                'ok=file:write_file("returned.etf",term_to_binary(T,[deterministic,{minor_version,2}])),'
                'D=code:delete(map_reentry505),P=code:purge(map_reentry505),L=code:is_loaded(map_reentry505),'
                'W("cleanup.etf",{D,P,L}),true=D,false=P,false=L,halt(0).')
            original_ok = phase('original', [str(installed/'bin/erl.exe'), '+S', '1:1', '+A', '1', '-noshell', '-noinput',
                '-boot', 'no_dot_erlang', '-pa', str(original), '-eval', expression], original)
            original_executed = original_ok
            raw = None
            if original_ok:
                try:
                    raw = (original/'returned.etf').read_bytes()
                    decoded = decoder.decode_exact(raw)
                    assert decoded[0] == 'tuple' and len(decoded[1]) == 2
                    save(original/'returned-readable.json', decoded)
                    default_raw = (original/'returned-default.etf').read_bytes()
                    default_decoded = decoder.decode_exact(default_raw)
                    assert default_decoded[0] == 'tuple' and len(default_decoded[1]) == 2
                    save(original/'returned-default-readable.json', default_decoded)
                    original_bytes = len(raw)
                    if args.check_golden:
                        assert raw == (tool/'original-values.etf').read_bytes()
                        golden_checked = True
                except Exception as error:
                    failures.append('original protocol validation: ' + repr(error))
                    raw = None
            if not args.original_only:
                # Routes are independent after successful C/managed-host preparation.
                for route in ['ordinary', 'reviewed']:
                    phase(route, ['dotnet', 'exec', '--fx-version', args.runtime_version, str(host), '--map-reentry505',
                        str(admission_path), sha(admission_path), str(out/route), route], out/(route+'-process'), root)
                for route in ['ordinary', 'reviewed']:
                    returned = out/route/'returned.etf'
                    try:
                        done = json.loads((out/route/'completion.json').read_bytes())
                        assert done['Status'] == 'NATIVE_IMAGE_RETIRED_RUNTIME_DISPOSED'
                        assert done['AdmissionSha256'] == sha(admission_path)
                        assert done['ReviewedPolicyInvocationCount'] == (1 if route == 'reviewed' else 0)
                        for key in ['EnvironmentBaselineRestored', 'BinaryOwnershipBaselineRestored', 'NativeModuleAbsentBeforeLoad',
                            'NativeModulePresentDuringInvocation', 'NativeModuleAbsentAfterRetirementAndRuntimeDisposal']:
                            assert done[key], (route, key)
                        cleanup_proofs[route] = done
                        assert done['ReturnedSha256'] == sha(returned)
                        decoded = decoder.decode_exact(returned.read_bytes())
                        assert decoded[0] == 'tuple' and len(decoded[1]) == 2
                        save(out/route/'returned-readable.json', decoded)
                    except Exception as error:
                        failures.append(route + ' independent physical/protocol proof: ' + repr(error))
                    if raw is not None:
                        try:
                            assert returned.read_bytes() == raw, route + ': whole deterministic ETF differs'
                            default_comparisons[route] = compare_default_profile(decoder,
                                (original/'returned-default.etf').read_bytes(), (out/route/'returned-default.etf').read_bytes())
                            assert route in cleanup_proofs
                            matched += 1
                        except Exception as error:
                            failures.append(route + ' whole original comparison: ' + repr(error))
    except Exception as error:
        failures.append(repr(error))
    finally:
        after = False
        try:
            after = inventory.inventory(installed) == expected
            if not after:
                failures.append('Original provider changed')
        except Exception as error:
            failures.append('Original provider post-check: ' + repr(error))
        try:
            assert sha(plan_path) == args.plan_sha256.upper()
            assert sha(tool/'build-native.py') == plan['BuildScriptSha256']
            assert sha(__file__) == plan['CollectorSha256']
            assert sha(tool/'seal.py') == plan['SealerSha256']
            for key in ['NativeSource', 'ErlangSource']:
                assert sha(root/plan[key+'Path']) == plan[key+'Sha256']
            dependencies(root, plan, args.original_only)
            for row in host_proof:
                assert sha(row['Path']) == row['Sha256']
        except Exception as error:
            failures.append('Source/managed host post-check: ' + repr(error))
        if not args.original_only and matched != 2:
            failures.append('Both complete loader/original comparisons were not established')
        save(out/'completion.json', dict(Passed=not failures, Pin=PIN, Platform='windows-x64',
            ProviderFiles=6427, ProviderBeforeEqual=True, ProviderAfterEqual=after,
            SourcePlanSha256=sha(plan_path), GoldenChecked=golden_checked,
            OriginalOnly=args.original_only, WholeRawLoaderComparisons=matched, NormalizedFields=0,
            NativeLibraryExecuted=original_executed, ManagedNativeExecuted=bool(cleanup_proofs),
            ManagedPhysicalCleanupProofs=cleanup_proofs,
            ManagedLoaderRoutesAttempted=sum(name in phases for name in ['ordinary', 'reviewed']),
            OriginalExecutionAttempted='original' in phases,
            OriginalReturnedBytes=original_bytes, ApiFamilyFunctions=plan['ApiFamilyFunctions'],
            SerializationProfiles=plan['SerializationProfiles'], DefaultProfileComparisons=default_comparisons,
            BothProfilesFromSameActualObservedTerm=True, PostCaptureEtfNormalizationPerformed=False,
            Phases=phases, Errors=failures, LinuxNativeExecutionClaimed=False,
            WholeFamilyCompatibilityClaimed=False, GoldenReferencePresent='OriginalGoldenSha256' in plan))
    if failures:
        raise RuntimeError('; '.join(failures))
    print(json.dumps({'Passed': True, 'OriginalOnly': args.original_only, 'WholeRawLoaderComparisons': matched,
        'OriginalReturnedBytes': original_bytes, 'GoldenChecked': golden_checked, 'ProviderFiles': 6427}))


if __name__ == '__main__':
    main()
