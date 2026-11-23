from pathlib import Path
import hashlib,json,sys
stage,j,m,p,source,fix=map(Path,sys.argv[1:])
read=lambda f:json.loads(f.read_bytes())
sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
old=read(p/'source-hashes.json')
manifest=read(stage/'held-preimages.json')
rows=[read(j/'hydration-receipt.json'),read(m/'hydration-receipt.json')]
changes=rows[0]['CandidateChangedPaths']|rows[1]['CandidateChangedPaths']
changed={
 'src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs',
 'tests/Erlang.Tests/Compiler/Beam/BeamNestedListGeneratorChecks.cs'
}
assert changed<=changes.keys()
for path,digest in manifest.items():
    if path not in changed:
        assert sha(source/path)==digest,path
for path,digest in old.items():
    if path not in changes:
        assert sha(source/path)==digest,path
patches=rows[0]['PatchSources']|rows[1]['PatchSources']
for f in (fix/'plan-nested-generator-labels.patch',stage/'mixed-generator-entry-regression.patch'):
    patches[str(f)]=sha(f)
receipt=dict(Parent=read(p/'source-freeze.json'),CandidateChangedPaths={f:sha(source/f) for f in changes},
             CandidateRemovedPaths={},PatchSources=patches)
(stage/'hydration-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(len(changes),'changed source files bound against accepted Part 1927')

