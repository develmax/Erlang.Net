"""Root-only bounded existing-tool readiness run; output staged separately, never auto-published."""
import argparse,sys,time
sys.dont_write_bytecode=True
from common348 import *
p=argparse.ArgumentParser()
for n in ('candidate','main','full-dir','independent-guard','tool-dll','output-dir'):p.add_argument('--'+n,type=Path,required=True)
for n in ('independent-guard-sha256','tool-dll-sha256'):p.add_argument('--'+n,required=True)
a=p.parse_args();c=a.candidate.resolve(strict=True);main=a.main.resolve(strict=True);full=a.full_dir.resolve(strict=True)
authority(c,full,a.independent_guard,a.independent_guard_sha256);assert sha(a.tool_dll)==a.tool_dll_sha256.upper() and not a.output_dir.exists()
assert a.tool_dll.resolve().is_relative_to(c)
for row in read(full/'source-freeze.json')['Files']:assert sha(checked(main,row['Path']))==row['Sha256']
out=a.output_dir;out.mkdir(parents=True);command=['dotnet',str(a.tool_dll.resolve()),'readiness',str(main/'docs'),str(full/'tests.json'),str(out/'generated')];start=time.monotonic()
with (out/'stdout.txt').open('xb') as stdout,(out/'stderr.txt').open('xb') as stderr:
 try:process=subprocess.run(command,cwd=main,stdout=stdout,stderr=stderr,timeout=60)
 except subprocess.TimeoutExpired:
  save(out/'exit.json',dict(Command=command,ExitCode=None,Settled=True,TimedOut=True,Seconds=time.monotonic()-start,Termination='subprocess.run timeout kills and waits for its owned child',ToolDllSha256=sha(a.tool_dll),FullGuardSha256=sha(a.independent_guard)))
  raise
save(out/'exit.json',dict(Command=command,ExitCode=process.returncode,Settled=True,TimedOut=False,Seconds=time.monotonic()-start,ToolDllSha256=sha(a.tool_dll),FullGuardSha256=sha(a.independent_guard)))
assert process.returncode==0
readiness=read(out/'generated/module-readiness.json');assert readiness['LocalPassedTests']==7268 and (readiness['VerifiedCompatibleMfas'],readiness['KnownDifferenceMfas'],readiness['NeedsVerificationMfas'])==(5,19,563)
files=[]
for name in ['module-readiness.json','MODULE_READINESS.md','API_COMPATIBILITY.md']:
 p=out/'generated'/name;files.append(dict(SourcePath=str(p.resolve()),TargetPath='docs/'+name,Bytes=p.stat().st_size,Sha256=sha(p)))
save(out/'copy-plan.json',dict(Status='ACTUAL348_READINESS_OUTPUTS_PREPARED_NOT_APPLIED',Files=files,MainModified=False))
