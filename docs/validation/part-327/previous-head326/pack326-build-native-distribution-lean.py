"""New326 native/distribution selected-raw proof supplement for future327; no earlier-head trees."""
from pathlib import Path
import hashlib,importlib.util,json,sys
sys.dont_write_bytecode=True
R=Path.cwd();A=R/'artifacts';h=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes())
spec=importlib.util.spec_from_file_location('passive326',A/'pack326-audit-common.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
HEAD='b51f050c287f3b91346e1d3e30e86061708fb035';PARENT='2bd148150ddf2cca49dd2c797d05de3260bf663c'
bind=lambda p:{'Path':p.relative_to(R).as_posix(),'Bytes':p.stat().st_size,'Sha256':h(p.read_bytes())}
paths={}
def add(p):
    p=Path(p);p=p if p.is_absolute() else R/p
    assert p.is_file() and p.resolve().is_relative_to(A) and p.suffix.lower() not in {'.dll','.so','.zip','.tar','.gz','.exe'}
    paths[p.relative_to(R).as_posix()]=p
policy=[]
for name,count,digest in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json',1109,'A673C88561EC5E31E1F69BE5795519A3AA658E013B6000910B03E192A520E6A5'),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json',1134,'53500EFC145705B70364DE7BC19E60FBCE55C62C7C4C00CB66712B1B8F01F9E4')]:
    b=m.blobs(HEAD,[name])[name];old=m.blobs(PARENT,[name])[name];v=json.loads(b);before=json.loads(old)
    assert h(b)==digest and len(v['Files'])==count and {k:x for k,x in v.items() if k!='Files'}=={k:x for k,x in before.items() if k!='Files'}
    assert [x['Path'] for x in v['Files']]==[x['Path'] for x in before['Files']]
    changed=[x['Path'] for x,y in zip(v['Files'],before['Files']) if x!=y]
    assert {'src/Erlang.Runtime/ProcessRuntime.cs','src/Erlang.Runtime/ProcessSpawnOptions.cs'}.issubset(changed)
    policy.append({'Path':name,'Inputs':count,'Sha256':digest,'AllNonFilesPoliciesExact325':True,'InputPathSetExact325':True,'ChangedRecords':changed})
native=j(A/'pack326-native-combined-independent-guard.json');retention=j(A/'pack326-retention-independent-guard.json')
guards=[]
for name in ['native-combined','group-leader','guest-send','guest-links','guest-exit','control-wait','retention']:
    p=A/f'pack326-{name}-independent-guard.json';v=j(p);assert v['Head']==HEAD and v['Status'].startswith('PASS');guards.append({**bind(p),'Status':v['Status']});add(p)
exitguard=j(A/'pack326-guest-exit-independent-guard.json');refusal=A/'pack326-guest-exit-strict-repr-refusal.json'
assert exitguard['StrictRemotePythonReprEqualityPassed'] is False and h(refusal.read_bytes())==exitguard['StrictReprRefusalSha256']
summary={'Status':'PASS_INDEPENDENT_PUBLISHED326_NATIVE_AND_DISTRIBUTION_TYPED_SCOPES_WITH_FRESH_EXIT_DISPLAY_REFUSAL','Head':HEAD,'Native':{'Run':38850744372,'Job':116811175864,'Artifact':12026035055,'RawRows':205,'StrictSdkStdoutLines':8,'DomainRoles':3,'SeparateOriginal13':True,'Original13ComparedWithSdk':False,'GeneratedCSharpSha256':native['GeneratedCSharpSha256'],'PublishedWindowsGuardSha256':native['WindowsPublishedGuardSha256']},'Distribution':{'Run':38850744058,'Job':116811173868,'Artifact':12025501020,'RawRows':600,'GroupLeaderContracts':4,'SendContracts':7,'LinkContracts':7,'ExitContracts':8,'ControlWaitFreshThreeVmCases':2,'RetainedLinkFreshThreeVmCases':2,'ActualRetentionCases':retention['Cases'],'ActualRetentionCompiledImages':retention['CompiledImages'],'NativeKillAndIndependentControllerDownBeforeCont':True,'LinkedReceiverExitKilledAfterCont':True,'AllOwnedCleanupSettled':True},'Plans':policy,'Guards':guards,'StrictExitRemotePythonReprEqualityPassed':False,'StrictExitDisplayOnlyRefusal':bind(refusal),'TwoActualDifferingDisplayFields':exitguard['DisplayOnlyFieldsDifferentInThisReplay'],'NoRawNormalization':True,'NoAssumedOldRefusalOrExpectedDisplayDifferenceSet':True,'InheritedThreeGuardStatusLabelsRetain323LiteralButActualHeadRunAndRawBindingsAre326':True,'WholeErlangOrNativeAbiOrWirePhaseParityClaimed':False,'Current326OriginalFourteenSpawnCasesObservedByTheseWorkflows':False,'ReallocationAndOrdinary326NotInferred':True,'ReviewerExecutedNativeVmSdkNetworkOrMainGitMutation':False}
sp=A/'pack326-native-distribution-independent-summary.json';assert not sp.exists();sp.write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8');add(sp)
for name in ['audit-common.py','native-terminal-guard.py','original13-detached-capture.py','native-join-guard.py','group-leader-guard.py','guest-send-guard.py','guest-links-guard.py','guest-exit-guard4.py','control-wait-guard.py','retention-guard.py','build-native-distribution-lean.py']:
    add(A/('pack326-'+name))
