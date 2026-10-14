from pathlib import Path
import json,hashlib,zipfile
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
m=j(a/'part-247-halt-native-raw-metadata.json');p=j(a/'part-247-halt-latest-provenance.json');root=a/'part-247-halt-native-raw';report=j(root/'halt-native.json');declared=j(root/'halt-native.json.predeclared.json')
assert m['Run']['id']==p['Run']['id']==38516451751 and m['Run']['head_sha']==p['Run']['head_sha']=='90bade7bd3e4d1b63f8c350b71f11418f973e5ef'
assert m['Run']['conclusion']==p['Run']['conclusion']=='failure' and p['Job']['id']==115696233718
assert p['Job']['labels']==['ubuntu-latest']
assert sha(a/'part-247-halt-native-raw.zip')==m['ZipSHA256']==m['Artifact']['digest'].removeprefix('sha256:').upper()
with zipfile.ZipFile(a/'part-247-halt-native-raw.zip') as z:
 for name in z.namelist():
  if not name.endswith('/'):assert z.read(name)==(root/name).read_bytes(),name
assert report['VersionVerified'] and report['Complete'] and report['Executed']==report['Planned']==24
assert report['ComparablePassed']==15 and report['ComparableFailed']==7 and report['PhysicalEndpointPassed']==2 and report['PhysicalEndpointFailed']==0
assert not report['PhysicalFormatComparisonComplete'] and report['InfrastructureError'] is None
sdk=j(r/'docs/validation/part-247/halt-sdk-results.json');contracts={x['Name']:x['Sources'] for x in sdk};failures=[]
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
assert len(failures)==7
digest=j(a/'part-247-halt-job.log.digest.json');assert sha(a/'part-247-halt-job.log')==digest['SHA256']
log=(a/'part-247-halt-job.log').read_text(encoding='utf-8-sig');assert 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log
result={'Status':'FAILED_ORIGINAL_PROOF_VERIFIED','Head':m['Run']['head_sha'],'Run':m['Run']['id'],'Job':p['Job']['id'],'Artifact':m['Artifact']['id'],'ZipSha256':m['ZipSHA256'],'RawJobLogSha256':digest['SHA256'],'Complete':True,'Executed':24,'ComparablePassed':15,'ComparableFailed':7,'PhysicalEndpointPassed':2,'PhysicalFormatComparisonComplete':False,'AllDeclaredAndSdkSourceContractsExact':True,'AllRawChildSourceStreamExitFilesExact':True,'Failures':failures,'NormalizationOrExclusionsAdded':False}
(a/'part247-halt-failed-guard.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('Verified FAILED genuine24:15/22 comparable,7 failures,2 physical; source/raw/provenance exact')
