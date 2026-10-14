from pathlib import Path
import json,hashlib,subprocess,re,zipfile
A=Path('artifacts'); HEAD='c2302e11af57b1fa75f4f9ab51455e3187b34c24'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
j=lambda p:json.loads(p.read_bytes().decode('utf-8-sig'))
def blob(p):return subprocess.check_output(['git','cat-file','blob',HEAD+':'+p])
m=j(A/'part-275-validation-metadata.json'); p=j(A/'part-275-ci-latest-provenance.json')
r=m['Run']; jobs=m['Jobs']; jobs=jobs['jobs'] if isinstance(jobs,dict) else jobs
assert len(jobs)==1
job=jobs[0]; ar=m['Artifact']; ar=ar['artifacts'][0] if 'artifacts' in ar else ar
for x in (r,p['Run']):assert x['id']==38646626055 and x['head_sha']==HEAD and x['status']=='completed' and x['conclusion']=='success' and x['run_attempt']==1
for x in (job,p['Job']):assert x['id']==116134953349 and x['head_sha']==HEAD and x['run_id']==r['id'] and x['status']=='completed' and x['conclusion']=='success' and len(x['steps'])==8 and all(t['status']=='completed' and t['conclusion']=='success' for t in x['steps'])
zbytes=(A/'part-275-validation.zip').read_bytes(); assert len(zbytes)==ar['size_in_bytes']==61939 and sha(zbytes)==m['ZipSHA256'].upper()==ar['digest'].split(':')[1].upper() and ar['id']==11932812525 and ar['workflow_run']['head_sha']==HEAD and not ar['expired']
with zipfile.ZipFile(A/'part-275-validation.zip') as z:
 assert set(z.namelist())=={'tests.json','integration-results.json','preprocessing-disabled.log'}
 for n in z.namelist():assert z.read(n)==(A/'part-275-validation'/n).read_bytes()
t=j(A/'part-275-validation/tests.json'); old=json.loads(blob('docs/validation/part-273/tests.json').decode('utf-8-sig'))
assert old['Passed']==6527 and old['Failed']==0 and t['Passed']==6544 and t['Failed']==0
names=[x['Name'] for x in t['Tests']]; olds=[x['Name'] for x in old['Tests']]; assert len(names)==len(set(names))==6544 and all(x['Status']=='Passed' for x in t['Tests'])
added=set(names)-set(olds);assert len(added)==17 and [n for n in names if n not in added]==olds
hosts=set()
for n in ['RuntimeSetNode','RuntimeNodeTextActivation','ProcessBumpCap','RuntimeNodeCreation','DistributionRegistered','ConnectedDistribution','ConnectedIngress']:
 s=blob('tests/Erlang.Tests/'+n+'HostNames.cs').decode();hosts.add(re.search(r'const string Test\s*=\s*"([^"]+)"',s).group(1))
compiled={'oracle/compiled-module/process-info-logical-'+s for s in ['small-bump','bump-after-yield','bump-error-domain']}
mfas={'mfa/erlang:'+s for s in ['setnode/2','setnode/3','dist_ctrl_get_opt/2','dist_ctrl_set_opt/3','dist_ctrl_get_data_notification/1','dist_ctrl_get_data/1','dist_ctrl_input_handler/2']}
assert added==hosts|compiled|mfas
log=(A/'part-275-ci-job.log').read_bytes();txt=log.decode('utf-8-sig');assert HEAD in txt and '6544/6544 passed' in txt and 'Validation passed' in txt
passes=re.findall(r'^\S+ PASS ([^\r\n]+)$',txt,re.M);assert passes==names
assert '"Checks":132,"Failed":0' in txt and '"Checks":24,"Failed":0' in txt
for text in ["Successfully created package",'Erlang.Net.CSharp.0.1.0.nupkg','Erlang.Net.Tool.0.1.0.nupkg',"Tool 'erlang.net.tool' (version '0.1.0') was successfully installed."] :assert text in txt
integ=j(A/'part-275-validation/integration-results.json'); expected={'Build':'Passed','DisabledPreprocessing':'Expected failure','OracleUnavailableReport':'Expected failure recorded','PackageConsumer':'Passed','Tests':'Passed','CleanRebuild':'Passed','LocalTool':'Passed','HybridOutput':'Hello World','Incremental':'Passed','Readiness':'Passed','PackagedReadiness':'Passed'};assert integ==expected
negative=(A/'part-275-validation/preprocessing-disabled.log').read_text(encoding='utf-8-sig');assert 'Build FAILED.' in negative and 'error ' in negative
paths=subprocess.check_output(['git','ls-tree','-r','--name-only',HEAD],text=True).splitlines();paths=[x for x in paths if x.startswith(('src/','tests/','tools/')) and x.endswith(('.cs','.csproj','.ps1','.props','.targets')) or x in ['.github/workflows/validate.yml','global.json','Directory.Build.props','Directory.Build.targets','Erlang.Net.sln']]
sources=[{'Path':x,'Sha256':sha(blob(x)),'Size':len(blob(x))} for x in paths]
workflow=blob('.github/workflows/validate.yml').decode();validate=blob('tools/validate.ps1').decode();assert './tools/validate.ps1' in workflow and 'param([switch]$SkipPackage)' in validate and "Write-Output 'Validation passed'" in validate
assert "if ($LASTEXITCODE -ne 2)" in validate and "if (-not $SkipPackage)" in validate
files=['part-275-validation.zip','part-275-validation-metadata.json','part-275-ci-latest-provenance.json','part-275-ci-job.log','part-275-ci-job.log.digest.json','part-275-validation/tests.json','part-275-validation/integration-results.json','part-275-validation/preprocessing-disabled.log']
assert j(A/'part-275-ci-job.log.digest.json')['SHA256']==sha(log)
out={'Status':'PASS','Head':HEAD,'Run':r['id'],'Job':job['id'],'Artifact':ar['id'],'Attempt':1,'CompletedSuccessfulSteps':8,'TestsPassed':6544,'TestsFailed':0,'Old6527NamesOrderExact':True,'Added17':sorted(added),'RawJobPassNamesExact6544':True,'PermanentConnectedAssertions':132,'PermanentIngressAssertions':24,'Integration':integ,'CommittedSourceBindings':sources,'Files':[{'Path':'artifacts/'+f,'Size':(A/f).stat().st_size,'Sha256':sha((A/f).read_bytes())} for f in files],'BoundedClaims':{'CIClrValidation':True,'OriginalOTPDifferential':False,'NativeCreationBoundaries':False,'PhysicalBEAMSchedulers':False,'HistoricalFailedLocalReportsRewritten':False,'MainWritten':False,'RuntimeRerun':False},'PipelineProof':'Successful raw validate.ps1 step and terminal Validation passed bind guarded HelloHybrid incremental/clean rebuild, negative preprocessing/oracle, package consumer/local tool/readiness assertions; no package binary archive or build-PDB provenance is present in this three-file CI artifact.'}
(A/'part275-ci-independent-guard.json').write_bytes((json.dumps(out,indent=2)+'\n').encode());print('PASS CI6544 old6527+17 sourcebindings'+str(len(sources))+' guardSHA '+sha((A/'part275-ci-independent-guard.json').read_bytes()))
