from pathlib import Path
import hashlib,json,os,re,subprocess,sys
k,z,prior,b,i,n=map(Path,sys.argv[1:])
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,t):p.write_text(t,encoding='utf-8',newline='\n')
def save(p,d):write(p,json.dumps(d,indent=2)+'\n')
def run(*args,**kw):subprocess.run([str(a) for a in args],check=True,**kw)
gate=k/'run-split-focus-and-full.ps1'
run(sys.executable,k/'declare-intake.py',k,k,b,i)
tests=[Path(line) for line in (k/'test-patches.txt').read_text().splitlines()]
run(sys.executable,k/'prepare-agent-intake.py',k,z,prior,n,*tests)
manifest=k/'pilot-modules.txt'
modules=set(manifest.read_text().splitlines())
required={p.replace(chr(92),'/') for p in re.findall(r"lib\\[^']+\.beam",gate.read_text())}
full={r['Beam'].replace(chr(92),'/'):r['Status'] for r in read(prior/'full-uncached/otp-corpus.json')['Modules']}
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
        item=re.search(r'^\+\+\+ b/tests/Erlang.Tests/(?:[A-Za-z0-9_]+/)*(\w+)\.cs$',block,re.MULTILINE)
        assert item,patch
        for method in signatures:
            fn=f'Erlang.Tests.{item[1]}.{method}'
            if any(row.endswith(fn+');') for row in registered):sync.append(fn)
assert len(sync)==len(set(sync))
save(k/'synchronous-callbacks.json',sync)
run(sys.executable,k/'merge-harness-hunks.py',k,n)
prior=k/'production-only-latest'
prior.mkdir()
for f in [k/'strict-preflight-draft',k/'replayed-draft.txt',k/'strict-preflight-draft.json',*k.glob('patch-*-draft.log')]:
    if f.exists():f.rename(prior/f.name)
queue=k/'intake-replay-order.txt'
rows=queue.read_text().splitlines()
context=read(k/'checked-pair-context-adaptation.json')
for name in ('Original','Adapted'):
    assert hashlib.sha256(Path(context[name]).read_bytes()).hexdigest()==context[name+'SHA']
assert rows.count(context['Original'])==1
rows=[context['Adapted'] if row==context['Original'] else row for row in rows]
write(queue,'\n'.join(rows)+'\n')
gitdir=i/'precall-shared-frame-and-typed-lists-1861'/'artifact-git'
run(sys.executable,k/'strict-preflight.py',k,n,gitdir,*rows,env={**os.environ,'PARITY_PREFLIGHT_DRAFT':'1'})
print(plan)

