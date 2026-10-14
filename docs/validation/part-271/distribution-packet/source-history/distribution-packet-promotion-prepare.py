import pathlib,subprocess,json,hashlib
r=pathlib.Path.cwd(); out=r/'artifacts/distribution-packet-promotion-prototype';out.mkdir(exist_ok=True)
paths=subprocess.check_output(['git','ls-files','-z']).decode().split('\0');seed=[]
for p in paths:
 if not p or not (p.startswith(('src/','tools/','tests/','benchmarks/','examples/')) or '/' not in p):continue
 f=r/p
 if not f.is_file():continue
 t=out/p;t.parent.mkdir(parents=True,exist_ok=True);t.write_bytes(f.read_bytes());seed.append({'Path':p,'Sha256':hashlib.sha256(f.read_bytes()).hexdigest()})
(r/'artifacts/distribution-packet-promotion-seed.json').write_text(json.dumps({'GitHead':subprocess.check_output(['git','rev-parse','HEAD']).decode().strip(),'WorkingSourceIncludesRootPidImports':True,'Files':seed},indent=2))
freeze=json.loads((r/'artifacts/distribution-packet-source-freeze-revision3.json').read_text())
for f in freeze['Files']:
 t=out/f['Path'];t.parent.mkdir(parents=True,exist_ok=True);t.write_text(f['SourceContent'],encoding='utf8',newline='')
tool=out/'tools/Erlang.DistributionPacketContrasts';tool.mkdir(exist_ok=True)
for name in ['Process','ProcessResult','CaptureProtocol','ErlangString','StringSyntax','Fields']:
 text=(r/f'tools/Erlang.NodeIdentityContrasts/Node{name}.cs').read_text().replace('NodeIdentityContrasts','DistributionPacketContrasts').replace('Node','Packet')
 (tool/f'Packet{name}.cs').write_text(text,encoding='utf8')
(tool/'Program.cs').write_text('''using DistributionPacketContrasts;

try
{
    await PacketExporter.Run(args);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}
''')
(tool/'Erlang.DistributionPacketContrasts.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType></PropertyGroup>
  <ItemGroup>
    <None Update="distribution_packet_probe.erl" CopyToOutputDirectory="PreserveNewest" />
    <None Update="source-plan.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
''')
source=(r/'artifacts/distribution-packet-prototype/native/distribution_packet_probe.erl').read_text()
source=source.replace('Payload = {packet_payload, <<0, 128, 255>>, [a | binary_tail]},','Payload = {packet_payload, <<0, 128, 255>>, [a | binary_tail]},\n        save("payload.etf", term_to_binary(Payload)),\n        save("identities.etf", term_to_binary({Owner, Controller, node(), erlang:is_alive()})),')
source=source.replace('Packet = packet(Control, Payload),','Packet = packet(Control, Payload),\n        save("incoming-send-control.etf", term_to_binary(Control)),')
source=source.replace('RegisteredPacket = packet(Registered, Payload),','RegisteredPacket = packet(Registered, Payload),\n        save("incoming-reg-send-control.etf", term_to_binary(Registered)),')
source=source.replace("receive {'DOWN', Monitor, process, Controller, _} -> ok\n        after 5000 -> exit(Controller, kill),\n                      receive {'DOWN', Monitor, process, Controller, _} -> ok after 5000 -> error(cleanup_timeout) end", "receive {'DOWN', Monitor, process, Controller, Reason} ->\n            save(\"cleanup-controller.etf\", term_to_binary(Reason))\n        after 5000 -> exit(Controller, kill),\n                      receive {'DOWN', Monitor, process, Controller, Reason} ->\n                          save(\"cleanup-controller.etf\", term_to_binary({forced, Reason}))\n                      after 5000 -> error(cleanup_timeout) end")
raw=source.replace('\r\n','\n').encode();(tool/'distribution_packet_probe.erl').write_bytes(raw);sha=hashlib.sha256(raw).hexdigest().upper()
plan=json.loads((r/'artifacts/distribution-packet-prototype/native/source-plan.json').read_text());plan['Pin']=plan.pop('PinSha');plan['SourceTag']='OTP-29.1.1';plan['SourceSha256']=sha;plan['Scenarios']=['pass-through-send-registered'];plan['RequiredArtifacts'] += ['payload.etf','identities.etf','incoming-send-control.etf','incoming-reg-send-control.etf','cleanup-controller.etf'];plan['PriorProbeSourceSha256']=freeze['NativeProbeSha256'];plan['RevisionNote']='Capture independent control/payload and live controller/owner identity bytes plus real monitored cleanup without changing original packet/delivery assertions.';(tool/'source-plan.json').write_text(json.dumps(plan,indent=2)+'\n')
workflow=(r/'.github/workflows/node-identity-contrasts.yml').read_text().replace('node identity','distribution packet').replace('node-identity','distribution-packet').replace('NodeIdentityContrasts','DistributionPacketContrasts').replace('Capture three identity transitions and a pending handle in fresh VMs','Capture actual process-controller SEND and REG_SEND in a fresh VM')
f=out/'.github/workflows/distribution-packet-contrasts.yml';f.parent.mkdir(parents=True,exist_ok=True);f.write_text(workflow)
print(out,'seed',len(seed),'native source SHA',sha,'source plan count',len(plan['RequiredArtifacts']))

