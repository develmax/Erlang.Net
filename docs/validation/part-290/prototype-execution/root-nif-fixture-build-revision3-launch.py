from pathlib import Path
import subprocess,sys,json,hashlib,datetime
r=Path.cwd();args=[sys.executable,'artifacts/root-nif-build-revision3/build_fixture.py','--plan','artifacts/root-nif-build-revision3/source-plan.json','--approved-plan-sha256','BE46C27611CA9909D7CBC7CAADDB27CF7C3BB0E483C20FB85DA6651BF26814B2','--output','artifacts/root-nif-fixture-build-revision3']
p=subprocess.Popen(args,cwd=r,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
timed=False
try:raw,_=p.communicate(timeout=180)
except subprocess.TimeoutExpired:
 timed=True;subprocess.run(['taskkill','/PID',str(p.pid),'/T','/F'],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,check=False);raw,_=p.communicate(timeout=20)
Path('artifacts/root-nif-fixture-build-revision3-launch.log').write_bytes(raw)
rec={'OwnedPid':p.pid,'TimedOut':timed,'ExitCode':p.returncode,'ObservedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'OutputSha256':hashlib.sha256(raw).hexdigest().upper(),'NativeFixtureBehaviorExecuted':False}
Path('artifacts/root-nif-fixture-build-revision3-launch.json').write_text(json.dumps(rec,indent=2)+'\n')
print(json.dumps(rec));print(raw.decode('utf-8',errors='replace')[-3000:])
sys.exit(1 if timed else p.returncode)
