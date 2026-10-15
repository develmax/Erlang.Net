from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();a=r/'artifacts';read=lambda p:json.loads(p.read_bytes());sha=lambda b:hashlib.sha256(b).hexdigest().upper()
assert read(a/'root-part308-full-exit.json')['ExitCode']==0 and read(a/'tests.json')['Passed']==6637
p=r/'docs/supported-mfas.json';raw=p.read_bytes();rows=json.loads(raw);old=json.loads(subprocess.check_output(['git','show','HEAD:docs/supported-mfas.json']))
key=lambda x:(x['Module'],x['Function'],x['Arity']);before={key(x):x for x in old};assert len(old)==504 and len(rows)==505 and sum(x['Module']=='erlang' for x in rows)==347
assert {key(x) for x in rows}-before.keys()=={('erlang','load_nif',2)}
changes=[]
for row in rows:
 k=key(row)
 if k==('erlang','process_info',1):
  assert {k:v for k,v in row.items() if k!='Limits'}=={k:v for k,v in before[k].items() if k!='Limits'}
  changes.append({'Mfa':list(k),'Field':'Limits','Raw':row['Limits'],'Derived':before[k]['Limits']});row['Limits']=before[k]['Limits']
 elif k==('erlang','load_nif',2):
  note='Exact pinned indirect BIF only: apply/3 and external-fun invocation return the original bad_lib error. Successful literal native loading, the default instruction handler and general native ABI remain incomplete.'
  changes.append({'Mfa':list(k),'Field':'Limits','Raw':row['Limits'],'Derived':note});row['Limits']=note
 else:assert row==before[k],k
assert [key(x) for x in rows if key(x) in before]==[key(x) for x in old]
rawout=a/'root-part308-raw-supported-mfas.json';out=a/'root-part308-registry-metadata-guard.json';assert not rawout.exists() and not out.exists();rawout.write_bytes(raw)
b=(json.dumps(rows,indent=2)+'\n').encode();p.write_bytes(b)
result={'Status':'ACTUAL_RAW505_CAPTURED_EXISTING_PROCESS_INFO_AND_NEW_INDIRECT_LOAD_NIF_DISCLOSURES','Registry':505,'ErlangMfas':347,'PublicMfaDelta':1,'RawSha256':sha(raw),'RestoredSha256':sha(b),'Other503RowsFieldsAndHistoricalOrderingExact':True,'SourceExporterChanged':False,'PublicLoadNifRegistered':True,'DefaultNativeLoadInstructionHandlerInstalled':False,'GeneralNifAbiComplete':False,'Changes':changes}
out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
