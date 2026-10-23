from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();path=r/'docs/mfa-compatibility.json';raw=path.read_bytes();old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json']));assert json.loads(raw)==old
paths=['src/Erlang.Runtime/ErlangOperatorValues.cs','src/Erlang.Runtime/ProcessAliasRegistrations.cs','src/Erlang.Runtime/RuntimeDistributionPending.AliasSend.cs','src/Erlang.Runtime/RuntimeDistributionPending.GuestSend.cs']
assert set(subprocess.check_output(['git','diff','--name-only','HEAD','--','src']).decode().splitlines())==set(paths)
changes=[]
for name in paths:
 row=next(x for x in old['VerifiedSourceFiles'] if x['Path']==name);new=hashlib.sha256((r/name).read_bytes()).hexdigest().upper();assert new!=row['Sha256'];assert raw.count(row['Sha256'].encode())==1;raw=raw.replace(row['Sha256'].encode(),new.encode());changes.append(dict(Path=name,Before=row['Sha256'],After=new))
now=json.loads(raw);assert now['Entries']==old['Entries'] and len(now['VerifiedSourceFiles'])==1618
for row in now['VerifiedSourceFiles']:assert hashlib.sha256((r/row['Path']).read_bytes()).hexdigest().upper()==row['Sha256'],row['Path']
path.write_bytes(raw);(r/'artifacts/library448-source-guard-delta.json').write_text(json.dumps(dict(Changed=changes,EntriesUnchanged=True,SourceGuards=1618,ProductReason='Route connected named targets into existing owned distribution outputs; native oracle send timeout/outgoing0 retained'),indent=2)+'\n')
