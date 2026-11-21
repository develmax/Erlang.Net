from pathlib import Path
import hashlib,json,os,re,subprocess,sys
u,z,y,v,a,n,i=map(Path,sys.argv[1:])
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def save(p,value): p.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def call(*args,**kw): subprocess.run([str(x) for x in args],check=True,**kw)
req=u/'component-specs.json'
spec=read(req)
for label,folder,file,digest in [
 ('ConstantRemoteOptionalDispatch','constant-remote-dispatch','constant-remote-dispatch','47f1eb44013a4340293dcc9f1d790745f4ce53b36ec732da24911f550ce1bb2f'),
 ('CompleteAttributeCertificateFunctions','attribute-certificate-final-functions','attribute-final-functions','c3523fad1cf4e7cb4c114ab0f7ead61c873e69c649379f562168164bd0f9e079')
]:
    folder=a/folder
    assert sha(folder/'final-receipt.json')==digest
    spec[label]=dict(Root=folder.as_posix(),Receipt='final-receipt.json',Report='candidate.json',
                    Hashes=['StrictReplay','Patches'],Patches=[file+'.patch',file+'-tests.patch'])
save(req,spec)
call(sys.executable,u/'declare-intake.py',u,a,i,i)
tests=[Path(line) for line in (u/'test-patches.txt').read_text().splitlines()]
call(sys.executable,u/'prepare-agent-intake.py',u,z,y,n,*tests)
manifest=u/'pilot-modules.txt'
paths=set(manifest.read_text().splitlines())
new={line.split('"')[1] for line in (v/'part1944-physical-entry-plans.terms').read_text().splitlines()}
assert len(new)==123
all_rows={r['Beam'].replace(chr(92),'/') for r in read(y/'full-uncached/otp-corpus.json')['Modules']}
targets={r for r in all_rows if Path(r).stem in {'mod_actions','local_udp','xmerl_html','PKIXAttributeCertificate-2009'}}
assert len(targets)==4 and new<=all_rows
paths|=new|targets
manifest.write_text('\n'.join(sorted(paths,key=str.casefold))+'\n',encoding='utf-8')
plan=read(u/'scope-plan.json')
plan.update(Modules=len(paths),SavedEntryAffectedModules=len(new),SavedEntryAffectedFunctions=162,RequiredExactTargets=sorted(targets))
save(u/'scope-plan.json',plan)
(u/'all-affected-scope.txt').write_text('\n'.join(sorted(new,key=str.casefold))+'\n',encoding='utf-8')
roles=(u/'harness-registrations.txt').read_text().splitlines()
sync=[]
for patch in tests:
    body=patch.read_text(encoding='utf-8-sig')
    names=re.findall(r'^\+\s*internal static void (\w+)\(',body,re.MULTILINE)
    if not names: continue
    cls=re.search(r'^\+\s*internal static class (\w+)',body,re.MULTILINE)
    assert cls,patch
    for name in names:
        cb=f'Erlang.Tests.{cls[1]}.{name}'
        if any(role.endswith(cb+');') for role in roles): sync.append(cb)
assert len(sync)==5,sync
save(u/'synchronous-callbacks.json',sync)
call(sys.executable,u/'merge-harness-hunks.py',u,n)
queue=u/'intake-replay-order.txt'
adapt={'mapped-record-helper.patch':u/'adapted-mapped-record-helper.patch','strict-list-alias-siblings.patch':u/'adapted-strict-list-alias-siblings.patch'}
assert sha(adapt['mapped-record-helper.patch'])==read(u/'mapped-helper-adaptation.json')['Adapted']
assert sha(adapt['strict-list-alias-siblings.patch'])=='9edaa373211a434d680277f6e96af54c1a8c17aa45611796a5b6578fae820c59'
prod=[Path(line) for line in queue.read_text().splitlines()]
prod=[adapt.get(p.name,p) for p in prod]
queue.write_text('\n'.join(p.as_posix() for p in prod)+'\n',encoding='utf-8')
draft=u/'strict-preflight-production-proof'
draft.mkdir()
for f in [u/'strict-preflight-draft',u/'replayed-draft.txt',u/'strict-preflight-draft.json',*u.glob('patch-*-draft.log')]:
    if f.exists(): f.rename(draft/f.name)
environ=dict(os.environ,PARITY_PREFLIGHT_DRAFT='1')
with (u/'all-patches-draft.log').open('wb') as stream:
    call(sys.executable,u/'strict-preflight.py',u,n,i/'precall-shared-frame-and-typed-lists-1861/artifact-git',*prod,env=environ,stdout=stream,stderr=subprocess.STDOUT)
print(plan)

