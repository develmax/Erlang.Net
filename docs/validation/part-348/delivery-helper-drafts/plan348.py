"""Read-only prospective overlay plan after settled full/independent guard; no Main edits."""
import argparse,sys
sys.dont_write_bytecode=True
from common348 import *
p=argparse.ArgumentParser()
for n in ('candidate','main','full-dir','independent-guard','output'):p.add_argument('--'+n,type=Path,required=True)
p.add_argument('--independent-guard-sha256',required=True);a=p.parse_args()
c=a.candidate.resolve(strict=True);main=a.main.resolve(strict=True);full=a.full_dir.resolve(strict=True)
assert c!=main and not a.output.exists() and subprocess.check_output(['git','-C',str(main),'rev-parse','HEAD']).decode().strip()==PARENT
g,owned,closure=authority(c,full,a.independent_guard,a.independent_guard_sha256)
parents=[];variances=[]
for name in sorted(closure):
 p=main/name
 if p.exists():
  p=checked(main,name);parents.append(dict(Path=name,Bytes=p.stat().st_size,Sha256=sha(p)))
  if name not in owned and p.read_bytes()!=checked(c,name).read_bytes():
   assert not name.endswith('.cs') and p.read_bytes().replace(b'\r\n',b'\n')==checked(c,name).read_bytes().replace(b'\r\n',b'\n'),name
   variances.append(dict(Path=name,MainSha256=sha(p),TestedSha256=sha(c/name)))
 else:assert name in owned,name
save(a.output,dict(Status='PLANNED_ACTUAL348_TESTED_OVERLAY_NO_MAIN_EDITS',ParentHead=PARENT,SourceFreezeSha256=SOURCE,SourceClosureSha256=CLOSURE,IndependentGuardSha256=sha(a.independent_guard),FullGuardSha256=sha(full/'completion-guard.json'),PromotionPaths=sorted(owned),ParentFiles=parents,UnownedLineEndingVariances=variances,FullDir=str(full)))
