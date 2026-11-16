"""Admit complete, source-bound focused reports for both gen_statem backends."""
import hashlib
import json
from pathlib import Path
import sys

repository, directory, audit_file, output = map(Path, sys.argv[1:])
repository = repository.resolve()
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest().upper()
audit = json.loads(audit_file.read_text(encoding="utf-8"))
provenance = json.loads((directory / "provenance.json").read_text(encoding="utf-8-sig"))
assert audit["ExactSourceBytesUnchanged"] and audit["ExportsMatch"] and audit["ExportCount"] == 40
assert provenance["Baseline"] == audit["Baseline"] == "OTP-29.1.1"
assert provenance["Commit"] == audit["Commit"] == "ad05823719d77c8faee87348ea39513d4e2f99c5"
for path, digest in audit["ManagedSources"].items():
    assert sha(repository / path) == digest, f"Source changed: {path}"
reports = {}
for backend in ("Managed", "Beam"):
    report = json.loads((directory / (backend + "-report.json")).read_text(encoding="utf-8-sig"))
    assert report["Backend"] == backend and report["Complete"] and not report["Failures"]
    assert report["Cases"] == report["Passed"] == 74
    assert report["EffectCases"] == report["EffectsRun"] == report["EffectPassed"] == 311
    assert report["Exports"] == 40 and report["OriginalSha256"] == audit["OriginalBeamSha256"]
    assert report["ManagedSources"] == audit["ManagedSources"]
    for key, file in (("CorpusSha256", "original.etf"), ("EffectsSha256", "effects.etf"),
                      ("FeatureBootstrapSha256", "feature-bootstrap.etf")):
        assert report[key] == provenance[key] == sha(directory / file)
    own_images = [image for image in report["HelperImages"] if image["Name"] == "gen_statem"]
    assert not own_images if backend == "Managed" else len(own_images) == 1
    if own_images:
        assert own_images[0]["OriginalBeamSha256"] == audit["OriginalBeamSha256"]
    reports[backend] = report
for key in ("ManagedAssemblySha256", "RuntimeAssemblySha256", "BootSubjectSha256"):
    assert reports["Managed"][key] == reports["Beam"][key]
receipt = {
    "Baseline": audit["Baseline"], "Commit": audit["Commit"],
    "Exports": 40, "ApiCasesPerBackend": 74, "EngineScenariosPerBackend": 311,
    "BothFocusedReportsComplete": True, "UniversalOtpCompatibilityCertified": False,
    "SourceAuditSha256": sha(audit_file), "ProvenanceSha256": sha(directory / "provenance.json"),
    "Reports": {backend: sha(directory / (backend + "-report.json")) for backend in reports},
    "ManagedAssemblySha256": reports["Managed"]["ManagedAssemblySha256"],
    "RuntimeAssemblySha256": reports["Managed"]["RuntimeAssemblySha256"],
    "Scope": "Same-platform complete terms and stacks in the focused corpus; full regression and global readiness deliberately unrun."
}
output.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
print("Both backends: 74 API observations + 311 engine scenarios each; exact source and assembly bindings admitted.")
