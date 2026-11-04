"""Inventory the pinned source and selectable export surface; no semantic certification."""
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
from gen_statem_validation_contract import (BASELINE, COMMIT, ORIGINAL_SOURCE, MANAGED_SOURCES, SOURCE_OBJECT, DEFAULT_AUDIT)

repository = Path(__file__).resolve().parents[2]
source_path = repository / ORIGINAL_SOURCE
beam_path = source_path.with_suffix(".beam")
pinned = subprocess.check_output([
    "git", "-C", str(Path(sys.argv[2]) if len(sys.argv) > 2 else repository.parent / "otp"), "show", SOURCE_OBJECT
])
current = source_path.read_bytes()
text = re.sub(r'""".*?"""', "", pinned.decode("utf-8"), flags=re.S)
text = re.sub(r"%[^\n]*", "", text)
exports = set()
for block in re.findall(r"-export\s*\((.*?)\)\s*\.", text, flags=re.S):
    exports.update((name, int(arity)) for name, arity in re.findall(r"([a-z_][a-z_0-9]*)\s*/\s*(\d+)", block))
exports.add(("behaviour_info", 1))
managed_root = repository / MANAGED_SOURCES
constants = {}
for path in managed_root.glob("*Names.cs"):
    for name, literal in re.findall(r'internal const string (\w+)\s*=\s*("(?:[^"\\]|\\.)*")\s*;', path.read_text(encoding="utf-8")):
        constants[path.stem + "." + name] = json.loads(literal)
for path in managed_root.glob("*Atoms.cs"):
    for name, literal in re.findall(r'internal const string (\w+)\s*=\s*("(?:[^"\\]|\\.)*")\s*;', path.read_text(encoding="utf-8")):
        constants[path.stem + "." + name] = json.loads(literal)
declaration = (managed_root / "GenStatemModule.cs").read_text(encoding="utf-8").split("public static void Register", 1)[0]
managed = {(constants[name], int(arity)) for name, arity in re.findall(r"\((GenStatem\w+\.\w+),\s*(\d+)\)", declaration)}
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
result = {
    "Baseline": BASELINE, "Commit": COMMIT,
    "OriginalSourceSha256": sha(current), "PinnedSourceSha256": sha(pinned),
    "ExactSourceBytesUnchanged": current == pinned,
    "OriginalBeamSha256": sha(beam_path.read_bytes()),
    "ExportsMatch": exports == managed, "ExportCount": len(exports),
    "Exports": sorted(exports),
    "OriginalEngineFunctions": sorted(set(re.findall(r"^([a-z][a-zA-Z_0-9]*)\(", text, flags=re.M))),
    "ManagedSources": {str(path.relative_to(repository)).replace("\\", "/"): sha(path.read_bytes())
                       for path in sorted(managed_root.glob("*.cs"))},
    "Scope": "Source/export inventory only. Finite differential acceptance is recorded separately."
}
output = Path(sys.argv[1]) if len(sys.argv) >= 2 else repository / DEFAULT_AUDIT
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
print(f"{len(exports)} exports; exact upstream source: {current == pinned}; managed surface: {exports == managed}")
sys.exit(0 if current == pinned and exports == managed else 1)
