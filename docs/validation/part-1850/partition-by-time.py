import hashlib
import json
import sys
from pathlib import Path

prior, target = map(Path, sys.argv[1:])
report = json.loads((prior / "otp-corpus.json").read_text(encoding="utf-8-sig"))
names = {row["Beam"] for row in report["Modules"]}
timings = {}
evidence = []
for path in sorted(prior.glob("shard-*/otp-corpus.json.timings.json")):
    data = path.read_bytes()
    evidence.append(dict(File=path.relative_to(prior).as_posix(), Sha256=hashlib.sha256(data).hexdigest()))
    for item in json.loads(data)["Modules"]:
        name = item["Beam"]
        assert name not in timings
        weight = item["Milliseconds"]
        assert weight > 0
        timings[name] = weight
assert names == timings.keys() and len(names) == 1366

groups = [list() for _ in range(4)]
loads = [0.0] * len(groups)
for name, weight in sorted(timings.items(), key=lambda item: (-item[1], item[0])):
    group = min(range(len(groups)), key=lambda index: (loads[index], index))
    groups[group].append(name)
    loads[group] += weight

target.mkdir()
partitions = []
for index, group in enumerate(groups):
    directory = target / f"shard-{index}"
    directory.mkdir()
    payload = ("\n".join(sorted(group)) + "\n").encode("utf-8")
    (directory / "modules.txt").write_bytes(payload)
    partitions.append(dict(Index=index, Count=len(group), PredictedMilliseconds=loads[index],
                           Sha256=hashlib.sha256(payload).hexdigest()))
assert sum(len(group) for group in groups) == len(names)
assert set().union(*groups) == names
manifest = dict(PriorCorpusSha256=hashlib.sha256((prior / "otp-corpus.json").read_bytes()).hexdigest(),
                TimingInputs=evidence, Partitions=partitions)
(target / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(json.dumps(partitions))

