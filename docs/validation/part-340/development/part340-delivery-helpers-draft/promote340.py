"""DRAFT. Root-owned future promotion; every precondition runs before copying owned files."""
import argparse,sys,subprocess,shutil
sys.dont_write_bytecode=True
from delivery340_common import *
p=argparse.ArgumentParser();p.add_argument('--candidate',type=Path,required=True);p.add_argument('--full-dir',type=Path,required=True);p.add_argument('--main',type=Path,required=True);p.add_argument('--guard-sha256',required=True);p.add_argument('--promotion-plan',type=Path,required=True);p.add_argument('--plan-sha256',required=True);p.add_argument('--finalized',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
c=a.candidate.resolve(strict=True);d=a.full_dir.resolve(strict=True);main=a.main.resolve(strict=True);assert c!=main and not a.output.exists()
g,f,t=completed(c,d,a.guard_sha256);assert sha(a.promotion_plan)==a.plan_sha256.upper();plan=read(a.promotion_plan)
assert plan['ParentHead']==PARENT and plan['SourceFreezeSha256']==sha(d/'source-freeze.json') and plan['SourceClosureSha256']==sha(d/'source-resource-closure.json')
assert subprocess.check_output(['git','-C',str(main),'rev-parse','HEAD']).decode().strip()==PARENT
assert not subprocess.check_output(['git','-C',str(main),'diff','--cached','--name-only']).strip()
fin=read(a.finalized);assert fin['Status']=='PASS340_FINALIZED_REGISTRY_AND_OLD_ORDER' and fin['FullGuardSha256']==a.guard_sha256.upper() and fin['RestoredRegistrySha256']==sha(c/'docs/supported-mfas.json')
owned=plan['PromotionPaths'];assert len(owned)==len(set(owned)) and set(owned)<={r['Path'] for r in f['Files']}
assert not any('Focus' in Path(p).parts or p.endswith('/Program.cs') and 'Focus' in p for p in owned)
parents={r['Path']:r for r in plan['ParentFiles']};assert len(parents)==len(plan['ParentFiles']);before={}
for path,row in parents.items():
 q=file_at(main,path);assert q.stat().st_size==row['Bytes'] and sha(q)==row['Sha256'];before[path]=q.read_bytes()
variance={r['Path']:r for r in plan['UnownedLineEndingVariances']}
for row in rows(d/'source-resource-closure.json'):
 path=row['Path'];tested=file_at(c,path).read_bytes()
 if path in owned:continue
 current=file_at(main,path).read_bytes();assert path in parents
 if current!=tested:
  v=variance[path];assert not path.endswith('.cs') and v['ParentRawSha256']==sha(main/path) and v['TestedRawSha256']==sha(c/path)
  assert current.replace(b'\r\n',b'\n')==tested.replace(b'\r\n',b'\n')
 else:assert path not in variance
for path in owned:
 assert (main/path).exists()==(path in parents),path
for path in owned:
 target=main/path;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(c/path,target)
for path in owned:assert (main/path).read_bytes()==(c/path).read_bytes()
for path,value in before.items():
 if path not in owned:assert (main/path).read_bytes()==value
save(a.output,dict(Status='EXACT_TESTED340_OWNED_SOURCE_PROMOTED',ParentHead=PARENT,Passed=t['Passed'],Registry=587,ProcLib=39,ImplementedModules=14,OwnedPaths=len(owned),AllOwnedRawExact=True,AllRecordedUnownedMainBytesPreserved=True,UnownedLineEndingVariances=len(variance),WholeClosureRawIdentical=not variance,FullGuardSha256=a.guard_sha256.upper(),SourceFreezeSha256=sha(d/'source-freeze.json'),SourceClosureSha256=sha(d/'source-resource-closure.json'),PromotionPlanSha256=a.plan_sha256.upper()))
