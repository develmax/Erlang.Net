from pathlib import Path
import json,hashlib
R=Path.cwd();A=R/'artifacts/proc-lib-real-peer-procfs-correction';B=R/'artifacts/proc-lib-real-peer-procfs-correction2';O=R/'artifacts/proc-lib-real-peer-procfs-independent'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
raw=(B/'source-freeze.json').read_bytes();assert sha(raw)=='D31D6B7035A98B9ABB7DB900E175C55C6E9B196C4633AB84E64D9E4B4AD8596C'
f=json.loads(raw);changed=[]
for row in f['Files']:
 p=B/'source'/row['Path'];old=A/'source'/row['Path'];b=p.read_bytes()
 assert len(b)==row['Bytes'] and sha(b)==row['Sha256']
 assert sha((R/row['Path']).read_bytes())==row['ParentSha256']
 if old.read_bytes()!=b:changed.append(row['Path'])
assert changed==['tools/Erlang.ProcLibRealPeerObservations/README.md','tools/Erlang.ProcLibRealPeerObservations/source-manifest.json']
tool='tools/Erlang.ProcLibRealPeerObservations/'
ma=json.loads((A/'source'/tool/'source-manifest.json').read_bytes());mb=json.loads((B/'source'/tool/'source-manifest.json').read_bytes())
assert sha((B/'source'/tool/'source-manifest.json').read_bytes())=='61E52EC3EEE23FD4084772C0E383470C7FD09DAC6A6F16D7BD50AD3A6CB85229'
for old,new in zip(ma['Files'],mb['Files']):
 assert old['Path']==new['Path']
 if old['Path']!='README.md':assert old==new
 else:
  ma['Files'][ma['Files'].index(old)]=new
 assert sha((B/'source'/tool/new['Path']).read_bytes())==new['Sha256']
assert ma==mb
readme=(B/'source'/tool/'README.md').read_text(encoding='utf-8')
for assertion in ['exact historical syscall','order of the peer boot crash','unproven','without peer identity or spawn results','not an API semantic comparison','do not replace a fresh original VM acquisition']:assert assertion in readme
clarification=(A/'diagnosis-ordering-clarification.json').read_bytes();assert sha(clarification)=='ECEA6ED9564FB1838D481B381145F19183020A7B32908903D1693EE720732BEF'
prior=(O/'report.json').read_bytes();assert sha(prior)=='44C78A991ADBCCDAAC538D15066500AFB89261511BD2963CF206A67DE8E7F575'
report={'Status':'PASS_INDEPENDENT_REVISION2_README_AND_MANIFEST_ONLY','SourceFreezeSha256':sha(raw),'ManifestSha256':'61E52EC3EEE23FD4084772C0E383470C7FD09DAC6A6F16D7BD50AD3A6CB85229','SourceRowsRehashed':6,'ChangedVsRevision1':changed,'OnlyReadmeManifestRowChanged':True,'CollectorProbesAndWorkflowRawUnchanged':True,'PriorSourceAndSyntheticControlsPeerSha256':sha(prior),'OrderingPrecisionSupplementSha256':sha(clarification),'HistoricalSyscallAndCrashOrderingUnproven':True,'NoCompleteEightCasePassClaimed':True,'MainModified':False,'NetworkUsed':False,'SdkExecuted':False,'OriginalReexecuted':False}
(O/'report2.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Status':report['Status'],'Sha256':sha((O/'report2.json').read_bytes())}))
