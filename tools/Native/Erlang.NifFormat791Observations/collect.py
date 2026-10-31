"""Prepare-only by default; Root may execute independent genuine NIF subjects."""
import argparse
import json
import os
from pathlib import Path
import shutil
import sys
from common import PACKAGE, PIN, HELPER, sha, save, require, load, check, snapshot, validate_rows


def expression(subject, library):
    module, observer = subject['Module'], subject['Observer']
    files = [Path(path).name for path in subject['Erlang']]
    file_list = '[' + ','.join(json.dumps(name) for name in files) + ']'
    modules = '[' + module + ',' + observer + ']'
    validate = ('{loaded,Rows}=T,41=length(Rows),'
                'true=lists:all(fun({N,S,R,B,C})->is_atom(N) andalso is_integer(S) andalso is_integer(R) '
                'andalso is_binary(B) andalso byte_size(B)=:=128 andalso is_integer(C);(_)->false end,Rows),')
    if subject['Kind'] == 'mutex':
        validate = ('{loaded,Tuple}=T,8=tuple_size(Tuple),'
                    'true=lists:all(fun erlang:is_integer/1,tuple_to_list(Tuple)),')
    return ('W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
            'try Expected=filename:absname(os:getenv("MAP_FAMILY504_RUNTIME_ROOT")),'
            'Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
            'W("metadata.etf",{Expected,erlang:system_info(version),erlang:system_info(system_architecture),'
            'erlang:system_info(dirty_cpu_schedulers),erlang:system_info(dirty_io_schedulers)}),'
            'C=[compile:file(F,[return_errors,return_warnings])||F<-' + file_list + '],W("compile.etf",C),'
            'true=lists:all(fun({ok,_,_})->true;({ok,_})->true;(_)->false end,C),'
            'W("executing-owners.etf",[{M,code:which(M),M:module_info(md5),M:module_info(compile),'
            'M:module_info(attributes),M:module_info(exports)}||M<-' + modules + ']),'
            'T=' + observer + ':observe(' + json.dumps(library.with_suffix('').as_posix()) + ',probe),'
            'W("observed.etf",T),ok=file:write_file("observed-deterministic.etf",'
            'term_to_binary(T,[deterministic,{minor_version,2}])),' + validate +
            'D=code:delete(' + module + '),P=code:purge(' + module + '),L=code:is_loaded(' + module + '),'
            'W("cleanup.etf",{D,P,L}),false=L,halt(0)'
            ' catch Class:Reason:Stack->W("entry-failure.etf",{Class,Reason,Stack}),halt(77) end.')


