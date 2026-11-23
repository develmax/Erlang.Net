from pathlib import Path
import hashlib,json,os,re,subprocess,sys
k,z,b,i,n=map(Path,sys.argv[1:])
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,t):p.write_text(t,encoding='utf-8',newline='\n')
def save(p,d):write(p,json.dumps(d,indent=2)+'\n')
def run(*args,**kw):subprocess.run([str(a) for a in args],check=True,**kw)
sas=b/'sasl-report-handler-1951'
receipt='sasl-report-handler-receipt.json'
assert hashlib.sha256((sas/receipt).read_bytes()).hexdigest()=='5a9682c558e075f6ccb51c60ae3f670f41b68ff3ad9dc98329ac80a94f13ee07'
spec=read(k/'component-specs.json')
spec['DeferredHandlerTupleFields']=dict(Root=sas.as_posix(),Receipt=receipt,Report='final-corpus.json',Patches=['deferred-handler-tuple-fields.patch','deferred-handler-tuple-fields-test.patch'],Hashes=['replayFiles'],DigestField='sha256')
save(k/'component-specs.json',spec)
gate=k/'run-split-focus-and-full.ps1'
body=gate.read_text()
path=r'lib\sasl-4.4\ebin\sasl_report_file_h.beam'
if path not in body:
    anchor=r" 'lib\megaco-4.9.2\ebin\megaco_binary_term_id.beam',"
    assert body.count(anchor)==1
    write(gate,body.replace(anchor,anchor+"\n '"+path+"',"))
run(sys.executable,k/'declare-intake.py',k,k,b,i)
tests=[Path(line) for line in (k/'test-patches.txt').read_text().splitlines()]
run(sys.executable,k/'prepare-agent-intake.py',k,z,z,n,*tests)
manifest=k/'pilot-modules.txt'
modules=set(manifest.read_text().splitlines())
required={p.replace(chr(92),'/') for p in re.findall(r"lib\\[^']+\.beam",gate.read_text())}
full={r['Beam'].replace(chr(92),'/'):r['Status'] for r in read(z/'full-uncached/otp-corpus.json')['Modules']}
assert required <= full.keys(), required-full.keys()
modules|=required
write(manifest,'\n'.join(sorted(modules,key=str.casefold))+'\n')
plan=read(k/'scope-plan.json')
plan.update(Modules=len(modules),RequiredExactTargets=sorted(required),BaselineCounts={s:sum(full[m]==s for m in modules) for s in sorted({full[m] for m in modules})})
save(k/'scope-plan.json',plan)
sync=[]
registered=(k/'harness-registrations.txt').read_text().splitlines()
for patch in tests:
    data=patch.read_text(encoding='utf-8-sig')
    sections=re.split(r'(?=^diff --git )',data,flags=re.MULTILINE)
    for block in sections:
        if not block or '+++ b/tests/Erlang.Tests/Harness/' in block:continue
        signatures=re.findall(r'^\+\s*internal static void (\w+)\(',block,re.MULTILINE)
        if not signatures:continue
        item=re.search(r'^\+\+\+ b/tests/Erlang.Tests/Compiler/Beam/(\w+)\.cs$',block,re.MULTILINE)
        assert item,patch
        for method in signatures:
            fn=f'Erlang.Tests.{item[1]}.{method}'
            if any(row.endswith(fn+');') for row in registered):sync.append(fn)
assert len(sync)==len(set(sync))
save(k/'synchronous-callbacks.json',sync)
run(sys.executable,k/'merge-harness-hunks.py',k,n)
prior=k/'production-only-preflight'
prior.mkdir()
for f in [k/'strict-preflight-draft',k/'replayed-draft.txt',k/'strict-preflight-draft.json',*k.glob('patch-*-draft.log')]:
    if f.exists():f.rename(prior/f.name)
rows=(k/'intake-replay-order.txt').read_text().splitlines()
gitdir=i/'precall-shared-frame-and-typed-lists-1861'/'artifact-git'
run(sys.executable,k/'strict-preflight.py',k,n,gitdir,*rows,env={**os.environ,'PARITY_PREFLIGHT_DRAFT':'1'})
print(plan)

