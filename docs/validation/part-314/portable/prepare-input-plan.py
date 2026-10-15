"""Read-only source selection, hashing and explicit indexes; never creates a ZIP."""
from pathlib import Path
import datetime
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
ARTIFACTS = ROOT / "artifacts"
COMPOSED = ARTIFACTS / "allocator-query-mailbox314-composed1"
CANDIDATE = COMPOSED / "candidate"
CHUNK = 1024 * 1024
ROWS = []
SEEN = set()


def digest(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(CHUNK), b""):
            value.update(chunk)
    return value.hexdigest().upper()


def add(path, logical, role, expected=None):
    path = path.resolve()
    assert path.is_relative_to(ROOT) and path.is_file()
    assert logical not in SEEN and not logical.startswith("/") and ".." not in Path(logical).parts
    assert not path.is_relative_to(ROOT / "docs")
    sha = digest(path)
    if expected is not None:
        assert sha == expected
    SEEN.add(logical)
    ROWS.append({
        "Role": role,
        "SourcePath": path.relative_to(ROOT).as_posix(),
        "LogicalPath": logical,
        "Bytes": path.stat().st_size,
        "Sha256": sha,
    })


def source_directory(name, role):
    directory = ARTIFACTS / name
    manifest_names = {
        "allocator-query-mailbox314-candidate": "allocator-query-mailbox314-source-freeze.json",
        "allocator-query-mailbox314-revision1-candidate": "allocator-query-mailbox314-revision1-source-freeze.json",
        "allocator-reducer313-foundation-candidate": "allocator-reducer313-foundation-source-freeze.json",
    }
    if name in manifest_names:
        manifest = json.loads((ARTIFACTS / manifest_names[name]).read_bytes())
        selections = [(directory / row["Path"], row["Sha256"]) for row in manifest["Files"]]
    else:
        names = ["DESIGN.md", "design-freeze.json", "freeze.py"] if name.endswith("mailbox314-design1") else ["DESIGN.md", "source-audit-freeze.json", "freeze.py"]
        selections = [(directory / item, None) for item in names]
    for path, expected in sorted(selections):
        assert not {"bin", "obj", "__pycache__", ".git"}.intersection(path.relative_to(directory).parts)
        add(path, "source-only/" + name + "/" + path.relative_to(directory).as_posix(), role, expected)


