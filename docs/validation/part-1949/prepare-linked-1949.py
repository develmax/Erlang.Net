from pathlib import Path
import hashlib,json,os,re,subprocess,sys
z,y,x,a,b,q,i,n=map(Path,sys.argv[1:])
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,t):p.write_text(t,encoding='utf-8',newline='\n')
def save(p,d):write(p,json.dumps(d,indent=2)+'\n')
def run(*args,**kw):subprocess.run([str(a) for a in args],check=True,**kw)
assert (y/'full-launch.json').is_file()
assert read(y/'full-launch.json')['SourceZipSha256']==read(y/'source-freeze.json')['Source']==sha(y/'source.zip')
spec=read(z/'component-specs.json')
def add(label,root,receipt,expected,report,patches,selector,**extra):
    assert sha(root/receipt)==expected
    spec[label]=dict(Root=root.as_posix(),Receipt=receipt,Report=report,Patches=patches,Hashes=selector,**extra)
add('EnrollmentFourFieldRecordEncoders',a/'enrollment-status-encoders','final-receipt.json',
   'e6a6eec4a966d1f94cf2f3b1c6948fd408082701b90005db12fd9ccc5415519f','candidate.json',
   ['enrollment-status-encoders.patch','enrollment-status-encoders-tests.patch'],['StrictReplay','Patches'])
add('PrivateBifPayloadAndSelectiveReceive',b/'sudoku-call-receive-1949','sudoku-continuations-receipt.json',
   '439cb4842b4e92b48cab277e5a150133d8e0980382180524c47aa8da94c55b14','candidate-corpus.json',
   ['private-payload-continuations.patch','private-payload-continuations-test.patch'],['replayFiles'],DigestField='sha256')
save(z/'component-specs.json',spec)
run(sys.executable,z/'declare-intake.py',z,a,b,i)
tests=[Path(line) for line in (z/'test-patches.txt').read_text().splitlines()]
run(sys.executable,z/'prepare-agent-intake.py',z,y,x,n,*tests)
module_manifest=z/'pilot-modules.txt'
modules=set(module_manifest.read_text().splitlines())
requirements={item.replace(chr(92),'/') for item in re.findall(r"lib\\[^']+(?:\\ebin)?\\[^']+\.beam",(z/'run-split-focus-and-full.ps1').read_text())}
selected='lib/wx-2.7/doc/examples/sudoku/sudoku.beam'
requirements.add(selected)
all_rows={r['Beam'].replace(chr(92),'/') for r in read(y/'full-uncached/otp-corpus.json')['Modules']}
assert requirements <= all_rows,requirements-all_rows
modules|=requirements
write(module_manifest,'\n'.join(sorted(modules,key=str.casefold))+'\n')
plan=read(z/'scope-plan.json')
plan.update(Modules=len(modules),RequiredExactTargets=sorted(requirements))
save(z/'scope-plan.json',plan)
sync=[]
registered=(z/'harness-registrations.txt').read_text().splitlines()
for patch in tests:
    text=patch.read_text(encoding='utf-8-sig')
    signatures=re.findall(r'^\+\s*internal static void (\w+)\(',text,re.MULTILINE)
    if not signatures:continue
    item=re.search(r'^\+\s*(?:internal )?static class (\w+)',text,re.MULTILINE)
    if not item:
        item=re.search(r'^\+\+\+ b/tests/Erlang.Tests/Compiler/Beam/(\w+)\.cs$',text,re.MULTILINE)
    assert item,patch
    for method in signatures:
        fn=f'Erlang.Tests.{item[1]}.{method}'
        if any(row.endswith(fn+');') for row in registered): sync.append(fn)
assert len(sync)==len(set(sync))
save(z/'synchronous-callbacks.json',sync)
run(sys.executable,z/'merge-harness-hunks.py',z,n)
queue=z/'intake-replay-order.txt'
rows=[Path(s) for s in queue.read_text().splitlines()]
mappings={Path(entry['Original']):Path(entry['Adapted']) for entry in read(z/'megaco-context-adaptation.json')}
for entry in read(z/'megaco-context-adaptation.json'):
    assert sha(Path(entry['Adapted']))==entry['AdapedSHA']
root_patch=q/'production.patch'
assert root_patch in rows
body=root_patch.read_text()
line='+        if (code.Span.Contains(InitYRegisters)\n'
assert body.count(line)==1
adapted=z/'guarded-entry-root-definitions.patch'
write(adapted,body.replace(line,'+        if (!ModuleOptimizationDisabled && code.Span.Contains(InitYRegisters)\n'))
save(z/'entry-option-binding.json',dict(Original=sha(root_patch),Adapted=sha(adapted),
    Reason='Keep the late frame optimization disabled for explicit module optimization opt-out.'))
mappings[root_patch]=adapted
rows=[mappings.get(row,row) for row in rows]
write(queue,'\n'.join(path.as_posix() for path in rows)+'\n')
prior=z/'latest-linked-draft'
prior.mkdir()
for path in [z/'strict-preflight-draft',z/'replayed-draft.txt',z/'strict-preflight-draft.json',*z.glob('patch-*-draft.log')]:
    if path.exists():path.rename(prior/path.name)
gitdir=i/'precall-shared-frame-and-typed-lists-1861'/'artifact-git'
with (z/'all-patches-draft.log').open('wb') as out:
    run(sys.executable,z/'strict-preflight.py',z,n,gitdir,*rows,stdout=out,stderr=subprocess.STDOUT,env={**os.environ,'PARITY_PREFLIGHT_DRAFT':'1'})
print(plan)

