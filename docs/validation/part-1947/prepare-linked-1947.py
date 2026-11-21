from pathlib import Path
import hashlib,json,os,re,subprocess,sys
x,u,z,b,live,gitdir=map(Path,sys.argv[1:])
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,t):p.write_text(t,encoding='utf-8',newline='\n')
def save(p,value):write(p,json.dumps(value,indent=2)+'\n')
def run(*a,**kw):subprocess.run([str(q) for q in a],check=True,**kw)
parent=read(u/'source-freeze.json')
assert read(u/'full-launch.json')['SourceZipSha256']==parent['Source']==sha(u/'source.zip')
assert (u/'full-uncached/shard-4/modules.txt').is_file()
spec=read(x/'component-specs.json')
spec['ImmediateJoinedTupleDestructuring']['Report']='repaired-scope.json'
save(x/'component-specs.json',spec)
run(sys.executable,x/'declare-intake.py',x,b,b,b)
tests=[Path(line) for line in (x/'test-patches.txt').read_text().splitlines()]
run(sys.executable,x/'prepare-agent-intake.py',x,u,z,live,*tests)
modules=set((x/'pilot-modules.txt').read_text().splitlines())
census=(b/'nested-list-continuations-1947/nested-list-census.terms').read_text()
affected={p[1] for line in census.splitlines() if line.startswith('{"lib/') for p in [line.split('"')]}
all_rows={row['Beam'].replace(chr(92),'/'):row['Status'] for row in read(z/'full-uncached/otp-corpus.json')['Modules']}
assert len(affected)==15 and affected<=all_rows.keys(),len(affected)
requirements={item.replace(chr(92),'/') for item in re.findall(r"lib\\[^']+\\ebin\\[^']+\.beam",(x/'run-split-focus-and-full.ps1').read_text())}
assert 'lib/xmerl-2.2/ebin/xmerl_xsd_re.beam' in requirements
requirements.add('lib/snmp-5.20.5/ebin/snmp_verbosity.beam')
assert requirements<=all_rows.keys(),requirements-all_rows.keys()
modules|=affected|requirements
write(x/'pilot-modules.txt','\n'.join(sorted(modules,key=str.casefold))+'\n')
write(x/'all-affected-scope.txt','\n'.join(sorted(affected,key=str.casefold))+'\n')
plan=read(x/'scope-plan.json')
plan.update(Modules=len(modules),NestedHelperAffectedModules=len(affected),RequiredExactTargets=sorted(requirements))
save(x/'scope-plan.json',plan)
sync=[]
callbacks=(x/'harness-registrations.txt').read_text().splitlines()
for path in tests:
    text=path.read_text(encoding='utf-8-sig')
    signatures=re.findall(r'^\+\s*internal static void (\w+)\(',text,re.MULTILINE)
    if not signatures:continue
    name=re.search(r'^\+\s*(?:internal )?static class (\w+)',text,re.MULTILINE)[1]
    for signature in signatures:
        cb=f'Erlang.Tests.{name}.{signature}'
        if any(line.endswith(cb+');') for line in callbacks):sync.append(cb)
assert len(sync)==5,sync
save(x/'synchronous-callbacks.json',sync)
run(sys.executable,x/'merge-harness-hunks.py',x,live)
queue=x/'intake-replay-order.txt'
rows=[Path(s) for s in queue.read_text().splitlines()]
patched=x/'adapted-cmp-record-witnesses.patch'
assert sha(patched)==read(x/'cmp-formatting-adaptation.json')['Adapted']
rows=[patched if q.name=='cmp-record-witnesses.patch' else q for q in rows]
write(queue,'\n'.join(p.as_posix() for p in rows)+'\n')
previous=x/'third-production-draft'
previous.mkdir()
for path in [x/'strict-preflight-draft',x/'replayed-draft.txt',x/'strict-preflight-draft.json',*x.glob('patch-*-draft.log')]:
    if path.exists():
        path.rename(previous/path.name)
with (x/'all-patches-draft.log').open('wb') as log:
    run(sys.executable,x/'strict-preflight.py',x,live,gitdir,*rows,stdout=log,stderr=subprocess.STDOUT,env={**os.environ,'PARITY_PREFLIGHT_DRAFT':'1'})
print(plan)

