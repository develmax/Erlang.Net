import sys
sys.dont_write_bytecode=True
import importlib.util,json
from pathlib import Path
s=importlib.util.spec_from_file_location('audit',Path(__file__).with_name('pack313-audit-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mapping,d,run,job,items,rows=m.load('ci')
assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
head=mapping['Head'];ex=d/f'artifact-{items[0]["id"]}'
tp=list(ex.rglob('tests.json'));ip=list(ex.rglob('integration-results.json'));assert len(tp)==len(ip)==1
names_to_read=['docs/validation/part-313/final-main/tests.json','docs/validation/part-312/tests.json','docs/validation/part-313/final-main/integration-results.json','docs/validation/part-313/final-main/root-part313-main1/source-freeze.json']
base=m.blobs(head,names_to_read)
t=m.read(tp[0]);expected=json.loads(base[names_to_read[0]]);prior=json.loads(base[names_to_read[1]])
names=[x['Name'] for x in t['Tests']]
assert t['Passed']==6640 and t['Failed']==0 and len(names)==len(set(names))==6640 and all(x['Status']=='Passed' for x in t['Tests'])
assert names==[x['Name'] for x in expected['Tests']]==[x['Name'] for x in prior['Tests']]
assert len([x for x in names if x.startswith('mfa/')])==505
assert m.read(ip[0])==json.loads(base[names_to_read[2]]) and len(m.read(ip[0]))==11
stage=m.read(m.A/'root-part313-stage-index-guard.json');f=json.loads(base[names_to_read[3]])
assert stage['Status']=='PASS_EXACT_PART313_STAGE_DEFAULT_WHITESPACE_AND_DECLARED_MODES' and stage['SourcePaths']==45 and stage['CSharpPaths']==19 and stage['StagedPaths']==485
assert stage['ProofsBinaryExact'] and stage['AllCSharpBinaryExact'] and stage['DefaultGitWhitespaceCheckPassed'] and not stage['BlankAtEofExceptionUsed']
assert stage['SourceFreezeSha256']==m.sha(base[names_to_read[3]])=='94ECE1FA74682375688E4FF65B174EC4BC993CB572D50372145FDF5FEB34507B'
indexed={x['Path']:x for x in stage['Records']};actual=m.blobs(head,[x['Path'] for x in f['Files']])
for row in f['Files']:
    record=indexed[row['Path']];data=actual[row['Path']]
    assert m.sha(data)==record['StagedSha256'] and len(data)==record['StagedBytes'] and record['WorkingSha256']==row['Sha256'] and record['WorkingBytes']==row['Bytes']
    if row['Path'].endswith('.cs'):assert record['ComparisonMode']=='BinaryExact'
result={'Status':'PASS_EXACT_HEAD313_CI6640_FULL','Head':head,'Run':mapping['Run'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'Passed':6640,'Failed':0,'Previous312OrderedNamesExact':True,'Integration':m.read(ip[0]),'RegisteredMfas':505,'PublishedStageSource45AndCSharp19Exact':True,'PublishedStageGuardSha256':m.sha((m.A/'root-part313-stage-index-guard.json').read_bytes()),'RawZipSha256':m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),'NativeExecutedByVerifier':False,'MainEdited':False,'PostExecuted':False}
out=m.A/'pack313-ci-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
