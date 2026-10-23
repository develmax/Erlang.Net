from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();p=r/'docs/mfa-compatibility.json';raw=p.read_bytes();old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json']));assert json.loads(raw)==old
names=['src/Erlang.Runtime/ErlangOperatorValues.cs','src/Erlang.Runtime/ProcessAliasRegistrations.cs','src/Erlang.Runtime/RuntimeDistributionPending.AliasSend.cs','src/Erlang.Runtime/RuntimeDistributionPending.GuestSend.cs']
assert set(subprocess.check_output(['git','diff','--name-only','HEAD','--','src']).decode().splitlines())==set(names)
eol='\r\n' if b'\r\n' in raw else '\n';changes=[]
for name in names:
 digest=hashlib.sha256((r/name).read_bytes()).hexdigest().upper();row=next((x for x in old['VerifiedSourceFiles'] if x['Path']==name),None)
 if row is not None:
  assert digest!=row['Sha256'] and raw.count(row['Sha256'].encode())==1;raw=raw.replace(row['Sha256'].encode(),digest.encode());changes.append(dict(Path=name,Before=row['Sha256'],After=digest,Added=False))
 else:
  following=next(x for x in sorted(old['VerifiedSourceFiles'],key=lambda x:x['Path']) if x['Path']>name)
  anchor=('    {'+eol+'      "Path": "'+following['Path']+'",').encode();assert raw.count(anchor)==1
  insert=('    {'+eol+'      "Path": "'+name+'",'+eol+'      "Sha256": "'+digest+'"'+eol+'    },'+eol).encode();raw=raw.replace(anchor,insert+anchor);changes.append(dict(Path=name,Before=None,After=digest,Added=True))
now=json.loads(raw);assert now['Entries']==old['Entries'] and len(now['VerifiedSourceFiles'])==1621
for row in now['VerifiedSourceFiles']:assert hashlib.sha256((r/row['Path']).read_bytes()).hexdigest().upper()==row['Sha256'],row['Path']
p.write_bytes(raw);(r/'artifacts/library448-source-guard-delta.json').write_text(json.dumps(dict(Changed=changes,EntriesUnchanged=True,SourceGuards=1621,ProductReason='Confirmed connected named-send drop: one existing guard updated, three newly reached implementation helpers guarded; all other entries untouched'),indent=2)+'\n')