def remote_indexes():
    zip_rows = []
    evidence = {}
    runs = []

    def raw(path, role):
        assert path.is_file() and not path.is_relative_to(ROOT / "docs")
        key = path.relative_to(ROOT).as_posix()
        value = evidence.setdefault(key, {"SourcePath": key, "Bytes": path.stat().st_size, "Sha256": digest(path), "Roles": []})
        if role not in value["Roles"]:
            value["Roles"].append(role)

    for kind, run in [("ci", 38800348459), ("native", 38800528519), ("reallocation", 38800489696)]:
        directory = ARTIFACTS / ("pack313-watch-" + str(run) + "-terminal")
        metadata = json.loads((directory / "artifacts.json").read_bytes())
        capture = json.loads((directory / "raw-capture.json").read_bytes())
        assert capture["RunId"] == run
        for artifact in metadata["artifacts"]:
            path = directory / ("artifact-" + str(artifact["id"]) + ".zip")
            sha = digest(path)
            assert artifact["digest"].lower() == "sha256:" + sha.lower()
            assert path.stat().st_size == artifact["size_in_bytes"] < 100 * 1024 * 1024
            zip_rows.append({
                "Role": "PreviousHead313RawApiZip-" + kind,
                "SourcePath": path.relative_to(ROOT).as_posix(),
                "SuggestedCopyName": "previous-head313-" + kind + "-artifact-" + str(artifact["id"]) + ".zip",
                "Bytes": path.stat().st_size,
                "Sha256": sha,
                "ApiDigestVerified": True,
                "Run": run,
                "Artifact": artifact["id"],
                "Conclusion": capture["WorkflowConclusion"],
                "Copied": False,
            })
        for path in sorted(directory.iterdir()):
            if path.is_file() and path.suffix in {".json", ".log"}:
                raw(path, "RawTerminalEnvelope-" + kind)
        runs.append({"Kind": kind, "Run": run, "Conclusion": capture["WorkflowConclusion"], "RawEnvelopeOnly": False})

    for path in sorted(ARTIFACTS.glob("pack313-*")):
        if path.is_file() and path.suffix in {".json", ".py"} and not path.name.startswith("pack313-ordinary-"):
            raw(path, "PreviousHead313IndependentAuditOrRunBinding")
    for path in sorted((ARTIFACTS / "part313-readonly-monitor").iterdir()):
        if path.is_file():
            raw(path, "PreviousHead313ReadOnlyDispatchMappingSnapshot")
    for name in [
        "root-part313-post-publication-dispatch-authority.json",
        "root-part313-reallocation-dispatch-attempt.json", "root-part313-reallocation-dispatch-result.json",
        "root-part313-native-dispatch-attempt.json", "root-part313-native-dispatch-result.json",
        "root-part313-ordinary-dispatch-attempt.json", "root-part313-ordinary-dispatch-result.json",
        "root-part313-stage-index-guard.json", "root-audit313-staged.py", "root-stage313.py",
    ]:
        raw(ARTIFACTS / name, "PreviousHead313DispatchOrStagingAudit")
    raw(ARTIFACTS / "part313-dispatch-proposal1/dispatch313-once-SKELETON.ps1", "PreviousHead313ActualDispatchScript")
    for directory_name in ["pack313-original13-source-contracts", "pack313-sdk-source-contracts"]:
        for path in sorted((ARTIFACTS / directory_name).iterdir()):
            if path.is_file():
                raw(path, "PreviousHead313StaticContractAuditInput")
    for path in sorted(ARTIFACTS.glob("pack313-*-part314-detached-staging-manifest.json")):
        if "ordinary" not in path.name:
            raw(path, "PreviousHead313DetachedStagingIndex")
    pending = [{
        "Kind": "ordinary", "Run": 38800562590, "Included": False,
        "Reason": "Explicitly deferred pending ordinary checkpoint supplement; no terminal result inferred from dispatch mapping.",
    }]
    output = {
        "Status": "EXPLICIT_SEPARATE_REMOTE_COPY_AND_AUDIT_INDEX_NO_COPY_PERFORMED",
        "Head": "a44f82a35ff95775941147ff9848a95a5247d76e",
        "RawApiZipRows": zip_rows,
        "RawJsonLogsAndReviewRows": sorted(evidence.values(), key=lambda row: row["SourcePath"]),
        "Runs": runs,
        "DeferredRuns": pending,
        "UnpackedArtifactContentsIndexedForCopy": False,
        "UnpackedRuntimeTarCopied": False,
        "MainDocsReadOrCopied": False,
        "CombinedRemoteZipCreated": False,
    }
    path = HERE / "remote-separate-copy-index.json"
    assert not path.exists()
    path.write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
    return output, path


