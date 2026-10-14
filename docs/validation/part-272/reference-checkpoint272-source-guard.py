from pathlib import Path
import hashlib,json,subprocess,sys
head=sys.argv[1];sha=lambda b:hashlib.sha256(b).hexdigest().upper()
guard=json.loads(Path('artifacts/part272-main-import-guard.json').read_text());assert guard['Imported']==len(guard['Files'])==25
for row in guard['Files']:
 working=Path(row['Path']).read_bytes();assert sha(working)==row['AfterSha256'],row['Path']
 committed=subprocess.check_output(['git','show',head+':'+row['Path']])
 assert committed.decode().replace('\r\n','\n')==working.decode().replace('\r\n','\n'),row['Path']
 if row['Path'].endswith('.erl'):assert committed==working,row['Path']
workflow=subprocess.check_output(['git','show',head+':.github/workflows/distribution-packet-contrasts.yml']).decode();assert 'capture_mixed:' in workflow and 'default: true' in workflow
assert workflow.count('git clone --depth 1 --branch OTP-29.1.1')==1 and 'continue-on-error' not in workflow
assert '!cancelled() && inputs.capture_mixed' in workflow and 'if: always() && inputs.capture_mixed' in workflow
assert workflow.count('dotnet run --project tools/Erlang.DistributionPacketContrasts')==1
probe=subprocess.check_output(['git','show',head+':tools/Erlang.DistributionPacketContrasts/distribution_packet_probe.erl']);plan=json.loads(subprocess.check_output(['git','show',head+':tools/Erlang.DistributionPacketContrasts/source-plan.json']))
assert sha(probe)==plan['SourceSha256'] and len(set(plan['RequiredArtifacts']))==18
assert 'pending-handle.etf' in plan['RequiredArtifacts'] and probe.index(b'new_connection(')<probe.index(b'spawn_monitor(')
print('Committed27225 source bindings, pending18 and packet-only option verified; ordinary13+4 additions require fresh native corpus5802/fd8')
