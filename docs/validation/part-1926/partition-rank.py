from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import hashlib,json,os,subprocess,sys,time

stage,reference,evaluator,cache,erl=map(Path,sys.argv[1:6])
workers=int(sys.argv[6]) if len(sys.argv)>6 else 4
assert 1 <= workers <= 5
source=stage/'rank-input'
assert source.is_dir()
paths=sorted(source.rglob('*.beam'))
raw=json.loads((stage/'full-uncached/otp-corpus.json').read_text(encoding='utf-8-sig'))
assert len(paths)==raw['Counts']['Different'], (len(paths),raw['Counts'])
rank=stage/'partitioned-rank'
rank.mkdir()
groups=[]
group=[]
used=0
maximum_bytes=10*1024*1024
for path in paths:
    size=path.stat().st_size
    if group and (len(group)==35 or used+size>maximum_bytes):
        groups.append(group)
        group=[]
        used=0
    group.append(path)
    used+=size
if group:
    groups.append(group)
digest=hashlib.sha256(evaluator.read_bytes()).hexdigest().upper()
text=evaluator.read_text(encoding='utf-8-sig')

def process(pair):
    batch,files=pair
    out=rank/f'batch-{batch:02d}'
    folder=out/'input'
    folder.mkdir(parents=True)
    relative=[path.relative_to(source) for path in files]
    for path,name in zip(files,relative,strict=True):
        target=folder/name
        target.parent.mkdir(parents=True,exist_ok=True)
        os.link(path,target)
    began=time.monotonic()
    with (out/'stdout.log').open('wb') as stdout, (out/'stderr.log').open('wb') as stderr:
        process=subprocess.run([erl,'+S','1:1','+A','1','-noshell','-eval',text,'-extra',
            reference,folder,out/'functions.terms',cache,digest],stdout=stdout,stderr=stderr,
            env=os.environ|{'ERL_CRASH_DUMP':str(out/'crash.dump')},check=False)
    result={'Batch':batch,'ReturnCode':process.returncode,'Seconds':time.monotonic()-began,
            'Files':[name.as_posix() for name in relative]}
    (out/'receipt.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    return result

started=time.monotonic()
with ThreadPoolExecutor(max_workers=workers) as pool:
    results=list(pool.map(process,enumerate(groups)))
record={'Images':len(paths),'Batches':len(groups),'Workers':workers,
        'EvaluatorSha256':digest,'Seconds':time.monotonic()-started,'Results':results}
(rank/'receipt.json').write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
assert not any(r['ReturnCode'] for r in results),[r['Batch'] for r in results if r['ReturnCode']]
assert sorted(name for r in results for name in r['Files'])==sorted(p.relative_to(source).as_posix() for p in paths)
for result in results:
    print(f"batch {result['Batch']:02d}: {len(result['Files'])} images in {result['Seconds']:.2f}s")
print(f"Ranked {len(paths)} images with {workers} workers in {record['Seconds']:.2f}s")
