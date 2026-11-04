"""Admit complete focused timer receipts; never refresh global readiness from them."""
from pathlib import Path
import hashlib
import json
import sys
import timer_validation_contract as contract

repository = Path(__file__).resolve().parents[2]
evidence = Path(sys.argv[1]).resolve()
output = Path(sys.argv[2]).resolve()


def read(relative):
    return json.loads((evidence / relative).read_text(encoding="utf-8-sig"))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


primary = []
profiles = []
upstream = []
for backend in ("Managed", "Beam"):
    row = read(f"acceptance-closure/{backend}-report.json")
    assert row["Backend"] == backend
    assert row["Exports"] == contract.TIMER_EXPORT_COUNT and row["Cases"] == row["Passed"] == contract.PRIMARY_CASE_COUNT
    assert row["EffectPassed"] == contract.BEHAVIORAL_SCENARIO_COUNT and not row["Failures"]
    assert row["CorpusSha256"] == digest(evidence / "acceptance-closure/original.etf")
    primary.append(row)
    for depth in (value for value in contract.BACKTRACE_DEPTHS if value != contract.DEFAULT_BACKTRACE_DEPTH):
        profile = read(f"closure-depth-{depth}/{backend}-report.json")
        assert profile["Cases"] == profile["Passed"] == contract.PRIMARY_CASE_COUNT
        assert profile["EffectPassed"] == contract.BEHAVIORAL_SCENARIO_COUNT and not profile["Failures"]
        for identity in ("ManagedAssemblySha256", "RuntimeAssemblySha256"):
            assert profile[identity] == row[identity]
        profiles.append({"depth": depth, "backend": backend, "passed": profile["Passed"],
                         "corpusSha256": profile["CorpusSha256"]})
    suite = read(f"accepted-suite/{backend}-upstream-report.json")
    assert suite["Cases"] == suite["Passed"] == contract.UPSTREAM_CASE_COUNT and not suite["Failures"]
    upstream.append(suite)

for identity in ("ManagedAssemblySha256", "RuntimeAssemblySha256"):
    assert primary[0][identity] == primary[1][identity]
binary = Path(sys.argv[3]).resolve() if len(sys.argv) > 3 else evidence / contract.DEFAULT_BINARY_PATH
assert digest(binary / contract.RUNTIME_ASSEMBLY) == primary[0]["RuntimeAssemblySha256"]
assert digest(binary / contract.STDLIB_ASSEMBLY) == primary[0]["ManagedAssemblySha256"]
suite_inputs = read("accepted-suite/suite-final-inputs.json")
for file, expected in suite_inputs["Files"].items():
    assert digest(binary / file) == expected.upper()
bindings = read("final-source-bindings.json")
for file, expected in bindings.items():
    assert digest(repository / file) == expected.upper()
inventory = json.loads((repository / "docs/validation/part-565/source-inventory.json").read_text())
assert len(inventory["exports"]) == contract.TIMER_EXPORT_COUNT and len(inventory["imports"]) == contract.TIMER_IMPORT_COUNT
assert inventory["sourceUnchanged"]
assert digest(repository / "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp/timer.beam") == inventory["beamSha256"].upper()

result = {
    "baseline": contract.PINNED_TAG, "module": contract.MODULE_NAME, "exports": contract.TIMER_EXPORT_COUNT,
    "primaryCasesPerBackend": contract.PRIMARY_CASE_COUNT, "behavioralScenariosPerBackend": contract.BEHAVIORAL_SCENARIO_COUNT,
    "upstreamCasesPerBackend": contract.UPSTREAM_CASE_COUNT, "backtraceDepths": list(contract.BACKTRACE_DEPTHS),
    "fullRegressionRun": False, "globalReadinessRegenerated": False,
    "universalCompatibilityCertified": False, "defaultTimerExports": 0,
    "primary": primary, "profiles": profiles, "upstream": upstream,
    "suiteInputIdentities": suite_inputs, "sourceBindings": bindings,
    "originalInputSha256": digest(evidence / "upstream-original/upstream.etf"),
    "projectedUpstreamSha256": digest(evidence / "accepted-suite/upstream.etf"),
    "projectionProof": (evidence / "accepted-suite/projection-proof.txt").read_text(),
    "ordinaryBuildBoundary": "Clean main plus scoped timer sources; ordinary connected build passed without exclusion filters."
}
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
print("timer: both backends admitted; 728 primary + 11 scenarios + 47 upstream; depth 0/1/2/3/8/32")
