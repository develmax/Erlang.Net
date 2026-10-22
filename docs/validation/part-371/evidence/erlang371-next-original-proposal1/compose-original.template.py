"""Root-only reviewed execution recipe. Agent prepared source; never auto-run."""
import argparse
import hashlib
import importlib.util
import json
import os
import shutil
import subprocess
from pathlib import Path

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def git(worktree,*arguments):
    return subprocess.check_output(['git','-C',str(worktree),*arguments]).decode().strip()

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--root',type=Path,required=True)
    parser.add_argument('--base-head',required=True)
    parser.add_argument('--branch',default='validation/erlang-371-conversions')
    parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--style-dll',type=Path,required=True)
    args=parser.parse_args()
    root=args.root.resolve();out=args.output.resolve()
    packet=root/'artifacts/erlang371-next-original-proposal1'
    worktree=root/'artifacts/proplists-stack-ci-validation1'
    assert sha(packet/'source-seal.json')=='__SEAL__'
    for row in json.loads((packet/'source-seal.json').read_bytes())['Files']:
        path=packet/row['Path'];assert sha(path)==row['Sha256'] and path.stat().st_size==row['Bytes']
    assert args.base_head=='20001454356011e318f2dea6b3d7ed8b8d569f90'
    assert git(worktree,'rev-parse','HEAD')==args.base_head
    assert not git(worktree,'diff','--name-only') and not git(worktree,'diff','--cached','--name-only')
    out.mkdir(parents=True,exist_ok=False)
    shutil.copytree(packet/'tool',out/'tool')
    subprocess.run(['git','-C',str(worktree),'switch','-c',args.branch],check=True)
    rows=json.loads((packet/'copy-plan.json').read_bytes())['Files'];paths=[];changed=[]
    for row in rows:
        source=root/row['Source'];target=worktree/row['Destination']
        assert target.resolve().is_relative_to((worktree/'tools/Erlang.ProplistFamilyObservations').resolve())
        assert sha(source)==row['Sha256'] and (sha(target) if target.is_file() else None)==row['BaseSha256']
        if row['BaseSha256']!=row['Sha256']:changed.append(row['Destination'])
        target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target);paths.append(row['Destination'])
    helper=root/'artifacts/spawn-priority351-preparation/execute-root.py'
    assert sha(helper)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
    spec=importlib.util.spec_from_file_location('binary369_owned',helper);owned=importlib.util.module_from_spec(spec);spec.loader.exec_module(owned)
    observer=root/'artifacts/proplists-protocol366-original1/parse-observer/bin/Debug/net10.0/ParseObserver.dll'
    env=dict(os.environ,DOTNET_PROCESSOR_COUNT='1',MSBUILDDISABLENODEREUSE='1')
    for key in ['GH_TOKEN','GITHUB_TOKEN']:env.pop(key,None)
    owned.owned(['C:/Program Files/dotnet/dotnet.exe',str(observer),str(worktree),str(out/'tool'),str(out/'parse-receipt.json')],out,'parse-six',90,env,root)
    gate=json.loads((out/'parse-receipt.json').read_bytes())
    assert gate['Status']=='PASS_FIXTURE_PARSE_GATE' and len(gate['Fixtures'])==7
    assert all(row.get('Passed') is True and 'Error' not in row for row in gate['Fixtures'])
    (out/'parse-parser-dlls.json').write_text(json.dumps([{'Path':str(path),'Sha256':sha(path)} for path in sorted(observer.parent.glob('*.dll'))],indent=2)+'\n')
    style=args.style_dll.resolve();assert style.is_file()
    style_closure=[{'Path':str(path),'Sha256':sha(path)} for path in sorted(style.parent.glob('*.dll'))]
    owned.owned(['C:/Program Files/dotnet/dotnet.exe',str(style),'--check',str(worktree)],out,'full-tree-style',180,env,root)
    assert all(sha(Path(row['Path']))==row['Sha256'] for row in style_closure)
    (out/'style-dll-closure.json').write_text(json.dumps(style_closure,indent=2)+'\n')
    subprocess.run(['git','-C',str(worktree),'add','--',*paths],check=True)
    staged=git(worktree,'diff','--cached','--name-only').splitlines();assert changed and set(staged)==set(changed)
    assert all(path.startswith('tools/Erlang.ProplistFamilyObservations/') for path in staged)
    for path in paths:
        assert subprocess.check_output(['git','-C',str(worktree),'show',':'+path])==(worktree/path).read_bytes()
    subprocess.run(['git','-C',str(worktree),'commit','-m','test: observe erlang conversion and bounded contracts against pinned OTP'],check=True)
    head=git(worktree,'rev-parse','HEAD');manifest=sha(out/'tool/source-inputs.corrected.json')
    assert manifest=='9A216480984F46A35178F3606B04D515E2639A71DB1A848578BEEB9C2B0871AF'
    (out/'test-only-commit.json').write_text(json.dumps({'Head':head,'Branch':args.branch,'Paths':paths,'Values':751,'Profiles':3755,'SourceInputsSha256':manifest,'ImplementationPublished':False},indent=2)+'\n')
    print(json.dumps({'Head':head,'Branch':args.branch,'Manifest':manifest,'Pushed':False,'Dispatched':False}))

if __name__=='__main__':main()
