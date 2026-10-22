"""Root-only pinned original capture; author performs no compiler/VM execution."""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import json
import os
import shutil


def sha(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest().upper()


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def save(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


parser = argparse.ArgumentParser()
parser.add_argument("--repository", type=Path, required=True)
parser.add_argument("--output", type=Path, required=True)
parser.add_argument("--admission", type=Path, required=True)
args = parser.parse_args()
root = args.repository.resolve()
packet = Path(__file__).resolve().parent
out = args.output.resolve()
assert not out.exists() and out.is_relative_to(root / "artifacts") and not out.is_relative_to(packet)
profile = json.loads((packet / "profile.json").read_bytes())
inputs = {}
for row in json.loads((packet / "manifest.json").read_bytes())["Files"]:
    file = (packet / row["Path"]).resolve()
    assert file.is_relative_to(packet) and sha(file) == row["Sha256"], row["Path"]
    inputs[str(file)] = row["Sha256"]
for row in profile["Dependencies"]:
    file = (root / row["Path"]).resolve()
    assert file.is_relative_to(root) and sha(file) == row["Sha256"], row["Path"]
    inputs[str(file)] = row["Sha256"]
helpers = profile["Helpers"]
owned = load("compressed_mutations442_owned", root / helpers["Owned"])
inventory = load("compressed_mutations442_inventory", root / helpers["Inventory"])
etf = load("compressed_mutations442_etf", root / helpers["Etf"])
environment = load("compressed_mutations442_environment", root / helpers["Environment"])
admission = json.loads(args.admission.read_bytes())
assert admission["SourceCommit"] == profile["Pin"] and admission["ReleaseTag"] == "OTP-29.1.1"
installed = Path(admission["InstalledOtpRoot"])
execution_manifest = Path(admission["ExecutionManifestPath"])
assert sha(execution_manifest) == admission["ExecutionManifestSha256"]
expected = {row["Path"].replace("\\", "/"): row["Sha256"] for row in json.loads(execution_manifest.read_bytes())}
assert len(expected) == 6427 and inventory.inventory(installed) == expected
for file in [args.admission.resolve(), execution_manifest.resolve(), packet / "manifest.json"]:
    inputs[str(file)] = sha(file)
fixture = root / "tests/Erlang.Tests/Fixtures/EtsCompressedMutations442"
for name, digest in json.loads((fixture / "manifest.json").read_bytes()).items():
    assert sha(fixture / name) == digest, name
    inputs[str((fixture / name).resolve())] = digest
for name in profile["Sources"]:
    assert (packet / name).read_bytes() == (fixture / name).read_bytes(), name
out.mkdir()
source = out / "source"
source.mkdir()
ebin = out / "observer-ebin"
ebin.mkdir()
for name in profile["Sources"]:
    file = source / name
    shutil.copyfile(packet / name, file)
    inputs[str(file.resolve())] = sha(file)
env = dict(os.environ)
for name in environment.REMOVED:
    env.pop(name, None)
env.pop("ERL_LIBS", None)
env["ERL_FLAGS"] = "+S 1:1 +A 1"
env["ERLC_USE_SERVER"] = "false"
save(out / "inputs-before.json", inputs)
stages = {}
errors = []
checks = []


def execute(command, name):
    before = inventory.inventory(installed) == expected
    assert before
    try:
        owned.owned(command, out, name, 120, env, str(source))
        stages[name] = True
    except Exception as error:
        stages[name] = False
        errors.append({"Stage": name, "Failure": repr(error)})
    finally:
        after = inventory.inventory(installed) == expected
        stable = all(sha(Path(path)) == digest for path, digest in inputs.items())
        checks.append({"Stage": name, "BeforeEqual": before, "AfterEqual": after, "InputsUnchanged": stable})
        save(out / "closure-checks.json", checks)
        assert after and stable


execute([str(installed / "bin/erlc.exe"), "+debug_info", "+deterministic", "-o", str(ebin),
         *(str(source / name) for name in profile["Sources"])], "compile-observer")
if stages.get("compile-observer"):
    for file in ebin.glob("*.beam"):
        inputs[str(file.resolve())] = sha(file)
    save(out / "inputs-before.json", inputs)
    text = out.as_posix().replace("\\", "\\\\").replace('"', '\\"')
    for stage, function in [("capture-values", "write_values"), ("capture-stacks", "write_stacks")]:
        # Independent routes both run after a successful preparation, even if
        # the first VM fails. No simulated compile-facet stage is reported.
        expression = 'ok=' + profile["Observer"] + ':' + function + '("' + text + '"),halt().'
        execute([str(installed / "bin/erl.exe"), "-noshell", "-noinput", "-boot", "no_dot_erlang",
                 "-pa", str(ebin), "-eval", expression], stage)


def proper(value):
    assert value[0] == "list" and len(value) == 2
    return value[1]


def fields(value, count):
    assert value[0] == "tuple" and len(value[1]) == count
    return value[1]


def read(name):
    file = out / name
    size = file.stat().st_size
    assert 1 < size <= profile["DecodeMaxBytes"]
    data = file.read_bytes()
    # Isolated trusted-capture parser instance only. Every supported ETF term
    # consumes at least one raw byte; no global production ETF limit changes.
    previous = etf.MAX_TERMS
    try:
        etf.MAX_TERMS = size
        return etf.decode_exact(data)
    finally:
        etf.MAX_TERMS = previous


def no_opaque(value):
    pending = [value]
    while pending:
        item = pending.pop()
        if not isinstance(item, (tuple, list)):
            continue
        if item and isinstance(item[0], str) and item[0] in profile["OpaqueTags"]:
            raise ValueError("Opaque VM identity escaped the portable outcome file")
        pending.extend(part for part in item if isinstance(part, (tuple, list)))


def rows(value, own=False):
    records = proper(value)
    assert len(records) == len(profile["Cases"])
    for name, record in zip(profile["Cases"], records):
        parts = fields(record, 3 if own else 2)
        assert parts[0] == ("atom", name)
        if own:
            scope = "own_state_protocol" if name in profile["OwnCases"] else "portable_full_term"
            assert parts[1] == ("atom", scope)
            observed = proper(parts[2])
            if name in profile["OwnCases"]:
                assert observed, (name, "Own-state case emitted no actual observation")
        else:
            outcome = parts[1]
            assert outcome[0] == "tuple"
            body = outcome[1]
            assert body[0] in [("atom", "returned"), ("atom", "raised")]
            assert len(body) == (2 if body[0] == ("atom", "returned") else 4)
            if body[0] == ("atom", "raised"):
                proper(body[3])
            no_opaque(record)
    return len(records)


counts = {}
for stage, main, own, stacked in [
    ("capture-values", "values.etf", "own-values.etf", False),
    ("capture-stacks", "stacks.etf", "own-stacks.etf", True),
]:
    if not stages.get(stage):
        continue
    try:
        if not stacked:
            counts[main] = rows(read(main))
            counts[own] = rows(read(own), True)
        else:
            depths = [("atom", "default"), ("integer", 0), ("integer", 1), ("integer", 2), ("integer", 8)]
            for name, is_own in [(main, False), (own, True)]:
                profiles = proper(read(name))
                assert len(profiles) == len(depths)
                count = 0
                for depth, entry in zip(depths, profiles):
                    values = fields(entry, 3)
                    assert values[0] == depth and values[1][0] == "integer"
                    count += rows(values[2], is_own)
                counts[name] = count
    except Exception as error:
        errors.append({"Stage": "verify-" + stage, "Failure": repr(error)})
captured = counts.get("values.etf", 0) + counts.get("stacks.etf", 0)
own_captured = counts.get("own-values.etf", 0) + counts.get("own-stacks.etf", 0)
whole_golden = {}
for name in ["values.etf", "stacks.etf"]:
    whole_golden[name] = (out / name).is_file() and (out / name).read_bytes() == (fixture / name).read_bytes()
    if not whole_golden[name]:
        errors.append({"Stage": "whole-same-host-" + name, "Failure": "Complete original output differs from committed original capture"})
passed = not errors and all(stages.get(name) for name in ["compile-observer", "capture-values", "capture-stacks"])
passed = bool(passed and captured == profile["WholeRows"] and own_captured == profile["WholeRows"])
files = [{"Path": str(file.resolve()), "Bytes": file.stat().st_size, "Sha256": sha(file)}
         for file in sorted(out.rglob("*")) if file.is_file()]
save(out / "completion.json", {"Passed": passed, "CapturedWholeRows": captured, "OwnObservationRows": own_captured,
    "ExpectedWholeRows": profile["WholeRows"],
    "CapturedPortableFullTermRows": (captured // len(profile["Cases"])) * (len(profile["Cases"]) - len(profile["OwnCases"])),
    "CapturedOwnStateProtocolRows": (captured // len(profile["Cases"])) * len(profile["OwnCases"]), "ComparedToManaged": False,
    "WholeCommittedOriginalBytesEqual": whole_golden, "ExpectedConstructed": False, "OpaqueIdentityNormalized": False, "OriginalBaseline156Rewritten": False,
    "CompileFacetClaim": False, "Pin": profile["Pin"], "Stages": stages, "Errors": errors,
    "Counts": counts, "ClosureChecks": checks, "ExecutionManifestSha256": sha(execution_manifest), "Files": files})
print(json.dumps({"Passed": passed, "WholeRows": captured, "OwnObservationRows": own_captured, "Errors": errors}))
raise SystemExit(0 if passed else 1)
