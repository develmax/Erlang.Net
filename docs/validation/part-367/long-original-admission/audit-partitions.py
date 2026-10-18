"""Passive full ETF admission and unique reverse-state decomposition."""
from pathlib import Path
import hashlib
import importlib.util
import json

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
TOOL = ROOT / "artifacts/queue-original367-original2/tool"
CAPTURE = ROOT / "artifacts/queue-original367-original2/download/run-39072770120/artifact-12116022082/capture"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def named(node, names):
    assert node[0] == "list" and len(node[1]) == len(names)
    rows = []
    for name, row in zip(names, node[1]):
        assert row[0] == "tuple" and len(row[1]) == 2 and row[1][0] == ("atom", name)
        rows.append((name, row[1][1]))
    return rows


def main():
    manifest = json.loads((TOOL / "source-inputs.corrected.json").read_bytes())
    decoder_path = TOOL / "passive_etf_family.py"
    assert sha(decoder_path) == next(x["Sha256"] for x in manifest["ToolInputs"] if x["Path"] == "passive_etf_family.py")
    spec = importlib.util.spec_from_file_location("admitted_queue_long_decoder", decoder_path)
    decoder = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(decoder)
    assert (decoder.MAX_TERMS, decoder.MAX_BYTES, decoder.MAX_DEPTH) == (8_000_000, 16 * 1024 * 1024, 128)
    profile = json.loads((TOOL / "profile.json").read_bytes())
    groups = []
    partitions = []
    controls = []
    values_count = profiles_count = 0
    labels = [("atom", "default")] + [("integer", n) for n in [0, 1, 2, 8]]
    for fixture in profile["Fixtures"]:
        directory = CAPTURE / fixture["Id"]
        rows = named(decoder.decode_exact((directory / "returned.etf").read_bytes()), fixture["Names"])
        for name, result in rows:
            assert result == ("tuple", (("atom", "error"), ("atom", "error"), ("atom", "badarg"))), name
        values_count += len(rows)
        stack_rows = decoder.decode_exact((directory / "stack-observations.etf").read_bytes())
        assert stack_rows[0] == "list" and len(stack_rows[1]) == 5
        for expected, depth in zip(labels, stack_rows[1]):
            assert depth[0] == "tuple" and len(depth[1]) == 2 and depth[1][0] == expected
            for name, result in named(depth[1][1], fixture["Names"]):
                assert result[0] == "tuple" and len(result[1]) == 4
                fields = result[1]
                assert fields[:3] == (("atom", "error"), ("atom", "error"), ("atom", "badarg"))
                stack = fields[3]
                assert stack[0] == "list" and stack[1]
                profiles_count += 1
                frames = []
                for frame in stack[1]:
                    assert frame[0] == "tuple" and len(frame[1]) in [3, 4]
                    f = frame[1]
                    assert f[0][0] == f[1][0] == "atom"
                    frames.append(dict(Module=f[0][1], Function=f[1][1], ArgumentKind=f[2][0]))
                reverse = name not in ["long_append_64017", "long_length_64017", "long_from_list_64017"]
                if reverse:
                    length = 256017 if "256017" in name else 64017 if "64017" in name else 3
                    top = stack[1][0][1]
                    assert top[:2] == (("atom", "lists"), ("atom", "reverse"))
                    assert top[2][0] == "list" and len(top[2][1]) == 2
                    remaining, accumulator = top[2][1]
                    assert accumulator[0] == "list"
                    k = len(accumulator[1])
                    assert 0 <= k <= length
                    assert all(value == ("integer", k - index) for index, value in enumerate(accumulator[1]))
                    if k == length:
                        assert remaining == ("atom", "tail")
                        remaining_count = 0
                    else:
                        assert remaining[0] == "improper" and remaining[2] == ("atom", "tail")
                        remaining_count = len(remaining[1])
                        assert remaining_count == length - k
                        assert all(value == ("integer", k + index) for index, value in enumerate(remaining[1], 1))
                    partitions.append(dict(Group=fixture["Id"], Name=name, Depth=expected[1],
                        Length=length, K=k, Remaining=remaining_count, EveryIntegerAndTailExact=True,
                        NativeNonzero=k > 0, Frames=frames))
                else:
                    controls.append(dict(Group=fixture["Id"], Name=name, Depth=expected[1],
                        Frames=frames, FullRawClassReasonArgumentsLocationsRetained=True))
        groups.append(dict(Id=fixture["Id"], Names=fixture["Names"], NamedValues=len(rows),
                          StackProfiles=len(rows) * 5, ReturnedSha256=sha(directory / "returned.etf"),
                          StacksSha256=sha(directory / "stack-observations.etf"),
                          StacksBytes=(directory / "stack-observations.etf").stat().st_size))
        del stack_rows
    assert values_count == 11 and profiles_count == 55 and len(groups) == 4
    assert len(partitions) == 40 and len(controls) == 15
    nonzero = [row for row in partitions if row["K"] > 0]
    assert nonzero
    for group in ["reverse_direct", "queue_to_list", "queue_out"]:
        assert any(row["Group"] == group and row["Length"] == 256017 and row["K"] > 0 for row in partitions)
    native_only = [c["Name"] for f in profile["Fixtures"] for c in f["Cases"] if c.get("OriginalOnlyDependencyControl")]
    assert native_only == ["reverse_64017", "reverse_256017", "short_reverse_direct"]
    output = dict(Status="PASS_PASSIVE_QUEUE_LONG11_55_NATIVE_UNIQUE_PARTITIONS",
        Values=values_count, StackProfiles=profiles_count, Groups=groups,
        OriginalOnlyDependencyControls=native_only, ReverseStateObservations=partitions,
        OtherControlObservations=controls, NonzeroNativePartitions=len(nonzero),
        ObservedKValues=sorted(set(row["K"] for row in partitions)),
        DiagnosticPredictionCorrection="64017 may remain k0; source-proposal nonzero-per-case prediction was not an acceptance rule. Actual256017 nonzero required and proved in all3routes.",
        EverySavedListTailElementVerified=True, FramesDropped=0, ArgumentsReplaced=False,
        NativePartitionInjected=False, OwnRuntimePartitionEqualityRequired=False,
        FullRawCapturePreserved=True, DownloadedCodeExecuted=False, SdkComparisonClaim=False,
        DecoderSha256=sha(decoder_path), DecoderLimits=dict(Terms=decoder.MAX_TERMS, Bytes=decoder.MAX_BYTES, Depth=decoder.MAX_DEPTH))
    (OUT / "actual1/partitions.json").write_text(json.dumps(output, indent=2) + "\n")
    print(json.dumps(dict(Status=output["Status"], Nonzero=len(nonzero), Ks=output["ObservedKValues"],
                         Sha256=sha(OUT / "actual1/partitions.json"))))


if __name__ == "__main__":
    main()
