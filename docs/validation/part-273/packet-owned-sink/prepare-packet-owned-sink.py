from pathlib import Path
import json,hashlib,shutil,subprocess
r=Path.cwd();a=r/'artifacts';root=a/'packet-owned-sink-prototype';tool=root/'tools/Erlang.DistributionPacketContrasts';tool.mkdir(parents=True,exist_ok=True)
for name in ['Directory.Build.props','global.json','NuGet.Config']:
 if (r/name).exists():shutil.copyfile(r/name,root/name)
for p in (r/'tools/Erlang.DistributionPacketContrasts').iterdir():
 if p.is_file():shutil.copyfile(p,tool/p.name)
p=tool/'distribution_packet_probe.erl';s=p.read_text();s=s.replace('        true = register(packet_sink, self()),','        Sink = start_sink(Owner),\n        save("registered-sink-identities.etf", term_to_binary({Owner, Sink, whereis(net_kernel), whereis(packet_sink)})),',1)
s=s.replace('        receive Payload -> save("incoming-reg-send-delivered.etf", term_to_binary(Payload))','''        receive {sink_delivery, Sink, Payload} ->
            save("incoming-reg-send-delivered.etf", term_to_binary(Payload))''',1)
s=s.replace('        cleanup_controller(Controller, Monitor)\n    end.', '''        try cleanup_sink()
        after cleanup_controller(Controller, Monitor) end
    end.''',1)
helper='''start_sink(Owner) ->
    {Sink, SinkMonitor} = spawn_monitor(fun() ->
        true = register(packet_sink, self()),
        Owner ! {sink_ready, self(), whereis(packet_sink)},
        sink_loop(Owner)
    end),
    put(packet_owned_sink, {Sink, SinkMonitor}),
    receive
        {sink_ready, Sink, Sink} -> Sink;
        {'DOWN', SinkMonitor, process, Sink, SinkReadyReason} ->
            put(packet_owned_sink_down, {observed, SinkReadyReason}),
            save("registered-sink-ready-failure.etf", term_to_binary(SinkReadyReason)),
            error({sink_died_before_ready, SinkReadyReason})
    after 5000 -> error(sink_ready_timeout) end.

sink_loop(Owner) ->
    receive
        stop -> ok;
        Message ->
            Owner ! {sink_delivery, self(), Message},
            sink_loop(Owner)
    end.

cleanup_sink() ->
    case erase(packet_owned_sink) of
        undefined -> ok;
        {Sink, SinkMonitor} ->
            case erase(packet_owned_sink_down) of
                {observed, ObservedSinkReason} ->
                    save("cleanup-sink.etf", term_to_binary(ObservedSinkReason));
                undefined ->
                    Sink ! stop,
                    receive {'DOWN', SinkMonitor, process, Sink, SinkCleanupReason} ->
                        save("cleanup-sink.etf", term_to_binary(SinkCleanupReason))
                    after 5000 -> exit(Sink, kill),
                                  receive {'DOWN', SinkMonitor, process, Sink, SinkForcedReason} ->
                                      save("cleanup-sink.etf", term_to_binary({forced, SinkForcedReason}))
                                  after 5000 -> error(sink_cleanup_timeout) end
                    end
            end
    end.

'''
s=s.replace('cleanup_controller(Controller, Monitor) ->',helper+'cleanup_controller(Controller, Monitor) ->',1);p.write_bytes(s.encode());sha=lambda b:hashlib.sha256(b).hexdigest().upper();digest=sha(p.read_bytes());plan=json.loads((tool/'source-plan.json').read_text());oldhash=plan['SourceSha256'];p=tool/'PacketNames.cs';s=p.read_text().replace(oldhash,digest).replace('        "pending-handle.etf"','        "pending-handle.etf",\n        "registered-sink-identities.etf",\n        "cleanup-sink.etf"');p.write_bytes(s.encode());plan['SourceSha256']=digest;plan['PreviousPublishedSourceSha256']=oldhash;plan['RequiredArtifacts']+=['registered-sink-identities.etf','cleanup-sink.etf'];plan['OptionalFailureArtifacts'].append('registered-sink-ready-failure.etf');plan['RevisionNote']='Retain registered net_kernel owner and use a separately monitored registered packet_sink process. Verify ready registration and exact payload forwarded from its identity; preserve owned sink and controller cleanup on all paths. Both original271 and272 failures remain immutable; no old workflow retry.';(tool/'source-plan.json').write_bytes((json.dumps(plan,indent=2)+'\n').encode());p=tool/'README.md';s=p.read_text().replace('all eighteen required raw artifacts','all twenty required raw artifacts');s+='\nThe registered packet sink is an independently owned monitored process; the net_kernel owner retains its registration. Its ready message confirms the registered PID, and exact payload delivery is forwarded with that PID. The identity tuple and normal or forced sink cleanup are retained as additional raw evidence. Controller cleanup runs even if sink cleanup fails. This new version follows the immutable part 272 registered_name failure.\n';p.write_bytes(s.encode());owned=['tools/Erlang.DistributionPacketContrasts/'+n for n in ['distribution_packet_probe.erl','PacketNames.cs','source-plan.json','README.md']];x={'Stage':'BeforeAnyChangedTargetExecution','BaseHead':subprocess.check_output(['git','rev-parse','HEAD']).decode().strip(),'SourceSha256':digest,'RequiredArtifacts':20,'NativeExecuted':False,'ProductionCodecChanges':0,'Historical271272FailuresPreserved':True,'Files':[{'Path':path,'BeforeSha256':sha((r/path).read_bytes()),'BeforeSource':(r/path).read_text(),'Sha256':sha((root/path).read_bytes()),'SourceContent':(root/path).read_text()} for path in owned]};p=a/'packet-owned-sink-source-freeze-initial.json';p.write_bytes((json.dumps(x,indent=2)+'\n').encode());print(digest,sha(p.read_bytes()))
