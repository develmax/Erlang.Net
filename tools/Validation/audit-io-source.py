"""Inventory the pinned io export surface and retained source; this is not semantic certification."""
from pathlib import Path
import hashlib
import json
import re
import subprocess

root = Path(__file__).resolve().parents[2]
baseline = subprocess.check_output([
    "git", "-C", str(root.parent / "otp"), "show", "OTP-29.1.1:lib/stdlib/src/io.erl"
])
retained = root / "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp/io.erl"
if retained.read_bytes() != baseline:
    raise SystemExit("Retained io.erl differs from pinned OTP-29.1.1")
expected = set()
for declaration in re.findall(rb"-export\(\[(.*?)\]\)\.", baseline, re.S):
    expected.update((name.decode(), int(arity)) for name, arity in re.findall(rb"(\w+)\s*/\s*(\d+)", declaration))
directory = root / "src/Otp/Managed/Erlang.Otp.Stdlib/Io"
names = dict(re.findall(r'const string (\w+) = "([^"]+)";', (directory / "IoLibraryNames.cs").read_text(encoding="utf-8-sig")))
implementation = (directory / "IoLibrary.cs").read_text(encoding="utf-8-sig")
inventory = implementation.split("public static void Register", 1)[0]
actual = [(names[name], int(arity)) for name, arity in re.findall(r"\((\w+), (\d+)\)", inventory)]
if len(actual) != len(set(actual)) or set(actual) != expected:
    raise SystemExit(json.dumps({"Missing": sorted(expected - set(actual)), "Extra": sorted(set(actual) - expected)}))
print(json.dumps({
    "Baseline": "OTP-29.1.1",
    "SourceSha256": hashlib.sha256(baseline).hexdigest().upper(),
    "Exports": len(expected),
    "ManagedLibraryExports": len(expected) - 1,
    "SharedNativeExports": ["printable_range/0"],
    "RetainedSourceIdentical": True,
    "SemanticCompletionClaim": False,
}, indent=2))
