import sys
sys.dont_write_bytecode=True
import importlib.util,json
from pathlib import Path
s=importlib.util.spec_from_file_location('audit',Path(__file__).with_name('pack314-audit-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mapping,d,run,job,items,rows=m.load('ci');assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
head=mapping['Head'];ex=d/f'artifact-{items[0]["id"]}';tp=list(ex.rglob('tests.json'));ip=list(ex.rglob('integration-results.json'));assert len(tp)==len(ip)==1
paths=['docs/validation/part-314/final-main/tests.json','docs/validation/part-313/final-main/tests.json','docs/validation/part-314/final-main/integration-results.json','docs/validation/part-314/final-main/root-part314-main1/source-freeze.json'];base=m.blobs(head,paths)
t=m.read(tp[0]);expected=json.loads(base[paths[0]]);prior=json.loads(base[paths[1]]);names=[x['Name'] for x in t['Tests']]
assert t['Passed']==6641 and t['Failed']==0 and len(names)==len(set(names))==6641 and all(x['Status']=='Passed' for x in t['Tests']) and names==[x['Name'] for x in expected['Tests']]
new='runtime/allocator-query-owned-mailbox-transport';assert [n for n in names if n!=new]==[x['Name'] for x in prior['Tests']] and names.count(new)==1 and prior['Passed']==6640
assert len([n for n in names if n.startswith('mfa/')])==505 and m.read(ip[0])==json.loads(base[paths[2]]) and len(m.read(ip[0]))==11
stage=m.read(m.A/'root-part314-stage-index-guard.json');f=json.loads(base[paths[3]])
assert stage['Status']=='PASS_EXACT_PART314_STAGE_DEFAULT_WHITESPACE_AND_DECLARED_MODES' and stage['SourcePaths']==17 and stage['CSharpPaths']==12 and stage['StagedPaths']==len(stage['Records'])
assert stage['ProofsBinaryExact'] and stage['AllCSharpBinaryExact'] and stage['DefaultGitWhitespaceCheckPassed'] and not stage['BlankAtEofExceptionUsed']
assert stage['SourceFreezeSha256']==m.sha(base[paths[3]])=='F4FC11897BCD6DF6E9C139B5BC845FAE7E774D29BC46FB6171A2EE7FB04BDF73'
indexed={x['Path']:x for x in stage['Records']};actual=m.blobs(head,[x['Path'] for x in f['Files']])
for row in f['Files']:
    record=indexed[row['Path']];data=actual[row['Path']];assert m.sha(data)==record['StagedSha256'] and len(data)==record['StagedBytes'] and record['WorkingSha256']==row['Sha256'] and record['WorkingBytes']==row['Bytes']
    if row['Path'].endswith('.cs'):assert record['ComparisonMode']=='BinaryExact'
result={'Status':'PASS_EXACT_HEAD314_CI6641_FULL','Head':head,'Run':mapping['Run'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'Passed':6641,'Failed':0,'Previous313All6640OrderedNamesExactPlusOneTransport':True,'Integration':m.read(ip[0]),'RegisteredMfas':505,'PublishedStageSource17AndCSharp12Exact':True,'PublishedStageGuardSha256':m.sha((m.A/'root-part314-stage-index-guard.json').read_bytes()),'RawZipSha256':m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),'NativeExecutedByVerifier':False,'MainEdited':False,'PostExecuted':False}
out=m.A/'pack314-ci-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
