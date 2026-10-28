"""Read-only admission of already executed original/managed receipt chains.

The caller supplies independently reviewed root SHA values. This validates
integrity, not authenticity of an arbitrary unreviewed JSON document.
"""
import hashlib
import json
from pathlib import Path


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read_bound(path, expected):
    require(digest(path) == expected.upper(), "Reviewed root receipt SHA mismatch: " + str(path))
    return json.loads(Path(path).read_bytes())


def files_bound(directory, rows):
    result = {}
    for row in rows:
        name = row["Name"]
        require(Path(name).name == name and name not in result, "Escaping or duplicate artifact name")
        path = Path(directory) / name
        require(path.is_file() and path.stat().st_size == row["Bytes"] and digest(path) == row["Sha256"], "Completion-bound artifact changed: " + name)
        result[name] = path
    return result


def original_job(completion_path, completion_sha, source_plan_sha, pin, job):
    path = Path(completion_path)
    completion = read_bound(path, completion_sha)
    require(completion["ObservationAcquisitionPassed"] and completion["OriginalOnly"] and completion["CompiledWorkerRequested"], "Original acquisition incomplete or wrong worker context")
    require(completion["SourcePlanSha256"] == source_plan_sha and completion["PinnedCommit"] == pin, "Original source/pin admission differs")
    root = path.parent
    launch = read_bound(root / "launch-plan.json", completion["LaunchPlanSha256"])
    require(launch["CompiledWorkerRequested"] and launch["SourcePlanSha256"] == source_plan_sha, "Launch worker/source context differs")
    for name, key in (("provider-before.json", "ProviderBeforeSha256"), ("provider-after.json", "ProviderAfterSha256")):
        read_bound(root / name, completion[key])
    require(json.loads((root / "provider-after.json").read_bytes())["ProviderEqual"], "Provider postflight did not pass")
    inventories = [row for row in completion["ArtifactInventories"] if row["Job"] == job]
    require(len(inventories) == 1 and inventories[0]["Path"] == job + "/artifacts.json", "One exact job inventory required")
    inventory = read_bound(root / inventories[0]["Path"], inventories[0]["Sha256"])
    artifacts = files_bound(root / job, inventory["Files"])
    phase = completion["Phases"][job]
    require(phase["ExitCode"] == 0 and phase.get("Settled", phase.get("ParentReaped", False)), "Original child not settled successfully")
    required = {"observed.etf", "observed-deterministic.etf", "worker-witnesses.etf", "executing-owners.etf", "metadata.etf", "compile.etf", "modules.etf", "contract_worker513.erl", "contract_worker513.beam"}
    require(required.issubset(artifacts), "Whole profiles/witnesses/owners/compiler evidence not bound")
    return {"Artifacts": artifacts, "CompletionSha256": completion_sha, "Context": "MATCHED_COMPILED_WORKER", "IntegrityAdmitted": True}


def managed_job(batch_path, batch_sha, plan_sha, provider_sha, job, owned):
    path = Path(batch_path)
    receipt = read_bound(path, batch_sha)
    require(receipt["PlanSha256"] == plan_sha and receipt["AdmissionSha256"] == provider_sha, "Managed image/provider plan differs")
    require(receipt["Isolation"] == "ONE_LOGICAL_VM_CHILD", "Coordinated dynamic state needs an isolated child")
    rows = [row for row in receipt["Runs"] if row["Job"] == job and row["Owned"] == owned]
    require(len(rows) == 1 and rows[0]["Captured"], "Managed profile failed or duplicated")
    directory = path.parent / job / ("private-owned" if owned else "ordinary")
    artifacts = files_bound(directory, rows[0]["Artifacts"])
    require({"observed.etf", "observed-deterministic.etf", "actual-owners-before.etf", "actual-owners-after.etf", "worker-witnesses.etf"}.issubset(artifacts), "Managed whole serialization/witness/image-owner evidence missing")
    # Equality is checked on actual whole owner snapshots, not on transformed
    # ETF or prepared module names. Build-source provenance remains a separate
    # required admission; this cannot mint it from an assembly filename.
    require(artifacts["actual-owners-before.etf"].read_bytes() == artifacts["actual-owners-after.etf"].read_bytes(), "Actual module owner changed across subject invocation")
    return {"Artifacts": artifacts, "BatchSha256": batch_sha, "Context": "MATCHED_COMPILED_WORKER", "IntegrityAdmitted": True, "Pending": ["EXACT_BUILD_RECEIPT_BINDING_RUNTIME_TEST_ASSEMBLY_TO_REVIEWED_SOURCES"]}
