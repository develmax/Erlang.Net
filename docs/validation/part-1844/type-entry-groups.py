from collections import Counter,defaultdict
import json
from pathlib import Path
import sys
from beam_chunk_inventory import chunks, types

reference, frozen, out = map(Path, sys.argv[1:])
seen = defaultdict(set)
entries = Counter()
names = ["atom", "bitstring", "cons", "float", "fun", "integer", "map",
         "nil", "pid", "port", "reference", "tuple", "record"]
for report in sorted(frozen.glob("shard-*/otp-corpus.json")):
    rows = json.loads(report.read_text(encoding="utf-8-sig"))["Modules"]
    for row in rows:
        if row["Status"] != "Different":
            continue
        name = row["Beam"]
        ref = chunks((reference / name).read_bytes())
        candidate = chunks((report.with_suffix(".json.generated") / name).read_bytes())
        for entry, count in (Counter(types(ref)) - Counter(types(candidate))).items():
            flags = int.from_bytes(entry[:2], "big")
            key = ("|".join(name for i, name in enumerate(names) if flags & (1 << i)),
                   bool(flags & 0x2000), bool(flags & 0x4000), bool(flags & 0x8000))
            seen[key].add(name)
            entries[key] += count
result = sorted([dict(Union=union, Lower=lower, Upper=upper, Unit=unit,
    Modules=len(modules), MissingEntries=entries[(union,lower,upper,unit)])
    for (union,lower,upper,unit), modules in seen.items()],
    key=lambda row: (-row["Modules"], -row["MissingEntries"], row["Union"]))
out.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
for row in result[:14]:
    print(json.dumps(row))
