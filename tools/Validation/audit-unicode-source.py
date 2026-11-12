"""Inventory pinned public clauses and bind the Unicode port to its actual sources."""
import hashlib
import json
import pathlib
import re
import subprocess
import sys

root = pathlib.Path(__file__).resolve().parents[2]
destination = pathlib.Path(sys.argv[1])
destination.mkdir(parents=True, exist_ok=True)
reference = subprocess.check_output([
    "git", "-C", str(root.parent / "otp"), "show",
    "OTP-29.1.1:lib/stdlib/src/unicode.erl"
])
retained = root / "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp/unicode.erl"
if retained.read_bytes().replace(b"\r\n", b"\n") != reference.replace(b"\r\n", b"\n"):
    raise SystemExit("Retained Unicode source differs from pinned OTP")
source = reference.decode("utf-8")
exports = sorted(set(
    (name, int(arity))
    for block in re.findall(r"-export\(\[(.*?)\]\)\.", source, re.S)
    for name, arity in re.findall(r"([a-z][a-z_0-9]*)/(\d+)", block)
))
if len(exports) != 22:
    raise SystemExit("Unexpected pinned Unicode exports")
native = {("bin_is_7bit", 1), ("characters_to_binary", 2), ("characters_to_list", 2)}
clauses = {}
for match in re.finditer(r"^([a-z][a-z_0-9]*)\(", source, re.M):
    name = match[1]
    clauses.setdefault(name, []).append({
        "Line": source.count("\n", 0, match.start()) + 1,
        "Head": source[match.start():source.find("->", match.start())].strip()
    })
owned = sorted((root / "src/Otp/Managed/Erlang.Otp.Stdlib/Text").glob("Unicode*.cs"))
owned += sorted((root / "src/Runtime/Erlang.Runtime.Core/Primitives").glob("UnicodeNative*.cs"))
owned += [root / "src/Runtime/Erlang.Runtime.Core/Modules/ModuleRegistry.Unicode.cs"]
owned += [root / name for name in [
    "src/Runtime/Erlang.Runtime.Core/Modules/ModuleRegistry.BeamPrimitives.cs",
    "src/Runtime/Erlang.Runtime.Core/Modules/ModuleRegistry.LibrarySelectionOwners.cs",
    "src/Runtime/Erlang.Runtime.Core/Modules/LibrarySelectionNames.cs",
    "src/Runtime/Erlang.Runtime.Core/Modules/RuntimeManagedLibraryStage456.cs",
    "src/Runtime/Erlang.Runtime.Core/Services/RuntimeModuleSelection.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessRuntime.LibrarySelection.cs",
    "src/Runtime/Erlang.Runtime.Core/Execution/Beam/BeamBinaryExecution.cs",
    "src/Runtime/Erlang.Runtime.Core/Execution/Beam/BeamRegisterInterpreter.cs",
    "src/Otp/Managed/Erlang.Otp.Stdlib/Registration/ManagedStdlibRegistration456.cs"
]]
hashes = {str(path.relative_to(root)).replace("\\", "/"): hashlib.sha256(path.read_bytes()).hexdigest().upper() for path in owned}
apis = []
for name, arity in exports:
    if (name, arity) in native:
        contract = "Shared native primitive; upstream wrapper remains a native binding"
    elif name.startswith("characters_to_nf"):
        contract = "Streaming normalization; pinned unicode_util, flattened list output or 200-grapheme binary rows; error residual and badarg source metadata"
    elif name.startswith("characters_to_"):
        contract = "Wrapper/trap dispatch, encoding aliases, exact partial conversions and ml_map residual structure; fake-stack versus error_with_info distinction"
    elif name in ("bom_to_encoding", "encoding_to_bom"):
        contract = "Ordered BOM prefixes or exact encoding aliases; original function_clause arguments and source location"
    else:
        contract = "Inclusive integer codepoint guards, ASCII fast paths and exact pinned Unicode property/category dispatch"
    apis.append({"Name": name, "Arity": arity, "Native": (name, arity) in native,
                 "Contract": contract, "SourceClausesForName": clauses.get(name, [])})
report = {"Baseline": "OTP-29.1.1", "Commit": "ad05823719d77c8faee87348ea39513d4e2f99c5",
          "ReferenceSha256": hashlib.sha256(reference).hexdigest().upper(),
          "ExportCount": len(exports), "Apis": apis, "Sources": hashes,
          "Acceptance": "Source inventory and finite comparisons do not certify universal compatibility"}
(destination / "api-source-catalog.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
lines = ["# Pinned Unicode API inventory", "", "| API | Binding | Reviewed contract |", "| --- | --- | --- |"]
lines += [f"| `{api['Name']}/{api['Arity']}` | {'Native' if api['Native'] else 'C#'} | {api['Contract']} |" for api in apis]
(destination / "api-audit.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
print(f"Pinned Unicode: {len(exports)} exports, {len(hashes)} implementation source hashes")
