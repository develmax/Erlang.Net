from pathlib import Path
import subprocess,json,os,hashlib,time
r=Path.cwd();d=r/'artifacts/root-part317-composed1';read=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper()
f=d/'focus-source-freeze.json';cl=d/'focus-source-resource-closure.json';v=read(f)
assert h(f.read_bytes())=='346BAEDF611C1FA69B77CFF0EBF6C6F238A2967D9D95158D02E29D65984234F3' and h(cl.read_bytes())==v['SourceClosureSha256']
for e in read(cl)['Files']:assert h((d/'candidate'/e['Path']).read_bytes())==e['Sha256']
out=d/'managed-actual-results.json';receipt=d/'managed-process.json';assert not out.exists() and not receipt.exists()
args=['dotnet',str(r/'artifacts/clr-memory-public317-managed-focus-proposal1/bin/Debug/net10.0/MemoryPublicManagedFocus.dll'),str(out),str(f),h(f.read_bytes())]
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2');start=time.monotonic();p=subprocess.Popen(args,stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=env);expired=False
try:stdout,stderr=p.communicate(timeout=240)
except subprocess.TimeoutExpired:
 expired=True;subprocess.run(['taskkill','/PID',str(p.pid),'/T','/F'],stdout=subprocess.PIPE,stderr=subprocess.PIPE);stdout,stderr=p.communicate()
(d/'managed-stdout.log').write_bytes(stdout);(d/'managed-stderr.log').write_bytes(stderr)
for e in read(cl)['Files']:assert h((d/'candidate'/e['Path']).read_bytes())==e['Sha256']
actual=read(out) if out.exists() else None
passed=not expired and p.returncode==0 and actual is not None and actual['Passed'] and actual['Executed']==11 and actual['Failed']==0
receipt.write_bytes((json.dumps({'Status':'PASS' if passed else 'FAIL','ExitCode':p.returncode,'TimedOut':expired,'ElapsedSeconds':time.monotonic()-start,'SourceFreezeSha256':h(f.read_bytes()),'SourceClosureSha256':h(cl.read_bytes()),'ActualReportSha256':h(out.read_bytes()) if out.exists() else None,'StdoutSha256':h(stdout),'StderrSha256':h(stderr),'SourceClosureVerifiedBeforeAndAfter':True,'ScriptSha256':h(Path(__file__).read_bytes())},indent=2)+'\n').encode())
print(stdout.decode(errors='replace'));assert passed
