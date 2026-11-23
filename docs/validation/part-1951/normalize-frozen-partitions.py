from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from time import perf_counter
import hashlib,json,os,re,subprocess,sys

stage,inputs,refs=map(Path,sys.argv[1:])
output=stage/'normalization-bounded-recovery'
output.mkdir()
full=stage/'full-uncached'
changed=full/'changed-paths.tsv'
recipe=inputs/'bounded-normalization-workloads-1921/bounded-normalized-delta.eval'
identity=inputs/'bounded-normalization-workloads-1921/serial-reference.eval'
cache=inputs/'singleton-returns-1840/part1839-cumulative-normalization/cache'
def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()
rows=changed.read_text(encoding='utf-8-sig').splitlines()
assert len(rows)==json.loads((full/'compare-summary.json').read_text(encoding='utf-8-sig'))['Changed']
assert all(len(row.split('\t'))==3 for row in rows)
n=40
lanes=[rows[i:i+n] for i in range(0,len(rows),n)]
clock=perf_counter()

def run(item):
    index,work=item
    root=output/f'partition-{index:03d}'
    root.mkdir()
    manifest=root/'paths.tsv'
    manifest.write_text('\n'.join(work)+'\n',encoding='utf-8',newline='\n')
    std=root/'normalized.terms'
    err=root/'normalized.stderr.log'
    environment={**os.environ,'ERL_CRASH_DUMP':str(root/'erl-crash.dump'),'ERL_FLAGS':'+S 1:1 +A 1'}
    begin=perf_counter()
    with std.open('wb') as stdout, err.open('wb') as stderr:
        cmd=[str(refs/'bin/erl.exe'),'+S','1:1','+A','1','-noshell','-eval',recipe.read_text(encoding='utf-8-sig'),
             '-extra',str(manifest),str(cache),sha(identity),'1']
        subprocess.run(cmd,cwd=root,env=environment,check=True,timeout=1000,stdout=stdout,stderr=stderr)
    actual=std.read_text(encoding='utf-8-sig').splitlines()
    assert len(actual)==len(work),root
    for line,original in zip(actual,work):
        name=Path(original.split('\t')[0]).name
        assert line.startswith('{"'+name+'",'),(root,line[:100],name)
    return dict(Partition=index,Modules=len(work),Seconds=perf_counter()-begin,Input=sha(manifest),Output=sha(std)),std

with ThreadPoolExecutor(max_workers=2) as workers:
    results=list(workers.map(run,enumerate(lanes)))
all_normalized=full/'normalized.terms'
with all_normalized.open('xb') as combined:
    for _,path in results:
        combined.write(path.read_bytes())
(full/'normalized.stderr.log').write_bytes(b''.join(
    (p.parent/'normalized.stderr.log').read_bytes() for _,p in results))
assert len(all_normalized.read_text(encoding='utf-8-sig').splitlines())==len(rows)
report=dict(Seconds=perf_counter()-clock,Workers=2,MaxModulesPerProcess=n,Modules=len(rows),
            Evaluator=sha(recipe),OriginalInput=sha(changed),Output=sha(all_normalized),
            ABBA=sha(inputs/'bounded-normalization-workloads-1921/receipt.json'),
            Partitions=[record for record,_ in results],
            Reason='Bound processes after Part 1950 exhausted Erlang heap at four concurrent workers.')
data=json.dumps(report,indent=2)+'\n'
(full/'normalization-timing.json').write_text(data,encoding='utf-8')
(output/'receipt.json').write_text(data,encoding='utf-8')
print(data)

