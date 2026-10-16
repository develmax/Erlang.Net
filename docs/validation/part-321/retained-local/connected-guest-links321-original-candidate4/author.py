from pathlib import Path
import hashlib, json

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
BASE = ROOT / 'artifacts/root-part319-composed4/candidate'
PREVIOUS = BASE / 'tools/Erlang.GuestConnectedSendObservations'
TOOL = 'tools/Erlang.GuestConnectedLinkObservations'
OVERLAY = OUT / 'overlay'
DEST = OVERLAY / TOOL
DEST.mkdir(parents=True, exist_ok=True)

def sha(raw):
    return hashlib.sha256(raw).hexdigest().upper()

def write(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(text.encode('utf-8'))

def read(path):
    return path.read_text(encoding='utf-8-sig').replace('\r\n', '\n')

write(DEST / 'guest_connected_links321.erl', '''-module(guest_connected_links321).
-export([run/1, worker/1]).

-define(DEADLINE, 5000).

%% Source-only original two-node relation probe; no execution is claimed.
run(RemoteNode) when is_atom(RemoteNode), RemoteNode =/= node() ->
    true = lists:member(RemoteNode, nodes()),
    PreviousTrap = process_flag(trap_exit, true),
    Remote = spawn(RemoteNode, ?MODULE, worker, [self()]),
    Monitor = erlang:monitor(process, Remote),
    Outcome = capture(fun() ->
        receive
            {ready, Remote} -> ok
        after ?DEADLINE -> erlang:error(worker_ready_timeout)
        end,
        Baseline = observe(Remote, baseline, false, false),
        true = erlang:link(Remote),
        Linked = observe(Remote, linked, true, false),
        true = erlang:link(Remote),
        Duplicate = observe(Remote, duplicate_link, true, false),
        true = erlang:link(Remote, [priority]),
        Priority = observe(Remote, priority_link, true, true),
        true = erlang:link(Remote, []),
        Cleared = observe(Remote, cleared_priority, true, false),
        true = erlang:unlink(Remote),
        Unlinked = observe(Remote, unlinked, false, false),
        true = erlang:unlink(Remote),
        Repeat = observe(Remote, duplicate_unlink, false, false),
        true = erlang:link(Remote),
        Relinked = observe(Remote, relinked, true, false),
        true = erlang:unlink(Remote),
        Final = observe(Remote, final_unlink, false, false),
        {guest_connected_links321,
         [{public_link_propagates, true},
          {duplicate_link_idempotent, true},
          {link_priority_is_local_receiver_permission, true},
          {empty_link_options_clear_priority, true},
          {public_unlink_propagates, true},
          {duplicate_unlink_idempotent, true},
          {relink_after_unlink_propagates, true}],
         {raw, self(), Remote, [Baseline, Linked, Duplicate, Priority, Cleared,
                                Unlinked, Repeat, Relinked, Final]}}
    end),
    Cleanup = capture(fun() ->
        CleanupMonitor = erlang:monitor(process, Remote),
        true = erlang:unlink(Remote),
        erlang:exit(Remote, kill),
        receive
            {'DOWN', CleanupMonitor, process, Remote, Reason} -> {worker_joined, Reason}
        after ?DEADLINE ->
            erlang:demonitor(CleanupMonitor, [flush]),
            erlang:error(worker_cleanup_timeout)
        end
    end),
    erlang:demonitor(Monitor, [flush]),
    process_flag(trap_exit, PreviousTrap),
    put(guest_connected_links321_cleanup, Cleanup),
    case Outcome of
        {exception, Class, Failure, Stack} -> erlang:raise(Class, Failure, Stack);
        {return, Value} ->
            case Cleanup of
                {return, _} -> Value;
                {exception, CleanupClass, CleanupFailure, CleanupStack} ->
                    erlang:raise(CleanupClass, CleanupFailure, CleanupStack)
            end
    end.

observe(Remote, Phase, ExpectedLinked, ExpectedPriority) ->
    Reference = make_ref(),
    Remote ! {probe, self(), Reference, Phase},
    RemoteLinks = receive
        {observed, Reference, Phase, Links} -> Links
    after ?DEADLINE -> erlang:error(relation_probe_timeout)
    end,
    {links, OwnerLinks} = erlang:process_info(self(), links),
    {priority_messages, Priority} = erlang:process_info(self(), priority_messages),
    ExpectedLinked = lists:member(self(), RemoteLinks),
    ExpectedLinked = lists:member(Remote, OwnerLinks),
    ExpectedCount = case ExpectedLinked of true -> 1; false -> 0 end,
    ExpectedCount = length([Pid || Pid <- RemoteLinks, Pid =:= self()]),
    ExpectedCount = length([Pid || Pid <- OwnerLinks, Pid =:= Remote]),
    ExpectedPriority = Priority,

    {Phase, OwnerLinks, RemoteLinks, Priority}.

worker(Owner) ->
    Owner ! {ready, self()},
    worker_loop(Owner).

worker_loop(Owner) ->
    receive
        {probe, Owner, Reference, Phase} ->
            {links, Links} = erlang:process_info(self(), links),
            Owner ! {observed, Reference, Phase, Links},
            worker_loop(Owner)
    after ?DEADLINE -> erlang:error(worker_idle_timeout)
    end.

capture(Operation) ->
    try Operation() of
        Value -> {return, Value}
    catch
        Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.
''')

write(DEST / 'guest_connected_bang322.erl', '''-module(guest_connected_bang322).
-export([bang/2]).

bang(Pid, Message) -> Pid ! Message.
''')
probe_path = DEST / 'guest_connected_links321.erl'
probe = read(probe_path)
probe = probe.replace('    Remote ! {probe, self(), Reference, Phase},',
                      '    Message = {probe, self(), Reference, Phase},\n    Message = guest_connected_bang322:bang(Remote, Message),')
probe = probe.replace('            Owner ! {observed, Reference, Phase, Links},',
                      '            Message = {observed, Reference, Phase, Links},\n            Message = guest_connected_bang322:bang(Owner, Message),')
write(probe_path, probe)

for name in ('owned_process.py', 'passive_etf.py', 'process_capture.py'):
    (DEST / name).write_bytes((PREVIOUS / name).read_bytes())

driver = read(PREVIOUS / 'guest_connected_send319_driver.erl')
driver = driver.replace('guest_connected_send319', 'guest_connected_links321').replace('GUEST_CONNECTED_SEND', 'GUEST_CONNECTED_LINK')
driver = driver.replace('beam_md5(guest_connected_links321), beam_md5(?MODULE),',
                        'beam_md5(guest_connected_links321), beam_md5(?MODULE), beam_md5(guest_connected_bang322),')
write(DEST / 'guest_connected_links321_driver.erl', driver)

collector = read(PREVIOUS / 'collect.py')
collector = collector.replace('guest_connected_send319', 'guest_connected_links321').replace('guest_send319', 'guest_link321')
collector = collector.replace('gs319_', 'gl321_').replace('GUEST_CONNECTED_SEND', 'GUEST_CONNECTED_LINK')
collector = collector.replace('TWO_NODE_GUEST_CONNECTED_SEND', 'TWO_NODE_GUEST_CONNECTED_LINK')
collector = collector.replace('for name in (MODULE, DRIVER)]', "for name in (MODULE, DRIVER, 'guest_connected_bang322')]")
collector = collector.replace("metadata = seq(decode(output / role / 'runtime.etf'), count=9)",
                              "metadata = seq(decode(output / role / 'runtime.etf'), count=10)")
collector = collector.replace("metadata[5:7] == (('binary', beam_md5[0]), ('binary', beam_md5[1]))",
                              "metadata[5:8] == (('binary', beam_md5[0]), ('binary', beam_md5[1]), ('binary', beam_md5[2]))")
collector = collector.replace('Both actual nodes use exact compiled probe/driver images',
                              'Both actual nodes use exact compiled probe/driver/minimal bang images')
collector = collector.replace("str(output / 'sources' / (DRIVER + '.erl'))]",
                              "str(output / 'sources' / (DRIVER + '.erl')), str(output / 'sources/guest_connected_bang322.erl')]")
start = collector.index('    expected = [')
end = collector.index('\n\n\n\ndef main():', start)
collector = collector[:start] + '''    expected = ['public_link_propagates', 'duplicate_link_idempotent',
                'link_priority_is_local_receiver_permission', 'empty_link_options_clear_priority',
                'public_unlink_propagates', 'duplicate_unlink_idempotent', 'relink_after_unlink_propagates']
    require(result[0] == atom(MODULE), 'Original link probe marker')
    require(seq(result[1], 'list') == tuple(('tuple', (atom(name), atom('true'))) for name in expected), 'Seven actual original relation contracts')
    raw = seq(result[2], count=4)
    require(raw[0] == atom('raw'), 'Raw original relation observation marker')
    owner, remote, observations = raw[1:]
    require(owner[0] == remote[0] == 'pid' and owner != remote, 'Two actual distinct PIDs retained')
    require(owner[1] == atom(names['client']) and remote[1] == atom(names['server']), 'Actual owner/worker node binding')
    expected_phases = [('baseline', False, False), ('linked', True, False),
                       ('duplicate_link', True, False), ('priority_link', True, True),
                       ('cleared_priority', True, False), ('unlinked', False, False),
                       ('duplicate_unlink', False, False), ('relinked', True, False),
                       ('final_unlink', False, False)]
    actual = seq(observations, 'list', count=len(expected_phases))
    for observation, (phase, linked, priority) in zip(actual, expected_phases):
        row = seq(observation, count=4)
        require(row[0] == atom(phase), 'Actual ordered relation phase')
        owner_links = seq(row[1], 'list')
        remote_links = seq(row[2], 'list')
        require(all(item[0] == 'pid' for item in owner_links + remote_links), 'Full actual PID link lists retained')
        require((remote in owner_links) == linked and (owner in remote_links) == linked, 'Both actual relationship directions')
        require(owner_links.count(remote) == int(linked) and remote_links.count(owner) == int(linked), 'Exact zero/one link cardinality in both directions')
        require(row[3] == atom('true' if priority else 'false'), 'Actual local priority receiver permission')
    joined = ('tuple', (atom('return'), ('tuple', (atom('worker_joined'), atom('killed')))))
    require(decode(output / 'client/worker-cleanup.etf') == joined, 'Fresh worker monitor joined actual kill after successful live-worker observation')
    stopped = ('tuple', (atom('return'), atom('server_joined_protocol')))
    require(decode(output / 'client/server-cleanup.etf') == stopped, 'Client observed server shutdown handshake')
    require(decode(output / 'server/shutdown.etf') == stopped, 'Server consumed final handshake')
    return {'OrderedOriginalContracts': expected, 'ObservedRelationPhases': [row[0] for row in expected_phases],
            'ActualRawOutcome': repr(outcome), 'ActualWorkerCleanup': repr(joined),
            'NativeControlWireBytesCaptured': False, 'SdkExecuted': False, 'SdkCompared': False,
            'LocalLinkPriorityObserved': True, 'TracePressureOrSchedulerPhaseObserved': False,
            'Roles': ['original public link/1, link/2 and unlink/1 -> live original remote worker',
                      'same-sender probes retain both complete raw link lists and local receiver priority',
                      'separate SDK HOST/controller packets are not executed or compared by this collector']}
''' + collector[end:]
collector = collector.replace("'PriorityTraceOrPressureObserved': False", "'LocalLinkPriorityObserved': True, 'TracePressureOrSchedulerPhaseObserved': False")
collector = collector.replace("'NativeNetworkingUsed': bool(children), 'LocalLinkPriorityObserved': True", "'NativeNetworkingUsed': bool(children), 'LocalLinkPriorityObserved': assertions is not None")
write(DEST / 'collect.py', collector)

write(DEST / 'README.md', '''# Pinned original connected link/unlink observation

SOURCE ONLY: this tool has not been compiled or executed. It reuses the exact319 foreground process-group ownership, bounded process captures, strict ETF decoder, clean pin/source-build/tool-image and loaded-BEAM attestations. Two real loopback OTP29.1.1 nodes plus a private owned epmd are required. A successful future run retains nine ordered raw owner/worker link-list snapshots and priority flags for seven public contracts, both node metadata, full streams/outcome and fresh-monitor worker/server cleanup settlement. The raw PIDs/lists are never renamed or reduced to a fabricated SDK equality result. Priority is a local receiver permission; no priority LINK control bit is claimed. Pressure, scheduler phases, old ACK injection, original wire bytes and live SDK peer networking are outside this observation.

The separate minimal guest_connected_bang322 module exports only bang/2 and is compiled by the same pinned source-built erlc invocation. Original owner probes and worker replies both call it and assert the exact returned message. Both node metadata attest its actual third compiled object MD5 as well as probe/driver images. No existing BEAM is rewritten, no opcode role allowlist is widened, and no compiled byte hash or SDK preparation success is inferred from this source. Its future original .beam can be separately tested by Root's actual SDK BEAM route.

Use the reviewed source-plan SHA with the existing collect.py CLI options --otp-source, --otp-root, --build-evidence, --approved-source-plan-sha256, --output. Linux x86_64 foreground ownership is required; runtime source-build prerequisites remain the existing workflow. The source plan binds six original blobs and every executable local helper before import. The copied319 helpers are byte-identical. The existing workflow steps are preserved and a final independent collection step/report pair is appended. All original/native execution is a later Root gate.
''')

plan = json.loads((PREVIOUS / 'source-plan.json').read_bytes())
plan['Observation'] = 'Seven public connected link/unlink contracts; nine raw owner/worker relation and local-priority states; no pressure/trace/scheduler-phase profile'
plan['Sources'] = [{'Path': path.name, 'Bytes': len(path.read_bytes()), 'Sha256': sha(path.read_bytes())}
                   for path in sorted(DEST.iterdir()) if path.name != 'source-plan.json']
write(DEST / 'source-plan.json', json.dumps(plan, indent=2) + '\n')
plan_sha = sha((DEST / 'source-plan.json').read_bytes())

workflow = read(BASE / '.github/workflows/distribution-signal-observations.yml')
anchor = '      - uses: actions/upload-artifact@v7'
step = '''      - name: Observe public connected links and unlinks between two original nodes
        if: ${{ !cancelled() && steps.signal-tool.outcome == 'success' && steps.pinned-build.outcome == 'success' }}
        shell: bash
        env:
          PYTHONDONTWRITEBYTECODE: '1'
          GUEST_CONNECTED_LINK_SOURCE_PLAN_SHA256: __PIN__
        run: |
          set -euo pipefail
          python3 -B tools/Erlang.GuestConnectedLinkObservations/collect.py \\
            --otp-source "$GITHUB_WORKSPACE/artifacts/beam-otp-source" \\
            --otp-root "$GITHUB_WORKSPACE/artifacts/beam-otp-oracle/lib/erlang" \\
            --build-evidence "$GITHUB_WORKSPACE/artifacts/beam-source-build-evidence.json" \\
            --approved-source-plan-sha256 "$GUEST_CONNECTED_LINK_SOURCE_PLAN_SHA256" \\
            --output "$GITHUB_WORKSPACE/artifacts/guest-connected-links321-original" \\
            2>&1 | tee artifacts/guest-connected-links321-collector.log
'''.replace('__PIN__', plan_sha)
assert workflow.count(anchor) == 1
workflow = workflow.replace(anchor, step + anchor)
workflow += '            artifacts/guest-connected-links321-original\n            artifacts/guest-connected-links321-collector.log\n'
write(OVERLAY / '.github/workflows/distribution-signal-observations.yml', workflow)
attrs = read(BASE / '.gitattributes')
attrs += '\n# Original connected guest link/unlink observation source plan.\n' + TOOL + '/source-plan.json text eol=lf\n'
write(OVERLAY / '.gitattributes', attrs)

write(OUT / 'HANDOFF.md', '''# Original321 source-only handoff

The current production candidate is separately frozen at5521C67F103B7DF2EEBF3C9B67B0CC159279681E0873D4D6C0DF9432DDD455F7 and layered on final320 at638059F4D1D6BE2FE2125181FD9A5796E3517A3954099DB2F0B19D1E03DA2FD3. Historical first A8D08AE8 is blocked, not releasable production. This Python/Erlang observer compiles no SDK source and remains separate/unexecuted. Root must bind it after final320/source composition, independent source peer, actual tool compilation and original VM execution. The actual owner and remote worker are both original OTP processes; SDK evidence remains a distinct HOST role. Do not infer cross-runtime transport or native wire-byte equivalence.

Existing319 helper bytes and process tree deadlines/cleanup are retained. Probe exceptions are captured before the ready receive, fresh worker monitoring joins kill even if the old monitor was already consumed, and the primary probe exception takes precedence over cleanup exceptions while both raw captures are retained. Cleanup explicitly unlinks before killing, including a failed still-linked probe, so late linked EXIT cannot race restoration of trap_exit. Both successful final unlink and failure cleanup suppress the local link before trap_exit is restored. Exact zero/one owner-worker cardinality is checked in both complete retained link lists, including duplicate/relink stages. Each relation observation is causally after a same-sender public probe message. No injected remote ACK or native scheduling claim is made.
''')

sources = []
for path in sorted(OVERLAY.rglob('*')):
    if path.is_file():
        relative = path.relative_to(OVERLAY).as_posix()
        parent = BASE / relative
        sources.append({'Path': relative, 'Bytes': len(path.read_bytes()), 'Sha256': sha(path.read_bytes()),
                        'ParentSha256': sha(parent.read_bytes()) if parent.is_file() else None})
assert len(sources) == 11
assert b'\r' not in (DEST / 'source-plan.json').read_bytes()
freeze = {'Status': 'SOURCE_ONLY_ORIGINAL321_CONNECTED_LINK_COLLECTOR_UNEXECUTED',
          'PreviousRevisionFreezeSha256': '69E99C8398533D80E7DC67E432833F82CE7048B2886A7BFBD8E1AB564F2FBE55',
          'Parent319ClosureSha256': 'E78578685DF11BE146CDE007C8DBDF16640A7CBAB18DDCA6A7322AD4A511EC3C',
          'SourcePaths': len(sources), 'CSharpPaths': 0, 'Files': sources,
          'SourcePlanSha256': plan_sha, 'DeclaredOriginalContracts': 7, 'RawRelationPhases': 9,
          'SdkExecuted': False, 'OriginalExecuted': False, 'SdkCompared': False,
          'MainGitBuildSdkVmNativeNetworkExecuted': False,
          'EvidenceBindings': [{'Path': path.name, 'Bytes': len(path.read_bytes()), 'Sha256': sha(path.read_bytes())}
                               for path in (OUT / 'author.py', OUT / 'HANDOFF.md')]}
write(OUT / 'source-freeze.json', json.dumps(freeze, indent=2) + '\n')
print(json.dumps({'FreezeSha256': sha((OUT / 'source-freeze.json').read_bytes()), 'SourcePlanSha256': plan_sha}))
