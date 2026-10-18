from pathlib import Path
import hashlib,json,re,difflib
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def dump(p,o):p.write_text(json.dumps(o,indent=2)+'\n',encoding='utf-8')
def tokens(p):
 s=p.read_text(encoding='utf-8-sig')
 pattern=r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[A-Za-z_][A-Za-z_0-9]*|[0-9]+|[^\s]'
 return [t for t in re.findall(pattern,s) if not t.startswith('//') and not t.startswith('/*')]
proposals=[
 'next-api371-proposal1','ordsets371-full-proposal1','ordsets371-lists-dispatch-proposal1',
 'erlang371-map-key-order-proposal1','erlang371-lists-unique-source-proposal1',
 'erlang371-numeric-differences-proposal1','erlang371-operator-source-location-proposal1',
]
selected={}
for name in proposals:
 plan=json.loads((ROOT/'artifacts'/name/'copy-plan.json').read_text())
 for r in plan['Files']:
  dest=r['Destination']
  if dest.startswith('src/'):
   selected[dest]=r['Source']
checks=[]
for dest,source in sorted(selected.items()):
 p=ROOT/dest;q=ROOT/source
 checks.append({'Path':dest,'MainSha256':sha(p),'ProposalPath':source,'ProposalSha256':sha(q),'TokenEquivalent':tokens(p)==tokens(q)})
dump(OUT/'final-source-proposal-equivalence.json',checks)
ledgerpath=ROOT/'docs/mfa-compatibility.json'
ledger=json.loads(ledgerpath.read_text())
existing={r['Path']:r['Sha256'] for r in ledger['VerifiedSourceFiles']}
assert len(existing)==462
changes=[{'Path':p,'Before':h,'After':sha(ROOT/p)} for p,h in existing.items() if sha(ROOT/p)!=h]
additional=set(selected)
basefour=json.loads((ROOT/'artifacts/erlang371-contract-audit1/source-bindings.json').read_text())
additional.update(r['Path'] for r in basefour['Files'])
for p in ['src/Erlang.Runtime/ProcessContext.GeneratedFrames.cs','src/Erlang.Runtime/NativeBuiltinCatalog.cs']:
 additional.add(p)
newrows=[{'Path':p,'Sha256':sha(ROOT/p)} for p in sorted(additional) if p not in existing]
guardrows=[{'Path':r['Path'],'Sha256':sha(ROOT/r['Path'])} for r in ledger['VerifiedSourceFiles']]+newrows
assert len({r['Path'] for r in guardrows})==len(guardrows)
dump(OUT/'verified-source-files.proposed.json',guardrows)
bindings=[{'Path':p,'Sha256':sha(ROOT/p),'Bytes':(ROOT/p).stat().st_size} for p in sorted(set(existing)|additional)]
dump(OUT/'final-source-bindings.json',bindings)

currentcore=(ROOT/'src/Erlang.Runtime/CoreModules.cs').read_bytes()
insertion=b'        OrdsetCoreModule.Register(r);\n'
assert currentcore.count(insertion)==1
assert hashlib.sha256(currentcore.replace(insertion,b'')).hexdigest().upper()=='A538B78EE3F0B0E8A206C69BD0B6A3883F6A8B2F2B310A951C64E4382DBE788A'
manual=[r for r in checks if not r['TokenEquivalent']]
report={'Status':'INDEPENDENT_CURRENT_MAIN_SOURCE_REVIEW_PENDING_FINAL_EXECUTION','BaseLedgerSha256':sha(ledgerpath),'ExistingGuardCount':len(existing),'ExistingGuardUpdates':changes,'AddedGuardRows':newrows,'FinalProposedGuardCount':len(guardrows),'UnchangedOldGuards':len(existing)-len(changes),'NoOldEntryRemoved':True,'ProposalSourceEquivalence':{'Files':len(checks),'TokenEquivalent':len(checks)-len(manual),'ExceptionsRequiringExplicitRootDeltaReview':manual},'NoMainBuildNetworkOrGit':True}
dump(OUT/'source-review.json',report)
print(json.dumps({'ExistingChanges':len(changes),'AddedGuards':len(newrows),'FinalGuards':len(guardrows),'ProposalFiles':len(checks),'TokenMismatch':[r['Path'] for r in manual]}))
