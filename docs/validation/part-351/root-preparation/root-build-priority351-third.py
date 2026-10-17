from pathlib import Path
import json,hashlib,subprocess,os,time
r=Path.cwd();c=r/'artifacts/spawn-priority351-candidate';p=r/'artifacts/spawn-priority351-preparation'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()=='c1f219f31726fc93e537080f400651d300c47d72'
assert sha(p/'runtime-styled-source-freeze2.json')=='F8A3CB00D45BD9491573B36F7C8857C925794AC4B1A320F39DD7B6EE90948404'
assert sha(p/'runtime-build-closure2.json')=='884BB2BD278FB14A8B39EE540C7B1D9CB97E28F441E5C75E497F33B25526FCC0'
peer=r/'artifacts/spawn-priority351-source-independent/report.json'
assert sha(peer)=='8D96FAA8337AD4EEBE2CE0104A8E3D170A51F750FD442DDA15B5BBA94383D0FB'
frozen=read(p/'runtime-styled-source-freeze2.json');assert len(frozen['Files'])==20
for row in frozen['Files']:assert row['Sha256']==sha(c/row['Path'])
assert sha(r/'artifacts/spawn-priority351-styled-independent/report.json')=='ECC70267D4B067DAF7E7E4DEC1F1E46CD8117BEA8F09CA2A91C156DC1D883292'
closure=read(p/'runtime-build-closure2.json')['Files'];assert len(closure)==2780
for row in closure:assert sha(c/row['Path'])==row['Sha256'],row['Path']
out=r/'artifacts/spawn-priority351-core-build-actual2';out.mkdir(exist_ok=False)
def save(name,obj):
    with (out/name).open('x',encoding='utf-8',newline='\n') as stream:json.dump(obj,stream,indent=2);stream.write('\n')
command=['C:/Program Files/dotnet/dotnet.exe','build','src/Erlang.Compiler/Erlang.Compiler.csproj','--nologo','-v:minimal','-m:1','-nodeReuse:false','--disable-build-servers']
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',DOTNET_CLI_TELEMETRY_OPTOUT='1')
start=time.monotonic();timedout=False;failures=[]
with (out/'stdout.log').open('xb') as stdout,(out/'stderr.log').open('xb') as stderr:
    process=subprocess.Popen(command,cwd=c,env=env,stdout=stdout,stderr=stderr)
    save('process.json',dict(Pid=process.pid,Command=command,Candidate=str(c),TimeoutSeconds=240,SourceFreezeSha256=sha(p/'runtime-styled-source-freeze2.json'),SourcePeerSha256=sha(peer)))
    try:process.wait(timeout=240)
    except subprocess.TimeoutExpired:
        timedout=True
        try:
            result=subprocess.run(['taskkill','/PID',str(process.pid),'/T','/F'],capture_output=True,timeout=30);(out/'kill.stdout.log').write_bytes(result.stdout);(out/'kill.stderr.log').write_bytes(result.stderr);process.wait(timeout=30)
        except Exception as error:failures.append(repr(error))
receipt=dict(ExitCode=process.poll(),Settled=process.poll() is not None,TimedOut=timedout,Seconds=time.monotonic()-start,LifecycleFailures=failures,StdoutSha256=sha(out/'stdout.log'),StderrSha256=sha(out/'stderr.log'),GuestTestsExecuted=False,MainModified=False)
save('exit.json',receipt)
for row in closure:assert sha(c/row['Path'])==row['Sha256'],row['Path']
print(json.dumps(receipt),flush=True)
assert receipt['ExitCode']==0 and receipt['Settled'] and not timedout and not failures
