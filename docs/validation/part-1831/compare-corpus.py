import hashlib
import json
import sys
from collections import Counter
from pathlib import Path, PureWindowsPath


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def save(path, data):
    path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def hash_file(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def image(root, index, report, relative_path, status, reference):
    if status == "Equal":
        return reference
    if status == "Different":
        return root / f"shard-{index}" / (report + ".generated") / relative_path
    return None


old_root, new_root, release = map(Path, sys.argv[1:])
report = "otp-corpus.json"
status_changes = []
changed_images = []
changed_paths = []
counts = Counter()
for index in range(4):
    old_report = load(old_root / f"shard-{index}" / report)
    new_report = load(new_root / f"shard-{index}" / report)
    before = {module["Beam"]: module["Status"] for module in old_report["Modules"]}
    after = {module["Beam"]: module["Status"] for module in new_report["Modules"]}
    if before.keys() != after.keys():
        raise ValueError(f"Shard {index} has a different corpus")
    for beam, new_status in after.items():
        counts[new_status] += 1
        old_status = before[beam]
        if old_status != new_status:
            status_changes.append({"Beam": beam, "Before": old_status, "After": new_status})
        relative_path = Path(*PureWindowsPath(beam).parts)
        reference = release / relative_path
        previous = image(old_root, index, report, relative_path, old_status, reference)
        current = image(new_root, index, report, relative_path, new_status, reference)
        if previous is None or current is None or previous == current:
            continue
        before_hash = hash_file(previous)
        after_hash = hash_file(current)
        if before_hash == after_hash:
            continue
        changed_images.append({"Beam": beam, "BeforeSha256": before_hash, "AfterSha256": after_hash})
        changed_paths.append("\t".join(map(str, [reference, previous, current])))

if counts.total() != 1366:
    raise ValueError(f"Unexpected corpus size {counts.total()}")
save(new_root / "status-changes.json", sorted(status_changes, key=lambda row: row["Beam"]))
save(new_root / "changed-images.json", sorted(changed_images, key=lambda row: row["Beam"]))
(new_root / "changed-paths.tsv").write_text("\n".join(sorted(changed_paths)) + "\n", encoding="utf-8")
print(dict(counts), len(changed_images), status_changes)
