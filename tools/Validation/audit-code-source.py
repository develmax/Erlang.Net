"""Inventory the pinned code API without inferring semantic acceptance from counts."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument("--output", required=True)
parser.add_argument("--reference-checkout", help="Read-only checkout containing the pinned OTP tag")
args = parser.parse_args()
repository = Path(__file__).resolve().parents[2]
baseline = json.loads((repository / "reference-baseline.json").read_text(encoding="utf-8-sig"))
if baseline["tag"] != "OTP-29.1.1" or baseline["commit"] != "ad05823719d77c8faee87348ea39513d4e2f99c5":
    raise SystemExit("Unexpected code baseline")
reference_checkout = Path(args.reference_checkout) if args.reference_checkout else repository.parent / "otp"
source = subprocess.check_output([
    "git", "-C", str(reference_checkout), "show",
    "OTP-29.1.1:lib/kernel/src/code.erl"
])
text = source.decode("utf-8")
exports = set()
for block in re.findall(r"-export\(\[(.*?)\]\)\.", text, re.S):
    exports.update((name, int(arity)) for name, arity in re.findall(r"(\w+)/(\d+)", block))
managed = repository / "src/Otp/Managed/Erlang.Otp.Stdlib/Code/CodeModule.cs"
body = managed.read_text(encoding="utf-8")
body = body[body.index("public static IReadOnlyList<(string Name"):body.index("public static void Register")]
api_names = dict(re.findall(r'internal const string (\w+) = "([^"]+)";',
                          (managed.parent / "CodeApiNames.cs").read_text(encoding="utf-8")))
actual = {(api_names[name], int(arity)) for name, arity in
          re.findall(r'\(CodeApiNames\.(\w+),\s*(\d+)\)', body)}
if actual != exports:
    raise SystemExit(f"Code API mismatch: missing={exports-actual}, extra={actual-exports}")
units = sorted((repository / "src/Otp/Managed/Erlang.Otp.Stdlib/Code").glob("*.cs"))
behavior_names = {"CodeModule.cs", "CodeModule.Documentation.cs", "CodeModule.Loading.cs",
                  "CodeModule.Paths.cs", "CodeSource.cs", "CodeParallelRecipe.cs", "CodeTermComparer.cs"}
literal_audit = []
for path in units:
    if path.name not in behavior_names:
        continue
    implementation = re.sub(r"//[^\n]*", "", path.read_text(encoding="utf-8"))
    strings = re.findall(r'"(?:[^"\\]|\\.)*"', implementation)
    unnamed_large_numbers = [value for value in re.findall(r"\b\d+\b", implementation) if int(value) > 3]
    if strings or unnamed_large_numbers:
        raise SystemExit(f"Unnamed code-domain literals in {path.name}: {strings}, {unnamed_large_numbers}")
    literal_audit.append({"Path": str(path.relative_to(repository)).replace("\\", "/"),
                          "InlineStringLiterals": 0, "UnnamedNumbersAboveArityRange": 0})
destination = Path(args.output)
destination.mkdir(parents=True, exist_ok=True)
inventory = {
    "Baseline": baseline,
    "ReferenceSourceSha256": hashlib.sha256(source).hexdigest().upper(),
    "PublicExports": len(exports),
    "Exports": [{"Function": name, "Arity": arity} for name, arity in sorted(exports)],
    "ManagedSourceBindings": [{"Path": str(path.relative_to(repository)).replace("\\", "/"),
                               "Sha256": hashlib.sha256(path.read_bytes()).hexdigest().upper()} for path in units],
    "ImplementationLiteralAudit": literal_audit,
    "SharedOriginalDependencies": ["code_server", "init", "erl_prim_loader", "filename", "beam_lib",
                                    "file", "io", "io_lib", "lists", "unicode", "zlib", "logger",
                                    "error_logger", "erl_features", "binary", "ets", "gb_sets", "gb_trees", "os"],
    "NativeCapabilityProfile": "Existing non-BEAMASM interpreter profile, seven shared native exports",
    "UniversalOtpCompatibilityCertified": False,
    "Interpretation": "Complete API inventory; individual contract, boot, resource and platform acceptance remain separate."
}
(destination / "source-inventory.json").write_text(json.dumps(inventory, indent=2) + "\n", encoding="utf-8")
lines = ["# code API inventory", "", "Pinned OTP 29.1.1; API presence is separate from semantic acceptance.", "",
         "| Export | Implementation |", "| --- | --- |"]
native = {"coverage_support", "get_coverage", "get_coverage_mode", "reset_coverage", "set_coverage_mode", "get_debug_info"}
for name, arity in sorted(exports):
    lines.append(f"| `{name}/{arity}` | {'Shared interpreter native primitive' if name in native else 'C# code library'} |")
(destination / "api-inventory.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
print(f"code: {len(exports)} pinned exports, exact managed signature inventory; no universal acceptance claim")
