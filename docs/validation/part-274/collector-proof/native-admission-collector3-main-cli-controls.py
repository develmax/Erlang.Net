from pathlib import Path
import json,subprocess,hashlib,shutil,sys
r=Path(__file__).resolve().parents[1];a=r/'artifacts'
if len(sys.argv)!=3:raise SystemExit('Usage: script actualAdmissionOracle.dll freshControlDirectory')
dll=Path(sys.argv[1]).resolve();bin=dll.parent;dest=Path(sys.argv[2]).resolve()
assert dll.name=='AdmissionOracle.dll' and dll.is_file()
assert dest.is_relative_to(a.resolve()) and not dest.exists();dest.mkdir()
old=a/'setnode2-native-oracle-revision2-cli-controls'
proof=json.loads((a/'setnode2-native-oracle-revision3-cli-controls/proof.json').read_text())
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def snap(p):return {str(x.relative_to(p)):sha(x.read_bytes()) for x in p.rglob('*') if x.is_file()} if p.exists() else None
actualsha=sha(dll.read_bytes());rows=[]
for x in proof['Cases']:
 name=x['Case'];fixture=old/name;d=dest/name;d.mkdir();closure=d/'collector';shutil.copytree(bin,closure)
 assert sha((closure/dll.name).read_bytes())==actualsha
 for n in ['source-plan.json','local_node_admission_probe.erl','setnode_compiled_driver.erl','setnode_gateway_probe.erl']:
  frozen=(fixture/'collector'/n).read_bytes()
  if frozen!=(closure/n).read_bytes():(closure/n).write_bytes(frozen)
 evidence=r/x['FixtureEvidencePath'];assert evidence.is_file()
 output=fixture/'refused-output' if name=='existing-output' else d/'refused-output';before=snap(output)
 result=subprocess.run(['dotnet',str(closure/dll.name),str(fixture/'synthetic-negative-root'),str(output),str(evidence)],stdout=subprocess.PIPE,stderr=subprocess.PIPE,timeout=20)
 (d/'stdout.raw').write_bytes(result.stdout);(d/'stderr.raw').write_bytes(result.stderr)
 assert result.returncode==1 and snap(output)==before,name
 rows.append({'Case':name,'ExitCode':result.returncode,'NoOutputMutation':True,'FixtureEvidencePath':str(evidence.relative_to(r)),'FixtureEvidenceSha256':sha(evidence.read_bytes()),'ExecutedDllSha256':actualsha,'PlanSha256':sha((closure/'source-plan.json').read_bytes()),'StdoutSha256':sha(result.stdout),'StderrSha256':sha(result.stderr),'OriginalReviewedCasePreserved':True})
assert sha(dll.read_bytes())==actualsha and len(rows)==12
p=dest/'proof.json';p.write_bytes((json.dumps({'Passed':len(rows),'Cases':rows,'ActualInputDll':str(dll),'ActualInputDllSha256':actualsha,'NativeExecuted':False,'SyntheticNegativeEvidenceOnly':True,'Scope':'Actual supplied collector DLL with exact previously reviewed deliberately invalid fixture inputs; no native executable invoked.'},indent=2)+'\n').encode())
print(sha(p.read_bytes()))
