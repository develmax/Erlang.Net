"""Seal exact committed-tool inputs; passive source hashing only."""
import argparse,hashlib,json
from pathlib import Path
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
REL='tools/Native/Erlang.NifPrivateData557Observations'
EXCLUDE={'bin','obj','__pycache__'}
SUFFIX={'.cs','.csproj','.props','.targets','.py','.c','.h','.erl'}

def sha(p,mode='Raw'):
    data=p.read_bytes()
    if mode=='TextLF':data=data.replace(b'\r\n',b'\n')
    return hashlib.sha256(data).hexdigest().upper()

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--repository',type=Path,default=Path(__file__).resolve().parents[3])
    ap.add_argument('--tool-directory',type=Path)
    ap.add_argument('--overlay-root',type=Path)
    a=ap.parse_args();root=a.repository.resolve();tool=a.tool_directory.resolve() if a.tool_directory else root/REL
    def actual(relative):
        proposed=a.overlay_root/relative if a.overlay_root else None
        return proposed if proposed and proposed.is_file() else root/relative
    rows=[]
    for path in sorted(tool.rglob('*')):
        if path.is_file() and path.name!='source-plan.json' and '__pycache__' not in path.parts:
            rows.append({'Scope':'Tool','Path':path.relative_to(tool).as_posix(),'HashMode':'Raw','Sha256':sha(path)})
    inputs=set()
    groups=['src/Runtime/Erlang.Runtime.Core','src/Runtime/Erlang.Terms','src/Runtime/Erlang.Runtime','src/Compiler/Erlang.Compiler','tools/Native/Erlang.NifNativeBackend','tools/Native/Erlang.DdllFormatterOriginalObservations/scripts','tools/Runtime/Erlang.OwnedGuest506Observations']
    for group in groups:
        for path in (root/group).rglob('*'):
            if path.is_file() and path.suffix in SUFFIX and not any(x in EXCLUDE for x in path.parts):inputs.add(path.relative_to(root).as_posix())
    inputs.update('tools/Validation/Erlang.Differential/'+name for name in ['GeneratedModuleCompiler.cs','GeneratedModuleArtifact.cs','GeneratedModuleReferences.cs','GeneratedModuleMetadata.cs','OracleDiagnostics.cs'])
    inputs.update(['.github/workflows/validate.yml','.gitattributes','Directory.Build.props','global.json','src/Runtime/Erlang.Runtime.Core/runtimes/win-x64/native/Erlang.Nif.WindowsVariadic.dll','tools/Native/Erlang.NifVariadic526Observations/build-native.py','tools/Native/Erlang.NifVariadic526Observations/seal-source-plan.py'])
    for name in sorted(inputs):
        path=actual(name);mode='Raw' if path.suffix in {'.cs','.dll'} else 'TextLF'
        rows.append({'Scope':'Repository','Path':name,'HashMode':mode,'Sha256':sha(path,mode)})
    headers=json.loads((root/'tools/Native/Erlang.NifMapFamily504Observations/source-plan.json').read_bytes())['Headers']
    plan={'PinnedCommit':PIN,'Platform':'windows-x64','NativeSourcePath':REL+'/fixtures/load_private557.c','ErlangSourcePath':REL+'/fixtures/load_private557.erl','NativeSourceSha256':sha(tool/'fixtures/load_private557.c'),'ErlangSourceSha256':sha(tool/'fixtures/load_private557.erl'),'BuildScriptSha256':sha(tool/'build-native.py'),'Headers':headers,'Inputs':rows,'ExecutionApproved':False,'OriginalCollected':False,'WholeContractsClosed':0}
    output=tool/'source-plan.json';output.write_bytes((json.dumps(plan,indent=2)+'\n').encode());print(sha(output))

if __name__=='__main__':main()
