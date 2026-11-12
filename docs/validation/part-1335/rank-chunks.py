from collections import Counter
from pathlib import Path
from struct import unpack_from
import sys


def chunks(path):
    data = path.read_bytes()
    if data[:4] != b"FOR1" or data[8:12] != b"BEAM":
        raise ValueError(path)
    result = {}
    offset = 12
    while offset < len(data):
        name, size = unpack_from(">4sI", data, offset)
        start = offset + 8
        result[name.decode("ascii")] = data[start:start + size]
        offset = start + ((size + 3) & ~3)
    return result


reference, generated = map(Path, sys.argv[1:3])
module_rows = []
frequency = Counter()
combinations = Counter()
for actual_path in sorted(generated.rglob("*.beam")):
    relative = actual_path.relative_to(generated)
    reference_path = reference / relative
    expected = chunks(reference_path)
    actual = chunks(actual_path)
    different = tuple(name for name in dict.fromkeys([*expected, *actual])
                      if expected.get(name) != actual.get(name))
    frequency.update(different)
    combinations[different] += 1
    code_ref = expected.get("Code", b"")
    code_actual = actual.get("Code", b"")
    code_gap = abs(len(code_ref) - len(code_actual))
    module_rows.append((len(different), code_gap, str(relative), different))

print(f"{len(module_rows)} differing images")
print("Chunk differences by frequency")
for chunk, count in frequency.most_common():
    print(f"{count:4d} {chunk}")
print("Most frequent chunk combinations")
for combination, count in combinations.most_common(12):
    print(f"{count:4d} {' '.join(combination)}")
print("Fewest differing chunks, then smallest Code length gap")
for count, code_gap, relative, different in sorted(module_rows)[:35]:
    print(f"{count:2d} {code_gap:5d} {relative} {' '.join(different)}")
