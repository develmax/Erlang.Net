"""Passive ZIP construction only after Root reviews the exact input-plan hash."""
from pathlib import Path
import argparse
import hashlib
import json
import zipfile

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
CHUNK = 1024 * 1024


def digest(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(CHUNK), b""):
            value.update(chunk)
    return value.hexdigest().upper()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--approved-plan-sha", required=True)
    args = parser.parse_args()
    plan_path = HERE / "input-plan.json"
    assert digest(plan_path) == args.approved_plan_sha.upper(), "Reviewed plan changed"
    plan = json.loads(plan_path.read_bytes())
    assert digest(Path(__file__).resolve()) == plan["ArchiveScriptSha256"], "Packager changed"
    assert plan["ArchiveContainsRemoteEvidence"] is False
    rows = plan["ArchiveRows"]
    blobs = {}
    for row in rows:
        source = (ROOT / row["SourcePath"]).resolve()
        assert source.is_relative_to(ROOT) and source.is_file()
        assert source.stat().st_size == row["Bytes"] and digest(source) == row["Sha256"]
        blobs.setdefault(row["Sha256"], row)
    index = {
        "Schema": "portable-content-addressed-zip-v1",
        "InputPlanSha256": digest(plan_path),
        "Scope": plan["Scope"],
        "RemoteEvidenceIncluded": False,
        "Rows": [dict(row, Blob="blobs/" + row["Sha256"]) for row in rows],
    }
    index_bytes = (json.dumps(index, indent=2) + "\n").encode("utf-8")
    expected = {"archive-index.json": hashlib.sha256(index_bytes).hexdigest().upper()}
    pending = HERE / "compiled315.pending.zip"
    assert not pending.exists() and not (HERE / "archive-replay-receipt.json").exists()
    with zipfile.ZipFile(pending, "x", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        archive.writestr("archive-index.json", index_bytes)
        for sha, row in sorted(blobs.items()):
            source = ROOT / row["SourcePath"]
            name = "blobs/" + sha
            value = hashlib.sha256()
            size = 0
            with source.open("rb") as stream, archive.open(name, "w", force_zip64=True) as output:
                for chunk in iter(lambda: stream.read(CHUNK), b""):
                    value.update(chunk)
                    size += len(chunk)
                    output.write(chunk)
            assert value.hexdigest().upper() == sha and size == row["Bytes"], row["SourcePath"]
            expected[name] = sha
    assert pending.stat().st_size < plan["MaximumArchiveBytes"]
    verified = []
    with zipfile.ZipFile(pending, "r") as archive:
        assert len(archive.infolist()) == len(expected)
        assert set(archive.namelist()) == set(expected)
        for member in archive.infolist():
            value = hashlib.sha256()
            size = 0
            with archive.open(member) as stream:
                for chunk in iter(lambda: stream.read(CHUNK), b""):
                    value.update(chunk)
                    size += len(chunk)
            assert value.hexdigest().upper() == expected[member.filename]
            verified.append({"Member": member.filename, "Bytes": size, "Sha256": expected[member.filename]})
    archive_sha = digest(pending)
    destination = HERE / ("compiled315-sha256-" + archive_sha + ".zip")
    assert not destination.exists()
    pending.rename(destination)
    receipt = {
        "Status": "PASS_PASSIVE_ARCHIVE_AND_SINGLE_PASS_MEMBER_REHASH",
        "Archive": destination.name,
        "ArchiveSha256": archive_sha,
        "ArchiveBytes": destination.stat().st_size,
        "InputPlanSha256": digest(plan_path),
        "LogicalRows": len(rows),
        "UniqueBlobs": len(blobs),
        "MembersRehashedExactlyOnce": len(verified),
        "Members": verified,
        "MainEdited": False,
        "GitAction": False,
        "SdkOrVmExecuted": False,
        "FocusReexecuted": False,
        "RemoteEvidenceCopied": False,
    }
    (HERE / "archive-replay-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: value for key, value in receipt.items() if key != "Members"}, indent=2))


if __name__ == "__main__":
    main()
