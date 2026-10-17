"""Bounded readonly managed readiness red/green; no build, native library or Main writes."""
from pathlib import Path
import hashlib,importlib.util,json,os,subprocess,zipfile
R=Path(__file__).resolve().parents[2];P=Path(__file__).parent;O=R/'artifacts/part358-readiness-focus-actual1'
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();S=lambda b:hashlib.sha256(b).hexdigest().upper();J=lambda p:json.loads(p.read_bytes());B=lambda v:(json.dumps(v,indent=2)+'\n').encode()
head='64460f8686b0f4d2b7e6d9a363b49f8d09fa00da';assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=R).decode().strip()==head
assert not O.exists();O.mkdir()
ledger=J(R/'docs/mfa-compatibility.json');scope=J(R/'docs/module-readiness-scope.json');paths={x['Path'] for x in ledger['VerifiedSourceFiles']}
paths.update('docs/'+e['Path'] for x in ledger['Entries'] for e in x['Evidence']);assert len(paths)==198
paths.update(['reference-baseline.json','docs/otp-inventory.json','docs/supported-mfas.json','docs/mfa-compatibility.json','docs/module-readiness-scope.json','docs/'+scope['ComparisonTestReport']])
ci=R/'artifacts/part357-remote-ci/run-39017498337/artifact-12095249090/tests.json';assert J(ci)['Passed']==7386 and J(ci)['Failed']==0
evidence='docs/validation/part-357/current-source-preservation.json';report=J(P/'source'/evidence.replace('current-source-preservation.json','windows-checkout-evidence-eol-repair.json'));prior=report['BeforeMismatches'][0]
inputs=[]
for name in sorted(paths):
 raw=subprocess.check_output(['git','show',head+':'+name],cwd=R,stderr=subprocess.DEVNULL)
 for stage in ('before','after'):
  data=raw
  if name==evidence and stage=='before':
   assert b'\r' not in raw;data=raw.replace(b'\n',b'\r\n');assert len(data)==prior['CurrentWindowsBytes'] and S(data)==prior['CurrentWindowsSha256']
  q=O/stage/name;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(data);inputs.append(dict(Stage=stage,Path=name,Bytes=len(data),Sha256=S(data)))
# Bind the reused canonical Tool and every DLL to the package accepted by canonical full2.
candidate=R/'artifacts/part357-candidate2';driver=candidate/'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll'
package=candidate/'artifacts/packages/Erlang.Net.Tool.0.1.0.nupkg';capture=J(R/'artifacts/part357-full-actual2/captured-outputs.json');assert H(package)==next(x['Sha256'] for x in capture['Packages'] if x['Path'].endswith('Erlang.Net.Tool.0.1.0.nupkg'))
dlls=[]
with zipfile.ZipFile(package) as archive:
 for q in driver.parent.glob('*.dll'):
  member='tools/net10.0/any/'+q.name;assert archive.read(member)==q.read_bytes();dlls.append(dict(Path=q.relative_to(R).as_posix(),Sha256=H(q)))
# All compiled Tool/runtime project source files match current committed source bytes.
for root in ('src','tools/Erlang.Tool'):
 for q in (candidate/root).rglob('*.cs'):
  if 'obj' in q.parts or 'bin' in q.parts:continue
  name=q.relative_to(candidate).as_posix();assert q.read_bytes()==subprocess.check_output(['git','show',head+':'+name],cwd=R,stderr=subprocess.DEVNULL)
(O/'inputs.json').write_bytes(B(dict(Head=head,Rows=inputs,DriverDlls=dlls,PackageSha256=H(package),CiTestReportSha256=H(ci),Scope='Exact pre-repair Windows evidence CRLF bytes reconstructed from Git and independently measured checkout SHA; after keeps fixed raw LF. All198 bound paths materialized, same docs/inventory/baseline/current/comparison reports; no SDK build or native NIF execution.')))
helper=R/'artifacts/spawn-priority351-preparation/execute-root.py';assert H(helper)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
spec=importlib.util.spec_from_file_location('owned_readiness358',helper);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',DOTNET_CLI_TELEMETRY_OPTOUT='1',MSBUILDDISABLENODEREUSE='1')
receipts=[]
for stage in ('before','after'):
 d=O/stage;command=['C:/Program Files/dotnet/dotnet.exe',str(driver),'readiness',str(d/'docs'),str(ci),str(d/'readiness-output')]
 try:m.owned(command,d,'readiness',30,env,R)
 except RuntimeError:
  assert stage=='before'
 v=J(d/'readiness.exit.json');assert v['Settled'] and not v['TimedOut'] and not v['Failures'] and v['ExitCode']==(1 if stage=='before' else 0)
 assert H(d/'readiness.stdout.log')==v['StdoutSha256'] and H(d/'readiness.stderr.log')==v['StderrSha256']
 if stage=='before':assert 'Invalid baseline-bound MFA compatibility ledger or evidence' in (d/'readiness.stderr.log').read_text()
 else:
  result=J(d/'readiness-output/module-readiness.json');assert result['RegisteredMfas']==587 and result['LocalPassedTests']==7386 and result['VerifiedCompatibleMfas']==16 and result['KnownDifferenceMfas']==30 and result['NeedsVerificationMfas']==541
 receipts.append(dict(Stage=stage,ExitCode=v['ExitCode'],Pid=v['Pid'],ElapsedSeconds=v['ElapsedSeconds'],ReceiptSha256=H(d/'readiness.exit.json')))
for row in inputs:assert H(O/row['Stage']/row['Path'])==row['Sha256']
for row in dlls:assert H(R/row['Path'])==row['Sha256']
(O/'completion.json').write_bytes(B(dict(Status='PASS_ACTUAL_MANAGED_READINESS_RED_BEFORE_GREEN_AFTER_EOL_ONLY',Head=head,Rows=len(paths),BoundSourceAndEvidenceRows=198,Receipts=receipts,RegistryRows=587,SourceGuards=157,UniqueEvidenceFiles=41,CompatibilityCounts={'Verified compatible':16,'Known differences':30,'Needs verification':541},CiRunStillFailed=True,FullCiRerunPerformed=False,BuildExecuted=False,NativeLibraryExecuted=False,MainChanged=False)))
print(H(O/'completion.json'))
