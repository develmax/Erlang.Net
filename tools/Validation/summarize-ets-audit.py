"""Admit complete focused ETS receipts; deliberately never certify universal OTP parity."""
import hashlib
import json
import sys
from pathlib import Path

directory = Path(sys.argv[1]).resolve()
runner = Path(sys.argv[2]).resolve()

def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()

def require(condition, message):
    if not condition:
        raise SystemExit(message)

provenance = json.loads((directory / "provenance.json").read_text(encoding="utf-8-sig"))
require(provenance["Baseline"] == "OTP-29.1.1" and provenance["Commit"] == "ad05823719d77c8faee87348ea39513d4e2f99c5", "Wrong OTP baseline")
ets_sha = next(row["Sha256"] for row in provenance["Inputs"] if row["Path"].endswith("/ebin/ets.beam"))
library_sha = digest(runner.parent / "Erlang.Otp.Stdlib.dll")
runtime_sha = digest(runner.parent / "Erlang.Runtime.dll")
reports = []
for backend in ("Managed", "Beam"):
    for suffix, cases, effects in (("", 355, 8), ("-browse", 0, 12), ("-files", 0, 76)):
        name = backend + suffix + "-report.json"
        row = json.loads((directory / name).read_text(encoding="utf-8-sig"))
        require(row["Backend"] == backend and row["CompleteCorpus"], name + ": partial or wrong backend")
        require((row["PublicExports"], row["LibraryExports"], row["NativeExports"]) == (76, 25, 51), name + ": incomplete surface")
        require((row["Cases"], row["Passed"], row["EffectCases"], row["Effects"]) == (cases, cases, effects, effects), name + ": incomplete or failed cases")
        require(not row["Failures"] and row["RegexDataBalanced"], name + ": failed observation or native data leak")
        require(row["ManagedAssemblySha256"] == library_sha and row["RuntimeAssemblySha256"] == runtime_sha, name + ": stale assembly")
        require(row["EtsBeamSha256"] == ets_sha, name + ": unpinned original ETS")
        for field, path in (("CorpusSha256", "original.etf"), ("EffectsSha256", row["EffectFile"]),
                            ("FeatureStateSha256", "features.etf"), ("ObserverBeamSha256", "ets_port_cases.beam"),
                            ("DeviceBeamSha256", "ets_port_device.beam")):
            require(row[field] == digest(directory / path), name + ": changed " + path)
        if suffix == "-files":
            require(row["ExportedDataMatched"] is True, name + ": failed dump export")
            require(row["ExportedDumpSha256"] == digest(directory / (backend + ".dump")), name + ": changed dump")
        reports.append({"Report": name, "Cases": cases, "Effects": effects})

interop = json.loads((directory / "interoperability.json").read_text(encoding="utf-8-sig"))
require(interop["Passed"] and interop["ExecutionManifestSha256"] == provenance["ExecutionManifestSha256"], "Unverified export provider")
for field, name in (("ManagedDumpSha256", "Managed.dump"), ("BeamDumpSha256", "Beam.dump"),
                    ("OutcomeSha256", "export-otp.etf"), ("ObserverBeamSha256", "ets_port_cases.beam")):
    require(interop[field] == digest(directory / name), "Changed cross-OTP input/outcome: " + name)
require((directory / "export-otp.etf").read_bytes() in (b"\x83w\x02ok", b"\x83d\x00\x02ok", b"\x83v\x00\x02ok"), "OTP export checks did not return ok")
require(provenance["OriginalDumpSha256"] == digest(directory / "original.dump"), "Original dump was modified")

file_gates = []
for backend in ("Managed", "Beam"):
    path = directory / (backend + "-files-report.json")
    if path.exists():
        row = json.loads(path.read_text(encoding="utf-8-sig"))
        file_gates.append({"Backend": backend, "Cases": row["EffectCases"], "Passed": row["Effects"], "Failures": len(row["Failures"])})
summary = {
    "Baseline": provenance["Baseline"], "PublicExports": 76, "LibraryExports": 25, "NativeExports": 51,
    "FocusedAccepted": True, "FullOtpCompatibility": False,
    "Reports": reports, "FilesystemGates": file_gates,
    "DumpInteroperabilityAccepted": True,
    "Remaining": ["Full upstream ETS suite and platform/resource/scheduling/metadata/whole-contract acceptance",
                  "Whole startup-manifest dependency closure and exact scoped publication"],
    "Unrun": ["Full regression", "Global readiness refresh", "Publication CI/review"],
    "ManagedAssemblySha256": library_sha, "RuntimeAssemblySha256": runtime_sha,
    "ValidatorAssemblySha256": digest(runner)
}
(directory / "summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
print("Focused ETS receipts admitted; full OTP compatibility remains unfinished.")
