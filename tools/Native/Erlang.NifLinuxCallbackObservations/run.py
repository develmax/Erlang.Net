"""Root-only bounded Linux SDK observations; preserves raw terms and process receipts."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,shutil
R=Path(__file__).resolve().parents[3]
H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
J=lambda p:json.loads(Path(p).read_bytes())
def module(path,name):
 s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def write(path,value):
 with path.open('xb') as f:f.write((json.dumps(value,indent=2)+'\n').encode())

def project_headers552(root, hash_file):
    paths=[root/'tools/Native/Erlang.NifNativeBackend/native'/name for name in ('nif_bridge_api.h','nif_snprintf791.h')]
    return [dict(Path=str(path.resolve()),Bytes=path.stat().st_size,Sha256=hash_file(path)) for path in paths]

def require_project_headers552(proof, root, hash_file):
    expected=project_headers552(root,hash_file);actual=proof['ProjectHeaderInputs']
    assert len(actual)==len(expected) and {row['Path']:(row['Bytes'],row['Sha256']) for row in actual}=={row['Path']:(row['Bytes'],row['Sha256']) for row in expected},'Project bridge headers changed or unbound'
    return expected

def main():
 p=argparse.ArgumentParser()
 for key in ['admission','approved-admission-sha256','focus','output']:p.add_argument('--'+key,required=True)
 p.add_argument('--framework-version')
 a=p.parse_args();admission=Path(a.admission).resolve();assert H(admission)==a.approved_admission_sha256.upper()
 ad=J(admission);focus=Path(a.focus).resolve();assert focus.is_relative_to(R) and focus.name=='Erlang.NifLinuxCallbackFocus.dll'
 dotnet=shutil.which('dotnet');assert dotnet
 for path,sha in [(ad['BridgePath'],ad['BridgeSha256']),(ad['BridgeReceiptPath'],ad['BridgeReceiptSha256'])]:assert H(path)==sha
 project_header_rows=require_project_headers552(J(ad['BridgeReceiptPath']),R,H)
 for image in ad['Images']:
  for pathkey,hashkey in [('LibraryPath','LibrarySha256'),('NativeSourcePath','NativeSourceSha256'),('ErlangSourcePath','ErlangSourceSha256'),('ManifestPath','ManifestSha256'),('BuildReceiptPath','BuildReceiptSha256')]:assert H(image[pathkey])==image[hashkey]
 dlls=[dict(Path=str(p),Sha256=H(p)) for p in sorted(focus.parent.glob('*.dll'))];assert any(Path(r['Path']).name=='Erlang.Runtime.dll' for r in dlls)
 assert H(focus.parent/'Erlang.NifBridge.so')==ad['BridgeSha256']
 out=Path(a.output).resolve();assert not out.exists();out.mkdir(parents=True)
 common=module(R/'tools/Native/Erlang.NifEntryOriginalObservations/common.py','linux17_run_common')
 identity=module(R/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py','linux17_run_identity')
 owned=module(R/'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py','linux17_run_owned')
 env=identity.sanitized_environment(os.environ);env.update(DOTNET_PROCESSOR_COUNT='2',DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
 write(out/'inputs.json',dict(AdmissionPath=str(admission),AdmissionSha256=H(admission),FocusPath=str(focus),FocusSha256=H(focus),DotnetPath=dotnet,DotnetSha256=H(dotnet),AdjacentDlls=dlls,ProjectHeaderInputs=project_header_rows))
 stages=[];failures=[]
 for route in ['reviewed','ordinary']:
  failure=None
  try:
   stages.append(common.stage(owned,identity,([dotnet,'exec','--fx-version',a.framework_version] if a.framework_version else [dotnet])+[focus,admission,H(admission),out/route,route],R,env,out/route,180))
   completion=J(out/route/'completion.json');assert completion['Status']=='ACQUIRED_LINUX_CALLBACK17_TWO_RUNTIME_ROWS' and completion['AdmissionSha256']==H(admission) and completion['Route']==route and completion['RuntimeDomains']==2 and completion['PrimaryObservationCount']==17 and completion['FirstForeignAllocationOwnershipChecked'] and completion['AllImageRetirementsCompleted'] and completion['RuntimeDisposalsCompleted']
   for row in completion['Files']:assert H(out/route/row['Path'])==row['Sha256']
  except Exception as error:
   failure=repr(error);failures.append(dict(Route=route,Failure=failure))
  # Failure aggregation never permits a still-running child or changed shared input.
  try:
   receipt=J(out/(route+'.process.json'))
   assert receipt['ParentReaped'] and receipt['StreamsClosed'] and receipt['CleanupFailure'] is None and not receipt['RemainingGroupKillRequested']
   assert require_project_headers552(J(ad['BridgeReceiptPath']),R,H)==project_header_rows
   for row in dlls:assert H(row['Path'])==row['Sha256']
   for path,expected in [(ad['BridgePath'],ad['BridgeSha256']),(ad['BridgeReceiptPath'],ad['BridgeReceiptSha256'])]:assert H(path)==expected
   for image in ad['Images']:
    for key in ['Library','NativeSource','ErlangSource','Manifest','BuildReceipt']:assert H(image[key+'Path'])==image[key+'Sha256']
  except Exception as error:
   failures.append(dict(Route=route,IsolationFailure=repr(error)));break
 write(out/'completion.json',dict(Status='FAILED_TWO_LINUX_ROUTES_CALLBACK17' if failures else 'ACQUIRED_TWO_LINUX_ROUTES_CALLBACK17',SubjectHead=ad['SubjectHead'],AdmissionSha256=H(admission),InputsSha256=H(out/'inputs.json'),OwnedStages=stages,Routes=['reviewed','ordinary'],PrimaryRowsPerRoute=17,RuntimeDomainsPerRoute=2,OriginalComparisonExecuted=False,Failures=failures,FrameworkVersion=a.framework_version))
 if failures:raise RuntimeError('Independent route failures: '+repr(failures))
if __name__=='__main__':main()
