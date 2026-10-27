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
# Static reviewed production scope of Part519; no dependency on artifact packets.
MAP_PRODUCTION519 = (
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/BoxedFloat556Names.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.Float556.cs",
    "src/Runtime/Erlang.Terms/Storage/IOwnedFloatStorage556.cs",
    "src/Runtime/Erlang.Terms/Values/FloatTerm.cs",
    "src/Compiler/Erlang.Compiler/Execution/Execution.cs",
    "src/Runtime/Erlang.Runtime.Core/Diagnostics/Debug/DebugSharingSize425.cs",
    "src/Runtime/Erlang.Runtime.Core/Diagnostics/Debug/DebugSharingSizeFrame425.cs",
    "src/Runtime/Erlang.Runtime.Core/Diagnostics/Debug/DebugSharingSizeGraph528.cs",
    "src/Runtime/Erlang.Runtime.Core/Diagnostics/Debug/DebugSharingSizePolicy528.cs",
    "src/Runtime/Erlang.Runtime.Core/Execution/Beam/BeamExternalImports.cs",
    "src/Runtime/Erlang.Runtime.Core/Modules/ModuleRegistry.BeamPrimitives.cs",
    "src/Runtime/Erlang.Runtime.Core/Modules/ModuleRegistry.DebugSharing425.cs",
    "src/Runtime/Erlang.Runtime.Core/Modules/ModuleRegistry.HamtMap430.cs",
    "src/Runtime/Erlang.Runtime.Core/Modules/ModuleRegistry.MapKeys426.cs",
    "src/Runtime/Erlang.Runtime.Core/Primitives/Maps/HamtViewNode.OwnedPayload517.cs",
    "src/Runtime/Erlang.Runtime.Core/Primitives/Maps/HamtViewNode.cs",
    "src/Runtime/Erlang.Runtime.Core/Primitives/Maps/NativeMapMaterialization.LogicalEscape517.cs",
    "src/Runtime/Erlang.Runtime.Core/Primitives/Maps/NativeMapMaterialization.OwnedPayload517.cs",
    "src/Runtime/Erlang.Runtime.Core/Primitives/Maps/NativeMapMaterializedLayout505.OwnedPayload517.cs",
    "src/Runtime/Erlang.Runtime.Core/Primitives/Maps/OwnedFlatMapLookup517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessRuntime.DebugSharing425.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/CellStorage526Names.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/EncodedMapHeader526.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/GuestAdoptionFrame507.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/MapRouting517Names.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/MapRoutingFallback517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/NativeTermCellBuffer526.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedDetachedCopyFrame510.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedMapEntries517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedMapHamtView517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedMapHeader517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedMapHeaderKind517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedMapNodePlan517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedMapPayload517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedMapPayloadNames517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedMapRow517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedPayloadCopyFrame506.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedPhysicalWordSnapshot526.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedTermDetachedCopy506.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedTermTransfer506.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedWordSizeFrame526.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/PreparedMapGraph517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/PreparedMapGraphUse517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/PreparedOwnedMap517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/PreparedOwnedMapNode526.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/PreparedOwnedMapUse517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermConstruction.MapFork517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermConstruction514.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermFrame.Guest507.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermFrame.MapFork517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermFrame.Source511.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermFrame506.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.DebugWords526.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.Generations508.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.Guest507.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.MapPayload517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.MapRouting517.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.NativeWords526.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.SharedHamt526.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap506.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermSequence506.cs",
    "src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermSlab506.cs",
    "src/Runtime/Erlang.Terms/Collections/Maps/HamtBitmapTerm429.OwnedPayload517.cs",
    "src/Runtime/Erlang.Terms/Collections/Maps/HamtBitmapTerm429.cs",
    "src/Runtime/Erlang.Terms/Collections/Maps/MapTerm.MapOrigin517.cs",
    "src/Runtime/Erlang.Terms/Collections/Maps/MapTerm.OwnedPayload517.cs",
    "src/Runtime/Erlang.Terms/Collections/Maps/MapTerm.cs",
    "src/Runtime/Erlang.Terms/Storage/IOwnedMapPayload517.cs",
)


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
    repository_inputs.update(ROOT / name for name in MAP_PRODUCTION519)
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
