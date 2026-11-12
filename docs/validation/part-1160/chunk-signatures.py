
from pathlib import Path
from collections import Counter
import sys
ref, gen = map(Path, sys.argv[1:])
def chunks(p):
    data = p.read_bytes()
    result = {}
    cursor = 12
    while cursor < len(data):
        tag = data[cursor:cursor+4].decode("ascii")
        count = int.from_bytes(data[cursor+4:cursor+8], "big")
        result[tag] = data[cursor+8:cursor+8+count]
        cursor += 8 + ((count+3)&~3)
    return result
combos = Counter()
for p in gen.rglob("*.beam"):
    name = p.relative_to(gen)
    a, b = chunks(ref/name), chunks(p)
    combo = tuple(sorted(key for key in a.keys()|b.keys() if a.get(key) != b.get(key)))
    combos[combo] += 1
    if len(combo) <= 4:
        print(len(combo), ",".join(combo), name.as_posix())
print("Signatures ranked by number of differing chunks")
for signature, count in sorted(combos.items(),key=lambda row:(len(row[0]),-row[1],row[0]))[:25]:
    print(count, ",".join(signature))

