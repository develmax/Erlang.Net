import importlib.util,json
from pathlib import Path
s=importlib.util.spec_from_file_location('audit',Path(__file__).with_name('pack310-audit-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mapping,d,run,job,items,rows=m.load('ci');assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps']);head=mapping['Head'];ex=d/f'artifact-{items[0]["id"]}'
tp=list(ex.rglob('tests.json'));ip=list(ex.rglob('integration-results.json'));assert len(tp)==len(ip)==1
base=m.blobs(head,['docs/validation/part-310/tests.json','docs/validation/part-309/tests.json','docs/validation/part-310/integration-results.json','docs/validation/part-310/root-artifacts/root-part310-final-source-freeze.json'])
t=m.read(tp[0]);old=json.loads(base['docs/validation/part-309/tests.json']);expected=json.loads(base['docs/validation/part-310/tests.json']);names=[x['Name'] for x in t['Tests']];prior=[x['Name'] for x in old['Tests']]
assert t['Passed']==6638 and t['Failed']==0 and len(names)==len(set(names))==6638 and all(x['Status']=='Passed' for x in t['Tests']) and names==[x['Name'] for x in expected['Tests']]
assert old['Passed']==len(prior)==6637 and [x for x in names if x in set(prior)]==prior and len([x for x in names if x.startswith('mfa/')])==505
assert m.read(ip[0])==json.loads(base['docs/validation/part-310/integration-results.json']) and len(m.read(ip[0]))==11
stage=m.read(m.A/'root-part310-stage-index-guard.json');f=json.loads(base['docs/validation/part-310/root-artifacts/root-part310-final-source-freeze.json']);assert stage['Status']=='PASS_EXACT_STAGE_PATHS_EXPLICIT_GIT_ATTRIBUTE_HASH_MODES' and stage['SourcePaths']==26 and stage['CSharpPaths']==20 and stage['ProofsBinaryExact'] and stage['WorkingSourceFreezePreserved']
assert stage['SourceFreezeSha256']==m.sha(base['docs/validation/part-310/root-artifacts/root-part310-final-source-freeze.json'])=='E16C4F41565A6D249999F1B386C0ABF10915BEF34BD3D453E423672705828F8F'
indexed={x['Path']:x for x in stage['Records']};actual=m.blobs(head,[x['Path'] for x in f['Files']])
for row in f['Files']:
 record=indexed[row['Path']];data=actual[row['Path']];assert m.sha(data)==record['StagedSha256'] and len(data)==record['StagedBytes'] and record['WorkingSha256']==row['Sha256'] and record['WorkingBytes']==row['Bytes']
 if row['Path'].endswith(('.cs','.beam','.etf')):assert record['ComparisonMode']=='BinaryExact'
result={'Status':'PASS_EXACT_HEAD310_CI6638_FULL','Head':head,'Run':mapping['Run'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'Passed':6638,'Failed':0,'Historical6637OrderedNamesExact':True,'OrderedNamesExact':True,'Integration':m.read(ip[0]),'RegisteredMfas':505,'PublishedStageSource26AndCSharp20Exact':True,'PublishedStageGuardSha256':m.sha((m.A/'root-part310-stage-index-guard.json').read_bytes()),'RawZipSha256':m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),'NativeExecutedByVerifier':False,'MainEdited':False,'PostExecuted':False}
out=m.A/'pack310-ci-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
