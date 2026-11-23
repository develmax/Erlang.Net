from pathlib import Path
import hashlib,json,os,subprocess,sys,time
stage, reference, evaluator, cache, erl = map(Path,sys.argv[1:])
source=stage/"rank-input"
assert source.is_dir()
paths=sorted(source.rglob("*.beam"))
assert len(paths)>900,len(paths)
rank=stage/"partitioned-rank"
rank.mkdir()
limit=35
maximum_bytes=10*1024*1024
groups=[]; group=[]; used=0
for path in paths:
    size=path.stat().st_size
    if group and (len(group)==limit or used+size>maximum_bytes):
        groups.append(group);group=[];used=0
    group.append(path);used+=size
if group:
    groups.append(group)
digest=hashlib.sha256(evaluator.read_bytes()).hexdigest().upper()
eval_text=evaluator.read_text(encoding="utf-8-sig")
results=[]
start=time.monotonic()
for batch,files in enumerate(groups):
    out=rank/f"batch-{batch:02d}"
    folder=out/"input"
    folder.mkdir(parents=True)
    for path in files:
        dst=folder/path.relative_to(source)
        dst.parent.mkdir(parents=True,exist_ok=True)
        os.link(path,dst)
    env=os.environ|{"ERL_CRASH_DUMP":str(out/"crash.dump")}
    terms=out/"functions.terms"
    began=time.monotonic()
    with (out/"stdout.log").open("wb") as stdout,(out/"stderr.log").open("wb") as stderr:
        rc=subprocess.run([erl,"+S","1:1","+A","1","-noshell","-eval",eval_text,"-extra",
                           reference,folder,terms,cache,digest],
                          stdout=stdout,stderr=stderr,env=env,check=False).returncode
    result={"Batch":batch,"ReturnCode":rc,"Seconds":time.monotonic()-began,
            "Files":[p.relative_to(source).as_posix() for p in files]}
    (out/"receipt.json").write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
    results.append(result)
    print(f"batch {batch:02d}: {len(files)} images in {result['Seconds']:.2f} s, exit {rc}",flush=True)
receipt={"Images":len(paths),"Batches":len(groups),"EvaluatorSha256":digest,"Seconds":time.monotonic()-start,"Results":results}
(rank/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
assert not any(r["ReturnCode"] for r in results),[r["Batch"] for r in results if r["ReturnCode"]]

