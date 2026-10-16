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
import traceback
import uuid

sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
VERSION = '29.1.1'
MODULES = ('distribution_control_wait323', 'distribution_control_wait323_driver')
CASES = ('send', 'link')
ACTIVE_SECONDS = 180
SESSION_SECONDS = 45
STOP_SECONDS = 20
STARTUP_SECONDS = 10
FILE_BYTES = 4 * 1024 * 1024
TOTAL_BYTES = 32 * 1024 * 1024
STOP_REQUESTED = False


def require(value, message):
    if not value:
        raise RuntimeError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest().upper()


def emit(path, value):
    raw = (json.dumps(value, indent=2) + '\n').encode('utf-8')
    temporary = path.with_name(path.name + '.tmp')
    temporary.write_bytes(raw)
    temporary.replace(path)


def helper(source, name):
    spec = importlib.util.spec_from_file_location('control_wait323_' + name, source / (name + '.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def atom(value):
    return ('atom', value)


def integer(value):
    return ('integer', value)


def seq(value, kind='tuple', count=None):
    require(type(value) is tuple and len(value) == 2 and value[0] == kind, 'Typed ETF sequence')
    require(count is None or len(value[1]) == count, 'Typed ETF exact sequence cardinality')
    return value[1]


def text(value):
    items = seq(value, 'list')
    require(all(type(item) is tuple and len(item) == 2 and item[0] == 'integer' and
                type(item[1]) is int and 0 <= item[1] <= 0x10ffff for item in items), 'ETF character list')
    return ''.join(chr(item[1]) for item in items)


def decode(path, decoder):
    require(path.stat().st_size <= FILE_BYTES, 'Per-ETF byte cap')
    raw = path.read_bytes()
    value, used = decoder.decode_one(raw)
    require(used == len(raw), 'No trailing ETF bytes')
    return value


def node_of(value, kind, name):
    require(type(value) is tuple and value[0] == kind and value[1] == atom(name), 'Actual typed ' + kind + ' node binding')


def verify_metadata(path, decoder, role, names, root, images, owned_pid):
    value = decode(path, decoder)
    row = seq(value, count=12)
    require(row[:3] == (atom('control_wait323_runtime'), atom(role), atom(names[role])), 'Actual role/node identity')
    require(text(row[3]) == '29' and text(row[4]) == VERSION and Path(text(row[5])).resolve() == root,
            'Exact actual loaded original version/root')
    loaded = seq(row[6], 'list', count=2)
    expected = tuple(('tuple', (atom(name), ('binary', images[name]))) for name in MODULES)
    require(loaded == expected, 'Every actual node loaded both actual source-compiled image MD5s')
    require(text(row[9]) == str(owned_pid), 'Owned foreground parent equals actual VM OS PID')
    require(row[10] == integer(1024) and row[11] == integer(60), 'Actual busy limit and unchanged default net tick')
    require(bool(text(row[7])) and bool(text(row[8])), 'Actual system version and architecture retained')
    return {'Raw': repr(value), 'Sha256': sha(path.read_bytes()), 'OwnedPid': owned_pid}


def verify_observation(value, mode, names):
    row = seq(value, count=8)
    require(row[:2] == (atom('observation'), atom(mode)), 'Actual scenario identity')
    identities = seq(row[2], count=8)
    require(identities[0] == atom('identities'), 'Actual identity record')
    owner, target, worker, filler, killer, bctrl, cctrl = identities[1:]
    for pid in (owner, target, filler):
        node_of(pid, 'pid', names['a'])
    node_of(worker, 'pid', names['b'])
    node_of(killer, 'pid', names['c'])
    require(len({owner, target, filler}) == 3, 'Observer/guest/filler are distinct actual processes')
    node_of(bctrl, 'port', names['a'])
    node_of(cctrl, 'port', names['a'])
    require(bctrl != cctrl, 'Distinct A-B and A-C real default TCP controllers')
    prepared = seq(row[3], count=3)
    require(prepared[0] == atom('prepared') and prepared[2] == ('tuple', (atom('status'), atom('suspended'))),
            'Actual filler suspended after observed busy/quiet barrier')
    initial_links = ('list', (worker,)) if mode == 'send' else ('list', ())
    require(prepared[1] == ('list', (('tuple', (atom('links'), initial_links)),
                                   ('tuple', (atom('trap_exit'), atom('true'))),
                                   ('tuple', (atom('async_dist'), atom('false'))))), 'Actual guest prepared profile')
    busy = seq(row[4], count=4)
    event = ('tuple', (atom('monitor'), target, atom('busy_dist_port'), bctrl))
    require(busy[:2] == (atom('busy'), event), 'TARGET-specific actual busy event on exact B controller')
    events = seq(busy[2], 'list')
    require(1 <= len(events) <= 2048, 'Bounded retained real busy events')
    decoded_events = [seq(item, count=2) for item in events]
    require(all(type(t[1]) is tuple and t[1][0] == 'integer' for t in decoded_events), 'Raw event monotonic timestamps')
    require(any(t[0] == event for t in decoded_events), 'Raw target event is actually retained')
    require(any(seq(t[0], count=4)[1] == filler for t in decoded_events), 'Separate filler event retained')
    progress = seq(busy[3], 'list')
    require(len(progress) <= 1024, 'Bounded filler progress')
    for index, item in enumerate(progress, 1):
        p = seq(item, count=4)
        require(p[0] == atom('progress') and p[2] == filler and p[3] == integer(index), 'Exact ordered filler progress')
        node_of(p[1], 'reference', names['a'])
    query = seq(row[5], count=3)
    require(query[0] == atom('query'), 'Actual public query record')
    node_of(query[1], 'reference', names['a'])
    returned = seq(query[2], count=2)
    require(returned[0] == atom('return'), 'Actual query succeeded; no normalized exception')
    info, started, ended = seq(returned[1], count=3)
    items = seq(info, 'list', count=4)
    require(items[:3] == (('tuple', (atom('links'), ('list', (worker,)))),
                          ('tuple', (atom('status'), atom('suspended'))),
                          ('tuple', (atom('async_dist'), atom('false')))), 'Exact owner query links/status/async flag')
    current = seq(items[3], count=2)
    function = seq(current[1], count=3)
    require(current[0] == atom('current_function') and function[0][0] == function[1][0] == 'atom' and
            function[2][0] == 'integer' and 0 <= function[2][1] <= 255, 'Full actual current function retained')
    require(started[0] == ended[0] == 'integer' and ended[1] >= started[1], 'Query monotonic bounds')
    kill = seq(row[6], count=4)
    require(kill[0] == atom('kill'), 'Actual remote kill record')
    node_of(kill[1], 'reference', names['a'])
    require(kill[1] != query[1], 'Distinct fresh query/kill references')
    require(kill[2] == ('tuple', (atom('kill_ack'), kill[1], killer, ('tuple', (atom('return'), atom('true'))))),
            'Actual C public remote exit returns true')
    down = seq(kill[3], count=5)
    node_of(down[1], 'reference', names['a'])
    require(down[1] not in (query[1], kill[1]) and down[0] == atom('DOWN') and
            down[2:] == (atom('process'), target, atom('killed')), 'Fresh actual target DOWN killed, not cleanup noproc')
    require(row[7] == ('tuple', (atom('postcall'), atom('false'))), 'No target post-call return before killed DOWN')
    return {'Mode': mode, 'RawObservation': repr(value), 'ActualTargetBusy': repr(event),
            'ActualPublicQuery': repr(query), 'ActualRemoteKillAndDown': repr(kill),
            'NativeWireCaptured': False, 'SdkExecuted': False, 'TicketTurnEquivalent': False}


def fingerprint(child):
    require(child.poll() is None, 'Owned B remains alive')
    pid = child.process.pid
    require(os.getpgid(pid) == pid, 'Only exact owned foreground process group')
    raw = Path('/proc/' + str(pid) + '/stat').read_text()
    fields = raw[raw.rfind(')') + 2:].split()
    return {'Pid': pid, 'Pgid': os.getpgid(pid), 'State': fields[0], 'StartTime': fields[19]}


def verify_timeline(journal):
    phases = [item['Phase'] for item in journal]
    required = ['all-role-metadata', 'before-SIGSTOP', 'SIGSTOP-confirmed-T', 'stop-ack',
                'query-completed-while-stopped', 'proof-query-and-killed-before-resume', 'SIGCONT', 'resume-ack']
    positions = [phases.index(name) for name in required]
    require(all(phases.count(name) == 1 for name in required), 'Single actual stop/query/killed/resume phase')
    require(positions == sorted(positions) and len(set(positions)) == len(required), 'Exact stop/query/killed/resume phase order')
    require(all(journal[index]['ParentMonotonicNs'] <= journal[index + 1]['ParentMonotonicNs'] for index in range(len(journal) - 1)), 'Parent journal monotonic order')
    confirmed = journal[positions[2]]['Identity']
    require(confirmed['State'] == 'T', 'Actual stop confirmed')
    queried = journal[positions[4]]['Identity']
    proof = journal[positions[5]]['StoppedB']
    continued = journal[positions[6]]
    for entry in (queried, proof, continued['Before'], continued['After']):
        same_process(entry, confirmed)
    require(queried['State'] == proof['State'] == continued['Before']['State'] == 'T' and continued['After']['State'] != 'T',
            'B remains stopped through actual query/killed proof and only then resumes')


def verify_cleanup(case, decoder, mode, names, proof_value):
    ids = seq(seq(proof_value, count=8)[2], count=8)
    local = decode(case / 'a/local-worker-cleanup.etf', decoder)
    rows = seq(local, 'list', count=3)
    expected = ('query_worker', 'target', 'filler')
    for name, raw in zip(expected, rows):
        row = seq(raw, count=2)
        returned = seq(row[1], count=2)
        joined = seq(returned[1], count=4)
        require(row[0] == atom(name) and returned[0] == atom('return') and joined[0] == atom('joined'), 'Every actual A worker freshly joined')
        node_of(joined[1], 'pid', names['a'])
        node_of(joined[2], 'reference', names['a'])
        if name == 'target':
            require(joined[1] == ids[2] and joined[3] == atom('noproc'), 'Target cleanup noproc distinct from actual prior killed DOWN')
        if name == 'filler':
            require(joined[1] == ids[4], 'Actual filler freshly joined')
    a = seq(decode(case / 'a/role-outcome.etf', decoder), count=2)
    body = seq(a[1], count=3)
    require(a[0] == atom('return') and body[0] == atom('probe') and body[1] == ('tuple', (atom('return'), proof_value)) and
            body[2] == ('tuple', (atom('return'), ('tuple', (atom('local_workers_joined'), local)))), 'Actual A proof/cleanup both preserved independently')
    for role in ('b', 'c'):
        require(decode(case / role / 'role-outcome.etf', decoder) == ('tuple', (atom('return'), ('tuple', (atom('server_joined'), atom(role))))), 'Actual role graceful completion')
    b = seq(decode(case / 'b/worker-cleanup.etf', decoder), count=2)
    require(decode(case / 'b/worker-ready.etf', decoder) == ('tuple', (atom('worker_ready'), ids[3], ('tuple', (atom('trap_exit'), atom('true'))))), 'B worker actually trapped before measurement')
    joined = seq(b[1], count=4)
    require(b[0] == atom('return') and joined[0] == atom('joined') and joined[1] == ids[3] and joined[3] == atom('killed'), 'B local actual trapping worker kill/join')
    node_of(joined[2], 'reference', names['b'])
    require(decode(case / 'c/worker-cleanup.etf', decoder) == ('tuple', (atom('return'), atom('no_worker'))), 'C has no invented worker cleanup')
    return {'RawAWorkers': repr(local), 'RawBWorker': repr(b), 'Mode': mode}


def same_process(current, original):
    require(all(current[key] == original[key] for key in ('Pid', 'Pgid', 'StartTime')), 'B process identity did not change')


def main():
    global STOP_REQUESTED
    parser = argparse.ArgumentParser()
    for name in ('otp-source', 'otp-root', 'build-evidence', 'output'):
        parser.add_argument('--' + name, required=True, type=Path)
    parser.add_argument('--approved-source-plan-sha256', required=True)
    args = parser.parse_args()
    require(sys.platform == 'linux' and os.uname().machine == 'x86_64', 'Reviewed Linux x86_64 foreground topology')
    source = Path(__file__).resolve().parent
    plan_raw = (source / 'source-plan.json').read_bytes()
    require(sha(plan_raw) == args.approved_source_plan_sha256.upper(), 'Exact reviewed source plan')
    plan = json.loads(plan_raw)
    require(plan['PinnedCommit'] == PIN and plan['OriginalNodesPerScenario'] == 3 and plan['Scenarios'] == list(CASES), 'Exact pin/three-node/two-case scope')
    for row in plan['Sources']:
        path = (source / row['Path']).resolve()
        require(path.is_relative_to(source) and sha(path.read_bytes()) == row['Sha256'], 'Hash-bound executable source/helper')
    execute_raw = helper(source, 'process_capture').execute
    owned = helper(source, 'owned_process').OwnedProcess
    decoder = helper(source, 'passive_control_etf')
    output = args.output.resolve()
    require(not output.exists(), 'Fresh output only')
    output.mkdir(parents=True)
    for name in ('sources', 'ebin'):
        (output / name).mkdir()
    (output / 'source-plan.json').write_bytes(plan_raw)
    for row in plan['Sources']:
        shutil.copyfile(source / row['Path'], output / 'sources' / row['Path'])
    environment = dict(os.environ)
    for name in ('ERL_AFLAGS', 'ERL_FLAGS', 'ERL_ZFLAGS', 'ERL_EPMD_PORT', 'ERL_EPMD_ADDRESS',
                 'ERL_EPMD_EXECUTABLE', 'ERL_INETRC', 'LD_PRELOAD', 'LD_LIBRARY_PATH'):
        environment.pop(name, None)
    environment['ERLC_USE_SERVER'] = 'false'
    environment['PYTHONDONTWRITEBYTECODE'] = '1'
    def request_stop(_number, _frame):
        global STOP_REQUESTED
        STOP_REQUESTED = True
    signal.signal(signal.SIGTERM, request_stop)
    signal.signal(signal.SIGINT, request_stop)
    active_end = time.monotonic() + ACTIVE_SECONDS
    def execute(arguments, prefix, seconds=10):
        require(not STOP_REQUESTED and time.monotonic() < active_end, 'Active preflight cancelled/deadline')
        value = execute_raw(arguments, output, environment, output / prefix,
                            timeout=min(seconds, max(0.1, active_end - time.monotonic())))
        require(len(value.stdout) <= FILE_BYTES and len(value.stderr) <= FILE_BYTES, 'Bounded preflight streams')
        return value
    primary = None
    cleanup_failures = []
    results = []
    tools = []
    try:
        source_root, otp_root = args.otp_source.resolve(), args.otp_root.resolve()
        require(execute(['git', '-C', str(source_root), 'rev-parse', 'HEAD'], 'source-head').stdout.decode().strip() == PIN, 'Exact source HEAD')
        require(not execute(['git', '-C', str(source_root), 'status', '--porcelain', '--untracked-files=no'], 'source-status').stdout.strip(), 'Clean tracked original source')
        for index, row in enumerate(plan['PinnedSources']):
            require(sha(execute(['git', '-C', str(source_root), 'show', PIN + ':' + row['Path']], 'pin-' + str(index)).stdout) == row['Sha256'], 'Exact original reference blob')
        receipt_raw = args.build_evidence.read_bytes()
        receipt = json.loads(receipt_raw)
        require(receipt['SourceCommit'] == PIN and receipt['SourceTag'] == 'OTP-29.1.1' and receipt['SourceBuildCompleted'] is True, 'Actual completed exact source build')
        require(Path(receipt['InstalledOtpRoot']).resolve() == otp_root, 'Exact source-built installed root')
        require(sha(Path(receipt['SourceBuildLog']).read_bytes()) == receipt['SourceBuildLogSha256'], 'Actual source build log binding')
        require((otp_root / 'releases/29/OTP_VERSION').read_text().strip() == VERSION, 'Exact installed patch version')
        (output / 'source-build-evidence.json').write_bytes(receipt_raw)
        binaries = [otp_root / 'bin' / name for name in ('erl', 'erlc', 'epmd')]
        engines = list(otp_root.glob('erts-*/bin/beam.smp'))
        require(len(engines) == 1 and all(p.is_file() for p in binaries), 'Actual source-built launchers/engine')
        tools = [{'Path': str(p), 'Bytes': p.stat().st_size, 'Sha256': sha(p.read_bytes())} for p in binaries + engines]
        erl, erlc, epmd = binaries
        execute([str(erlc), '-Werror', '+debug_info', '-o', str(output / 'ebin')] +
                [str(output / 'sources' / (name + '.erl')) for name in MODULES], 'compile', 30)
        images = {name: hashlib.md5((output / 'ebin' / (name + '.beam')).read_bytes()).digest() for name in MODULES}
        for mode in CASES:
            case = output / mode
            case.mkdir()
            end = min(active_end, time.monotonic() + SESSION_SECONDS)
            children, roles = [], {}
            journal = []
            stop_sent, resumed, query_seen = False, False, False
            original_b = None
            stopped_at = None
            error = None
            proof = None
            def log(phase, **values):
                journal.append({'Phase': phase, 'ParentMonotonicNs': time.monotonic_ns(), **values})
                emit(case / 'journal.json', journal)
            def mark(role, name, nonce):
                if (case / role / name).exists():
                    require((case / role / name).read_bytes() == nonce.encode('ascii'), 'Existing marker exact nonce')
                    log('marker-already-present', Role=role, Name=name, Nonce=nonce)
                    return
                temporary = case / role / (name + '.tmp')
                temporary.write_bytes(nonce.encode('ascii'))
                temporary.replace(case / role / name)
                log(name, Role=role, Nonce=nonce)
            def resume(reason):
                nonlocal resumed
                if stop_sent and not resumed:
                    if roles['b'].poll() is not None:
                        log('SIGCONT-not-needed-already-exited', Reason=reason, ExitCode=roles['b'].process.returncode)
                        return
                    now = fingerprint(roles['b'])
                    same_process(now, original_b)
                    os.killpg(original_b['Pgid'], signal.SIGCONT)
                    resumed = True
                    after_end = time.monotonic() + 1
                    while True:
                        after = fingerprint(roles['b'])
                        same_process(after, original_b)
                        if after['State'] != 'T':
                            break
                        require(time.monotonic() < after_end, 'Actual B SIGCONT state confirmation')
                        time.sleep(0.025)
                    log('SIGCONT', Reason=reason, Before=now, After=after)
            try:
                nonce = uuid.uuid4().hex
                names = {role: 'cw323_' + mode + '_' + role + '_' + nonce[:12] + '@127.0.0.1' for role in ('a', 'b', 'c')}
                with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as reservation:
                    reservation.bind(('127.0.0.1', 0))
                    port = reservation.getsockname()[1]
                env = dict(environment, ERL_EPMD_PORT=str(port), CONTROL_WAIT_NONCE=nonce, CONTROL_WAIT_MODE=mode,
                           CONTROL_WAIT_B=names['b'], CONTROL_WAIT_C=names['c'])
                end = min(active_end, time.monotonic() + SESSION_SECONDS)
                mapper = owned([str(epmd), '-address', '127.0.0.1', '-port', str(port)], output, env, case / 'epmd')
                children.append(mapper)
                startup = min(end, time.monotonic() + STARTUP_SECONDS)
                while True:
                    require(not STOP_REQUESTED and mapper.poll() is None and time.monotonic() < startup, 'Private epmd startup')
                    try:
                        with socket.create_connection(('127.0.0.1', port), timeout=0.1):
                            break
                    except OSError:
                        time.sleep(0.025)
                common = [str(erl), '+S', '1:1', '+SDcpu', '1', '+SDio', '1', '+A', '1', '+zdbbl', '1',
                          '-noshell', '-noinput', '-start_epmd', 'false', '-setcookie', 'control_wait323_private',
                          '-kernel', 'inet_dist_use_interface', '{127,0,0,1}', '-pa', str(output / 'ebin')]
                metadata = {}
                for role in ('b', 'c', 'a'):
                    (case / role).mkdir()
                    child = owned(common + ['-name', names[role], '-eval', MODULES[1] + ':start().'], output,
                                  dict(env, CONTROL_WAIT_ROLE=role, CONTROL_WAIT_OUTPUT=str(case / role)), case / role / 'process')
                    children.append(child)
                    roles[role] = child
                    startup = min(end, time.monotonic() + STARTUP_SECONDS)
                    while not (case / role / 'runtime.etf').is_file():
                        require(not STOP_REQUESTED and child.poll() is None and time.monotonic() < startup, 'Actual original role metadata startup')
                        time.sleep(0.025)
                    metadata[role] = verify_metadata(case / role / 'runtime.etf', decoder, role, names, otp_root, images, child.process.pid)
                log('all-role-metadata', Metadata=metadata, Nodes=names)
                stop_request = case / 'a/stop-request.etf'
                resume_request = case / 'a/resume-request.etf'
                while True:
                    require(not STOP_REQUESTED and time.monotonic() < end, 'Session cancellation/deadline')
                    for child in children:
                        code = child.poll()
                        require(code in (None, 0), 'Original role failed; raw outcome/streams retained')
                    for path in case.rglob('*'):
                        if path.is_file():
                            require(path.stat().st_size <= FILE_BYTES, 'Per-file/stream cap')
                    require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= TOTAL_BYTES, 'Total evidence cap')
                    if stop_request.is_file() and not stop_sent:
                        request = seq(decode(stop_request, decoder), count=6)
                        require(request[:5] == (atom('stop_request'), ('binary', nonce.encode()), atom(mode), atom(names['a']), atom(names['b'])), 'Exact stop nonce/scenario/nodes')
                        require(text(request[5]) == str(roles['b'].process.pid), 'Stop request actual B OS PID')
                        original_b = fingerprint(roles['b'])
                        require(original_b['State'] not in ('T', 't'), 'B not already externally stopped')
                        log('before-SIGSTOP', Identity=original_b, RequestSha256=sha(stop_request.read_bytes()))
                        os.killpg(original_b['Pgid'], signal.SIGSTOP)
                        stop_sent = True
                        stopped_at = time.monotonic()
                        confirmation = min(end, stopped_at + 1)
                        while True:
                            now = fingerprint(roles['b'])
                            same_process(now, original_b)
                            if now['State'] == 'T':
                                break
                            require(time.monotonic() < confirmation, 'B SIGSTOP confirmation')
                            time.sleep(0.025)
                        log('SIGSTOP-confirmed-T', Identity=now)
                        mark('a', 'stop-ack', nonce)
                    if stop_sent and not resumed:
                        require(time.monotonic() - stopped_at < STOP_SECONDS, 'Absolute stopped B deadline')
                        now = fingerprint(roles['b'])
                        same_process(now, original_b)
                        require(now['State'] == 'T', 'B must remain stopped throughout measurement')
                    if (case / 'a/query.etf').is_file() and not query_seen:
                        require(stop_sent and not resumed, 'No public query after B resume')
                        now = fingerprint(roles['b'])
                        same_process(now, original_b)
                        require(now['State'] == 'T', 'Query completed with B still stopped')
                        log('query-completed-while-stopped', Identity=now, QuerySha256=sha((case / 'a/query.etf').read_bytes()))
                        query_seen = True
                    if resume_request.is_file() and not resumed:
                        request = seq(decode(resume_request, decoder), count=5)
                        require(request[:4] == (atom('resume_request'), ('binary', nonce.encode()), atom(mode), atom(names['a'])), 'Exact resume identity')
                        require(stop_sent, 'Successful resume protocol cannot precede actual stop')
                        outcome = seq(request[4])
                        require(outcome[0] == atom('return'), 'Raw original measurement failed; preserve before cleanup')
                        proof = verify_observation(outcome[1], mode, names)
                        require(decode(case / 'a/measurement.etf', decoder) == outcome[1], 'Exact persisted actual observation')
                        query = seq(seq(outcome[1], count=8)[5], count=3)
                        require(decode(case / 'a/query.etf', decoder) == ('tuple', (query[1], query[2])), 'Exact independently persisted public query')
                        kill = seq(seq(outcome[1], count=8)[6], count=4)
                        ids = seq(seq(outcome[1], count=8)[2], count=8)
                        require(decode(case / 'c/kill-call.etf', decoder) == ('tuple', (ids[1], kill[1], ids[5], ids[2], ('tuple', (atom('return'), atom('true'))))), 'Independent C actual exit-call receipt')
                        now = fingerprint(roles['b'])
                        same_process(now, original_b)
                        require(query_seen and now['State'] == 'T', 'Full actual query/killed proof while B remains stopped')
                        log('proof-query-and-killed-before-resume', RequestSha256=sha(resume_request.read_bytes()), ActualProof=proof, StoppedB=now)
                        resume('actual-query-and-killed-proof')
                        mark('a', 'resume-ack', nonce)
                        mark('b', 'cleanup-go', nonce)
                        mark('c', 'cleanup-go', nonce)
                    if all(child.poll() is not None for child in roles.values()):
                        require(resumed and proof is not None, 'No passing exit without genuine stopped proof')
                        break
                    time.sleep(0.025)
                for role in ('a', 'b', 'c'):
                    connection = seq(decode(case / role / 'connections.etf', decoder), count=3)
                    require(connection[0] == atom(names[role]) and set(seq(connection[1], 'list')) == {atom(n) for key, n in names.items() if key != role}, 'Actual private three-node connected inventory')
                    pairs = seq(connection[2], 'list', count=2)
                    require({seq(pair, count=2)[0] for pair in pairs} == {atom(n) for key, n in names.items() if key != role}, 'Two actual distribution controller mappings per role')
                    for pair in pairs:
                        node_of(seq(pair, count=2)[1], 'port', names[role])
                verify_timeline(journal)
                cleanup = verify_cleanup(case, decoder, mode, names, decode(case / 'a/measurement.etf', decoder))
                results.append({'Mode': mode, 'Nodes': names, 'Proof': proof, 'ActualCleanup': cleanup})
            except BaseException as failure:
                error = {'Type': type(failure).__name__, 'Message': str(failure), 'Traceback': traceback.format_exc()}
            finally:
                if stop_sent and not resumed:
                    try:
                        resume('failure-finally-before-graceful-cleanup')
                    except BaseException as failure:
                        cleanup_failures.append({'Mode': mode, 'Phase': 'SIGCONT-before-close', 'Error': repr(failure)})
                for role in roles:
                    try:
                        mark(role, 'resume-ack' if role == 'a' else 'cleanup-go', nonce)
                    except BaseException as failure:
                        cleanup_failures.append({'Mode': mode, 'Phase': 'cleanup-marker', 'Error': repr(failure)})
                try:
                    grace = min(end, active_end, time.monotonic() + 3)
                    while any(child.poll() is None for child in roles.values()) and time.monotonic() < grace:
                        time.sleep(0.025)
                except BaseException as failure:
                    cleanup_failures.append({'Mode': mode, 'Phase': 'bounded-in-session-grace', 'Error': repr(failure)})
                for child in reversed(children):
                    try:
                        child.close()
                    except BaseException as failure:
                        cleanup_failures.append({'Mode': mode, 'Phase': 'owned-group-close', 'Error': repr(failure)})
                log('session-closed', PrimaryFailure=error, CleanupFailures=cleanup_failures,
                    StopIssued=stop_sent, ResumeIssued=resumed)
            require(error is None and not cleanup_failures, 'Original case failed: ' + repr(error))
    except BaseException as failure:
        primary = {'Type': type(failure).__name__, 'Message': str(failure), 'Traceback': traceback.format_exc()}
    finally:
        members = [{'Path': p.relative_to(output).as_posix(), 'Bytes': p.stat().st_size, 'Sha256': sha(p.read_bytes())}
                   for p in sorted(output.rglob('*')) if p.is_file()]
        passed = primary is None and not cleanup_failures and len(results) == 2
        report = {'Status': 'PASS_ACTUAL_ORIGINAL_THREE_NODE_BUSY_QUERY_AND_REMOTE_KILL' if passed else 'FAIL_ORIGINAL_THREE_NODE_BUSY_OBSERVATION',
                  'PinnedCommit': PIN, 'SourcePlanSha256': sha(plan_raw), 'PrimaryFailure': primary,
                  'CleanupFailures': cleanup_failures, 'ActualCases': results, 'Tools': tools,
                  'SdkExecuted': False, 'NativeWireCaptured': False, 'TicketTurnEquivalent': False, 'Files': members}
        emit(output / 'report.json', report)
    print(json.dumps({'Status': report['Status'], 'PrimaryFailure': primary, 'CleanupFailures': cleanup_failures}))
    return 0 if passed else 1


if __name__ == '__main__':
    sys.exit(main())
