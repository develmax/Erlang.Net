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
    assert sha(tool/'source-inputs.corrected.json')=='3C80B156E5E5704FFE172B1E23C2662C1F00FE6E7B191C19F9A11760BC4E0325'
    manifest=json.loads((tool/'source-inputs.corrected.json').read_bytes())
    for row in manifest['ToolInputs']:
        path=tool/row['Path'];assert path.is_file() and path.stat().st_size==row['Bytes'] and sha(path)==row['Sha256']
    profile=json.loads((tool/'profile.json').read_bytes());assert profile['SubjectModule']=='ordsets' and len(profile['Fixtures'])==4
    assert sum(len(group['Names']) for group in profile['Fixtures'])==436
    assert profile['ValueAcquisition']=='compiled-monitored-worker-values-and-stacks-v1' and all(group['ValueAcquisition']=='compiled-monitored-worker-values-and-stacks-v1' for group in profile['Fixtures'])
    for row in profile['FixtureSources']:
        assert sha(tool/row['Path'])==row['Sha256']
    harness=out.parent/'main-capture'
    assert not harness.exists();shutil.copytree(root/'artifacts/next-api371-proposal1/sdk-harness',harness)
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
    (out/'main-dll-closure.json').write_text(json.dumps({'Files':rows,'MainSource':True,'PrivateCandidateDllsUsed':False,'SubjectModule':'ordsets'},indent=2)+'\n')
    failure=None
    try:
        owned.owned([dotnet,str(harness/'bin/Debug/net10.0/MainCapture.dll'),str(root),str(tool),str(out/'sdk')],out,'erlang57-capture',1200,env,root)
    except Exception as error:failure=repr(error)
    finally:
        for row in rows:assert sha(root/row['Path'])==row['Sha256']
        (out/'settlement.json').write_text(json.dumps({'Status':'CAPTURED_PENDING_PASSIVE_ACCEPTANCE' if failure is None else 'FAIL_RAW_RETAINED','Failure':failure,'ExpectedValues':436,'ExpectedProfiles':2180,'AllIndependentGroupsAttemptedByHarness':True,'OriginalExecuted':False},indent=2)+'\n')
    if failure:raise RuntimeError(failure)

if __name__=='__main__':main()
