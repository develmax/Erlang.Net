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
HEAD = "029d02548d5c055d9238e2711bebe15a643e6f09"
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
        if tag == 109:
            return take(struct.unpack(">I", take(4))[0])
        if tag in (110,111):
            size = take(1)[0] if tag == 110 else struct.unpack(">I", take(4))[0]
            sign = take(1)[0]
            value = int.from_bytes(take(size), "little")
            return -value if sign else value
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


def module_md5(data, chunks):
    selected = {c["Id"]: data[c["HeaderOffset"]+8:c["HeaderOffset"]+8+c["PayloadBytes"]] for c in chunks}
    payloads = []
    for tag in ["Atom", "AtU8", "Code", "StrT", "ImpT", "ExpT", "FunT", "LitT", "Meta", "Recs", "DbgB"]:
        if tag not in selected:
            continue
        payload = bytearray(selected[tag])
        if tag == "FunT":
            for offset in range(4, len(payload)-23, 24):
                payload[offset+20:offset+24] = bytes(4)
        payloads.append(payload)
    return hashlib.md5(b"".join(payloads)).hexdigest().upper()


report = {"Status": "FAIL", "SourceHead": HEAD, "Pin": PIN, "ClrBeamExecutionPerformed": False}
try:
    beam_run, beam_job = terminal(load(ART / "part-239-beam-latest-provenance.json"), 38495531713, 115630326131, "BEAM")
    ci_run, ci_job = terminal(load(ART / "part-239-ci-latest-provenance.json"), 38495502286, 115630228570, "CI")
    require(beam_run["path"] == ".github/workflows/beam-fixtures.yml" and ci_run["path"] == ".github/workflows/validate.yml", "actual workflow paths")
    require("ubuntu-latest" in beam_job["labels"] and "windows-latest" in ci_job["labels"], "actual source-build/CI runner labels")
    logs = {}
    for kind in ["beam", "ci"]:
        name = "part-239-" + kind + "-job.log"
        digest = load(ART / (name + ".digest.json"))
        file_hash(ART / name, digest["SHA256"], kind + " immutable raw log digest")
        logs[kind] = re.sub(r"\x1b\[[0-?]*[ -/]*[@-~]", "", (ART / name).read_text(encoding="utf-8-sig"))
    extracted = ART / "part-239-beam-fixtures"
    meta, zip_hash, inventory = zip_guard("part-239-beam-fixtures", beam_run["id"], 11863905725, extracted)
    require(len(inventory) == 130, "exact130 six-fixture/metadata artifact files")
    root = extracted / "beam-fixtures"
    manifest = load(root / "manifest.json")
    evidence_path = extracted / "beam-source-build-evidence.json"
    evidence = load(evidence_path)
    require((root / "source-build-evidence.json").read_bytes() == evidence_path.read_bytes(), "source evidence copies exact")
    file_hash(evidence_path, manifest["SourceBuildEvidenceSha256"], "source-build evidence manifest binding")
    file_hash(extracted / "beam-otp-build.log", evidence["SourceBuildLogSha256"], "source-build raw log binding")
    require(evidence["SourceBuildLogSha256"].upper() == manifest["SourceBuildLogSha256"].upper(), "source log manifest binding")
    installed = "/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-otp-oracle/lib/erlang"
    require(evidence["SourceCommit"] == manifest["SourcePin"] == PIN and evidence["SourceTag"] == manifest["SourceTag"] == TAG, "source pin/tag")
    require(evidence["SourceBuildCompleted"] is True and evidence["HarnessCommit"] == HEAD and evidence["RunId"] == str(beam_run["id"]) and evidence["RunAttempt"] == "1", "actual source-build run/head")
    require(evidence["InstalledOtpRoot"] == manifest["InstalledOtpRoot"] == installed and manifest["InstalledVersion"] == "29.1.1", "installed root/version")
    require(evidence["RunnerOs"] == "Linux" and evidence["BuildArguments"] == ["make", "-j2"] and evidence["InstallArguments"] == ["make", "install"], "Linux source build/install arguments")
    require("PINNED_BEAM_SOURCE_VERIFIED " + PIN in logs["beam"] and "make -j2" in logs["beam"] and "make install" in logs["beam"] and installed in logs["beam"], "raw selected pin/build/install/version path evidence")
    require("--execute" in logs["beam"] and str(meta["Artifact"]["id"]) in logs["beam"] and zip_hash.lower() in logs["beam"], "raw execute/upload artifact binding")
    require(manifest["CompilerFlags"] == ["-Werror", "+debug_info"] and "[source]" in manifest["OracleVersionRaw"], "source-built development compiler flags")
    for name in ["CompilerExecutableSha256", "OracleExecutableSha256"]:
        require(re.fullmatch(r"[0-9A-F]{64}", manifest[name]) is not None, "retained actual " + name)
    declared = load(ART / "beam-fixture-declared-ledger/declared-fixtures.json")
    declarations = {row["Module"]: row for row in declared["Rows"]}
    require(len(declarations) == 5 and declared["ValuesDerivedFromOriginalObservation"] is False, "old five independent declarations retained")
    require(git_blob(HEAD, "tools/Erlang.BeamFixtures/FixtureCases.cs") == git_blob(declared["PublishedDeclarationsHead"], "tools/Erlang.BeamFixtures/FixtureCases.cs"), "old five published authoring sources unchanged")
    closure_source = "-module(beam_fixture_closure).\n-export([run/0,make/1]).\nrun() -> F=make(5), {ok,F(7),is_function(F,1)}.\nmake(N) -> fun(X) -> N+X end.\n"
    closure_class = git_blob(HEAD, "tools/Erlang.BeamFixtures/FixtureClosureCases.cs").decode()
    require(all(line in closure_class for line in closure_source.splitlines()) and '"{ok,12,true}"' in closure_class, "closure source/expected declared in exact published239 class")
    declarations["beam_fixture_closure"] = {"Module": "beam_fixture_closure", "Source": closure_source, "SourceSha256": sha(closure_source.encode()), "IndependentExpectedExpression": "{ok,12,true}", "Purpose": "Captured environment closure and FunT OldUniq metadata"}
    fixtures = {row["Module"]: row for row in manifest["Fixtures"]}
    require(len(fixtures) == 6 and fixtures.keys() == declarations.keys(), "exact six declared fixture names")
    expected = {"beam_fixture_constant": ("ok", ("ok", 42, ["alpha", "beta"])), "beam_fixture_add": ("ok", 42), "beam_fixture_local": ("ok", ("ok", [1, 2, 3])), "beam_fixture_receive": ("ok", ("ok", "ping")), "beam_fixture_closure": ("ok", ("ok", 12, "true"))}
    rows = []
    md5_rows = []
    prior = {row["Module"]: row for row in load(ROOT / "docs/validation/part-237/beam-fixtures.json")["Fixtures"]}
    for name, f in fixtures.items():
        declaration = declarations[name]
        source = root / f["SourceFile"]
        require(source.read_bytes() == declaration["Source"].encode(), name + " full source predeclaration")
        file_hash(source, f["SourceSha256"], name + " source manifest hash")
        require(f["SourceSha256"] == declaration["SourceSha256"] and f["Purpose"] == declaration["Purpose"], name + " independent source/purpose ledger")
        if name in prior:
            require(f["SourceSha256"] == prior[name]["SourceSha256"], name + " original old source hash exact")
        beam = root / f["BeamFile"]
        data = beam.read_bytes()
        file_hash(beam, f["BeamSha256"], name + " BEAM digest")
        chunks = beam_chunks(data)
        require(len(data) == f["BeamBytes"] and chunks == f["Chunks"], name + " independent exact chunk inventory")
        file_hash(root / f["DisassemblyFile"], f["DisassemblySha256"], name + " disassembly digest")
        for mode in ["compile", "disassemble"]:
            require((root / (name + "-" + mode + ".exit-code.txt")).read_text().strip() == "0", name + " actual " + mode + " exit0")
        execution = f["Execution"]
        require(execution["IndependentExpectedExpression"] == declaration["IndependentExpectedExpression"], name + " expected expression predeclared")
        if name in expected:
            require(execution["Executed"] is True and execution["ExitCode"] == 0 and (root / (name + "-execute.exit-code.txt")).read_text().strip() == "0", name + " actual execution exit0")
            for file_key, hash_key in [("ActualEtfFile", "ActualEtfSha256"), ("ExpectedEtfFile", "ExpectedEtfSha256"), ("EvaluationFile", "EvaluationSha256")]:
                file_hash(root / execution[file_key], execution[hash_key], name + " execution " + file_key)
            for mode, key in [("stdout", "StandardOutputSha256"), ("stderr", "StandardErrorSha256")]:
                file_hash(root / (name + "-execute." + mode + ".txt"), execution[key], name + " execution " + mode)
            require(etf((root / execution["ActualEtfFile"]).read_bytes()) == etf((root / execution["ExpectedEtfFile"]).read_bytes()) == expected[name], name + " independent fixed outer-ok ETF")
            evaluation = (root / execution["EvaluationFile"]).read_text()
            require("Actual=" + name + ":run()" in evaluation and "Expected=" + declaration["IndependentExpectedExpression"] in evaluation and "Actual =:= Expected" in evaluation, name + " execution eval contract")
        else:
            require(name == "beam_fixture_on_load" and execution["Executed"] is False and execution["ExitCode"] is None and execution["IndependentExpectedExpression"] is None and not (root / (name + "-execute.exit-code.txt")).exists(), "on_load compilation-only execution absence")
            require("separate observer contract" in execution["NotExecutedReason"], "on_load explicit observer gap")
        metadata = f["Metadata"]
        require(metadata["ExitCode"] == 0 and metadata["FileOnlyQuery"] is True and metadata["ModuleLoadPerformed"] is False and (root / (name + "-metadata.exit-code.txt")).read_text().strip() == "0", name + " actual file-only metadata exit0")
        for file_key, hash_key in [("EtfFile", "EtfSha256"), ("TextFile", "TextSha256"), ("EvaluationFile", "EvaluationSha256")]:
            file_hash(root / metadata[file_key], metadata[hash_key], name + " metadata " + file_key)
        for mode, key in [("stdout", "StandardOutputSha256"), ("stderr", "StandardErrorSha256")]:
            file_hash(root / (name + "-metadata." + mode + ".txt"), metadata[key], name + " metadata " + mode)
        evaluation = (root / metadata["EvaluationFile"]).read_text()
        require("beam_lib:md5(" in evaluation and "beam_lib:chunks(" in evaluation and "beam_disasm:file(" in evaluation and "[attributes,compile_info,exports,locals,\"FunT\"]" in evaluation, name + " exact file metadata query roles")
        require(re.search(r"\bcode\s*:", evaluation) is None and ":run(" not in evaluation and ":init(" not in evaluation and "apply(" not in evaluation, name + " metadata has no module execution/activation call")
        observed = etf((root / metadata["EtfFile"]).read_bytes())
        require(len(observed) == 5 and observed[:2] == ("beam_metadata", name) and isinstance(observed[2], bytes) and len(observed[2]) == 16, name + " independent metadata ETF envelope")
        observed_md5 = observed[2].hex().upper()
        calculated = module_md5(data, chunks)
        require(calculated == observed_md5, name + " source-derived significant-chunk/zeroed-OldUniq MD5 independently matches")
        chunk_keys = ["".join(chr(i) for i in key) if isinstance(key, list) else key for key, value in observed[3]]
        require(chunk_keys == ["attributes", "compile_info", "exports", "locals", "FunT"], name + " actual metadata chunk query order")
        text = (root / metadata["TextFile"]).read_text()
        require(text.startswith("{beam_metadata," + name + ",") and text.rstrip().endswith("}."), name + " metadata textual envelope")
        md5_rows.append({"Module": name, "Calculated": calculated, "OriginalObserved": observed_md5, "Equal": True, "BeamSHA256": f["BeamSha256"]})
        rows.append({"Module": name, "SourceSha256": f["SourceSha256"], "BeamSha256": f["BeamSha256"], "Chunks": len(chunks), "OriginalExecuted": execution["Executed"], "FileOnlyMetadata": True})
    require(manifest["OriginalFixturesExecuted"] == 5 and manifest["OriginalFixtureExecutionRequested"] is True and manifest["LoaderExecutionPerformed"] is False, "native5 / CLR-loader0 scope")
    previous_md5 = load(ART / "part239-native-md5-repaired-audit.json")
    require(previous_md5 == md5_rows, "independent recomputation equals Root repaired six-row MD5 audit")
    require((ART / "part239-native-md5-independent-audit.json").exists(), "initial omitted-significant-chunk failure preserved")
    ci_extracted = ART / "part-239-validation"
    ci_meta, ci_zip_hash, ci_inventory = zip_guard("part-239-validation", ci_run["id"], 11864150097, ci_extracted)
    baseline = json.loads(git_blob(HEAD, "docs/validation/part-238/tests.json"))
    ci_tests = load(ci_extracted / "tests.json")
    require(ci_tests["Passed"] == baseline["Passed"] == 6435 and ci_tests["Failed"] == baseline["Failed"] == 0, "CI actual6435/full")
    require([x["Name"] for x in ci_tests["Tests"]] == [x["Name"] for x in baseline["Tests"]] and all(x["Status"] == "Passed" for x in ci_tests["Tests"]), "CI exact6435 names/order/pass states")
    pass_names = [re.sub(r"^\S+Z ", "", line)[5:] for line in logs["ci"].splitlines() if re.sub(r"^\S+Z ", "", line).startswith("PASS ")]
    require(pass_names == [x["Name"] for x in ci_tests["Tests"]] and "6435/6435 passed" in logs["ci"] and "Validation passed" in logs["ci"], "raw CI6435 ordered PASS/pipeline terminal")
    integration = load(ci_extracted / "integration-results.json")
    require(all(integration[k] == "Passed" for k in ["Build", "Tests", "Readiness", "PackagedReadiness", "Incremental", "CleanRebuild", "PackageConsumer", "LocalTool"]) and integration["DisabledPreprocessing"] == "Expected failure" and integration["OracleUnavailableReport"] == "Expected failure recorded" and integration["HybridOutput"] == "Hello World", "CI complete pipeline gates")
    report.update({"Status": "PASS", "Checks": len(checks), "CheckNames": checks, "BeamRunId": beam_run["id"], "BeamJobId": beam_job["id"], "BeamArtifactId": meta["Artifact"]["id"], "BeamZipSha256": zip_hash, "BeamFileInventory": inventory, "SourceBuildEvidenceSha256": manifest["SourceBuildEvidenceSha256"], "OriginalFixtureRows": rows, "OriginalExecutions": 5, "MetadataFileOnlyQueries": 6, "MetadataModuleLoadPerformed": False, "OnLoadExecuted": False, "IndependentModuleMd5Rows": md5_rows, "CiRunId": ci_run["id"], "CiJobId": ci_job["id"], "CiArtifactId": ci_meta["Artifact"]["id"], "CiZipSha256": ci_zip_hash, "CiFileInventory": ci_inventory, "CiTests": 6435, "CiFullPipeline": True, "CompilerBinaryRehashedLocally": False, "Limits": ["Original238 failed three absence fixtures; this BEAM/tool/CI guard is not an original238 success claim", "Five native run0 executions and six file-only metadata queries are not CLR loader or interpreter completeness", "On_load metadata is file-only and callback execution remains unobserved", "Compiler executable digests are retained source-built exporter observations, not independent compiler rebuild attestation", "Metadata term text is hash-bound with an envelope check; independent semantic verification is performed on its actual ETF bytes"]})
except Exception as exception:
    report.update({"Error": repr(exception), "CompletedChecks": len(checks), "CheckNames": checks})
output = ART / "part239-beam-ci-provenance-guard.json"
output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8", newline="\n")
print(json.dumps({"Status": report["Status"], "Checks": len(checks), "Output": str(output), "Error": report.get("Error")}))
sys.exit(0 if report["Status"] == "PASS" else 1)


