from pathlib import Path
from zipfile import ZipFile
import difflib,hashlib,json,re,sys

stage,source,prior,p,aliases,returns=map(Path,sys.argv[1:])
read=lambda f:json.loads(f.read_text(encoding='utf-8-sig'))
sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
tests=[(aliases,'BeamProtectedEntryAliasChecks'),(returns,'BeamProtectedShortReturnChecks')]
preimages=read(prior/'source-hashes.json')
freeze=read(prior/'source-freeze.json')
assert len(preimages)==freeze['Paths']
for file,digest in preimages.items():
    assert sha(source/file)==digest,file
assert sha(prior/'source.zip')==freeze['Source']==read(prior/'full-launch.json')['SourceZipSha256']
harness='tests/Erlang.Tests/Harness/Program.cs'
with ZipFile(prior/'source.zip') as archive:
    before=archive.read(harness).decode()
assert before.encode()==(source/harness).read_bytes()
anchor='Test(Erlang.Tests.BeamProtectedProjectionChecks.TestName, Erlang.Tests.BeamProtectedProjectionChecks.Run);'
assert before.count(anchor)==1
patch=[]
registrations=[]
for folder,test in tests:
    path=f'tests/Erlang.Tests/Compiler/Beam/{test}.cs'
    assert not (source/path).exists()
    code=(folder/'fixture-proof'/f'{test}.cs').read_text().rstrip()+'\n'
    patch.extend(difflib.unified_diff([],code.splitlines(keepends=True),'/dev/null','b/'+path))
    registrations.append(f'Test(Erlang.Tests.{test}.TestName, Erlang.Tests.{test}.Run);')
after=before.replace(anchor,anchor+'\n'+'\n'.join(registrations))
patch.extend(difflib.unified_diff(before.splitlines(keepends=True),after.splitlines(keepends=True),'a/'+harness,'b/'+harness))
def store(name,data):
    (stage/name).write_text(data,encoding='utf-8',newline='\n')
store('harness-registrations.txt','\n'.join(registrations)+'\n')
store('protected-entry-return-tests.patch',''.join(patch))
rows=lambda path:set(path.read_text(encoding='utf-8-sig').replace(chr(92),'/').splitlines())
exact=rows(prior/'exact-controls.txt')
known={row['Beam'].replace(chr(92),'/'):row['Status'] for row in read(p/'full-uncached/otp-corpus.json')['Modules']}
assert all(known[item]=='Equal' for item in exact)
scope=rows(aliases/'neutral-tail-modules.txt')|rows(returns/'neutral-tail-modules.txt')|exact|{
    'lib/asn1-5.5.2/ebin/asn1ct.beam','lib/wx-2.7/ebin/wxSizer.beam'}
assert scope<=known.keys()
store('pilot-modules.txt','\n'.join(sorted(scope,key=str.casefold))+'\n')
store('exact-controls.txt','\n'.join(sorted(exact,key=str.casefold))+'\n')
manifest={
    'PilotModules':len(scope),
    'PriorEqualControls':len(exact),
    'ProtectedEntryCensus':(aliases/'census.stdout.log').read_text().strip(),
    'ProtectedShortReturnCensus':(returns/'census.stdout.log').read_text().strip(),
    'ParentSource':freeze['Source'],
    'WholeGroupGate':'The complete uncached release checks every affected image after the representative scope.',
}
store('scope-plan.json',json.dumps(manifest,indent=2)+'\n')
print(manifest)

