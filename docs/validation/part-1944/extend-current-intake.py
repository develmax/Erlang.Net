from pathlib import Path
import difflib,hashlib,json,re,sys
stage,source,binary=map(Path,sys.argv[1:])
p=stage/'component-specs-draft.json'
data=json.loads(p.read_text())
projected=binary/'projected-entry-list-frames-1944'
data['ProjectedEntryListFrames']=dict(Root=projected.as_posix(),Receipt='receipt.json',Report='candidate-corpus.json',
    Hashes=['ArtifactHashes'],Patches=['projected-entry-list-frame.patch','projected-entry-list-frame-test.patch'])
(stage/'component-specs.json').write_text(json.dumps(data,indent=2)+'\n')
main='src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.cs'
baseline=(source/main).read_text()
patch=projected/'projected-entry-list-frame.patch'
original=patch.read_text()
sha=lambda file:hashlib.sha256(file.read_bytes()).hexdigest()
assert sha(patch)=='1c6694f55dcaca77710caf0b3e4d8d12fe7d3502758708f1bf14b7ee3a8ad832'
head='        code = FoldHeadTupleReturnFrames(code, labelCount) ?? code;\n'
anchor='        if (OrderFinalPhysicalLabels(code, labelCount) is { } ordered)'
hook='        code = ProjectEntryListDirectlyIntoFrame(code, labelCount) ?? code;\n'
assert baseline.count(head+anchor)==1 and original.count('+'+hook)==1
candidate=baseline.replace(head+anchor,head+hook+anchor)
local=stage/'adapted-projected-entry-list-frames.patch'
part=original.index('diff --git a/src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.ProjectedEntryListFrames.cs')
local.write_text('diff --git a/'+main+' b/'+main+'\n'+''.join(
    difflib.unified_diff(baseline.splitlines(True),candidate.splitlines(True),'a/'+main,'b/'+main))+original[part:],encoding='utf-8',newline='\n')
(stage/'projection-adaptation.json').write_text(json.dumps(dict(Original=sha(patch),Adapted=sha(local),
    Placement='After direct parser returns and before physical label ordering',ComponentBytesUnchanged=True),indent=2)+'\n')

