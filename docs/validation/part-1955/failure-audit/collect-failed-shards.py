from pathlib import Path
from collections import Counter
import json,sys
stage,parent,accepted,out=map(Path,sys.argv[1:])
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
rows=[r for f in sorted((stage/'full-uncached').glob('shard-*/otp-corpus.json')) for r in read(f)['Modules']]
assert len(rows)==1366 and len({r['Beam'] for r in rows})==1366
actual={row['Beam']:row for row in rows}
def against(p):
    before={row['Beam']:row['Status'] for row in read(p/'full-uncached/otp-corpus.json')['Modules']}
    assert before.keys()==actual.keys()
    return [dict(Beam=name,Before=s,After=actual[name]['Status'],Detail=actual[name].get('Detail'))
            for name,s in before.items() if s!=actual[name]['Status']]
proof=dict(Counts=dict(Counter(r['Status'] for r in rows)),ProvisionalDelta=against(parent),AcceptedDelta=against(accepted),
           Interruptions=[r for r in rows if r['Status'] not in ('Equal','Different')])
(out/'first-failure-inventory.json').write_text(json.dumps(proof,indent=2)+'\n')
print(json.dumps(proof))

