import sys
sys.dont_write_bytecode=True
from pathlib import Path,PurePosixPath
import hashlib,importlib.util,json,posixpath,tarfile
s=importlib.util.spec_from_file_location('audit',Path(__file__).with_name('pack313-audit-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mapping,d,run,job,items,rows=m.load('reallocation');assert run['conclusion']==job['conclusion']=='failure' and len(items)==1
ex=d/f'artifact-{items[0]["id"]}';control=ex/'control';build=ex/'source-build';sdk=ex/'sdk-native';runtime=ex/'runtime-receipt'
planpath='tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json';rawplan=m.blobs(mapping['Head'],[planpath])[planpath];plan=json.loads(rawplan)
assert m.sha(rawplan)=='EE514F42467BF1A3FD4764D07F696CA4F2BFDA2126157F3A0C06E07DD065209A' and len(plan['Files'])==1074
published=m.blobs(mapping['Head'],[e['Path'] for e in plan['Files']])
for e in plan['Files']:
    b=published[e['Path']];b=b.replace(b'\r\n',b'\n') if e['HashMode']=='TextLF' else b
    assert len(b)==e['Bytes'] and m.sha(b)==e['Sha256']
producer=m.read(build/'build-process-receipt.json');review=m.read(control/'source-review.json');approval=m.read(control/'runtime-approval.json');receipt=m.read(runtime/'source-build-receipt.json')
assert producer['Complete'] and not producer['ApprovalIssued'] and not producer['ObservationVmExplicitlyInvoked']
assert producer['Head']==approval['Head']==mapping['Head'] and producer['PlanSha256']==approval['PlanSha256']==m.sha(rawplan)
assert producer['WorkflowRun']['GITHUB_RUN_ID']==str(mapping['Run']) and producer['WorkflowRun']['GITHUB_RUN_ATTEMPT']=='1'
assert producer['SourceBefore']==producer['SourceAfter'] and producer['PinnedOtpCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5'
assert approval['Passed'] and approval['ApprovedReviewSha256']==m.sha((control/'source-review.json').read_bytes()) and approval['ApprovedSourceBuildReceiptSha256']==m.sha((runtime/'source-build-receipt.json').read_bytes())
assert producer['InstalledClosure']==receipt['InstalledClosure'] and producer['ProgramIdentitiesBefore']==producer['ProgramIdentities'] and len(producer['ProgramIdentities'])==2
assert producer['Roles']==['configure','build','install']
log=b''
for role in producer['Roles']:
    process=m.read(build/(role+'.process.json'));attempt=m.read(build/(role+'.attempt.json'))
    assert process['ExitCode']==0 and not process['TimedOut'] and not process['LogBudgetExceeded'] and process['ParentReaped'] and process['OutputsClosed']
    assert process['Arguments']==attempt['Arguments'] and process['AttemptSha256']==m.sha((build/(role+'.attempt.json')).read_bytes()) and not attempt['AutomaticRetryAllowed']
    for suffix,key in [('stdout','Stdout'),('stderr','Stderr')]:
        b=(build/(role+'.'+suffix)).read_bytes();assert m.sha(b)==process[key]['Sha256'] and len(b)==process[key]['Bytes'];log+=('=== '+role+'.'+suffix+' ===\n').encode()+b+b'\n'
assert log==(build/'complete-build.log').read_bytes()
canonical=lambda value:json.dumps(value,sort_keys=True,separators=(',',':')).encode()
assert review['ReviewNote']==canonical({'Schema':'VerifiedFullPinnedInstalledBuild-v1','InstalledClosureSha256':m.sha(canonical(producer['InstalledClosure'])),'BuildProcessReceiptSha256':m.sha((build/'build-process-receipt.json').read_bytes())}).decode()
closure=producer['InstalledClosure'];prefix=PurePosixPath(closure['InstalledPrefix']);runtimefiles={str(PurePosixPath(e['Path']).relative_to(prefix)):e for e in closure['RuntimeFiles']}
archive=control/'installed-runtime.tar.gz';assert (control/'installed-runtime.tar.gz.sha256').read_text().split()[0].upper()==m.sha(archive.read_bytes())
with tarfile.open(archive,'r:gz') as tar:
    members={}
    for member in tar.getmembers():
        n=member.name.removeprefix('./').rstrip('/')
        if n in ('','.'):continue
        assert n not in members and not PurePosixPath(n).is_absolute() and '..' not in PurePosixPath(n).parts
        members[n]=member
    assert set(members)=={e['Path'] for e in closure['Entries']}
    for e in closure['Entries']:
        member=members[e['Path']]
        if e['Kind']=='Directory':assert member.isdir() and member.mode==e['Mode']
        elif e['Kind']=='FileSymlink':assert member.issym() and member.linkname==e['LinkText'] and posixpath.normpath(posixpath.join(posixpath.dirname(e['Path']),member.linkname))==e['Target']
        else:assert e['Kind']=='File' and (member.isfile() or member.islnk()) and member.mode==e['Mode']
    for name,e in runtimefiles.items():
        b=tar.extractfile(members[name]).read();assert len(b)==e['Bytes'] and m.sha(b)==e['Sha256']
assert len(runtimefiles)==closure['RegularFileCount']==approval['RuntimeFiles']==2646
assert sum(e['Bytes'] for e in runtimefiles.values())==closure['TotalRegularFileBytes']
native=sdk/'reallocation_fixture.so';process=m.read(sdk/'reallocation_fixture-so-compile.process.json')
assert process['ExitCode']==0 and process['ParentReaped'] and process['PipesClosed'] and not process['TimedOut'] and not (sdk/'reallocation_fixture-so-compile.stderr').read_bytes()
imports={line.split()[-1].split('@')[0] for line in (sdk/'reallocation_fixture-so-undefined.stdout').read_text().splitlines() if line.split()}
extra=imports-set(plan['NativeImports']['reallocation_fixture.so']);assert extra=={'__stack_chk_fail'}
assert not (control/'native-approval.json').exists() and not (sdk/'build-receipt.json').exists() and not (ex/'original3').exists() and not (sdk/'sdk.stdout').exists() and not (sdk/'managed').exists()
assert not (sdk/'reallocation_fixture-so-readelf.process.json').exists()
log=(d/f'job-{job["id"]}.log').read_text(encoding='utf-8-sig')
assert "Unexpected native imports: reallocation_fixture.so" in log and "run-reviewed.py\", line 61" in log
report={'Status':'VERIFIED_ACTUAL_FIRST_REALLOCATION_LINUX_IMPORT_PREREQUISITE_FAILURE','Head':mapping['Head'],'Run':mapping['Run'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'RawZipSha256':m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),'SourcePlanInputsVerified':1074,'SourcePlanSha256':m.sha(rawplan),'SourceConfigureBuildInstallActualPassed':True,'IndependentSourceAndRuntimeApprovalsBound':True,'CompleteInstalledPrefixArchiveVerifiedFiles':len(runtimefiles),'InstalledNamespaceEntries':len(closure['Entries']),'InstalledRuntimeArchiveSha256':m.sha(archive.read_bytes()),'FirstFixtureCompilePassed':True,'FailureSource':'tools/Erlang.NifReallocationObservations/run-reviewed.py:61','Failure':'Unexpected native imports: reallocation_fixture.so','ExtraNormalizedImports':sorted(extra),'ActualExtraRawSymbol':'__stack_chk_fail@GLIBC_2.4','ActualFixtureSha256':m.sha(native.read_bytes()),'ReadelfOrNativeApprovalReached':False,'ManagedSdkBuildReached':False,'OriginalCasesExecuted':0,'SdkCasesExecuted':0,'OriginalAndSdkCompared':False,'SemanticRedClaimed':False,'NoRetryPerformed':True,'NativeExecutedByVerifier':False,'VmExecutedByVerifier':False,'MainEdited':False,'GitMutation':False,'PostExecuted':False}
out=m.A/'pack313-reallocation-independent-failure-guard.json';assert not out.exists();out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