def admission(subject, out, library, build_path, plan_path):
    original = out / 'original'
    paths = [original / (name + '.beam') for name in (subject['Module'], subject['Observer'])]
    inputs = [plan_path, build_path, original / 'compile.etf', original / 'executing-owners.etf',
              original / 'metadata.etf', original / 'observed.etf', original / 'observed-deterministic.etf', original / 'cleanup.etf']
    value = {'Status': 'SOURCE_BOUND_ORIGINAL_DIRTY521_JOB', 'Pin': PIN, 'Mode': 'probe',
             'SubjectKind': 'GENUINE_FUNCTIONAL_' + subject['Name'].upper(),
             'ObserverModule': subject['Observer'], 'NifModule': subject['Module'],
             'NativeLibraryPath': str(library), 'NativeLibrarySha256': sha(library),
             'BuildReceiptPath': str(build_path), 'BuildReceiptSha256': sha(build_path),
             'Images': [{'Module': p.stem, 'Path': str(p), 'Sha256': sha(p)} for p in paths],
             'Dependencies': ['erlang', subject['Module'], subject['Observer']], 'ControlDependencies': [],
             'OriginalRawPath': str(original / 'observed.etf'),
             'OriginalDeterministicPath': str(original / 'observed-deterministic.etf'),
             'OriginalCleanupPath': str(original / 'cleanup.etf'),
             'Inputs': [{'Path': str(p), 'Sha256': sha(p)} for p in inputs]}
    save(out / 'execution-admission.json', value)
    return out / 'execution-admission.json'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--plan', type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--official-admission', type=Path, required=True)
    parser.add_argument('--admission-sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--execute', action='store_true')
    parser.add_argument('--with-managed', action='store_true')
    args = parser.parse_args()
    require(sha(args.plan) == args.plan_sha256.upper(), 'Final observation plan differs')
    plan = json.loads(args.plan.read_bytes()); check(plan)
    repo = Path(plan['Repository']); out = args.output.resolve()
    require(os.name == 'nt' and out.is_relative_to(repo / 'artifacts') and not out.exists(), 'Fresh Windows repository-artifacts output required')
    require(str(out).isascii(), 'Existing managed child protocol requires ASCII owned artifact paths')
    require(not args.with_managed or args.execute, 'Managed comparison requires genuine original execution')
    out.mkdir(parents=True)
    save(out / 'launch-plan.json', {'Execute': args.execute, 'Managed': args.with_managed,
                                   'PlanSha256': sha(args.plan), 'FullCompatibilityClaimed': False})
    if not args.execute:
        print(json.dumps({'PreparedOnly': True, 'Output': str(out)})); return 0
    helper = load(repo / HELPER, 'functional_nif_observation_owned')
    args.build_receipt = args.otp_source = args.expected_head = None
    installed, erl, provider_before, provider_after = helper.provider(args, repo)
    save(out / 'provider-before.json', provider_before)
    env = dict(os.environ, MAP_FAMILY504_RUNTIME_ROOT=str(installed), ERL_CRASH_DUMP='NUL')
    for key in ('ERL_FLAGS', 'ERL_AFLAGS', 'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS', 'ERLC_EMULATOR'):
        env.pop(key, None)
    errors = []; phases = {}; matched = []; shape_counts = {}
    def phase(name, command, directory, prepared=False):
        print(json.dumps({'Stage': name, 'State': 'starting'}), flush=True)
        try:
            if not prepared: directory.mkdir(parents=True)
            phases[name] = helper.execute(repo, command, directory, env)
            print(json.dumps({'Stage': name, 'State': 'completed'}), flush=True)
            return True
        except Exception as error:
            errors.append(name + ': ' + repr(error)); phases[name] = {'Failed': True, 'Error': repr(error)}
            print(json.dumps({'Stage': name, 'State': 'failed', 'Error': repr(error)}), flush=True)
            return False
    try:
        for subject in plan['Subjects']:
            subject_out = out / subject['Name']; subject_out.mkdir()
            prefix = subject['Name']
            build = subject_out / 'build'
            built = phase(prefix + '/build', [sys.executable, '-B', str(PACKAGE / 'compile-native.py'),
                '--plan', str(args.plan.resolve()), '--plan-sha256', args.plan_sha256, '--subject', subject['Name'],
                '--provider-root', str(installed), '--output', str(build)], subject_out / 'prepare')
            if not built: continue  # Dependent VM runs MUST NOT consume a failed preparation.
            try:
                build_path = build / 'build-receipt.json'; proof = json.loads(build_path.read_bytes())
                require(proof['Status'] == 'SOURCE_BOUND_FUNCTIONAL_NIF_WINDOWS_BUILD', 'Wrong native build status')
                library = Path(proof['LibraryPath']); require(sha(library) == proof['LibrarySha256'], 'Built library changed')
                original = subject_out / 'original'; original.mkdir()
                for source in subject['Erlang']:
                    shutil.copyfile(PACKAGE / source, original / Path(source).name)
                command = [str(erl), '+S', '1:1', '+A', '1', '+SDcpu', '1', '+SDio', str(plan['OriginalIoWorkers']),
                           '-noshell', '-noinput', '-boot', 'no_dot_erlang', '-pa', str(original), '-eval', expression(subject, library)]
                save(original / 'launch.json', {'Command': command})
                if not phase(prefix + '/original', command, original, prepared=True): continue
                shape_counts[prefix + '/original'] = validate_rows(repo, original / 'observed.etf', subject)
                entry = admission(subject, subject_out, library, build_path, args.plan.resolve())
            except Exception as error:
                errors.append(prefix + '/original-validation: ' + repr(error)); continue
            if args.with_managed:
                for profile in ('ordinary', 'private-owned'):
                    for route in ('ordinary', 'reviewed'):
                        name = prefix + '/' + profile + '-' + route
                        directory = subject_out / (profile + '-' + route); actual = directory / 'actual'
                        command = [plan['DotnetPath'], 'exec', '--fx-version', plan['RuntimeVersion'], plan['ManagedHost'],
                                   '--nif-dirty521-child', str(entry), sha(entry), route, profile, str(actual)]
                        passed = phase(name, command, directory)
                        # Validate all independent children, including a completion
                        # failure, without rerunning or hiding the failed child.
                        try:
                            receipt = json.loads((actual / 'completion.json').read_bytes())
                            require(passed and receipt['ChildPassedFiniteRawComparison'] and receipt['RawComparisonPassed']
                                    and receipt['ExecutionAndCleanupPassed'] and receipt['NativeEnvironmentAndBinaryBaselinesRestored']
                                    and receipt['NativeModuleAbsentBeforeAndAfter'], 'Managed whole outcome/lifecycle failed')
                            require(receipt['AdmissionSha256'] == sha(entry) and receipt['Route'] == route
                                    and receipt['OwnedProfile'] == (profile == 'private-owned'), 'Wrong actual profile/route/admission')
                            require(receipt['NormalizationPerformed'] is False, 'Normalization is forbidden')
                            require(receipt['NativeAllocationLedgerBaselineRestored']
                                    and receipt['NativeMutexRegistryBaselineRestored']
                                    and receipt['NativeMutexRegistryBefore'] == receipt['NativeMutexRegistryAfter'] == 0,
                                    'Actual native arena/mutex registry failed to settle')
                            ledger = receipt['NativeAllocationLedgerAfter']
                            require(ledger['JoinedOwners'] == 0
                                    and ledger['Allocations']['Count'] == 0
                                    and ledger['Allocations']['RequestedBytes'] == 0
                                    and ledger['Allocations']['BackingRequestBytes'] == 0,
                                    'Actual native allocation storage or owners remain')
                            shape_counts[name] = validate_rows(repo, actual / 'observed.etf', subject)
                            matched.append(name)
                        except Exception as error:
                            errors.append(name + '/validation: ' + repr(error))
                require(sha(library) == proof['LibrarySha256'], 'Native fixture changed after replay')
    except Exception as error:
        errors.append('batch: ' + repr(error))
    finally:
        try:
            after = provider_after(); save(out / 'provider-after.json', after)
            require(after == provider_before['Inventory'], 'Admitted original provider changed')
            check(plan)
            save(out / 'managed-inputs-after.json', snapshot(Path(plan['ManagedHost']).parent))
        except Exception as error:
            errors.append('final-input-guards: ' + repr(error))
        artifacts = [{'Path': str(p.relative_to(out)), 'Sha256': sha(p), 'Bytes': p.stat().st_size}
                     for p in sorted(out.rglob('*')) if p.is_file() and p.name not in ('completion.json', 'artifacts.json')]
        save(out / 'artifacts.json', artifacts)
        expected = len(plan['Subjects']) * 4 if args.with_managed else 0
        if len(matched) != expected: errors.append('Matched managed routes ' + str(len(matched)) + '/' + str(expected))
        save(out / 'completion.json', {'Passed': not errors, 'PlanSha256': sha(args.plan), 'Phases': phases,
            'MatchedManagedRoutes': matched, 'ExpectedManagedRoutes': expected, 'ActualWholeShapeCounts': shape_counts,
            'ArtifactsSha256': sha(out / 'artifacts.json'), 'Errors': errors, 'NormalizationPerformed': False,
            'FullFormatterCompatibilityClaimed': False, 'MutexLedgerBaselineExplicitlyObserved': len(matched) == len(plan['Subjects']) * 4,
            'WholeContractsClosed': 0})
    return 1 if errors else 0


if __name__ == '__main__':
    raise SystemExit(main())
