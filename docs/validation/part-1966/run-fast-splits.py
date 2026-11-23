from pathlib import Path
import collections
import hashlib
import json
import os
import subprocess
import sys
import time

root,sdk,binary=map(Path,sys.argv[1:])
def read(p):
    return json.loads(p.read_text(encoding="utf-8-sig"))
def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest().upper()
catalog=read(root/"scope.json")
identity=read(root/"focused-identity.json")
assert sha(binary)==identity["Tests"]
for name in ("Compiler","Runtime"):
    assert sha(binary.parent/f"Erlang.{name}.dll")==identity[name]
cases=(root/"fast-test-names.txt").read_text(encoding="utf-8-sig").splitlines()
assert len(cases)==len(set(cases))==catalog["FastCases"]
run=root/"fast-splits"
run.mkdir()
args=[str(sdk),str(binary)]
def options(group,report):
    return [*args,"--report",str(report),*(part for case in group for part in ("--case",case))]
def length(group,index):
    return len(subprocess.list2cmdline(options(group,run/f"lane-{index}.json")))
partitions=[]
cur=[]
limit=24000
for case in cases:
    if cur and length(cur+[case],len(partitions))>limit:
        partitions.append(cur)
        cur=[]
    cur.append(case)
if cur:
    partitions.append(cur)
assert all(length(group,i)<=limit for i,group in enumerate(partitions))
stats=[]
collected=[]
for index,group in enumerate(partitions):
    report=run/f"lane-{index}.json"
    start=time.monotonic()
    with (run/f"lane-{index}.stdout.log").open("wb") as out, (run/f"lane-{index}.stderr.log").open("wb") as err:
        result=subprocess.run(options(group,report),stdout=out,stderr=err,check=False)
    after=time.monotonic()-start
    if result.returncode:
        raise RuntimeError(f"Focused test lane {index} exited {result.returncode}.")
    leaf=read(report)
    assert leaf["Partial"] and leaf["TotalCount"]==catalog["TotalCatalog"] and not leaf["Blocks"]
    assert leaf["SelectedCount"]==leaf["Passed"]==len(group) and not leaf["Failed"]
    assert set(leaf["ExactCases"])==set(group) and len(leaf["ExactCases"])==len(group)
    assert set(test["Name"] for test in leaf["Tests"])==set(group)
    collected.extend(leaf["Tests"])
    stats.append({"Cases":len(group),"Seconds":after,"CommandLength":length(group,index),
                  "Exit":result.returncode,"Report":sha(report)})
assert collections.Counter(item["Name"] for item in collected)==collections.Counter(cases)
data={"Partial":True,"SelectedCount":len(cases),"TotalCount":catalog["TotalCatalog"],
      "Blocks":[],"ExactCases":sorted(cases),"Passed":len(cases),"Failed":0,"Tests":collected}
(root/"focused-fast.json").write_text(json.dumps(data,indent=2)+"\n",encoding="utf-8")
receipt={"CommandLengthLimit":limit,"OriginalCommandLength":length(cases,0),"Lanes":stats,
         "Compiler":identity["Compiler"],"Tests":identity["Tests"],"Sequential":True,"Coverage":len(cases)}
(run/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
print(f"{len(cases)}/{len(cases)} passed")