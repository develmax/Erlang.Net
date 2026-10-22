"""Bounded passive ETF/BEAM inspection. Does not execute artifact code."""
from pathlib import Path
import hashlib
import importlib.util
import json
import struct

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
TOOL = ROOT / "artifacts/queue-original367-original1/tool"
ARTIFACT = ROOT / "artifacts/queue-original367-original1/download/run-39071324870/artifact-12114654364"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def beam(data):
    assert len(data) <= 32 * 1024 * 1024
    assert data[:4] == b"FOR1" and data[8:12] == b"BEAM"
    assert int.from_bytes(data[4:8], "big") + 8 == len(data)
    chunks = {}
    position = 12
    while position < len(data):
        assert position + 8 <= len(data)
        key = data[position:position + 4].decode("ascii")
        size = int.from_bytes(data[position + 4:position + 8], "big")
        assert key not in chunks and position + 8 + size <= len(data)
        chunks[key] = data[position + 8:position + 8 + size]
        position += 8 + (size + 3) // 4 * 4
    assert position == len(data)
    raw = chunks.get("AtU8", chunks.get("Atom"))
    count = int.from_bytes(raw[:4], "big", signed=True)
    atoms = [None]
    position = 4
    while position < len(raw):
        size = raw[position]
        position += 1
        if count < 0:
            assert size & 7 == 0
            if size & 8:
                assert not size & 16 and position < len(raw)
                size = ((size & 224) << 3) | raw[position]
                position += 1
            else:
                size >>= 4
        assert position + size <= len(raw)
        atoms.append(raw[position:position + size].decode("utf8"))
        position += size
    assert len(atoms) - 1 == abs(count) and position == len(raw)
    funs = []
    if "FunT" in chunks:
        raw = chunks["FunT"]
        assert len(raw) == 4 + int.from_bytes(raw[:4], "big") * 24
        for position in range(4, len(raw), 24):
            atom, arity, label, index, free, old = struct.unpack(">6I", raw[position:position + 24])
            assert 0 < atom < len(atoms)
            funs.append(dict(Name=atoms[atom], Arity=arity, Label=label, Index=index,
                             FreeVariables=free, OldUnique=old))
    return dict(Module=atoms[1], AtomCount=len(atoms) - 1, FunTable=funs)


def rows(node, names):
    assert node[0] == "list" and len(node[1]) == len(names)
    result = []
    for name, row in zip(names, node[1]):
        assert row[0] == "tuple" and len(row[1]) == 2 and row[1][0] == ("atom", name)
        result.append(row[1][1])
    return result


def visit(node):
    if not isinstance(node, tuple):
        return
    if node and node[0] == "new_fun":
        yield node
        for child in node[9]:
            yield from visit(child)
        return
    for child in node:
        if isinstance(child, tuple):
            yield from visit(child)


