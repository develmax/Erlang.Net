"""Passive current-head CI review. Only local Git and frozen API/ZIP artifacts are read."""
from pathlib import Path,PurePosixPath,PureWindowsPath
import json,hashlib,zipfile,subprocess
R=Path.cwd();A=R/'artifacts';O=A/'successor345-ci-independent';O.mkdir(exist_ok=True)
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

head='fe3dc9582997ee1365289e8f08ff20da14e74cb0';rid=38975736167;jid=117224608728;aid=12079276588
raw,t,integrations,testsha,downloadsha,download_count=load(345,head,rid,jid,aid)
names=[x['Name'] for x in t['Tests']];assert t['Passed']==len(names)==len(set(names))==7259 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
prefix='docs/validation/part-345/full/'
paths=[prefix+x for x in ['tests.json','integration-results.json','source-freeze.json','source-resource-closure.json','completion-guard.json','finalized-supported-mfas.json','finalized.json','registry-before.json']]+['docs/supported-mfas.json','docs/module-readiness.json','docs/mfa-compatibility.json','docs/module-readiness-scope.json']
b=blobs(head,paths)
local,local_integrations,freeze,closure,guard,finalized,finalizer,before,registry,readiness,ledger,scope=[json.loads(b[p]) for p in paths]
assert local['Passed']==7259 and local['Failed']==0 and names==[x['Name'] for x in local['Tests']]
expected=dict(Readiness='Passed',PackagedReadiness='Passed',Build='Passed',Tests='Passed',OracleUnavailableReport='Expected failure recorded',HybridOutput='Hello World',Incremental='Passed',DisabledPreprocessing='Expected failure',CleanRebuild='Passed',PackageConsumer='Passed',LocalTool='Passed')
assert integrations==local_integrations==guard['IntegrationResults']==expected
oldhead='9c7949f87348223a61c65d37d859b02841036374';oldpaths=['docs/validation/part-343/full/tests.json','docs/supported-mfas.json','docs/mfa-compatibility.json'];oldb=blobs(oldhead,oldpaths);old=json.loads(oldb[oldpaths[0]]);oldnames=[x['Name'] for x in old['Tests']];oldset=set(oldnames)
assert old['Passed']==7224 and old['Failed']==0 and [n for n in names if n in oldset]==oldnames
assert [n for n in names if n not in oldset]==guard['AddedTestNames'] and len(guard['AddedTestNames'])==35
assert registry==finalized==before==json.loads(oldb['docs/supported-mfas.json']) and len(registry)==587
assert sum(n.startswith('mfa/') for n in names)==587 and len({x['Module'] for x in registry})==14
assert sum(n.startswith('mfa/erlang:') for n in names)==351 and sum(n.startswith('mfa/proc_lib:') for n in names)==39
assert (readiness['VerifiedCompatibleMfas'],readiness['KnownDifferenceMfas'],readiness['NeedsVerificationMfas'])==(5,17,565) and readiness['LocalPassedTests']==7259
counts={s:sum(e['Status']==s for e in ledger['Entries']) for s in ['Verified compatible','Known differences','Needs verification']};assert list(counts.values())==[5,17,565]
priorledger=json.loads(oldb['docs/mfa-compatibility.json']);assert len(ledger['Entries'])==len(priorledger['Entries'])==587
for prior,current in zip(priorledger['Entries'],ledger['Entries']):
 assert {k:v for k,v in prior.items() if k!='Evidence'}=={k:v for k,v in current.items() if k!='Evidence'}
 assert current['Evidence'][:len(prior['Evidence'])]==prior['Evidence']
 assert all(e['Kind']=='SourceReview' for e in current['Evidence'][len(prior['Evidence']):])
