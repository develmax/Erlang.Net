from pathlib import Path
import json,sys,shutil,hashlib,subprocess
r=Path(__file__).resolve().parents[1];a=r/'artifacts'
if len(sys.argv)!=3:raise SystemExit('Usage: actualErlang.NodeCreationContrasts.dll freshControlOutput')
dll=Path(sys.argv[1]).resolve();destination=Path(sys.argv[2]).resolve()
assert dll.is_file() and dll.name=='Erlang.NodeCreationContrasts.dll'
assert destination.is_relative_to(a.resolve()) and not destination.exists();destination.mkdir()
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def snap(p):return {x.relative_to(p).as_posix():sha(x.read_bytes()) for x in p.rglob('*') if x.is_file()} if p.exists() else None
actualsha=sha(dll.read_bytes());rows=[]
labels=['wrong-pin','wrong-tag','incomplete-source','wrong-root','changed-log','wrong-version','missing-executables','plan-wrong-tag','plan-fresh-vm','plan-whitespace','altered-probe','existing-output']
for label in labels:
 d=destination/label;d.mkdir();closure=d/'collector';shutil.copytree(dll.parent,closure)
 root=d/'synthetic-negative-root';version=root/'releases/29/OTP_VERSION';version.parent.mkdir(parents=True);version.write_bytes(b'29.1.1\n')
 (root/'bin').mkdir()
 if label!='missing-executables':
  for file in ['erl.exe','erlc.exe']:(root/'bin'/file).write_bytes(b'not an executable: deliberately invalid synthetic pre-write fixture')
 log=d/'synthetic-negative-build.log';log.write_bytes(b'Deliberately synthetic invalid pre-write fixture; not a native source build.\n')
 evidence={'SourceCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5','SourceTag':'OTP-29.1.1','SourceBuildCompleted':True,'InstalledOtpRoot':str(root),'SourceBuildLog':str(log),'SourceBuildLogSha256':sha(log.read_bytes()),'SyntheticNegativeEvidenceOnly':True}
 expected=b'Pinned source-built runtime evidence does not match'
 if label=='wrong-pin':evidence['SourceCommit']='deliberately-invalid-pin'
 if label=='wrong-tag':evidence['SourceTag']='deliberately-invalid-tag'
 if label=='incomplete-source':evidence['SourceBuildCompleted']=False
 if label=='wrong-root':evidence['InstalledOtpRoot']=str(d/'deliberately-other-root')
 if label=='changed-log':log.write_bytes(log.read_bytes()+b'changed\n')
 if label=='wrong-version':version.write_bytes(b'deliberately-invalid-version\n')
 plan=closure/'source-plan.json'
 if label in ['plan-wrong-tag','plan-fresh-vm','plan-whitespace']:
  parsed=json.loads(plan.read_text())
  if label=='plan-wrong-tag':parsed['SourceTag']='deliberately-invalid-tag'
  if label=='plan-fresh-vm':parsed['FreshVmPerScenario']=False
  plan.write_bytes(plan.read_bytes()+b' ' if label=='plan-whitespace' else (json.dumps(parsed,indent=2)+'\n').encode())
  expected=b'Declared creation boundary source closure differs from frozen inputs.'
 if label=='altered-probe':
  source=closure/'creation_boundary_probe.erl';source.write_bytes(source.read_bytes()+b'\n');expected=b'Declared creation boundary source closure differs from frozen inputs.'
 output=d/'refused-output'
 if label=='existing-output':
  output.mkdir();(output/'sentinel.raw').write_bytes(bytes([0,128,255]));expected=b'Original creation boundary output must not exist.'
 before=snap(output);proof=d/'synthetic-negative-evidence.json';proof.write_bytes((json.dumps(evidence,indent=2)+'\n').encode())
 result=subprocess.run(['dotnet',str(closure/dll.name),str(root),str(output),str(proof)],stdout=subprocess.PIPE,stderr=subprocess.PIPE,timeout=20)
 (d/'stdout.raw').write_bytes(result.stdout);(d/'stderr.raw').write_bytes(result.stderr)
 assert result.returncode==1 and expected in result.stderr and not result.stdout and snap(output)==before,label
 rows.append({'Case':label,'ExitCode':result.returncode,'ExpectedFailureTextPresent':True,'NoOutputMutation':True,'ExecutedDllSha256':actualsha,'EvidenceSha256':sha(proof.read_bytes()),'PlanSha256':sha(plan.read_bytes()),'StdoutSha256':sha(result.stdout),'StderrSha256':sha(result.stderr),'NativeExecutableInvoked':False})
assert sha(dll.read_bytes())==actualsha and len(rows)==12
proof=destination/'proof.json';proof.write_bytes((json.dumps({'Passed':12,'Cases':rows,'ActualDll':str(dll),'ActualDllSha256':actualsha,'NativeExecuted':False,'SyntheticNegativeEvidenceOnly':True,'Scope':'Actual supplied collector DLL pre-write refusals with deliberately invalid synthetic fixtures. No completed native/source-build claim.'},indent=2)+'\n').encode());print(sha(proof.read_bytes()))
