"""Partition immutable ETF lists by raw term boundaries; never re-encode a row."""
import hashlib
import json
from pathlib import Path
import struct
import sys

ROOT = Path(__file__).resolve().parents[2]
FIXTURE = ROOT / 'tests/Erlang.Tests/Resources/OrdsetsCompatibility'
PART_SIZE = 1024
EXPECTED_COUNT = 12247

def u32(b, p):
    return int.from_bytes(b[p:p + 4], 'big')

def end(b, p):
    tag = b[p]
    p += 1
    fixed = {97: 1, 98: 4, 70: 8, 99: 31, 106: 0}
    if tag in fixed:
        return p + fixed[tag]
    if tag in (100, 118, 107):
        return p + 2 + int.from_bytes(b[p:p + 2], 'big')
    if tag in (115, 119):
        return p + 1 + b[p]
    if tag in (109, 77):
        return p + 4 + (1 if tag == 77 else 0) + u32(b, p)
    if tag in (110, 111):
        width = 1 if tag == 110 else 4
        size = b[p] if width == 1 else u32(b, p)
        return p + width + 1 + size
    if tag == 112:
        return p + u32(b, p)
    if tag in (104, 105, 108, 116):
        width = 1 if tag == 104 else 4
        count = b[p] if width == 1 else u32(b, p)
        p += width
        count = count * 2 if tag == 116 else count + (1 if tag == 108 else 0)
        for _ in range(count):
            p = end(b, p)
        return p
    if tag == 113:
        for _ in range(3):
            p = end(b, p)
        return p
    if tag in (103, 88, 102, 89, 120, 101):
        return end(b, p) + {103: 9, 88: 12, 102: 5, 89: 8, 120: 12, 101: 5}[tag]
    if tag in (114, 90):
        count = int.from_bytes(b[p:p + 2], 'big')
        return end(b, p + 2) + (1 if tag == 114 else 4) + count * 4
    raise ValueError('Unsupported ETF tag at raw boundary: ' + str(tag))

def rows(b):
    assert b[:2] == bytes([131, 108]) and b[-1] == 106
    count = u32(b, 2)
    result = []
    p = 6
    for _ in range(count):
        q = end(b, p)
        assert p < q <= len(b) - 1
        result.append(b[p:q])
        p = q
    assert p == len(b) - 1 and count == EXPECTED_COUNT
    return result

def wrap(items):
    return bytes([131, 108]) + struct.pack('>I', len(items)) + b''.join(items) + bytes([106])

def main():
    check = sys.argv[1:] == ['--check']
    assert not sys.argv[1:] or check
    inputs = rows((FIXTURE / 'inputs.etf').read_bytes())
    golden = rows((FIXTURE / 'original.etf').read_bytes())
    parts = []
    restored_inputs = []
    restored_golden = []
    for index, start in enumerate(range(0, EXPECTED_COUNT, PART_SIZE)):
        stop = min(start + PART_SIZE, EXPECTED_COUNT)
        incoming = inputs[start:stop]
        expected = golden[EXPECTED_COUNT - stop:EXPECTED_COUNT - start]
        restored_inputs.extend(incoming)
        restored_golden[0:0] = expected
        record = dict(Index=index, FirstInput=start, Count=stop - start, Files={})
        for prefix, payload in [('inputs', wrap(incoming)), ('original', wrap(expected))]:
            name = prefix + '-' + str(index).zfill(2) + '.etf'
            target = FIXTURE / name
            if check:
                assert target.read_bytes() == payload, name
            else:
                target.write_bytes(payload)
            record['Files'][name] = dict(Sha256=hashlib.sha256(payload).hexdigest().upper(), Bytes=len(payload))
        parts.append(record)
    assert wrap(restored_inputs) == (FIXTURE / 'inputs.etf').read_bytes()
    assert wrap(restored_golden) == (FIXTURE / 'original.etf').read_bytes()
    manifest = dict(Status='LOSSLESS_RAW_PARTITION_OF_EXISTING_ORACLE', Count=EXPECTED_COUNT,
                    PartSize=PART_SIZE, Parts=parts, InputsReconstructedByteForByte=True,
                    GoldenReconstructedByteForByte=True, RowsReencoded=False)
    payload = (json.dumps(manifest, indent=2) + '\n').encode()
    if check:
        assert (FIXTURE / 'partitions.json').read_bytes() == payload
    else:
        (FIXTURE / 'partitions.json').write_bytes(payload)
    print(json.dumps(dict(Parts=len(parts), Rows=EXPECTED_COUNT, ExactReconstruction=True)))

if __name__ == '__main__':
    main()
