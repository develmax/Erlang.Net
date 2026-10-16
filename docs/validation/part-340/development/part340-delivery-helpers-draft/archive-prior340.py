"""DRAFT. Copy only an explicit reviewed proof plan, never whole artifact directories."""
import argparse,sys,shutil
sys.dont_write_bytecode=True
from delivery340_common import *
p=argparse.ArgumentParser();p.add_argument('--repository',type=Path,required=True);p.add_argument('--plan',type=Path,required=True);p.add_argument('--plan-sha256',required=True);p.add_argument('--destination',type=Path,required=True);a=p.parse_args();root=a.repository.resolve(strict=True);dest=a.destination.resolve()
assert sha(a.plan)==a.plan_sha256.upper() and not dest.exists();plan=read(a.plan);sources=set();targets=set()
for row in plan['Files']:
 assert row['Source'] not in sources and row['Destination'] not in targets;sources.add(row['Source']);targets.add(row['Destination'])
 source=file_at(root,row['Source']);target=dest/row['Destination'];assert target.resolve().is_relative_to(dest)
 assert source.stat().st_size==row['Bytes'] and sha(source)==row['Sha256']
 assert source.suffix not in {'.zip','.dll','.exe','.pdb','.nupkg','.beam'} and 'LICENSE.local' not in source.name
# All checks precede any proof copy. Destination is new; a partial failed write is retained for diagnosis.
dest.mkdir(parents=True)
for row in plan['Files']:
 target=dest/row['Destination'];target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/row['Source'],target);assert sha(target)==row['Sha256']
(dest/'.gitattributes').write_bytes(b'* binary\n')
save(dest/'evidence-index.json',plan['Files'])
save(dest/'evidence-receipt.json',dict(Status='EXACT_REVIEWED_PRIOR_PROOF_FILES_COPIED',Files=len(plan['Files']),PlanSha256=a.plan_sha256.upper(),ArchiveZipAndRuntimeBinariesCopied=False,Scope='Guards/receipts/raw files retained, no new native execution or reinterpretation. Original archive API digests remain recorded; ZIP contents are not copied here.'))
