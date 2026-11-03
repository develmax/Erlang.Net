"""Capture the unchanged observer against the admitted local OTP oracle."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
FIXTURE = ROOT / "tests/Erlang.Tests/Resources/OrdsetsCompatibility"
ADMISSION = ROOT / "artifacts/library376-official-compiler/official-admission.json"
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


profile = json.loads(ADMISSION.read_text(encoding="utf-8-sig"))
assert profile["SourceCommit"] == PIN and profile["EveryExtractedFileMatchesArchive"]
manifest = Path(profile["ExecutionManifestPath"])
assert sha(manifest) == profile["ExecutionManifestSha256"]
provider = Path(profile["InstalledOtpRoot"])
files = json.loads(manifest.read_text(encoding="utf-8-sig"))


def verify_provider():
    for row in files:
        assert sha(provider / row["Path"]) == row["Sha256"], row["Path"]


verify_provider()
check_only = sys.argv[1:] == ["--check"]
if sys.argv[1:] and not check_only:
    raise SystemExit("Usage: capture-ordsets-compatibility.py [--check]")
output = ROOT / "artifacts/ordsets-verification"
output.mkdir(parents=True, exist_ok=True)
destination = output / "original-repeat.etf" if check_only else FIXTURE / "original.etf"
compiler = (
    '{ok,ordsets_compatibility_observer}=compile:file('
    '"ordsets_compatibility_observer.erl",[debug_info,report_errors,report_warnings]),'
    '{ok,ordsets_core_worker}=compile:file("ordsets_core_worker.erl",[debug_info]),'
)
if check_only:
    retained = json.loads((FIXTURE / "provenance.json").read_text(encoding="utf-8"))
    for name, digest in retained["Files"].items():
        assert sha(FIXTURE / name) == digest, name
expression = ("" if check_only else compiler) + (
    'Cases=ordsets_compatibility_observer:cases(),'
    'Rows=ordsets_core_worker:run(ordsets_compatibility_observer,run),'
    'FromInputs=ordsets_core_worker:run(ordsets_compatibility_observer,run,[Cases]),'
    'Bytes=term_to_binary(Rows,[deterministic]),'
    'Bytes=term_to_binary(FromInputs,[deterministic]),'
    'ok=file:write_file("inputs.etf",term_to_binary(Cases,[deterministic])),'
    'ok=file:write_file(' + json.dumps(destination.as_posix()) + ',term_to_binary(Rows,[deterministic])), '
    'io:format("~p observations~n",[length(Rows)]),halt().'
)
result = subprocess.run(
    [str(provider / "bin/erl.exe"), "+S", "2:2", "-noshell", "-eval", expression],
    cwd=FIXTURE, capture_output=True, timeout=120, check=False,
)
prefix = "oracle-repeat" if check_only else "oracle"
(output / (prefix + ".stdout")).write_bytes(result.stdout)
(output / (prefix + ".stderr")).write_bytes(result.stderr)
assert result.returncode == 0, result.stderr.decode(errors="replace")
verify_provider()
receipt = {
    "PinnedOtpCommit": PIN,
    "ProviderAdmissionSha256": sha(ADMISSION),
    "ExecutionManifestSha256": sha(manifest),
    "ProviderFilesVerifiedBeforeAndAfter": len(files),
    "ExitCode": result.returncode,
    "Stdout": result.stdout.decode(errors="replace"),
    "Comparison": "Entire deterministic ETF, including values and complete exception stacks",
    "Files": {p.name: sha(p) for p in sorted(FIXTURE.iterdir()) if p.suffix in (".erl", ".beam", ".etf")},
}
if check_only:
    assert destination.read_bytes() == (FIXTURE / "original.etf").read_bytes()
    receipt["WholeCorpusReproducedByteForByte"] = True
    (output / "oracle-repeat.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
else:
    (FIXTURE / "provenance.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
print(receipt["Stdout"].strip())
