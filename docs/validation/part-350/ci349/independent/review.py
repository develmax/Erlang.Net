"""Passive current-head CI review. Only local Git and frozen API/ZIP artifacts are read."""

from pathlib import Path,PurePosixPath,PureWindowsPath

import json,hashlib,zipfile,subprocess

R=Path.cwd();A=R/'artifacts';O=A/'successor349-ci-independent';O.mkdir(exist_ok=True)

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
p=argparse.ArgumentParser();p.add_argument('--job',type=int,required=True);p.add_argument('--artifact',type=int,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
head='712933b97a654a3b6ed42250902017bde0740f17';priorhead='d1bd571fc1f655abf81bdc0336b7133f3d023fac';rid=38981313422
raw,t,integrations,testsha,downloadsha,download_count=load(349,head,rid,a.job,a.artifact)
names=[x['Name'] for x in t['Tests']];assert t['Passed']==len(names)==len(set(names))==7268 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
priorguardpath=A/'successor348-ci-independent/report.json';assert sha(priorguardpath.read_bytes())=='E4B87001BCD9159DC0CD60888C0403AAD59F13209E4A74D5F7719CEFB6D0BD95';priorguard=read(priorguardpath)
previouspath=A/'part348-remote-new-review/ci/artifact-12080353461/tests.json';assert sha(previouspath.read_bytes())==priorguard['TestReportSha256'];assert names==[x['Name'] for x in read(previouspath)['Tests']]
paths=['docs/supported-mfas.json','docs/mfa-compatibility.json','docs/module-readiness.json','docs/validation/part-348/full/tests.json','docs/validation/part-348/full/integration-results.json','docs/validation/part-348/full/source-freeze.json','docs/validation/part-348/full/package-resources.json']
b=blobs(head,paths);registry,ledger,readiness,local,localint,freeze,packages=[json.loads(b[x]) for x in paths]
assert names==[x['Name'] for x in local['Tests']] and integrations==localint and len(integrations)==11
assert len(registry)==sum(n.startswith('mfa/') for n in names)==587 and len({x['Module'] for x in registry})==14
assert sum(n.startswith('mfa/erlang:') for n in names)==351 and sum(n.startswith('mfa/proc_lib:') for n in names)==39
baseline=blobs(priorhead,['docs/supported-mfas.json','docs/mfa-compatibility.json']);assert b['docs/supported-mfas.json']==baseline['docs/supported-mfas.json']
assert (readiness['VerifiedCompatibleMfas'],readiness['KnownDifferenceMfas'],readiness['NeedsVerificationMfas'],readiness['LocalPassedTests'])==(10,19,558,7268)
counts={s:sum(e['Status']==s for e in ledger['Entries']) for s in ['Verified compatible','Known differences','Needs verification']};assert list(counts.values())==[10,19,558] and len(ledger['Entries'])==587
old=json.loads(baseline['docs/mfa-compatibility.json']);changes=[]
for x,y in zip(old['Entries'],ledger['Entries'],strict=True):
 if x!=y:
  assert x['Module']==y['Module']=='erlang' and x['Function']==y['Function'] and x['Arity']==y['Arity']==1 and x['Status']=='Needs verification' and y['Status']=='Verified compatible' and y['Evidence'][:len(x['Evidence'])]==x['Evidence'];changes.append(y['Function'])
assert set(changes)=={'is_atom','is_integer','is_float','is_number','is_tuple'} and len(changes)==5
bindings=ledger['VerifiedSourceFiles'];assert len(bindings)==59 and bindings[:len(old['VerifiedSourceFiles'])]==old['VerifiedSourceFiles'] and len(old['VerifiedSourceFiles'])==35
source=blobs(head,[x['Path'] for x in bindings])
for row in bindings:assert row['Path'].startswith('src/') and sha(source[row['Path']])==row['Sha256']
evidence=blobs(head,{'docs/'+e['Path'] for entry in ledger['Entries'] for e in entry['Evidence']})
for entry in ledger['Entries']:
 for e in entry['Evidence']:assert sha(evidence['docs/'+e['Path']])==e['Sha256']
 if entry['Status']=='Verified compatible':assert not entry['Limits'] and {'CompatibilityAudit','OriginalOtpComparison'}<={e['Kind'] for e in entry['Evidence']}
stagepath=A/'root-part349-stage-guard.json';stage=read(stagepath);sourcefreeze=read(O/'source-freeze.json');assert sha(stagepath.read_bytes())==sourcefreeze['ExactStageGuardSha256'] and stage['Status']=='PASS_EXACT349_STAGE'
published=blobs(head,[x['Path'] for x in stage['Files']]);assert len(published)==len(stage['Files'])
for row in stage['Files']:assert len(published[row['Path']])==row['Bytes'] and sha(published[row['Path']])==row['Sha256']
assert len(freeze['Files'])==25;archives=blobs(head,['docs/validation/part-348/tested-source/'+r['Path'] for r in freeze['Files']]);csrows=[r for r in freeze['Files'] if r['Path'].endswith('.cs')];assert len(csrows)==17
csactual=blobs(head,[r['Path'] for r in csrows])
for row in freeze['Files']:
 snapshot=archives['docs/validation/part-348/tested-source/'+row['Path']];assert len(snapshot)==row['Bytes'] and sha(snapshot)==row['Sha256']
 if row['Path'].endswith('.cs'):assert csactual[row['Path']]==snapshot
allpaths=subprocess.check_output(['git','ls-tree','-r','--name-only',priorhead],cwd=R,text=True).splitlines();runtimepaths=[p for p in allpaths if (p.startswith(('src/','tests/','tools/','examples/')) and p.endswith(('.cs','.csproj','.props','.targets','.slnx'))) or p in ['global.json','.editorconfig','Directory.Build.props','Directory.Build.targets','Directory.Packages.props','NuGet.Config','Erlang.Net.slnx']]
runtimeold=blobs(priorhead,runtimepaths);runtimenew=blobs(head,runtimepaths);assert runtimeold==runtimenew
packagepaths=[r['Path'] for r in packages['Files']];assert len(packagepaths)==6 and blobs(priorhead,packagepaths)==blobs(head,packagepaths)
report={'Status':'PASS_ACTUAL_PUBLISHED349_CI7268_ELEVEN_INTEGRATIONS','Head':head,'Run':rid,'Job':a.job,'Artifact':a.artifact,'Passed':7268,'Failed':0,'Integrations':11,'Exact348OrderedNamesPreserved':True,'Registry':587,'CustomLimitsRegistryRawExact348':True,'CompatibilityCounts':counts,'ExactlyFivePredicatesPromoted':changes,'Other582LedgerEntriesAndOldFiveVerifiedEvidenceExact':True,'CurrentRuntimeSourceBindingsRehashed':59,'Old35BindingsPrefixExact':True,'AllLedgerEvidenceRehashed':True,'PublishedStageBlobsRehashed':len(stage['Files']),'StageGuardSha256':sha(stagepath.read_bytes()),'Frozen348SourceSnapshotsRehashed':25,'SeventeenCSharpRawExactTested348':True,'AllRuntimeBuildInputsRawExactPublished348':len(runtimepaths),'PackageInputsPublishedRawExact348':6,'Inherited348ProjectEolExceptionsUnchangedNotRenormalized':True,'DownloadManifestSha256':downloadsha,'FrozenDownloadedFilesRehashed':download_count,'TestReportSha256':testsha,'SourceReaderSha256':sha(Path(__file__).read_bytes()),'DownloadedCodeExecuted':False,'SdkExecuted':False,'Network':False,'MainModified':False}
assert not a.output.exists();a.output.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(sha(a.output.read_bytes()))
