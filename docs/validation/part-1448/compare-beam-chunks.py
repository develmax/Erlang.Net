"""Group differing release BEAM images by the chunks with unequal bytes."""

import argparse
from collections import Counter
import json
from pathlib import Path
import struct


def read_chunks(path):
    data = path.read_bytes()
    assert data[:4] == b"FOR1" and data[8:12] == b"BEAM", path
    assert struct.unpack_from(">I", data, 4)[0] + 8 == len(data), path
    chunks = {}
    position = 12
    while position < len(data):
        raw_tag, size = struct.unpack_from(">4sI", data, position)
        tag = raw_tag.decode("ascii")
        payload = position + 8
        position = payload + ((size + 3) & ~3)
        assert tag not in chunks and position <= len(data), path
        chunks[tag] = data[payload:position]
    return chunks


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("reference", type=Path)
    parser.add_argument("report", type=Path)
    args = parser.parse_args()
    generated = args.report.with_name(args.report.name + ".generated")
    cases = json.loads(args.report.read_text(encoding="utf-8-sig"))["Modules"]

    by_chunk = Counter()
    by_signature = Counter()
    by_chunk_count = Counter()
    for case in cases:
        if case["Status"] != "Different":
            continue
        relative = case["Beam"]
        expected = read_chunks(args.reference / relative)
        actual = read_chunks(generated / relative)
        signature = tuple(
            sorted(
                tag
                for tag in expected.keys() | actual.keys()
                if expected.get(tag) != actual.get(tag)
            )
        )
        by_chunk.update(signature)
        by_signature[signature] += 1
        by_chunk_count[len(signature)] += 1

    print("Differing images by number of unequal chunks")
    for count, images in sorted(by_chunk_count.items()):
        print(f"{count:2} chunks: {images:4} images")
    print("\nDiffering images per chunk")
    for tag, images in by_chunk.most_common():
        print(f"{tag}: {images:4}")
    print("\nDiffering images per chunk combination")
    for signature, images in by_signature.most_common():
        print(f"{images:4} {' '.join(signature) or '(container layout only)'}")


if __name__ == "__main__":
    main()
