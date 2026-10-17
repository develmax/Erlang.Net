from pathlib import Path
import json,hashlib,subprocess,time,os
r=Path.cwd();c=r/'artifacts/spawn-priority351-full-candidate';o=r/'artifacts/spawn-priority351-full-style';sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
dll=r/'tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll';assert sha(dll)=='279980EF0166578AA46702ECFBF984A3CB58974971987C715A123766BB0DD820'
files=[p for folder in ['src','tools','tests','benchmarks','examples'] for p in (c/folder).rglob('*.cs') if not {'bin','obj'}&set(p.relative_to(c).parts)]
before={p.relative_to(c).as_posix():sha(p) for p in files};o.mkdir(exist_ok=False)
for mode in ['--write','--check']:
    stem=mode[2:];command=['C:/Program Files/dotnet/dotnet.exe',str(dll),mode,str(c)];start=time.monotonic()
    with (o/(stem+'.stdout.log')).open('xb') as out,(o/(stem+'.stderr.log')).open('xb') as err:
        process=subprocess.Popen(command,cwd=c,stdout=out,stderr=err,env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2'));timedout=False
        try:process.wait(timeout=90)
        except subprocess.TimeoutExpired:
            timedout=True;process.kill();process.wait(timeout=15)
    receipt=dict(Pid=process.pid,Command=command,ExitCode=process.poll(),Settled=process.poll() is not None,TimedOut=timedout,Seconds=time.monotonic()-start,StdoutSha256=sha(o/(stem+'.stdout.log')),StderrSha256=sha(o/(stem+'.stderr.log')),SdkExecuted=False,MainModified=False)
    (o/(stem+'.exit.json')).write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8',newline='\n');assert process.returncode==0 and not timedout
changes=[dict(Path=path,BeforeSha256=old,Sha256=sha(c/path)) for path,old in before.items() if sha(c/path)!=old]
(o/'changes.json').write_text(json.dumps(dict(Files=changes,BeforeFiles=len(before),StyleDllSha256=sha(dll)),indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(dict(Changed=len(changes),Paths=[x['Path'] for x in changes])))
