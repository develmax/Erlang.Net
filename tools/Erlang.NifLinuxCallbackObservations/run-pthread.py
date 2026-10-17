import argparse,hashlib,json,os,pathlib,shutil,sys,time,platform,resource
ROOT=pathlib.Path.cwd().resolve()
sys.path.insert(0,str(ROOT/'tools/Erlang.GuestConnectedSendObservations'))
from owned_process import OwnedProcess
def sha(p):return hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest().upper()
def check(p,s):assert sha(p)==s.upper(),str(p)
a=argparse.ArgumentParser();a.add_argument('--source-plan',required=True);a.add_argument('--source-plan-sha256',required=True);a.add_argument('--approved-admission-sha256',required=True);args=a.parse_args()
base=ROOT/'artifacts/linux-callback17';new_bridge=ROOT/'artifacts/linux-pthread-bridge';capture=ROOT/'artifacts/linux-callback-pthread-control';capture.mkdir(exist_ok=False)
(capture/'bridge').symlink_to(new_bridge,target_is_directory=True)
for family in ('size-original','reference-original'):(capture/family).symlink_to(base/family,target_is_directory=True)
out=capture/'sdk';out.mkdir(exist_ok=False)
check(new_bridge/'admission.json',args.approved_admission_sha256)
admission=json.loads((new_bridge/'admission.json').read_bytes())
assert admission['SubjectHead']=='f7399755673e510c15ddcdc573a376141c1c5965'
check(args.source_plan,args.source_plan_sha256);plan=json.loads(pathlib.Path(args.source_plan).read_bytes())
assert args.source_plan_sha256.upper()=='83667FC235724A7E618C6DA8AAE36FF6BCCA0C4616E1C1AE83E1BA1F25A43995'
changed={'tools/Erlang.NifNativeBackend/NifLinuxCallbackDriver.cs','tools/Erlang.NifLinuxCallbackFocus/Program.cs','.github/workflows/nif-entry-original.yml','src/Erlang.Runtime/NifNativeLinuxNamespace.cs','tools/Erlang.NifNativeBackend/NifLinuxCallbackAdmission.cs'}
for row in plan['Files']:
    if row['Path'].replace('\\','/') not in changed:check(ROOT/row['Path'],row['Sha256'])
check(admission['BridgePath'],admission['BridgeSha256']);check(admission['BridgeReceiptPath'],admission['BridgeReceiptSha256'])
for image in admission['Images']:
    for key in ('Library','NativeSource','ErlangSource','Manifest','BuildReceipt'):check(image[key+'Path'],image[key+'Sha256'])
focus=ROOT/'tools/Erlang.NifLinuxCallbackFocus/bin/Debug/net10.0/Erlang.NifLinuxCallbackFocus.dll';dotnet=pathlib.Path(shutil.which('dotnet')).resolve()
old=json.loads((base/'sdk/inputs.json').read_bytes());dlls=[]
for row in old['AdjacentDlls']:
    path=pathlib.Path(row['Path']);dlls.append({'Path':str(path),'Sha256':sha(path),'PreviousSha256':row['Sha256'],'ByteIdentical':sha(path)==row['Sha256']})
# Record new bridge and runtime provider binding separately from the retained original fixture producers.
gdb=shutil.which('gdb');environment=os.environ.copy()
for key in ('GH_ARTIFACT_TOKEN','GITHUB_TOKEN','LD_PRELOAD','LD_AUDIT'):environment.pop(key,None)
# Preserve the actual target major/minor CLR identity rather than lexical version order.
coreclr=[]
for binary in sorted((dotnet.parent/'shared/Microsoft.NETCore.App').glob('10.0.*/libcoreclr.so'),key=lambda p:tuple(int(x) for x in p.parent.name.split('.')),reverse=True)[:2]:
    assert binary.stat().st_size<32*1024*1024
    target=out/('coreclr-'+binary.parent.name+'.so');shutil.copyfile(binary,target)
    coreclr.append({'InstalledPath':str(binary),'CapturedPath':str(target),'Sha256':sha(target),'Bytes':target.stat().st_size})
