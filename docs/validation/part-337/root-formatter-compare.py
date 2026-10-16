"""Passive ETF term comparison. Original capture remains the independent input."""
from pathlib import Path
import argparse
import base64
import hashlib
import json


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


class Reader:
    def __init__(self, data):
        self.data = data
        self.offset = 0
        self.nodes = 0

    def take(self, count):
        if count < 0 or self.offset + count > len(self.data):
            raise ValueError('Truncated ETF')
        result = self.data[self.offset:self.offset + count]
        self.offset += count
        return result

    def number(self, count):
        return int.from_bytes(self.take(count), 'big')

    def term(self, depth=0):
        self.nodes += 1
        if depth > 512 or self.nodes > 1000000:
            raise ValueError('ETF structural bound exceeded')
        tag = self.number(1)
        if tag == 97:
            return ('integer', self.number(1))
        if tag == 98:
            return ('integer', int.from_bytes(self.take(4), 'big', signed=True))
        if tag in (110, 111):
            count = self.number(1 if tag == 110 else 4)
            sign = self.number(1)
            if sign not in (0, 1):
                raise ValueError('Invalid big integer sign')
            value = int.from_bytes(self.take(count), 'little')
            return ('integer', -value if sign else value)
        if tag in (100, 115, 118, 119):
            count = self.number(1 if tag in (115, 119) else 2)
            return ('atom', self.take(count).decode('utf-8' if tag in (118, 119) else 'latin1'))
        if tag == 70:
            return ('float64-bits', self.take(8).hex())
        if tag == 106:
            return ('nil',)
        if tag == 107:
            values = tuple(('integer', value) for value in self.take(self.number(2)))
            return ('list', values, ('nil',)) if values else ('nil',)
        if tag == 108:
            values = tuple(self.term(depth + 1) for _ in range(self.number(4)))
            tail = self.term(depth + 1)
            if tail[0] == 'list':
                values += tail[1]
                tail = tail[2]
            return ('list', values, tail) if values else tail
        if tag in (104, 105):
            return ('tuple', tuple(self.term(depth + 1) for _ in range(self.number(1 if tag == 104 else 4))))
        if tag == 109:
            return ('binary', self.take(self.number(4)).hex())
        if tag == 77:
            count = self.number(4)
            bits = self.number(1)
            data = self.take(count)
            if not count or bits < 1 or bits > 8 or data[-1] & ((1 << (8 - bits)) - 1):
                raise ValueError('Invalid bit binary')
            return ('binary', data.hex()) if bits == 8 else ('bitstring', data.hex(), bits)
        if tag == 116:
            pairs = [(self.term(depth + 1), self.term(depth + 1)) for _ in range(self.number(4))]
            if len({key for key, _ in pairs}) != len(pairs):
                raise ValueError('Duplicate ETF map keys')
            return ('map', tuple(sorted(pairs, key=lambda pair: repr(pair[0]))))
        raise ValueError('Unsupported ETF tag: ' + str(tag))


def decode(data):
    reader = Reader(data)
    if reader.number(1) != 131:
        raise ValueError('Expected ETF version 131')
    value = reader.term()
    if reader.offset != len(data):
        raise ValueError('Trailing ETF bytes')
    return value


def controls():
    string = bytes.fromhex('836B00024142')
    explicit_list = bytes.fromhex('836C00000002614161426A')
    assert decode(string) == decode(explicit_list)
    assert decode(bytes.fromhex('836C000000016B000241426A')) != decode(string)
    assert decode(bytes.fromhex('836D000000024142')) != decode(string)
    assert decode(bytes.fromhex('836C0000000161416142')) != decode(bytes.fromhex('836C0000000161416A'))
    assert decode(bytes.fromhex('83770161')) == decode(bytes.fromhex('8364000161'))
    assert decode(bytes.fromhex('836C0000000161416C0000000161426A')) == decode(explicit_list)
    for invalid in [b'', bytes.fromhex('8361'), string + b'\x00', bytes.fromhex('8377FF'), bytes.fromhex('8350')]:
        try:
            decode(invalid)
        except ValueError:
            continue
        raise AssertionError('Malformed or unsupported ETF accepted')
    return {'Controls': 11, 'NestedListsFlattened': False, 'TextNormalized': False, 'SdkExecuted': False, 'OriginalExecuted': False}


