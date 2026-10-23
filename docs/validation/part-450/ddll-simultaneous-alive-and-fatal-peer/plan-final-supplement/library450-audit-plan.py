from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
paths=['docs/WORK_PLAN.md','docs/remaining-work/INDEX.md','docs/remaining-work/architecture-and-compatibility-queue.md']
texts={n:(r/n).read_text(encoding='utf-8') for n in paths};ledger=json.loads((r/'docs/mfa-compatibility.json').read_bytes());old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json']));assert ledger==old
known=[x for x in ledger['Entries'] if x['Status']=='Known differences'];assert len(known)==31
queue=texts[paths[-1]]
for row in known:assert ('`%s:%s/%s`'%(row['Module'],row['Function'],row['Arity'])) in queue
for name,text in texts.items():
 for term in ['559','708','C#','BEAM','current','source','DLL']:assert term in text,(name,term)
assert '## P1 — full agreed library API acceptance' in queue and 'Zero known31 alone is neither full-library acceptance' in queue
assert 'Full agreed library API acceptance' in texts[paths[0]] or '**P1: full agreed library API acceptance' in texts[paths[0]]
assert 'Fully validate all agreed library API' in texts[paths[1]]
assert not subprocess.check_output(['git','diff','--name-only','HEAD','--','src','build','original'],cwd=r).decode().strip()
assert 'complete matrix:' in queue and '## A4 — mandatory future scheduler architecture' in queue
assert 'A19FB1A4E5FF8EC994FA09F735BE64B30AA013ED61BD3FAADF700C3E2B4D463B' in queue
receipt={'Passed':True,'DocumentationOnly':True,'Sections':['P0','P1','A1','A2','A3'],'KnownLedgerRows':31,'NeedsVerification':559,'NewResolvedDifferences':0,'MfaLedgerUnchanged':True,'NoRuntimeProjectOrSnapshotImplementation':True,'FullAgreedLibraryScopeBeyond708Queued':True,'NoNewSuitesByPlanUpdate':True,'Paths':[{'Path':p,'Sha256':sha(r/p)} for p in paths]}
(r/'artifacts/library450-plan-supplement-final.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
