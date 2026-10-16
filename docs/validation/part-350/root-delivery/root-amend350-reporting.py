from pathlib import Path
import json,hashlib
r=Path.cwd();o=r/'artifacts/root-part350-delivery';p=o/'promotion-plan.json';v=json.loads(p.read_bytes())
for row in v['Files']:
    path=row['TargetPath']
    if path in ['AGENTS.md','docs/PROGRESS.md','docs/COMPATIBILITY.md','docs/semantic-differences.md']:
        f=r/path;s=f.read_text(encoding='utf-8')
        s=s.replace('Part350 adds18 portable scenarios (six predicates/twelve numeric boundaries), with no production C# or API status changes.','Part350 adds18 portable scenarios (six predicates/twelve numeric boundaries), with no production C# changes; a separate post-full source audit marks four spawn_opt APIs as Known differences.')
        s=s.replace('Registry remains587/14modules and **10 verified,19 known differences,558 pending**, with59 runtime-source guards.','Final registry reporting is587/14modules and **10 verified,23 known differences,554 pending**, with59 runtime-source guards; the immutable full-run input had10/19/558.')
        b=s.encode();f.write_bytes(b);(o/'overlay'/path).write_bytes(b);row.update(Bytes=len(b),Sha256=hashlib.sha256(b).hexdigest().upper())
p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
