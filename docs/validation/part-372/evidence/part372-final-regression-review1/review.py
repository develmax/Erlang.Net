"""Passive regression receipt: preserve known differences, never promote contracts."""
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
BASE = "230de4cafb1a67f34855fbea6b089e4b399a3c5d"


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def load(path):
    return json.loads((ROOT / path).read_text(encoding="utf-8-sig"))


def binding(path):
    data = (ROOT / path).read_bytes()
    return {"Path": path, "Bytes": len(data), "Sha256": sha(data)}


old_path = "docs/validation/part-371/evidence/erlang371-binary-supplement-comparison1/comparison.json"
new_path = "artifacts/erlang372-final-baseline-comparisons1/binary-supplement/comparison.json"
published = subprocess.run(["git", "show", BASE + ":" + old_path], cwd=ROOT, check=True, capture_output=True).stdout
assert json.loads(published) == load(old_path), "Published witness differs from pinned Git commit"
old, new = load(old_path), load(new_path)
assert old["Rows"] == new["Rows"]
assert old["StrictEqual"] == new["StrictEqual"] == 214
assert old["ActualRows"] == new["ActualRows"] == 216
assert len(old["Inputs"]) == len(new["Inputs"]) == 8
input_checks = []
for before, after in zip(old["Inputs"], new["Inputs"]):
    before_data = (ROOT / before["Path"]).read_bytes()
    after_data = (ROOT / after["Path"]).read_bytes()
    assert sha(before_data) == before["Sha256"]
    assert sha(after_data) == after["Sha256"]
    assert before_data == after_data, "Raw ETF changed despite identical bounded diagnostic strings"
    input_checks.append({"PublishedInput": before["Path"], "FinalInput": after["Path"], "Bytes": len(after_data), "Sha256": sha(after_data), "ByteIdentical": True})
for key in old:
    if key != "Inputs":
        assert old[key] == new[key], key
diffs = [{"Group": row["Group"], "Case": row["Case"], "Depth": row["Depth"], "Fields": len(row["Differences"]), "Paths": [d["Path"] for d in row["Differences"]]} for row in new["Rows"] if not row["StrictEqual"]]
assert [(d["Case"], d["Depth"], d["Fields"]) for d in diffs] == [("live_stack_three", "values", 1), ("live_stack_four", "values", 3)]
completion_path = "artifacts/erlang372-final-baseline-comparisons1/completion.json"
completion = load(completion_path)
assert len(completion["Stages"]) == 8
assert [(s["Name"], s["ExitCode"]) for s in completion["Stages"] if s["ExitCode"]] == [("binary-supplement", 1)]
assert completion["AllRequestedAttempted"] and completion["SourceGuardsUnchanged"]
assert not completion["AllComparisonsPassed"]
stages = []
sources = [old_path, new_path, completion_path]
for stage in completion["Stages"]:
    directory = "artifacts/erlang372-final-baseline-comparisons1/" + stage["Name"]
    path = directory + ("/acceptance.json" if (ROOT / directory / "acceptance.json").exists() else "/comparison.json")
    data = load(path)
    sources.append(path)
    stages.append({"Name": stage["Name"], "ExitCode": stage["ExitCode"], "Summary": {k: v for k, v in data.items() if not isinstance(v, (list, dict))}})
nine_path = "artifacts/part372-nine-retained-comparison-proposal1/actual1/comparison.json"
nine = load(nine_path)
sources.append(nine_path)
assert (nine["Observations"], nine["StrictWholeEqual"], nine["AcceptedWholeFields"]) == (8117, 7683, 8087)
assert len(nine["Rows"]) == 8117 and not nine["Unexpected"]
assert len(nine["NegativeControls"]) == 68 and all(row["Rejected"] for row in nine["NegativeControls"])
not_accepted = [row for row in nine["Rows"] if not row["AcceptedWholeFields"]]
assert len(not_accepted) == 30 and len(nine["OwnTopologyPending"]) == 12
canonical_path = "artifacts/binary-integration369/ordsets372-canonical-tests2/execution.exit.json"
canonical_stdout_path = "artifacts/binary-integration369/ordsets372-canonical-tests2/execution.stdout.log"
canonical_stderr_path = "artifacts/binary-integration369/ordsets372-canonical-tests2/execution.stderr.log"
canonical = load(canonical_path)
assert canonical["ExitCode"] == 0 and canonical["Settled"] and not canonical["TimedOut"] and not canonical["Failures"]
assert sha((ROOT / canonical_stdout_path).read_bytes()) == canonical["StdoutSha256"]
assert sha((ROOT / canonical_stderr_path).read_bytes()) == canonical["StderrSha256"]
assert (ROOT / canonical_stdout_path).read_text(encoding="utf-8-sig").rstrip().endswith("7688/7688 passed")
sources.extend([canonical_path, canonical_stdout_path, canonical_stderr_path])
new_packet_path = "artifacts/part372-after-comparison3/acceptance.json"
new_packet = load(new_packet_path)
sources.append(new_packet_path)
receipt = {
    "Status": "PASS_BOUNDED_NO_NEW_BINARY_SUPPLEMENT_DIFFERENCES_NOT_ALL_EIGHT_STAGES_PASS",
    "PublishedBaselineCommit": BASE,
    "PublishedGitBlobSha256": sha(published),
    "ComparisonRowsExactlyEqual": True,
    "FullRawInputBytesExactlyEqual": True,
    "LegacyDifferencesNotWaived": diffs,
    "InputChecks": input_checks,
    "Stages": stages,
    "SevenStagesExitZero": True,
    "BinarySupplementExitCode": 1,
    "AllEightStagesPassed": False,
    "NineRetained": {k: v for k, v in nine.items() if not isinstance(v, (list, dict))},
    "NineRetainedUnexpected": 0,
    "NineRetainedRejectedMutationControls": 68,
    "NineRetainedNotAccepted": 30,
    "NineRetainedOwnTopologyPending": 12,
    "NewPacket": {k: v for k, v in new_packet.items() if not isinstance(v, (list, dict))},
    "NewPacketBoundary": "Healthy original ordsets/usort families only: 642 strict. Numeric 360 observations remain blocked by actual native collector abort; parent CI failure is preserved.",
    "Scope": "Passive review of existing bytes and receipts. No VM, build, network, normalization, projection, error masking, contract promotion or Main modification.",
    "CanonicalFullSuite": {"Passed": 7688, "Total": 7688, "ExitCode": 0, "ElapsedSeconds": canonical["ElapsedSeconds"], "StdoutAndStderrHashVerified": True, "ExecutionOwner": "Root; reviewed passively here"},
    "Sources": [binding(path) for path in dict.fromkeys(sources)],
}
(OUT / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"Status": receipt["Status"], "ReceiptSha256": sha((OUT / "receipt.json").read_bytes()), "Inputs": len(input_checks), "LegacyDifferenceCases": len(diffs)}))
