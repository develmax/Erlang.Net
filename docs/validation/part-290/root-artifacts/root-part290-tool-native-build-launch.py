from pathlib import Path
import subprocess,sys,json,hashlib,datetime
r=Path.cwd();args=[sys.executable,'tools/Erlang.NifFixtureHost/native/build_fixture.py','--plan','tools/Erlang.NifFixtureHost/native/source-plan.json','--approved-plan-sha256','D37C784308E2DB8238949A02FAF035736401AB86538D047D3E0E179A7F738E63','--output','artifacts/root-part290-tool-native-build']
p=subprocess.Popen(args,cwd=r,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
timed=False
try:raw,_=p.communicate(timeout=180)
except subprocess.TimeoutExpired:
 timed=True;subprocess.run(['taskkill','/PID',str(p.pid),'/T','/F'],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,check=False);raw,_=p.communicate(timeout=20)
Path('artifacts/root-part290-tool-native-build-launch.log').write_bytes(raw)
rec={'OwnedPid':p.pid,'TimedOut':timed,'ExitCode':p.returncode,'ObservedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'OutputSha256':hashlib.sha256(raw).hexdigest().upper(),'NativeFixtureBehaviorExecuted':False}
Path('artifacts/root-part290-tool-native-build-launch.json').write_text(json.dumps(rec,indent=2)+'\n')
print(json.dumps(rec));print(raw.decode('utf-8',errors='replace')[-3000:])
sys.exit(1 if timed else p.returncode)
