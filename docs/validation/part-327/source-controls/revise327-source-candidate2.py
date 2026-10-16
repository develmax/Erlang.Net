from pathlib import Path
import json,hashlib,shutil
root=Path(__file__).resolve().parents[1]
old=root/'artifacts/process-error-handler327-candidate1'
out=root/'artifacts/process-error-handler327-candidate2'
assert not out.exists();out.mkdir()
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
def dump(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
assert sha((old/'source-freeze.json').read_bytes())=='B8A672933A44C9DF3C8F74FF7430EC82C79F40EFD292BF88508738028333474E'
shutil.copytree(old/'candidate',out/'candidate')
path='src/Erlang.Compiler/Execution.cs'
p=out/'candidate'/path;b=p.read_bytes()
before=b'''            throw RuntimeCallFrames.Capture(
                exception,
                module,
                function,
                arguments,
                out dispatch
            );'''
after=before.replace(b'arguments,\n                out dispatch',b'arguments')
assert b.count(before)==1
p.write_bytes(b.replace(before,after))
closure=json.loads((old/'source-resource-closure.json').read_bytes())
for row in closure['Files']:
 data=(out/'candidate'/row['Path']).read_bytes()
 if row['Path']==path:row.update(Bytes=len(data),Sha256=sha(data))
 else:assert len(data)==row['Bytes'] and sha(data)==row['Sha256']
dump(out/'source-resource-closure.json',closure)
freeze=json.loads((old/'source-freeze.json').read_bytes())
freeze['CandidateDirectory']=str(out/'candidate')
freeze['FinalSourceClosureSha256']=sha((out/'source-resource-closure.json').read_bytes())
for row in freeze['Files']:
 if row['Path']==path:row.update(Bytes=p.stat().st_size,Sha256=sha(p.read_bytes()))
freeze['ParentCandidateFreezeSha256']=sha((old/'source-freeze.json').read_bytes())
freeze['Revision']='Only unsupported extra out argument removed from RuntimeCallFrames.Capture; other fourteen owned files raw exact. Candidate1 never built.'
freeze['BaselineArchivePath']=str(old/'baseline-source-resources.zip')
freeze['BaselineArchiveSha256']=sha((old/'baseline-source-resources.zip').read_bytes())
shutil.copyfile(old/'SOURCE_NOTE.txt',out/'SOURCE_NOTE.txt')
dump(out/'source-freeze.json',freeze)
dump(out/'unbuilt-candidate1-precondition.json',{'Status':'AUTHOR_SOURCE_PREFLIGHT_UNBUILT_UNSUPPORTED_FIFTH_ARGUMENT','PriorFreezeSha256':sha((old/'source-freeze.json').read_bytes()),'Path':path,'Before':before.decode(),'After':after.decode(),'BuildOrSdkExecuted':False,'RuntimeSemanticResult':None})
print(json.dumps({'FreezeSha256':sha((out/'source-freeze.json').read_bytes()),'ClosureSha256':freeze['FinalSourceClosureSha256']}))
