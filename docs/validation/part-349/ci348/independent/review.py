"""Passive current-head CI review. Only local Git and frozen API/ZIP artifacts are read."""

from pathlib import Path,PurePosixPath,PureWindowsPath

import json,hashlib,zipfile,subprocess

R=Path.cwd();A=R/'artifacts';O=A/'successor348-ci-independent';O.mkdir(exist_ok=True)

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



import argparse
p=argparse.ArgumentParser(description='Passive exact published348 CI audit; terminal capture must already be frozen.')
p.add_argument('--job',type=int,required=True);p.add_argument('--artifact',type=int,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
head='d1bd571fc1f655abf81bdc0336b7133f3d023fac';priorhead='2ae381b4f0258d9846bb5b0fc7b9f00ed46b905b';rid=38979387792
raw,t,integrations,testsha,downloadsha,download_count=load(348,head,rid,a.job,a.artifact)
names=[x['Name'] for x in t['Tests']];assert t['Passed']==len(names)==len(set(names))==7268 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
priorguard=read(A/'successor347-ci-independent/report.json');assert priorguard['Head']==priorhead and priorguard['Passed']==7259 and priorguard['Integrations']==11
prioractual=A/'part347-remote-new-review/ci'/f"artifact-{priorguard['Artifact']}"/'tests.json'
assert sha(prioractual.read_bytes())==priorguard['TestReportSha256'];previous=read(prioractual);oldnames=[x['Name'] for x in previous['Tests']]
prefix='docs/validation/part-348/full/'
paths=[prefix+n for n in ['tests.json','integration-results.json','source-freeze.json','source-resource-closure.json','completion-guard.json','package-resources.json']]+['docs/supported-mfas.json','docs/module-readiness.json','docs/mfa-compatibility.json']
b=blobs(head,paths);local,localintegrations,freeze,closure,guard,packages,registry,readiness,ledger=[json.loads(b[x]) for x in paths]
assert guard['Status']=='PASS_ACTUAL_ISOLATED348_FULL' and guard['SingleUninterruptedFullPass'] and guard['Passed']==7268 and guard['Failed']==0
assert sha(b[prefix+'source-freeze.json'])==guard['SourceFreezeSha256']=='08A9020739E9E8C3ADA1741FDBB099779B02D41ECF3D114C24E691DF3A2A488B'
assert sha(b[prefix+'source-resource-closure.json'])==guard['SourceClosureSha256']=='A76624B096F08F6F6781EF2ACE25D1D268A04E39376DEC4F5416FDD05372FDFA'
assert names==[x['Name'] for x in local['Tests']] and local['Passed']==7268 and local['Failed']==0
added=guard['AddedTestNames'];assert len(added)==len(set(added))==9 and set(added)==set(names)-set(oldnames) and [n for n in names if n not in added]==oldnames
assert integrations==localintegrations==guard['IntegrationResults'] and len(integrations)==11
assert len(registry)==sum(n.startswith('mfa/') for n in names)==587 and len({x['Module'] for x in registry})==14
assert sum(n.startswith('mfa/erlang:') for n in names)==351 and sum(n.startswith('mfa/proc_lib:') for n in names)==39
baseline=blobs(priorhead,['docs/supported-mfas.json','docs/mfa-compatibility.json']);assert b['docs/supported-mfas.json']==baseline['docs/supported-mfas.json']
assert (readiness['VerifiedCompatibleMfas'],readiness['KnownDifferenceMfas'],readiness['NeedsVerificationMfas'],readiness['LocalPassedTests'])==(5,19,563,7268)
counts={s:sum(e['Status']==s for e in ledger['Entries']) for s in ['Verified compatible','Known differences','Needs verification']};assert list(counts.values())==[5,19,563] and len(ledger['Entries'])==587
oldledger=json.loads(baseline['docs/mfa-compatibility.json']);assert len(oldledger['VerifiedSourceFiles'])==30 and len(ledger['VerifiedSourceFiles'])==35
for x,y in zip(oldledger['Entries'],ledger['Entries'],strict=True):
 if x!=y:
  assert x['Status']==y['Status']=='Verified compatible' and all(x[k]==y[k] for k in x if k!='Evidence') and y['Evidence'][:len(x['Evidence'])]==x['Evidence']
sourcebindings=blobs(head,[x['Path'] for x in ledger['VerifiedSourceFiles']])
for row in ledger['VerifiedSourceFiles']:assert sha(sourcebindings[row['Path']])==row['Sha256']
oldbindings={x['Path']:x['Sha256'] for x in oldledger['VerifiedSourceFiles']};newbindings={x['Path']:x['Sha256'] for x in ledger['VerifiedSourceFiles']};assert all(newbindings[k]==v for k,v in oldbindings.items())
assert set(newbindings)-set(oldbindings)=={'src/Erlang.Terms/'+n+'.cs' for n in ['TermOrder','TermComparisonStep','TermHashFrame','TermHashStep','TermHashStepKind']}
evidencepaths={'docs/'+e['Path'] for entry in ledger['Entries'] for e in entry['Evidence']};evidence=blobs(head,evidencepaths)
for entry in ledger['Entries']:
 if entry['Status']=='Verified compatible':assert not entry['Limits'] and {'CompatibilityAudit','OriginalOtpComparison'}<={e['Kind'] for e in entry['Evidence']}
 for e in entry['Evidence']:assert sha(evidence['docs/'+e['Path']])==e['Sha256']
stagepath=A/'root-part348-stage-guard.json';stage=read(stagepath);assert stage['Status']=='EXACT348_CACHED_STAGE_PASS' and len(stage['Files'])==426
published=blobs(head,[r['Path'] for r in stage['Files']])
for row in stage['Files']:assert len(published[row['Path']])==row['Bytes'] and sha(published[row['Path']])==row['Sha256']
assert len(freeze['Files'])==25 and len(closure['Files'])==2764
archives=blobs(head,['docs/validation/part-348/tested-source/'+r['Path'] for r in freeze['Files']]);ownedactual=blobs(head,[r['Path'] for r in freeze['Files']]);eol=[];csharp=0
alloweol={'.gitattributes','.github/workflows/nif-pending-native.yml','.github/workflows/nif-reallocation-native.yml','docs/module-scope.json'}
for row in freeze['Files']:
 snapshot=archives['docs/validation/part-348/tested-source/'+row['Path']];assert len(snapshot)==row['Bytes'] and sha(snapshot)==row['Sha256']
 actual=ownedactual[row['Path']]
 if actual!=snapshot:
  assert row['Path'] in alloweol and actual.replace(b'\r\n',b'\n')==snapshot.replace(b'\r\n',b'\n');eol.append(row['Path'])
 if row['Path'].endswith('.cs'):assert actual==snapshot;csharp+=1
buildrows=[r for r in closure['Files'] if r['Path'].endswith(('.cs','.csproj','.props','.targets','.slnx')) or r['Path'] in ['.editorconfig','global.json','NuGet.Config']]
buildblobs=blobs(head,[r['Path'] for r in buildrows])
for row in buildrows:assert len(buildblobs[row['Path']])==row['Bytes'] and sha(buildblobs[row['Path']])==row['Sha256']
packageblobs=blobs(head,[r['Path'] for r in packages['Files']]);packageeol=[];assert len(packages['Files'])==6
for row in packages['Files']:
 frozen=(R/row['Path']).read_bytes();assert sha(frozen)==row['Sha256'] and len(frozen)==row['Bytes']
 if packageblobs[row['Path']]!=frozen:
  assert packageblobs[row['Path']].replace(b'\r\n',b'\n')==frozen.replace(b'\r\n',b'\n');packageeol.append(row['Path'])
report={'Status':'PASS_ACTUAL_PUBLISHED348_CI7268_ELEVEN_INTEGRATIONS','Head':head,'Run':rid,'Job':a.job,'Artifact':a.artifact,'Passed':7268,'Failed':0,'Integrations':11,'AllPrevious7259OrderedNamesPreserved':True,'AddedNames':added,'Registry':587,'AllRegistryRowsAndCustomLimitsRawExact347':True,'CompatibilityCounts':counts,'All35CurrentSourceBindingsAndAllLedgerEvidenceRehashed':True,'PublishedStageBlobsRehashed':426,'StageGuardSha256':sha(stagepath.read_bytes()),'DownloadManifestSha256':downloadsha,'FrozenDownloadedFilesRehashed':download_count,'TestReportSha256':testsha,'OwnedFrozenSourceSnapshots':25,'OwnedCSharpRawExact':csharp,'ExplicitOwnedPublicationEolOnlyDifferences':eol,'FrozenRuntimeBuildInputsRehashedAtPublishedHead':len(buildrows),'InheritedPackageResourcesFrozenHashesRechecked':6,'ExplicitPackagePublicationEolOnlyDifferences':packageeol,'GeneratedReadinessSeparatelyChecked':True,'Whole2764FrozenClosureRawIdentityClaimed':False,'SourceReaderSha256':sha(Path(__file__).read_bytes()),'DownloadedCodeExecuted':False,'SdkExecuted':False,'OriginalExecuted':False,'NetworkUsed':False,'MainModified':False}
assert not a.output.exists();a.output.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(sha(a.output.read_bytes()))
