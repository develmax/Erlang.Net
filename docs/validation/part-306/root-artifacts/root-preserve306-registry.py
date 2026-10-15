from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd(); a=r/'artifacts'; read=lambda p:json.loads(p.read_bytes()); sha=lambda b:hashlib.sha256(b).hexdigest().upper()
assert read(a/'root-part306-full-exit.json')['ExitCode']==0 and read(a/'tests.json')['Passed']==6627
p=r/'docs/supported-mfas.json'; rawbytes=p.read_bytes(); raw=json.loads(rawbytes); previous=json.loads(subprocess.check_output(['git','show','HEAD:docs/supported-mfas.json']))
key=lambda x:(x['Module'],x['Function'],x['Arity']); old={key(x):x for x in previous}; expected=('erlang','dist_get_stat',1)
assert len(raw)==504 and len(previous)==503 and {key(x) for x in raw}-old.keys()=={expected}
assert [key(x) for x in raw if key(x)!=expected]==[key(x) for x in previous]
changes=[]
for x in raw:
 k=key(x)
 if k==expected:continue
 y=old[k]
 if k==('erlang','process_info',1):
  assert {k:v for k,v in x.items() if k!='Limits'}=={k:v for k,v in y.items() if k!='Limits'}
  if x['Limits']!=y['Limits']: changes.append({'Mfa':'erlang:process_info/1','Raw':x['Limits'],'PublishedDisclosure':y['Limits']})
  x['Limits']=y['Limits']
 else:assert x==y,k
assert sum(x['Module']=='erlang' for x in raw)==346
out=a/'root-part306-raw-supported-mfas.json'; receipt=a/'root-part306-registry-metadata-guard.json'; assert not out.exists() and not receipt.exists()
out.write_bytes(rawbytes); derived=(json.dumps(raw,indent=2,ensure_ascii=False)+'\n').encode(); p.write_bytes(derived)
v={'Status':'ACTUAL_RAW504_CAPTURED_ONE_NEW_MFA_ONLY_EXISTING_PROCESS_INFO_DISCLOSURE_RESTORED','Registry':504,'ErlangMfas':346,'PublicMfaDelta':1,'AddedMfa':'erlang:dist_get_stat/1','RawSha256':sha(rawbytes),'RestoredSha256':sha(derived),'Other503RowsFieldsAndOrderingExact':True,'SourceExporterChanged':False,'Changes':changes}
receipt.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8'); print(json.dumps(v))
