"""Root-only Linux bridge build/admission; source preparation never runs this file."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,shutil,subprocess,sys
P=Path(__file__).resolve().parent;R=P.parents[1]
H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
J=lambda p:json.loads(Path(p).read_bytes())
def module(p,name):
 spec=importlib.util.spec_from_file_location(name,p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
def write(p,v):
 with p.open('xb') as f:f.write((json.dumps(v,indent=2)+'\n').encode())
def main():
 p=argparse.ArgumentParser()
 for key in ['expected-head','source-plan','approved-source-plan-sha256','otp-source','runtime-root','build-receipt','approved-build-receipt-sha256','size-native-build','approved-size-native-receipt-sha256','reference-native-build','approved-reference-native-receipt-sha256','output']:p.add_argument('--'+key,required=True)
 a=p.parse_args();assert sys.platform=='linux' and os.uname().machine=='x86_64'
 assert subprocess.check_output(['git','--no-optional-locks','-C',R,'rev-parse','HEAD'],text=True).strip()==a.expected_head==os.environ['GITHUB_SHA']
 assert H(a.source_plan)==a.approved_source_plan_sha256.upper()
 plan=J(a.source_plan)
 for row in plan['Files']:
  q=(R/row['Path']).resolve();assert q.is_relative_to(R) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
 common=module(R/'tools/Erlang.NifEntryOriginalObservations/common.py','linux17_common')
 identity=module(R/'tools/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py','linux17_identity')
 owned=module(R/'tools/Erlang.GuestConnectedSendObservations/owned_process.py','linux17_owned')
 runtime,_=common.approve_runtime(a,{'RuntimeBuildManifestSha256':J(R/'tools/Erlang.NifMakeReferenceOriginalObservations/manifest.json')['RuntimeBuildManifestSha256']},identity)
 out=Path(a.output).resolve();assert not out.exists();out.mkdir(parents=True)
 env=identity.sanitized_environment(os.environ);headers=runtime/'usr/include';stages=[];images=[]
 for family,toolname,directory,approved in [('size','Erlang.NifMapSizeOriginalObservations',a.size_native_build,a.approved_size_native_receipt_sha256),('reference','Erlang.NifMakeReferenceOriginalObservations',a.reference_native_build,a.approved_reference_native_receipt_sha256)]:
  tool=R/'tools'/toolname;manifest=J(tool/'manifest.json');native=Path(directory).resolve();proof=native/'native-build-receipt.json';assert H(proof)==approved.upper()
  build=J(proof);assert build['Status']=='PASS_OWNED_NATIVE_ENTRY_FIXTURE_BUILD' and build['SubjectHead']==a.expected_head and build['SourceManifestSha256']==H(tool/'manifest.json') and build['RuntimeBuildReceiptSha256']==H(a.build_receipt)
  assert len(build['Images'])==1
  image=build['Images'][0];library=Path(image['Path']).resolve();assert library.is_relative_to(native) and H(library)==image['Sha256']
  case=J(tool/'cases.json')['Cases'][0];source=tool/case['NativeSource'].removeprefix('source/');erlang=tool/case['ErlangSource'].removeprefix('source/')
  assert H(source)==image['SourceSha256']
  for row in manifest['Headers']:assert H(headers/row['Name'])==row['Sha256']
  images.append(dict(Module=case['Module'],LibraryPath=str(library),LibrarySha256=H(library),NativeSourcePath=str(source),NativeSourceSha256=H(source),ErlangSourcePath=str(erlang),ErlangSourceSha256=H(erlang),ManifestPath=str(tool/'manifest.json'),ManifestSha256=H(tool/'manifest.json'),BuildReceiptPath=str(proof),BuildReceiptSha256=H(proof),ObservationNames=J(tool/'cases.json')['ObservationNames']))
 gcc,nm,readelf=[shutil.which(x) for x in ['gcc','nm','readelf']];assert gcc and nm and readelf
 bridge=out/'Erlang.NifBridge.so';sources=[R/'tools/Erlang.NifNativeBackend/native'/x for x in ['nif_bridge_exports.c','nif_bridge_varargs.c','nif_bridge_pthread_control.c']]
 def stage(name,argv,seconds=90):
  result=common.stage(owned,identity,argv,out,env,out/name,seconds);stages.append(result)
 stage('gcc-version',[gcc,'--version'],30)
 stage('bridge-build',[gcc,'-shared','-fPIC','-std=c11','-O2','-Wall','-Wextra','-Werror','-I',headers,*sources,'-o',bridge])
 stage('bridge-defined',[nm,'-D','--defined-only','--format=posix',bridge],30)
 stage('bridge-elf',[readelf,'-h','-Ws','-d',bridge],30)
 defined={x.split()[0] for x in (out/'bridge-defined.stdout').read_text().splitlines() if x.split()}
 assert {'nif_bridge_register','nif_bridge_read_term','enif_get_map_size','enif_make_ref','enif_alloc_env','enif_free_env','enif_clear_env','enif_make_copy'}<=defined
 receipt=out/'bridge-build-receipt.json';write(receipt,dict(Status='PASS_OWNED_LINUX_V4_BRIDGE_BUILD',SubjectHead=a.expected_head,SourcePlanSha256=H(a.source_plan),RuntimeBuildReceiptSha256=H(a.build_receipt),HeaderInputs=[dict(Path=str(headers/x),Bytes=(headers/x).stat().st_size,Sha256=H(headers/x)) for x in ['erl_nif.h','erl_drv_nif.h','erl_nif_api_funcs.h','erl_int_sizes_config.h']],Sources=[dict(Path=str(x),Sha256=H(x)) for x in sources],BridgePath=str(bridge),BridgeSha256=H(bridge),OwnedStages=stages,NativeLoaded=False))
 write(out/'admission.json',dict(Status='ROOT_OWN_LINUX_CALLBACK17_ADMISSION',SubjectHead=a.expected_head,BridgePath=str(bridge),BridgeSha256=H(bridge),BridgeReceiptPath=str(receipt),BridgeReceiptSha256=H(receipt),Images=images))
 print(H(out/'admission.json'))
if __name__=='__main__':main()
