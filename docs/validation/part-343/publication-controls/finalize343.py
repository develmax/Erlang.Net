"""DRAFT candidate-only registry finalizer after independent actual-full guard; no Main writes."""
import argparse,sys
from pathlib import Path
sys.dont_write_bytecode=True
from common343 import *
p=argparse.ArgumentParser()
for n in ('candidate','main','guard','contract','output'):p.add_argument('--'+n,type=Path,required=True)
for n in ('guard-sha256','contract-sha256'):p.add_argument('--'+n,required=True)
a=p.parse_args();c=a.candidate.resolve(strict=True);main=a.main.resolve(strict=True)
assert c!=main and not a.output.exists() and sha(a.guard)==a.guard_sha256.upper() and sha(a.contract)==a.contract_sha256.upper()
g=read(a.guard);i=read(a.contract);assert g['Status']=='PASS_INDEPENDENT_ACTUAL343_FULL' and g['InputContractSha256']==sha(a.contract) and g['AllPrevious7200OrderedNamesPreserved'] and g['Failed']==0
assert i['ParentHead']==PARENT and g['Registry']==587 and g['ImplementedModules']==14
# Recheck source/closure after every gate and before any candidate metadata write.
full=a.guard.resolve().parent
for name in ('SourceFreeze','SourceClosure','PackageResources'):
 r=i['Inputs'][name];path=checked(full,r['Path']);assert sha(path)==r['Sha256'];exact(c,path)
old=read(main/'docs/supported-mfas.json');raw=(c/'docs/supported-mfas.json').read_bytes();actual=json.loads(raw)
assert len(old)==len(actual)==587 and [key(x) for x in old]==[key(x) for x in actual]
for before,after in zip(old,actual):assert {k:v for k,v in before.items() if k!='Limits'}=={k:v for k,v in after.items() if k!='Limits'}
archive=a.output.with_name(a.output.stem+'-generated-registry.json');assert not archive.exists()
# All checks above precede both writes. Keep raw generated history, restore every587 published row including customLimits.
archive.write_bytes(raw);(c/'docs/supported-mfas.json').write_text(json.dumps(old,indent=2)+'\n',encoding='utf-8',newline='\n')
assert read(c/'docs/supported-mfas.json')==old
save(a.output,dict(Status='PASS343_FINALIZED_EXACT587_REGISTRY',Registry=587,AllPrevious587RowsExact=True,GuardSha256=sha(a.guard),InputContractSha256=sha(a.contract),RawGeneratedRegistrySha256=sha(archive),RestoredRegistrySha256=sha(c/'docs/supported-mfas.json'),MainEdited=False))
