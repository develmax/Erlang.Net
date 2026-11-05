"""Passive integrated-workflow byte admission, without platform/build/VM execution."""
import hashlib
import json
from pathlib import Path
import re


PRODUCER_FIELDS = {
    "CAPTURE_PLAN": {"BuildPlanSha256"},
    "ACCEPTANCE_PLAN": {"N1BuildPlanSha256", "OriginalPlanSha256", "FixtureManifests"},
    "N2_ORIGINAL_PLAN": {"NativeBuildPlanSha256", "WrongEntryNativeBuildPlans"},
    "N2_MANAGED_PLAN": {"NativeBuildPlanSha256", "OriginalPlanSha256", "WrongEntryNativeBuildPlans"},
    "ENV709_ORIGINAL_PLAN": {"NativeBuildPlanSha256"},
    "ENV709_MANAGED_PLAN": {"NativeBuildPlanSha256", "OriginalPlanSha256"},
}


def require_producer_fields(role, dependency):
    missing = PRODUCER_FIELDS.get(role, set()) - dependency.keys()
    if missing:
        raise ValueError("Required producer fields missing: " + role + ": " + ", ".join(sorted(missing)))


def powershell_pin(repository, caller_text=None):
    root = Path(repository)
    if caller_text is None:
        caller_text = (root / "tools/validate-async-microstates.ps1").read_text()
    values = re.findall(r"\[string\]\$PlanSha256\s*=\s*['\"]([0-9A-Fa-f]{64})['\"]", caller_text)
    expected = hashlib.sha256((root / "tools/Validation/Erlang.AsyncMicrostateValidation/source-plan.json").read_bytes()).hexdigest().upper()
    if [value.upper() for value in values] != [expected]:
        raise ValueError("PowerShell async source-plan pin differs")
    return expected


def qualified_inputs(repository, owner, rows):
    repository = Path(repository).resolve()
    owner = Path(owner).resolve()
    for row in rows:
        if "Root" not in row:
            continue
        if row["Root"] not in ("Packet", "Tool", "Repository"):
            raise ValueError("Unknown source root: " + row["Root"])
        base = owner if row["Root"] in ("Packet", "Tool") else repository
        path = (base / row["Path"]).resolve()
        if not path.is_relative_to(base) or not path.is_file():
            raise ValueError("Qualified source is unavailable: " + row["Path"])
        raw = path.read_bytes()
        if row.get("HashMode") == "TextLF":
            raw = raw.replace(b"\r\n", b"\n")
        if hashlib.sha256(raw).hexdigest().upper() != row["Sha256"]:
            raise ValueError("Qualified source bytes differ: " + row["Path"])


def verify(repository, workflow_text=None):
    root = Path(repository).resolve()
    powershell_pin(root)
    plan_path = root / "tools/Native/Erlang.NifIntegratedObservations/inputs.json"
    plan = json.loads(plan_path.read_bytes())
    if workflow_text is None:
        workflow_text = (root / ".github/workflows/nif-upgrade-integrated.yml").read_text()

    def source_sha(name):
        path = (root / name).resolve()
        if not path.is_relative_to(root) or not path.is_file():
            raise ValueError("Integrated source unavailable: " + name)
        return hashlib.sha256(path.read_bytes()).hexdigest().upper()

    def workflow_pin(key, expected):
        matches = re.findall(r"(?m)^      " + re.escape(key) + r": ([0-9A-F]{64})$", workflow_text)
        if matches != [expected]:
            raise ValueError("Integrated workflow pin differs: " + key)

    if plan.get("ReadyForRootReviewedExecution") is not True:
        raise ValueError("Integrated plan is not admitted")
    workflow_pin("INTEGRATED_PLAN", source_sha(str(plan_path.relative_to(root))))
    for key, row in plan["PlanPins"].items():
        if source_sha(row["Path"]) != row["Sha256"]:
            raise ValueError("Integrated dependency bytes differ: " + key)
        workflow_pin(key, row["Sha256"])
        dependency_path = root / row["Path"]
        dependency = json.loads(dependency_path.read_bytes())
        require_producer_fields(key, dependency)
        qualified_inputs(root, dependency_path.parent, dependency.get("Inputs", []))
        if key == "CAPTURE_PLAN" and dependency["BuildPlanSha256"] != plan["PlanPins"]["NATIVE_PLAN"]["Sha256"]:
            raise ValueError("Native producer plan differs: " + key)
        if key == "ACCEPTANCE_PLAN":
            if dependency["N1BuildPlanSha256"] != plan["PlanPins"]["NATIVE_PLAN"]["Sha256"]:
                raise ValueError("Native producer plan differs: " + key)
            if dependency["OriginalPlanSha256"] != plan["PlanPins"]["ORIGINAL_PLAN"]["Sha256"]:
                raise ValueError("Original producer plan differs: " + key)
            for name, digest in dependency["FixtureManifests"].items():
                if source_sha("tools/Native/" + name + "/manifest.json") != digest:
                    raise ValueError("Fixture producer manifest differs: " + name)
        if key in ("N2_ORIGINAL_PLAN", "N2_MANAGED_PLAN"):
            expected = {
                "Windows": source_sha("tools/Native/Erlang.NifVersion651Observations/build-inputs.json"),
                "Linux": plan["PlanPins"]["NATIVE_PLAN"]["Sha256"],
            }
            if dependency.get("WrongEntryNativeBuildPlans") != expected:
                raise ValueError("Wrong-entry N1 producer plans differ: " + key)
        prefix = "ENV709" if key.startswith("ENV709") else "N2" if key.startswith("N2") else None
        if prefix and "NativeBuildPlanSha256" in dependency:
            expected = plan["PlanPins"][prefix + "_NATIVE_PLAN"]["Sha256"]
            if dependency["NativeBuildPlanSha256"] != expected:
                raise ValueError("Native producer plan differs: " + key)
        if prefix and "OriginalPlanSha256" in dependency:
            expected = plan["PlanPins"][prefix + "_ORIGINAL_PLAN"]["Sha256"]
            if dependency["OriginalPlanSha256"] != expected:
                raise ValueError("Original producer plan differs: " + key)
    for group in ["Inputs", "DriverSources"]:
        rows = plan[group]
        if not rows or len(rows) != len({row["Path"] for row in rows}):
            raise ValueError("Integrated source membership is empty or duplicated")
        for row in rows:
            if source_sha(row["Path"]) != row["Sha256"]:
                raise ValueError("Integrated input bytes differ: " + row["Path"])
    return {"PlanPins": len(plan["PlanPins"]), "SourceInputs": len(plan["Inputs"]),
            "DriverSources": len(plan["DriverSources"]), "BuildOrVmExecuted": False}
