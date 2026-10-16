import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import signal
import socket
import sys
import time
import uuid

sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
VERSION = '29.1.1'
MODULE = 'guest_connected_exit322'
DRIVER = MODULE + '_driver'
SESSION_SECONDS = 35
STARTUP_SECONDS = 10
STOP_REQUESTED = False


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def emit(path, value):
    path.write_bytes((json.dumps(value, indent=2) + '\n').encode('utf-8'))


def load_helper(directory, name):
    spec = importlib.util.spec_from_file_location('guest_exit322_' + name, directory / (name + '.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def seq(value, kind='tuple', count=None):
    require(type(value) is tuple and len(value) == 2 and value[0] == kind, 'Typed ETF sequence')
    require(count is None or len(value[1]) == count, 'Typed ETF sequence length')
    return value[1]


def atom(name):
    return ('atom', name)


def text(value):
    items = seq(value, 'list')
    require(all(type(item) is tuple and len(item) == 2 and item[0] == 'integer' and
                type(item[1]) is int and 0 <= item[1] <= 0x10ffff for item in items), 'Typed character list')
    return ''.join(chr(item[1]) for item in items)


def verify_actual(output, decoder, otp_root, names):
    def decode(path):
        data = path.read_bytes()
        value, consumed = decoder.decode_one(data)
        require(consumed == len(data), 'Trailing raw ETF bytes: ' + str(path))
        return value

    beam_md5 = [hashlib.md5((output / 'ebin' / (name + '.beam')).read_bytes()).digest()
                for name in (MODULE, DRIVER, 'guest_connected_bang322')]
    for role in ('server', 'client'):
        metadata = seq(decode(output / role / 'runtime.etf'), count=10)
        require(metadata[0] == atom(MODULE + '_runtime') and metadata[1] == atom(names[role]), 'Actual node identity')
        require(text(metadata[2]) == '29' and text(metadata[3]) == VERSION, 'Actual node exact OTP version')
        require(Path(text(metadata[4])).resolve() == otp_root, 'Actual node loaded code root')
        require(metadata[5:8] == (('binary', beam_md5[0]), ('binary', beam_md5[1]), ('binary', beam_md5[2])), 'Both actual nodes use exact compiled probe/driver/minimal bang images')
    connection = seq(decode(output / 'client/connection.etf'), count=3)
    require(connection == (atom(names['client']), atom(names['server']), ('list', (atom(names['server']),))), 'Actual private two-node connection before measurement')
    outcome = seq(decode(output / 'client/outcome.etf'), count=2)
    require(outcome[0] == atom('return'), 'Original outcome must return; raw exceptions remain retained')
    result = seq(outcome[1], count=3)
    expected = ['exit_two','exit_three','exit_signal_two','exit_signal_three',
                'duplicate_priority','unknown_option','improper_options','kill_trapped_worker']
    require(result[0] == atom(MODULE), 'Original explicit exit probe marker')
    require(seq(result[1], 'list') == tuple(('tuple', (atom(name), atom('true'))) for name in expected), 'Eight actual original public exit contracts')
    raw = seq(result[2], count=4)
    require(raw[0] == atom('raw'), 'Raw public exit observation marker')
    owner, remote, observations = raw[1:]
    require(owner[0] == remote[0] == 'pid' and owner != remote, 'Two actual distinct PIDs retained')
    require(owner[1] == atom(names['client']) and remote[1] == atom(names['server']), 'Actual owner/worker node binding')
    rows = seq(observations, 'list', count=len(expected))
    raw_outcome = outcome
    for actual, label in zip(rows, expected):
        row = seq(actual, count=7)
        require(row[0] == atom('case_result') and row[1] == atom(label), 'Exact ordered actual case label')
        before, after, outcome, observation, history = row[2:]
        token = seq(before, count=5)
        require(before == after and token[1] == ('integer',42) and token[2] == ('integer',8) and token[4] == ('integer',4), 'Exact full token unchanged immediately after public call')
        if label in ('unknown_option','improper_options'):
            error = seq(outcome,count=4)
            require(error[:3] == (atom('exception'),atom('error'),atom('badarg')), 'Actual invalid options badarg')
            require(error[3][0] == 'list', 'Complete original exception stack retained')
            no_exit = seq(observation,count=2)
            require(no_exit[0] == atom('no_exit') and no_exit[1][0] == 'reference', 'Invalid call identity retained')
            require(seq(history,'list') == (), 'No remote exit effect after ordered original history barrier')
        else:
            require(outcome == ('tuple',(atom('return'),atom('true'))), 'Actual public exit returns true')
            if label == 'kill_trapped_worker':
                down = seq(observation,count=5)
                require(down[0] == atom('down') and down[1][0] == 'reference' and down[1][1] == atom(names['client']) and
                        down[2:] == (atom('process'),remote,atom('killed')), 'Fresh actual DOWN killed despite remote trap_exit')
                require(seq(history,'list') == (), 'Killed worker has no fabricated exit acknowledgement')
            else:
                observed = seq(observation,count=4)
                reason = ('tuple',(atom(label),('binary',bytes([1,2,3]))))
                exit_value = ('tuple',(atom('EXIT'),owner,reason))
                require(observed[0] == atom('observed') and observed[1][0] == 'reference' and
                        observed[2] == exit_value and seq(observed[3],'list') == (), 'Actual remote EXIT and absent public trace token')
                require(seq(history,'list') == (exit_value,), 'Exactly one actual remote EXIT, including duplicate priority options')
    # The explicit kill case already joined killed via its fresh monitor; final cleanup freshly joins already-dead noproc.
    joined = ('tuple',(atom('return'),('tuple',(atom('worker_joined'),atom('noproc')))))
    require(decode(output/'client/worker-cleanup.etf') == joined, 'Actual final fresh monitor joins already-killed worker')
    stopped = ('tuple',(atom('return'),atom('server_joined_protocol')))
    require(decode(output/'client/server-cleanup.etf') == stopped and decode(output/'server/shutdown.etf') == stopped, 'Both actual server shutdown handshake records')
    return {'OrderedOriginalContracts':expected,'ActualRawOutcome':repr(raw_outcome),'AllEightRawCaseRows':repr(rows),
            'ExactTokenComparisonBeforeAnyReceive':True,'PublicExitHasNoRemoteToken':True,
            'DuplicatePriorityAcceptedOnce':True,'UnknownAndImproperNoRemoteExit':True,
            'KillFreshDownKilledWhileRemoteTraps':True,'ActualWorkerCleanup':repr(joined),
            'NativeControlWireBytesCaptured':False,'SdkExecuted':False,'SdkCompared':False,
            'PriorityDeliveryOrderingObserved':False,'PressureOrSchedulerPhaseObserved':False,
            'Roles':['original public exit/2,3 and exit_signal/2,3 on a real trapping original remote worker',
                     'full raw sender token before/after call precedes every receive/probe acknowledgement',
                     'SDK HOST/controller packets and live-peer/network equivalence are separate unclaimed roles']}





def main():
    global STOP_REQUESTED
    parser = argparse.ArgumentParser()
    parser.add_argument('--otp-source', required=True, type=Path)
    parser.add_argument('--otp-root', required=True, type=Path)
    parser.add_argument('--build-evidence', required=True, type=Path)
    parser.add_argument('--approved-source-plan-sha256', required=True)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    require(sys.platform == 'linux' and os.uname().machine == 'x86_64', 'Reviewed foreground process-group collector requires Linux x86_64')
    source = Path(__file__).resolve().parent
    plan_bytes = (source / 'source-plan.json').read_bytes()
    require(sha(plan_bytes) == args.approved_source_plan_sha256.upper(), 'Reviewed exact source-plan bytes')
    plan = json.loads(plan_bytes)
    require(plan['PinnedCommit'] == PIN and plan['ExpectedOriginalNodes'] == 2, 'Original source pin/two-node scope')
    for row in plan['Sources']:
        path = (source / row['Path']).resolve()
        require(path.is_relative_to(source) and sha(path.read_bytes()) == row['Sha256'], 'Reviewed source/helper bytes: ' + row['Path'])
    # No project-local executable helper is imported until its exact source bytes have passed the plan.
    execute_single = load_helper(source, 'process_capture').execute

    def request_stop(_number, _frame):
        global STOP_REQUESTED
        STOP_REQUESTED = True

    signal.signal(signal.SIGTERM, request_stop)
    signal.signal(signal.SIGINT, request_stop)

    def execute(*arguments, **options):
        require(not STOP_REQUESTED, 'Collection cancellation before bounded single process')
        result = execute_single(*arguments, **options)
        require(not STOP_REQUESTED, 'Collection cancellation after joined bounded single process')
        return result
    owned = load_helper(source, 'owned_process').OwnedProcess
    decoder = load_helper(source, 'passive_etf')
    output = args.output.resolve()
    require(not output.exists(), 'Fresh evidence output required')
    output.mkdir(parents=True)
    for name in ('sources', 'ebin', 'server', 'client'):
        (output / name).mkdir()
    (output / 'source-plan.json').write_bytes(plan_bytes)
    for row in plan['Sources']:
        shutil.copyfile(source / row['Path'], output / 'sources' / row['Path'])
    environment = dict(os.environ)
    for name in ('ERL_AFLAGS', 'ERL_FLAGS', 'ERL_ZFLAGS', 'ERL_EPMD_PORT', 'ERL_EPMD_ADDRESS',
                 'ERL_EPMD_EXECUTABLE', 'ERL_INETRC', 'LD_PRELOAD', 'LD_LIBRARY_PATH'):
        environment.pop(name, None)
    environment['ERLC_USE_SERVER'] = 'false'
    environment['PYTHONDONTWRITEBYTECODE'] = '1'
    children = []
    primary = None
    cleanup_failures = []
    assertions = None
    tools = []
    try:
        otp_source = args.otp_source.resolve()
        otp_root = args.otp_root.resolve()
        head = execute(['git', '-C', str(otp_source), 'rev-parse', 'HEAD'], output, environment, output / 'source-head', timeout=10).stdout.decode().strip()
        dirty = execute(['git', '-C', str(otp_source), 'status', '--porcelain', '--untracked-files=no'], output, environment, output / 'source-status', timeout=10).stdout.strip()
        require(head == PIN and not dirty, 'Exact clean tracked original source pin')
        for index, row in enumerate(plan['PinnedSources']):
            raw = execute(['git', '-C', str(otp_source), 'show', PIN + ':' + row['Path']], output, environment, output / ('pin-' + str(index)), timeout=10).stdout
            require(sha(raw) == row['Sha256'], 'Exact pinned original reference blob')
        evidence_bytes = args.build_evidence.read_bytes()
        evidence = json.loads(evidence_bytes)
        require(evidence['SourceCommit'] == PIN and evidence['SourceTag'] == 'OTP-29.1.1' and evidence['SourceBuildCompleted'] is True, 'Pinned source-build evidence')
        require(Path(evidence['InstalledOtpRoot']).resolve() == otp_root, 'Source-built exact installed root')
        build_log = Path(evidence['SourceBuildLog']).read_bytes()
        require(sha(build_log) == evidence['SourceBuildLogSha256'], 'Actual pinned source-build log')
        require((otp_root / 'releases/29/OTP_VERSION').read_text().strip() == VERSION, 'Exact installed OTP version')
        (output / 'source-build-evidence.json').write_bytes(evidence_bytes)
        (output / 'source-build.log').write_bytes(build_log)
        binaries = [otp_root / 'bin' / name for name in ('erl', 'erlc', 'epmd')]
        emulators = list(otp_root.glob('erts-*/bin/beam.smp'))
        require(len(emulators) == 1 and all(path.is_file() for path in binaries), 'Actual source-built launchers/private epmd/one beam.smp')
        tools = [{'Path': str(path.resolve()), 'Bytes': path.stat().st_size, 'Sha256': sha(path.read_bytes())} for path in binaries + emulators]
        erl, erlc, epmd = binaries
        execute([str(erlc), '-Werror', '+debug_info', '-o', str(output / 'ebin'),
                 str(output / 'sources' / (MODULE + '.erl')), str(output / 'sources' / (DRIVER + '.erl')), str(output / 'sources/guest_connected_bang322.erl')],
                output, environment, output / 'compile', timeout=30)
        suffix = uuid.uuid4().hex[:12]
        names = {'server': 'ge322_server_' + suffix + '@127.0.0.1', 'client': 'ge322_client_' + suffix + '@127.0.0.1'}
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as reservation:
            reservation.bind(('127.0.0.1', 0))
            port = reservation.getsockname()[1]
        environment['ERL_EPMD_PORT'] = str(port)
        deadline = time.monotonic() + SESSION_SECONDS
        mapper = owned([str(epmd), '-address', '127.0.0.1', '-port', str(port)], output, environment, output / 'epmd')
        children.append(mapper)
        startup = min(deadline, time.monotonic() + STARTUP_SECONDS)
        while True:
            require(not STOP_REQUESTED, 'Collection cancellation during private epmd startup')
            require(mapper.poll() is None, 'Owned private epmd remains alive during startup')
            try:
                with socket.create_connection(('127.0.0.1', port), timeout=0.1):
                    break
            except OSError:
                require(time.monotonic() < startup, 'Private epmd startup deadline')
                time.sleep(0.025)
        common = [str(erl), '+S', '1:1', '+SDcpu', '1', '+SDio', '1', '+A', '1', '-noshell', '-noinput',
                  '-start_epmd', 'false', '-setcookie', 'guest_exit322_local_observation',
                  '-kernel', 'inet_dist_use_interface', '{127,0,0,1}', '-pa', str(output / 'ebin')]
        server_environment = dict(environment, GUEST_CONNECTED_EXIT_OUTPUT=str(output / 'server'))
        server = owned(common + ['-name', names['server'], '-eval', DRIVER + ':server(),halt(0).'], output, server_environment, output / 'server/process')
        children.append(server)
        startup = min(deadline, time.monotonic() + STARTUP_SECONDS)
        while not (output / 'server/runtime.etf').is_file():
            require(not STOP_REQUESTED, 'Collection cancellation during original server startup')
            require(server.poll() is None, 'Original server exited before metadata readiness')
            require(time.monotonic() < startup, 'Original server metadata startup deadline')
            time.sleep(0.025)
        client_environment = dict(environment, GUEST_CONNECTED_EXIT_OUTPUT=str(output / 'client'), GUEST_CONNECTED_EXIT_REMOTE=names['server'])
        client = owned(common + ['-name', names['client'], '-eval', DRIVER + ':client().'], output, client_environment, output / 'client/process')
        children.append(client)
        client.wait(deadline, lambda: STOP_REQUESTED)
        server.wait(deadline, lambda: STOP_REQUESTED)
        assertions = verify_actual(output, decoder, otp_root, names)
    except BaseException as error:
        primary = repr(error)
    finally:
        for child in reversed(children):
            try:
                child.close()
            except BaseException as error:
                cleanup_failures.append(repr(error))
        members = []
        for path in sorted(output.rglob('*')):
            if path.is_file():
                members.append({'Path': path.relative_to(output).as_posix(), 'Bytes': path.stat().st_size, 'Sha256': sha(path.read_bytes())})
        report = {'Status': 'PASS_ACTUAL_ORIGINAL_TWO_NODE_GUEST_CONNECTED_EXIT_OBSERVATION' if primary is None and not cleanup_failures else 'FAIL_ORIGINAL_TWO_NODE_GUEST_CONNECTED_EXIT_OBSERVATION',
                  'PinnedCommit': PIN, 'SourcePlanSha256': sha(plan_bytes), 'PrimaryFailure': primary,
                  'CleanupFailures': cleanup_failures, 'ActualAssertions': assertions, 'Tools': tools,
                  'SdkExecuted': False, 'SdkCompared': False, 'NativeControlWireBytesCaptured': False,
                  'NativeNetworkingUsed': bool(children), 'PublicExitTokenContractsObserved': assertions is not None, 'PriorityDeliveryOrderingObserved': False, 'Files': members}
        emit(output / 'report.json', report)
    print(json.dumps({'Status': report['Status'], 'PrimaryFailure': primary, 'CleanupFailures': cleanup_failures}))
    return 0 if primary is None and not cleanup_failures else 1


if __name__ == '__main__':
    sys.exit(main())
