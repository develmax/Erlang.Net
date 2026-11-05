"""Admit only complete, current-source, independently bound Unicode comparisons."""
import hashlib
import json
import pathlib
import subprocess
import sys

root = pathlib.Path(__file__).resolve().parents[2]
directory = pathlib.Path(sys.argv[1]).resolve()
destination = pathlib.Path(sys.argv[2]).resolve()


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def read(name):
    return json.loads((directory / name).read_text(encoding="utf-8-sig"))


catalog = read("api-source-catalog.json")
assert catalog["ExportCount"] == 22
for name, expected in catalog["Sources"].items():
    assert digest(root / name) == expected, f"Implementation source changed: {name}"
provenance = read("provenance.json")
assert provenance["Baseline"] == "OTP-29.1.1"
assert provenance["Commit"] == "ad05823719d77c8faee87348ea39513d4e2f99c5"
assert provenance["ObserverSourceSha256"] == digest(root / "tools/Validation/Erlang.UnicodeCompatibility/unicode_port_cases.erl")
assert provenance["ObserverBeamSha256"] == digest(directory / "unicode_port_cases.beam")
assert provenance["CorpusSha256"] == digest(directory / "original.etf")
partition = read("partition-provenance.json")
assert partition["OriginalSha256"] == provenance["CorpusSha256"]
assert len(partition["Parts"]) == 4
for part in partition["Parts"]:
    assert part["Sha256"] == digest(directory / part["Name"])

# Independently reconstruct the exact ETF byte stream with the admitted provider.
admission = json.loads((root / "artifacts/library376-official-compiler/official-admission.json").read_text(encoding="utf-8-sig"))
assert digest(pathlib.Path(admission["ExecutionManifestPath"])) == admission["ExecutionManifestSha256"]
for entry in provenance["Inputs"]:
    assert digest(pathlib.Path(admission["InstalledOtpRoot"]) / entry["Path"]) == entry["Sha256"]
expression = 'D=' + json.dumps(directory.as_posix()) + ',{ok,B}=file:read_file(filename:join(D,"original.etf")),' + \
    'Parts=[begin {ok,P}=file:read_file(filename:join(D,"original-part-"++integer_to_list(I)++".etf")),binary_to_term(P) end||I<-lists:seq(1,4)],' + \
    'B=term_to_binary(lists:append(Parts)),halt().'
subprocess.run([str(pathlib.Path(admission["InstalledOtpRoot"]) / "bin/erl.exe"), "-noshell", "-eval", expression], check=True)

runner = root / "tools/Validation/Erlang.UnicodeCompatibility/bin/Debug/net10.0"
stdlib_hash = digest(runner / "Erlang.Otp.Stdlib.dll")
runtime_hash = digest(runner / "Erlang.Runtime.dll")
reports = []
for backend in ("Managed", "Beam"):
    corpora = ["contracts.etf", "extra.etf", "upstream-suite-original.etf"]
    corpora += ["original.etf"] if backend == "Managed" else [f"original-part-{index}.etf" for index in range(1, 5)]
    for corpus in corpora:
        name = f"{backend}-report.json" if corpus == "original.etf" else f"{backend}-{corpus}-report.json"
        report = read(name)
        assert report["CompleteCorpus"] and report["Cases"] == report["Passed"]
        assert report["BackingMatched"] and report["BackingProbes"] == 29
        assert report["Failures"] == []
        assert report["Backend"] == backend and report["Baseline"] == "OTP-29.1.1"
        assert report["Exports"] == 22
        assert report["CorpusSha256"] == digest(directory / corpus)
        assert report["ManagedAssemblySha256"] == stdlib_hash
        assert report["RuntimeAssemblySha256"] == runtime_hash
        reports.append({"Backend": backend, "Corpus": corpus, "Cases": report["Cases"], "Values": report["Values"],
                        "Exceptions": report["Exceptions"], "Report": name, "ReportSha256": digest(directory / name)})
    assert sum(row["Cases"] for row in reports if row["Backend"] == backend and row["Corpus"].startswith("original")) == 65339

tests = []
for name, count in [("unicode-linked-tests-initial.json", 3), ("unicode-shared-runtime-tests.json", 8), ("unicode-string-boundary-tests.json", 8)]:
    path = root / "artifacts" / name
    result = json.loads(path.read_text(encoding="utf-8-sig"))
    assert result["Partial"] and result["SelectedCount"] == count
    # Preserve the harness's own pass/fail schema instead of guessing a full run.
    assert result.get("Failed", result.get("failed", -1)) == 0, name
    tests.append({"Report": name, "Cases": count, "Sha256": digest(path)})
examples = []
for backend in ("Managed", "Beam"):
    path = root / "artifacts" / f"unicode-example-{backend}.log"
    example = json.loads(path.read_text(encoding="utf-8-sig"))
    assert example["Backend"] == backend
    examples.append(example)
assert examples[0]["Result"] == examples[1]["Result"]

summary = {"Baseline": provenance["Baseline"], "Commit": provenance["Commit"], "Exports": 22,
           "Implementation": "Complete exported module surface; original Erlang preserved; explicit selection",
           "CompatibilityStatus": "Partially compatible; finite scoped evidence, universal resource/platform equivalence not certified",
           "PrimaryObservationsPerBackend": 65339 + 711 + 408, "PermanentSubsetPerBackend": 490, "BackingProbesPerBackend": 29,
           "Reports": reports, "FocusedRegression": tests, "FocusedRegressionGroups": 19,
           "ManagedAssemblySha256": stdlib_hash, "RuntimeAssemblySha256": runtime_hash,
           "ExactPartitionReconstruction": True, "Examples": examples,
           "FullRegressionRun": False, "ReadinessRegenerated": False,
           "Reason": "User requested only connected focused checks; partial reports cannot refresh whole-project readiness",
           "Publication": "No scoped commit/push/CI; pre-existing unrelated dirty work remains preserved"}
destination.write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
print(f"Unicode admission: 22 exports, {summary['PrimaryObservationsPerBackend']} primary observations per backend, 19 focused groups")
