from collections import Counter
from pathlib import Path
import sys

reference, generated = map(Path, sys.argv[1:])


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


sizes = Counter()
sections = Counter()
fallbacks = []
for beam in sorted(generated.rglob("*.beam")):
    relative = beam.relative_to(generated)
    expected, actual = chunks(reference / relative), chunks(beam)
    changed = {tag for tag in expected.keys() | actual.keys() if expected.get(tag) != actual.get(tag)}
    sizes[len(changed)] += 1
    sections.update(changed)
    if len(actual.get("Dbgi", b"")) == 66 and len(expected.get("Dbgi", b"")) > 66:
        fallbacks.append(relative.as_posix())

print("Differing chunks per module:", sorted(sizes.items()))
print("Modules affected per chunk:", sections.most_common())
print("66-byte Dbgi candidates:", len(fallbacks))
for path in fallbacks:
    print(path)
