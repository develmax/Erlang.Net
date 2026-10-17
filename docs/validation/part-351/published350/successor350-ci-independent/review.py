"""Passive current-head CI review. Only local Git and frozen API/ZIP artifacts are read."""

from pathlib import Path,PurePosixPath,PureWindowsPath

import json,hashlib,zipfile,subprocess

R=Path.cwd();A=R/'artifacts';O=A/'successor350-ci-independent';O.mkdir(exist_ok=True)

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
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args()
head='c1f219f31726fc93e537080f400651d300c47d72';priorhead='712933b97a654a3b6ed42250902017bde0740f17';rid=38983525835;jid=117249607117;aid=12082372762
raw,t,integrations,testsha,downloadsha,download_count=load(350,head,rid,jid,aid)
names=[x['Name'] for x in t['Tests']];assert t['Passed']==len(names)==len(set(names))==7286 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
priorguard=A/'successor349-ci-independent/report.json';assert sha(priorguard.read_bytes())=='3B5D0E65ACE0E5D61450F6D562DA5C23782814FA0F80A0E7D9783BC1D8795112';pg=read(priorguard)
prioractual=A/'part349-remote-new-review/ci/artifact-12081242824/tests.json';assert sha(prioractual.read_bytes())==pg['TestReportSha256'];oldnames=[x['Name'] for x in read(prioractual)['Tests']]
prefix='docs/validation/part-350/full/';paths=[prefix+n for n in ['tests.json','integration-results.json','source-freeze.json','completion-guard.json','registry-before.json','package-resources.json']]+['docs/supported-mfas.json','docs/mfa-compatibility.json','docs/module-readiness.json']
b=blobs(head,paths);local,localint,freeze,guard,before,packages,registry,ledger,readiness=[json.loads(b[x]) for x in paths]
assert names==[x['Name'] for x in local['Tests']] and integrations==localint==guard['IntegrationResults'] and len(integrations)==11
assert guard['Status']=='PASS_ACTUAL_ISOLATED350_FULL' and guard['Passed']==7286 and guard['Failed']==0 and guard['SingleUninterruptedFullPass']
assert sha(b[prefix+'source-freeze.json'])==guard['SourceFreezeSha256']=='B2CA082349C03497A4992D9148D122848EE59FFF517940B30BAF49C4EB0D1AC5'
added=guard['AddedTestNames'];assert len(added)==18 and len(set(added))==18 and set(names)-set(oldnames)==set(added) and [n for n in names if n not in added]==oldnames
assert len(registry)==sum(n.startswith('mfa/') for n in names)==587 and len({r['Module'] for r in registry})==14 and sum(n.startswith('mfa/erlang:') for n in names)==351 and sum(n.startswith('mfa/proc_lib:') for n in names)==39
baseline=blobs(priorhead,['docs/supported-mfas.json','docs/mfa-compatibility.json']);assert b['docs/supported-mfas.json']==baseline['docs/supported-mfas.json']==b[prefix+'registry-before.json']
assert (readiness['RegisteredMfas'],readiness['LocalPassedTests'],readiness['VerifiedCompatibleMfas'],readiness['KnownDifferenceMfas'],readiness['NeedsVerificationMfas'])==(587,7286,10,23,554)
counts={s:sum(e['Status']==s for e in ledger['Entries']) for s in ['Verified compatible','Known differences','Needs verification']};assert list(counts.values())==[10,23,554] and guard['CompatibilityCounts']=={'Verified compatible':10,'Known differences':19,'Needs verification':558}
old=json.loads(baseline['docs/mfa-compatibility.json']);changes=[]
for x,y in zip(old['Entries'],ledger['Entries'],strict=True):
 if x!=y:
  assert x['Module']==y['Module']=='erlang' and x['Function']==y['Function']=='spawn_opt' and x['Arity']==y['Arity'] in [2,3,4,5] and x['Status']=='Needs verification' and y['Status']=='Known differences' and y['Evidence'][:len(x['Evidence'])]==x['Evidence'];changes.append(y['Arity'])
