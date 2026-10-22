"""Root-only real native fixture build; no original VM/compiler invocation."""
from pathlib import Path
import argparse, hashlib, importlib.util, json, os, shutil

def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()
def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
def save(path,value): path.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')

p=argparse.ArgumentParser()
for name in ['repository','output','compiler','toolchain-environment','admission']:p.add_argument('--'+name,type=Path,required=True)
for name in ['compiler-sha256','toolchain-environment-sha256']:p.add_argument('--'+name,required=True)
a=p.parse_args();root=a.repository.resolve();packet=Path(__file__).resolve().parent;out=a.output.resolve()
assert os.name=='nt' and not out.exists() and out.is_relative_to(root/'artifacts') and not out.is_relative_to(packet)
for row in json.loads((packet/'manifest.json').read_bytes())['Files']:assert sha(packet/row['Path'])==row['Sha256'],row['Path']
for name,digest in json.loads((packet/'baseline-bindings.json').read_bytes()).items():assert sha(root/name)==digest,name
assert sha(a.compiler)==a.compiler_sha256.upper() and sha(a.toolchain_environment)==a.toolchain_environment_sha256.upper()
old=root/'artifacts/library435-ddll-init-prefix-proposal4';profile=json.loads((old/'profile.json').read_bytes())
owned=load('visibility438_owned',root/profile['Bindings'][0]['Path'])
inventory=load('visibility438_inventory',root/profile['BoundedInventory']['Path'])
ad=json.loads(a.admission.read_bytes());assert ad['SourceCommit']==profile['Pin'] and ad['ReleaseTag']=='OTP-29.1.1'
installed=Path(ad['InstalledOtpRoot']);manifest=Path(ad['ExecutionManifestPath']);assert sha(manifest)==ad['ExecutionManifestSha256']
expected={row['Path'].replace('\\','/'):row['Sha256'] for row in json.loads(manifest.read_bytes())};assert len(expected)==6427 and inventory.inventory(installed)==expected
headers=list(installed.glob('erts-*/include/erl_driver.h'));assert len(headers)==1
out.mkdir();source=out/'source';source.mkdir();drivers=out/'drivers';drivers.mkdir()
shutil.copyfile(packet/'native/ddll_visibility438_names.h',source/'ddll_visibility438_names.h')
env=dict(os.environ);env.update(json.loads(a.toolchain_environment.read_bytes()))
stages=[];errors=[]
inputs={str(file.resolve()):sha(file) for file in [a.compiler,a.toolchain_environment,a.admission,manifest,headers[0],packet/'manifest.json',packet/'native/ddll_visibility438_names.h',packet/'native/ddll_init_prefix435.c']}
fixtures=profile['NativeFixtures']+[{'Name':'ddll_cross_ok438','RootFail':0,'ChildFail':0}]
for fixture in fixtures:
    name=fixture['Name'];generated=source/(name+'.c')
    generated.write_text('#define DRIVER_NAME_TEXT "'+name+'"\n#define ROOT_FAIL '+str(fixture['RootFail'])+'\n#define CHILD_FAIL '+str(fixture['ChildFail'])+'\n'+(packet/'native/ddll_init_prefix435.c').read_text(),encoding='utf-8')
    inputs[str(generated.resolve())]=sha(generated)
    before=inventory.inventory(installed)==expected;failure=None
    try:
        assert before
        owned.owned([str(a.compiler),'/nologo','/LD','/MD','/O2','/I'+str(headers[0].parent),'/Fo'+str(drivers)+os.sep,str(generated),'/link','/OUT:'+str(drivers/(name+'.dll'))],out,'compile-'+name,120,env,str(source))
    except Exception as error:failure=repr(error);errors.append({'Fixture':name,'Failure':failure})
    finally:
        after=inventory.inventory(installed)==expected;stable=all(sha(path)==digest for path,digest in inputs.items())
        stages.append({'Fixture':name,'Before':before,'After':after,'InputsStable':stable,'Failure':failure})
        save(out/'stages.json',stages)
        if not after or not stable:raise RuntimeError('Bound source/provider changed')
files=[{'Path':str(file.resolve()),'Bytes':file.stat().st_size,'Sha256':sha(file)} for file in sorted(out.rglob('*')) if file.is_file()]
passed=not errors and len(stages)==4 and all((drivers/(fixture['Name']+'.dll')).is_file() for fixture in fixtures)
save(out/'completion.json',{'Passed':passed,'NativeOnly':True,'NativeFixtures':4,'OriginalVmRerun':False,'OriginalCompilerRun':False,'Sources':inputs,'Stages':stages,'Errors':errors,'Files':files,'Pin':profile['Pin']})
raise SystemExit(0 if passed else 1)
