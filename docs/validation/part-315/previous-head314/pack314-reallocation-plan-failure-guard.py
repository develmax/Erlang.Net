import sys
sys.dont_write_bytecode=True
from pathlib import Path
import importlib.util,json,re
s=importlib.util.spec_from_file_location('audit',Path(__file__).with_name('pack314-audit-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mapping,d,run,job,items,rows=m.load('reallocation');assert run['conclusion']==job['conclusion']=='failure' and len(items)==1
pn='tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json';wf='.github/workflows/nif-reallocation-native.yml';b=m.blobs(mapping['Head'],[pn,wf]);plan=json.loads(b[pn]);assert len(plan['Files'])==1082
published=m.blobs(mapping['Head'],[e['Path'] for e in plan['Files']])
for e in plan['Files']:
    raw=published[e['Path']];raw=raw.replace(b'\r\n',b'\n') if e['HashMode']=='TextLF' else raw
    assert len(raw)==e['Bytes'] and m.sha(raw)==e['Sha256'],e['Path']
expected=re.search(rb'REVIEWED_REALLOCATION_PLAN_SHA256: ([A-F0-9]{64})',b[wf]).group(1).decode();assert expected=='9D10C312F86E12AE4829399A0D5245F25A6BC3EF045BCF17CE83521199ACC0CB'
actual=m.sha(b[pn]);assert actual=='774B86DC439FA1CC5EFCB9CF3AC74D6C67B1C55ADFFB8633D634CF7158741795' and b'\r\n' not in b[pn]
authored=(m.A/'allocator-query-mailbox314-import-policy2/candidate'/pn).read_bytes();assert m.sha(authored)==expected and authored.replace(b'\r\n',b'\n')==b[pn]
stage=m.read(m.A/'root-part314-stage-index-guard.json');record=next(e for e in stage['Records'] if e['Path']==pn)
assert record['ComparisonMode']=='TextLF' and record['WorkingSha256']==expected and record['StagedSha256']==actual and record['WorkingBytes']==len(authored) and record['StagedBytes']==len(b[pn])
ex=d/f'artifact-{items[0]["id"]}';assert {p.relative_to(ex).as_posix() for p in ex.rglob('*') if p.is_file()}=={'control/clone.stdout','control/clone.stderr'}
log=(d/f'job-{job["id"]}.log').read_text(encoding='utf-8-sig');assert "RuntimeError: Reviewed source plan changed" in log and 'reviewed_common.py", line 40' in log
contracts=m.A/'pack314-reallocation-source-contracts';assert not contracts.exists();contracts.mkdir()
for n,raw in [('published-SOURCE-PLAN.json',b[pn]),('published-nif-reallocation-native.yml',b[wf])]: (contracts/n).write_bytes(raw)
report={'Status':'VERIFIED_ACTUAL_FIRST314_REALLOCATION_CANONICAL_PLAN_SEAL_FAILURE_BEFORE_SOURCE_BUILD','Head':mapping['Head'],'Run':mapping['Run'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'RawZipSha256':m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),'Failure':'Reviewed source plan changed','FailureSource':'tools/Erlang.NifReallocationObservations/reviewed_common.py:40','WorkflowExpectedRawPlanSha256':expected,'PublishedRawPlanSha256':actual,'AuthoredWindowsPlanBytes':len(authored),'PublishedLfPlanBytes':len(b[pn]),'AuthoredCrLfCount':authored.count(b'\r\n'),'CanonicalLfBytesExactlyPublished':True,'StageTextLfNormalizationExplicitlyRecorded':True,'PlanAll1082PublishedInputRowsIndependentlyVerified':True,'SourceCloneControlCaptured':True,'ConfigureBuildInstallStarted':False,'NativeCompilationStarted':False,'OriginalCasesExecuted':0,'SdkCasesExecuted':0,'OriginalAndSdkCompared':False,'ImportPolicySemanticFailure':False,'SemanticRedClaimed':False,'SourcePlanVerificationBypassed':False,'RetryOrPostPerformed':False,'NativeOrVmExecutedByVerifier':False,'MainOrGitMutated':False}
out=m.A/'pack314-reallocation-independent-failure-guard.json';assert not out.exists();out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
