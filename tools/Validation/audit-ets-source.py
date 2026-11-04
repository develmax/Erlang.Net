"""Inventory pinned ETS exports, implementation clauses and external dependencies."""
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[2]
source = subprocess.check_output(
    ["git", "-C", str(root.parent / "otp"), "show", "OTP-29.1.1:lib/stdlib/src/ets.erl"]
)
text = source.decode("utf-8")
exports = sorted({(name, int(arity)) for block in re.findall(r"-export\(\[(.*?)\]\)\.", text, re.S)
                  for name, arity in re.findall(r"(\w+)\s*/\s*(\d+)", block)})
clauses = [{"function": match.group(1), "line": text.count("\n", 0, match.start()) + 1}
           for match in re.finditer(r"^(\w+)\(.*?\)\s*(?:when[^.]*?)?->", text, re.M)]
dependencies = sorted(set(re.findall(r"\b([a-z][a-z_0-9]*):[a-z][a-z_0-9]*\(", text)))
report = {
    "Baseline": "OTP-29.1.1",
    "Commit": "ad05823719d77c8faee87348ea39513d4e2f99c5",
    "SourceSha256": hashlib.sha256(source).hexdigest().upper(),
    "Exports": [{"Name": name, "Arity": arity} for name, arity in exports],
    "ExportCount": len(exports), "Clauses": clauses, "MentionedDependencies": dependencies,
    "Acceptance": "Inventory only; API presence does not prove semantic compatibility."
}
output = Path(sys.argv[1]) if len(sys.argv) > 1 else root / "artifacts/ets-source-audit.json"
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
print(f"ETS pinned public exports: {len(exports)}; {output}")
