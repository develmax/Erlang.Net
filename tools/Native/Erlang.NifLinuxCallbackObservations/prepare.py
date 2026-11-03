"""Root-only Linux bridge build/admission; source preparation never runs this file."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,shutil,subprocess,sys
P=Path(__file__).resolve().parent;R=P.parents[2]
H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
J=lambda p:json.loads(Path(p).read_bytes())
def module(p,name):
 spec=importlib.util.spec_from_file_location(name,p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
def write(p,v):
 with p.open('xb') as f:f.write((json.dumps(v,indent=2)+'\n').encode())

def project_headers552(root, hash_file):
    paths=[root/'tools/Native/Erlang.NifNativeBackend/native'/name for name in ('nif_bridge_api.h','nif_snprintf791.h')]
    return [dict(Path=str(path.resolve()),Bytes=path.stat().st_size,Sha256=hash_file(path)) for path in paths]

def require_project_headers552(proof, root, hash_file):
    expected=project_headers552(root,hash_file);actual=proof['ProjectHeaderInputs']
    assert len(actual)==len(expected) and {row['Path']:(row['Bytes'],row['Sha256']) for row in actual}=={row['Path']:(row['Bytes'],row['Sha256']) for row in expected},'Project bridge headers changed or unbound'
    return expected

def main():
 p=argparse.ArgumentParser()
 for key in ['expected-head','source-plan','approved-source-plan-sha256','otp-source','runtime-root','build-receipt','approved-build-receipt-sha256','size-native-build','approved-size-native-receipt-sha256','reference-native-build','approved-reference-native-receipt-sha256','output']:p.add_argument('--'+key,required=True)
 p.add_argument('--reuse-n1-bridge-receipt');p.add_argument('--approved-n1-bridge-receipt-sha256')
 a=p.parse_args();assert sys.platform=='linux' and os.uname().machine=='x86_64'
 assert subprocess.check_output(['git','--no-optional-locks','-C',R,'rev-parse','HEAD'],text=True).strip()==a.expected_head
 workflow_head=os.environ.get('EXPECTED_HEAD',os.environ.get('GITHUB_SHA'))
 assert workflow_head is None or workflow_head==a.expected_head
 assert H(a.source_plan)==a.approved_source_plan_sha256.upper()
 plan=J(a.source_plan)
 for row in plan['Files']:
  q=(R/row['Path']).resolve();assert q.is_relative_to(R) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
 common=module(R/'tools/Native/Erlang.NifEntryOriginalObservations/common.py','linux17_common')
 identity=module(R/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py','linux17_identity')
 owned=module(R/'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py','linux17_owned')
 runtime,_=common.approve_runtime(a,{'RuntimeBuildManifestSha256':J(R/'tools/Native/Erlang.NifMakeReferenceOriginalObservations/manifest.json')['RuntimeBuildManifestSha256']},identity)
 out=Path(a.output).resolve();assert not out.exists();out.mkdir(parents=True)
 env=identity.sanitized_environment(os.environ);headers=runtime/'usr/include';stages=[];images=[]
 for family,toolname,directory,approved in [('size','Erlang.NifMapSizeOriginalObservations',a.size_native_build,a.approved_size_native_receipt_sha256),('reference','Erlang.NifMakeReferenceOriginalObservations',a.reference_native_build,a.approved_reference_native_receipt_sha256)]:
  tool=R/'tools/Native'/toolname;manifest=J(tool/'manifest.json');native=Path(directory).resolve();proof=native/'native-build-receipt.json';assert H(proof)==approved.upper()
  build=J(proof);assert build['Status']=='PASS_OWNED_NATIVE_ENTRY_FIXTURE_BUILD' and build['SubjectHead']==a.expected_head and build['SourceManifestSha256']==H(tool/'manifest.json') and build['RuntimeBuildReceiptSha256']==H(a.build_receipt)
  assert len(build['Images'])==1
  image=build['Images'][0];library=Path(image['Path']).resolve();assert library.is_relative_to(native) and H(library)==image['Sha256']
  case=J(tool/'cases.json')['Cases'][0];source=tool/case['NativeSource'].removeprefix('source/');erlang=tool/case['ErlangSource'].removeprefix('source/')
  assert H(source)==image['SourceSha256']
  for row in manifest['Headers']:assert H(headers/row['Name'])==row['Sha256']
  images.append(dict(Module=case['Module'],LibraryPath=str(library),LibrarySha256=H(library),NativeSourcePath=str(source),NativeSourceSha256=H(source),ErlangSourcePath=str(erlang),ErlangSourceSha256=H(erlang),ManifestPath=str(tool/'manifest.json'),ManifestSha256=H(tool/'manifest.json'),BuildReceiptPath=str(proof),BuildReceiptSha256=H(proof),ObservationNames=J(tool/'cases.json')['ObservationNames']))
 project_header_rows=project_headers552(R,H)
 if a.reuse_n1_bridge_receipt:
  parent=Path(a.reuse_n1_bridge_receipt).resolve();assert H(parent)==a.approved_n1_bridge_receipt_sha256.upper()
  proof=J(parent);assert proof['Status']=='VERSION651_LINUX_BRIDGE_BUILD' and proof['Pin']=='ad05823719d77c8faee87348ea39513d4e2f99c5' and proof['SubjectHead']==a.expected_head and proof['NativeLoaded'] is False
  bridge=Path(proof['BridgePath']).resolve();assert bridge.is_relative_to(parent.parent) and H(bridge)==proof['BridgeSha256']
  sources=[R/'tools/Native/Erlang.NifNativeBackend/native'/x for x in ['nif_bridge_exports.c','nif_bridge_varargs.c','nif_bridge_pthread_control.c']]
  assert {Path(x['Path']).resolve() for x in proof['Sources']}=={x.resolve() for x in sources}
  for row in proof['Sources']:assert H(row['Path'])==row['Sha256']
  assert require_project_headers552(proof,R,H)==project_header_rows
  headerrows=[]
  for name in ['erl_nif.h','erl_drv_nif.h','erl_nif_api_funcs.h','erl_int_sizes_config.h']:
   row=next(x for x in proof['HeaderInputs'] if Path(x['Path']).name==name)
   assert H(row['Path'])==row['Sha256']==H(headers/name) and Path(row['Path']).stat().st_size==row['Bytes']
   headerrows.append(dict(Path=str(headers/name),Bytes=(headers/name).stat().st_size,Sha256=H(headers/name)))
  assert {'nif_bridge_register','enif_get_map_size','enif_make_ref','pthread_create','pthread_join','pthread_key_create','pthread_key_delete','pthread_getspecific','pthread_setspecific','__cxa_thread_atexit_impl','pthread_exit','nif_bridge_pthread_register','nif_bridge_pthread_provider','nif_bridge_pthread_calls'}<=set(proof['Exports'])
  # Verify the real preceding build's owned compile/export/ELF process receipts.
  for name in ['compile','exports','elf']:
   identity.require_normal_receipt(J(parent.parent/(name+'.process.json')))
  assert project_headers552(R,H)==project_header_rows
  receipt=out/'bridge-build-receipt.json'
  write(receipt,dict(Status='PASS_SOURCE_BOUND_N1_BRIDGE_REUSE',SubjectHead=a.expected_head,SourcePlanSha256=H(a.source_plan),RuntimeBuildReceiptSha256=H(a.build_receipt),HeaderInputs=headerrows,ProjectHeaderInputs=project_header_rows,Sources=proof['Sources'],BridgePath=str(bridge),BridgeSha256=H(bridge),ParentBuildReceiptPath=str(parent),ParentBuildReceiptSha256=H(parent),CompileExecuted=False,NativeLoaded=False))
  write(out/'admission.json',dict(Status='ROOT_OWN_LINUX_CALLBACK17_ADMISSION',SubjectHead=a.expected_head,BridgePath=str(bridge),BridgeSha256=H(bridge),BridgeReceiptPath=str(receipt),BridgeReceiptSha256=H(receipt),Images=images))
  print(H(out/'admission.json'));return
 assert not a.approved_n1_bridge_receipt_sha256
 gcc,nm,readelf=[shutil.which(x) for x in ['gcc','nm','readelf']];assert gcc and nm and readelf
 bridge=out/'Erlang.NifBridge.so';sources=[R/'tools/Native/Erlang.NifNativeBackend/native'/x for x in ['nif_bridge_exports.c','nif_bridge_varargs.c','nif_bridge_pthread_control.c']]
 def stage(name,argv,seconds=90):
  result=common.stage(owned,identity,argv,out,env,out/name,seconds);stages.append(result)
 stage('gcc-version',[gcc,'--version'],30)
 stage('bridge-build',[gcc,'-shared','-fPIC','-std=c11','-O2','-Wall','-Wextra','-Werror','-I',headers,*sources,'-o',bridge])
 stage('bridge-defined',[nm,'-D','--defined-only','--format=posix',bridge],30)
 stage('bridge-elf',[readelf,'-h','-Ws','-d',bridge],30)
 defined={x.split()[0] for x in (out/'bridge-defined.stdout').read_text().splitlines() if x.split()}
 assert {'nif_bridge_register','nif_bridge_read_term','enif_get_map_size','enif_make_ref','enif_alloc_env','enif_free_env','enif_clear_env','enif_make_copy'}<=defined
 assert project_headers552(R,H)==project_header_rows
 receipt=out/'bridge-build-receipt.json';write(receipt,dict(Status='PASS_OWNED_LINUX_V4_BRIDGE_BUILD',SubjectHead=a.expected_head,SourcePlanSha256=H(a.source_plan),RuntimeBuildReceiptSha256=H(a.build_receipt),HeaderInputs=[dict(Path=str(headers/x),Bytes=(headers/x).stat().st_size,Sha256=H(headers/x)) for x in ['erl_nif.h','erl_drv_nif.h','erl_nif_api_funcs.h','erl_int_sizes_config.h']],ProjectHeaderInputs=project_header_rows,Sources=[dict(Path=str(x),Sha256=H(x)) for x in sources],BridgePath=str(bridge),BridgeSha256=H(bridge),OwnedStages=stages,NativeLoaded=False))
 write(out/'admission.json',dict(Status='ROOT_OWN_LINUX_CALLBACK17_ADMISSION',SubjectHead=a.expected_head,BridgePath=str(bridge),BridgeSha256=H(bridge),BridgeReceiptPath=str(receipt),BridgeReceiptSha256=H(receipt),Images=images))
 print(H(out/'admission.json'))
if __name__=='__main__':main()
