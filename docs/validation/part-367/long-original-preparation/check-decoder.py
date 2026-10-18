"""Synthetic ETF parser controls only; never runs Erlang, BEAM or SDK code."""
from pathlib import Path
import hashlib
import importlib.util
import json
import time

ROOT = Path(__file__).resolve().parent


def load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def rejected(decoder, packet, message):
    try:
        decoder.decode_exact(packet)
    except ValueError as error:
        assert message in str(error), str(error)
        return str(error)
    raise AssertionError("Malformed parser control was accepted")


def main():
    decoder = load(ROOT / "tool/passive_etf_family.py", "supplement_passive_decoder")
    baseline = load(ROOT.parent / "queue-original367-original1/tool/passive_etf_family.py", "accepted_passive_decoder")
    assert (decoder.MAX_TERMS, decoder.MAX_BYTES, decoder.MAX_DEPTH) == (8_000_000, 16 * 1024 * 1024, 128)
    started = time.monotonic()
    positives = []
    for length in [64017, 256017]:
        packet = b"\x83\x6c" + length.to_bytes(4, "big") + b"".join(
            b"\x62" + value.to_bytes(4, "big") for value in range(1, length + 1)
        ) + b"\x77\x04tail"
        node = decoder.decode_exact(packet)
        assert node[0] == "improper" and len(node[1]) == length and node[2] == ("atom", "tail")
        assert all(value == ("integer", index) for index, value in enumerate(node[1], 1))
        if length == 256017:
            rejected(baseline, packet, "term budget")
        positives.append(dict(Length=length, Bytes=len(packet), FullUniquePrefixExact=True,
                              Sha256=hashlib.sha256(packet).hexdigest().upper()))
    negatives = []
    controls = [
        (b"\x83\x6c" + (decoder.MAX_TERMS + 1).to_bytes(4, "big") + b"\x6a" * (decoder.MAX_TERMS + 2), "term budget"),
        (b"\x83" + b"\x00" * decoder.MAX_BYTES, "version/size"),
        (b"\x83" + b"\x68\x01" * (decoder.MAX_DEPTH + 2) + b"\x6a", "depth limit"),
        (b"\x83\x6a\x00", "Trailing bytes"),
        (b"\x83\x6c\x00\x00\x00\x01", "List size exceeds"),
        (b"\x83\x50\x00\x00\x00\x00", "Unsupported ETF tag"),
        (b"\x00\x6a", "version/size"),
    ]
    for packet, reason in controls:
        negatives.append(dict(Bytes=len(packet), Rejection=rejected(decoder, packet, reason)))
    decoder.MAX_TERMS = 12
    try:
        packet = b"\x83\x68\x02" + (b"\x6c\x00\x00\x00\x04" + b"\x61\x01" * 4 + b"\x6a") * 2
        negatives.append(dict(Bytes=len(packet), Rejection=rejected(decoder, packet, "term budget"),
                              AggregateNestedBudgetFaultControl=True))
    finally:
        decoder.MAX_TERMS = 8_000_000
    result = dict(Status="PASS_SYNTHETIC_DECODER_LIMIT_CONTROLS", Positive=positives,
                  Negative=negatives, PositiveCount=2, NegativeCount=8,
                  FinalLimits=dict(Terms=decoder.MAX_TERMS, Bytes=decoder.MAX_BYTES, Depth=decoder.MAX_DEPTH),
                  ElapsedSeconds=time.monotonic() - started, OriginalExecuted=False, SdkExecuted=False)
    output = ROOT / "decoder-controls.json"
    assert not output.exists()
    output.write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"Status": result["Status"], "ElapsedSeconds": result["ElapsedSeconds"]}))


if __name__ == "__main__":
    main()
