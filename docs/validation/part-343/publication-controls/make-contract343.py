"""Passive actual-output adapter builder: refuses running/failed pipeline; no placeholder hashes."""
from pathlib import Path
import argparse,sys
sys.dont_write_bytecode=True
from common343 import *
p=argparse.ArgumentParser();p.add_argument('--full-dir',type=Path,required=True);p.add_argument('--producer-sha256',required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args();d=a.full_dir.resolve(strict=True)
assert not a.output.exists() and sha(d/'completion-guard.json')==a.producer_sha256.upper()
g=read(d/'completion-guard.json');assert g['Status']=='PASS_ACTUAL_ISOLATED343_FULL' and g['SingleUninterruptedFullPass'] is True and g['ParentHead']==PARENT
exit=read(d/'full-exit.json');assert exit['ExitCode']==0 and exit['Settled'] is True and exit['TimedOut'] is False
names=dict(ProducerGuard='completion-guard.json',SourceFreeze='source-freeze.json',SourceClosure='source-resource-closure.json',PackageResources='package-resources.json',FullExit='full-exit.json',Tests='tests.json',Integrations='integration-results.json',Registry='raw-supported-mfas.json',PackageOutputs='packages.json',AddedTests='added-tests.json',Preflight='preflight.json')
save(a.output,dict(ParentHead=PARENT,Inputs={k:dict(Path=v,Sha256=sha(checked(d,v))) for k,v in names.items()},FailureHistory=[]))
