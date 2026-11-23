from pathlib import Path
import hashlib,json,sys

stage,inputs=map(Path,sys.argv[1:])
root=stage/"normalization-bounded-recovery"
full=stage/"full-uncached"
def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):
    return json.loads(p.read_text(encoding="utf-8-sig"))
expected=(full/"changed-paths.tsv").read_text(encoding="utf-8-sig").splitlines()
out=[]
manifests=[]
lanes=[]
started,finished=[],[]
for path in sorted(root.glob("partition-*")):
    manifest=path/"paths.tsv"
    normalized=path/"normalized.terms"
    stderr=path/"normalized.stderr.log"
    assert stderr.stat().st_size==0 and not (path/"erl-crash.dump").exists()
    source=manifest.read_text(encoding="utf-8-sig").splitlines()
    data=normalized.read_text(encoding="utf-8-sig").splitlines()
    assert len(source)==len(data) and all(len(row.split("\t"))==3 for row in source)
    for original,result in zip(source,data):
        assert result.startswith('{"'+Path(original.split("\t",1)[0]).name+'",')
    manifests.extend(source)
    out.append(normalized.read_bytes())
    elapsed=normalized.stat().st_mtime-manifest.stat().st_mtime
    assert elapsed>=0
    started.append(manifest.stat().st_mtime)
    finished.append(normalized.stat().st_mtime)
    lanes.append(dict(Partition=len(lanes),Modules=len(source),ElapsedFromFiles=elapsed,
                      Input=digest(manifest),Output=digest(normalized)))
assert manifests==expected and len(expected)==read(full/"compare-summary.json")["Changed"]
assert b"".join(out)==(full/"normalized.terms").read_bytes()
report=dict(SecondsFromFiles=max(finished)-min(started),Workers=2,MaxModulesPerProcess=40,Modules=len(expected),
            Evaluator=digest(stage/"cached-normalized-delta.eval"),OriginalInput=digest(full/"changed-paths.tsv"),
            Output=digest(full/"normalized.terms"),CompilerResultsReused=False,
            MeasuredBenchmark=digest(stage/"normalized-cache-benchmark.json"),
            ABBA=digest(inputs/"bounded-normalization-workloads-1921/receipt.json"),
            OriginalError=digest(stage/"normalize-bounded.stderr.log"),RecoveredFromExistingOutputs=True,
            Partitions=lanes)
for dst in (full/"normalization-timing.json",root/"receipt.json"):
    with dst.open("x",encoding="utf-8",newline="\n") as f:
        json.dump(report,f,indent=2)
        f.write("\n")
print(len(expected),"bound outputs from",len(lanes),"already completed partitions")