def compare(original, sdk, catalog_path, output, selected_track=None):
    native = json.loads((original / 'observations.json').read_bytes())
    catalog = json.loads(catalog_path.read_bytes())
    assert native['Status'] == 'ORIGINAL_OBSERVATIONS_COMPLETE' and native['Executed'] == native['Planned'] == 76
    assert native['PinnedOtpCommit'] == catalog['PinnedOtpCommit'] == 'ad05823719d77c8faee87348ea39513d4e2f99c5'
    assert native['SdkExecuted'] is False and native['RawNormalization'] is False
    rows = []
    for track, file in [('report', 'report-observations.json'), ('primitive-original-only', 'primitive-observations.json')]:
        if selected_track is not None and track != selected_track:
            continue
        sdk_rows = json.loads((sdk / file).read_bytes())
        native_rows = [row for row in native['Cases'] if row['Track'] == track]
        sources = [row for row in catalog['Cases'] if row['Track'] == track]
        assert [row['Name'] for row in sdk_rows] == [row['Name'] for row in native_rows] == [row['Name'] for row in sources]
        for managed, baseline, source in zip(sdk_rows, native_rows, sources):
            assert baseline['Source'] == {key: source[key] for key in ['Path', 'Bytes', 'Sha256']}
            assert baseline['Status'] == 'OBSERVED_RETURNED' and managed['ComparedWithOriginal'] is False
            directory = original / (track + '--' + baseline['Name'])
            raw = (directory / 'returned.etf').read_bytes()
            assert sha(raw) == baseline['RawValueEtf']['Sha256'] and len(raw) == baseline['RawValueEtf']['Bytes']
            sdk_raw = base64.b64decode(managed['RawEtfBase64'], validate=True)
            envelope = decode(sdk_raw)
            assert envelope[0] == 'tuple' and len(envelope[1]) == 2 and envelope[1][0] == ('atom', 'ok')
            expected, actual = decode(raw), envelope[1][1]
            row = dict(Track=track, Name=baseline['Name'], Passed=expected == actual, OriginalEtfSha256=sha(raw), SdkEnvelopeEtfSha256=sha(sdk_raw), SourceSha256=source['Sha256'])
            if expected != actual:
                row.update(Original=expected, Sdk=actual)
            rows.append(row)
    expected_count = 76 if selected_track is None else 57 if selected_track == 'report' else 19
    assert len(rows) == expected_count
    result = dict(Status='MATCH' if all(row['Passed'] for row in rows) else 'MISMATCH', SelectedTrack=selected_track, Compared=len(rows), Passed=sum(row['Passed'] for row in rows), Rows=rows, Comparison='Decoded Erlang terms; ETF encoding variants accepted, list nesting/tails/binary bytes retained', TextNormalized=False, SdkOrOriginalExecutedByReader=False, ParserControls=controls(), ReaderSha256=sha(Path(__file__).read_bytes()))
    with output.open('xb') as stream:
        stream.write((json.dumps(result, indent=2) + '\n').encode())
    print(json.dumps({key: value for key, value in result.items() if key != 'Rows'}))
    return 0 if result['Status'] == 'MATCH' else 1


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--controls', action='store_true')
    parser.add_argument('--original', type=Path)
    parser.add_argument('--sdk', type=Path)
    parser.add_argument('--catalog', type=Path)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--track', choices=['report', 'primitive-original-only'])
    args = parser.parse_args()
    if args.controls:
        print(json.dumps(controls()))
    else:
        if not all([args.original, args.sdk, args.catalog, args.output]):
            parser.error('Original, SDK, catalog and immutable output paths are required')
        raise SystemExit(compare(args.original, args.sdk, args.catalog, args.output, args.track))
