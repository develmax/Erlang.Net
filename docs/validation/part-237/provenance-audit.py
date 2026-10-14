import hashlib
import json
import re
import struct
import subprocess
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "artifacts"
HEAD = "5f2fd55572cc6c31c1dc603286d5e6bfee73560f"
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
TAG = "OTP-29.1.1"
checks = []


def require(value, name):
    if not value:
        raise AssertionError(name)
    checks.append(name)


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def file_hash(path, expected, name):
    require(sha(path.read_bytes()) == expected.upper(), name)


def git_blob(head, path):
    return subprocess.check_output(["git", "-C", str(ROOT), "show", head + ":" + path])


def terminal(metadata, run_id, job_id, workflow):
    run, job = metadata["Run"], metadata["Job"]
    require(run["id"] == run_id and job["id"] == job_id and job["run_id"] == run_id, workflow + " actual run/job IDs")
    require(run["head_sha"] == job["head_sha"] == HEAD, workflow + " exact head")
    require(run["status"] == job["status"] == "completed" and run["conclusion"] == job["conclusion"] == "success", workflow + " terminal success")
    require(run["run_attempt"] == job["run_attempt"] == 1, workflow + " attempt1")
    require(all(s["status"] == "completed" and s["conclusion"] == "success" for s in job["steps"]), workflow + " all steps terminal success")
    return run, job


def zip_guard(prefix, run_id, artifact_id, extracted):
    metadata = load(ART / (prefix + "-metadata.json"))
    artifact = metadata["Artifact"]
    require(artifact["id"] == artifact_id and artifact["workflow_run"]["id"] == run_id and artifact["workflow_run"]["head_sha"] == HEAD, prefix + " actual artifact run/head IDs")
    require(metadata["Run"]["id"] == run_id and metadata["Run"]["head_sha"] == HEAD and metadata["Run"]["conclusion"] == "success", prefix + " metadata terminal run")
    path = ART / (prefix + ".zip")
    digest = sha(path.read_bytes())
    require(digest == metadata["ZipSHA256"].upper() == artifact["digest"].split(":", 1)[1].upper(), prefix + " ZIP server/local digest")
    require(path.stat().st_size == artifact["size_in_bytes"], prefix + " actual ZIP bytes")
    inventory = []
    with zipfile.ZipFile(path) as archive:
        for entry in archive.infolist():
            if entry.is_dir():
                continue
            require(not Path(entry.filename).is_absolute() and ".." not in Path(entry.filename).parts, "ZIP safe entry " + entry.filename)
            data = archive.read(entry)
            local = extracted / entry.filename
            require(local.read_bytes() == data, "ZIP/extracted exact " + entry.filename)
            inventory.append({"Path": entry.filename, "Bytes": len(data), "Sha256": sha(data)})
    return metadata, digest, inventory


def beam_chunks(data):
    require(data[:4] == b"FOR1" and data[8:12] == b"BEAM", "BEAM container markers")
    require(struct.unpack(">I", data[4:8])[0] + 8 == len(data), "BEAM exact FORM span")
    result = []
    offset = 12
    while offset < len(data):
        require(offset + 8 <= len(data), "chunk header bound")
        tag = data[offset:offset+4].decode("ascii")
        size = struct.unpack(">I", data[offset+4:offset+8])[0]
        padding = (-size) % 4
        require(offset + 8 + size + padding <= len(data), "chunk payload bound")
        payload = data[offset+8:offset+8+size]
        result.append({"Id": tag, "HeaderOffset": offset, "PayloadBytes": size, "PayloadSha256": sha(payload), "PaddingBytes": padding})
        offset += 8 + size + padding
    require(offset == len(data), "BEAM all bytes consumed")
    return result


def etf(data):
    require(data[0] == 131, "ETF version")
    offset = 1
    def take(n):
        nonlocal offset
        result = data[offset:offset+n]
        require(len(result) == n, "ETF bound")
        offset += n
        return result
    def term():
        tag = take(1)[0]
        if tag == 97:
            return take(1)[0]
        if tag == 98:
            return struct.unpack(">i", take(4))[0]
        if tag in (100, 118):
            length = struct.unpack(">H", take(2))[0]
            return take(length).decode("utf-8" if tag == 118 else "latin-1")
        if tag in (115, 119):
            return take(take(1)[0]).decode("utf-8" if tag == 119 else "latin-1")
        if tag in (104, 105):
            count = take(1)[0] if tag == 104 else struct.unpack(">I", take(4))[0]
            return tuple(term() for _ in range(count))
        if tag == 106:
            return []
        if tag == 107:
            return list(take(struct.unpack(">H", take(2))[0]))
        if tag == 108:
            values = [term() for _ in range(struct.unpack(">I", take(4))[0])]
            require(term() == [], "ETF proper list tail")
            return values
        raise AssertionError("Unexpected independent fixture ETF tag " + str(tag))
    result = term()
    require(offset == len(data), "ETF trailing bytes absent")
    return result