def main():
    plan = json.loads((TOOL / "source-inputs.corrected.json").read_bytes())
    decoder_path = TOOL / "passive_etf_family.py"
    expected = next(x for x in plan["ToolInputs"] if x["Path"] == "passive_etf_family.py")
    assert sha(decoder_path) == expected["Sha256"]
    decoder = load(decoder_path, "trusted_queue_passive_decoder")
    provenance = load(OUT / "audit-provenance.py", "trusted_queue_beam_md5")
    fixture = json.loads((TOOL / "profile.json").read_bytes())["Fixtures"][0]
    names = fixture["Names"]
    directory = ARTIFACT / "capture/queue_core"
    values = decoder.decode_exact((directory / "returned.etf").read_bytes())
    ordinary = rows(values, names)
    assert len(ordinary) == 160
    stacks = decoder.decode_exact((directory / "stack-observations.etf").read_bytes())
    assert stacks[0] == "list" and len(stacks[1]) == 5
    labels = [("atom", "default")] + [("integer", x) for x in [0, 1, 2, 8]]
    summaries = []
    total = 0
    for expected_label, profile in zip(labels, stacks[1]):
        assert profile[0] == "tuple" and len(profile[1]) == 2 and profile[1][0] == expected_label
        captures = rows(profile[1][1], names)
        counts = {"ok": 0, "error": 0}
        for captured in captures:
            assert captured[0] == "tuple"
            fields = captured[1]
            assert fields[0] in [("atom", "ok"), ("atom", "error")]
            if fields[0] == ("atom", "ok"):
                assert len(fields) == 2
                counts["ok"] += 1
            else:
                assert len(fields) == 4 and fields[1][0] == "atom" and fields[3][0] == "list"
                counts["error"] += 1
        total += len(captures)
        summaries.append(dict(Depth=expected_label[1], Cases=len(captures), Outcomes=counts))
    assert total == 800
    for name, value in zip(names, ordinary):
        if name.startswith("legacy_") and name != "legacy_raw_observer":
            assert value == ("tuple", (("atom", "ok"), ("atom", "done")))
    beam_path = directory / "ebin/queue_core_contract.beam"
    data = beam_path.read_bytes()
    md5, chunks = provenance.module_md5(data)
    information = beam(data)
    assert information["Module"] == fixture["Module"]
    funs = []
    for file in [directory / "returned.etf", directory / "stack-observations.etf"]:
        raw = file.read_bytes()
        for function in visit(decoder.decode_exact(raw)):
            assert function[5] == ("atom", fixture["Module"])
            assert function[3] == md5
            entry = [x for x in information["FunTable"] if x["Index"] == function[4]]
            assert len(entry) == 1
            entry = entry[0]
            assert entry["Name"] == "local_identity" and entry["Arity"] == function[2] == 1
            assert entry["FreeVariables"] == len(function[9]) == 0
            assert entry["OldUnique"] == function[7][1]
            assert function[6] == ("integer", entry["Index"])
            span = function[10]
            assert span[0] == "raw_span"
            decoded_span = decoder.decode_exact(b"\x83" + raw[span[1]:span[1] + span[2]])
            assert decoded_span[:10] == function[:10]
            funs.append(dict(File=file.name, Index=function[4], Name=entry["Name"], Arity=1,
                             Unique=md5.hex().upper(), OldUnique=entry["OldUnique"],
                             OwnerPid=repr(function[8]), RawSpan=span[1:]))
    assert len(funs) == 24
    controls = [b"\x00\x6a", b"\x83\x6a\x00", b"\x83\x68\x01",
                b"\x83\x50\x00\x00\x00\x00",
                b"\x83" + b"\x68\x01" * (decoder.MAX_DEPTH + 2) + b"\x6a",
                b"\x83\x6c" + (decoder.MAX_TERMS + 1).to_bytes(4, "big") + b"\x6a",
                b"\x83\x70\x00\x00\x00\x03",
                b"\x83" + b"\x00" * decoder.MAX_BYTES]
    rejected = 0
    for control in controls:
        try:
            decoder.decode_exact(control)
        except (ValueError, AssertionError):
            rejected += 1
    assert rejected == len(controls)
    result = dict(Status="PASS_PASSIVE_QUEUE160_VALUES_800_STACKS_AND_FUN_BINDINGS",
                  NamedValues=160, FullStackProfiles=800, Profiles=summaries,
                  EveryNameOrderedExact=True, EightLegacyAssertionsDone=True,
                  ReturnedSha256=sha(directory / "returned.etf"),
                  StacksSha256=sha(directory / "stack-observations.etf"),
                  FixtureBeamSha256=sha(beam_path), FixtureModuleInfoMd5=md5.hex().upper(),
                  FixtureBeam=information, OpaqueLocalFunctions=funs,
                  DecoderSha256=sha(decoder_path), DecoderLimits=dict(Bytes=decoder.MAX_BYTES,
                      Depth=decoder.MAX_DEPTH, Terms=decoder.MAX_TERMS),
                  DecoderNegativeControls=len(controls), AllNegativeControlsRejected=True,
                  DownloadedCodeExecuted=False, RawOriginalOpaqueValuesPreserved=True,
                  SdkComparisonOrCompatibilityClaim=False)
    (OUT / "actual1/rows-and-functions.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"Status": result["Status"], "Sha256": sha(OUT / "actual1/rows-and-functions.json")}))


if __name__ == "__main__":
    main()
