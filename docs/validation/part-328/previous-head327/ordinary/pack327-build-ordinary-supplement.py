"""Explicit new327 selected ordinary evidence only; no SDK/VM/native execution."""
from pathlib import Path
import hashlib,json
R=Path.cwd();A=R/'artifacts';HEAD='4bc4c00b713c1cc0bbf32a1c74add45174bbd10a'
j=lambda p:json.loads(p.read_bytes());h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
bind=lambda p:{'Path':p.relative_to(R).as_posix(),'Bytes':p.stat().st_size,'Sha256':h(p)}
def write(p,value):
 assert not p.exists();p.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
semantic=A/'pack327-ordinary-semantic-independent-guard.json';strict=A/'pack327-ordinary-strict-independent-guard.json'
s=j(semantic);q=j(strict);assert s['Head']==q['Head']==HEAD and s['Passed']==5925 and s['FdPassed']==8
assert q['Status']=='REFUSED_STRICT_RAW_EQUALITY_EXISTING_PREDECLARED_MAP_STDOUT_FLIP' and not q['StrictRawEqualityPassed'] and q['PredeclaredMapStdoutVarianceRows']==1
allocator=A/'pack327-allocator-profile-independent-guard2.json';memory=A/'pack327-memory-profile-independent-guard2.json'
for p in [allocator,memory]:assert j(p)['Head']==HEAD and j(p)['Status'].startswith('PASS')
summary=A/'pack327-ordinary-independent-summary.json'
write(summary,{'Status':'PASS_EXACT_PUBLISHED327_ORIGINAL5925_FD8_WITH_SEPARATE_STRICT_MAP_STDOUT_REFUSAL','Head':HEAD,'Run':38856979991,'Job':116830552534,'Artifact':12027904580,'ActualOriginalPassed':5925,'ActualFdPassed':8,'AllPrevious5905OrderedContractsVerified':True,'NewTwentyErrorHandlerCasesBoundActualManagedEb16':True,'ManagedSha256':'EB16F5B4D3C4E8B08860055234725B6F8AE56EE6220D46C07B9A5CA3F8DDCAB5','AllOld233DifferentialSourceFilesExactExceptAdditiveCatalog':True,'NewCaseSourceFiles':2,'SixHistoricalExclusionsAndCallerCompilerPoliciesExact':True,'CompiledModuleTrackOnlyCountDelta20':True,'SemanticGuard':bind(semantic),'StrictGuard':bind(strict),'StrictRawEqualityPassed':False,'RefusalReason':'Actual predeclared map stdout flip at ModuleResults3605; exact captures retained without normalization','AllocatorBackendProfileGuard':bind(allocator),'MemoryPayloadProfileGuard':bind(memory),'BackendOrPhysicalMemoryEqualityNotClaimed':True,'ReviewerExecutedSdkVmNativeNetworkMainGitMutation':False})
four=A/'pack327-four-terminal-independent-summary.json';v=j(four);assert v['Head']==HEAD and v['Status'].startswith('PASS')
for row in v['Bindings']:
 p=R/row['Path'];assert p.stat().st_size==row['Bytes'] and h(p)==row['Sha256']
allfive=A/'pack327-all-five-terminal-independent-summary.json'
write(allfive,{'Status':'PASS_PUBLISHED327_ALL_FIVE_TYPED_TERMINAL_SCOPES_WITH_ORDINARY_RAW_STDOUT_REFUSAL_SEPARATE','Head':HEAD,'CiTests':6729,'Integrations':11,'OriginalOrdinary5925AndFd8':True,'NewTwentyOriginalProcessErrorHandlerCasesConfirmed':True,'NativeSdkEightLinesDomainThreeAndSeparateOriginal13':True,'DistributionGl4Send7Link7Exit8ControlWait2Retention2':True,'ReallocationOriginal3Sdk3ConfiguredFullPrefix2646':True,'StrictOrdinaryMapStdoutEqualityPassed':False,'StrictOrdinaryMapStdoutRefusal':bind(strict),'StrictExitReprEqualityPassedInThisFreshReplay':v['StrictExitReprEqualityThisReplay'],'NoFreshExitRefusalInvented':True,'RawNormalization':False,'PreviousFourTypedScopes':bind(four),'OrdinaryTypedScope':bind(summary),'AlreadyPreparedOtherFourLeanRowsNotDuplicated':True,'ReviewerExecutedSdkVmNativeNetworkMainGitMutation':False})
terminal=A/'pack327-watch-38856979991-terminal';ex=terminal/'artifact-12027904580'
names=['pack327-audit-common.py','pack327-ordinary-contract.py','pack327-allocator-profile-guard.py','pack327-memory-profile-guard.py','pack327-ordinary-semantic-independent-guard.json','pack327-ordinary-strict-independent-guard.json','pack327-ordinary-semantic-historical-differences.json','pack327-ordinary-strict-historical-differences.json','pack327-allocator-profile-independent-guard2.json','pack327-memory-profile-independent-guard2.json','pack327-ordinary-run-map.json',summary.name,allfive.name,'pack327-build-ordinary-supplement.py']
paths=[A/n for n in names]+[terminal/n for n in ['run.json','jobs.json','artifacts.json','raw-capture.json','download-manifest.json','job-116830552534.log']]+[ex/n for n in ['differential.json','fd-differential.json','allocator-profile.json','differential.progress.json','allocator-profile.log']]
rows=[]
for p in paths:
 assert p.is_file() and p.resolve().is_relative_to(A) and p.suffix.lower() not in {'.dll','.so','.exe','.zip','.tar','.gz'}
 rows.append({'SourcePath':p.relative_to(R).as_posix(),'TargetPath':'docs/validation/part-328/previous-head327/ordinary/'+p.relative_to(A).as_posix(),'Bytes':p.stat().st_size,'Sha256':h(p)})
assert len(rows)==len({x['TargetPath'] for x in rows}) and all(x['Bytes']<12*1024*1024 for x in rows)
manifest=A/'pack327-ordinary-terminal-lean-copy-rows.json'
write(manifest,{'Status':'EXPLICIT_NEW327_ORDINARY_ONLY_SELECTED_RAW_FOR_FUTURE328','Head':HEAD,'Rows':rows,'Count':len(rows),'Bytes':sum(x['Bytes'] for x in rows),'NoArchivesRuntimeTreesOrOtherFourRecopies':True})
print(json.dumps({'Manifest':bind(manifest),'AllFiveSummary':bind(allfive),'OrdinarySummary':bind(summary),'Rows':len(rows),'Bytes':sum(x['Bytes'] for x in rows)}))
