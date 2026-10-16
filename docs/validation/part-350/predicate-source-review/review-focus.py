from pathlib import Path
import hashlib,json
R=Path.cwd();O=R/'artifacts/additional-type-predicate-source-independent'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
read=lambda p:json.loads(p.read_bytes())
source=R/'artifacts/additional-type-predicate-proposal/source-freeze.json'
full=R/'artifacts/additional-type-predicate-focus-actual2';g=read(full/'report.json')
assert sha(source.read_bytes())==g['SourceFreezeSha256']=='0F5D888D3BF9ABCBA37F26E8657EE0E277E6B06004B05C00C4B30338260DE227'
for row in read(source)['Files']:assert sha((source.parent/'source'/row['Path']).read_bytes())==row['Sha256']
for name in ['build','focus']:
 e=read(full/(name+'-exit.json'));assert e['ExitCode']==0 and e['Settled'] and not e['TimedOut']
actual=(full/'focus.stdout').read_text(encoding='utf-8').splitlines()
assert actual==['PASS '+name for name in g['Names']] and g['Passed']==6 and len(set(g['Names']))==6
prior=R/'artifacts/additional-type-predicate-focus-actual1';assert read(prior/'build-exit.json')['ExitCode']!=0
assert 'CS0122' in (prior/'build.stdout').read_text(encoding='utf-8')
files=[]
for folder,names in [(full,['report.json','build-exit.json','build-process.json','build.stdout','build.stderr','focus-exit.json','focus-process.json','focus.stdout','focus.stderr']),(prior,['build-exit.json','build-process.json','build.stdout','build.stderr'])]:
 for name in names:
  p=folder/name;files.append({'Path':p.relative_to(R).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p.read_bytes())})
report={'Status':'PASS_PASSIVE_BINDING_ROOT_EXECUTED_SIX_MANAGED_FOCUS_ONLY','SourceAuditSha256':sha((O/'report.json').read_bytes()),'SourceFreezeSha256':sha(source.read_bytes()),'ManagedPassed':6,'ExactPassNames':g['Names'],'Files':files,'History':'Initial separate harness failed CS0122 on internal OracleOutcomeTags. Root linked the exact existing constant source in that harness; frozen three fixture sources stayed unchanged. Actual2 build and six focus cases settled exit0.','SourceAuditExecutionLimitClarification':'The earlier source audit did not itself execute SDK/original code. This supplement separately binds Root-executed managed observations received after that audit.','OriginalExecuted':False,'WholeApiCompatibilityPromoted':False,'FullExecuted':False,'ThisPassiveReviewSdkExecuted':False,'Network':False,'MainChanged':False}
p=O/'managed-focus-binding.json';assert not p.exists();p.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(sha(p.read_bytes()))
