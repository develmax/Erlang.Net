"""Artifact proposal only; root reviews and executes. No builds/tests/git writes/network."""
from pathlib import Path
import hashlib,json,re,subprocess
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-292'
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());git=lambda *x:subprocess.check_output(['git',*x],cwd=r)
parent='9c5b8a809b2a6bcd76965cdeae2d9dbd2ec873e2'
assert git('rev-parse','HEAD').decode().strip()==parent
assert not git('diff','HEAD','--name-only','--','src','tests','tools','Erlang.Net.slnx','docs/supported-mfas.json').strip(), 'Source must remain published291'
assert not o.exists(), 'Refuse incomplete/repeated archive'
freeze=read(r/'docs/validation/part-291/root-artifacts/root-part291-final-source-freeze.json');assert len(freeze['Files'])==21
for x in freeze['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and sha(b)==x['Sha256'],x['Path']
closed='1246C10CA856935B004FA7ACB7E177E5E6A9B6F1250E32EBB893BE9739B6F0E1';assert sha((r/'docs/changelog/0043.md').read_bytes())==closed
registry=read(r/'docs/supported-mfas.json');assert len(registry)==502 and sum(x['Module']=='erlang' for x in registry)==344
ci=read(a/'pack291-ci-38698195062-independent-guard.json');native=read(a/'pack291-original-38698216243-independent-guard.json')
assert ci['Status']=='PASS_EXACT_HEAD291_CI6599_FULL' and ci['Head']==parent and ci['Passed']==6599 and ci['Failed']==0 and ci['OrderedNamesExact'] and ci['Historical6588OrderedNamesExact'] and len(ci['Integration'])==11 and ci['RegisteredMfas']==502
assert native['Status']=='PASS_EXACT_HEAD291_ORIGINAL5846_AND_FD8' and native['Head']==parent and native['Passed']==5846 and native['Failed']==0 and native['FdPassed']==8
assert (native['Expressions'],native['CompiledModules'],native['Diagnostics'])==(778,4903,165)
assert native['Historical4895ModuleRowsExact'] and native['Historical778Expressions163DiagnosticsSixExclusionsAndContextExact'] and native['NewEightCompiledAndTwoTypedDiagnosticsExact']
scope_path=r/'docs/module-readiness-scope.json';scope=read(scope_path);assert scope['ComparisonTestReport']=='validation/part-291/tests.json'
ready=read(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==6599 and ready['RegisteredMfas']==502 and ready['RegisteredMfaDelta']==0
components={x['Id']:x for x in scope['Components']};assert all(x in components for x in ['Compiler','Runtime','NifFixtureHost'])
note='Part292: independently verified published291 exact-head CI6599/full and original OTP5846/5846 plus fd8/8 close all eight new source NIF declaration cases and two typed diagnostics. All historical4895 module rows,778 expressions,163 diagnostics and six exclusions/context remain exact. No C# source changes or repeated local full run; published6599 readiness reused. Default502 and erlang344/351 (98.0% API presence) unchanged. General load_nif, warnings/BEAM declarations, atomic replacement/purge and all-writer pressure/EXITING remain unfinished.'
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
for name in metadata:
 b=(r/name).read_bytes();assert b'Part292:' not in b and re.search(br'\r?\n\r?\n',b)
assert not any('Part292:' in x for c in components.values() for x in c.get('Implemented',[]))
volume=r/'docs/changelog/0044.md';entry=(a/'part292-changelog-draft.md').read_text(encoding='utf-8-sig');combined=volume.read_bytes()+b'\r\n'+entry.replace('\r\n','\n').replace('\n','\r\n').encode()
assert b'## Part 292' not in volume.read_bytes() and len(combined)<=32768 and len(combined.splitlines())<=400
index=r/'CHANGELOG.md';index_before=index.read_bytes();old='parts289–291 (active)'.encode();assert index_before.count(old)==1
attrs=r/'.gitattributes';assert b'docs/validation/part-292/** binary' not in attrs.read_bytes()
copies={}
def select(src,dst,expected=None,size=None):
 src=src.resolve();dst=dst.resolve();assert dst.is_relative_to(o) and src.is_file()
 b=src.read_bytes();assert 'LICENSE.local.txt' not in src.parts and '__pycache__' not in src.parts
 if expected:assert sha(b)==expected
 if size is not None:assert len(b)==size
 if dst in copies:assert copies[dst][1]==sha(b)
 copies[dst]=(src,sha(b))
mp=a/'pack291-detached-staging-manifest.json';manifest=read(mp);assert len(manifest)==28
for x in manifest:select(r/x['SourcePath'],r/x['Destination'],x['Sha256'],x['Bytes'])
select(mp,o/'staging-manifest.json')
for name in ['part292-finalize-proposal.py','part292-stage-guard-proposal.py','part292-changelog-draft.md']:
 select(a/name,o/'root-artifacts'/name)
for name in ['module-readiness.json','MODULE_READINESS.md']:
 select(r/'docs'/name,o/'reused-published291-readiness'/name)
select(r/'docs/validation/part-291/root-artifacts/root-part291-final-source-freeze.json',o/'published291-source-freeze.json')
for dst,(src,digest) in copies.items():
 b=src.read_bytes();assert sha(b)==digest;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(b)
volume.write_bytes(combined);index.write_bytes(index_before.replace(old,'parts289–292 (active)'.encode()))
for name in metadata:
 q=r/name;b=q.read_bytes();cut=re.search(br'\r?\n\r?\n',b).end();nl=b'\r\n' if b'\r\n' in b else b'\n';q.write_bytes(b[:cut]+note.encode()+nl+nl+b[cut:])
for key in ['Compiler','Runtime']:components[key].setdefault('Implemented',[]).append(note)
components['NifFixtureHost'].setdefault('Implemented',[]).append('Part292: exact published291 source-declaration contracts independently verified against pinned original OTP (eight compiled cases and two diagnostics). Prior fixture7/control2 remains separate; no generic SDK load_nif or native unload comparison. No new tool/Main run in this proof checkpoint.')
scope_path.write_text(json.dumps(scope,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
attrs.write_bytes(attrs.read_bytes()+b'docs/validation/part-292/** binary\n')
assert not git('diff','HEAD','--name-only','--','src','tests','tools','Erlang.Net.slnx','docs/supported-mfas.json').strip()
assert sha((r/'docs/changelog/0043.md').read_bytes())==closed
receipt={'Status':'PUBLISHED291_EXACT_HEAD_CI6599_AND_ORIGINAL5846_FD8_VERIFIED_NO_NEW_SOURCE_OR_LOCAL_RUN','ParentHead':parent,'SourcePaths':0,'Published291SourcePathsVerified':21,'HistoricalMainPassed':6599,'CI291Passed':6599,'Original291Passed':5846,'Original291FdPassed':8,'Original291Expressions':778,'Original291Modules':4903,'Original291Diagnostics':165,'NewOriginalCasesVerified':10,'Historical4895RowsExact':True,'Registry':502,'ErlangPresence':344,'ErlangReference':351,'IntegrationAndPackagesPassed':True,'LocalMainFullRepeated':False,'NewNativeDispatch':False,'SourceChanges':False,'ReadinessReusedFromPublished291':True,'CI292Verified':False,'WholeCompatibility':False,'Closed0043Sha256':closed}
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':sha(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file()]
(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'ProofFiles':len(receipt['Files']),'SourcePaths':0,'CI291Passed':6599,'Original291Passed':5846,'FdPassed':8,'ChangelogBytes':len(combined)}))
