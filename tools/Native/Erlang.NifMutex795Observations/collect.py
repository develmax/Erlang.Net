"""Owned genuine lifecycle acquisition + independent actual managed children; opt-in."""
import argparse
import json
import os
from pathlib import Path
import shutil
import sys
from common import PACKAGE, PIN, HELPER, PASSIVE, sha, save, require, load, check


def expression(subject, original, libraries):
    def q(value): return json.dumps(str(value).replace('\\', '/'))
    runner = original / 'mutex_lifecycle_runner795.erl'
    observer = original / 'mutex_lifecycle_observer795.erl'
    wrapper = original / 'mutex_lifecycle795.erl'
    library = libraries['failed' if subject == 'failed' else 'a']['Path']
    stem = lambda value: str(Path(value).with_suffix(''))
    return ('Expected=filename:absname(os:getenv("MAP_FAMILY504_RUNTIME_ROOT")),'
            'Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
            '{ok,mutex_lifecycle_runner795,R}=compile:file(' + q(runner) + ',[binary]),'
            '{module,mutex_lifecycle_runner795}=code:load_binary(mutex_lifecycle_runner795,' + q(runner) + ',R),'
            'mutex_lifecycle_runner795:run(' + subject + ',' + q(wrapper) + ',' + q(observer) + ','
            + q(stem(library)) + ',' + q(stem(libraries['b']['Path']) if subject == 'upgrade' else '') + ',' + q(original) + ').')


