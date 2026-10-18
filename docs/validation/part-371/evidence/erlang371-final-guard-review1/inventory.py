from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
ledgerpath=ROOT/'docs/mfa-compatibility.json'
ledger=json.loads(ledgerpath.read_text())
changes=[]
missing=[]
for r in ledger['VerifiedSourceFiles']:
 p=ROOT/r['Path']
 if not p.exists():missing.append(r['Path']);continue
 current=sha(p)
 if current!=r['Sha256']:changes.append({'Path':r['Path'],'Before':r['Sha256'],'After':current})
counts={}
for r in ledger['Entries']:counts[r['Status']]=counts.get(r['Status'],0)+1
data={'LedgerSha256':sha(ledgerpath),'Entries':len(ledger['Entries']),'Statuses':counts,'ExistingGuards':len(ledger['VerifiedSourceFiles']),'ChangedGuards':changes,'MissingGuardPaths':missing}
(OUT/'inventory.json').write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
print(json.dumps(data,indent=2))
