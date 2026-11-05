"""Passive integrated-workflow byte admission, without platform/build/VM execution."""
import hashlib
import json
from pathlib import Path
import re


def verify(repository, workflow_text=None):
    root = Path(repository).resolve()
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
    for group in ["Inputs", "DriverSources"]:
        rows = plan[group]
        if not rows or len(rows) != len({row["Path"] for row in rows}):
            raise ValueError("Integrated source membership is empty or duplicated")
        for row in rows:
            if source_sha(row["Path"]) != row["Sha256"]:
                raise ValueError("Integrated input bytes differ: " + row["Path"])
    return {"PlanPins": len(plan["PlanPins"]), "SourceInputs": len(plan["Inputs"]),
            "DriverSources": len(plan["DriverSources"]), "BuildOrVmExecuted": False}
