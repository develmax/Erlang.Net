"""Seal promoted variadic sources and runtime asset; no build or VM execution."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
TOOL = Path(__file__).resolve().parent
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
CORE = "src/Runtime/Erlang.Runtime.Core"
NATIVE_ASSET = CORE + "/runtimes/win-x64/native/Erlang.Nif.WindowsVariadic.dll"
WINDOWS_ADAPTER_SOURCE = CORE + "/Native/Nif/native/windows_varargs526.c"
SOURCE_SUFFIXES = frozenset((".cs", ".csproj", ".props", ".targets", ".py", ".c", ".h"))
EXCLUDED_DIRECTORIES = frozenset(("bin", "obj", "__pycache__"))


def sha(path, mode="Raw"):
    data = path.read_bytes()
    if mode == "TextLF":
        data = data.replace(b"\r\n", b"\n")
    elif mode != "Raw":
        raise ValueError("Unknown source hash mode: " + mode)

    return hashlib.sha256(data).hexdigest().upper()


def main():
    rows = []
    for path in sorted(TOOL.rglob("*")):
        if not path.is_file() or path.name == "source-plan.json" or "__pycache__" in path.parts:
            continue
        rows.append({
            "Scope": "Tool",
            "Path": path.relative_to(TOOL).as_posix(),
            "HashMode": "Raw",
            "Sha256": sha(path),
        })

    repository_inputs = set()
    source_groups = (
        "tools/Native/Erlang.NifNativeBackend",
        "tools/Runtime/Erlang.OwnedGuest506Observations",
        "tools/Native/Erlang.DdllFormatterOriginalObservations/scripts",
        CORE + "/Native/Nif",
        CORE + "/Processes/Context",
        CORE + "/Scheduling",
        CORE + "/Execution/Beam",
    )
    for name in source_groups:
        for path in sorted((ROOT / name).rglob("*")):
            if not path.is_file() or path.suffix not in SOURCE_SUFFIXES:
                continue
            if any(part in EXCLUDED_DIRECTORIES for part in path.parts):
                continue
            repository_inputs.add(path)
    # Explicit required inputs fail sealing when the asset has not been built.
    repository_inputs.add(ROOT / CORE / "Erlang.Runtime.Core.csproj")
    repository_inputs.add(ROOT / NATIVE_ASSET)
    for path in sorted(repository_inputs):
        relative = path.relative_to(ROOT).as_posix()
        raw = path.suffix == ".cs" or relative in (NATIVE_ASSET, WINDOWS_ADAPTER_SOURCE)
        mode = "Raw" if raw else "TextLF"
        rows.append({
            "Scope": "Repository",
            "Path": relative,
            "HashMode": mode,
            "Sha256": sha(path, mode),
        })

    baseline_path = ROOT / "tools/Native/Erlang.NifMapFamily504Observations/source-plan.json"
    baseline = json.loads(baseline_path.read_bytes())
    native = TOOL / "fixtures/variadic_family526.c"
    erlang = TOOL / "fixtures/variadic_family526.erl"
    plan = {
        "PinnedCommit": PIN,
        "Platform": "windows-x64",
        "NativeSourcePath": native.relative_to(ROOT).as_posix(),
        "ErlangSourcePath": erlang.relative_to(ROOT).as_posix(),
        "NativeSourceSha256": sha(native),
        "ErlangSourceSha256": sha(erlang),
        "BuildScriptSha256": sha(TOOL / "build-native.py"),
        "Headers": baseline["Headers"],
        "Inputs": rows,
        "ExecutionApproved": False,
        "OriginalCollected": False,
        "WholeContractsClosed": 0,
    }
    output = TOOL / "source-plan.json"
    output.write_bytes((json.dumps(plan, indent=2) + "\n").encode("utf-8"))
    print(sha(output))


if __name__ == "__main__":
    main()
