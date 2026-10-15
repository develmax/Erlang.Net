from pathlib import Path
import subprocess,sys,json,hashlib,datetime
r=Path.cwd();args=[sys.executable,'artifacts/nif-boundary-build-revision2/build_fixture.py','--plan','artifacts/nif-boundary-build-revision2/source-plan.json','--approved-plan-sha256','4F8A20012181EF8728CC8AFF49873DC45CC56AB8ACB3057AA313105BF7662077','--output','artifacts/root-nif-fixture-build-revision2']
p=subprocess.Popen(args,cwd=r,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
timed=False
try:raw,_=p.communicate(timeout=180)
except subprocess.TimeoutExpired:
 timed=True;subprocess.run(['taskkill','/PID',str(p.pid),'/T','/F'],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,check=False);raw,_=p.communicate(timeout=20)
Path('artifacts/root-nif-fixture-build-revision2-launch.log').write_bytes(raw)
rec={'OwnedPid':p.pid,'TimedOut':timed,'ExitCode':p.returncode,'ObservedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'OutputSha256':hashlib.sha256(raw).hexdigest().upper(),'NativeFixtureBehaviorExecuted':False}
Path('artifacts/root-nif-fixture-build-revision2-launch.json').write_text(json.dumps(rec,indent=2)+'\n')
print(json.dumps(rec));print(raw.decode('utf-8',errors='replace')[-3000:])
sys.exit(1 if timed else p.returncode)
