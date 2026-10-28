"""Finite input/byte custody helpers for the existing atomics observer, not a collector."""
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess

VERSION = "10.0.11"
STRICT = "BothWholeDefaultAndDeterministicETFBytes"
OWNREF = "WholeETFWithExactSixNativeAtomicsReferenceCorrespondence"


def require(value, why):
    if not value:
        raise ValueError(why)


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def files(root):
    root = Path(root).resolve()
    require(root.is_dir(), "Closure directory missing: " + str(root))
    rows = []
    for path in sorted(root.rglob("*")):
        if path.is_file():
            require(path.resolve().is_relative_to(root), "Closure symlink escapes admitted directory")
            rows.append(dict(Path=path.relative_to(root).as_posix(), Bytes=path.stat().st_size, Sha256=digest(path)))
    require(rows, "Empty byte closure")
    return rows


def managed_before(host, output):
    host = Path(host).resolve()
    require(host.is_file(), "Managed host missing")
    require(host.with_suffix(".runtimeconfig.json").is_file() and host.with_suffix(".deps.json").is_file(),
            "Actual runtimeconfig/dependency graph missing")
    config = json.loads(host.with_suffix(".runtimeconfig.json").read_bytes())["runtimeOptions"]
    frameworks = config.get("frameworks", [config.get("framework")])
    require(frameworks and all(row and row.get("name") == "Microsoft.NETCore.App" for row in frameworks),
            "Undeclared additional shared framework closure")
    executable = shutil.which("dotnet")
    require(executable is not None, "Actual dotnet executable missing")
    executable = Path(executable).resolve()
    result = subprocess.run([str(executable), "--list-runtimes"], capture_output=True, timeout=30, check=False)
    (output / "dotnet-list-runtimes.stdout").write_bytes(result.stdout)
    (output / "dotnet-list-runtimes.stderr").write_bytes(result.stderr)
    require(result.returncode == 0, "Actual .NET runtime enumeration failed")
    matches = set()
    for line in result.stdout.decode("utf-8", errors="strict").splitlines():
        match = re.fullmatch(r"Microsoft\.NETCore\.App " + re.escape(VERSION) + r" \[(.+)\]", line.strip())
        if match:
            matches.add((Path(match.group(1)) / VERSION).resolve())
    require(len(matches) == 1, "Exactly one actual required Core runtime closure is required")
    runtime = next(iter(matches))
    fxr = executable.parent / "host" / "fxr"
    return dict(HostPath=str(host), HostSha256=digest(host), HostDirectory=str(host.parent),
                DependencyClosure=files(host.parent), DotnetPath=str(executable), DotnetSha256=digest(executable),
                CoreVersion=VERSION, RuntimeDirectory=str(runtime), RuntimeClosure=files(runtime),
                HostFxrDirectory=str(fxr), HostFxrSearchClosure=files(fxr),
                ClosureScope="Actual resolved executable plus all declared app/deps files, required Core runtime and hostfxr search inputs; per-route host trace retained")


def managed_after(before):
    require(digest(before["HostPath"]) == before["HostSha256"], "Managed host drifted")
    require(files(before["HostDirectory"]) == before["DependencyClosure"], "Managed dependency closure drifted")
    require(digest(before["DotnetPath"]) == before["DotnetSha256"], "Actual dotnet binary drifted")
    require(files(before["RuntimeDirectory"]) == before["RuntimeClosure"], "Actual Core runtime closure drifted")
    require(files(before["HostFxrDirectory"]) == before["HostFxrSearchClosure"], "Actual hostfxr closure drifted")
    return dict(Equal=True, HostSha256=before["HostSha256"], DependencyFiles=len(before["DependencyClosure"]),
                RuntimeFiles=len(before["RuntimeClosure"]), HostFxrFiles=len(before["HostFxrSearchClosure"]))


def validate_comparison(proof, owned, ownref):
    require(proof["Passed"] and proof["RequiredSuccessfulCases"] and proof["OwnedProfile"] is owned,
            "Actual profile did not pass complete required worker outcomes")
    require(proof["WholeRawValuesAndStacksCompared"] and proof["RawEtfRetained"] and not proof["RawEtfRewritten"],
            "Raw/full-stack evidence policy differs")
    require(proof["OwnReferenceProtocolOptIn"] is ownref, "Child protocol differs from explicit request")
    require(proof["Comparison"] == (OWNREF if ownref else STRICT), "Child comparison policy differs")
    if not ownref:
        require(proof["WholeDefaultEtfEqual"] and proof["WholeDeterministicEtfEqual"], "Strict byte comparison failed")
    else:
        require(proof["OwnReferenceProtocolPassed"], "Explicit own-reference protocol failed")
        a, b = proof["OwnReferenceDefault"], proof["OwnReferenceDeterministic"]
        for row in (a, b):
            require(row["Passed"] and row["OriginalOccurrences"] == 6 and row["ManagedOccurrences"] == 6
                    and row["Failure"] is None, "Exact six named native-reference law was not satisfied")
        require(a["OriginalRole"] == b["OriginalRole"] and a["ManagedRole"] == b["ManagedRole"],
                "Default/deterministic profiles do not use the same real reference binding")
    stocks = proof["StockImages"]
    require([row["Name"] for row in stocks] == ["atomics", "counters"], "Actual stock image ownership absent")
    require(all(row["OriginalBeamSha256"] and row["NativeMd5"] for row in stocks), "Stock owner evidence absent")
