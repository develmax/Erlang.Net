"""Controlled pinned OTP build producer; emits evidence, never approval."""
import argparse,hashlib,json,os,pathlib,shlex,shutil,signal,subprocess,sys,time
import runtime_receipt_adapter as A
from reviewed_common import verified_plan,exact_head,sha,write_fresh,files_receipt
LOG_LIMIT=128*1024**2
TOTAL_LOG_LIMIT=192*1024**2
ROLES=('configure','build','install')

def commands(source):
    prefix=source/'observer-install';make=shutil.which('make')
    if not make:raise ValueError('Existing make required')
    return [('configure',[str(source/'configure'),'--prefix='+str(prefix),'--without-javac','--without-wx','--without-odbc','--without-debugger','--without-observer','--without-et'],600),('build',[make,'-j2'],1800),('install',[make,'install'],600)]

def execute(command,cwd,env,output,role,deadline):
    attempt={'Role':role,'Arguments':command,'Cwd':str(cwd),'DeadlineSeconds':deadline,'StartedUnixSeconds':time.time(),'AutomaticRetryAllowed':False}
    write_fresh(output/(role+'.attempt.json'),attempt);timed=False;budget=False
    with (output/(role+'.stdout')).open('xb') as stdout,(output/(role+'.stderr')).open('xb') as stderr:
        process=subprocess.Popen(command,cwd=cwd,env=env,stdout=stdout,stderr=stderr,start_new_session=True);started=time.monotonic()
        while process.poll() is None:
            sizes=[p.stat().st_size for p in output.iterdir() if p.suffix in ('.stdout','.stderr')]
            budget=any(size>LOG_LIMIT for size in sizes) or sum(sizes)>TOTAL_LOG_LIMIT
            timed=time.monotonic()-started>deadline
            if budget or timed:
                try:os.killpg(process.pid,signal.SIGKILL)
                except ProcessLookupError:pass
                break
            time.sleep(0.1)
        code=process.wait(timeout=15)
    sizes=[p.stat().st_size for p in output.iterdir() if p.suffix in ('.stdout','.stderr')];budget=budget or any(x>LOG_LIMIT for x in sizes) or sum(sizes)>TOTAL_LOG_LIMIT
    result={'Role':role,'Arguments':command,'Cwd':str(cwd),'OwnedPid':process.pid,'OwnedProcessGroup':process.pid,'ExitCode':code,'TimedOut':timed,'LogBudgetExceeded':budget,'ParentReaped':True,'OutputsClosed':True,'DeadlineSeconds':deadline,'ElapsedSeconds':time.monotonic()-started,'AttemptSha256':sha((output/(role+'.attempt.json')).read_bytes()),'Stdout':A.digest(output/(role+'.stdout'),LOG_LIMIT),'Stderr':A.digest(output/(role+'.stderr'),LOG_LIMIT),'CompletedUnixSeconds':time.time()}
    write_fresh(output/(role+'.process.json'),result)
    if code or timed or budget:raise RuntimeError('Pinned source build failed: '+role)

def main():
    p=argparse.ArgumentParser()
    for n in ('repository','otp-source','plan','output'):p.add_argument('--'+n,type=pathlib.Path,required=True)
    for n in ('expected-head','approved-plan-sha256'):p.add_argument('--'+n,required=True)
    args=p.parse_args();A.validate_platform(sys.platform,os.uname().machine);root=args.repository.resolve();source=args.otp_source.resolve();output=args.output.resolve();A.validate_output(source,output);plan=verified_plan(root,args.plan,args.approved_plan_sha256);head=exact_head(root,args.expected_head);before=A.git_state(source);prefix=source/'observer-install'
    A.require(not prefix.exists(),'Fresh configured installation prefix required');output.mkdir(parents=True,exist_ok=False);wf=root/'.github/workflows/nif-reallocation-native.yml';env=os.environ.copy()
    for n in ('ERL_AFLAGS','ERL_FLAGS','ERL_ZFLAGS','MAKEFLAGS','MFLAGS'):env.pop(n,None)
    try:
        recipes=commands(source)
        tools_before=[{'Role':role,'CommandPath':str(command[0]),'CanonicalFile':A.digest(pathlib.Path(command[0]).resolve())} for role,command in [('configure',recipes[0][1]),('make',recipes[1][1])]]
        for role,command,deadline in recipes:execute(command,source,env,output,role,deadline)
        after=A.git_state(source);A.require(before==after,'Tracked pinned source changed')
        runtime=list((prefix/'lib/erlang').glob('erts-*/bin'));A.require(len(runtime)==1,'Exactly one installed runtime required');paths=[prefix/'bin/erl',prefix/'bin/erlc',runtime[0]/'beam.smp',runtime[0]/'erlexec'];legacy={'SourceHead':A.PIN,'ConfiguredPrefix':str(prefix),'NativeNifCasesExecuted':False,'Files':[{'Path':path.relative_to(source).as_posix(),'Bytes':path.stat().st_size,'Sha256':sha(path.read_bytes())} for path in paths]};write_fresh(output/'legacy-provenance.json',legacy)
        with (output/'complete-build.log').open('xb') as stream:
            for role in ROLES:
                for suffix in ('stdout','stderr'):stream.write(('=== '+role+'.'+suffix+' ===\n').encode());stream.write((output/(role+'.'+suffix)).read_bytes());stream.write(b'\n')
        closure=A.inventory(source,prefix)
        tools=[{'Role':role,'CommandPath':str(command[0]),'CanonicalFile':A.digest(pathlib.Path(command[0]).resolve())} for role,command in [('configure',recipes[0][1]),('make',recipes[1][1])]]
        write_fresh(output/'build-process-receipt.json',{'Complete':True,'ApprovalIssued':False,'Head':head,'WorkflowRun':{k:os.environ.get(k,'') for k in ('GITHUB_SHA','GITHUB_RUN_ID','GITHUB_RUN_ATTEMPT','GITHUB_JOB')},'WorkflowSha256':sha(wf.read_bytes()),'PlanSha256':sha(args.plan.read_bytes()),'PinnedOtpCommit':A.PIN,'SourceRoot':str(source),'InstalledPrefix':str(prefix),'SourceBefore':before,'SourceAfter':after,'Roles':list(ROLES),'InstalledClosure':closure,'ProgramIdentitiesBefore':tools_before,'ProgramIdentities':tools,'ObservationVmExplicitlyInvoked':False,'Files':files_receipt(output)})
    except BaseException as error:
        write_fresh(output/'build-failure.json',{'Complete':False,'ErrorType':type(error).__name__,'Files':files_receipt(output)});raise
if __name__=='__main__':main()
