"""Run and join the complete Windows runtime regression across isolated runners."""

import argparse
import json
from pathlib import Path
import shutil
import subprocess


BLOCKS = {
    "runtime-terms": {"terms"},
    "runtime-tooling": {"tooling"},
    "runtime-platform": {"compiler", "processes", "ports-distribution", "otp", "native"},
}
HANDOFF = Path("artifacts/runtime-shards")
EVIDENCE_DIRS = tuple(Path(f"library{number}/permanent") for number in (477, 479, 480, 481, 482, 483, 484, 485))
ASSEMBLY = "tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll"


def read_json(path):
    return json.loads(path.read_bytes())


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def require(condition, message):
    if not condition:
        raise ValueError(message)


def run(shard, head, runtime, reuse):
    HANDOFF.mkdir(parents=True, exist_ok=True)
    if not reuse:
        host = ["dotnet", "exec", "--fx-version", runtime, ASSEMBLY]
        subprocess.run(
            [*host, "--list", "--report", str(HANDOFF / "catalog.json")],
            stdout=subprocess.DEVNULL,
            check=True,
        )
        selection = [value for block in sorted(BLOCKS[shard]) for value in ("--block", block)]
        subprocess.run([*host, *selection, "--report", str(HANDOFF / "partial.json")], check=True)
        for folder in EVIDENCE_DIRS:
            source = Path("artifacts") / folder
            if source.is_dir():
                shutil.copytree(source, HANDOFF / folder)
    write_json(HANDOFF / "receipt.json", {"Head": head, "Shard": shard, "Reuse": reuse})


def merge(root, head):
    sources = {shard: root / f"runtime-handoff-{shard}" for shard in BLOCKS}
    receipts = {shard: read_json(source / "receipt.json") for shard, source in sources.items()}
    for shard, receipt in receipts.items():
        require(receipt.get("Head") == head and receipt.get("Shard") == shard, f"Invalid receipt for {shard}")
        require(type(receipt.get("Reuse")) is bool, f"Missing reuse decision for {shard}")
    reuse = {receipt["Reuse"] for receipt in receipts.values()}
    require(len(reuse) == 1, "Runtime shard reuse decisions disagree")
    if reuse == {True}:
        return "reused"

    catalog = None
    results = {}
    for shard, source in sources.items():
        listing = read_json(source / "catalog.json")
        rows = listing["Tests"]
        require(listing["Listed"] is True and listing["Partial"] is False, f"Incomplete catalog in {shard}")
        require(listing["TotalCount"] == listing["SelectedCount"] == len(rows) > 0, f"Invalid catalog size in {shard}")
        require(len({row["Name"] for row in rows}) == len(rows), f"Duplicate catalog name in {shard}")
        if catalog is None:
            catalog = listing
        require(listing == catalog, f"Catalog mismatch in {shard}")

        report = read_json(source / "partial.json")
        expected = [row["Name"] for row in rows if row["Block"] in BLOCKS[shard]]
        names = [row["Name"] for row in report["Tests"]]
        require(report["Partial"] is True and not report["ExactCases"], f"Invalid selection for {shard}")
        require(sorted(BLOCKS[shard]) == report["Blocks"], f"Block mismatch in {shard}")
        require(report["TotalCount"] == len(rows) and report["SelectedCount"] == len(expected), f"Count mismatch in {shard}")
        require(names == expected and not results.keys() & set(names), f"Missing, repeated or unexpected tests in {shard}")
        require(
            report["Failed"] == 0 and report["Passed"] == len(names) and all(row["Status"] == "Passed" for row in report["Tests"]),
            f"Unsuccessful tests in {shard}",
        )
        results.update((row["Name"], row) for row in report["Tests"])

    expected = [row["Name"] for row in catalog["Tests"]]
    require(results.keys() == set(expected), "Incomplete runtime regression")

    for source in sources.values():
        for folder in EVIDENCE_DIRS:
            for file in (source / folder).rglob("*"):
                if file.is_file():
                    target = Path("artifacts") / file.relative_to(source)
                    require(not target.exists(), f"Duplicate runtime evidence {target}")
                    target.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copyfile(file, target)

    write_json(Path("artifacts/tests.json"), {"Passed": len(expected), "Failed": 0, "Tests": [results[name] for name in expected]})
    return "fresh"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    worker = commands.add_parser("run")
    worker.add_argument("--shard", choices=BLOCKS, required=True)
    worker.add_argument("--head", required=True)
    worker.add_argument("--runtime", required=True)
    worker.add_argument("--reuse", action="store_true")
    join = commands.add_parser("merge")
    join.add_argument("--root", type=Path, required=True)
    join.add_argument("--head", required=True)
    args = parser.parse_args()
    if args.command == "run":
        run(args.shard, args.head, args.runtime, args.reuse)
    else:
        print(merge(args.root, args.head))


if __name__ == "__main__":
    main()
