"""Read-only local Git blob verification; no VM or downloaded code execution."""
from pathlib import Path
import hashlib
import json
import subprocess

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
HEAD = "672e4424ec9f0238be80ae14e6e077018adcb933"
SOURCE = None
TOOL = ROOT / "artifacts/binary-original369-original1/tool"


def main():
    import argparse
    p=argparse.ArgumentParser();p.add_argument('--artifact',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    sources=list(a.artifact.rglob('source-before.json'));assert len(sources)==1
    global SOURCE,OUT
    SOURCE=sources[0];OUT=a.output.resolve();assert not OUT.exists() and OUT.is_relative_to(Path(__file__).resolve().parent)
    OUT.mkdir()
    rows = json.loads(SOURCE.read_bytes())["TrackedFiles"]
    tree = subprocess.check_output(["git", "-C", str(ROOT.parent / "otp"), "ls-tree", "-r", "-z", "--full-tree", PIN])
    blobs = {}
    for entry in tree.split(b"\0"):
        if not entry:
            continue
        meta, path = entry.split(b"\t", 1)
        mode, kind, oid = meta.split(b" ")
        assert kind == b"blob"
        blobs[path.decode()] = (mode.decode(), oid.decode())
    assert len(blobs) == len(rows) == 12008
    process = subprocess.Popen(["git", "-C", str(ROOT.parent / "otp"), "cat-file", "--batch"],
                               stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    verified = []
    try:
        for row in rows:
            mode, oid = blobs[row["Path"]]
            assert mode == row["Mode"]
            process.stdin.write((oid + "\n").encode())
            process.stdin.flush()
            header = process.stdout.readline().decode().split()
            assert header[:2] == [oid, "blob"] and int(header[2]) == row["Bytes"]
            remaining = row["Bytes"]
            digest = hashlib.sha256()
            while remaining:
                data = process.stdout.read(min(remaining, 1048576))
                assert data
                digest.update(data)
                remaining -= len(data)
            assert process.stdout.read(1) == b"\n"
            assert digest.hexdigest().upper() == row["Sha256"]
            verified.append(dict(Path=row["Path"], Mode=mode, Blob=oid, Bytes=row["Bytes"], Sha256=row["Sha256"]))
        process.stdin.close()
        assert process.wait(timeout=15) == 0 and not process.stderr.read()
    finally:
        if process.poll() is None:
            process.kill()
            process.wait(timeout=15)
    pinned = dict(Status="PASS_ALL12008_PINNED_GIT_BLOBS", Commit=PIN, Files=verified)
    (OUT / "pinned-blobs.json").write_text(json.dumps(pinned, indent=2) + "\n")
    plan = json.loads((TOOL / "source-inputs.corrected.json").read_bytes())
    bindings = json.loads((ROOT / "artifacts/binary-original369-original1/ci/dispatch-intent.json").read_bytes())
    inputs = {row["Path"]: row for row in plan["RepositoryInputs"]}
    for row in plan["ToolInputs"]:
        inputs["tools/Erlang.ProplistFamilyObservations/" + row["Path"]] = row
    for row in bindings["Bindings"]:
        inputs[row["Path"]] = row
    checkout = ROOT
    current = subprocess.check_output(["git", "-C", str(checkout), "rev-parse", "HEAD"]).decode().strip()
    checked = []
    for path, row in inputs.items():
        data = subprocess.check_output(["git", "-C", str(checkout), "show", HEAD + ":" + path])
        assert len(data) == row["Bytes"] and hashlib.sha256(data).hexdigest().upper() == row["Sha256"]
        checked.append(dict(Path=path, Bytes=len(data), Sha256=row["Sha256"], ExactProducerGitBlob=True))
    producer = dict(Status="PASS_COMMITTED_PRODUCER_INPUTS", Head=HEAD, CurrentCheckoutHead=current,
                    CurrentHeadMatches=current == HEAD, RepositoryInputs=len(plan["RepositoryInputs"]),
                    ToolInputs=len(plan["ToolInputs"]), Files=checked, GitReadOnly=True)
    (OUT / "producer-authority.json").write_text(json.dumps(producer, indent=2) + "\n")
    print(json.dumps(dict(PinnedBlobs=len(verified), ProducerGitInputs=len(checked), CurrentHeadMatches=current == HEAD)))


if __name__ == "__main__":
    main()
