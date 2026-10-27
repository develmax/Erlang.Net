"""One source-bound observer batch. Prepare-only unless explicitly executed."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time

sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL = 'tools/Runtime/Erlang.OwnedGuest506Observations'
HELPERS = 'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts'


def sha(path, mode='Raw'):
    digest = hashlib.sha256()
    if mode == 'TextLF':
        digest.update(Path(path).read_bytes().replace(b'\r\n', b'\n'))
    else:
        with Path(path).open('rb') as stream:
            for chunk in iter(lambda: stream.read(1024 * 1024), b''):
                digest.update(chunk)
    return digest.hexdigest().upper()


def save(path, value):
    Path(path).write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')


def require(value, why):
    if not value:
        raise RuntimeError(why)


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def files(root):
    """Linux build links are retained as links, with confined target identities."""
    rows = []
    for directory, dirs, names in os.walk(root, followlinks=False):
        for name in sorted([*dirs, *names]):
            path = Path(directory)/name
            if path.is_symlink():
                require(path.resolve().is_relative_to(root), 'Escaping provider link: '+str(path))
                rows.append(dict(Path=path.relative_to(root).as_posix(), Link=os.readlink(path),
                    TargetSha256=sha(path) if path.is_file() else None))
            elif path.is_file():
                rows.append(dict(Path=path.relative_to(root).as_posix(), Sha256=sha(path)))
    return sorted(rows, key=lambda row: row['Path'])


def check_inputs(repo, tool, plan):
    for row in plan['Inputs']:
        root = tool if row['Scope'] == 'Tool' else repo
        path = (root/row['Path']).resolve()
        require(path.is_relative_to(root) and path.is_file(), 'Invalid sealed input: '+row['Path'])
        require(sha(path, row['HashMode']) == row['Sha256'], 'Source seal differs: '+row['Path'])


def provider(args, repo):
    proof = args.official_admission if args.official_admission else args.build_receipt
    require(proof is not None and args.admission_sha256, 'Explicit admitted provider path and SHA required')
    proof = proof.resolve()
    require(sha(proof) == args.admission_sha256.upper(), 'Provider admission SHA differs')
    admission = json.loads(proof.read_bytes())
    related = []
    if args.official_admission:
        require(os.name == 'nt', 'Official Windows admission requires Windows')
        require(admission['SourceCommit'] == PIN and admission['ReleaseTag'] == 'OTP-29.1.1', 'Original release pin')
        require(admission['EveryExtractedFileMatchesArchive'], 'Missing archive closure admission')
        installed = Path(admission['InstalledOtpRoot']).resolve()
        index = Path(admission['ExecutionManifestPath']).resolve()
        require(sha(index) == admission['ExecutionManifestSha256'], 'Provider inventory admission differs')
        expected = {row['Path'].replace('\\', '/'): row['Sha256'] for row in json.loads(index.read_bytes())}
        inventory = load('owned506_inventory', repo/HELPERS/'bounded_inventory.py')
        require(len(expected) == 6427 and inventory.inventory(installed) == expected, 'Official provider closure differs')
        before = inventory.inventory(installed)
        related.append(dict(Path=str(index), Sha256=sha(index)))
        after = lambda: inventory.inventory(installed)
        executable = installed/'bin/erl.exe'
        profile = 'OFFICIAL_WINDOWS_ARCHIVE_FULL_6427'
    else:
        require(sys.platform.startswith('linux'), 'Pinned source-build profile requires Linux')
        require(args.otp_source is not None, 'Pinned source root required')
        require(admission['Status'] == 'PASS_OWNED_PINNED_ORIGINAL_BUILD' and admission['Commit'] == PIN,
                'Build provider is not pinned successful original')
        require(admission['SubjectHead'] == args.expected_head, 'Build receipt belongs to another subject head')
        source = args.otp_source.resolve()
        require(Path(admission['SourceRoot']).resolve() == source, 'Build source path differs')
        source_tools = load('owned506_source', repo/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py')
        source_tools.verify(repo, admission['SourceManifestSha256'], args.expected_head)
        identity = source_tools.source_identity(source)
        for name in ['source-before.json', 'source-after.json']:
            path = proof.parent/name
            declared = admission['SourceBeforeSha256' if 'before' in name else 'SourceAfterSha256']
            require(sha(path) == declared and json.loads(path.read_bytes()) == identity, 'Build source identity differs')
            related.append(dict(Path=str(path), Sha256=sha(path)))
        for stage in admission['Stages']:
            path = Path(stage['ReceiptPath'])
            require(sha(path) == stage['ReceiptSha256'], 'Build process receipt differs')
            source_tools.require_normal_receipt(json.loads(path.read_bytes()))
            related.append(dict(Path=str(path), Sha256=sha(path)))
            for stream in ['Stdout', 'Stderr']:
                log = path.parent/(stage['Name']+'.'+stream.lower())
                require(sha(log) == stage[stream+'Sha256'], 'Build stream receipt differs')
                related.append(dict(Path=str(log), Sha256=sha(log)))
        installed = Path(admission['RuntimeRoot']).resolve()
        for row in admission['InstalledFiles']:
            path = (installed/row['Path']).resolve()
            require(path.is_relative_to(installed) and sha(path) == row['Sha256'], 'Build binary admission differs')
        before = files(installed)
        def after():
            require(source_tools.source_identity(source) == identity, 'Pinned original source changed')
            return files(installed)
        executable = installed/'bin/erl'
        profile = 'PINNED_LINUX_BUILD_RECEIPT_KEY_BINARIES_AND_FULL_BEFORE_AFTER'
    require(executable.is_file(), 'Admitted Erlang executable absent')
    binary_names = {'erl', 'erl.exe', 'erlc', 'erlc.exe', 'beam.smp', 'beam.smp.dll', 'erlexec', 'erlexec.dll',
                    'proc_lib.beam', 'erlang.beam', 'compile.beam', 'init.beam', 'erl_eval.beam'}
    binaries = [dict(Path=path.relative_to(installed).as_posix(), Sha256=sha(path), Bytes=path.stat().st_size)
                for path in sorted(installed.rglob('*')) if path.is_file() and path.name in binary_names]
    return installed, executable, dict(AdmissionPath=str(proof), AdmissionSha256=sha(proof),
        Profile=profile, RelatedAdmissions=related, Binaries=binaries, Inventory=before), after


def execute(repo, command, directory, environment):
    if os.name == 'nt':
        owned = load('owned506_windows', repo/HELPERS/'owned.py')
        return owned.owned(command, directory, 'original', 180, environment, str(directory))
    owned = load('owned506_linux', repo/'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py')
    process = None
    errors = []
    try:
        process = owned.OwnedProcess(command, directory, environment, directory/'original')
        process.wait(time.monotonic()+180, lambda: False)
    except Exception as error:
        errors.append(repr(error))
    finally:
        if process is not None:
            try:
                process.close()
            except Exception as error:
                errors.append('cleanup: '+repr(error))
    path = directory/'original.process.json'
    require(path.is_file(), 'Owned process receipt missing; '+repr(errors))
    receipt = json.loads(path.read_bytes())
    require(receipt['ParentReaped'] and receipt['StreamsClosed'] and receipt['CleanupFailure'] is None,
            'Physical process cleanup failed; '+repr(errors))
    require(not errors and receipt['ExitCode'] == 0, 'Original execution failed; '+repr(errors))
    return receipt


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--expected-head')
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--execute-original', action='store_true')
    parser.add_argument('--check-paired-golden', action='store_true')
    sources = parser.add_mutually_exclusive_group()
    sources.add_argument('--official-admission', type=Path)
    sources.add_argument('--build-receipt', type=Path)
    parser.add_argument('--admission-sha256')
    parser.add_argument('--otp-source', type=Path)
    parser.add_argument('--job', action='append')
    args = parser.parse_args()
    repo, tool, out = args.repository.resolve(), Path(__file__).resolve().parent, args.output.resolve()
    require(tool == repo/TOOL, 'Collector must be integrated at the declared tracked tool path')
    require(out.is_relative_to(repo/'artifacts') and not out.exists(), 'Fresh artifact output required')
    plan_path = tool/'source-plan.json'
    require(sha(plan_path) == args.plan_sha256.upper(), 'Reviewed source plan differs')
    plan = json.loads(plan_path.read_bytes())
    require(plan['PinnedCommit'] == PIN, 'Plan pin differs')
    check_inputs(repo, tool, plan)
    jobs = json.loads((tool/'jobs.json').read_bytes())['Jobs']
    selected = args.job or list(jobs)
    require(selected and len(selected) == len(set(selected)) and all(name in jobs for name in selected), 'Invalid/duplicate jobs')
    for name in selected:
        row = jobs[name]
        require(row['Module'].replace('_', '').isalnum(), 'Unsafe module')
        for fixture in row['Fixtures']:
            require(Path(fixture).name == fixture and (tool/'fixtures'/fixture).is_file(), 'Invalid fixture')
    out.mkdir(parents=True)
    save(out/'launch-plan.json', dict(Prepared=True, ExecuteOriginal=args.execute_original, Jobs=selected,
        SourcePlanSha256=sha(plan_path), WholeContractsClosed=0, ExpectedHead=args.expected_head))
    if not args.execute_original:
        print(json.dumps(dict(Prepared=True, Executed=False, WholeContractsClosed=0)))
        return
    require(not args.check_paired_golden or 'paired-observable' in selected,
            'Historical paired-golden comparison requested without the paired job')
    require(args.expected_head and subprocess.check_output(['git', '-C', str(repo), 'rev-parse', 'HEAD'], timeout=30).decode().strip() == args.expected_head,
            'Exact repository head admission required')
    try:
        installed, executable, before, provider_after = provider(args, repo)
    except Exception as error:
        save(out/'preflight-failure.json', dict(Error=repr(error), VmStarted=False))
        raise
    save(out/'provider-before.json', before)
    errors, observations, phases, golden_comparisons = [], [], {}, []
    env = dict(os.environ)
    for key in ['ERL_FLAGS', 'ERL_AFLAGS', 'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS', 'ERLC_EMULATOR',
                'ERLC_USE_SERVER', 'ERLC_SERVER_ID', 'ERLC_CONFIGURATION', 'ERL_ROOTDIR']:
        env.pop(key, None)
    env['CONTRACT506_ORIGINAL_ROOT'] = str(installed)
    env['PYTHONDONTWRITEBYTECODE'] = '1'
    try:
        for job in selected:
            directory = out/job
            try:
                directory.mkdir()
                row = jobs[job]
                for name in row['Fixtures']:
                    shutil.copyfile(tool/'fixtures'/name, directory/name)
                job_env = dict(env, ERL_CRASH_DUMP=str(directory/'erl_crash.dump'), ERL_CRASH_DUMP_SECONDS='15')
                paired = ('ok=file:write_file("observed-deterministic.etf",term_to_binary(T,[deterministic,{minor_version,2}])),'
                          'W("witnesses.etf",get(paired_witness506)),') if row.get('PairedProfiles') else ''
                expression = (
                    'W=fun(N,T)->ok=file:write_file(N,term_to_binary(T,[{minor_version,2}])) end,'
                    'Expected=filename:absname(os:getenv("CONTRACT506_ORIGINAL_ROOT")),'
                    'Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
                    'W("metadata.etf",{Expected,erlang:system_info(version),erlang:system_info(system_architecture),'
                    'erlang:system_info(wordsize),erlang:system_info(schedulers),code:which(proc_lib)}),'
                    'Files=filelib:wildcard("*.erl"),Compiled=[compile:file(F,[return_errors,return_warnings])||F<-Files],'
                    'W("compile.etf",Compiled),true=lists:all(fun({ok,_,_})->true;({ok,_})->true;(_)->false end,Compiled),'
                    'Modules=[case Item of {ok,M,_}->M;{ok,M}->M end||Item<-Compiled],'
                    'W("modules.etf",[{M,code:which(M),M:module_info(md5)}||M<-Modules]),'
                    'try '+row['Module']+':run() of T->W("observed.etf",T),'+paired+'halt(0) '
                    'catch C:R:S->W("entry-failure.etf",{C,R,S}),halt(77) end.')
                command = [str(executable), '+S', '1:1', '+A', '1', '-noshell', '-noinput', '-boot', 'no_dot_erlang',
                           '-pa', str(directory), '-eval', expression]
                phases[job] = execute(repo, command, directory, job_env)
                observed = directory/'observed.etf'
                require(observed.is_file(), 'No raw observation')
                observations.append(dict(Job=job, Bytes=observed.stat().st_size, Sha256=sha(observed)))
                if args.check_paired_golden and row.get('PairedProfiles'):
                    mismatches = []
                    for name in ['observed.etf', 'observed-deterministic.etf']:
                        actual, golden = directory/name, tool/'golden/paired335'/name
                        equal = actual.read_bytes() == golden.read_bytes()
                        golden_comparisons.append(dict(Job=job, Profile=name,
                            ActualSha256=sha(actual), GoldenSha256=sha(golden), Equal=equal))
                        if not equal:
                            mismatches.append(name)
                    require(not mismatches, 'Whole historical original protocol differs: '+repr(mismatches))
            except Exception as error:
                errors.append(job+': '+repr(error))
            finally:
                try:
                    if directory.is_dir():
                        save(directory/'artifacts.json', dict(CrashDumpRequestedPath=str(directory/'erl_crash.dump'),
                            Files=[dict(Name=p.name, Bytes=p.stat().st_size, Sha256=sha(p))
                                   for p in sorted(directory.iterdir()) if p.is_file()]))
                except Exception as error:
                    errors.append(job+' artifact inventory: '+repr(error))
    finally:
        try:
            require(provider_after() == before['Inventory'], 'Provider closure changed')
            require(sha(before['AdmissionPath']) == before['AdmissionSha256'], 'Provider admission changed')
            for row in before['RelatedAdmissions']:
                require(sha(row['Path']) == row['Sha256'], 'Related provider receipt changed')
            check_inputs(repo, tool, plan)
            require(sha(plan_path) == args.plan_sha256.upper(), 'Source plan changed')
            save(out/'provider-after.json', dict(ProviderEqual=True, AdmissionSha256=sha(before['AdmissionPath']),
                RelatedAdmissions=before['RelatedAdmissions']))
        except Exception as error:
            errors.append('postflight: '+repr(error))
            save(out/'provider-after.json', dict(ProviderEqual=False, Error=repr(error)))
        save(out/'completion.json', dict(ObservationAcquisitionPassed=not errors, Errors=errors, Phases=phases,
            Observations=observations, SourcePlanSha256=args.plan_sha256.upper(), PinnedCommit=PIN,
            OriginalOnly=True, WholeContractsClosed=0,
            ComparisonPerformed=bool(golden_comparisons),
            OriginalGoldenComparisonRequested=args.check_paired_golden,
            OriginalGoldenComparisonPerformed=bool(golden_comparisons),
            OriginalGoldenComparisons=golden_comparisons,
            ManagedComparisonPerformed=False, ManagedExecutionClaimed=False,
            RawEtfRewritten=False))
    require(not errors, '; '.join(errors))


if __name__ == '__main__':
    main()
