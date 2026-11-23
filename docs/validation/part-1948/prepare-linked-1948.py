from pathlib import Path
import hashlib,json,os,re,subprocess,sys
y,x,u,r,a,i,n=map(Path,sys.argv[1:])
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,t):p.write_text(t,encoding='utf-8',newline='\n')
def save(p,d):write(p,json.dumps(d,indent=2)+'\n')
def run(*a,**kw):subprocess.run([str(v) for v in a],check=True,**kw)
assert (x/'full-launch.json').is_file()
assert read(x/'full-launch.json')['SourceZipSha256']==read(x/'source-freeze.json')['Source']==sha(x/'source.zip')
spec=read(y/'component-specs.json')
def add(label,root,receipt,report,expected,patches,selector,**extra):
    assert sha(root/receipt)==expected,(root,receipt)
    spec[label]=dict(Root=root.as_posix(),Receipt=receipt,Report=report,Patches=patches,Hashes=selector,**extra)
add('CheckedSelectorProjection',i/'checked-selector-projection-1967','receipt.json','final-scope.json',
   '25c1d205c159b6880c72207e0b79a4348e38c53a2505cc61f3f58c4169333b64',
   ['production.patch','test.patch'],['files'],DigestField='sha256')
add('ClearConsumedOutgoingRoots',r,'receipt.json','pilot.json',
   '6ad1016ce4998588e5f0e45f472a83b78def165660b1fed93a15118ac12e59ac',
   ['production.patch','test.patch'],['PatchDigests'])
add('EnrollmentSelectedRecordEncoder',a/'enrollment-open-types','final-receipt.json','candidate.json',
   '046f2c92390dd62ea2f85449bff6e84b851a3f5e7005eecf4f4c9f8d6eb0585a',
   ['enrollment-selected-record.patch','enrollment-selected-record-tests.patch'],['StrictReplay','Patches'])
save(y/'component-specs.json',spec)
run(sys.executable,y/'declare-intake.py',y,a,a,i)
tests=[Path(line) for line in (y/'test-patches.txt').read_text().splitlines()]
run(sys.executable,y/'prepare-agent-intake.py',y,x,u,n,*tests)
module_manifest=y/'pilot-modules.txt'
modules=set(module_manifest.read_text().splitlines())
requirements={item.replace(chr(92),'/') for item in re.findall(r"lib\\[^']+\\ebin\\[^']+\.beam",(y/'run-split-focus-and-full.ps1').read_text())}
profiles={row['Beam'].replace(chr(92),'/') for row in read(x/'full-uncached/otp-corpus.json')['Modules']}
assert 'lib/public_key-1.21.7/ebin/PKIXCMP-2023.beam' in requirements
assert requirements<=profiles,requirements-profiles
modules|=requirements
write(module_manifest,'\n'.join(sorted(modules,key=str.casefold))+'\n')
plan=read(y/'scope-plan.json')
plan.update(Modules=len(modules),RequiredExactTargets=sorted(requirements))
save(y/'scope-plan.json',plan)
sync=[]
registered=(y/'harness-registrations.txt').read_text().splitlines()
for patch in tests:
    body=patch.read_text(encoding='utf-8-sig')
    signatures=re.findall(r'^\+\s*internal static void (\w+)\(',body,re.MULTILINE)
    if not signatures:continue
    name=re.search(r'^\+\s*(?:internal )?static class (\w+)',body,re.MULTILINE)[1]
    for signature in signatures:
        callback=f'Erlang.Tests.{name}.{signature}'
        if any(line.endswith(callback+');') for line in registered):
            sync.append(callback)
assert len(sync)==len(set(sync))
save(y/'synchronous-callbacks.json',sync)
run(sys.executable,y/'merge-harness-hunks.py',y,n)
queue=y/'intake-replay-order.txt'
rows=[Path(s) for s in queue.read_text().splitlines()]
adapt=y/'adapted-cmp-final-schedules.patch'
assert sha(adapt)==read(y/'cmp-final-adaptation.json')['Adapted']
rows=[adapt if q.name=='cmp-final-schedules.patch' else q for q in rows]
write(queue,'\n'.join(p.as_posix() for p in rows)+'\n')
previous=y/'second-production-draft'
previous.mkdir()
for path in [y/'strict-preflight-draft',y/'replayed-draft.txt',y/'strict-preflight-draft.json',*y.glob('patch-*-draft.log')]:
    if path.exists():path.rename(previous/path.name)
gitdir=i/'precall-shared-frame-and-typed-lists-1861/artifact-git'
with (y/'all-patches-draft.log').open('wb') as log:
    run(sys.executable,y/'strict-preflight.py',y,n,gitdir,*rows,stdout=log,stderr=subprocess.STDOUT,env={**os.environ,'PARITY_PREFLIGHT_DRAFT':'1'})
print(plan)

