from pathlib import Path
import hashlib,json,sys
parent,stage,live=map(Path,sys.argv[1:])
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
prefix='src/Compiler/Erlang.Compiler/'
old=read(parent/'compiler-source-closure.json')
snapshot=read(stage/'source-hashes.json')
source=read(stage/'source-freeze.json')
full={k.removeprefix(prefix):v for k,v in snapshot.items() if k.startswith(prefix) and k.endswith('.cs')}
inputs=old['Inputs']|full
for relative in source.get('RemovedPaths',[]):
    if relative.startswith(prefix) and relative.endswith('.cs'):
        inputs.pop(relative.removeprefix(prefix))
found={}
for file in (live/prefix).rglob('*.cs'):
    relative=file.relative_to(live/prefix)
    if {'bin','obj'}.isdisjoint(relative.parts):
        found[relative.as_posix()]=sha(file)
assert inputs==found,sorted(set(inputs).symmetric_difference(found))
assert sha(stage/'source.zip')==source['Source']
receipt=dict(Inputs=dict(sorted(inputs.items())),Count=len(inputs),Compiler=source['Compiler'],
             ParentCompilerClosure=sha(parent/'compiler-source-closure.json'),SourceOverlaySHA=source['Source'],
             SourceOverlayInputs=len(full),BaseInputs=len(inputs)-len(full))
(stage/'compiler-source-closure.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(len(inputs))

