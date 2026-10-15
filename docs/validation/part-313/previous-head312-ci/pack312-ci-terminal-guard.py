import sys
sys.dont_write_bytecode=True
import importlib.util,json
from pathlib import Path
s=importlib.util.spec_from_file_location('audit',Path(__file__).with_name('pack312-audit-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mapping,d,run,job,items,rows=m.load('ci');assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps']);head=mapping['Head'];ex=d/f'artifact-{items[0]["id"]}'
tp=list(ex.rglob('tests.json'));ip=list(ex.rglob('integration-results.json'));assert len(tp)==len(ip)==1
base=m.blobs(head,['docs/validation/part-312/tests.json','docs/validation/part-311/tests.json','docs/validation/part-312/integration-results.json','docs/validation/part-312/root-artifacts/source-freeze.json'])
t=m.read(tp[0]);old=json.loads(base['docs/validation/part-311/tests.json']);expected=json.loads(base['docs/validation/part-312/tests.json']);names=[x['Name'] for x in t['Tests']];prior=[x['Name'] for x in old['Tests']]
assert t['Passed']==6640 and t['Failed']==0 and len(names)==len(set(names))==6640 and all(x['Status']=='Passed' for x in t['Tests']) and names==[x['Name'] for x in expected['Tests']]
assert old['Passed']==len(prior)==6639 and [x for x in names if x in set(prior)]==prior and len([x for x in names if x.startswith('mfa/')])==505
assert m.read(ip[0])==json.loads(base['docs/validation/part-312/integration-results.json']) and len(m.read(ip[0]))==11
stage=m.read(m.A/'root-part312-stage-index-guard.json');f=json.loads(base['docs/validation/part-312/root-artifacts/source-freeze.json']);assert stage['Status']=='PASS_EXACT_STAGE_PATHS_EXPLICIT_GIT_ATTRIBUTE_HASH_MODES' and stage['SourcePaths']==38 and stage['CSharpPaths']==35 and stage['ProofsBinaryExact'] and stage['WorkingSourceFreezePreserved']
assert stage['DefaultGitWhitespaceCheckPassed'] and not stage['BlankAtEofExceptionUsed'] and stage['BulkGitByteAndAttributeSubprocesses']==2
assert stage['SourceFreezeSha256']==m.sha(base['docs/validation/part-312/root-artifacts/source-freeze.json'])=='BF76223CC62315095AD10FDCB699B4A873834E8CBACBF935DF4A74E6FAF81452'
indexed={x['Path']:x for x in stage['Records']};actual=m.blobs(head,[x['Path'] for x in f['Files']])
for row in f['Files']:
 record=indexed[row['Path']];data=actual[row['Path']];assert m.sha(data)==record['StagedSha256'] and len(data)==record['StagedBytes'] and record['WorkingSha256']==row['Sha256'] and record['WorkingBytes']==row['Bytes']
 if row['Path'].endswith(('.cs','.beam','.etf')):assert record['ComparisonMode']=='BinaryExact'
result={'Status':'PASS_EXACT_HEAD312_CI6640_FULL','Head':head,'Run':mapping['Run'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'Passed':6640,'Failed':0,'Historical6639OrderedNamesExact':True,'OrderedNamesExact':True,'Integration':m.read(ip[0]),'RegisteredMfas':505,'PublishedStageSource38AndCSharp35Exact':True,'PublishedStageGuardSha256':m.sha((m.A/'root-part312-stage-index-guard.json').read_bytes()),'RawZipSha256':m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),'NativeExecutedByVerifier':False,'MainEdited':False,'PostExecuted':False}
out=m.A/'pack312-ci-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
