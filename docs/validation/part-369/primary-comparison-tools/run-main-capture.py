"""Root-only fresh Main capture; no original execution or network."""
import argparse
import hashlib
import importlib.util
import json
import os
import shutil
from pathlib import Path

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--root',type=Path,required=True);parser.add_argument('--output',type=Path,required=True);parser.add_argument('--tool',type=Path,required=True)
    args=parser.parse_args();root=args.root.resolve();out=args.output.resolve();tool=args.tool.resolve()
    assert not out.exists();out.mkdir(parents=True)
    assert sha(tool/'source-inputs.corrected.json')=='A54B3C119311C183D635FD8112507E979FE574031584240D78639BF637A4CBC0'
    manifest=json.loads((tool/'source-inputs.corrected.json').read_bytes())
    for row in manifest['ToolInputs']:
        path=tool/row['Path'];assert path.is_file() and path.stat().st_size==row['Bytes'] and sha(path)==row['Sha256']
    profile=json.loads((tool/'profile.json').read_bytes());assert profile['SubjectModule']=='binary' and len(profile['Fixtures'])==8
    assert sum(len(group['Names']) for group in profile['Fixtures'])==585
    for row in profile['FixtureSources']:
        assert sha(tool/row['Path'])==row['Sha256']
    harness=out.parent/'main-capture'
    assert not harness.exists();shutil.copytree(Path(__file__).resolve().parent/'harness',harness)
    (out/'harness-source-closure.json').write_text(json.dumps({'Files':[{'Path':path.name,'Sha256':sha(path),'Bytes':path.stat().st_size} for path in sorted(harness.glob('*')) if path.is_file()]},indent=2)+'\n')
    helper=root/'artifacts/spawn-priority351-preparation/execute-root.py'
    assert sha(helper)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
    spec=importlib.util.spec_from_file_location('binary369_owned_capture',helper);owned=importlib.util.module_from_spec(spec);spec.loader.exec_module(owned)
    env=dict(os.environ,DOTNET_PROCESSOR_COUNT='1',MSBUILDDISABLENODEREUSE='1')
    for key in ['GH_TOKEN','GITHUB_TOKEN']:env.pop(key,None)
    dotnet='C:/Program Files/dotnet/dotnet.exe'
    owned.owned([dotnet,'build',str(harness/'MainCapture.csproj'),'-m:1','--disable-build-servers','-nr:false','--nologo','-p:UseSharedCompilation=false','-p:NuGetAudit=false'],out,'build',180,env,root)
    rows=[]
    for name,folder in [('Compiler','src'),('Runtime','src'),('Terms','src'),('Serialization','src'),('Differential','tools')]:
        source=root/f'{folder}/Erlang.{name}/bin/Debug/net10.0/Erlang.{name}.dll';copy=harness/'bin/Debug/net10.0'/source.name
        assert sha(source)==sha(copy)
        rows.append({'Path':source.relative_to(root).as_posix(),'Sha256':sha(source),'CopiedTo':copy.relative_to(root).as_posix()})
    (out/'main-dll-closure.json').write_text(json.dumps({'Files':rows,'MainSource':True,'PrivateCandidateDllsUsed':False,'SubjectModule':'binary'},indent=2)+'\n')
    failure=None
    try:
        owned.owned([dotnet,str(harness/'bin/Debug/net10.0/MainCapture.dll'),str(root),str(tool),str(out/'sdk')],out,'binary31-capture',540,env,root)
    except Exception as error:failure=repr(error)
    finally:
        for row in rows:assert sha(root/row['Path'])==row['Sha256']
        (out/'settlement.json').write_text(json.dumps({'Status':'CAPTURED_PENDING_PASSIVE_ACCEPTANCE' if failure is None else 'FAIL_RAW_RETAINED','Failure':failure,'ExpectedValues':585,'ExpectedProfiles':2925,'AllIndependentGroupsAttemptedByHarness':True,'OriginalExecuted':False},indent=2)+'\n')
    if failure:raise RuntimeError(failure)

if __name__=='__main__':main()
