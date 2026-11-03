"""Bounded N2 acquisition helpers; runtime/native execution is root-owned."""
import hashlib,importlib.util,json,os,subprocess
from pathlib import Path
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
HEAD=None
PROVIDER='tools/Runtime/Erlang.OwnedGuest506Observations/collect.py'
OWNED='tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py'
def select_head(repo,expected):
    global HEAD
    need(len(expected)==40 and all(c in '0123456789abcdef' for c in expected),'Exact lowercase current commit required')
    need(subprocess.check_output(['git','--no-optional-locks','-C',str(repo),'rev-parse','HEAD'],text=True).strip()==expected,'Current committed subject differs')
    HEAD=expected
    return expected

def need(value,why):
    if not value:raise RuntimeError(why)
def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()
def save(path,value):
    with Path(path).open('x',encoding='utf-8') as stream:json.dump(value,stream,indent=2)
def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
def checked(path,digest):
    path=Path(path).resolve();need(path.is_file() and sha(path)==digest.upper(),'Changed admitted input '+str(path));return path
def row(path):
    path=Path(path).resolve();return dict(Path=str(path),Sha256=sha(path),Bytes=path.stat().st_size)
def rows(paths):return [row(path) for path in sorted(set(Path(p).resolve() for p in paths))]
def verify(items):
    for item in items:need(checked(item['Path'],item['Sha256']).stat().st_size==item['Bytes'],'Input size differs')
def guard(repo,packet,plan_hash,filename):
    path=checked(packet/filename,plan_hash);plan=json.loads(path.read_bytes());need(plan['Pin']==PIN and plan['ExpectedHeadMode']=='exact-current-argument' and HEAD is not None,'Wrong source/pin boundary')
    need(subprocess.check_output(['git','--no-optional-locks','-C',str(repo),'rev-parse','HEAD'],text=True).strip()==HEAD,'Current subject commit differs')
    for item in plan['Inputs']:
        root=packet if item['Root']=='Packet' else repo
        path=(root/item['Path']).resolve();need(path.is_relative_to(root),'Escaping reviewed input');checked(path,item['Sha256'])
    return plan
def fresh(repo,path):
    path=Path(path).resolve();need(path.is_relative_to(repo/'artifacts/nif-upgrade693') and not path.exists(),'Fresh owned N2 output required');path.mkdir(parents=True);return path
def environment():
    result=dict(os.environ,PYTHONDONTWRITEBYTECODE='1',ERLC_USE_SERVER='false')
    for key in ['ERL_FLAGS','ERL_AFLAGS','ERL_ZFLAGS','ERL_LIBS','ERL_COMPILER_OPTIONS','ERLC_EMULATOR','ERLC_SERVER_ID','ERLC_CONFIGURATION','ERL_ROOTDIR','LD_PRELOAD','LD_AUDIT','LD_LIBRARY_PATH']:result.pop(key,None)
    return result
def estr(value):return json.dumps(str(value).replace('\\','/'),ensure_ascii=True)
def stages(repo,linux,env):
    if not linux:
        owned=load('upgrade693_owned',repo/OWNED)
        def stage(argv,d,name,timeout=120):return owned.owned([str(a) for a in argv],d,name,timeout,env,str(d))
        return stage
    owned=load('upgrade693_linux_owned',repo/'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py')
    helper=load('upgrade693_linux_stage',repo/'tools/Native/Erlang.NifEntryOriginalObservations/common.py')
    identity=load('upgrade693_identity',repo/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py')
    def stage(argv,d,name,timeout=120):return helper.stage(owned,identity,[str(a) for a in argv],d,env,d/name,timeout)
    return stage
