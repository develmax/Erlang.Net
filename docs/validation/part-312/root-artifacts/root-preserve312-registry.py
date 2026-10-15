from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();a=r/'artifacts';read=lambda p:json.loads(p.read_bytes());sha=lambda b:hashlib.sha256(b).hexdigest().upper()
assert read(a/'root-part312-revision2/full-exit.json')['ExitCode']==0 and read(a/'tests.json')['Passed']==6640
p=r/'docs/supported-mfas.json';raw=p.read_bytes();rows=json.loads(raw);old=json.loads(subprocess.check_output(['git','show','HEAD:docs/supported-mfas.json']))
assert len(rows)==len(old)==505 and sum(x['Module']=='erlang' for x in rows)==347
key=lambda x:(x['Module'],x['Function'],x['Arity']);assert [key(x) for x in rows]==[key(x) for x in old]
changes=[]
for row,before in zip(rows,old):
 if key(row) in {('erlang','process_info',1),('erlang','load_nif',2)}:
  assert {k:v for k,v in row.items() if k!='Limits'}=={k:v for k,v in before.items() if k!='Limits'}
  changes.append({'Mfa':list(key(row)),'Field':'Limits','Raw':row['Limits'],'Derived':before['Limits']});row['Limits']=before['Limits']
 else:assert row==before,key(row)
assert rows==old and len(changes)==2
rawout=a/'root-part312-raw-supported-mfas.json';out=a/'root-part312-registry-metadata-guard.json';assert not rawout.exists() and not out.exists();rawout.write_bytes(raw)
b=(json.dumps(rows,indent=2)+'\n').encode();p.write_bytes(b)
result={'Status':'ACTUAL_RAW505_CAPTURED_TWO_EXISTING_LIMITS_DISCLOSURES_RESTORED_ALL505_ROWS_EXACT','Registry':505,'ErlangMfas':347,'PublicMfaDelta':0,'RawSha256':sha(raw),'RestoredSha256':sha(b),'All505RowsFieldsAndOrderingExact':True,'SourceExporterChanged':False,'PublicLoadNifRegistered':True,'DefaultNativeLoadInstructionHandlerInstalled':False,'GeneralNifAbiComplete':False,'Changes':changes}
out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
