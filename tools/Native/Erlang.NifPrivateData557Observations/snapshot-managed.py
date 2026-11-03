"""Passive exact host closure snapshot. No build or VM execution."""
import argparse
from pathlib import Path
from evidence import require,save,sha
ap=argparse.ArgumentParser();ap.add_argument('--managed-host',type=Path,required=True);ap.add_argument('--output',type=Path,required=True);a=ap.parse_args()
host=a.managed_host.resolve();out=a.output.resolve();require(host.is_file() and not out.exists() and not out.is_relative_to(host.parent),'Actual existing host and fresh snapshot path required')
rows=[]
for p in sorted(host.parent.rglob('*')):
    require(not p.is_symlink(),'Host symlinks require separate admission')
    if p.is_file() and p.suffix in {'.dll','.json'}:rows.append({'Path':str(p.resolve()),'Bytes':p.stat().st_size,'Sha256':sha(p)})
require(any(r['Path']==str(host) for r in rows),'Host missing in closure');out.parent.mkdir(parents=True,exist_ok=True);save(out,rows);print(sha(out))
