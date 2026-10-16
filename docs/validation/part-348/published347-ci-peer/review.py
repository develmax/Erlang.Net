"""Passive current-head CI review. Only local Git and frozen API/ZIP artifacts are read."""
from pathlib import Path,PurePosixPath,PureWindowsPath
import json,hashlib,zipfile,subprocess
R=Path.cwd();A=R/'artifacts';O=A/'successor347-ci-independent';O.mkdir(exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
read=lambda p:json.loads(p.read_bytes())
def blobs(head,paths):
 paths=list(dict.fromkeys(paths));assert all(not any(c in p for c in '\r\n\0') for p in paths)
 p=subprocess.run(['git','cat-file','--batch'],cwd=R,input=''.join(head+':'+x+'\n' for x in paths).encode(),capture_output=True);assert p.returncode==0
 raw=p.stdout;offset=0;result={}
 for path in paths:
  end=raw.index(b'\n',offset);header=raw[offset:end].split();assert len(header)==3 and header[1]==b'blob';n=int(header[2]);start=end+1;b=raw[start:start+n];assert len(b)==n and raw[start+n:start+n+1]==b'\n';result[path]=b;offset=start+n+1
 assert offset==len(raw);return result
def load(part,head,rid,jid,aid):
 d=A/f'part{part}-remote-new-review/ci';mapping=read(d/'run-map.json');run=read(d/'run.json');jobs=read(d/'jobs.json')['jobs'];items=read(d/'artifacts.json')['artifacts'];rows=read(d/'download-manifest.json')
 assert mapping['Head']==head and mapping['Run']==rid and mapping['Job']==jid and mapping['Attempt']==1
 assert run['id']==rid and run['head_sha']==head and run['event']=='push' and run['head_branch']=='main' and run['path']=='.github/workflows/validate.yml' and run['run_attempt']==1 and run['status']=='completed' and run['conclusion']=='success' and run['name']=='Validate CLR implementation'
 assert len(jobs)==len(items)==1;job=jobs[0];item=items[0]
 assert job['id']==jid and job['head_sha']==head and job['run_id']==rid and job['run_attempt']==1 and job['status']=='completed' and job['conclusion']=='success' and all(x['conclusion'] in ('success','skipped') for x in job['steps'])
 assert item['id']==aid and item['name']=='validation' and not item['expired'] and item['workflow_run']['id']==rid and item['workflow_run']['head_sha']==head
 paths={x['Path'].replace('\\','/') for x in rows};assert len(rows)==len(paths)
 assert {p.relative_to(d).as_posix() for p in d.rglob('*') if p.is_file() and p.name not in ['download-manifest.json','run-map.json']}==paths
 for row in rows:
  rel=row['Path'].replace('\\','/');assert not PurePosixPath(rel).is_absolute() and not PureWindowsPath(rel).is_absolute() and '..' not in PurePosixPath(rel).parts
  b=(d/rel).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256']
 zpath=d/f'artifact-{aid}.zip';ex=d/f'artifact-{aid}';assert zpath.stat().st_size==item['size_in_bytes'] and item['digest']=='sha256:'+sha(zpath.read_bytes()).lower()
 with zipfile.ZipFile(zpath) as z:
  names=z.namelist();assert len(names)==len(set(names));files={n for n in names if not n.endswith('/')}
  assert {p.relative_to(ex).as_posix() for p in ex.rglob('*') if p.is_file()}==files
  for name in names:
   assert not PurePosixPath(name).is_absolute() and not PureWindowsPath(name).is_absolute() and '..' not in PurePosixPath(name).parts and '..' not in PureWindowsPath(name).parts
   if name in files:assert z.read(name)==(ex/name).read_bytes()
 assert head.encode() in (d/f'job-{jid}.log').read_bytes()
 tests=list(ex.rglob('tests.json'));integrations=list(ex.rglob('integration-results.json'));assert len(tests)==len(integrations)==1
 return d,read(tests[0]),read(integrations[0]),sha(tests[0].read_bytes()),sha((d/'download-manifest.json').read_bytes()),len(rows)


head='2ae381b4f0258d9846bb5b0fc7b9f00ed46b905b';rid=38976969010;jid=117228639180;aid=12079706306
raw,t,integrations,testsha,downloadsha,download_count=load(347,head,rid,jid,aid)
names=[x['Name'] for x in t['Tests']];assert t['Passed']==len(names)==len(set(names))==7259 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
prefix='docs/validation/part-345/full/'
paths=[prefix+x for x in ['tests.json','integration-results.json','source-freeze.json','source-resource-closure.json','completion-guard.json','finalized-supported-mfas.json']]+['docs/supported-mfas.json','docs/module-readiness.json','docs/mfa-compatibility.json']
b=blobs(head,paths);local,local_integrations,freeze,closure,guard,finalized,registry,readiness,ledger=[json.loads(b[p]) for p in paths]
assert local['Passed']==7259 and local['Failed']==0 and names==[x['Name'] for x in local['Tests']]
expected=dict(Readiness='Passed',PackagedReadiness='Passed',Build='Passed',Tests='Passed',OracleUnavailableReport='Expected failure recorded',HybridOutput='Hello World',Incremental='Passed',DisabledPreprocessing='Expected failure',CleanRebuild='Passed',PackageConsumer='Passed',LocalTool='Passed')
assert integrations==local_integrations==guard['IntegrationResults']==expected
oldhead='fe3dc9582997ee1365289e8f08ff20da14e74cb0';oldb=blobs(oldhead,['docs/supported-mfas.json','docs/mfa-compatibility.json']);assert registry==finalized==json.loads(oldb['docs/supported-mfas.json']) and len(registry)==587
assert len({x['Module'] for x in registry})==14 and sum(n.startswith('mfa/') for n in names)==587 and sum(n.startswith('mfa/proc_lib:') for n in names)==39
assert readiness['LocalPassedTests']==7259 and readiness['VerifiedCompatibleMfas']==5 and readiness['KnownDifferenceMfas']==19 and readiness['NeedsVerificationMfas']==563
counts={s:sum(e['Status']==s for e in ledger['Entries']) for s in ['Verified compatible','Known differences','Needs verification']};assert list(counts.values())==[5,19,563]
oldledger=json.loads(oldb['docs/mfa-compatibility.json']);assert len(ledger['Entries'])==len(oldledger['Entries'])==587
changes=[]
for prior,current in zip(oldledger['Entries'],ledger['Entries']):
 assert (prior['Module'],prior['Function'],prior['Arity'])==(current['Module'],current['Function'],current['Arity'])
 if prior!=current:
  assert prior['Module']==current['Module']=='erlang' and prior['Function']==current['Function']=='process_info' and current['Arity'] in (1,2)
  assert prior['Status']=='Needs verification' and current['Status']=='Known differences'
  assert current['Evidence'][:len(prior['Evidence'])]==prior['Evidence'];changes.append(current['Arity'])
assert sorted(changes)==[1,2] and ledger['VerifiedSourceFiles']==oldledger['VerifiedSourceFiles'] and len(ledger['VerifiedSourceFiles'])==30
sourcebindings=blobs(head,[x['Path'] for x in ledger['VerifiedSourceFiles']])
for row in ledger['VerifiedSourceFiles']:assert sha(sourcebindings[row['Path']])==row['Sha256']
evidence_paths={'docs/'+e['Path'] for entry in ledger['Entries'] if entry['Status']=='Verified compatible' or (entry['Module']=='erlang' and entry['Function']=='process_info') for e in entry['Evidence']};ev=blobs(head,evidence_paths)
for entry in ledger['Entries']:
 if entry['Status']=='Verified compatible' or (entry['Module']=='erlang' and entry['Function']=='process_info'):
  for e in entry['Evidence']:assert sha(ev['docs/'+e['Path']])==e['Sha256']
 if entry['Status']=='Verified compatible':assert not entry['Limits'] and {'CompatibilityAudit','OriginalOtpComparison'}<={e['Kind'] for e in entry['Evidence']}
stagepath=A/'root-part347-stage-guard.json';stage=read(stagepath);assert stage['Status']=='EXACT347_CACHED_STAGE_PASS' and len(stage['Files'])==44
pub=blobs(head,[x['Path'] for x in stage['Files']])
for x in stage['Files']:assert len(pub[x['Path']])==x['Bytes'] and sha(pub[x['Path']])==x['Sha256']
assert len(freeze['Files'])==34 and sum(x['Path'].endswith('.cs') for x in freeze['Files'])==25
archives=blobs(head,['docs/validation/part-345/tested-source/'+x['Path'] for x in freeze['Files']]);codepaths=[x['Path'] for x in freeze['Files'] if x['Path'].endswith('.cs')];publishedcode=blobs(head,codepaths)
for x in freeze['Files']:
 archived=archives['docs/validation/part-345/tested-source/'+x['Path']];assert len(archived)==x['Bytes'] and sha(archived)==x['Sha256']
 if x['Path'].endswith('.cs'):assert publishedcode[x['Path']]==archived
changed=subprocess.check_output(['git','diff','--name-only',oldhead,head,'--','src','tests','tools'],cwd=R,text=True).splitlines();assert not any(x.endswith('.cs') for x in changed)
assert guard['SourceClosureRows']==len(closure['Files'])==2751
assert guard['SourceFreezeSha256']==sha(b[prefix+'source-freeze.json']) and guard['SourceClosureSha256']==sha(b[prefix+'source-resource-closure.json']) and guard['TestReportSha256']==sha(b[prefix+'tests.json'])
report={'Status':'PASS_ACTUAL_PUBLISHED347_CI7259_ELEVEN_INTEGRATIONS','Head':head,'Run':rid,'Job':jid,'Artifact':aid,'Passed':7259,'Failed':0,'Integrations':11,'AllPublished345OrderedNamesExact':True,'Registry':587,'All587RegistryRowsOrderAndCustomLimitsExactPublished345':True,'ImplementedModules':14,'ProcLibMfas':39,'CompatibilityCounts':counts,'OnlyClassificationChangesSince345':['erlang:process_info/1 Needs verification -> Known differences','erlang:process_info/2 Needs verification -> Known differences'],'Other585LedgerEntriesAndAll30SourceBindingsExact345':True,'AllVerifiedAndProcessInfoEvidenceRehashed':True,'Published347StageBlobsRehashed':44,'StageGuardSha256':sha(stagepath.read_bytes()),'DownloadManifestSha256':downloadsha,'FrozenDownloadedFilesRehashed':download_count,'TestReportSha256':testsha,'Archived345TestedSourceFilesRehashed':34,'PublishedCSharpRawExactInheritedTested':25,'NoCSharpChanges345To347':True,'InheritedRuntimeSourceClosureRows':2751,'GeneratedPublished347ReadinessSeparatelyVerified':True,'Fresh347FullLocalRunClaimed':False,'SourceReaderSha256':sha(Path(__file__).read_bytes()),'DownloadedCodeExecuted':False,'SdkExecuted':False,'OriginalExecuted':False,'MainModified':False,'NetworkUsed':False}
p=O/'report.json';assert not p.exists();p.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'Report':str(p),'Sha256':sha(p.read_bytes())}))
