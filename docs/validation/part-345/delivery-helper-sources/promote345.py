"""Root-only execution after explicit source review; preflight all inputs before first write."""
import argparse,sys,shutil
sys.dont_write_bytecode=True
from common345 import *
p=argparse.ArgumentParser()
for n in ('candidate','main','full-dir','independent-guard','plan','output'):p.add_argument('--'+n,type=Path,required=True)
for n in ('independent-guard-sha256','plan-sha256'):p.add_argument('--'+n,required=True)
a=p.parse_args();c=a.candidate.resolve(strict=True);main=a.main.resolve(strict=True);full=a.full_dir.resolve(strict=True)
assert c!=main and not a.output.exists() and sha(a.plan)==a.plan_sha256.upper()
assert subprocess.check_output(['git','-C',str(main),'rev-parse','HEAD']).decode().strip()==PARENT
assert not subprocess.check_output(['git','-C',str(main),'diff','--cached','--name-only']).strip()
g,owned,closure=authority(c,full,a.independent_guard,a.independent_guard_sha256);plan=read(a.plan)
assert plan['ParentHead']==PARENT and plan['SourceFreezeSha256']==SOURCE and plan['SourceClosureSha256']==CLOSURE and plan['IndependentGuardSha256']==sha(a.independent_guard) and set(plan['PromotionPaths'])==owned
before={}
for row in plan['ParentFiles']:
 p=checked(main,row['Path']);assert p.stat().st_size==row['Bytes'] and sha(p)==row['Sha256'];before[row['Path']]=p.read_bytes()
assert set(before)|owned==closure
for name in owned:assert (main/name).exists()==(name in before)
variants={r['Path']:r for r in plan['UnownedLineEndingVariances']};actual=set()
for name in closure-owned:
 b=checked(c,name).read_bytes()
 if before[name]!=b:
  row=variants[name];actual.add(name);assert not name.endswith('.cs') and row['MainSha256']==sha(main/name) and row['TestedSha256']==sha(c/name) and before[name].replace(b'\r\n',b'\n')==b.replace(b'\r\n',b'\n')
assert actual==set(variants)
for name in sorted(owned):
 p=main/name;p.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(checked(c,name),p)
for name in owned:assert checked(main,name).read_bytes()==checked(c,name).read_bytes()
for name,b in before.items():
 if name not in owned:assert checked(main,name).read_bytes()==b
save(a.output,dict(Status='EXACT_TESTED345_OWNED_SOURCE_PROMOTED',ParentHead=PARENT,OwnedPaths=len(owned),ClosurePaths=len(closure),AllOwnedRawExact=True,AllUnownedRecordedMainBytesPreserved=True,ExplicitUnownedEolOnlyVariances=len(actual),WholeClosureRawIdentical=not actual,SourceFreezeSha256=SOURCE,SourceClosureSha256=CLOSURE,IndependentGuardSha256=sha(a.independent_guard),PlanSha256=sha(a.plan)))
