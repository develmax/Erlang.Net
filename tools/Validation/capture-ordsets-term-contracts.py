"""Capture the term matrix from the admitted, unchanged OTP provider."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
FIXTURE = ROOT / "tests/Erlang.Tests/Resources/OrdsetsTermContracts"
OUTPUT = ROOT / "artifacts/ordsets-term-contracts"
ADMISSION = ROOT / "artifacts/library376-official-compiler/official-admission.json"
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


check = sys.argv[1:] == ["--check"]
if sys.argv[1:] and not check:
    raise SystemExit("Usage: capture-ordsets-term-contracts.py [--check]")
profile = json.loads(ADMISSION.read_text(encoding="utf-8-sig"))
assert profile["SourceCommit"] == PIN and profile["EveryExtractedFileMatchesArchive"]
manifest = Path(profile["ExecutionManifestPath"])
assert sha(manifest) == profile["ExecutionManifestSha256"]
files = json.loads(manifest.read_text(encoding="utf-8-sig"))
provider = Path(profile["InstalledOtpRoot"])


def verify():
    for row in files:
        assert sha(provider / row["Path"]) == row["Sha256"], row["Path"]


verify()
OUTPUT.mkdir(parents=True, exist_ok=True)
destination = OUTPUT / "matrix-repeat.etf" if check else FIXTURE / "matrix.etf"
if check:
    retained = json.loads((FIXTURE / "provenance.json").read_text())
    for name, digest in retained["Files"].items():
        assert sha(FIXTURE / name) == digest, name
compile_expression = "" if check else (
    '{ok,ordsets_term_contracts}=compile:file("ordsets_term_contracts.erl",'
    '[debug_info,report_errors,report_warnings]),'
)
expression = compile_expression + (
    '{Pool,Rows}=ordsets_term_contracts:run(),'
    'ok=file:write_file(' + json.dumps(destination.as_posix()) +
    ',term_to_binary(Rows,[deterministic])),'
    'io:format("~p terms, ~p pairs~n",[length(Pool),length(Rows)]),halt().'
)
result = subprocess.run(
    [str(provider / "bin/erl.exe"), "+S", "2:2", "-noshell", "-eval", expression],
    cwd=FIXTURE, capture_output=True, timeout=120, check=False,
)
prefix = "terms-oracle-repeat" if check else "terms-oracle"
(OUTPUT / (prefix + ".stdout")).write_bytes(result.stdout)
(OUTPUT / (prefix + ".stderr")).write_bytes(result.stderr)
assert result.returncode == 0, result.stdout.decode(errors="replace") + result.stderr.decode(errors="replace")
verify()
receipt = {
    "PinnedOtpCommit": PIN,
    "ProviderAdmissionSha256": sha(ADMISSION),
    "ExecutionManifestSha256": sha(manifest),
    "ProviderFilesVerifiedBeforeAndAfter": len(files),
    "ExitCode": result.returncode,
    "Stdout": result.stdout.decode(errors="replace"),
    "InputProtocol": "Replay unchanged pool/0 BEAM; ETF transports scalar expected matrix only",
    "Contracts": ["term order", "numeric equality", "exact equality", "ordsets membership", "ordsets sortedness"],
    "Files": {name: sha(FIXTURE / name) for name in ["ordsets_term_contracts.erl", "ordsets_term_contracts.beam", "matrix.etf"]},
}
if check:
    assert destination.read_bytes() == (FIXTURE / "matrix.etf").read_bytes()
    receipt["WholeMatrixReproducedByteForByte"] = True
    target = OUTPUT / "terms-oracle-repeat.json"
else:
    target = FIXTURE / "provenance.json"
target.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
print(receipt["Stdout"].strip())
