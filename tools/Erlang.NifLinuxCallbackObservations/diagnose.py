import argparse,hashlib,json,os,pathlib,shutil,sys,time,platform,resource
ROOT=pathlib.Path.cwd().resolve()
sys.path.insert(0,str(ROOT/'tools/Erlang.GuestConnectedSendObservations'))
from owned_process import OwnedProcess
def sha(p):return hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest().upper()
def check(p,s):assert sha(p)==s.upper(),str(p)
a=argparse.ArgumentParser();a.add_argument('--source-plan',required=True);a.add_argument('--source-plan-sha256',required=True);args=a.parse_args()
base=ROOT/'artifacts/linux-callback17';out=ROOT/'artifacts/linux-callback-diagnostic';out.mkdir(exist_ok=False)
check(base/'bridge/admission.json','0711868322C3A608AE6B2FB249D42D05C4C420A5B842D11532577F0C00A00CD3')
admission=json.loads((base/'bridge/admission.json').read_bytes())
assert admission['SubjectHead']=='f7399755673e510c15ddcdc573a376141c1c5965'
check(args.source_plan,args.source_plan_sha256);plan=json.loads(pathlib.Path(args.source_plan).read_bytes())
assert args.source_plan_sha256.upper()=='83667FC235724A7E618C6DA8AAE36FF6BCCA0C4616E1C1AE83E1BA1F25A43995'
changed={'tools/Erlang.NifNativeBackend/NifLinuxCallbackDriver.cs','tools/Erlang.NifLinuxCallbackFocus/Program.cs','.github/workflows/nif-entry-original.yml'}
for row in plan['Files']:
    if row['Path'].replace('\\','/') not in changed:check(ROOT/row['Path'],row['Sha256'])
check(admission['BridgePath'],admission['BridgeSha256']);check(admission['BridgeReceiptPath'],admission['BridgeReceiptSha256'])
for image in admission['Images']:
    for key in ('Library','NativeSource','ErlangSource','Manifest','BuildReceipt'):check(image[key+'Path'],image[key+'Sha256'])
focus=ROOT/'tools/Erlang.NifLinuxCallbackFocus/bin/Debug/net10.0/Erlang.NifLinuxCallbackFocus.dll';dotnet=pathlib.Path(shutil.which('dotnet')).resolve()
old=json.loads((base/'sdk/inputs.json').read_bytes());dlls=[]
for row in old['AdjacentDlls']:
    path=pathlib.Path(row['Path']);dlls.append({'Path':str(path),'Sha256':sha(path),'PreviousSha256':row['Sha256'],'ByteIdentical':sha(path)==row['Sha256']})
# Source-equivalent rebuilds are recorded honestly; only focus/backend instrumentation may change source.
gdb=shutil.which('gdb');environment=os.environ.copy()
for key in ('GH_ARTIFACT_TOKEN','GITHUB_TOKEN','LD_PRELOAD','LD_AUDIT'):environment.pop(key,None)
command=[str(dotnet),str(focus),str(base/'bridge/admission.json'),sha(base/'bridge/admission.json'),str(out/'reviewed'),'reviewed']
mode='gdb' if gdb else 'dotnet-triage-dump'
if gdb:
    script=out/'capture.gdb'
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
    environment.update(DOTNET_DbgEnableMiniDump='1',DOTNET_DbgMiniDumpType='3',DOTNET_DbgMiniDumpName=str(out/'crash.%p.dmp'),DOTNET_EnableCrashReport='1')
resource.setrlimit(resource.RLIMIT_FSIZE,(64*1024*1024,64*1024*1024))
inputs={'DiagnosticOnly':True,'PrimaryObservationCount':0,'AdmissionSubjectHead':admission['SubjectHead'],'ExecutingCodeHead':os.environ['GITHUB_SHA'],'PreviousRun':39030222266,'PreviousArtifact':12099548874,'SourcePlanSha256':args.source_plan_sha256,'Dotnet':str(dotnet),'DotnetSha256':sha(dotnet),'PreviousDotnetSha256':old['DotnetSha256'],'DotnetByteIdentical':sha(dotnet)==old['DotnetSha256'],'AdjacentDlls':dlls,'FocusSha256':sha(focus),'Mode':mode,'DebuggerPath':gdb,'DebuggerSha256':sha(gdb) if gdb else None,'Platform':platform.platform(),'Libc':platform.libc_ver(),'Arguments':command,'SignalCaptureBudget':4,'BacktraceFramesPerThread':16,'WallDeadlineSeconds':180,'MaximumStreamBytes':4194304,'MaximumDumpFileBytes':67108864}
(out/'inputs.json').write_text(json.dumps(inputs,indent=2))
process=None;failure=None
try:
    process=OwnedProcess(command,ROOT,environment,out/'reviewed-process')
    process.wait(time.monotonic()+180,lambda:False)
except BaseException as error:
    failure=repr(error)
finally:
    if process:process.close()
    check(admission['BridgePath'],admission['BridgeSha256'])
    for image in admission['Images']:check(image['LibraryPath'],image['LibrarySha256'])
    target_completion=out/'reviewed/completion.json'
    completed=False
    if target_completion.exists():
        completion=json.loads(target_completion.read_bytes())
        completed=completion.get('Status')=='DIAGNOSTIC_NUMERIC_FOREIGN_GC_RETIREMENT_COMPLETED' and completion.get('DiagnosticOnly') is True and completion.get('PrimaryObservationCount')==0
    if failure is None and not completed:failure='Wrapper exited zero without verified diagnostic target completion'
    (out/'diagnostic-receipt.json').write_text(json.dumps({'DiagnosticOnly':True,'PrimaryObservationCount':0,'Failure':failure,'Mode':mode,'TargetDiagnosticCompletionVerified':completed,'TargetCompletionSha256':sha(target_completion) if target_completion.exists() else None,'WrapperExitCode':None if process is None else process.process.returncode,'TargetSignalNotInferredFromWrapper':True,'DumpFiles':[{'Path':str(p),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in out.glob('*.dmp')],'ProductionSourcePlanPreservedExceptTwoEntrypointsAndDispatchAlias':True,'NativeImagesPreserved':True},indent=2))
if failure:raise RuntimeError(failure)