def validate_original(repo, original, subject):
    decoder = load(repo / PASSIVE, 'mutex795_exact_decoder')
    value = decoder.decode_exact((original / 'observed.etf').read_bytes())
    require(value[0] == 'tuple' and len(value[1]) == 2 and value[1][0] == ('atom', 'returned'), 'Original source failed before complete outcome')
    returned = value[1][1]
    require(returned[0] == 'tuple' and len(returned[1]) == (4 if subject == 'failed' else 9), 'Original lifecycle result shape differs')
    require(decoder.decode_exact((original / 'cleanup.etf').read_bytes()) == ('atom', 'false'), 'Original code cleanup did not complete')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--plan', type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--official-admission', type=Path, required=True)
    parser.add_argument('--admission-sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--execute', action='store_true')
    parser.add_argument('--subject', action='append', choices=['failed', 'upgrade'])
    args = parser.parse_args()
    selected_subjects = list(dict.fromkeys(args.subject or ['failed', 'upgrade']))
    expected_routes = len(selected_subjects) * 4
    require(sha(args.plan) == args.plan_sha256.upper(), 'Final plan changed')
    plan = json.loads(args.plan.read_bytes()); check(plan)
    repo = Path(plan['Repository']); out = args.output.resolve()
    require(os.name == 'nt' and out.is_relative_to(repo / 'artifacts') and not out.exists(), 'Fresh Windows repository-artifacts output required')
    out.mkdir(parents=True)
    save(out / 'launch-plan.json', {'Execute': args.execute, 'PlanSha256': sha(args.plan), 'FullCompatibilityClaimed': False})
    if not args.execute:
        print(json.dumps({'PreparedOnly': True, 'Output': str(out)})); return 0
    helper = load(repo / HELPER, 'mutex795_owned_original')
    args.build_receipt = args.otp_source = args.expected_head = None
    installed, erl, before, after = helper.provider(args, repo)
    save(out / 'provider-before.json', before)
    env = dict(os.environ, MAP_FAMILY504_RUNTIME_ROOT=str(installed), ERL_CRASH_DUMP='NUL')
    for key in ('ERL_FLAGS', 'ERL_AFLAGS', 'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS', 'ERLC_EMULATOR'):
        env.pop(key, None)
    errors = []; stages = {}; libraries = {}; completed = []
    def phase(name, command, directory, prepared=False):
        print(json.dumps({'Stage': name, 'State': 'starting'}), flush=True)
        try:
            if not prepared: directory.mkdir(parents=True)
            stages[name] = helper.execute(repo, command, directory, env)
            print(json.dumps({'Stage': name, 'State': 'completed'}), flush=True); return True
        except Exception as error:
            errors.append(name + ': ' + repr(error)); stages[name] = {'Error': repr(error)}
            print(json.dumps({'Stage': name, 'State': 'failed', 'Error': repr(error)}), flush=True); return False
    try:
        for subject in plan['Subjects']:
            name = subject['Name']; build = out / ('build-' + name)
            if not phase('build-' + name, [sys.executable, '-B', str(PACKAGE / 'compile-native.py'), '--plan', str(args.plan.resolve()),
                    '--plan-sha256', args.plan_sha256, '--subject', name, '--provider-root', str(installed), '--output', str(build)], out / ('prepare-' + name)):
                continue
            try:
                native_build = build / 'build-receipt.json'; receipt = json.loads(native_build.read_bytes())
                require(receipt['Status'] == 'SOURCE_BOUND_FUNCTIONAL_NIF_WINDOWS_BUILD', 'Wrong actual native build receipt')
                original_library = Path(receipt['LibraryPath']); require(sha(original_library) == receipt['LibrarySha256'], 'Compiled library differs')
                library = build / ('mutex_lifecycle795_' + name + '.dll')
                shutil.copyfile(original_library, library)
                require(sha(library) == sha(original_library), 'Byte-exact physical library publication differs')
                published = build / 'published-library.json'
                save(published, {'Status': 'SOURCE_BOUND_MUTEX795_PHYSICAL_LIBRARY_COPY', 'Pin': PIN,
                    'LibraryPath': str(library), 'LibrarySha256': sha(library), 'CompilerBuildReceiptPath': str(native_build),
                    'CompilerBuildReceiptSha256': sha(native_build), 'CompiledLibraryPath': str(original_library),
                    'CompiledLibrarySha256': sha(original_library), 'NativeExecuted': False})
                libraries[name] = {'Id': name, 'Path': str(library), 'Sha256': sha(library),
                    'BuildReceiptPath': str(published), 'BuildReceiptSha256': sha(published)}
            except Exception as error: errors.append('build-validation-' + name + ': ' + repr(error))
        for subject in selected_subjects:
            required = ('failed',) if subject == 'failed' else ('a', 'b')
            if not all(name in libraries for name in required):
                errors.append(subject + ': dependent native preparation failed'); continue
            destination = out / subject; destination.mkdir()
            original = destination / 'original'; original.mkdir()
            for name in ('mutex_lifecycle795.erl', 'mutex_lifecycle_observer795.erl', 'mutex_lifecycle_runner795.erl'):
                shutil.copyfile(PACKAGE / 'fixtures' / name, original / name)
            command = [str(erl), '+S', '1:1', '+A', '1', '+SDcpu', '1', '+SDio', str(plan['OriginalIoWorkers']),
                '-noshell', '-noinput', '-boot', 'no_dot_erlang', '-eval', expression(subject, original, libraries)]
            save(original / 'launch.json', {'Command': command})
            if not phase(subject + '-original', command, original, True): continue
            try:
                validate_original(repo, original, subject)
                # Compile original observer binary is retained verbatim; no
                # compiler/native expected value is generated from our VM.
                for name in ('observer.beam', 'beam-a.beam', 'beam-b.beam'):
                    require((original / name).is_file(), 'Missing genuine original BEAM: ' + name)
                inputs = [args.plan.resolve(), *sorted(original.glob('*.etf')), *sorted(original.glob('*.beam')),
                    *[Path(row['Path']) for row in plan['Inputs']]]
                for row in (libraries[name] for name in required):
                    inputs.extend([Path(row['Path']), Path(row['BuildReceiptPath'])])
                    pub = json.loads(Path(row['BuildReceiptPath']).read_bytes())
                    inputs.extend([Path(pub['CompilerBuildReceiptPath']), Path(pub['CompiledLibraryPath'])])
                entry = destination / 'admission.json'
                save(entry, {'Status': 'SOURCE_BOUND_ORIGINAL_MUTEX795_LIFECYCLE', 'Pin': PIN, 'Subject': subject,
                    'ManagedHost': plan['ManagedHost'], 'ManagedInputs': plan['ManagedInputs'],
                    'NativeImages': [libraries[name] for name in required], 'ObserverBeam': str(original / 'observer.beam'),
                    'BeamA': str(original / 'beam-a.beam'), 'BeamB': str(original / 'beam-b.beam'),
                    'OriginalRawPath': str(original / 'observed.etf'), 'OriginalDeterministicPath': str(original / 'observed-deterministic.etf'),
                    'Inputs': [{'Path': str(p), 'Sha256': sha(p)} for p in sorted(set(inputs))]})
            except Exception as error:
                errors.append(subject + '-admission: ' + repr(error)); continue
            for profile in ('ordinary', 'private-owned'):
                for route in ('ordinary', 'reviewed'):
                    name = subject + '-' + profile + '-' + route
                    directory = destination / (profile + '-' + route); actual = directory / 'actual'
                    command = [plan['DotnetPath'], 'exec', '--fx-version', plan['RuntimeVersion'], plan['ManagedHost'],
                        '--nif-mutex-lifecycle795-child', str(entry), sha(entry), route, profile, str(actual)]
                    good = phase(name, command, directory)
                    try:
                        proof = json.loads((actual / 'completion.json').read_bytes())
                        require(good and proof['Passed'] and proof['WholeRawAndDeterministicEqual'] and proof['ActualNativeBaselinesRestored'], 'Managed lifecycle/raw comparison failed')
                        require(proof['AdmissionSha256'] == sha(entry) and proof['Route'] == route and proof['OwnedProfile'] == (profile == 'private-owned')
                            and proof['Subject'] == subject and proof['NormalizationPerformed'] is False, 'Wrong route/profile/admission/subject')
                        require(proof['FailedInitializationNativeCustodyClosed'] if subject == 'failed' else proof['PhysicalCloseBeforeTransferredMutexUse'], 'Required actual native lifetime control missing')
                        completed.append(name)
                    except Exception as error: errors.append(name + '-validation: ' + repr(error))
    except Exception as error: errors.append('batch: ' + repr(error))
    finally:
        try:
            provider_after = after(); save(out / 'provider-after.json', provider_after)
            require(provider_after == before['Inventory'], 'Original provider changed')
            check(plan)
            for row in libraries.values(): require(sha(row['Path']) == row['Sha256'] and sha(row['BuildReceiptPath']) == row['BuildReceiptSha256'], 'Native publication changed')
        except Exception as error: errors.append('final-guards: ' + repr(error))
        artifacts = [{'Path': str(p.relative_to(out)), 'Sha256': sha(p)} for p in sorted(out.rglob('*')) if p.is_file() and p.name not in ('completion.json', 'artifacts.json')]
        save(out / 'artifacts.json', artifacts)
        if len(completed) != expected_routes: errors.append('Managed routes completed ' + str(len(completed)) + '/' + str(expected_routes))
        save(out / 'completion.json', {'Passed': not errors, 'CompletedRoutes': completed, 'ExpectedRoutes': expected_routes, 'SelectedSubjects': selected_subjects, 'DefaultLifecycleSubjectsSelected': selected_subjects == ['failed', 'upgrade'], 'Stages': stages,
            'Errors': errors, 'ArtifactsSha256': sha(out / 'artifacts.json'), 'NormalizationPerformed': False,
            'FullCompatibilityClaimed': False, 'WholeContractsClosed': 0})
    return 1 if errors else 0

if __name__ == '__main__':
    raise SystemExit(main())
