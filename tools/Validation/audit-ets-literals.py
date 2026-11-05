"""Keep ETS protocol literals and semantic numeric values in their owning catalogs."""
import json
import re
import sys
from pathlib import Path

root = Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
directory = root / "src/Otp/Managed/Erlang.Otp.Stdlib/Collections/Ets"
failures = []
files = sorted(directory.glob("EtsModule*.cs")) + [directory / "EtsSource.cs", directory / "EtsFileOptions.cs"]
files.append(root / "tools/Validation/Erlang.EtsCompatibility/Program.cs")
for path in files:
    for number, line in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), 1):
        code = line.split("//", 1)[0]
        if 'public const string Name = "ets";' in code:
            continue
        if re.search(r'"(?:[^"\\]|\\.)*"', code):
            failures.append(f"{path.name}:{number}: inline protocol/text literal")
        if re.search(r"\b[0-9]{2,}\b", code):
            failures.append(f"{path.name}:{number}: unnamed semantic number/source coordinate")
print(json.dumps({"CheckedFiles": len(files), "Passed": not failures, "Failures": failures}, indent=2))
raise SystemExit(1 if failures else 0)
