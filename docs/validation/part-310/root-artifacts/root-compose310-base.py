from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();a=r/'artifacts';d=a/'root-part310-combined-isolated';c=d/'candidate';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
baseline=d/'baseline-snapshot.json';assert sha(baseline.read_bytes())=='2A948A7432CEA89AC5C207DE5F666CE1808FB9687F5CAE1A281A93BF792DFFFA'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()=='e829c53ef4323ea8cdd0948cd77c243efa787abd'
for row in read(baseline)['Files']:
 b=(c/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
sets=[('nif-allocation-domain309-candidate2','promotion-map.json','3DE90A145710A2051F56213612BB1570D9C97728982C7B98F4247128E7D8C1F5'),('nif-allocation-domain309-native-validated1','promotion-map.json','F979754694B840B868978E4E744D396532DDF0C5B250004CEA468C42AB4C3A8C'),('nif-domain-sdk-stdout310-validated2','promotion-map.json','F49FBB9C97DFF0DAB20D089EA235C6F949422678836B0C08A7BF9AEF038DFDB2')]
names=set();actions=[]
for folder,mapname,expected in sets:
 p=a/folder;mp=p/mapname;assert sha(mp.read_bytes())==expected;rows=read(mp);rows=rows['Files'] if isinstance(rows,dict) else rows
 for row in rows:
  name=row['Path'];assert name not in names;src=p/'overlay'/name;data=src.read_bytes();assert sha(data)==row['ResultSha256'] and len(data)==row['ResultBytes'],name
  main=r/name;assert (sha(main.read_bytes()) if main.exists() else None)==row['ParentSha256'],name
  target=c/name
  if row.get('Operation')=='InsertRegistrationAfterLedgerRow':
   old=target.read_bytes();anchor=b'Test(Erlang.Tests.NifAllocationLedgerHostNames.Test, Erlang.Tests.NifAllocationLedgerHostChecks.Run);';registration=b'Test(Erlang.Tests.NifAllocationDomainHostNames.Test, Erlang.Tests.NifAllocationDomainHostChecks.Run);'
   assert old.count(anchor)==1 and registration not in old
   pos=old.index(anchor);start=old.rfind(b'\n',0,pos)+1;indent=old[start:pos];assert not indent.strip();end=pos+len(anchor);nl=b'\r\n' if old[end:end+2]==b'\r\n' else b'\n';data=old[:end]+nl+indent+registration+old[end:]
  target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data);names.add(name)
 actions.append({'Candidate':folder,'MapSha256':expected,'Paths':len(rows)})
assert len(names)==18 and sum(x.endswith('.cs') for x in names)==16
rows=[]
for name in sorted(names):
 b=(c/name).read_bytes();rows.append({'Path':name,'Bytes':len(b),'Sha256':sha(b)})
v={'Status':'RUNTIME_NATIVE_AND_READER_COMPOSED_PENDING_ACTUAL_BEAM_HOST_EXTENSION_AND_COMPOSITE_VALIDATION','ParentHead':'e829c53ef4323ea8cdd0948cd77c243efa787abd','BaselineSha256':sha(baseline.read_bytes()),'Actions':actions,'Files':rows,'ExpectedFullPassed':6638,'DefaultRegistry':505,'ErlangPresence':347,'NewPublicMfa':False,'MainEdited':False,'BuildExecuted':False}
p=d/'base-composition.json';assert not p.exists();p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8');print(json.dumps({'BasePaths':18,'CSharp':16,'CompositionSha256':sha(p.read_bytes())}))