sourcebindings=blobs(head,[x['Path'] for x in ledger['VerifiedSourceFiles']]);assert len(ledger['VerifiedSourceFiles'])==30
for row in ledger['VerifiedSourceFiles']:assert sha(sourcebindings[row['Path']])==row['Sha256']
evidence_paths={'docs/'+e['Path'] for entry in ledger['Entries'] if entry['Status']=='Verified compatible' for e in entry['Evidence']};ev=blobs(head,evidence_paths)
for entry in ledger['Entries']:
 if entry['Status']=='Verified compatible':
  assert not entry['Limits'] and {'CompatibilityAudit','OriginalOtpComparison'}<={e['Kind'] for e in entry['Evidence']}
  for e in entry['Evidence']:assert sha(ev['docs/'+e['Path']])==e['Sha256']
stage_path=A/'root-part345-stage-guard.json';stage=read(stage_path);assert stage['Status']=='EXACT345_CACHED_STAGE_PASS' and len(stage['Files'])==451
published=blobs(head,[x['Path'] for x in stage['Files']])
for row in stage['Files']:assert len(published[row['Path']])==row['Bytes'] and sha(published[row['Path']])==row['Sha256']
archives=blobs(head,['docs/validation/part-345/tested-source/'+x['Path'] for x in freeze['Files']]);current=blobs(head,[x['Path'] for x in freeze['Files']]);assert len(freeze['Files'])==34 and sum(x['Path'].endswith('.cs') for x in freeze['Files'])==25
metadata_lf={'.gitattributes','.github/workflows/nif-pending-native.yml'};api_eof=[]
for row in freeze['Files']:
 tested=archives['docs/validation/part-345/tested-source/'+row['Path']];assert len(tested)==row['Bytes'] and sha(tested)==row['Sha256']
 actual=current[row['Path']]
 if row['Path'] in metadata_lf:assert actual.replace(b'\r\n',b'\n')==tested.replace(b'\r\n',b'\n')
 elif row['Path']=='docs/module-readiness-scope.json':
  assert actual.rstrip(b'\r\n')==tested.rstrip(b'\r\n')
  if actual!=tested:api_eof.append(row['Path'])
 else:assert actual==tested,row['Path']
assert guard['SourceClosureRows']==len(closure['Files'])==2751
assert guard['SourceFreezeSha256']==sha(b[prefix+'source-freeze.json']) and guard['SourceClosureSha256']==sha(b[prefix+'source-resource-closure.json']) and guard['TestReportSha256']==sha(b[prefix+'tests.json'])
assert finalizer['FullGuardSha256']==sha(b[prefix+'completion-guard.json']) and finalizer['FinalizedRegistrySha256']==sha(b[prefix+'finalized-supported-mfas.json']) and finalizer['All587CustomLimitsRestored']
report={'Status':'PASS_ACTUAL_PUBLISHED345_CI7259_ELEVEN_INTEGRATIONS','Head':head,'Run':rid,'Job':jid,'Artifact':aid,'Passed':7259,'Failed':0,'Integrations':11,'Registry':587,'ImplementedModules':14,'ProcLibMfas':39,'CompatibilityCounts':counts,'AllPrevious7224OrderedNamesPreserved':True,'Added35OrderedNamesExact':True,'All587RegistryRowsOrderAndCustomLimitsExactPublished344':True,'PublishedStageBlobsRehashed':451,'StageGuardSha256':sha(stage_path.read_bytes()),'DownloadManifestSha256':downloadsha,'FrozenDownloadedFilesRehashed':download_count,'TestReportSha256':testsha,'ArchivedTestedSourceFilesRehashed':34,'PublishedCSharpRawExactTested':25,'OwnedTextLfComparisonsOnly':sorted(metadata_lf),'ApiTextTrailingEofOnlyComparisons':api_eof,'GeneratedReadinessSeparatelyVerified':True,'All30CurrentSourceBindingsAndVerifiedOriginalEvidenceRehashed':True,'Prior587LedgerClassificationsAndScopesLimitsAndEvidencePrefixesPreserved':True,'SourceReaderSha256':sha(Path(__file__).read_bytes()),'Original345ResultClaimed':False,'DownloadedCodeExecuted':False,'SdkExecuted':False,'MainModified':False,'NetworkUsed':False}
p=O/'report.json';assert not p.exists();p.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'Report':str(p),'Sha256':sha(p.read_bytes())}))
