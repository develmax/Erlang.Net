"""Admit complete, source-bound focused reports for both gen_statem backends."""
import hashlib
import json
from pathlib import Path
import sys
from gen_statem_validation_contract import (BASELINE, COMMIT, EXPORT_COUNT, API_CASE_COUNT, ENGINE_SCENARIO_COUNT, BACKENDS, MANAGED_BACKEND, REPORT_SUFFIX, PROVENANCE_FILE, ORIGINAL_MODULE, CORPUS_DIGESTS)

repository, directory, audit_file, output = map(Path, sys.argv[1:])
repository = repository.resolve()
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest().upper()
audit = json.loads(audit_file.read_text(encoding="utf-8"))
provenance = json.loads((directory / PROVENANCE_FILE).read_text(encoding="utf-8-sig"))
assert audit["ExactSourceBytesUnchanged"] and audit["ExportsMatch"] and audit["ExportCount"] == EXPORT_COUNT
assert provenance["Baseline"] == audit["Baseline"] == BASELINE
assert provenance["Commit"] == audit["Commit"] == COMMIT
for path, digest in audit["ManagedSources"].items():
    assert sha(repository / path) == digest, f"Source changed: {path}"
reports = {}
for backend in BACKENDS:
    report = json.loads((directory / (backend + REPORT_SUFFIX)).read_text(encoding="utf-8-sig"))
    assert report["Backend"] == backend and report["Complete"] and not report["Failures"]
    assert report["Cases"] == report["Passed"] == API_CASE_COUNT
    assert report["EffectCases"] == report["EffectsRun"] == report["EffectPassed"] == ENGINE_SCENARIO_COUNT
    assert report["Exports"] == EXPORT_COUNT and report["OriginalSha256"] == audit["OriginalBeamSha256"]
    assert report["ManagedSources"] == audit["ManagedSources"]
    for key, file in CORPUS_DIGESTS:
        assert report[key] == provenance[key] == sha(directory / file)
    own_images = [image for image in report["HelperImages"] if image["Name"] == ORIGINAL_MODULE]
    assert not own_images if backend == MANAGED_BACKEND else len(own_images) == 1
    if own_images:
        assert own_images[0]["OriginalBeamSha256"] == audit["OriginalBeamSha256"]
    reports[backend] = report
for key in ("ManagedAssemblySha256", "RuntimeAssemblySha256", "BootSubjectSha256"):
    assert reports["Managed"][key] == reports["Beam"][key]
receipt = {
    "Baseline": audit["Baseline"], "Commit": audit["Commit"],
    "Exports": EXPORT_COUNT, "ApiCasesPerBackend": API_CASE_COUNT, "EngineScenariosPerBackend": ENGINE_SCENARIO_COUNT,
    "BothFocusedReportsComplete": True, "UniversalOtpCompatibilityCertified": False,
    "SourceAuditSha256": sha(audit_file), "ProvenanceSha256": sha(directory / PROVENANCE_FILE),
    "Reports": {backend: sha(directory / (backend + REPORT_SUFFIX)) for backend in reports},
    "ManagedAssemblySha256": reports["Managed"]["ManagedAssemblySha256"],
    "RuntimeAssemblySha256": reports["Managed"]["RuntimeAssemblySha256"],
    "Scope": "Same-platform complete terms and stacks in the focused corpus; full regression and global readiness deliberately unrun."
}
output.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
print(f"Both backends: {API_CASE_COUNT} API observations + {ENGINE_SCENARIO_COUNT} engine scenarios each; exact source and assembly bindings admitted.")
