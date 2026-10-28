"""Acquire and compare the complete pinned NIF map family using a real C fixture."""
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


def dependencies(root, plan):
    verified = []
    for row in plan['Dependencies']:
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
            ActualRawSha256=sha(path), ActualRawBytes=len(raw)))
    return verified


def compare_default_profile(decoder, original, managed):
    first, first_spans = decoder.decode_spanned(original)
    second, second_spans = decoder.decode_spanned(managed)
    assert first[0] == second[0] == 'tuple' and len(first[1]) == len(second[1]) == 14
    for group in range(14):
        if group != 9:
            assert first[1][group] == second[1][group], 'Default typed group differs: ' + str(group)
    left, right = first[1][9], second[1][9]
    assert left[0] == right[0] == 'tuple' and len(left[1]) == len(right[1]) == 3
    assert left[1][0] == right[1][0] and left[1][2] == right[1][2]
    left_map, right_map = left[1][1], right[1][1]
    assert left_map[0] == right_map[0] == 'map' and len(left_map[1]) == len(right_map[1]) == 2
    exact_bindings = {(('atom', 'added'), ('integer', 99)), (('atom', 'old'), ('atom', 'kept'))}
    assert set(left_map[1]) == set(right_map[1]) == exact_bindings
    path = (9, 1)
    a, b = first_spans[path], second_spans[path]
    # Compare raw prefixes/suffixes and each complete map pair directly. Never
    # rewrite, sort or replace any captured ETF data to create a comparison.
    assert original[:a[0]] == managed[:b[0]], 'Default raw prefix differs outside declared map'
    assert original[a[1]:] == managed[b[1]:], 'Default raw suffix differs outside declared map'
    assert original[a[0]:a[0]+5] == managed[b[0]:b[0]+5] == bytes([116, 0, 0, 0, 2])
    def pairs(raw, spans):
        return tuple(raw[spans[path+(index*2,)][0]:spans[path+(index*2+1,)][1]] for index in range(2))
    original_pairs, managed_pairs = pairs(original, first_spans), pairs(managed, second_spans)
    assert len(set(original_pairs)) == len(set(managed_pairs)) == 2
    assert set(original_pairs) == set(managed_pairs), 'Default raw pair bytes differ'
    return dict(WholeRawBytesEqual=original == managed,
        OnlyAllowedMapEntryOrderDifference=original != managed,
        AllowedPath='root.group9.independent.result_map', ExactAtomKeyBindingsVerified=True,
        AllOtherTypedGroupsStrictlyEqual=True, AllRawBytesOutsideAllowedMapStrictlyEqual=True,
        CompleteRawPairBytesEqual=True, OriginalRawSha256=hashlib.sha256(original).hexdigest().upper(),
        ManagedRawSha256=hashlib.sha256(managed).hexdigest().upper(),
        CapturedFilesRewritten=False, EtfNormalizationPerformed=False)


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
    assert os.name == 'nt' and tool == root/'tools/Native/Erlang.NifMapFamily504Observations'
    assert out.is_relative_to(root/'artifacts/native-map504') and not out.exists()
    plan_path = tool/'source-plan.json'
    assert sha(plan_path) == args.plan_sha256.upper()
    plan = json.loads(plan_path.read_bytes())
    assert plan['PinnedCommit'] == PIN and sha(tool/'build-native.py') == plan['BuildScriptSha256']
    assert sha(__file__) == plan['CollectorSha256']
    for key in ['NativeSource', 'ErlangSource']:
        source = (root/plan[key+'Path']).resolve()
        assert source.is_relative_to(tool) and sha(source) == plan[key+'Sha256']
    assert plan['SerializationProfiles']['CanonicalOptions'] == ['deterministic', {'minor_version': 2}]
    assert plan['SerializationProfiles']['DefaultOptions'] == [{'minor_version': 2}]
    dependency_proof = dependencies(root, plan)
    if args.check_golden:
        assert 'OriginalGoldenSha256' in plan and sha(tool/'original-values.etf') == plan['OriginalGoldenSha256']
    host = root/'tools/Native/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll'
    if not args.original_only and not host.is_file():
        raise RuntimeError('Managed host preparation is absent; dependent native loader execution was not started: ' + str(host))
    helpers = root/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts'
    owned, inventory = load('owned_family504', helpers/'owned.py'), load('inventory_family504', helpers/'bounded_inventory.py')
    decoder = load('decoder_family504', tool/'diagnostic_etf.py')
    provider = json.loads(args.provider_admission.read_bytes())
    assert provider['SourceCommit'] == PIN and provider['ReleaseTag'] == 'OTP-29.1.1'
    installed = Path(provider['InstalledOtpRoot']).resolve()
    manifest = Path(provider['ExecutionManifestPath'])
    assert sha(manifest) == provider['ExecutionManifestSha256']
    expected = {row['Path'].replace('\\', '/'): row['Sha256'] for row in json.loads(manifest.read_bytes())}
    assert len(expected) == 6427 and inventory.inventory(installed) == expected
    out.mkdir(parents=True)
    save(out/'dependency-proof.json', dependency_proof)
    failures, phases = [], {}
    golden_checked, matched, original_executed = False, 0, False
    original_bytes = None
    default_comparisons = {}
    env = dict(os.environ)
    env['ERL_CRASH_DUMP'] = 'NUL'
    env['MAP_FAMILY504_RUNTIME_ROOT'] = str(installed)
    for name in ['ERL_FLAGS', 'ERL_AFLAGS', 'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS', 'ERLC_EMULATOR']:
        env.pop(name, None)

    def phase(name, command, directory, cwd=None):
        directory.mkdir(exist_ok=True)
        try:
            receipt = owned.owned(command, directory, name, 180, env, str(cwd or directory))
            phases[name] = receipt
            return True
        except Exception as error:
            receipt_path = directory/(name+'.exit.json')
            if receipt_path.is_file():
                phases[name] = json.loads(receipt_path.read_bytes())
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
            assert build['Status'] == 'MAP_FAMILY504_WINDOWS_BUILD_COMPLETE'
            assert build['CompileExitCode'] == build['LinkExitCode'] == 0
            assert build['ExpectedExports'] == ['nif_init'] and not build['ExecutionApproved']
            assert build['CompiledSourceSha256'] == build['NativeSourceSha256'] == plan['NativeSourceSha256']
            assert build['CopiedErlangSourceSha256'] == build['ErlangSourceSha256'] == plan['ErlangSourceSha256']
            for row in build['Files']:
                assert Path(row['Path']).name == row['Path']
                assert sha(folder/row['Path']) == row['Sha256'] and (folder/row['Path']).stat().st_size == row['Bytes']
            library = folder/'map_family504.dll'
            assert sha(library) == build['LibrarySha256']
            admission_path = out/'execution-admission.json'
            admission = dict(Status='PARENT_REVIEWED_MAP_FAMILY504_ADMISSION', BuildReceiptPath=str(receipt_path),
                BuildReceiptSha256=sha(receipt_path), PlanPath=str(plan_path), PlanSha256=sha(plan_path),
                LibraryPath=str(library), LibrarySha256=sha(library),
                NativeSourcePath=str(root/plan['NativeSourcePath']), NativeSourceSha256=plan['NativeSourceSha256'],
                ErlangSourcePath=str(root/plan['ErlangSourcePath']), ErlangSourceSha256=plan['ErlangSourceSha256'])
            save(admission_path, admission)
            original = out/'original'
            original.mkdir()
            shutil.copyfile(root/plan['ErlangSourcePath'], original/'map_family504.erl')
            stem = library.with_suffix('').as_posix()
            expression = ('W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
                'Expected=filename:absname(os:getenv("MAP_FAMILY504_RUNTIME_ROOT")),Expected=filename:absname(code:root_dir()),'
                '8=erlang:system_info(wordsize),W("metadata.etf",{Expected,erlang:system_info(version),erlang:system_info(wordsize),erlang:system_info(system_architecture)}),'
                'C=compile:file("map_family504.erl",[return_errors,return_warnings]),W("compile.etf",C),{ok,map_family504,_}=C,'
                'ok=map_family504:load("' + stem + '"),'
                'T=map_family504:run(),W("returned-default.etf",T),'
                'ok=file:write_file("returned.etf",term_to_binary(T,[deterministic,{minor_version,2}])),'
                'D=code:delete(map_family504),P=code:purge(map_family504),L=code:is_loaded(map_family504),'
                'W("cleanup.etf",{D,P,L}),true=D,false=P,false=L,halt(0).')
            original_ok = phase('original', [str(installed/'bin/erl.exe'), '+S', '1:1', '+A', '1', '-noshell', '-noinput',
                '-boot', 'no_dot_erlang', '-pa', str(original), '-eval', expression], original)
            original_executed = original_ok
            raw = None
            if original_ok:
                try:
                    raw = (original/'returned.etf').read_bytes()
                    decoded = decoder.decode_exact(raw)
                    assert decoded[0] == 'tuple' and len(decoded[1]) == 14
                    save(original/'returned-readable.json', decoded)
                    default_raw = (original/'returned-default.etf').read_bytes()
                    default_decoded = decoder.decode_exact(default_raw)
                    assert default_decoded[0] == 'tuple' and len(default_decoded[1]) == 14
                    save(original/'returned-default-readable.json', default_decoded)
                    original_bytes = len(raw)
                    if args.check_golden:
                        assert raw == (tool/'original-values.etf').read_bytes()
                        golden_checked = True
                except Exception as error:
                    failures.append('original protocol validation: ' + repr(error))
            if not args.original_only:
                # Routes are independent after successful C/managed-host preparation.
                for route in ['ordinary', 'reviewed']:
                    phase(route, ['dotnet', 'exec', '--fx-version', args.runtime_version, str(host), '--map-family504',
                        str(admission_path), sha(admission_path), str(out/route), route], out/(route+'-process'), root)
                if raw is not None:
                    for route in ['ordinary', 'reviewed']:
                        returned = out/route/'returned.etf'
                        if not returned.is_file():
                            failures.append(route + ': no returned ETF')
                            continue
                        try:
                            assert returned.read_bytes() == raw, route + ': whole deterministic ETF differs'
                            default_comparisons[route] = compare_default_profile(decoder,
                                (original/'returned-default.etf').read_bytes(), (out/route/'returned-default.etf').read_bytes())
                            done = json.loads((out/route/'completion.json').read_bytes())
                            assert done['ReviewedPolicyInvocationCount'] == (1 if route == 'reviewed' else 0)
                            for key in ['EnvironmentBaselineRestored', 'BinaryOwnershipBaselineRestored', 'NativeModuleAbsentBeforeLoad',
                                'NativeModulePresentDuringInvocation', 'NativeModuleAbsentAfterRetirementAndRuntimeDisposal']:
                                assert done[key], (route, key)
                            matched += 1
                        except Exception as error:
                            failures.append(route + ': ' + repr(error))
    except Exception as error:
        failures.append(repr(error))
    finally:
        after = inventory.inventory(installed) == expected
        if not after:
            failures.append('Original provider changed')
        save(out/'completion.json', dict(Passed=not failures, Pin=PIN, Platform='windows-x64',
            ProviderFiles=6427, ProviderBeforeEqual=True, ProviderAfterEqual=after,
            SourcePlanSha256=sha(plan_path), GoldenChecked=golden_checked,
            OriginalOnly=args.original_only, WholeRawLoaderComparisons=matched, NormalizedFields=0,
            NativeLibraryExecuted=original_executed, ManagedNativeExecuted=matched > 0,
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
