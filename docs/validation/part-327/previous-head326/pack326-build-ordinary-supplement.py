"""Explicit new326 ordinary supplement for327; no source/binary tree copying or native execution."""
from pathlib import Path
import hashlib,json
R=Path.cwd();A=R/'artifacts';H='b51f050c287f3b91346e1d3e30e86061708fb035'
read=lambda p:json.loads(p.read_bytes());sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
bind=lambda p:{'Path':p.relative_to(R).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)}
def write(p,v):
 assert not p.exists();p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
sg=A/'pack326-ordinary-semantic-independent-guard.json';qg=A/'pack326-ordinary-strict-independent-guard.json';s=read(sg);q=read(qg);assert s['Head']==H and s['Passed']==5905 and s['FdPassed']==8 and q['Status'].startswith('REFUSED_') and not q['StrictRawEqualityPassed']
terminal=A/'pack326-watch-38850739246-terminal';ex=terminal/'artifact-12025227970';old=read(A/'pack325-watch-38848192670-terminal/artifact-12023784821/differential.json');now=read(ex/'differential.json')
x,y=old['CompiledModuleTrack'],now['CompiledModuleTrack'];assert x['ReferenceMode']==y['ReferenceMode'] and x['ImplementationMode']==y['ImplementationMode'] and x['Failed']==y['Failed']==0
for k in ['Planned','Executed','Passed']:assert x[k]==4948 and y[k]==4962
allocator=A/'pack326-allocator-profile-independent-guard2.json';memory=A/'pack326-memory-profile-independent-guard2.json'
for p in [allocator,memory]:assert read(p)['Head']==H and read(p)['Status'].startswith('PASS')
summary=A/'pack326-ordinary-independent-summary.json'
write(summary,{'Status':'PASS_EXACT_PUBLISHED326_ORIGINAL5905_FD8_WITH_SEPARATE_STRICT_MAP_STDOUT_REFUSAL','Head':H,'Run':38850739246,'Job':116811160938,'Artifact':12025227970,'ActualOriginalPassed':5905,'ActualFdPassed':8,'AllPrevious5891OrderedContractsVerified':True,'FourteenNewOriginalCasesExactlyBoundActualManaged92DD':True,'ManagedSha256':'92DD944CADF2FD909D773A0255F9CE4267DC6A45BED0E52A7086664C5350D52F','AllOld231PublishedSourceFilesUnchangedExceptAdditiveCaseCatalog':True,'NewCaseSourceFiles':2,'SixHistoricalExclusionsAndCallerCompilerPoliciesExact':True,'CompiledModuleTrackOnlyCountDelta14':True,'SemanticGuard':bind(sg),'StrictGuard':bind(qg),'StrictRawEqualityPassed':False,'RefusalReason':'Existing predeclared map stdout flip at ModuleResults3605; raw captures retained, no normalization','AllocatorBackendProfileGuard':bind(allocator),'MemoryPayloadProfileGuard':bind(memory),'BackendPayloadAndPhysicalMemoryEqualityNotClaimed':True,'ReviewerExecutedSdkVmNativeNetworkMainOrGitMutation':False})
previous=A/'pack326-four-terminal-independent-summary.json';v=read(previous);assert v['Head']==H and v['Status'].startswith('PASS') and len(v['Bindings'])==3
for b in v['Bindings']:
 p=R/b['Path'];assert p.stat().st_size==b['Bytes'] and sha(p)==b['Sha256']
allfive=A/'pack326-all-five-terminal-independent-summary.json'
write(allfive,{'Status':'PASS_PUBLISHED326_ALL_FIVE_TYPED_TERMINAL_SCOPES_WITH_STRICT_RAW_REFUSALS_RETAINED','Head':H,'CiTests':6708,'Integrations':11,'OriginalOrdinary5905AndFd8':True,'New326OriginalFourteenSpawnCasesConfirmed':True,'NativeEightSdkStdoutLinesDomainThreeSeparateOriginalThirteen':True,'DistributionGroupLeader4Send7Link7Exit8ControlWait2Retention2':True,'ReallocationOriginalThreeSdkThreeConfiguredInstalledRuntime2646FilePrefix':True,'AllStrictRawComparisonsPassed':False,'StrictOrdinaryMapStdoutRefusal':bind(qg),'StrictExitDisplayReprRefusalRetainedFromFourScopeSummary':True,'RawNormalization':False,'PreviousFourTypedScopes':bind(previous),'OrdinaryTypedScope':bind(summary),'AlreadyPreparedOtherFourLeanRowsNotDuplicated':True,'ReviewerExecutedSdkVmNativeNetworkMainOrGitMutation':False})
names=['pack326-audit-common.py','pack326-ordinary-contract.py','pack326-allocator-profile-guard.py','pack326-memory-profile-guard.py','pack326-ordinary-semantic-independent-guard.json','pack326-ordinary-strict-independent-guard.json','pack326-ordinary-semantic-historical-differences.json','pack326-ordinary-strict-historical-differences.json','pack326-allocator-profile-independent-guard2.json','pack326-memory-profile-independent-guard2.json','pack326-ordinary-run-map.json',summary.name,allfive.name,'pack326-build-ordinary-supplement.py']
paths=[A/n for n in names]+[terminal/n for n in ['run.json','jobs.json','artifacts.json','raw-capture.json','download-manifest.json','job-116811160938.log']]+[ex/n for n in ['differential.json','fd-differential.json','allocator-profile.json','differential.progress.json','allocator-profile.log']]
rows=[]
for p in paths:
 assert p.is_file() and p.suffix.lower() not in {'.dll','.so','.zip','.tar','.gz'}
 rows.append({'SourcePath':p.relative_to(R).as_posix(),'TargetPath':'docs/validation/part-327/previous-head326/'+p.relative_to(A).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)})
assert len({x['TargetPath'] for x in rows})==len(rows) and all(x['Bytes']<12*1024*1024 for x in rows)
out=A/'pack326-ordinary-terminal-lean-copy-rows.json';write(out,{'Status':'EXPLICIT_NEW326_ORDINARY_ONLY_TERMINAL_SUPPLEMENT_FOR_FUTURE327','Rows':rows,'Count':len(rows),'Bytes':sum(x['Bytes'] for x in rows),'NoArchivesRuntimeTreesOrOtherFourScopeRecopies':True})
print(json.dumps({'Manifest':bind(out),'AllFiveSummary':bind(allfive),'OrdinarySummary':bind(summary),'Rows':len(rows),'Bytes':sum(x['Bytes'] for x in rows)}))