report = {"Status": "FAIL", "SourceHead": HEAD, "Pin": PIN, "ClrBeamExecutionPerformed": False}
try:
    beam_run, beam_job = terminal(load(ART / "part-236-retry-beam-latest-provenance.json"), 38491968358, 115619023527, "BEAM")
    ci_run, ci_job = terminal(load(ART / "part-236-retry-ci-latest-provenance.json"), 38491968324, 115619019883, "CI")
    require(beam_run["path"] == ".github/workflows/beam-fixtures.yml" and ci_run["path"] == ".github/workflows/validate.yml", "actual workflow paths")
    logs = {}
    for kind, job_id in [("beam", 115619023527), ("ci", 115619019883)]:
        name = "part-236-retry-" + kind + "-job.log"
        digest = load(ART / (name + ".digest.json"))
        file_hash(ART / name, digest["SHA256"], kind + " raw job log digest")
        logs[kind] = (ART / name).read_text()
    extracted = ART / "part-236-retry-beam-fixtures"
    meta, zip_hash, inventory = zip_guard("part-236-retry-beam-fixtures", beam_run["id"], 11861914628, extracted)
    require(len(inventory) == 79, "actual79 BEAM artifact files")
    root = extracted / "beam-fixtures"
    manifest = load(root / "manifest.json")
    evidence_path = extracted / "beam-source-build-evidence.json"
    evidence = load(evidence_path)
    require((root / "source-build-evidence.json").read_bytes() == evidence_path.read_bytes(), "source evidence copies exact")
    file_hash(evidence_path, manifest["SourceBuildEvidenceSha256"], "source evidence manifest digest")
    file_hash(extracted / "beam-otp-build.log", evidence["SourceBuildLogSha256"], "actual source-build log digest")
    require(evidence["SourceBuildLogSha256"].upper() == manifest["SourceBuildLogSha256"].upper(), "source log manifest binding")
    installed = "/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-otp-oracle/lib/erlang"
    require(evidence["SourceCommit"] == manifest["SourcePin"] == PIN and evidence["SourceTag"] == manifest["SourceTag"] == TAG, "source pin/tag")
    require(evidence["SourceBuildCompleted"] is True and evidence["HarnessCommit"] == HEAD and evidence["RunId"] == str(beam_run["id"]) and evidence["RunAttempt"] == "1", "real source-build evidence run/head")
    require(evidence["InstalledOtpRoot"] == manifest["InstalledOtpRoot"] == installed and manifest["InstalledVersion"] == "29.1.1", "source-built installed root/version")
    require(evidence["RunnerOs"] == "Linux" and evidence["BuildArguments"] == ["make", "-j2"] and evidence["InstallArguments"] == ["make", "install"], "actual Linux source build/install")
    require("PINNED_BEAM_SOURCE_VERIFIED " + PIN in logs["beam"] and "make -j2" in logs["beam"] and "make install" in logs["beam"] and installed in logs["beam"], "raw source-build/prefix command evidence")
    require("--execute" in logs["beam"] and str(meta["Artifact"]["id"]) in logs["beam"] and zip_hash.lower() in logs["beam"], "raw execute/upload artifact evidence")
    require(manifest["CompilerFlags"] == ["-Werror", "+debug_info"] and "[source]" in manifest["OracleVersionRaw"], "actual development compiler/version flags")
    for name in ["CompilerExecutableSha256", "OracleExecutableSha256"]:
        require(re.fullmatch(r"[0-9A-F]{64}", manifest[name]) is not None, "retained " + name)
    declared_path = ART / "beam-fixture-declared-ledger/declared-fixtures.json"
    declared = load(declared_path)
    require(declared["PublishedDeclarationsHead"] == "75ec3fd6b0b62541c40c21d6fb563985c37a92b6" and declared["ValuesDerivedFromOriginalObservation"] is False, "independent predeclaration head/values")
    require(sha(git_blob(declared["PublishedDeclarationsHead"], "tools/Erlang.BeamFixtures/FixtureCases.cs")) == declared["FixtureClassSourceSHA256"], "published authoring class source hash")
    require(git_blob(HEAD, "tools/Erlang.BeamFixtures/FixtureCases.cs") == git_blob(declared["PublishedDeclarationsHead"], "tools/Erlang.BeamFixtures/FixtureCases.cs"), "predeclarations unchanged before retry")
    declarations = {row["Module"]: row for row in declared["Rows"]}
    fixtures = {row["Module"]: row for row in manifest["Fixtures"]}
    require(len(declarations) == len(fixtures) == 5 and declarations.keys() == fixtures.keys(), "all5 independent source predeclarations")
    expected = {"beam_fixture_constant": ("ok", ("ok", 42, ["alpha", "beta"])), "beam_fixture_add": ("ok", 42), "beam_fixture_local": ("ok", ("ok", [1, 2, 3])), "beam_fixture_receive": ("ok", ("ok", "ping"))}
    rows = []
    for name, f in fixtures.items():
        declaration = declarations[name]
        source = root / f["SourceFile"]
        require(source.read_bytes() == declaration["Source"].encode(), name + " full source predeclaration exact")
        file_hash(source, f["SourceSha256"], name + " source manifest hash")
        require(f["SourceSha256"] == declaration["SourceSha256"] and f["Purpose"] == declaration["Purpose"], name + " source/purpose ledger")
        beam = root / f["BeamFile"]
        file_hash(beam, f["BeamSha256"], name + " BEAM hash")
        require(beam.stat().st_size == f["BeamBytes"] and beam_chunks(beam.read_bytes()) == f["Chunks"], name + " independent complete chunk inventory")
        file_hash(root / f["DisassemblyFile"], f["DisassemblySha256"], name + " disassembly hash")
        for mode in ["compile", "disassemble"]:
            require((root / (name + "-" + mode + ".exit-code.txt")).read_text().strip() == "0", name + " native " + mode + " exit0")
        execution = f["Execution"]
        require(execution["IndependentExpectedExpression"] == declaration["IndependentExpectedExpression"], name + " expected expression predeclared")
        if name in expected:
            require(execution["Executed"] is True and execution["ExitCode"] == 0 and (root / (name + "-execute.exit-code.txt")).read_text().strip() == "0", name + " actual native execution exit0")
            for file_key, hash_key in [("ActualEtfFile", "ActualEtfSha256"), ("ExpectedEtfFile", "ExpectedEtfSha256"), ("EvaluationFile", "EvaluationSha256")]:
                file_hash(root / execution[file_key], execution[hash_key], name + " " + file_key + " digest")
            for mode, key in [("stdout", "StandardOutputSha256"), ("stderr", "StandardErrorSha256")]:
                file_hash(root / (name + "-execute." + mode + ".txt"), execution[key], name + " execution " + mode + " digest")
            require(etf((root / execution["ActualEtfFile"]).read_bytes()) == etf((root / execution["ExpectedEtfFile"]).read_bytes()) == expected[name], name + " independent fixed outer-ok ETF result")
            evaluation = (root / execution["EvaluationFile"]).read_text()
            require("Actual=" + name + ":run()" in evaluation and "Expected=" + declaration["IndependentExpectedExpression"] in evaluation and "Actual =:= Expected" in evaluation, name + " independent evaluated expression/code contract")
        else:
            require(name == "beam_fixture_on_load" and execution["Executed"] is False and execution["ExitCode"] is None and execution["IndependentExpectedExpression"] is None and not (root / (name + "-execute.exit-code.txt")).exists(), "on_load compiled/disassembled only, never executed")
            require("separate observer contract" in execution["NotExecutedReason"], "on_load reason explicit")
        rows.append({"Module": name, "SourceSha256": f["SourceSha256"], "BeamSha256": f["BeamSha256"], "Chunks": len(f["Chunks"]), "OriginalExecuted": execution["Executed"]})
    require(manifest["OriginalFixturesExecuted"] == 4 and manifest["OriginalFixtureExecutionRequested"] is True and manifest["LoaderExecutionPerformed"] is False, "native4 / managed-loader0 scope")
    reader = load(ART / "beam-reader-real-fixture-results.json")
    require(len(reader) == 5 and {x["Module"] for x in reader} == fixtures.keys(), "reader actual5 modules")
    for row in reader:
        output = json.loads(row["Output"])
        f = fixtures[row["Module"]]
        require(row["Exit"] == 0 and output["SourcePin"] == PIN and output["InputSha256"] == f["BeamSha256"] and output["InputBytes"] == f["BeamBytes"] and output["BytesOutsideForm"] == 0 and output["Chunks"] == f["Chunks"], row["Module"] + " real structural reader binding")
        require(output["StructuralDecodeOnly"] is True and output["LoaderExecutionPerformed"] is False, "reader no CLR loader execution")
    independent = load(ART / "beam-fixture-etf-audit/results.json")
    require(independent["DecoderOnly"] is True and independent["ClrBeamExecutionPerformed"] is False and len(independent["Rows"]) == 4 and all(x["Passed"] and x["Actual"] == x["IndependentExpected"] for x in independent["Rows"]), "Root independent ETF4 decoder-only proof")
    ci_extracted = ART / "part-236-retry-validation"
    ci_meta, ci_zip_hash, ci_inventory = zip_guard("part-236-retry-validation", ci_run["id"], 11862328065, ci_extracted)
    baseline = json.loads(git_blob("75ec3fd6b0b62541c40c21d6fb563985c37a92b6", "docs/validation/part-235/tests.json"))
    ci_tests = load(ci_extracted / "tests.json")
    require(ci_tests["Passed"] == baseline["Passed"] == 6407 and ci_tests["Failed"] == baseline["Failed"] == 0, "CI actual6407/full")
    require([x["Name"] for x in ci_tests["Tests"]] == [x["Name"] for x in baseline["Tests"]] and all(x["Status"] == "Passed" for x in ci_tests["Tests"]), "CI6407 exact235 names/order/pass states")
    pass_names = [re.sub(r"^\S+Z ", "", x)[5:] for x in logs["ci"].splitlines() if re.sub(r"^\S+Z ", "", x).startswith("PASS ")]
    require(pass_names == [x["Name"] for x in ci_tests["Tests"]] and "6407/6407 passed" in logs["ci"] and "Validation passed" in logs["ci"], "raw CI6407 ordered PASS +pipeline completion")
    integration = load(ci_extracted / "integration-results.json")
    require(all(integration[k] == "Passed" for k in ["Build", "Tests", "Readiness", "PackagedReadiness", "Incremental", "CleanRebuild", "PackageConsumer", "LocalTool"]) and integration["DisabledPreprocessing"] == "Expected failure" and integration["OracleUnavailableReport"] == "Expected failure recorded" and integration["HybridOutput"] == "Hello World", "CI actual complete pipeline gates")
    provider_paths = subprocess.check_output(["git", "-C", str(ROOT), "ls-tree", "-r", "--name-only", HEAD, "tools/Erlang.Differential"], text=True).splitlines()
    require(all(git_blob(HEAD, p) == git_blob("75ec3fd6b0b62541c40c21d6fb563985c37a92b6", p) for p in provider_paths), "all235->236 source ledger provider bytes unchanged")
    report.update({"Status": "PASS", "Checks": len(checks), "CheckNames": checks, "BeamRunId": beam_run["id"], "BeamJobId": beam_job["id"], "BeamArtifactId": meta["Artifact"]["id"], "BeamZipSha256": zip_hash, "BeamFileInventory": inventory, "InstalledRoot": installed, "OriginalFixtureRows": rows, "OriginalExecutions": 4, "OnLoadExecuted": False, "StructuralReaderArtifacts": 5, "IndependentEtfAudits": 4, "CiRunId": ci_run["id"], "CiJobId": ci_job["id"], "CiArtifactId": ci_meta["Artifact"]["id"], "CiZipSha256": ci_zip_hash, "CiFileInventory": ci_inventory, "CiTests": 6407, "CiFullPipeline": True, "CompilerBinaryRehashedLocally": False, "CompilerBinaryLimit": "Compiler/erl binary hashes are retained exporter observations; binaries not supplied in downloaded ZIP. Source-build step/log/evidence and exact artifact binding checked; no independent compiler rebuild or cryptographic attestation claimed.", "Limits": ["5 structural reads are not CLR instruction interpretation/loader execution", "4 native code:load_abs executions are development evidence; on_load observer contract remains unexecuted", "Original235 map action discrepancy stays failed and is outside this provenance guard"]})
except Exception as exception:
    report.update({"Error": repr(exception), "CompletedChecks": len(checks), "CheckNames": checks})
output = ART / "part237-beam-ci-provenance-guard.json"
output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8", newline="\n")
print(json.dumps({k: report[k] for k in ["Status", "SourceHead", "ClrBeamExecutionPerformed"]} | {"Checks": len(checks), "Output": str(output), "Error": report.get("Error")}))
sys.exit(0 if report["Status"] == "PASS" else 1)
