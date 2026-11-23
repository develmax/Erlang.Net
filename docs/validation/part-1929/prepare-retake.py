from pathlib import Path
from zipfile import ZipFile
import difflib,hashlib,json,re,sys

stage,j,m,p,source,fix,k,l=map(Path,sys.argv[1:])
read=lambda f:json.loads(f.read_text(encoding='utf-8-sig'))
sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
def save(name,data):
    (stage/name).write_text(data,encoding='utf-8',newline='\n')

parent=read(j/'source-hashes.json')
overlay={row['Path']:row['Sha256'].lower() for row in read(m/'source-identities.json')}
assert set(overlay)==set(read(m/'hydration-receipt.json')['CandidateChangedPaths'])
joined=parent|overlay
for file,digest in joined.items():
    assert sha(source/file)==digest,file
save('held-preimages.json',json.dumps(joined,indent=2)+'\n')
save('held-preimage-receipt.json',json.dumps(dict(ArchivedParent=read(j/'source-freeze.json'),IntegratedPilot=read(m/'pilot-receipt.json'),
                                                Candidate=read(m/'focused-identity.json'),BeforeFiles=len(joined)),indent=2)+'\n')
fixture='tests/Erlang.Tests/Compiler/Beam/BeamNestedListGeneratorChecks.cs'
before=(source/fixture).read_text()
origin=fix/'fixture-declaration-order/nested_gen_entry_probe.erl'
fragment=''.join('        '+line+'\n' if line else '\n' for line in origin.read_text().strip().splitlines())
first='    internal static Task Run()'
last='        Verify("overlap", 1, hasFilter: true);'
assert before.count(first)==before.count(last)==1
after=before.replace(first,'    private const string MixedSource = """\n'+fragment+'        """;\n\n'+first)
after=after.replace(last,last+'\n        _ = Code(CodeGeneration.CompileBeamModule(MixedSource, "nested_gen_entry_probe.erl"));')
patch=''.join(difflib.unified_diff(before.splitlines(keepends=True),after.splitlines(keepends=True),'a/'+fixture,'b/'+fixture))
save('mixed-generator-entry-regression.patch',patch)
def rows(file):
    return set(file.read_text(encoding='utf-8-sig').replace(chr(92),'/').splitlines())
controls=rows(j/'exact-controls.txt')
probe=rows(k/'neutral-tail-modules.txt')|rows(l/'neutral-tail-modules.txt')|controls|{
    'lib/asn1-5.5.2/ebin/asn1ct.beam','lib/wx-2.7/ebin/wxSizer.beam'}
old={row['Beam'].replace(chr(92),'/'):row['Status'] for row in read(p/'full-uncached/otp-corpus.json')['Modules']}
assert probe<=old.keys() and all(old[item]=='Equal' for item in controls)
save('exact-controls.txt','\n'.join(sorted(controls,key=str.casefold))+'\n')
save('pilot-modules.txt','\n'.join(sorted(probe,key=str.casefold))+'\n')
inputs=dict(Cases=266,FastCases=265,TotalCatalog=10061,Modules=len(probe))
save('scope.json',json.dumps(inputs,indent=2)+'\n')
for name in ('focused-test-names.txt','fast-test-names.txt'):
    tests=rows(m/name)
    assert len(tests)==(266 if name[0]=='f' and name.startswith('focused') else 265)
    save(name,(m/name).read_text(encoding='utf-8-sig').strip()+'\n')
save('harness-registrations.txt', '\n'.join(
    (st/'harness-registrations.txt').read_text(encoding='utf-8-sig').strip() for st in (j,m))+'\n')
print(inputs)