assert coreclr
(out/'coreclr-identities.json').write_text(json.dumps(coreclr,indent=2))
stages=[];failures=[]
inputs={'DiagnosticInstrumentation':True,'PrimaryObservationCount':17,'AdmissionSubjectHead':admission['SubjectHead'],'ExecutingCodeHead':os.environ['GITHUB_SHA'],'PreviousRun':39030222266,'PreviousArtifact':12099548874,'SourcePlanSha256':args.source_plan_sha256,'Dotnet':str(dotnet),'DotnetSha256':sha(dotnet),'PreviousDotnetSha256':old['DotnetSha256'],'DotnetByteIdentical':sha(dotnet)==old['DotnetSha256'],'AdjacentDlls':dlls,'FocusSha256':sha(focus),'DebuggerPath':gdb,'DebuggerSha256':sha(gdb) if gdb else None,'Platform':platform.platform(),'Libc':platform.libc_ver(),'SignalCaptureBudget':4,'BacktraceFramesPerThread':16,'WallDeadlineSecondsPerRoute':180,'MaximumStreamBytes':4194304,'MaximumDumpFileBytes':67108864}
(out/'inputs.json').write_text(json.dumps(inputs,indent=2))
resource.setrlimit(resource.RLIMIT_FSIZE,(64*1024*1024,64*1024*1024))
for route in ('reviewed','ordinary'):
    command=[str(dotnet),str(focus),str(new_bridge/'admission.json'),sha(new_bridge/'admission.json'),str(out/route),route]
    mode='gdb' if gdb else 'dotnet-triage-dump'
    if gdb:
        script=out/('capture-'+route+'.gdb')
        script.write_text("""set pagination off
set confirm off
set debuginfod enabled off
set auto-load safe-path /nonexistent
set print thread-events off
set follow-fork-mode parent
set detach-on-fork on
set disable-randomization off
handle SIGPIPE SIGXCPU SIGPWR SIGUSR1 SIGUSR2 SIG34 SIG35 nostop noprint pass
set $signals = 0
catch signal SIGSEGV SIGABRT
commands
silent
set $signals = $signals + 1
printf "DIAGNOSTIC_SIGNAL_CAPTURE %d\\n", $signals
info threads
thread apply all bt 16
info registers
info proc mappings
info symbol $pc
p $_siginfo
x/8i $pc
info sharedlibrary
if $signals >= 4
printf "DIAGNOSTIC_SIGNAL_BUDGET_EXHAUSTED\\n"
quit 86
end
continue
end
run
""")
        for key in ('DOTNET_DbgEnableMiniDump','COMPlus_DbgEnableMiniDump'):environment.pop(key,None)
        command=[gdb,'--nx','--batch','--return-child-result','-x',str(script),'--args']+command
    else:
        environment.update(DOTNET_DbgEnableMiniDump='1',DOTNET_DbgMiniDumpType='3',DOTNET_DbgMiniDumpName=str(out/(route+'.crash.%p.dmp')),DOTNET_EnableCrashReport='1')
    (out/(route+'-execution.json')).write_text(json.dumps({'Mode':mode,'Arguments':command,'ExecutingCodeHead':os.environ['GITHUB_SHA'],'AdmissionSubjectHead':admission['SubjectHead']},indent=2))
    process=None;failure=None
    try:
        process=OwnedProcess(command,ROOT,environment,out/route)
        process.wait(time.monotonic()+180,lambda:False)
    except BaseException as error:
        failure=repr(error)
    finally:
        if process:process.close()
        check(admission['BridgePath'],admission['BridgeSha256'])
        for image in admission['Images']:check(image['LibraryPath'],image['LibrarySha256'])
        target_completion=out/route/'completion.json';completed=False
        try:
            if target_completion.exists():
                completion=json.loads(target_completion.read_bytes())
                completed=completion.get('Status')=='ACQUIRED_LINUX_CALLBACK17_TWO_RUNTIME_ROWS' and completion.get('PrimaryObservationCount')==17 and completion.get('Route')==route and completion.get('RuntimeDomains')==2 and completion.get('AllImageRetirementsCompleted') is True and completion.get('RuntimeDisposalsCompleted') is True
                if completed:
                    for row in completion['Files']:check(out/route/row['Path'],row['Sha256'])
        except Exception as error:
            completed=False;failure=failure or 'Invalid route output: '+repr(error)
        if failure is None and not completed:failure='Wrapper exited zero without verified full17 target completion'
        (out/(route+'-diagnostic-receipt.json')).write_text(json.dumps({'DiagnosticInstrumentation':True,'PrimaryObservationCount':17,'Failure':failure,'Mode':mode,'TargetCompletionVerified':completed,'TargetCompletionSha256':sha(target_completion) if target_completion.exists() else None,'WrapperExitCode':None if process is None else process.process.returncode,'TargetSignalNotInferredFromWrapper':True,'DumpFiles':[{'Path':str(p),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in out.glob(route+'.crash.*.dmp')],'SourcePlanExceptions':['Driver','Program','Admission','DispatchAlias','NamespaceHostThreadRegistration'],'NativeImagesPreserved':True},indent=2))
    receipt=out/(route+'.process.json');value=json.loads(receipt.read_bytes())
    # An unsettled child invalidates isolation for the next route; do not continue.
    assert value['ParentReaped'] and value['StreamsClosed'] and value['CleanupFailure'] is None
    if value['ExitCode']!=0 or value['TerminationRequested'] or value['RemainingGroupKillRequested']:
        failure=failure or 'Owned route did not complete normally'
    if failure:failures.append({'Route':route,'Failure':failure})
    stages.append({'Name':route,'Passed':failure is None,'ReceiptSha256':sha(receipt),'StdoutSha256':sha(out/(route+'.stdout')),'StderrSha256':sha(out/(route+'.stderr'))})
for row in dlls:check(row['Path'],row['Sha256'])
(out/'completion.json').write_text(json.dumps({'Status':'FAILED_TWO_LINUX_ROUTES_CALLBACK17' if failures else 'ACQUIRED_TWO_LINUX_ROUTES_CALLBACK17','SubjectHead':admission['SubjectHead'],'ExecutingCodeHead':os.environ['GITHUB_SHA'],'AdmissionSha256':sha(new_bridge/'admission.json'),'InputsSha256':sha(out/'inputs.json'),'OwnedStages':stages,'Routes':['reviewed','ordinary'],'Failures':failures,'PrimaryRowsPerRoute':17,'RuntimeDomainsPerRoute':2,'OriginalComparisonExecuted':False},indent=2))
if failures:raise RuntimeError('Independent route failures: '+repr(failures))
