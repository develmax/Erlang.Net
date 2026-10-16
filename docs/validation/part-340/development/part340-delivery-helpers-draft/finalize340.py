"""DRAFT. Candidate-only registry finalization; requires actual completed-full authority."""
import argparse,sys
sys.dont_write_bytecode=True
from delivery340_common import *
p=argparse.ArgumentParser();p.add_argument('--candidate',type=Path,required=True);p.add_argument('--full-dir',type=Path,required=True);p.add_argument('--baseline-root',type=Path,required=True);p.add_argument('--guard-sha256',required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
c=a.candidate.resolve(strict=True);d=a.full_dir.resolve(strict=True);base=a.baseline_root.resolve(strict=True);assert c!=base and not a.output.exists()
g,f,t=completed(c,d,a.guard_sha256)
oldtests=read(base/'docs/validation/part-338/final-candidate/tests.json');oldnames=[r['Name'] for r in oldtests['Tests']];oldset=set(oldnames)
assert oldtests['Passed']==len(oldnames)==7105 and oldtests['Failed']==0
assert [r['Name'] for r in t['Tests'] if r['Name'] in oldset]==oldnames
old=read(base/'docs/supported-mfas.json');new=read(c/'docs/supported-mfas.json');key=lambda r:(r['Module'],r['Function'],r['Arity']);previous={key(r):r for r in old};actual={key(r):r for r in new}
assert len(old)==len(previous)==576 and len(new)==len(actual)==587 and set(actual)-set(previous)==DELTA
for k,row in previous.items():assert {n:v for n,v in actual[k].items() if n!='Limits'}=={n:v for n,v in row.items() if n!='Limits'}
assert sum(k[0]=='proc_lib' for k in actual)==39 and len({k[0] for k in actual})==14
# All assertions precede the only candidate mutation. Preserve generator raw history before restoring custom Limits.
assert not (d/'supported-mfas-generated.json').exists();(d/'supported-mfas-generated.json').write_bytes((c/'docs/supported-mfas.json').read_bytes())
restored=[previous.get(key(row),row) for row in new]
(c/'docs/supported-mfas.json').write_text(json.dumps(restored,indent=2)+'\n',encoding='utf-8',newline='\n')
assert all(next(r for r in restored if key(r)==k)==v for k,v in previous.items())
exact(c,d/'source-freeze.json');exact(c,d/'source-resource-closure.json');exact(c,d/'package-resources.json')
save(a.output,dict(Status='PASS340_FINALIZED_REGISTRY_AND_OLD_ORDER',ParentHead=PARENT,Passed=t['Passed'],Registry=587,ProcLib=39,ImplementedModules=14,Previous576RowsExact=True,Previous7105OrderExact=True,FullGuardSha256=a.guard_sha256.upper(),SourceFreezeSha256=sha(d/'source-freeze.json'),SourceClosureSha256=sha(d/'source-resource-closure.json'),RestoredRegistrySha256=sha(c/'docs/supported-mfas.json'),MainEdited=False))
