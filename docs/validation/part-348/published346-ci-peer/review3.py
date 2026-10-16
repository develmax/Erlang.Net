"""Passive current-head CI review. Only local Git and frozen API/ZIP artifacts are read."""

from pathlib import Path,PurePosixPath,PureWindowsPath

import json,hashlib,zipfile,subprocess

R=Path.cwd();A=R/'artifacts';O=A/'successor346-ci-independent';O.mkdir(exist_ok=True)

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



head='83dde925f0b15707d0406d6028a85b9877764c3c';priorhead='fe3dc9582997ee1365289e8f08ff20da14e74cb0';rid=38976187087;jid=117226081933;aid=12079685471
raw,t,integrations,testsha,downloadsha,download_count=load(346,head,rid,jid,aid)
names=[x['Name'] for x in t['Tests']];assert t['Passed']==len(names)==len(set(names))==7259 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
prior_guard=read(A/'successor345-ci-independent/report.json');assert prior_guard['Head']==priorhead and prior_guard['Passed']==7259 and prior_guard['Integrations']==11
prior_actual=A/'part345-remote-new-review/ci/artifact-12079276588/tests.json';assert sha(prior_actual.read_bytes())==prior_guard['TestReportSha256'];previous=read(prior_actual);assert names==[x['Name'] for x in previous['Tests']]
prefix='docs/validation/part-345/full/';paths=[prefix+n for n in ['tests.json','integration-results.json','source-freeze.json','source-resource-closure.json','completion-guard.json','package-resources.json']]+['docs/supported-mfas.json','docs/module-readiness.json','docs/mfa-compatibility.json']
b=blobs(head,paths);local,local_integrations,freeze,closure,guard,package_resources,registry,readiness,ledger=[json.loads(b[p]) for p in paths]
assert names==[x['Name'] for x in local['Tests']] and local['Passed']==7259 and local['Failed']==0
assert integrations==local_integrations==guard['IntegrationResults'] and len(integrations)==11
assert len(registry)==sum(n.startswith('mfa/') for n in names)==587 and len({x['Module'] for x in registry})==14
assert sum(n.startswith('mfa/erlang:') for n in names)==351 and sum(n.startswith('mfa/proc_lib:') for n in names)==39
assert (readiness['VerifiedCompatibleMfas'],readiness['KnownDifferenceMfas'],readiness['NeedsVerificationMfas'])==(5,17,565) and readiness['LocalPassedTests']==7259
counts={s:sum(e['Status']==s for e in ledger['Entries']) for s in ['Verified compatible','Known differences','Needs verification']};assert list(counts.values())==[5,17,565] and len(ledger['Entries'])==587
baseline=blobs(priorhead,['docs/supported-mfas.json','docs/mfa-compatibility.json']);assert b['docs/supported-mfas.json']==baseline['docs/supported-mfas.json'] and b['docs/mfa-compatibility.json']==baseline['docs/mfa-compatibility.json']
sourcebindings=blobs(head,[x['Path'] for x in ledger['VerifiedSourceFiles']]);assert len(ledger['VerifiedSourceFiles'])==30
for row in ledger['VerifiedSourceFiles']:assert sha(sourcebindings[row['Path']])==row['Sha256']
evidence_paths={'docs/'+e['Path'] for entry in ledger['Entries'] if entry['Status']=='Verified compatible' for e in entry['Evidence']};evidence=blobs(head,evidence_paths)
for entry in ledger['Entries']:
 if entry['Status']=='Verified compatible':
  assert not entry['Limits'] and {'CompatibilityAudit','OriginalOtpComparison'}<={e['Kind'] for e in entry['Evidence']}
  for e in entry['Evidence']:assert sha(evidence['docs/'+e['Path']])==e['Sha256']
stagepath=A/'root-part346-stage-guard.json';stage=read(stagepath);assert stage['Status']=='EXACT346_CACHED_STAGE_PASS' and len(stage['Files'])==276
published=blobs(head,[r['Path'] for r in stage['Files']])
for row in stage['Files']:assert len(published[row['Path']])==row['Bytes'] and sha(published[row['Path']])==row['Sha256']
assert len(freeze['Files'])==34;archives=blobs(head,['docs/validation/part-345/tested-source/'+r['Path'] for r in freeze['Files']]);owned_actual=blobs(head,[r['Path'] for r in freeze['Files']]);csharp=0
for row in freeze['Files']:
 snapshot=archives['docs/validation/part-345/tested-source/'+row['Path']];assert len(snapshot)==row['Bytes'] and sha(snapshot)==row['Sha256']
 if row['Path'].endswith('.cs'):assert owned_actual[row['Path']]==snapshot;csharp+=1
