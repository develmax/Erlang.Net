"""Bounded new325 ordinary-only supplement; previous typed scopes referenced, not recopied."""
from pathlib import Path
import hashlib,json
R=Path.cwd();A=R/'artifacts';H='2bd148150ddf2cca49dd2c797d05de3260bf663c'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
read=lambda p:json.loads(p.read_bytes())
bind=lambda p:{'Path':p.relative_to(R).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)}
write=lambda p,v:p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
semantic=A/'pack325-ordinary-semantic-independent-guard.json';strict=A/'pack325-ordinary-strict-independent-guard.json'
s=read(semantic);q=read(strict);assert s['Passed']==5891 and s['FdPassed']==8 and s['Head']==H and q['StrictRawEqualityPassed'] is False
allocator=A/'pack325-allocator-profile-independent-guard2.json';memory=A/'pack325-memory-profile-independent-guard2.json'
assert read(allocator)['Head']==read(memory)['Head']==H
summary=A/'pack325-ordinary-independent-summary.json';assert not summary.exists()
write(summary,{'Status':'PASS_EXACT_HEAD325_ORIGINAL5891_FD8_WITH_SEPARATE_STRICT_MAP_STDOUT_REFUSAL','Head':H,'Run':38848192670,'Job':116803361433,'Artifact':12023784821,'ActualOriginalPassed':5891,'ActualFdPassed':8,'All5891Previous324OrderedContractsVerified':True,'All231DifferentialSourcesExactPublished324':True,'FiveExistingFlagCasesAndSixProcessInfoCasesRetained':True,'SixCaseActualManagedBindingSha256':'6A2C2C548B9F5444827CA7F517CFB3C690012D7874BA8EF41C137FDD9210A7F8','SemanticGuard':bind(semantic),'StrictGuard':bind(strict),'StrictRawEqualityPassed':False,'StrictRefusalReason':'One existing predeclared erlang-term-display-map native stdout-order flip compared with324; no normalization','AllocatorProfileGuard':bind(allocator),'MemoryProfileGuard':bind(memory),'BackendScopesDistinctAndPhysicalEqualityNotClaimed':True,'Current326OriginalExecuted':False,'ReviewerExecutedNativeVmSdkNetworkOrMainMutation':False})
roles=[('ci','pack325-ci-independent-guard.json'),('native','pack325-native-combined-independent-guard.json'),('reallocation','pack325-reallocation-independent-guard2.json'),('distribution','pack325-retention-terminal-independent-summary.json'),('ordinary',summary.name)]
bindings=[]
for role,name in roles:
    p=A/name;v=read(p);assert v['Head']==H and v['Status'].startswith('PASS');bindings.append({'Role':role,**bind(p),'Status':v['Status']})
allfive=A/'pack325-all-five-terminal-independent-summary.json';assert not allfive.exists()
write(allfive,{'Status':'PASS_PUBLISHED325_ALL_FIVE_TYPED_TERMINAL_SCOPES_WITH_STRICT_ORDINARY_MAP_REFUSAL_RETAINED','Head':H,'AllFiveWorkflowTypedScopesPassed':True,'AllStrictRawComparisonsPassed':False,'Ordinary5891AndFd8':True,'Ci6693AndElevenIntegrations':True,'NativeSdkEightStdoutLinesDomainThreeAndSeparateOriginalThirteen':True,'ReallocationOriginalThreeSdkThreeAndConfigured2646FilePrefix':True,'DistributionIncludesTwoNewActualOriginalRetainedLinkNormalAndAsyncCases':True,'InheritedGroupLeader4Send7Link7Exit8ControlWait2ScopesRetained':True,'OriginalRetentionDoesNotClaimSdkWireOrSchedulerPhaseEquality':True,'StrictOrdinaryRefusal':bind(strict),'RawNormalization':False,'Current326OriginalExecuted':False,'Bindings':bindings,'OtherFourTypedProofsAlreadyPublishedInPart326NotRecopied':True})
names=['pack325-audit-common.py','pack325-ordinary-contract.py','pack325-allocator-profile-guard.py','pack325-memory-profile-guard.py','pack325-ordinary-semantic-independent-guard.json','pack325-ordinary-strict-independent-guard.json','pack325-ordinary-semantic-historical-differences.json','pack325-ordinary-strict-historical-differences.json','pack325-allocator-profile-independent-guard2.json','pack325-memory-profile-independent-guard2.json','pack325-ordinary-run-map.json',summary.name,allfive.name,'pack325-build-ordinary-supplement.py']
terminal=A/'pack325-watch-38848192670-terminal'
paths=[A/name for name in names]+[terminal/name for name in ['run.json','jobs.json','artifacts.json','raw-capture.json','download-manifest.json','job-116803361433.log']]+[terminal/'artifact-12023784821'/name for name in ['differential.json','fd-differential.json','allocator-profile.json','differential.progress.json','allocator-profile.log']]
rows=[]
for p in paths:
    assert p.is_file() and p.suffix.lower() not in {'.dll','.so','.zip','.tar','.gz'}
    rows.append({'SourcePath':p.relative_to(R).as_posix(),'TargetPath':'docs/validation/part-327/previous-head325/'+p.relative_to(A).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)})
assert len({x['TargetPath'] for x in rows})==len(rows)
out=A/'pack325-ordinary-terminal-lean-copy-rows.json';assert not out.exists()
write(out,{'Status':'EXPLICIT_NEW325_ORDINARY_ONLY_TERMINAL_SUPPLEMENT_FOR_FUTURE327','Rows':rows,'Count':len(rows),'Bytes':sum(x['Bytes'] for x in rows),'NoOtherFourOrPreviousHistoryRecopied':True,'SelectedRawNotWholeRuntimePortableClaim':True})
print(json.dumps({'Summary':bind(allfive),'OrdinarySummary':bind(summary),'Manifest':bind(out),'Rows':len(rows),'Bytes':sum(x['Bytes'] for x in rows)}))