def main():
    assert not (HERE / "input-plan.json").exists()
    closure_path = COMPOSED / "formatted-source-resource-closure.json"
    assert digest(closure_path) == "FBC1F442D1BCB9E45ADB86EB682798DE35E33AD063BF3E1E76F9A54B8E03BB89"
    closure = json.loads(closure_path.read_bytes())
    assert closure["SourceFiles"] == len(closure["Files"]) == 2215
    for row in closure["Files"]:
        add(CANDIDATE / row["Path"], "compiled314/candidate/" + row["Path"], "ActualFrozenSourceClosure314", row["Sha256"])
    for path in sorted(COMPOSED.iterdir()):
        if path.is_file():
            add(path, "compiled314/evidence/" + path.name, "ActualTargetFocusAndFormatEvidence314")
    focus = json.loads((COMPOSED / "focus-results.json").read_bytes())
    assert focus["Status"] == "PASS" and len(focus["Identities"]) == 3
    for row in focus["Identities"]:
        assert digest(Path(row["Path"])) == row["Sha256"]
    binary_directory = CANDIDATE / "tests/Erlang.Tests/bin/Debug/net10.0"
    for path in sorted(binary_directory.rglob("*")):
        if path.is_file():
            add(path, "compiled314/candidate/tests/Erlang.Tests/bin/Debug/net10.0/" + path.relative_to(binary_directory).as_posix(), "ReferencedCompiledOutputSnapshot314")
    runner = ARTIFACTS / "source-purge-stage315-focus"
    for name in ["Program.cs", "Focus.csproj"]:
        add(runner / name, "compiled314/focus-runner/" + name, "ReplayHelperSourceSnapshot")
    for path in sorted((runner / "bin/Debug/net10.0").iterdir()):
        if path.is_file():
            add(path, "compiled314/focus-runner/bin/Debug/net10.0/" + path.name, "ReplayHelperBinarySnapshot")
    for name in [
        "root-focus314-mailbox.ps1", "root-format314-mailbox.ps1", "root-guard314-mailbox-focus.py",
        "allocator-query-mailbox314-beam310-source-peer2.json", "allocator-query-mailbox314-beam310-formatted-source-peer.json",
        "allocator-query-mailbox314-source-freeze.json", "allocator-query-mailbox314-revision1-source-freeze.json",
        "allocator-reducer313-foundation-source-freeze.json",
    ]:
        add(ARTIFACTS / name, "source-only/bindings/" + name, "SourcePeerOrAuthorBinding314")
    for name, role in [
        ("allocator-query-mailbox314-candidate", "SourceOnlyAuthor314"),
        ("allocator-query-mailbox314-revision1-candidate", "SourceOnlySupersededAuthor314"),
        ("allocator-query-mailbox314-design1", "SourceOnlyTransportDesign314"),
        ("allocator-reducer313-foundation-candidate", "SourceOnlyPriorPureReducerFoundation"),
        ("native-payload-provider315-design1", "SourceOnlyUnimplementedProvider315Design"),
    ]:
        source_directory(name, role)
    policy_directory = ARTIFACTS / "allocator-query-mailbox314-import-policy2"
    policy = json.loads((policy_directory / "final-source-freeze.json").read_bytes())
    policy_closure = json.loads((policy_directory / "source-resource-closure.json").read_bytes())
    reconstructed = {row["Path"]: row["Sha256"] for row in closure["Files"]}
    for row in policy["Files"]:
        add(policy_directory / "candidate" / row["Path"], "source-only/policy2-final-plan/overlay/" + row["Path"], "SourceOnlyPolicy2OwnedOverlay", row["Sha256"])
        reconstructed[row["Path"]] = row["Sha256"]
    assert reconstructed == {row["Path"]: row["Sha256"] for row in policy_closure["Files"]}
    assert len(reconstructed) == 2215
    for path in sorted(policy_directory.iterdir()):
        if path.is_file():
            add(path, "source-only/policy2-final-plan/" + path.name, "SourceOnlyPolicy2FullPlanAndClosure")
    for path in [HERE / "archive-reviewed.py", Path(__file__).resolve()]:
        add(path, "packaging/" + path.name, "PassivePackagingSource")
    remote, remote_path = remote_indexes()
    groups = {}
    blobs = {}
    for row in ROWS:
        stats = groups.setdefault(row["Role"], {"LogicalRows": 0, "LogicalBytes": 0})
        stats["LogicalRows"] += 1
        stats["LogicalBytes"] += row["Bytes"]
        blobs.setdefault(row["Sha256"], row["Bytes"])
    plan = {
        "Status": "REVIEW_REQUIRED_NO_ARCHIVE_CREATED",
        "CreatedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "Scope": "Initial compiled314 sixteen-owned-role frozen source closure, actual target and seven HOST groups; author/provider design roles are source-only. Not final Main314/full/native execution proof.",
        "ArchiveContainsRemoteEvidence": False,
        "MaximumArchiveBytes": 100 * 1024 * 1024,
        "ArchiveScriptSha256": digest(HERE / "archive-reviewed.py"),
        "SourceClosureSha256": digest(closure_path),
        "SourceClosureRows": 2215,
        "ActualLoadedAssemblyIdentities": focus["Identities"],
        "ReplayRequirements": "Materialize LogicalPath from Blob entries; .NET10 and the original supported Windows environment are external prerequisites. No focus or embedded script runs automatically.",
        "Groups": groups,
        "LogicalRows": len(ROWS),
        "LogicalBytes": sum(row["Bytes"] for row in ROWS),
        "UniqueBlobs": len(blobs),
        "UniqueBlobBytes": sum(blobs.values()),
        "RemoteSeparateIndex": {"Path": remote_path.relative_to(ROOT).as_posix(), "Sha256": digest(remote_path), "ApiZipRows": len(remote["RawApiZipRows"]), "JsonLogReviewRows": len(remote["RawJsonLogsAndReviewRows"])},
        "DeferredRuns": remote["DeferredRuns"],
        "ArchiveRows": sorted(ROWS, key=lambda row: row["LogicalPath"]),
        "Execution": {"ArchiveCreated": False, "FocusExecuted": False, "MainEdited": False, "GitAction": False, "NetworkOrSdkOrVm": False},
    }
    target = HERE / "input-plan.json"
    target.write_text(json.dumps(plan, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: value for key, value in plan.items() if key != "ArchiveRows"}, indent=2))
    print("InputPlanSha256=" + digest(target))


if __name__ == "__main__":
    main()
