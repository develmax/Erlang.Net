from pathlib import Path
import hashlib,json,re,sys
stage,prior,verified,source,*tests=map(Path,sys.argv[1:])
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def digest(f):return hashlib.sha256(f.read_bytes()).hexdigest()
def save(name,text):
    (stage/name).write_text(text,encoding='utf-8',newline='\n')
parent=read(prior/'source-freeze.json')
paths=read(prior/'source-hashes.json')
assert len(paths)==parent['Paths']
assert digest(prior/'source.zip')==parent['Source']==read(prior/'full-launch.json')['SourceZipSha256']
for path,expected in paths.items():
    assert digest(source/path)==expected,path

plan=read(stage/'agent-intake.json')
modules=set()
for entry in plan:
    records=read(Path(entry['Report']))['Modules']
    modules.update(row['Beam'].replace(chr(92),'/') for row in records)
    for name,expected in entry['PatchDigests'].items():
        assert digest(Path(name)).lower()==expected.lower(),name

controls=set((prior/'exact-controls.txt').read_text(encoding='utf-8-sig').replace(chr(92),'/').splitlines())
all_rows={row['Beam'].replace(chr(92),'/'):row['Status'] for row in read(verified/'full-uncached/otp-corpus.json')['Modules']}
assert all(all_rows[row]=='Equal' for row in controls)
scope=modules|controls|{'lib/asn1-5.5.2/ebin/asn1ct.beam','lib/stdlib-8.1/ebin/ordsets.beam'}
assert scope<=all_rows.keys(),scope-all_rows.keys()
save('pilot-modules.txt','\n'.join(sorted(scope,key=str.casefold))+'\n')
save('exact-controls.txt','\n'.join(sorted(controls|{'lib/stdlib-8.1/ebin/ordsets.beam'},key=str.casefold))+'\n')
old=set((prior/'focused-test-names.txt').read_text(encoding='utf-8-sig').splitlines())
registrations=[]
names=[]
for patch in tests:
    data=patch.read_text(encoding='utf-8-sig')
    registrations.extend(re.findall(r'^\+(Test\(Erlang\.Tests\.[^\r\n]+\);)\s*$',data,re.MULTILINE))
    names.extend(re.findall(r'^\+\s*internal const string (?:[A-Za-z]*TestName) = "([^"]+)";',
                            data,re.MULTILINE))
assert len(registrations)==len(set(registrations))==len(names)==len(set(names)),(registrations,names)
assert not old.intersection(names)
save('harness-registrations.txt','\n'.join(registrations)+'\n')
scope_plan=dict(Modules=len(scope),AllOriginalIndependentScope=len(modules),PreviousEqualControls=len(controls),
                NewlyRegistered=len(names),PriorSource=parent['Source'],IndependentGroups=len(plan))
save('scope-plan.json',json.dumps(scope_plan,indent=2)+'\n')
print(scope_plan)




