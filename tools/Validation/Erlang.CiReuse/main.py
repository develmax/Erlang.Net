"""Workflow entry: capture, admit or publish provenance; errors require full work."""
import argparse
import json
import os
import urllib.error
from pathlib import Path

from collector import collect
from fingerprint import capture


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def diagnostic(error):
    # HTTP exception strings may contain temporary signed download URLs.
    if isinstance(error, urllib.error.HTTPError):
        return "HTTPError: status " + str(error.code)
    if isinstance(error, urllib.error.URLError):
        return "URLError: evidence transport unavailable"
    return type(error).__name__ + ": " + str(error)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["prepare", "seal"])
    parser.add_argument("--repository", default=".")
    parser.add_argument("--provider-admission", required=True)
    parser.add_argument("--job", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    output = Path(args.output)
    # A repeated preparation/seal must not upload a stale successful receipt.
    receipt_path = output / "receipt.json"
    if receipt_path.exists():
        receipt_path.unlink()
    if args.mode == "prepare":
        decision = {"Reuse": False, "Reasons": []}
        try:
            fingerprint = capture(args.repository, args.provider_admission)
            write(output / "inputs.json", {"Fingerprint": fingerprint, "FingerprintComplete": True})
            decision = collect(Path(args.repository), fingerprint, args.job)
        except Exception as error:
            # A failed admission never suppresses an existing required validation step.
            decision = {"Reuse": False, "Reasons": [diagnostic(error)]}
        write(output / "decision.json", decision)
        github_output = os.environ.get("GITHUB_OUTPUT")
        if github_output:
            with open(github_output, "a", encoding="utf-8") as stream:
                stream.write("reuse=" + str(decision["Reuse"]).lower() + "\n")
        print(json.dumps({"Reuse": decision["Reuse"], "Reasons": decision.get("Reasons", [])}))
    else:
        # Called only after all the existing required job stages succeed.
        # If admission cannot be recaptured, no reusable receipt is issued.
        try:
            before = json.loads((output / "inputs.json").read_text(encoding="utf-8"))
            after = capture(args.repository, args.provider_admission)
            if before["Fingerprint"] != after:
                raise ValueError("Validation environment/provider changed during the job")
            value = {"Schema": 1, "Completed": True, "Job": args.job,
                     "Run": int(os.environ["GITHUB_RUN_ID"]), "Attempt": int(os.environ["GITHUB_RUN_ATTEMPT"]),
                     "Head": os.environ.get("EXPECTED_HEAD", os.environ["GITHUB_SHA"]),
                     "Fingerprint": after, "FingerprintComplete": True}
            write(output / "receipt.json", value)
            print("Reusable input receipt sealed; GitHub job/workflow conclusions remain authoritative")
        except Exception as error:
            print("No reusable receipt issued: " + diagnostic(error))


if __name__ == "__main__":
    main()