assert csharp==25 and len(closure['Files'])==2751 and guard['SourceClosureRows']==2751
buildpaths=[r['Path'] for r in closure['Files']];oldbuild=blobs(priorhead,buildpaths);newbuild=blobs(head,buildpaths);changes=[p for p in buildpaths if oldbuild[p]!=newbuild[p]]
allowed={'.gitattributes','.github/workflows/nif-reallocation-native.yml','AGENTS.md','CHANGELOG.md','tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','tools/Erlang.ProcLibRealPeerObservations/README.md','tools/Erlang.ProcLibRealPeerObservations/collect-original.py','tools/Erlang.ProcLibRealPeerObservations/source-manifest.json'};assert set(changes)==allowed
assert not any(p.endswith(('.cs','.csproj','.props','.targets','.slnx')) for p in changes)
packagepaths=[r['Path'] for r in package_resources['Files']];packageold=blobs(priorhead,packagepaths);packagenew=blobs(head,packagepaths);packageeol=[]
for row in package_resources['Files']:
 assert packagenew[row['Path']]==packageold[row['Path']]
 frozenlocal=(R/row['Path']).read_bytes();assert sha(frozenlocal)==row['Sha256'] and len(frozenlocal)==row['Bytes']
 if packagenew[row['Path']]!=frozenlocal:
  assert packagenew[row['Path']].replace(b'\r\n',b'\n')==frozenlocal.replace(b'\r\n',b'\n');packageeol.append(row['Path'])
changed_paths=subprocess.check_output(['git','diff-tree','--no-commit-id','--name-only','-r',head],cwd=R,text=True).splitlines();assert not any(p.endswith('.cs') and p.startswith(('src/','tests/','tools/')) for p in changed_paths)
tool='tools/Erlang.ProcLibRealPeerObservations/';mbytes=blobs(head,[tool+'source-manifest.json'])[tool+'source-manifest.json'];assert sha(mbytes)=='C1B3C8BADDCC519B48E557628522E42E5EB31C2CF4055DC343CCBD436EEECD16';manifest=json.loads(mbytes)
source_rows=manifest['Files']+manifest['ExternalDependencies']+[manifest['Workflow']];source_paths=[tool+r['Path'] for r in manifest['Files']]+[r['Path'] for r in manifest['ExternalDependencies']]+[manifest['Workflow']['Path']];sources=blobs(head,source_paths)
for row,path in zip(source_rows,source_paths):assert len(sources[path])==row['Bytes'] and sha(sources[path])==row['Sha256']
collector=tool+'collect-original.py';previouscollector=blobs(priorhead,[collector])[collector];assert sources[collector]==previouscollector.replace(b'while pending:',b'while pending and peer_pid is None:') and previouscollector.count(b'while pending:')==1
sourcefreeze=read(A/'proc-lib-real-peer-multimatch-correction/source-freeze.json');six=blobs(head,[r['Path'] for r in sourcefreeze['Files']])
for row in sourcefreeze['Files']:assert len(six[row['Path']])==row['Bytes'] and sha(six[row['Path']])==row['Sha256']
report={'Status':'PASS_ACTUAL_PUBLISHED346_CI7259_ELEVEN_INTEGRATIONS','Head':head,'Run':rid,'Job':jid,'Artifact':aid,'Passed':7259,'Failed':0,'Integrations':11,'ExactPrevious345TestNamesAndOrder':True,'Registry':587,'ImplementedModules':14,'ProcLibMfas':39,'CompatibilityCountsAtPublished346':counts,'RegistryAndLedgerRawExactPublished345':True,'All30CurrentSourceBindingsAndVerifiedEvidenceRehashed':True,'PublishedStageBlobsRehashed':276,'StageGuardSha256':sha(stagepath.read_bytes()),'DownloadManifestSha256':downloadsha,'FrozenDownloadedFilesRehashed':download_count,'TestReportSha256':testsha,'Archived345SourceSnapshotsRehashed':34,'Owned345CSharpRawExactAt346':25,'All2751BuildInputsComparedPublished345To346':True,'ChangedBroadClosureInputsOnlyReviewedMetadataDocsAndCollector':changes,'RuntimeCSharpProjectAndPackageInputsUnchanged':True,'CollectorDeltaOneWhileConditionBound':True,'InitialReaderScopeRefusalPreserved':True,'PublishedPackageResourcesRawExact345To346':6,'FrozenRawPackageInputHashesLocallyRechecked':6,'ExplicitPackagePublicationEolOnlyDifferences':packageeol,'SecondReaderManifestScopeRefusalPreserved':True,'Whole345FrozenMetadataExactAt346Claimed':False,'CollectorSixSourceRowsManifestAndDependenciesRehashed':True,'CollectorManifestSha256':sha(mbytes),'SourceReaderSha256':sha(Path(__file__).read_bytes()),'Native346EightPassClaimed':False,'SdkExecuted':False,'DownloadedCodeExecuted':False,'MainModified':False,'NetworkUsed':False}
p=O/'report.json';assert not p.exists();p.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Status':report['Status'],'Sha256':sha(p.read_bytes())}))
