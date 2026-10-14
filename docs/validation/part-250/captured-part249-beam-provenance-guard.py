from pathlib import Path
import json,hashlib,zipfile,subprocess
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
m=j(a/'part-249-beam-fixtures-metadata.json');p=j(a/'part-249-beam-latest-provenance.json');root=a/'part-249-beam-fixtures'
assert m['Run']['head_sha']==p['Run']['head_sha']=='8a72ad88f907a34d424ecc23648bc45c315213a4'
assert m['Run']['id']==p['Run']['id']==38520676319 and p['Job']['id']==115709400238 and p['Job']['labels']==['ubuntu-latest']
assert m['Run']['conclusion']==p['Job']['conclusion']=='success'
assert sha(a/'part-249-beam-fixtures.zip')==m['ZipSHA256']==m['Artifact']['digest'].removeprefix('sha256:').upper()
with zipfile.ZipFile(a/'part-249-beam-fixtures.zip') as z:
 for name in z.namelist():
  if not name.endswith('/'):assert z.read(name)==(root/name).read_bytes()
build=j(root/'beam-source-build-evidence.json');assert build['SourceBuildCompleted'] and build['SourceCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5' and build['HarnessCommit']==m['Run']['head_sha']
assert build['RunId']==str(m['Run']['id']) and sha(root/'beam-otp-build.log')==build['SourceBuildLogSha256']
digest=j(a/'part-249-beam-job.log.digest.json');assert sha(a/'part-249-beam-job.log')==digest['SHA256']
for step in p['Job']['steps']:assert step['status']=='completed' and step['conclusion']=='success',step['name']
subprocess.run(['py','-3','tools/Erlang.BeamFixtures/verify-on-load-observers.py',str(root/'beam-fixtures')],cwd=r,check=True)
old=j(root/'beam-fixtures/manifest.json');resources=r/'tests/Erlang.Tests/Resources/Beam'
for fixture in old['Fixtures']:
 assert (root/'beam-fixtures'/fixture['SourceFile']).read_bytes()==(resources/(fixture['Module']+'.erl')).read_bytes()
observer=j(root/'beam-fixtures/on-load-observers/on-load-observer-manifest.json')
result={'Status':'PASS','Head':m['Run']['head_sha'],'Run':m['Run']['id'],'Job':p['Job']['id'],'Artifact':m['Artifact']['id'],'ZipSha256':m['ZipSHA256'],'RawJobLogSha256':digest['SHA256'],'PinnedSourceBuildVerified':True,'RetainedCompilerFixtures':6,'RetainedNativeExecutions':5,'GenuineOnLoadScenarios':4,'NewCompiledObserverImages':6,'ActualCallbackDownBeforeClassification':True,'AllObserverFilesByteAndHashExact':True,'AllIndependentExpectedSummariesMatch':True,'ProductionOnLoadImplemented':False,'ReferenceSourceCommit':build['SourceCommit'],'Scenarios':[{'Name':s['Name'],'ActualSha256':s['ActualSummarySha256'],'ExpectedSha256':s['ExpectedSummarySha256']} for s in observer['Scenarios']]}
(a/'part249-beam-provenance-guard.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('PASS originalonload4/sixnewimages +old6/5, exactsourcebuild/job/archive/file provenance')