assert set(changes)=={2,3,4,5} and len(changes)==4 and ledger['VerifiedSourceFiles']==old['VerifiedSourceFiles'] and len(ledger['VerifiedSourceFiles'])==59
sources=blobs(head,[r['Path'] for r in ledger['VerifiedSourceFiles']])
for row in ledger['VerifiedSourceFiles']:assert row['Path'].startswith('src/') and sha(sources[row['Path']])==row['Sha256']
evidence=blobs(head,{'docs/'+e['Path'] for entry in ledger['Entries'] for e in entry['Evidence']})
for entry in ledger['Entries']:
 for e in entry['Evidence']:assert sha(evidence['docs/'+e['Path']])==e['Sha256']
 if entry['Status']=='Verified compatible':assert not entry['Limits'] and {'OriginalOtpComparison','CompatibilityAudit'}<={e['Kind'] for e in entry['Evidence']}
stagepath=A/'root-part350-stage-guard.json';sf=read(O/'source-freeze.json');assert sha(stagepath.read_bytes())==sf['StageGuardSha256'];stage=read(stagepath);assert stage['Status']=='PASS_EXACT350_CACHED_STAGE' and len(stage['Files'])==219
pub=blobs(head,[r['Path'] for r in stage['Files']])
for row in stage['Files']:assert len(pub[row['Path']])==row['Bytes'] and sha(pub[row['Path']])==row['Sha256']
assert len(freeze['Files'])==11;archives=blobs(head,['docs/validation/part-350/tested-source/'+r['Path'] for r in freeze['Files']]);actual=blobs(head,[r['Path'] for r in freeze['Files']]);eol=[];cs=0
for row in freeze['Files']:
 snapshot=archives['docs/validation/part-350/tested-source/'+row['Path']];assert len(snapshot)==row['Bytes'] and sha(snapshot)==row['Sha256']
 if actual[row['Path']]!=snapshot:
  assert row['Path'] in ['.gitattributes','.github/workflows/nif-reallocation-native.yml','docs/module-readiness-scope.json'] and actual[row['Path']]==snapshot.replace(b'\r\n',b'\n');eol.append(row['Path'])
 if row['Path'].endswith('.cs'):assert actual[row['Path']]==snapshot;cs+=1
assert cs==7
allpaths=subprocess.check_output(['git','ls-tree','-r','--name-only',priorhead],text=True).splitlines();production=[p for p in allpaths if p.startswith('src/') and p.endswith(('.cs','.csproj','.props','.targets'))];assert blobs(priorhead,production)==blobs(head,production)
packagepaths=[r['Path'] for r in packages['Files']];assert len(packagepaths)==6 and blobs(priorhead,packagepaths)==blobs(head,packagepaths)
pending='tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';assert blobs(head,[pending])[pending]==blobs(priorhead,[pending])[pending]
proof='docs/validation/part-350/independent-full/guard.json';proofbytes=blobs(head,[proof])[proof];assert sha(proofbytes)=='A880ED1A33BE32A6807B0A92B8609F41890F61105683C79A7C0241693476F42C' and json.loads(proofbytes)['CompatibilityCountsAtActualFull']==guard['CompatibilityCounts']
report={'Status':'PASS_ACTUAL_PUBLISHED350_CI7286_ELEVEN_INTEGRATIONS','Head':head,'Run':rid,'Job':jid,'Artifact':aid,'Passed':7286,'Failed':0,'Integrations':11,'AllPrevious7268OrderedNamesPreserved':True,'Added18Names':added,'Registry':587,'AllCustomLimitsRegistryRawExact349':True,'CompatibilityCountsAtPublished350':counts,'ActualFrozenFullCountsSeparatelyPreserved':[10,19,558],'ExactlyFourSpawnOptKnownReportingChanges':changes,'Other583EntriesAndTenVerifiedEvidenceUnchanged':True,'All59RuntimeSourceBindingsAndLedgerEvidenceRehashed':True,'PublishedStageBlobsRehashed':219,'StageGuardSha256':sha(stagepath.read_bytes()),'SourceSnapshotsRehashed':11,'SevenFixtureCSharpRawExact':True,'ExplicitSourcePublicationEolOnlyDifferences':eol,'ProductionSourceAndProjectsRawExact349':len(production),'PackageInputsRawExact349':6,'PendingPlanRawExact349':True,'IndependentLocalFullProofRehashed':True,'GeneratedReadinessSeparateFromFullInputs':True,'DownloadManifestSha256':downloadsha,'FrozenDownloadedFilesRehashed':download_count,'TestReportSha256':testsha,'ReaderSha256':sha(Path(__file__).read_bytes()),'Original350PassClaimed':False,'SdkExecuted':False,'Network':False,'DownloadedCodeExecuted':False,'MainModified':False}
assert not a.output.exists();a.output.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(sha(a.output.read_bytes()))
