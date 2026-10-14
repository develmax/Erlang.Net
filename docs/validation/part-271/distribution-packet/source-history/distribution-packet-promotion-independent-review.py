from pathlib import Path
import json,hashlib,re,subprocess
a=Path('artifacts');p=a/'distribution-packet-promotion-source-freeze-formatted.json';j=json.loads(p.read_text());sha=lambda b:hashlib.sha256(b).hexdigest().upper()
assert sha(p.read_bytes())=='13B9FBC71C8D1E0417FD62C3ECAE4374D2D528DFD097CBC83FC6CDC3CC572D9A'
files={f['Path']:f for f in j['Files']};assert len(files)==25 and not j['NativeExecuted']
for f in files.values():assert sha(f['SourceContent'].encode())==f['Sha256'].upper(),f['Path']
old=json.loads((a/'distribution-packet-source-freeze-revision3.json').read_text());assert sha((a/'distribution-packet-source-freeze-revision3.json').read_bytes())==j['ProductionCodecSourceFreezeSha256']
codec=[f for f in old['Files'] if f['Path'].startswith('src/')]
for f in codec:assert re.sub(r'\s+','',f['SourceContent'])==re.sub(r'\s+','',files[f['Path']]['SourceContent'])
assert len(codec)==5
source=files['tools/Erlang.DistributionPacketContrasts/distribution_packet_probe.erl']['SourceContent'];plan=json.loads(files['tools/Erlang.DistributionPacketContrasts/source-plan.json']['SourceContent'])
names=files['tools/Erlang.DistributionPacketContrasts/PacketNames.cs']['SourceContent'];exporter=files['tools/Erlang.DistributionPacketContrasts/PacketExporter.cs']['SourceContent'];process=files['tools/Erlang.DistributionPacketContrasts/PacketProcess.cs']['SourceContent']
assert sha(source.encode())==j['NativeProbeSha256']==plan['SourceSha256'] and j['NativeProbeSha256'] in names and '\r' not in source
assert len(plan['RequiredArtifacts'])==len(set(plan['RequiredArtifacts']))==17
assert plan['Pin']=='ad05823719d77c8faee87348ea39513d4e2f99c5' and not plan['NativeExecuted'] and not plan['ObservedExpectations'] and plan['FreshVmRequired']
for name in plan['RequiredArtifacts']:assert name=='stage' or '"'+name+'"' in source,name
assert 'true = register(net_kernel, self())' in source and 'true = erlang:setnode(packet_local@localhost, 1)' in source
assert 'erlang:setnode(packet_remote@localhost, self(), {flags(), 1})' in source and 'erlang:dist_ctrl_input_handler(Handle, self())' in source and 'erlang:dist_ctrl_put_data(Handle, Bytes)' in source
assert source.index('save("stage", <<"complete">>)')<source.index('save("cleanup-controller.etf"')
assert 'Payload = binary_to_term(MessageBytes)' in source and "{6, Owner, '', packet_sink} = OutgoingControl" in source
assert 'SemanticCompatibilityClaim = false' in exporter and 'NativeNetworkingUsed = false' in exporter
assert 'if (Directory.Exists(output))' in exporter and 'PacketNames.RequiredArtifacts.All' in exporter
assert 'result.ExitCode == 0 && !result.TimedOut && result.RawComplete' in exporter
assert 'StandardOutput.BaseStream.CopyToAsync' in process and 'StandardError.BaseStream.CopyToAsync' in process
assert 'WaitAsync(TimeSpan.FromSeconds(PacketNames.StreamDrainSeconds))' in process and 'captureStop.Cancel()' in process and 'rawComplete ? PacketCaptureProtocol.CompleteTrue : PacketCaptureProtocol.CompleteFalse' in process
assert 'SequenceEqual(new[] { PacketNames.Scenario })' in exporter and 'SequenceEqual(PacketNames.RequiredArtifacts)' in exporter
program=files['tests/Erlang.Tests/Program.cs'];assert program['BeforeSource'] is not None
assert program['SourceContent'].replace('Test(Erlang.Tests.DistributionPacketTestNames.Test, Erlang.Tests.DistributionPacketChecks.Run);\n\n','')==program['BeforeSource']
style=(a/'distribution-packet-promotion-evidence/style-final.log').read_text(encoding='utf-8-sig');assert '0 files need formatting' in style
sourcepin=json.loads((a/'distribution-packet-revision3-independent-source-review.json').read_text())
for row in sourcepin['OriginalSources']:assert sha(subprocess.check_output(['git','-C','../otp','show','OTP-29.1.1:'+row['Path']]))==row['Sha256'].upper()
receipt={'Status':'PASS_BOUNDED_SOURCE_REVIEW_NATIVE_AND_MAIN_PENDING','FreezeSha256':sha(p.read_bytes()),'Files':25,'FiveProductionCodecSourcesSemanticTextExactToReviewedRevision3':True,'NativeProbeSha256':j['NativeProbeSha256'],'RequiredDistinctRawArtifacts':17,'FreshNativeExecutionsPlanned':1,'RealOtpKernelAndControllerAdoptionAttempted':True,'RuntimePinRootJitBuildAndCommittedSourceGuardsPresent':True,'ActualNativeExecuted':False,'ActualCliRefusalControlsExecutedByThisReview':False,'SdkSemanticCompatibilityClaim':False,'NoMainWritesOrBuilds':True,'SourceDefectsFound':[],'Limits':['Presence of required output files and process capture establishes observational completeness only; independently decode native packet bytes/results after execution.','No socket, handshake, cache, fragment, sender-opcode or full distribution compatibility claim.','Focused candidate collector process/CLI controls and exact Main full validation remain required separate evidence before promotion.']}
(a/'distribution-packet-promotion-independent-source-review.json').write_text(json.dumps(receipt,indent=2)+'\n');print(sha((a/'distribution-packet-promotion-independent-source-review.json').read_bytes()))
