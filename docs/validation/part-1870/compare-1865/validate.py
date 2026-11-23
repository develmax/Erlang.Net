from pathlib import Path
import collections,json,sys
stage,baseline=map(Path,sys.argv[1:])
load=lambda p:json.loads(p.read_text(encoding="utf-8-sig"))
report=load(stage/"pilot.json")
scope=(stage/"pilot-modules.txt").read_text(encoding="utf-8-sig").splitlines()
previous=load(baseline/"full-uncached/otp-corpus.json")
old={r["Beam"]:r["Status"] for r in previous["Modules"]}
new={r["Beam"]:r["Status"] for r in report["Modules"]}
assert len(scope)==report["Total"]==len(new)==len(set(scope))
assert set(scope)==set(new) and set(new)<=set(old)
changed=[{"Beam":k,"Previous":old[k],"Current":new[k]} for k in scope if old[k]!=new[k]]
counts=collections.Counter(new.values())
for status in ("Equal","Different","Unsupported","SourceRejected"):
    counts.setdefault(status,0)
data={"Count":len(new),"Counts":dict(counts),"Transitions":changed,
      "RawChangedImages":len(load(stage/"compare-1865/pilot-delta.json")),
      "CompilerSeconds":load(stage/"pilot-receipt.json")["Seconds"]}
out=stage/"pilot-status-changes.json"
out.write_text(json.dumps(data,indent=2)+"\n")
print(json.dumps(data))
assert not any(x["Previous"]=="Equal" for x in changed)
assert not counts["Unsupported"] and not counts["SourceRejected"]

