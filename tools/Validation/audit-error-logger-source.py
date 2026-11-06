"""Audit only the pinned error_logger surface and retained source identity."""
import hashlib
import json
import re
import subprocess
from pathlib import Path

baseline = "OTP-29.1.1"
commit = "ad05823719d77c8faee87348ea39513d4e2f99c5"
reference = subprocess.check_output(["git", "-C", "../otp", "show", baseline + ":lib/kernel/src/error_logger.erl"])
exports = set()
for declaration in re.findall(rb"-export\(\[(.*?)\]\)", reference, re.S):
    exports.update((name.decode(), int(arity)) for name, arity in re.findall(rb"([a-z_]+)/(\d+)", declaration))
root = Path("src/Otp/Managed/Erlang.Otp.Kernel/Logging/ErrorLogger")
constants = dict(re.findall(r'const string (\w+) = "([^"\n]+)";', (root / "ErrorLoggerFunctions.cs").read_text()))
managed = {(constants[name], int(arity)) for name, arity in re.findall(
    r"\(ErrorLoggerFunctions\.(\w+), (\d+)\)", (root / "ErrorLoggerModule.cs").read_text())}
original = Path("src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/src/error_logger.erl")
def digest(data):
    return hashlib.sha256(data).hexdigest()
result = {"baseline": baseline, "commit": commit, "exports": len(exports),
    "libraryExports": len(exports - {("warning_map", 0)}), "sharedPrimitives": [["warning_map", 0]],
    "missing": sorted(exports - managed), "extra": sorted(managed - exports),
    "referenceSourceSha256": digest(reference), "retainedSourceSha256": digest(original.read_bytes()),
    "retainedSourceMatchesIgnoringLineEndings": reference.replace(b"\r\n", b"\n") == original.read_bytes().replace(b"\r\n", b"\n"),
    "sources": {str(path).replace("\\", "/"): digest(path.read_bytes()) for path in sorted(root.glob("*.cs"))},
    "semanticAcceptance": "Separate focused OTP reports; API coverage is not universal compatibility"}
print(json.dumps(result, indent=2))
raise SystemExit(bool(result["missing"] or result["extra"] or not result["retainedSourceMatchesIgnoringLineEndings"]))
