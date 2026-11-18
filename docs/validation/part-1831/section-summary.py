import json
import struct
import sys
from collections import Counter
from pathlib import Path, PureWindowsPath


def chunks(path):
    data = path.read_bytes()
    if data[:4] != b"FOR1" or data[8:12] != b"BEAM":
        raise ValueError(f"Not a BEAM file: {path}")
    table = {}
    offset = 12
    while offset < len(data):
        name, length = struct.unpack_from(">4sI", data, offset)
        start = offset + 8
        table[name.decode("ascii")] = data[start:start + length]
        offset = start + ((length + 3) & ~3)
    return table


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


old, current, release, output = map(Path, sys.argv[1:])
counts = {"Before": Counter(), "After": Counter()}
removed = Counter()
introduced = Counter()
nearest = []
for index in range(4):
    modules = {}
    exact_old = old / f"shard-{index}" / "otp-corpus.json"
    exact_current = current / f"shard-{index}" / "otp-corpus.json"
    for side, report in [("Before", exact_old), ("After", exact_current)]:
        for module in load(report)["Modules"]:
            module_path = module["Beam"]
            modules.setdefault(module_path, {})[side] = module["Status"]
    for module, statuses in modules.items():
        if statuses == {"Before": "Equal", "After": "Equal"}:
            continue
        relative = Path(*PureWindowsPath(module).parts)
        original = chunks(release / relative)
        by_side = {}
        for side, report in [("Before", exact_old), ("After", exact_current)]:
            table = chunks(report.with_suffix(".json.generated") / relative) if statuses[side] == "Different" else original
            difference = {tag for tag in table.keys() | original.keys() if table.get(tag) != original.get(tag)}
            counts[side].update(difference)
            by_side[side] = difference
        removed.update(by_side["Before"] - by_side["After"])
        introduced.update(by_side["After"] - by_side["Before"])
        difference = by_side["After"]
        if difference and len(difference) <= 4:
            nearest.append({"Beam": module, "Sections": sorted(difference)})

output.write_text(json.dumps({
    "Sections": {tag: {
        "Before": counts["Before"][tag],
        "After": counts["After"][tag],
        "Closed": removed[tag],
        "Introduced": introduced[tag],
    } for tag in sorted(counts["Before"].keys() | counts["After"].keys())},
    "NearBySections": sorted(nearest, key=lambda row: (len(row["Sections"]), row["Beam"])),
}, indent=2) + "\n", encoding="utf-8")
