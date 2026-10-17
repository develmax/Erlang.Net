"""Passive final corrected published352 CI audit. Never execute downloaded source or guest code."""
from pathlib import Path
import collections,hashlib,importlib.util,json,re,subprocess,sys
sys.dont_write_bytecode=True
F=Path(__file__).resolve().parent
s=importlib.util.spec_from_file_location('common352',F/'common352.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
H='4c9cb16e7c52bc10e54ceaf652797d100def488b';BASE='547a2dd266586589a90c7883dac88d09a5a1de20';INITIAL='c09a075b7921b3875f526ea0da9d011c263e3bad'
mp,raw,run,job,items,downloaded=m.load('ci')
assert mp['Head']==H and run['id']==38991296407 and mp['Job']==job['id'] and mp['Attempt']==run['run_attempt']==1
assert run['path']=='.github/workflows/validate.yml' and run['event']=='push' and run['head_branch']=='main'
assert run['conclusion']==job['conclusion']=='success' and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
assert len(items)==1 and items[0]['workflow_run']['id']==run['id'] and items[0]['name']=='validation'
log=(raw/f'job-{job["id"]}.log').read_text()
assert 'origin +'+H+':refs/remotes/origin/main' in log and len(re.findall(r'^\S+ '+H+r'\s*$',log,re.M))>=2
ex=raw/f'artifact-{items[0]["id"]}';tp=list(ex.rglob('tests.json'));ip=list(ex.rglob('integration-results.json'));assert len(tp)==len(ip)==1
tests=m.read(tp[0]);integration=m.read(ip[0]);names=[x['Name'] for x in tests['Tests']]
assert tests['Passed']==len(names)==len(set(names))==7297 and tests['Failed']==0 and all(x['Status']=='Passed' for x in tests['Tests'])
prefix='docs/validation/part-351/full/'
paths=[prefix+n for n in ['tests.json','integration-results.json','completion-guard.json','source-freeze.json','source-resource-closure.json','package-resources.json','registry-before.json']]+['docs/validation/part-351/independent-full/guard.json','docs/supported-mfas.json','docs/mfa-compatibility.json','docs/module-readiness.json','.github/workflows/validate.yml','docs/validation/part-352/root-promotion-correction/correction.json']
published=m.blobs(H,paths);local,localintegration,full,source,closure,package,before,peer,registry,ledger,readiness,workflow,correction=[json.loads(published[p]) if not p.endswith('.yml') else published[p] for p in paths]
assert names==[x['Name'] for x in local['Tests']] and local['Passed']==full['Passed']==peer['Passed']==7297 and local['Failed']==full['Failed']==peer['Failed']==0
assert m.sha(published['docs/validation/part-351/independent-full/guard.json'])=='B9C1DEFFAB121801E80D278A4512A0CA43787623D8B1C1D0201AF2A5A0FDD165'
assert full['SourceFreezeSha256']==m.sha(published[prefix+'source-freeze.json'])=='C5FC54F85ED13C883C7C985EA3F88495E7E4EC6EFE515DDEB3129E23A42D9B1F'
assert full['SourceClosureSha256']==m.sha(published[prefix+'source-resource-closure.json'])=='B3897680F2AA4222E258D350D1FC163EC673E205B59D2E341D8FC0A531E36C6F'
expected=dict(Readiness='Passed',PackagedReadiness='Passed',Build='Passed',Tests='Passed',OracleUnavailableReport='Expected failure recorded',HybridOutput='Hello World',Incremental='Passed',DisabledPreprocessing='Expected failure',CleanRebuild='Passed',PackageConsumer='Passed',LocalTool='Passed')
assert integration==localintegration==full['IntegrationResults']==expected
assert m.blobs(BASE,[p for p in paths if p.startswith(prefix) or p=='docs/validation/part-351/independent-full/guard.json'])=={p:b for p,b in published.items() if p.startswith(prefix) or p=='docs/validation/part-351/independent-full/guard.json'}
assert len(registry)==sum(n.startswith('mfa/') for n in names)==587 and len({x['Module'] for x in registry})==14
assert published['docs/supported-mfas.json']==published[prefix+'registry-before.json']==m.blobs(BASE,['docs/supported-mfas.json'])['docs/supported-mfas.json']
assert (readiness['RegisteredMfas'],readiness['LocalPassedTests'],readiness['VerifiedCompatibleMfas'],readiness['KnownDifferenceMfas'],readiness['NeedsVerificationMfas'])==(587,7297,16,24,547)
assert dict(collections.Counter(x['Status'] for x in ledger['Entries']))=={'Verified compatible':16,'Known differences':24,'Needs verification':547}
assert full['CompatibilityCounts']=={'Verified compatible':10,'Known differences':24,'Needs verification':553} and full['CurrentRuntimeSourceBindings']==74
assert len(ledger['VerifiedSourceFiles'])==len({r['Path'] for r in ledger['VerifiedSourceFiles']})==77
bindings=m.blobs(H,[r['Path'] for r in ledger['VerifiedSourceFiles']])
for r in ledger['VerifiedSourceFiles']:assert r['Path'].startswith('src/') and m.sha(bindings[r['Path']])==r['Sha256']
prior=json.loads(m.blobs(BASE,['docs/mfa-compatibility.json'])['docs/mfa-compatibility.json']);assert ledger['VerifiedSourceFiles'][:74]==prior['VerifiedSourceFiles']
assert {r['Path'] for r in ledger['VerifiedSourceFiles'][74:]}=={'src/Erlang.Runtime/ErlangBasicRegistrations.cs','src/Erlang.Runtime/ErlangBasicBifNames.cs','src/Erlang.Compiler/AutoImportNames.cs'}
changed=[]
for x,y in zip(prior['Entries'],ledger['Entries']):
 if x!=y:
  assert x['Module']==y['Module']=='erlang' and x['Function']==y['Function'] and x['Arity']==y['Arity']==1
  assert x['Status']=='Needs verification' and y['Status']=='Verified compatible' and y['Limits']==[] and y['Evidence'][:len(x['Evidence'])]==x['Evidence'];changed.append(y['Function'])
 else:assert x==y
assert set(changed)=={'is_boolean','is_function','is_list','is_pid','is_port','is_reference'} and len(changed)==6
assert all(x==y for x,y in zip(prior['Entries'],ledger['Entries']) if x['Status']=='Verified compatible')
epaths={'docs/'+r['Path'] for e in ledger['Entries'] for r in e['Evidence']};evidence=m.blobs(H,epaths)
for e in ledger['Entries']:
 for r in e['Evidence']:assert m.sha(evidence['docs/'+r['Path']])==r['Sha256']
# The131 initial staged blobs remain historical authority; final correction overrides precisely two and adds five.
preparation=m.read(F/'source-freeze.json');manifestpath=F/'published-source-manifest.json';assert m.sha(manifestpath.read_bytes())==preparation['PublishedSourceManifestSha256'];manifest=m.read(manifestpath)
assert manifest['Head']==H and manifest['Base']==BASE and manifest['InitialHead']==INITIAL and len(manifest['FinalBlobs'])==136
stagepath=m.A/'root-part352-promotion/stage-guard.json';correctionpath=m.A/'root-part352-attrs-correction/files.json'
assert m.sha(stagepath.read_bytes())==preparation['InitialStageGuardSha256']==manifest['InitialStageSha256']
assert m.sha(correctionpath.read_bytes())==preparation['CorrectionFilesSha256']==manifest['CorrectionFilesSha256']
stage=m.read(stagepath);repair=m.read(correctionpath);assert stage['Status']=='PASS_EXACT131_STAGED352_PATHS_AND_BYTE_POLICY' and len(stage['Files'])==131 and len(repair['Files'])==7
initialblobs=m.blobs(INITIAL,[r['Path'] for r in stage['Files']]);final=m.blobs(H,[r['Path'] for r in manifest['FinalBlobs']]);corrected={r['Path'] for r in repair['Files']}
assert {r['Path'] for r in stage['Files']} & corrected=={'.gitattributes','docs/changelog/0055.md'}
for r in stage['Files']:
 assert m.sha(initialblobs[r['Path']])==r['StagedSha256']
 if r['Path'] not in corrected:assert final[r['Path']]==initialblobs[r['Path']]
for r in manifest['FinalBlobs']:assert len(final[r['Path']])==r['Bytes'] and m.sha(final[r['Path']])==r['Sha256']
for r in manifest['CorrectionBindings']:assert m.sha(final[r['Path']])==r['PublishedSha256']
assert m.sha(final['.gitattributes'])=='ECDA73879045E0EB47D4CE6BBB31E355C562814D284F93CEDA69CC52E4A686F5'
assert correction['Status']=='ACTUAL_ROOT352_ATTRIBUTE_ORDER_PUBLICATION_ERROR_CORRECTED' and correction['InitialPublishedHead']==INITIAL and correction['InitialCiRun']==38991076980
# Entire runtime/compiler/test/tool code/build inputs are unchanged since351; only reporting/proof/native attrs metadata differ.
process=subprocess.run(['git','diff','--name-only',BASE,H],cwd=m.ROOT if hasattr(m,'ROOT') else Path.cwd(),capture_output=True,text=True);assert process.returncode==0;diff=process.stdout.splitlines();assert len(diff)==136 and set(diff)==set(final)
allowed={'.gitattributes','AGENTS.md','CHANGELOG.md','.github/workflows/nif-reallocation-native.yml','tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'}
assert all(p.startswith('docs/') or p in allowed for p in diff)
assert workflow==m.blobs(BASE,['.github/workflows/validate.yml'])['.github/workflows/validate.yml'] and b'./tools/validate.ps1' in workflow
buildpaths=[r['Path'] for r in closure['Files'] if r['Path'].startswith(('src/','tests/','tools/')) and r['Path']!='tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json']
assert m.blobs(H,buildpaths)==m.blobs(BASE,buildpaths)
assert len(package['Files'])==6 and m.blobs(H,[r['Path'] for r in package['Files']])==m.blobs(BASE,[r['Path'] for r in package['Files']])
# Admit native metadata only after final corrected attrs bytes, with every row and both seals exact.
plans=['tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'];pb=m.blobs(H,plans);native=[]
for p,n,key,w in [(plans[0],1262,'REVIEWED_SOURCE_PLAN_SHA256','.github/workflows/nif-pending-native.yml'),(plans[1],1287,'REVIEWED_REALLOCATION_PLAN_SHA256','.github/workflows/nif-reallocation-native.yml')]:
 v=json.loads(pb[p]);assert len(v['Files'])==n and b'\r' not in pb[p];rawfiles=m.blobs(H,[r['Path'] for r in v['Files']])
 for r in v['Files']:
  b=rawfiles[r['Path']]
  if r['HashMode']=='TextLF':b=b.replace(b'\r\n',b'\n')
  else:assert r['HashMode']=='BinaryExact'
  assert len(b)==r['Bytes'] and m.sha(b)==r['Sha256'],(p,r['Path'])
 wb=m.blobs(H,[w])[w];assert (key+': '+m.sha(pb[p])).encode() in wb;native.append(dict(Path=p,Files=n,Sha256=m.sha(pb[p])))
result=dict(Status='PASS_ACTUAL_CORRECTED_PUBLISHED352_CI7297_ELEVEN_INTEGRATIONS',Head=H,Run=run['id'],Job=job['id'],Artifact=items[0]['id'],Passed=7297,Failed=0,Integrations=11,All351Ordered7297NamesExact=True,Registry=587,CustomLimitsRawExact351=True,CompatibilityCounts={'Verified compatible':16,'Known differences':24,'Needs verification':547},ImmutableFull351Counts={'Verified compatible':10,'Known differences':24,'Needs verification':553},RuntimeSourceBindings=77,AllLedgerEvidenceFilesRehashed=len(evidence),ExactlySixPromotions=changed,AllOther581AndPriorTenVerifiedExact=True,InitialPublished131BlobsRehashed=True,InitialBlobsPreserved129=True,FinalCommittedBlobs=136,CorrectionPaths=7,PublishedRuntimeCodeAndBuildInputsRawExact351=len(buildpaths),NativePlanRows= native,ActualCheckoutHeadBoundInJobLog=True,ActualTestReportSha256=m.sha(tp[0].read_bytes()),DownloadedFiles=len(downloaded),InitialCi38991076980ConclusionNotAudited=True,Original352Dispatched=False,Original351PassClaimed=False,NoNewPrimitiveRuntimeOrFullOrPerformanceClaim=True,ReaderSha256=m.sha(Path(__file__).read_bytes()),NativeOrDownloadedCodeExecuted=False,SdkExecuted=False,MainModified=False,NetworkUsed=False)
out=F/'ci-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',newline='\n');print(m.sha(out.read_bytes()))
