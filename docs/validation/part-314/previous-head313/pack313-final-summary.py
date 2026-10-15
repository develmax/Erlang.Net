import hashlib,json
from pathlib import Path
r=Path(__file__).resolve().parent.parent;a=r/'artifacts';read=lambda p:json.loads(p.read_bytes());sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
attempt={'Status':'ACTUAL_PASSIVE_AUDIT_TOOL_RESULTS_RECORDED','Strict':{'ToolChunk':'738102','ExitCode':1,'Script':'artifacts/pack313-ordinary-contract.py','Mode':'strict','Failure':'Strict raw variance retained; only separate semantic gate can apply existing published allowance','FailureLine':22,'DifferencesPath':'artifacts/pack313-ordinary-strict-historical-differences.json'},'Semantic':{'ToolChunk':'df984f','ExitCode':0,'Mode':'semantic','GuardPath':'artifacts/pack313-ordinary-semantic-independent-guard.json'},'ToolResultsRecordedFromReturnedOutput':True,'RawCapturesNormalized':False,'NativeOrVmExecuted':False}
p=a/'pack313-ordinary-passive-audit-attempts.json'
if p.exists():assert read(p)==attempt
else:p.write_text(json.dumps(attempt,indent=2)+'\n')
roles=[]
for kind,guard in [('ci','pack313-ci-independent-guard.json'),('native','pack313-native-combined-independent-guard.json'),('ordinary','pack313-ordinary-semantic-independent-guard.json'),('reallocation','pack313-reallocation-independent-failure-guard.json')]:
    mp=read(a/f'pack313-{kind}-run-map.json');g=read(a/guard);envelope=read(a/f'pack313-{kind}-terminal-envelope-guard.json')
    assert g['Head']==mp['Head']==envelope['Head']=='a44f82a35ff95775941147ff9848a95a5247d76e' and g['Run']==mp['Run']==envelope['Run'] and g['Job']==mp['Job'] and g['Attempt']==1
    roles.append({'Kind':kind,'Run':mp['Run'],'Job':mp['Job'],'Attempt':1,'Artifact':g['Artifact'],'Conclusion':envelope['Conclusion'],'Guard':{'Path':'artifacts/'+guard,'Sha256':sha(a/guard)},'EnvelopeSha256':sha(a/f'pack313-{kind}-terminal-envelope-guard.json'),'ZipSha256':envelope['Artifacts'][0]['Sha256']})
out=a/'pack313-terminal-independent-summary.json';assert not out.exists()
report={'Status':'EXACT_HEAD313_FOUR_RUNS_INDEPENDENT_TERMINAL_AUDIT_COMPLETE_WITH_REALLOCATION_PREREQUISITE_FAILURE','Head':'a44f82a35ff95775941147ff9848a95a5247d76e','Roles':roles,'CiPassed':6640,'IntegrationResults':11,'Registry':505,'OldLinuxStrictSdkLines':8,'OldLinuxDomainRoles':3,'SeparateOriginal13Passed':13,'OrdinaryPassed':5861,'FdPassed':8,'OrdinaryExistingMapPrintAlternativeRows':1,'OrdinaryRawStrictFailurePreserved':True,'OrdinarySourceOrPolicyChanged':False,'ReallocationFirstFixtureCompilePassed':True,'ReallocationExtraImport':'__stack_chk_fail@GLIBC_2.4','ReallocationOriginalCasesExecuted':0,'ReallocationSdkCasesExecuted':0,'ReallocationComparisonPassed':False,'InstalledRuntimeFilesPassivelyVerified':2646,'RemoteRetryOrPostPerformedByVerifier':False,'LocalNativeOrVmExecuted':False,'MainOrGitMutated':False,'GeneralNifAbiComplete':False}
out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'Path':str(out),'Sha256':sha(out),'Roles':roles}))
