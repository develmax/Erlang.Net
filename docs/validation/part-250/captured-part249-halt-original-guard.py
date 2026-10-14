from pathlib import Path
import json,hashlib,zipfile
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
m=j(a/'part-249-halt-native-raw-metadata.json');p=j(a/'part-249-halt-latest-provenance.json');root=a/'part-249-halt-native-raw';report=j(root/'halt-native.json');declared=j(root/'halt-native.json.predeclared.json')
assert m['Run']['id']==p['Run']['id']==38520672647 and m['Run']['head_sha']==p['Run']['head_sha']=='8a72ad88f907a34d424ecc23648bc45c315213a4'
assert m['Run']['conclusion']==p['Run']['conclusion']=='success' and p['Job']['id']==115709388505
assert p['Job']['labels']==['ubuntu-latest']
assert sha(a/'part-249-halt-native-raw.zip')==m['ZipSHA256']==m['Artifact']['digest'].removeprefix('sha256:').upper()
with zipfile.ZipFile(a/'part-249-halt-native-raw.zip') as z:
 for name in z.namelist():
  if not name.endswith('/'):assert z.read(name)==(root/name).read_bytes(),name
assert report['VersionVerified'] and report['Complete'] and report['Executed']==report['Planned']==28
assert report['ComparablePassed']==26 and report['ComparableFailed']==0 and report['PhysicalEndpointPassed']==2 and report['PhysicalEndpointFailed']==0
assert not report['PhysicalFormatComparisonComplete'] and report['InfrastructureError'] is None
sdk=j(r/'docs/validation/part-249/halt-sdk56.json');contracts={x['Name']:x['Sources'] for x in sdk};failures=[]
assert [x['Contract'] for x in report['Results']]==declared
for row in report['Results']:
 c=row['Contract'];actual=row['Actual'];assert c['Sources']==contracts[c['Name']]
 directory=root/'halt-native-cases'/Path(actual['WorkingDirectory']).name
 for source in c['Sources']:
  assert hashlib.sha256(source['Source'].encode()).hexdigest().upper()==source['SourceSha256']
  assert sha(directory/(source['Module']+'.erl'))==source['SourceSha256']
 stdout=(directory/'process-stdout.raw').read_bytes();stderr=(directory/'process-stderr.raw').read_bytes();raw=j(directory/'process-raw-evidence.json')
 assert list(stdout)==actual['StandardOutput'] and list(stderr)==actual['StandardError']
 assert raw['RawExitCode']==actual['RawExitCode'] and raw['StandardOutputComplete'] and raw['StandardErrorComplete'] and raw['InfrastructureError'] is None
 if row['Errors']:failures.append({'Name':c['Name'],'Errors':row['Errors'],'Stage':actual['Stage'],'RawExitCode':actual['RawExitCode'],'StdoutSha256':hashlib.sha256(stdout).hexdigest().upper(),'StderrSha256':hashlib.sha256(stderr).hexdigest().upper(),'SourceHashes':[x['SourceSha256'] for x in c['Sources']]})
 if row['Dump']:
  dump=row['Dump'];dp=directory/Path(dump['Path']).name
  assert dump['Exists'] and dump['SignatureMatches'] and dump['Error'] is None and sha(dp)==dump['Sha256'] and dp.stat().st_size==dump['Length']
  assert list(dp.read_bytes()[:len(dump['Prefix'])])==dump['Prefix']
assert len(failures)==0
digest=j(a/'part-249-halt-job.log.digest.json');assert sha(a/'part-249-halt-job.log')==digest['SHA256']
log=(a/'part-249-halt-job.log').read_text(encoding='utf-8-sig');assert 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log
result={'Status':'ORIGINAL_PROOF_VERIFIED','Head':m['Run']['head_sha'],'Run':m['Run']['id'],'Job':p['Job']['id'],'Artifact':m['Artifact']['id'],'ZipSha256':m['ZipSHA256'],'RawJobLogSha256':digest['SHA256'],'Complete':True,'Executed':28,'ComparablePassed':26,'ComparableFailed':0,'PhysicalEndpointPassed':2,'PhysicalFormatComparisonComplete':False,'AllDeclaredAndSdkSourceContractsExact':True,'AllRawChildSourceStreamExitFilesExact':True,'Failures':failures,'NormalizationOrExclusionsAdded':False}
(a/'part249-halt-original-guard.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('Verified genuine28:26/26 comparable,0 failures,2 physical; source/raw/provenance exact')
