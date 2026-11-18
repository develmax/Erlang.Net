import hashlib
import json
import sys
from collections import Counter
from pathlib import Path, PureWindowsPath


def read_partitioned_corpus(directory, release):
    results = {}
    for report in sorted(directory.glob("shard-*/otp-corpus.json")):
        data = json.loads(report.read_text(encoding="utf-8-sig"))
        for result in data["Modules"]:
            name = result["Beam"]
            if name in results:
                raise ValueError(f"Duplicate module path: {name}")
            relative = Path(*PureWindowsPath(name).parts)
            status = result["Status"]
            reference = release / relative
            generated = report.with_suffix(".json.generated") / relative
            image = {"Equal": reference, "Different": generated}.get(status)
            results[name] = (status, reference, image)
    return results


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def save(path, data):
    path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


old_dir, new_dir, release = map(Path, sys.argv[1:])
old = read_partitioned_corpus(old_dir, release)
new = read_partitioned_corpus(new_dir, release)
if old.keys() != new.keys() or len(new) != 1366:
    raise ValueError("The reports do not cover the same 1,366 module paths.")

changes = []
changed_status = []
path_rows = []
for name in sorted(new):
    old_status, _, old_image = old[name]
    new_status, reference, new_image = new[name]
    if old_status != new_status:
        changed_status.append({"Beam": name, "Before": old_status, "After": new_status})
    if old_image is None or new_image is None or old_image == new_image:
        continue
    before, after = digest(old_image), digest(new_image)
    if before != after:
        changes.append({"Beam": name, "BeforeSha256": before, "AfterSha256": after})
        path_rows.append("\t".join(map(str, [reference, old_image, new_image])))

counts = dict(Counter(status for status, _, _ in new.values()))
save(new_dir / "changed-images.json", changes)
save(new_dir / "status-changes.json", changed_status)
(new_dir / "changed-paths.tsv").write_text("\n".join(path_rows) + "\n", encoding="utf-8")
print(json.dumps({"Counts": counts, "Changed": len(changes), "StatusChanges": changed_status}, indent=2))
