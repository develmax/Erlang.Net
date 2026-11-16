from collections import Counter
from pathlib import Path
import sys

reference, before, after = map(Path, sys.argv[1:])


def chunks(path):
    image = path.read_bytes()
    offset = 12
    result = {}
    while offset < len(image):
        tag = image[offset:offset + 4].decode("ascii")
        size = int.from_bytes(image[offset + 4:offset + 8], "big")
        result[tag] = image[offset + 8:offset + 8 + size]
        offset += 8 + ((size + 3) & ~3)
    return result


changes = Counter()
for beam in sorted(after.rglob("*.beam")):
    relative = beam.relative_to(after)
    old = chunks(before / relative)
    new = chunks(beam)
    expected = chunks(reference / relative)
    tags = sorted(tag for tag in old.keys() | new.keys() if old.get(tag) != new.get(tag))
    if not tags:
        continue
    changes["changed_modules"] += 1
    if old.get("Dbgi") != new.get("Dbgi"):
        changes["changed_debug"] += 1
        if len(old.get("Dbgi", b"")) == 66 and len(new.get("Dbgi", b"")) > 66:
            changes["restored_debug"] += 1
        if new.get("Dbgi") == expected.get("Dbgi"):
            changes["new_exact_debug"] += 1
        elif old.get("Dbgi") == expected.get("Dbgi"):
            changes["lost_exact_debug"] += 1
    print(relative.as_posix(), ",".join(tags), len(old.get("Dbgi", b"")),
          len(new.get("Dbgi", b"")), len(expected.get("Dbgi", b"")),
          "Dbgi exact" if new.get("Dbgi") == expected.get("Dbgi") else "")
print(dict(changes))
