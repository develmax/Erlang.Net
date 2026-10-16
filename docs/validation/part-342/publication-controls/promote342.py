"""DRAFT Root-owned promotion. Preflight complete raw prospective overlay before first Main write."""
import argparse,sys,subprocess,shutil
from pathlib import Path
sys.dont_write_bytecode=True
from common342 import *
p=argparse.ArgumentParser()
for n in ('candidate','main','full-dir','guard','finalized','plan','output'):p.add_argument('--'+n,type=Path,required=True)
for n in ('guard-sha256','finalized-sha256','plan-sha256'):p.add_argument('--'+n,required=True)
a=p.parse_args();c=a.candidate.resolve(strict=True);main=a.main.resolve(strict=True);d=a.full_dir.resolve(strict=True)
assert c!=main and not a.output.exists()
for path,digest in ((a.guard,a.guard_sha256),(a.finalized,a.finalized_sha256),(a.plan,a.plan_sha256)):assert sha(path)==digest.upper()
g=read(a.guard);fin=read(a.finalized);plan=read(a.plan)
assert g['Status']=='PASS_INDEPENDENT_ACTUAL342_FULL' and g['Failed']==0 and g['SingleUninterruptedFullPass'] is True
assert fin['Status']=='PASS342_FINALIZED_EXACT587_REGISTRY' and fin['GuardSha256']==sha(a.guard) and fin['RestoredRegistrySha256']==sha(c/'docs/supported-mfas.json')
assert plan['ParentHead']==PARENT and plan['FullGuardSha256']==sha(a.guard) and plan['SourceFreezeSha256']==g['SourceFreezeSha256'] and plan['SourceClosureSha256']==g['SourceClosureSha256']
assert subprocess.check_output(['git','-C',str(main),'rev-parse','HEAD']).decode().strip()==PARENT
assert not subprocess.check_output(['git','-C',str(main),'diff','--cached','--name-only']).strip()
source=d/plan['SourceFreezePath'];closure=d/plan['SourceClosurePath'];assert sha(source)==g['SourceFreezeSha256'] and sha(closure)==g['SourceClosureSha256']
owned=exact(c,source);tested=exact(c,closure);paths=plan['PromotionPaths'];assert len(paths)==len(set(paths)) and set(paths)==owned
parents={r['Path']:r for r in plan['ParentFiles']};assert len(parents)==len(plan['ParentFiles']);before={}
for name,r in parents.items():
 path=checked(main,name);assert path.stat().st_size==r['Bytes'] and sha(path)==r['Sha256'];before[name]=path.read_bytes()
for name in paths:assert (main/name).exists()==(name in parents)
# No silent normalization: every unowned tested input must already be raw-identical.
variances={x['Path']:x for x in plan['UnownedLineEndingVariances']};assert len(variances)==len(plan['UnownedLineEndingVariances'])
actual_variances=set()
for name in tested-owned:
 assert name in before
 candidate_bytes=checked(c,name).read_bytes()
 if before[name]!=candidate_bytes:
  actual_variances.add(name);v=variances[name]
  assert not name.endswith('.cs') and v['MainSha256']==sha(main/name) and v['TestedSha256']==sha(c/name)
  assert before[name].replace(b'\r\n',b'\n')==candidate_bytes.replace(b'\r\n',b'\n')
assert actual_variances==set(variances)
for name in paths:
 path=main/name;path.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(checked(c,name),path)
for name in paths:assert checked(main,name).read_bytes()==checked(c,name).read_bytes()
for name,data in before.items():
 if name not in owned:assert checked(main,name).read_bytes()==data
save(a.output,dict(Status='EXACT_TESTED342_OWNED_SOURCE_PROMOTED',ParentHead=PARENT,OwnedPaths=len(owned),ClosurePaths=len(tested),SourceFreezeSha256=g['SourceFreezeSha256'],SourceClosureSha256=g['SourceClosureSha256'],FullGuardSha256=sha(a.guard),FinalizedSha256=sha(a.finalized),PlanSha256=sha(a.plan),AllOwnedRawExact=True,AllUnownedRecordedMainBytesPreserved=True,ExplicitUnownedEolOnlyVariances=len(actual_variances),WholeClosureRawIdentical=not actual_variances))
