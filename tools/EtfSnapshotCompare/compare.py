"""Local-only complete passive ETF comparisons; never evaluates Erlang or BEAM."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
TAG = 'OTP-29.1.1'
COMMIT = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
DECODER_SHA256 = 'A069AD650B3EA1BE6B90DE60117B84ED7A1A6B67128CC41E7ECAEFD2889C1713'
MANIFEST_LIMIT = 256 * 1024
FILE_LIMIT = 16 * 1024 * 1024
TOTAL_LIMIT = 64 * 1024 * 1024
MAX_COMPARISONS = 16
MAX_INPUTS = 32


class InvalidInput(ValueError):
    pass


class Arguments(argparse.ArgumentParser):
    def error(self, message):
        raise InvalidInput(message)


def require(condition, message):
    if not condition:
        raise InvalidInput(message)


def keys(value, expected):
    require(isinstance(value, dict) and set(value) == set(expected),
            'Object fields must be exactly: ' + ', '.join(expected))


def unique_object(pairs):
    result = {}
    for name, value in pairs:
        require(name not in result, 'Duplicate JSON field: ' + name)
        result[name] = value
    return result


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def save(path, value):
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2, sort_keys=True, ensure_ascii=True)
        stream.write('\n')


def main(argv=None):
    output = None
    created_output = False
    report = {'Status': 'INVALID_INPUT', 'ExitCode': 2, 'Errors': [],
              'Comparisons': [], 'Inputs': [], 'Normalized': False, 'Projected': False}
    try:
        parser = Arguments(description=__doc__)
        parser.add_argument('--manifest', required=True)
        parser.add_argument('--output', required=True)
        args = parser.parse_args(argv)
        manifest_path = Path(args.manifest).resolve()
        output = Path(args.output).absolute()
        require(not output.exists(), 'Output already exists; no overwrite is permitted')
        require(output.parent.is_dir(), 'Output parent directory must exist')
        require(output != manifest_path and not manifest_path.is_relative_to(output),
                'Output must be separate from input manifest')
        output.mkdir(exist_ok=False)
        created_output = True
        (output / 'inputs').mkdir()
        report['ManifestPath'] = str(manifest_path)
        report['Baseline'] = {'Tag': TAG, 'Commit': COMMIT}
        require(manifest_path.is_file(), 'Manifest is missing')
        require(manifest_path.stat().st_size <= MANIFEST_LIMIT, 'Manifest exceeds byte limit')
        with manifest_path.open('rb') as stream:
            manifest_bytes = stream.read(MANIFEST_LIMIT + 1)
        require(len(manifest_bytes) <= MANIFEST_LIMIT, 'Manifest grew beyond byte limit')
        (output / 'manifest.json').write_bytes(manifest_bytes)
        report['ManifestSha256'] = sha(manifest_bytes)
        manifest = json.loads(manifest_bytes.decode('utf-8'), object_pairs_hook=unique_object)
        keys(manifest, ['schema_version', 'baseline', 'decoder', 'fixture', 'inputs', 'comparisons'])
        require(type(manifest['schema_version']) is int and manifest['schema_version'] == 1,
                'Unsupported schema_version')
        keys(manifest['baseline'], ['tag', 'commit'])
        require(manifest['baseline'] == {'tag': TAG, 'commit': COMMIT}, 'Pinned OTP baseline mismatch')
        require(isinstance(manifest['inputs'], list) and 1 <= len(manifest['inputs']) <= MAX_INPUTS,
                'Require between 1 and 32 provenance inputs')
        require(isinstance(manifest['comparisons'], list)
                and 1 <= len(manifest['comparisons']) <= MAX_COMPARISONS,
                'Require between 1 and 16 comparisons')
        total = 0
        receipt_index = 0

        def bound(binding, role):
            nonlocal total, receipt_index
            keys(binding, ['path', 'sha256'])
            require(isinstance(binding['path'], str) and binding['path'], 'Input path must be nonempty')
            require(isinstance(binding['sha256'], str) and len(binding['sha256']) == 64
                    and all(c in '0123456789abcdefABCDEF' for c in binding['sha256']),
                    'Input sha256 must be 64 hexadecimal characters')
            path = Path(binding['path'])
            if not path.is_absolute():
                path = manifest_path.parent / path
            path = path.resolve()
            require(not path.is_relative_to(output.resolve()), 'An input is inside the new output directory')
            receipt = {'Role': role, 'Path': str(path), 'ExpectedSha256': binding['sha256'].upper()}
            report['Inputs'].append(receipt)
            require(path.is_file(), 'Missing input: ' + str(path))
            require(path.stat().st_size <= FILE_LIMIT, 'Input exceeds 16 MiB limit: ' + str(path))
            require(total + path.stat().st_size <= TOTAL_LIMIT, 'Aggregate input byte limit exceeded')
            with path.open('rb') as stream:
                data = stream.read(FILE_LIMIT + 1)
            require(len(data) <= FILE_LIMIT and total + len(data) <= TOTAL_LIMIT,
                    'Input grew beyond byte limit')
            total += len(data)
            copy = 'inputs/{:03d}.bin'.format(receipt_index)
            receipt_index += 1
            (output / copy).write_bytes(data)
            receipt.update(Bytes=len(data), ActualSha256=sha(data), RetainedCopy=copy)
            require(receipt['ActualSha256'] == receipt['ExpectedSha256'], 'Input SHA256 mismatch: ' + str(path))
            return path, data

        decoder_path, decoder_bytes = bound(manifest['decoder'], 'decoder-source')
        expected_decoder = Path(__file__).resolve().parents[2] / 'tools/Erlang.GuestConnectedSendObservations/passive_etf.py'
        require(decoder_path == expected_decoder.resolve(), 'Decoder must be the existing repository helper')
        require(manifest['decoder']['sha256'].upper() == DECODER_SHA256, 'Decoder SHA256 is outside the reviewed profile')
        bound(manifest['fixture'], 'fixture')
        for index, item in enumerate(manifest['inputs']):
            bound(item, 'provenance-{}'.format(index))
        spec = importlib.util.spec_from_file_location('etf_snapshot_compare_decoder', decoder_path)
        decoder = importlib.util.module_from_spec(spec)
        # Import the exact repository source bytes verified above, without a
        # second file read or a cached .pyc replacing the reviewed parser.
        exec(compile(decoder_bytes, str(decoder_path), 'exec'), decoder.__dict__)
        names = set()
        for index, item in enumerate(manifest['comparisons']):
            entry = {'Index': index}
            report['Comparisons'].append(entry)
            try:
                keys(item, ['name', 'left', 'right'])
                name = item['name']
                require(isinstance(name, str) and 1 <= len(name) <= 128 and name not in names,
                        'Comparison names must be unique nonempty strings of at most 128 characters')
                names.add(name)
                entry['Name'] = name
                values = []
                side_errors = []
                for side in ['left', 'right']:
                    try:
                        _, data = bound(item[side], '{}-{}'.format(index, side))
                        values.append((data, decoder.decode_exact(data)))
                    except (InvalidInput, OSError, ValueError, RecursionError) as error:
                        side_errors.append({'Side': side, 'Error': str(error)})
                if side_errors:
                    entry.update(Status='INVALID_INPUT', Errors=side_errors)
                else:
                    left, right = values
                    equal = left[1] == right[1]
                    entry.update(Status='MATCH' if equal else 'DIFFERENT',
                                 DecodedEqual=equal, RawBytesEqual=left[0] == right[0])
                    for side, value in [('Left', left[1]), ('Right', right[1])]:
                        entry[side + 'TopLevelKind'] = value[0]
                        entry[side + 'TopLevelCount'] = len(value[1]) if value[0] in ('list', 'tuple', 'map') else None
            except (InvalidInput, OSError, ValueError) as error:
                entry.update(Status='INVALID_INPUT', Errors=[{'Error': str(error)}])
        statuses = [item['Status'] for item in report['Comparisons']]
        report['BytesRetained'] = total
        report['Status'] = 'INVALID_INPUT' if 'INVALID_INPUT' in statuses else 'DIFFERENT' if 'DIFFERENT' in statuses else 'MATCH'
        report['ExitCode'] = {'MATCH': 0, 'DIFFERENT': 1, 'INVALID_INPUT': 2}[report['Status']]
    except (InvalidInput, OSError, ValueError, TypeError, KeyError) as error:
        report['Errors'].append({'Error': str(error)})
    if created_output:
        try:
            save(output / 'report.json', report)
        except OSError as error:
            report['Errors'].append({'Error': 'Cannot retain report: ' + str(error)})
            report.update(Status='INVALID_INPUT', ExitCode=2)
    print(json.dumps(report, sort_keys=True, ensure_ascii=True))
    return report['ExitCode']


if __name__ == '__main__':
    raise SystemExit(main())
