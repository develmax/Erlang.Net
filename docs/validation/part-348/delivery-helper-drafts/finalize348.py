"""Candidate-only custom Limits restoration, preserving generated registry; no Main writes."""
import argparse,sys
sys.dont_write_bytecode=True
from common348 import *
p=argparse.ArgumentParser()
for n in ('candidate','main','full-dir','independent-guard','output'):p.add_argument('--'+n,type=Path,required=True)
p.add_argument('--independent-guard-sha256',required=True);a=p.parse_args();c=a.candidate.resolve(strict=True);main=a.main.resolve(strict=True);full=a.full_dir.resolve(strict=True)
assert c!=main and not a.output.exists();authority(c,full,a.independent_guard,a.independent_guard_sha256)
assert subprocess.check_output(['git','-C',str(main),'rev-parse','HEAD']).decode().strip()==PARENT
old=read(main/'docs/supported-mfas.json');generated=(c/'docs/supported-mfas.json').read_bytes();actual=json.loads(generated)
assert len(old)==len(actual)==587 and [key(r) for r in old]==[key(r) for r in actual]
for x,y in zip(old,actual):assert {k:v for k,v in x.items() if k!='Limits'}=={k:v for k,v in y.items() if k!='Limits'}
archive=a.output.with_name(a.output.stem+'-generated-registry.json');assert not archive.exists();archive.write_bytes(generated)
(c/'docs/supported-mfas.json').write_bytes((main/'docs/supported-mfas.json').read_bytes())
assert read(c/'docs/supported-mfas.json')==old
save(a.output,dict(Status='PASS348_FINALIZED_EXACT587_REGISTRY',Registry=587,AllPrevious587RowsRawExact=True,GuardSha256=sha(a.independent_guard),RawGeneratedRegistrySha256=sha(archive),RestoredRegistrySha256=sha(c/'docs/supported-mfas.json'),MainEdited=False))