for name in ['pack326-native-38850744372-independent-envelope-and-sdk-guard.json','pack326-native-run-map.json','pack326-distribution-run-map.json','pack326-original13-independent-local-report.json','pack326-original13-independent-local-capture.json','pack326-guest-exit-strict-repr-refusal.json']:add(A/name)
for kind,rid,aid,jid in [('native',38850744372,12026035055,116811175864),('distribution',38850744058,12025501020,116811173868)]:
    base=A/f'pack326-watch-{rid}-terminal';ex=base/f'artifact-{aid}'
    for name in ['run.json','jobs.json','artifacts.json','raw-capture.json','download-manifest.json',f'job-{jid}.log']:add(base/name)
    if kind=='native':
        for name in ['source-binding.json','build-receipt.json','sdk-receipt.json','sdk-six-argument.process.json','sdk-six-argument.stdout','sdk-six-argument.stderr']:add(ex/'nif-bounded-sdk'/name)
        for name in ['nif-pending-original-typed-review.json','nif-pending-otp-build-provenance.json']:add(ex/name)
        o=ex/'nif-pending-original-observations'
        for name in ['receipt.json','source-plan.json','metadata.etf','disassembly.etf','fixture.c','nif_pending_varargs_fixture.erl','nif_pending_varargs_observer.erl']:add(o/name)
        cases=j(o/'receipt.json')['OrderedFreshVmCases'];assert len(cases)==13
        for case in cases:
            for name in ['result.etf','observation.process.json','observation.stdout','observation.stderr']:add(o/case/name)
        for name in ['verify-original13.py','verify-metadata-mutations.py','reviewed-source-plan.json']:add(A/'pack326-original13-source-contracts'/name)
    else:
        # Same bounded inherited raw-role selection as previous324 proof, but every row is this new326 API artifact.
        previous=j(A/'pack324-three-terminal-lean-copy-rows.json')['Rows']
        for row in previous:
            old=row['SourcePath']
            if 'pack324-watch-38845675043-terminal' in old:
                add(old.replace('pack324-watch-38845675043','pack326-watch-38850744058').replace('12023123026','12025501020').replace('116795531533','116811173868'))
        o=ex/'connected-link-ownership325-original';report=j(o/'report.json')
        add(o/'report.json')
        for row in report['Files']:add(o/row['Path'])
        add(ex/'connected-link-ownership325-collector.log')
rows=[{'SourcePath':name,'TargetPath':'docs/validation/part-327/previous-head326/'+name.removeprefix('artifacts/'),'Bytes':p.stat().st_size,'Sha256':h(p.read_bytes())} for name,p in sorted(paths.items())]
assert len({x['TargetPath'] for x in rows})==len(rows)
out=A/'pack326-native-distribution-terminal-lean-copy-rows.json';assert not out.exists()
out.write_text(json.dumps({'Status':'EXPLICIT_NEW326_NATIVE_DISTRIBUTION_SELECTED_RAW_FOR_FUTURE327','Head':HEAD,'SummarySha256':h(sp.read_bytes()),'Rows':rows,'Count':len(rows),'Bytes':sum(x['Bytes'] for x in rows),'NoPreviousHeadTreesOrApiArchivesOrRuntimeBinariesCopied':True,'WholeRawNativeDistributionArtifactPortabilityNotClaimed':True},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Summary':bind(sp),'Manifest':bind(out),'Rows':len(rows),'Bytes':sum(x['Bytes'] for x in rows)}))
