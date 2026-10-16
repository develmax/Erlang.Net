"""Passive replay of the published typed reviewer over downloaded bytes; no native execution."""
from pathlib import Path
import hashlib,json,subprocess,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';mapping=json.loads((a/'pack327-native-run-map.json').read_bytes());head=mapping['Head'];run_id=mapping['Run']
d=a/f'pack327-watch-{run_id}-terminal';items=json.loads((d/'artifacts.json').read_bytes())['artifacts'];assert len(items)==1
ex=d/('artifact-'+str(items[0]['id']));base=a/'pack327-original13-source-contracts';assert not base.exists();base.mkdir()
blob=lambda name:subprocess.check_output(['git','cat-file','blob',head+':'+name])
for path in subprocess.check_output(['git','ls-tree','-r','--name-only',head,'--','tools/Erlang.NifPendingVarargsObservations/original']).decode().splitlines():
 dst=base/'original'/Path(path).name;dst.parent.mkdir(exist_ok=True);dst.write_bytes(blob(path))
for path,name in [('tools/Erlang.NifPendingVarargsObservations/verify-original13.py','verify-original13.py'),('tools/Erlang.BeamFixtures/verify-metadata-mutations.py','verify-metadata-mutations.py'),('docs/validation/part-327/final-native/guard.json','windows-main-native-guard.json'),('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','reviewed-source-plan.json')]:
 (base/name).write_bytes(blob(path))
digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
assert digest(base/'verify-original13.py')=='46F8837A41981C15DD6D49E3F16CFE06ABB2A8DE2B404BBCF84D9B93A248150F'
report=a/'pack327-original13-independent-local-report.json';assert not report.exists()
args=[sys.executable,'-B',str(base/'verify-original13.py'),'--observations',str(ex/'nif-pending-original-observations'),'--proposal',str(base/'original'),'--build-provenance',str(ex/'nif-pending-otp-build-provenance.json'),'--decoder-source',str(base/'verify-metadata-mutations.py'),'--report',str(report)]
result=subprocess.run(args,capture_output=True,text=True)
capture={'Head':head,'Arguments':args,'ExitCode':result.returncode,'Stdout':result.stdout,'Stderr':result.stderr,'NativeExecuted':False,'MainEdited':False,'GuardPublishedSourceSha256':digest(base/'verify-original13.py'),'ReportSha256':digest(report) if report.exists() else None}
out=a/'pack327-original13-independent-local-capture.json';assert not out.exists();out.write_text(json.dumps(capture,indent=2)+'\n');print(json.dumps(capture));raise SystemExit(result.returncode)
