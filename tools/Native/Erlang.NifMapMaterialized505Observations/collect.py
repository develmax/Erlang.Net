"""Acquire 32 independent original observations before any whole-batch execution."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL = 'tools/Native/Erlang.NifMapMaterialized505Observations'


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def save(path, value):
    Path(path).write_text(json.dumps(value, indent=2,
        default=lambda item: {'BinaryHex': item.hex()} if isinstance(item, bytes) else str(item)) + '\n', encoding='utf-8')


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def dependencies(root, rows):
    proof = []
    for row in rows:
        path = (root / row['Path']).resolve()
        require(path.is_relative_to(root), 'dependency escapes root')
        raw = path.read_bytes()
        require(row['HashMode'] == 'TextLF', 'declared dependency hash mode')
        actual = hashlib.sha256(raw.replace(b'\r\n', b'\n')).hexdigest().upper()
        require(actual == row['Sha256'], 'dependency seal: ' + row['Path'])
        proof.append(dict(Path=row['Path'], HashMode='TextLF', PlannedSha256=actual,
            ActualRawSha256=sha(path), ActualRawBytes=len(raw)))
    return proof


def sources(root, tool, plan):
    for row in plan['Files']:
        path = (root / row['Path']).resolve()
        require(path.is_relative_to(tool) and sha(path) == row['Sha256'], 'raw source seal: ' + row['Path'])
    return dependencies(root, plan['Dependencies'])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--provider-admission', type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--runtime-version', default='10.0.11')
    parser.add_argument('--original-only', action='store_true')
    parser.add_argument('--owned-terms516', action='store_true')
    parser.add_argument('--both-runtime-profiles516', action='store_true')
    parser.add_argument('--check-golden', action='store_true')
    args = parser.parse_args()
    if args.both_runtime_profiles516 and args.owned_terms516:
        parser.error('--owned-terms516 and --both-runtime-profiles516 are mutually exclusive')
    root, tool, out = args.repository.resolve(), Path(__file__).resolve().parent, args.output.resolve()
    require(os.name == 'nt' and __debug__ and tool == root / TOOL, 'Windows non-optimized source collector')
    require(out.is_relative_to(root / 'artifacts/native-map505') and not out.exists(), 'fresh owned output')
    profiles516 = [('ordinary-runtime', False), ('owned-runtime', True)] if args.both_runtime_profiles516 else [
        ('owned-runtime' if args.owned_terms516 else 'ordinary-runtime', args.owned_terms516)]
    loader_routes516 = ['ordinary', 'reviewed']
    managed_runs516 = [(profile, enabled, route,
        profile + '--' + route if args.both_runtime_profiles516 else route,
        out/profile/route if args.both_runtime_profiles516 else out/route)
        for profile, enabled in profiles516 for route in loader_routes516]
    matched_by_profile516 = {profile: 0 for profile, _ in profiles516}
    managed_receipts516 = {}
    expected_comparisons516 = 0 if args.original_only else len(managed_runs516)
    plan_path = tool / 'source-plan.json'
    require(sha(plan_path) == args.plan_sha256.upper(), 'external plan seal')
    plan = json.loads(plan_path.read_bytes())
    require(plan['PinnedCommit'] == PIN and plan['ProtocolCaseCount'] == 32, 'pinned case protocol')
    require(plan['SerializationProfiles'] == dict(CanonicalOptions=['deterministic',dict(minor_version=2)],
        DefaultOptions=[dict(minor_version=2)]), 'declared serialization profiles')
    require(plan['BuildScriptSha256'] == sha(tool / 'build-native.py') and plan['CollectorSha256'] == sha(__file__), 'executable source seals')
    proof = sources(root, tool, plan)
    expected_cases = [(n, size, target) for n in [2,32,33,65] for size in [64,65] for target in [1,128,129,143]]
    require(plan['Cases'] == [dict(Count=n, Bytes=size, Target=target) for n,size,target in expected_cases], 'declared matrix')
    if args.check_golden:
        for key, name in [('OriginalGoldenSha256','original-values.etf'), ('OriginalDefaultGoldenSha256','original-values-default.etf')]:
            require(key in plan and sha(tool / name) == plan[key], 'independent original golden seal')
    owned = load('materialized505_owned', root / plan['OwnedHelper'])
    inventory = load('materialized505_inventory', root / plan['InventoryHelper'])
    decoder = load('materialized505_decoder', root / plan['Decoder'])
    provider = json.loads(args.provider_admission.read_bytes())
    require(provider['SourceCommit'] == PIN and provider['ReleaseTag'] == 'OTP-29.1.1', 'provider pin')
    installed = Path(provider['InstalledOtpRoot']).resolve()
    manifest = Path(provider['ExecutionManifestPath'])
    require(sha(manifest) == provider['ExecutionManifestSha256'], 'provider manifest seal')
    expected = {row['Path'].replace('\\','/'): row['Sha256'] for row in json.loads(manifest.read_bytes())}
    require(len(expected) == 6427 and inventory.inventory(installed) == expected, 'complete original provider before batch')
    out.mkdir(parents=True)
    save(out / 'dependency-proof.json', proof)
    save(out / 'inputs.json', dict(ProviderAdmissionPath=str(args.provider_admission.resolve()),
        ProviderAdmissionSha256=sha(args.provider_admission), ProviderManifestSha256=sha(manifest),
        SourcePlanSha256=sha(plan_path), OwnedTermsPilot506=None if args.both_runtime_profiles516 else args.owned_terms516, OriginalOnly=args.original_only, OriginalGoldenProvided='OriginalGoldenSha256' in plan))
    env = dict(os.environ)
    env['MAP_MATERIALIZED505_RUNTIME_ROOT'] = str(installed)
    # Preserve stderr and crash dumps; malformed-map failures must remain visible.
    for key in ['ERL_FLAGS','ERL_AFLAGS','ERL_ZFLAGS','ERL_LIBS','ERL_COMPILER_OPTIONS','ERLC_EMULATOR','ERL_ROOTDIR']:
        env.pop(key, None)
    failures, phases, safety = [], {}, []
    matched, batch_original_ok, provider_after, golden_checked = 0, False, False, False

    def phase(name, command, directory, cwd=None, environment=None):
        try:
            directory.mkdir(exist_ok=True)
            phases[name] = owned.owned(command, directory, name, 180, environment or env, str(cwd or directory))
            return True
        except Exception as error:
            receipt = directory / (name + '.exit.json')
            phases[name] = dict(CollectionFailed=True, Error=repr(error))
            try:
                if receipt.is_file():
                    phases[name] = json.loads(receipt.read_bytes())
            except Exception as receipt_error:
                failures.append(name + ' receipt: ' + repr(receipt_error))
            failures.append(name + ': ' + repr(error))
            return False

    def original(name, directory, library, case=None):
        directory.mkdir()
        shutil.copyfile(root / plan['ErlangSourcePath'], directory / 'map_postreturn505.erl')
        invocation = 'map_postreturn505:run()' if case is None else 'map_postreturn505:run_case(%d,%d,%d)' % case
        expression = (
            'W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
            'Expected=filename:absname(os:getenv("MAP_MATERIALIZED505_RUNTIME_ROOT")),Expected=filename:absname(code:root_dir()),'
            '8=erlang:system_info(wordsize),W("metadata.etf",{Expected,erlang:system_info(version),erlang:system_info(wordsize),erlang:system_info(system_architecture)}),'
            'C=compile:file("map_postreturn505.erl",[return_errors,return_warnings]),W("compile.etf",C),{ok,map_postreturn505,_}=C,'
            'ok=map_postreturn505:load("' + library.with_suffix('').as_posix() + '"),'
            'R=try ' + invocation + ' of T->W("returned-default.etf",T),'
            'ok=file:write_file("returned.etf",term_to_binary(T,[deterministic,{minor_version,2}])),{returned,ok}'
            ' catch Class:Reason:Stack->W("raised.etf",{Class,Reason,Stack}),{raised,Class,Reason,Stack} end,W("outcome.etf",R),'
            'D=code:delete(map_postreturn505),P=code:purge(map_postreturn505),L=code:is_loaded(map_postreturn505),'
            'W("cleanup.etf",{D,P,L}),true=D,false=P,false=L,case R of {returned,ok}->halt(0);_->halt(1) end.'
        )
        command = [str(installed / 'bin/erl.exe'), '+S','1:1','+A','1','-noshell','-noinput',
            '-boot','no_dot_erlang','-pa',str(directory),'-eval',expression]
        if not phase(name, command, directory, environment=dict(env,ERL_CRASH_DUMP=str(directory/'erl_crash.dump'))):
            return False
        try:
            cleanup = decoder.decode_exact((directory / 'cleanup.etf').read_bytes())
            require(cleanup == ('tuple', (('atom','true'),('atom','false'),('atom','false'))), 'original module cleanup')
            expected_items = 32 if case is None else 4
            for leaf in ['returned.etf','returned-default.etf']:
                value = decoder.decode_exact((directory / leaf).read_bytes())
                require(value[0] == ('list' if case is None else 'tuple') and len(value[1]) == expected_items, 'original protocol shape')
                save(directory / (leaf + '.readable.json'), value)
            return True
        except Exception as error:
            failures.append(name + ' protocol: ' + repr(error))
            return False

    try:
        prepared = phase('build', [sys.executable,'-B',str(tool / 'build-native.py'), '--plan',str(plan_path),
            '--approved-plan-sha256',args.plan_sha256,'--provider-admission',str(args.provider_admission.resolve()),
            '--output',str(out / 'build')], out / 'preparation', root)
        if prepared:
            folder = out / 'build'
            receipt_path = folder / 'build-receipt.json'
            build = json.loads(receipt_path.read_bytes())
            require(build['Status'] == 'MAP_MATERIALIZED505_WINDOWS_BUILD_COMPLETE', 'native build status')
            require(build['CompileExitCode'] == build['LinkExitCode'] == 0 and build['ExpectedExports'] == ['nif_init'], 'native build exits/exports')
            require(not build['ExecutionApproved'], 'build is not execution authorization')
            require(build['CompiledSourceSha256'] == build['NativeSourceSha256'] == plan['NativeSourceSha256'], 'compiled exact C source')
            require(build['CopiedErlangSourceSha256'] == build['ErlangSourceSha256'] == plan['ErlangSourceSha256'], 'copied exact Erlang source')
            for row in build['Files']:
                require(Path(row['Path']).name == row['Path'] and sha(folder / row['Path']) == row['Sha256'], 'native build artifact seal')
            library = folder / 'map_postreturn505.dll'
            require(sha(library) == build['LibrarySha256'], 'native DLL seal')
            admission = dict(Status='PARENT_REVIEWED_MAP_MATERIALIZED505_ADMISSION',BuildReceiptPath=str(receipt_path),
                BuildReceiptSha256=sha(receipt_path),PlanPath=str(plan_path),PlanSha256=sha(plan_path),
                LibraryPath=str(library),LibrarySha256=sha(library),NativeSourcePath=str(root / plan['NativeSourcePath']),
                NativeSourceSha256=plan['NativeSourceSha256'],ErlangSourcePath=str(root / plan['ErlangSourcePath']),ErlangSourceSha256=plan['ErlangSourceSha256'])
            admission_path = out / 'execution-admission.json'
            save(admission_path, admission)
            cases_dir = out / 'original-cases'
            cases_dir.mkdir()
            for index, case in enumerate(expected_cases):
                name = 'case-%02d-n%d-b%d-t%d' % (index,*case)
                directory = cases_dir / name
                try:
                    passed = original(name,directory,library,case)
                except Exception as error:
                    failures.append(name + ': ' + repr(error))
                    passed = False
                safety.append(dict(Index=index,Count=case[0],Bytes=case[1],Target=case[2],Passed=passed,
                    CanonicalSha256=sha(directory / 'returned.etf') if (directory / 'returned.etf').is_file() else None,
                    DefaultSha256=sha(directory / 'returned-default.etf') if (directory / 'returned-default.etf').is_file() else None))
            save(out / 'original-safety.json', dict(Cases=safety,All32Safe=len(safety)==32 and all(row['Passed'] for row in safety)))
            if len(safety)==32 and all(row['Passed'] for row in safety):
                batch_original_ok = original('original-batch',out / 'original-batch',library)
                if batch_original_ok and args.check_golden:
                    try:
                        require((out / 'original-batch/returned.etf').read_bytes() == (tool / 'original-values.etf').read_bytes(), 'original actual batch canonical golden differs')
                        require((out / 'original-batch/returned-default.etf').read_bytes() == (tool / 'original-values-default.etf').read_bytes(), 'original actual batch default golden differs')
                        golden_checked = True
                    except Exception as error:
                        failures.append('independent golden repeat: ' + repr(error))
                if not args.original_only:
                    host = root / 'tools/Native/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll'
                    prepared516 = all(path.is_file() for path in [host, host.with_suffix('.deps.json'), host.with_suffix('.runtimeconfig.json')]) and shutil.which('dotnet')
                    if not prepared516:
                        failures.append('managed preparation missing; dependent routes skipped')
                    else:
                        for profile, enabled, route, label, directory in managed_runs516:
                            phase(label,['dotnet','exec','--fx-version',args.runtime_version,str(host),'--map-materialized505',
                                str(admission_path),sha(admission_path),str(directory),route] + (['--owned-terms516'] if enabled else []),out / (label+'-process'),root)
                        for profile, enabled, route, label, directory in managed_runs516:
                            try:
                                done = json.loads((directory / 'completion.json').read_bytes())
                                require(done['OwnedTermsPilot506'] == enabled, label + ': runtime profile')
                                assert done['Route'] == route and done['AdmissionSha256'] == sha(admission_path)
                                assert done['Status'] == 'NATIVE_IMAGE_RETIRED_RUNTIME_DISPOSED'
                                require(done['ReviewedPolicyInvocationCount'] == (1 if route=='reviewed' else 0), label + ': reviewed policy count')
                                for key in ['EnvironmentBaselineRestored','BinaryOwnershipBaselineRestored','NativeModuleAbsentBeforeLoad',
                                    'NativeModulePresentDuringInvocation','NativeModuleAbsentAfterRetirementAndRuntimeDisposal']:
                                    require(done[key], label + ': ' + key)
                                managed_receipts516[label] = done
                                if batch_original_ok:
                                    for leaf in ['returned.etf','returned-default.etf']:
                                        require((directory / leaf).read_bytes() == (out / 'original-batch' / leaf).read_bytes(), label + ': whole raw profile differs: ' + leaf)
                                    matched += 1
                                    matched_by_profile516[profile] += 1
                            except Exception as error:
                                failures.append(label + ': ' + repr(error))
            else:
                failures.append('independent original safety incomplete; whole original batch and managed routes skipped')
    except Exception as error:
        failures.append(repr(error))
    finally:
        try:
            provider_after = inventory.inventory(installed) == expected
            require(provider_after, 'original provider changed')
            require(sha(args.provider_admission) == json.loads((out / 'inputs.json').read_bytes())['ProviderAdmissionSha256'], 'provider admission changed')
            sources(root,tool,plan)
        except Exception as error:
            failures.append('final input recheck: ' + repr(error))
        if not args.original_only and (matched != expected_comparisons516 or any(count != len(loader_routes516) for count in matched_by_profile516.values())):
            failures.append('Not all requested runtime-profile/loader comparisons passed')
        save(out / 'completion.json',dict(Passed=not failures,PinnedCommit=PIN,ProviderFiles=6427,
            ProviderBeforeEqual=True,ProviderAfterEqual=provider_after,SourcePlanSha256=sha(plan_path),
            OriginalSafetyCases=safety,OriginalSafetyPassed=sum(row['Passed'] for row in safety),
            ManagedNativeExecuted=bool(managed_receipts516), ManagedLoaderRoutesAttempted=sum(label in phases for _, _, _, label, _ in managed_runs516), WholeOriginalBatchPassed=batch_original_ok,BothRuntimeProfiles516=args.both_runtime_profiles516, ExpectedRawLoaderComparisons=expected_comparisons516, WholeRawComparisonsByRuntimeProfile=matched_by_profile516, ManagedRuntimeProfileReceipts=managed_receipts516, WholeRawLoaderComparisons=matched,
            OwnedTermsPilot506=None if args.both_runtime_profiles516 else args.owned_terms516, OriginalOnly=args.original_only,GoldenChecked=golden_checked,GoldenReferencePresent='OriginalGoldenSha256' in plan,
            WholeProfilesComparisonPolicy='strict whole bytes for canonical and default',
            WholeProfilesComparedStrictly=not args.original_only and matched==expected_comparisons516,NormalizedFields=0,PostCaptureEtfNormalizationPerformed=False,
            ActualWholeBatchProfilesCaptured=batch_original_ok,Phases=phases,Errors=failures,WholeApiCompatibilityClaimed=False))
    require(not failures, 'independent observations retained; inspect completion.json')


if __name__ == '__main__':
    main()
