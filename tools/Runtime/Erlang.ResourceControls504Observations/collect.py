"""Source-bound Linux OTP observations; all independent cases run before failure."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import threading
import time

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL = 'tools/Runtime/Erlang.ResourceControls504Observations'
FLAGS = ['+S', '1:1', '+SDcpu', '1:1', '+SDio', '1', '+A', '1']
KEYS = ['run_queue', 'run_queue_lengths', 'run_queue_lengths_all',
        'total_run_queue_lengths', 'total_run_queue_lengths_all', 'active_tasks',
        'active_tasks_all', 'total_active_tasks', 'total_active_tasks_all', 'run_queues']


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def save(path, value):
    Path(path).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def git(root, *arguments):
    return subprocess.check_output(['git', '-C', str(root), *arguments], timeout=30).decode().strip()


def check_files(root, rows):
    for row in rows:
        path = (root / row['Path']).resolve()
        require(path.is_relative_to(root) and path.is_file(), 'source path: ' + row['Path'])
        raw = path.read_bytes()
        require(row['HashMode'] == 'TextLF', 'declared hash mode')
        require(hashlib.sha256(raw.replace(b'\r\n', b'\n')).hexdigest().upper() == row['Sha256'], 'source seal: ' + row['Path'])


def normal_receipt(path):
    receipt = json.loads(path.read_bytes())
    require(receipt['ExitCode'] == 0 and receipt['ParentReaped'] and receipt['StreamsClosed'], 'process did not complete normally')
    require(not receipt['TerminationRequested'] and not receipt['RemainingGroupKillRequested'], 'process required forced cleanup')
    require(receipt['CleanupFailure'] is None, 'process cleanup failure')
    return receipt


def run_owned(owned, arguments, cwd, environment, prefix, seconds):
    process = None
    failures = []
    try:
        process = owned.OwnedProcess(arguments, cwd, environment, prefix)
        process.wait(time.monotonic() + seconds, threading.Event().is_set)
    except Exception as error:
        failures.append(repr(error))
    finally:
        if process is not None:
            try:
                process.close()
            except Exception as error:
                failures.append('cleanup: ' + repr(error))
    receipt_path = prefix.with_suffix('.process.json')
    if receipt_path.exists():
        try:
            normal_receipt(receipt_path)
        except Exception as error:
            failures.append('receipt: ' + repr(error))
    else:
        failures.append('missing process receipt')
    result = dict(Arguments=arguments, TimeoutSeconds=seconds, Failures=failures,
                  ReceiptSha256=sha(receipt_path) if receipt_path.exists() else None)
    save(prefix.with_suffix('.status.json'), result)
    return result


def integer(value):
    require(value[0] == 'integer' and value[1] >= 0, 'nonnegative queue integer required')
    return value[1]


def check_sample(value):
    require(value[0] == 'tuple' and len(value[1]) == 2, 'sample tuple')
    rows = value[1][1]
    require(rows[0] == 'list' and len(rows[1]) == len(KEYS), 'all ten queue keys')
    result = {}
    for key, row in zip(KEYS, rows[1]):
        require(row[0] == 'tuple' and len(row[1]) == 2 and row[1][0] == ('atom', key), 'key order: ' + key)
        number = row[1][1]
        if key in ['run_queue_lengths', 'active_tasks']:
            require(number[0] == 'list' and len(number[1]) == 2, 'normal plus CPU list')
            result[key] = [integer(item) for item in number[1]]
        elif key in ['run_queue_lengths_all', 'active_tasks_all', 'run_queues']:
            require(number[0] == ('tuple' if key == 'run_queues' else 'list') and len(number[1]) == 3, 'normal plus CPU/IO shape')
            result[key] = [integer(item) for item in number[1]]
        else:
            result[key] = integer(number)
    # Sequential key calls cannot imply simultaneous equality for moving normal queues.
    return result


def main():
    require(sys.platform.startswith('linux') and __debug__, 'Linux non-optimized collector required')
    parser = argparse.ArgumentParser()
    for name in ['repository', 'expected-head', 'plan-sha256', 'otp-source', 'otp-install',
                 'build-receipt', 'build-receipt-sha256', 'output']:
        parser.add_argument('--' + name, required=True)
    args = parser.parse_args()
    root = Path(args.repository).resolve()
    tool = Path(__file__).resolve().parent
    require(tool == root / TOOL, 'collector location')
    require(git(root, 'rev-parse', 'HEAD') == args.expected_head, 'subject commit')
    plan_path = tool / 'source-plan.json'
    require(sha(plan_path) == args.plan_sha256.upper(), 'external source plan seal')
    plan = json.loads(plan_path.read_bytes())
    require(plan['OriginalCommit'] == PIN and plan['Flags'] == FLAGS and plan['QueueKeys'] == KEYS, 'fixed source profile')
    check_files(root, plan['Files'] + plan['Dependencies'])
    owned = load('resource504_owned', root / plan['OwnedProcess'])
    decoder = load('resource504_etf', root / plan['Decoder'])
    source_helper = load('resource504_source', root / plan['SourceIdentity'])
    source = Path(args.otp_source).resolve()
    identity = source_helper.source_identity(source)
    install = Path(args.otp_install).resolve()
    receipt_path = Path(args.build_receipt).resolve()
    require(sha(receipt_path) == args.build_receipt_sha256.upper(), 'external build receipt seal')
    build = json.loads(receipt_path.read_bytes())
    require(build['Status'] == 'PASS_OWNED_PINNED_ORIGINAL_BUILD' and build['Commit'] == PIN, 'original build status/pin')
    require(build['SubjectHead'] == args.expected_head, 'original build subject head')
    require(build['SourceManifestSha256'] == sha(root / plan['BuildManifest']), 'original build retained manifest identity')
    # Recheck the original builder's complete retained source/workflow closure,
    # rather than rebinding its old manifest to this independent collector plan.
    source_helper.verify(root, build['SourceManifestSha256'], args.expected_head)
    require(Path(build['SourceRoot']).resolve() == source and Path(build['RuntimeRoot']).resolve() == install, 'build source/runtime roots')
    require(Path(build['Prefix']).resolve() / 'lib/erlang' == install, 'installed prefix')
    for name, field in [('source-before.json', 'SourceBeforeSha256'), ('source-after.json', 'SourceAfterSha256')]:
        path = receipt_path.parent / name
        require(sha(path) == build[field] and json.loads(path.read_bytes()) == identity, 'full pinned source identity: ' + name)
    require([row['Name'] for row in build['Stages']] == ['configure', 'make', 'install'], 'build stages')
    for row in build['Stages']:
        path = Path(row['ReceiptPath'])
        require(sha(path) == row['ReceiptSha256'], 'build stage receipt seal')
        normal_receipt(path)
        for stream in ['stdout', 'stderr']:
            require(sha(path.parent / (row['Name'] + '.' + stream)) == row[stream.title() + 'Sha256'], 'build stream seal')
    require(len(build['InstalledFiles']) >= 11, 'installed closure')
    for row in build['InstalledFiles']:
        path = (install / row['Path']).resolve()
        require(path.is_relative_to(install) and sha(path) == row['Sha256'], 'installed file seal')
    require(any(path.read_text().strip() == '29.1.1' for path in (install / 'releases').glob('*/OTP_VERSION')), 'installed OTP version')
    headers = install / 'usr/include'
    for name in ['erl_nif.h', 'erl_nif_api_funcs.h']:
        require((headers / name).read_bytes() == (source / 'erts/emulator/beam' / name).read_bytes(), 'installed original header: ' + name)
    output = Path(args.output).resolve()
    require(not output.exists(), 'fresh output required')
    output.mkdir(parents=True)
    save(output / 'inputs.json', dict(PlanSha256=sha(plan_path), BuildReceiptSha256=sha(receipt_path),
        SourceFiles=[dict(Path=row['Path'], ActualRawSha256=sha(root / row['Path'])) for row in plan['Files'] + plan['Dependencies']],
        SubjectHead=args.expected_head, OriginalCommit=PIN, RuntimeRoot=str(install),
        WorkflowRunId=os.environ.get('GITHUB_RUN_ID'), WorkflowRunAttempt=os.environ.get('GITHUB_RUN_ATTEMPT'),
        Flags=FLAGS, SdkCompared=False, WholeApiPromoted=False))
    environment = source_helper.sanitized_environment(os.environ)
    environment['ERLC_USE_SERVER'] = 'false'
    preparation = output / 'preparation'
    preparation.mkdir()
    fixture = preparation / 'queue_statistics504.erl'
    fixture.write_bytes((tool / 'fixtures/queue_statistics504.erl').read_bytes())
    erlc = install / 'bin/erlc'
    erl = install / 'bin/erl'
    compiler = shutil.which('cc')
    erlang_build = run_owned(owned, [str(erlc), '-o', str(preparation), str(fixture)], preparation,
        environment, preparation / 'erlc', 45)
    native_build = dict(Failures=['cc unavailable'])
    if compiler:
        native_build = run_owned(owned, [compiler, '-shared', '-fPIC', '-pthread', '-I', str(headers),
            str(tool / 'fixtures/queue_statistics504.c'), '-o', str(preparation / 'queue_statistics504.so')],
            preparation, environment, preparation / 'cc', 45)
    outcomes = []
    for case in plan['Cases']:
        directory = output / case['Name']
        directory.mkdir()
        failures = []
        if erlang_build['Failures']:
            failures.append('dependency failed: Erlang fixture compilation')
        if case['Native'] and native_build['Failures']:
            failures.append('dependency failed: native fixture compilation')
        if not failures:
            private_environment = dict(environment, RESOURCE504_DIR=str(directory),
                RESOURCE504_LIBRARY=str(preparation / 'queue_statistics504'),
                RESOURCE504_ROOT=str(install), ERL_CRASH_DUMP=str(directory / 'erl_crash.dump'))
            expression = (
                'D=os:getenv("RESOURCE504_DIR"), W=fun(N,V)->ok=file:write_file(filename:join(D,N),term_to_binary(V,[{minor_version,2}])) end, '
                'Root=filename:absname(code:root_dir()), Root=os:getenv("RESOURCE504_ROOT"), '
                'Profile={erlang:system_info(schedulers),erlang:system_info(schedulers_online),erlang:system_info(dirty_cpu_schedulers),erlang:system_info(dirty_io_schedulers)}, '
                'W("profile.etf",{Root,Profile}), {1,1,1,1}=Profile, '
                + ('ok=queue_statistics504:load(os:getenv("RESOURCE504_LIBRARY")), ' if case['Native'] else '')
                + 'R=try queue_statistics504:run(' + case['Name'] + ') of V->{returned,V} catch C:E:S->{raised,C,E,S} end, '
                'W("outcome.etf",R),case R of {returned,_}->halt(0);_->halt(1) end.'
            )
            invocation = run_owned(owned, [str(erl), *FLAGS, '-noshell', '-noinput', '-boot', 'start_clean',
                '-pa', str(preparation), '-eval', expression], directory, private_environment, directory / 'vm', 45)
            failures.extend(invocation['Failures'])
            try:
                outcome = decoder.decode_exact((directory / 'outcome.etf').read_bytes())
                require(outcome[0] == 'tuple' and outcome[1][0] == ('atom', 'returned'), 'original raised (full ETF retained)')
                if case['Name'] == 'debug_flags':
                    require(outcome[1][1] == ('tuple', (('atom', 'debug_flags'), ('list', (('atom', 'true'),) * 15))), '15 unconditional debug_flags results')
                else:
                    samples = {}
                    for path in sorted(directory.glob('*.etf')):
                        if path.name not in ['outcome.etf', 'profile.etf']:
                            samples[path.stem] = check_sample(decoder.decode_exact(path.read_bytes()))
                    require(set(samples) == set(case['Stages']), 'all declared stages captured')
                    if case['Native']:
                        for stage, expected in [('held', [0, 0]), ('queued', [1, 1])]:
                            require(samples[stage]['run_queue_lengths_all'][-2:] == expected, 'held dirty queued counts: ' + stage)
                            require(samples[stage]['active_tasks_all'][-2:] == [number + 1 for number in expected], 'held dirty active counts: ' + stage)
                        require(samples['retired']['run_queue_lengths_all'][-2:] == [0, 0], 'retired dirty queues')
                    save(directory / 'typed-samples.json', samples)
            except Exception as error:
                failures.append('typed guard: ' + repr(error))
        result = dict(Name=case['Name'], Native=case['Native'], Started=not any(item.startswith('dependency failed') for item in failures), Failures=failures)
        outcomes.append(result)
        save(directory / 'case-status.json', result)
    try:
        require(source_helper.source_identity(source) == identity, 'original source changed during collection')
        check_files(root, plan['Files'] + plan['Dependencies'])
        for row in build['InstalledFiles']:
            require(sha(install / row['Path']) == row['Sha256'], 'installed runtime changed')
        for name in ['erl_nif.h', 'erl_nif_api_funcs.h']:
            require((headers / name).read_bytes() == (source / 'erts/emulator/beam' / name).read_bytes(), 'installed original header changed')
    except Exception as error:
        outcomes.append(dict(Name='final-input-recheck', Failures=[repr(error)]))
    failures = [row for row in outcomes if row['Failures']]
    files = [dict(Path=str(path.relative_to(output)), Bytes=path.stat().st_size, Sha256=sha(path))
        for path in sorted(output.rglob('*')) if path.is_file()]
    save(output / 'collection.json', dict(Status='FAIL' if failures else 'PASS_ORIGINAL_ONLY',
        Outcomes=outcomes, Files=files, Preparation=dict(Erlang=erlang_build, Native=native_build),
        SdkCompared=False, WholeApiPromoted=False))
    require(not failures, 'independent cases collected; inspect collection.json for failures')


if __name__ == '__main__':
    main()
