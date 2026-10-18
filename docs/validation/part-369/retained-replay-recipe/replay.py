from pathlib import Path
import argparse,hashlib,importlib.util,json,os,shutil,sys

root=Path(__file__).resolve().parents[2]
base=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def save(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
ap=argparse.ArgumentParser();ap.add_argument('--output',type=Path,required=True);ap.add_argument('--only',nargs='*');a=ap.parse_args()
out=a.output.resolve();assert out.is_relative_to((root/'artifacts').resolve()) and not out.exists();out.mkdir(parents=True)
manifest=json.loads((base/'inputs.json').read_bytes())
helper=root/manifest['OwnedHelper'];assert sha(helper)==manifest['OwnedHelperSha256']
spec=importlib.util.spec_from_file_location('retained369_owned',helper);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
env={k:v for k,v in os.environ.items() if k.upper() not in {'GH_TOKEN','GITHUB_TOKEN','GITHUB_API_TOKEN'}}
env['DOTNET_PROCESSOR_COUNT']='1'
closure=[]
for name,folder in [('Compiler','src'),('Runtime','src'),('Terms','src'),('Serialization','src'),('Differential','tools')]:
    p=root/f'{folder}/Erlang.{name}/bin/Debug/net10.0/Erlang.{name}.dll'
    closure.append({'Path':p.relative_to(root).as_posix(),'Sha256':sha(p)})
save(out/'initial-main-dlls.json',{'MainSource':True,'PrivateCandidateDllsUsed':False,'Files':closure})
stages=manifest['Stages'];assert not a.only or set(a.only)<={s['Name'] for s in stages}
results=[]
for stage in stages:
    name=stage['Name']
    if a.only and name not in a.only:continue
    stageout=out/name;stageout.mkdir()
    try:
        for row in stage['NativeFiles']:assert sha(root/row['Path'])==row['Sha256']
        originaltool=root/stage['Tool'];assert sha(originaltool/'profile.json')==stage['ProfileSha256']
        tool=stageout/'tool';tool.mkdir();shutil.copyfile(originaltool/'profile.json',tool/'profile.json')
        for row in stage['Sources']:
            source=(originaltool if row['Location']=='tool' else root)/row['Path'];assert sha(source)==row['Sha256']
            if row['Location']=='tool':
                target=tool/row['Path'];target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
        oldharness=root/stage['Harness']
        for row in stage['HarnessSource']:assert sha(root/row['Path'])==row['Sha256']
        originalbin=oldharness/'bin/Debug/net10.0';assert sha(originalbin/'MainCapture.dll')==stage['HarnessSha256']
        binary=stageout/'bin';shutil.copytree(originalbin,binary)
        copied=[]
        for row in closure:
            source=root/row['Path'];assert sha(source)==row['Sha256']
            target=binary/source.name;shutil.copyfile(source,target);assert sha(target)==row['Sha256']
            copied.append(dict(row,CopiedTo=target.relative_to(root).as_posix()))
        save(stageout/'main-dll-closure.json',{'MainSource':True,'PrivateCandidateDllsUsed':False,'Files':copied,'HarnessSha256':sha(binary/'MainCapture.dll')})
        receipt=m.owned(['C:/Program Files/dotnet/dotnet.exe',str(binary/'MainCapture.dll'),str(root),str(tool),str(stageout/'sdk')],stageout,'capture',stage['Timeout'],env,root)
        captured=json.loads((stageout/'sdk/completion.json').read_bytes());assert captured['FailedGroups']==0
        assert len(captured['Groups'])==len(stage['Fixtures'])
        results.append({'Name':name,'Passed':True,'Receipt':receipt,'Comparison':'PENDING_WHOLE_FIELD_PASSIVE_REVIEW'})
    except Exception as e:
        results.append({'Name':name,'Passed':False,'Error':str(e)})
    save(out/'settlement.json',{'Stages':results,'Acceptance':False,'OriginalExecuted':False,'BuildExecuted':False})
unchanged=all(sha(root/r['Path'])==r['Sha256'] for r in closure)
save(out/'completion.json',{'Status':'CAPTURED_PENDING_PASSIVE_COMPARISON' if unchanged and all(r['Passed'] for r in results) else 'FAILED_RAW_RETAINED','Stages':results,'MainDllClosureUnchanged':unchanged,'OriginalExecuted':False,'BuildExecuted':False,'All76Claimed':False})
sys.exit(0 if unchanged and all(r['Passed'] for r in results) else 1)
