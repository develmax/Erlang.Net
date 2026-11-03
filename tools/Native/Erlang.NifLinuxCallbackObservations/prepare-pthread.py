"""Build only the owned host-thread bridge; retain original fixture producers."""
from pathlib import Path
import hashlib,importlib.util,json,os,shutil,subprocess

R=Path(__file__).resolve().parents[3]
H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
J=lambda p:json.loads(Path(p).read_bytes())

def module(path,name):
    spec=importlib.util.spec_from_file_location(name,path);value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value);return value

def write(path,value):
    with path.open('x',encoding='utf-8') as stream:json.dump(value,stream,indent=2);stream.write('\n')


def project_headers552(root, hash_file):
    paths=[root/'tools/Native/Erlang.NifNativeBackend/native'/name for name in ('nif_bridge_api.h','nif_snprintf791.h')]
    return [dict(Path=str(path.resolve()),Bytes=path.stat().st_size,Sha256=hash_file(path)) for path in paths]

def require_project_headers552(proof, root, hash_file):
    expected=project_headers552(root,hash_file);actual=proof['ProjectHeaderInputs']
    assert len(actual)==len(expected) and {row['Path']:(row['Bytes'],row['Sha256']) for row in actual}=={row['Path']:(row['Bytes'],row['Sha256']) for row in expected},'Project bridge headers changed or unbound'
    return expected

def main():
    assert os.uname().machine=='x86_64'
    head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=R,text=True).strip();assert head==os.environ['GITHUB_SHA']
    base=R/'artifacts/linux-callback17';parent=base/'bridge/admission.json'
    assert H(parent)=='0711868322C3A608AE6B2FB249D42D05C4C420A5B842D11532577F0C00A00CD3'
    admission=J(parent);assert admission['SubjectHead']=='f7399755673e510c15ddcdc573a376141c1c5965'
    old=J(base/'bridge/bridge-build-receipt.json');headers=base/'size-native/headers'
    for row in old['HeaderInputs']:
        path=headers/Path(row['Path']).name;assert path.stat().st_size==row['Bytes'] and H(path)==row['Sha256']
    for image in admission['Images']:
        for prefix in ['Library','NativeSource','ErlangSource','Manifest','BuildReceipt']:assert H(image[prefix+'Path'])==image[prefix+'Sha256']
    out=R/'artifacts/linux-pthread-bridge';out.mkdir(exist_ok=False)
    common=module(R/'tools/Native/Erlang.NifEntryOriginalObservations/common.py','pthread_common')
    identity=module(R/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py','pthread_identity')
    owned=module(R/'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py','pthread_owned')
    env=identity.sanitized_environment(os.environ)
    for key in ['GH_ARTIFACT_TOKEN','GITHUB_TOKEN','LD_PRELOAD','LD_AUDIT']:env.pop(key,None)
    gcc,nm,readelf=[shutil.which(name) for name in ['gcc','nm','readelf']];assert gcc and nm and readelf
    sources=[R/'tools/Native/Erlang.NifNativeBackend/native'/name for name in ['nif_bridge_exports.c','nif_bridge_varargs.c','nif_bridge_pthread_control.c']]
    before=[dict(Path=str(path),Sha256=H(path)) for path in sources]
    project_header_rows=project_headers552(R,H)
    bridge=out/'Erlang.NifBridge.so';stages=[]
    def stage(name,command,seconds=90):stages.append(common.stage(owned,identity,command,R,env,out/name,seconds))
    stage('gcc-version',[gcc,'--version'],30)
    stage('bridge-build',[gcc,'-shared','-fPIC','-std=c11','-O2','-Wall','-Wextra','-Werror','-I',headers,*sources,'-o',bridge])
    stage('bridge-defined',[nm,'-D','--defined-only','--format=posix',bridge],30)
    stage('bridge-elf',[readelf,'-h','-Ws','-d',bridge],30)
    required={'pthread_create','pthread_join','pthread_key_create','pthread_key_delete','pthread_getspecific','pthread_setspecific','__cxa_thread_atexit_impl','pthread_exit','nif_bridge_pthread_register','nif_bridge_pthread_provider','nif_bridge_pthread_calls','nif_bridge_register','enif_make_ref','enif_get_map_size'}
    defined={line.split()[0] for line in (out/'bridge-defined.stdout').read_text().splitlines() if line.split()};assert required<=defined
    for row in before:assert H(row['Path'])==row['Sha256']
    assert project_headers552(R,H)==project_header_rows
    receipt=out/'bridge-build-receipt.json'
    write(receipt,dict(Status='PASS_OWNED_LINUX_V4_BRIDGE_BUILD',SubjectHead=head,NativeFixtureProducerHead=admission['SubjectHead'],Variant='NEWLM_BASE_HOST_THREAD_PROVIDERS_CONTROL',ParentAdmissionSha256=H(parent),ParentBridgeReceiptSha256=H(base/'bridge/bridge-build-receipt.json'),HeaderInputs=old['HeaderInputs'],ProjectHeaderInputs=project_header_rows,Sources=before,BridgePath=str(bridge),BridgeSha256=H(bridge),OwnedStages=stages,NativeLoaded=False))
    admission.update(BridgePath=str(bridge),BridgeSha256=H(bridge),BridgeReceiptPath=str(receipt),BridgeReceiptSha256=H(receipt),BridgeSubjectHead=head)
    write(out/'admission.json',admission)
    print(H(out/'admission.json'))

if __name__=='__main__':main()
