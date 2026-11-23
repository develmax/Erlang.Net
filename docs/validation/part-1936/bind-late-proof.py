from pathlib import Path
from zipfile import ZipFile
import difflib,hashlib,json,shutil,sys
stage,previous,proof,work=map(Path,sys.argv[1:])
target=stage/'late-proof-parent-bound'
target.mkdir()
sha=lambda data:hashlib.sha256(data).hexdigest()
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
freeze=read(previous/'source-freeze.json')
assert sha((previous/'source.zip').read_bytes())==freeze['Source']==read(previous/'full-launch.json')['SourceZipSha256']
original=read(proof/'patch-bindings.json')
assert sha((proof/'production.patch').read_bytes())==original['PatchDigests']['production.patch']
assert sha((proof/'test.patch').read_bytes())==original['PatchDigests']['test.patch']
def diff(old,new,name):
    header=f'diff --git a/{name} b/{name}\n'
    if not old:
        header+='new file mode 100644\n'
    return header+''.join(difflib.unified_diff(old,new,'a/'+name if old else '/dev/null','b/'+name))
base='src/Compiler/Erlang.Compiler/Compilation/'
file=base+'BeamModuleCompiler.FinalPhysicalLabelOrder.cs'
with ZipFile(previous/'source.zip') as archive:
    data=archive.read(file)
assert sha(data)==read(previous/'source-hashes.json')[file]==sha((work/file).read_bytes())
old=data.decode()
marker='            CollectRedundantRecordChecks(\n'
assert old.count(marker)==1 and 'CollectExactRegisterEdits(' not in old
new=old.replace(marker,'            CollectExactRegisterEdits(body, edits);\n'+marker)
patch=diff(old.splitlines(keepends=True),new.splitlines(keepends=True),file)
addition='BeamModuleCompiler.ExactRegisterProofs.cs'
new_file=(proof/'layout/src'/addition).read_text(encoding='utf-8')
patch+=diff([],new_file.splitlines(keepends=True),base+addition)
(target/'production.patch').write_text(patch,encoding='utf-8',newline='\n')
for file in ('test.patch','pilot.json'):
    shutil.copy2(proof/file,target/file)
receipt=dict(Proof=original,FrozenReplayParent=freeze['Source'],
             Rationale='Compose the existing final label edits without revisiting register allocation.',
             PatchDigests={name:sha((target/name).read_bytes()) for name in ('production.patch','test.patch')})
(target/'patch-bindings.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(receipt['PatchDigests'])