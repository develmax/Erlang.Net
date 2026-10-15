from pathlib import Path
import argparse
import hashlib
import json
import struct
import zlib

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
TAG = 'OTP-29.1.1'
AUTHORING_SHA256 = '371DE67C8D27EB2AC6196DEB3CDAA943088A1CD65855D30D5BFB279D39DC400E'
VERSION = 131
MAX_BYTES = 16 * 1024 * 1024
MAX_DEPTH = 128


def require(value, message):
    if not value:
        raise ValueError(message)


def digest(data):
    return hashlib.sha256(data).hexdigest().upper()


def json_file(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def declared_term(value):
    if 'Atom' in value:
        return ('atom', value['Atom'])
    if 'Integer' in value:
        return ('integer', value['Integer'])
    if 'Tuple' in value:
        return ('tuple', tuple(map(declared_term, value['Tuple'])))
    if 'List' in value:
        return ('list', tuple(map(declared_term, value['List'])))
    if 'ImproperList' in value:
        row = value['ImproperList']
        return ('improper', tuple(map(declared_term, row['Items'])), declared_term(row['Tail']))
    if 'Map' in value:
        return ('map', frozenset((declared_term(k), declared_term(v)) for k, v in value['Map']))
    raise ValueError('Unknown static term declaration')


def decode_one(data):
    require(len(data) <= MAX_BYTES and len(data) > 1 and data[0] == VERSION, 'ETF version/size')
    position = 1

    def take(count):
        nonlocal position
        require(count >= 0 and position + count <= len(data), 'Truncated ETF')
        result = data[position:position + count]
        position += count
        return result

    def count32():
        return int.from_bytes(take(4), 'big')

    def term(depth):
        require(depth <= MAX_DEPTH, 'ETF diagnostic depth limit')
        tag = take(1)[0]
        if tag == 97:
            return ('integer', take(1)[0])
        if tag == 98:
            return ('integer', int.from_bytes(take(4), 'big', signed=True))
        if tag in (110, 111):
            count = take(1)[0] if tag == 110 else count32()
            sign = take(1)[0]
            require(sign in (0, 1), 'Invalid big integer sign')
            number = int.from_bytes(take(count), 'little')
            return ('integer', -number if sign else number)
        if tag in (100, 115, 118, 119):
            size = take(1)[0] if tag in (115, 119) else int.from_bytes(take(2), 'big')
            return ('atom', take(size).decode('utf-8' if tag in (118, 119) else 'latin1'))
        if tag in (104, 105):
            size = take(1)[0] if tag == 104 else count32()
            require(size <= len(data) - position, 'Tuple size exceeds available data')
            return ('tuple', tuple(term(depth + 1) for _ in range(size)))
        if tag == 106:
            return ('list', ())
        if tag == 107:
            size = int.from_bytes(take(2), 'big')
            return ('list', tuple(('integer', byte) for byte in take(size)))
        if tag == 108:
            size = count32()
            require(size <= len(data) - position, 'List size exceeds available data')
            items = tuple(term(depth + 1) for _ in range(size))
            tail = term(depth + 1)
            if tail[0] == 'list':
                return ('list', items + tail[1])
            if tail[0] == 'improper':
                return ('improper', items + tail[1], tail[2])
            return ('improper', items, tail)
        if tag == 116:
            size = count32()
            require(size <= (len(data) - position) // 2, 'Map size exceeds available data')
            pairs = [(term(depth + 1), term(depth + 1)) for _ in range(size)]
            require(len({key for key, _ in pairs}) == size, 'Duplicate map key')
            return ('map', frozenset(pairs))
        raise ValueError('Unsupported ETF tag: ' + str(tag))

    result = term(0)
    return result, position


def decode_exact(data):
    term, used = decode_one(data)
    require(used == len(data), 'Trailing bytes in result ETF')
    return term


def chunks(data):
    require(len(data) >= 12 and data[:4] == b'FOR1' and data[8:12] == b'BEAM', 'Invalid BEAM form')
    require(int.from_bytes(data[4:8], 'big') + 8 == len(data), 'BEAM form length')
    result = []
    at = 12
    while at < len(data):
        require(at + 8 <= len(data), 'Truncated chunk header')
        identifier = data[at:at + 4].decode('ascii')
        size = int.from_bytes(data[at + 4:at + 8], 'big')
        start = at + 8
        end = start + size
        next_at = end + ((4 - size % 4) % 4)
        require(next_at <= len(data), 'Truncated chunk/padding')
        require(data[end:next_at] == bytes(next_at - end), 'Fixture chunk padding must be zero')
        result.append((identifier, data[start:end]))
        at = next_at
    require(at == len(data), 'Chunk termination')
    return result


def safe_file(root, name):
    require(name and Path(name).name == name, 'Evidence path escapes root')
    path = root / name
    require(path.is_file(), 'Missing evidence file: ' + str(path))
    return path


def verify(root, authoring_path, expected_head, expected_run, expected_attempt, control_only=False):
    root = root.resolve()
    require(digest(authoring_path.read_text(encoding='utf-8-sig').replace('\r\n', '\n').encode('utf-8')) == AUTHORING_SHA256, 'Static authoring source digest')
    authoring = json_file(authoring_path)
    require(len(authoring['Cases']) == 24, 'Static24 declaration count')
    manifest = json_file(root / 'manifest.json')
    require(manifest['SourcePin'] == PIN and manifest['SourceTag'] == TAG and manifest['InstalledVersion'] == '29.1.1', 'Original pin/version')
    require(len(manifest['Fixtures']) == 6 and manifest['OriginalFixturesExecuted'] == 5, 'Retained six/five evidence')
    build_bytes = (root / 'source-build-evidence.json').read_bytes()
    build = json.loads(build_bytes)
    require(digest(build_bytes) == manifest['SourceBuildEvidenceSha256'], 'Source-build evidence digest')
    require(build['SourceCommit'] == PIN and build['SourceTag'] == TAG and build['SourceBuildCompleted'] is True, 'Source-build pin/completion')
    require(build['HarnessCommit'] == expected_head and str(build['RunId']) == str(expected_run) and str(build['RunAttempt']) == str(expected_attempt) and build['Job'] == 'fixtures', 'Run/head/job binding')
    require(build['InstalledOtpRoot'] == manifest['InstalledOtpRoot'], 'Installed-root binding')
    require(digest((root / 'source-build.log').read_bytes()) == build['SourceBuildLogSha256'] == manifest['SourceBuildLogSha256'], 'Original source-build log digest')
    for fixture, declared in zip(manifest['Fixtures'], authoring['BaselineSixSources']):
        require(fixture['Module'] == declared['Module'], 'Retained fixture order')
        require(digest(safe_file(root, fixture['SourceFile']).read_bytes()) == fixture['SourceSha256'] == declared['SourceSha256'], 'Retained source hash')
        require(digest(safe_file(root, fixture['BeamFile']).read_bytes()) == fixture['BeamSha256'], 'Retained BEAM hash')
        require(fixture['Execution']['IndependentExpectedExpression'] == declared['IndependentExpectedExpression'], 'Retained independent expression')
        require(fixture['Metadata']['FileOnlyQuery'] is True and fixture['Metadata']['ModuleLoadPerformed'] is False, 'Retained file-only metadata scope')
        if declared['IndependentExpectedExpression'] is not None:
            execution = fixture['Execution']
            require(execution['Executed'] is True and execution['ExitCode'] == 0, 'Retained actual native execution')
            actual = safe_file(root, execution['ActualEtfFile']).read_bytes()
            expected = safe_file(root, execution['ExpectedEtfFile']).read_bytes()
            require(actual == expected and digest(actual) == execution['ActualEtfSha256'] == execution['ExpectedEtfSha256'], 'Retained native body bytes')
            require(decode_exact(actual) == declared_term(declared['ExpectedBodyEnvelope']), 'Retained body differs from static independent term')
        else:
            require(fixture['Execution']['Executed'] is False, 'Retained on_load fixture must remain unexecuted')
    mutation_root = root / 'metadata-mutations'
    declarations = json_file(mutation_root / 'metadata-mutation-declarations.json')
    require(declarations['SourcePin'] == PIN and declarations['SourceTag'] == TAG and declarations['OnLoadExecuted'] is False, 'Mutation source/scope binding')
    require(len(declarations['Declarations']) == 24, 'Runtime declaration count')
    require(declarations['BaselineFixtures'] == [{'Module': x['Module'], 'SourceSha256': x['SourceSha256'], 'IndependentExpectedExpression': x['IndependentExpectedExpression']} for x in authoring['BaselineSixSources']], 'Mutation original-source binding')
    evidence = json_file(mutation_root / 'metadata-mutation-results.json')
    require(evidence['Complete'] is True and evidence['OriginalObserved'] is True and evidence['OnLoadExecuted'] is False and len(evidence['Evidence']) == 24, 'Complete actual24 evidence required')
    original = safe_file(root, manifest['Fixtures'][0]['BeamFile']).read_bytes()
    require(digest(original) == declarations['InputBeamSha256'], 'Mutation input binding')
    original_chunks = chunks(original)
    rows = []
    names = set()
    for authored, declared, observed in zip(authoring['Cases'], declarations['Declarations'], evidence['Evidence']):
        name = authored['Name']
        require(name not in names, 'Duplicate mutation name')
        names.add(name)
        for key in ('Name', 'Chunk', 'PayloadKind', 'TermExpression', 'IndependentExpectedExpression', 'ExpectedLoadAccepted', 'BodyExpected'):
            require(declared[key] == authored[key], 'Mutation declaration changed: ' + key)
        for key in ('Name', 'Chunk', 'ExpectedLoadAccepted', 'IndependentExpectedExpression'):
            require(observed[key] == authored[key], 'Observed contract binding changed: ' + key)
        require(observed['ExitCode'] == 0 and int(safe_file(mutation_root, name + '.oracle.exit-code.txt').read_text().strip()) == 0, 'Mutation native exitcode')
        files = {}
        for suffix, key in (('.beam', 'BeamSha256'), ('.payload.bin', 'PayloadSha256'), ('.actual.etf', 'ActualEtfSha256'), ('.expected.etf', 'ExpectedEtfSha256'), ('.eval.erl', 'EvaluationSha256')):
            data = safe_file(mutation_root, name + suffix).read_bytes()
            require(digest(data) == observed[key], 'Mutation raw file digest')
            files[suffix] = data
        require(files['.actual.etf'] == files['.expected.etf'], 'Mutation actual/expected byte comparison')
        wanted = declared_term(authored['ExpectedRuntimeTerm'])
        require(decode_exact(files['.actual.etf']) == wanted and decode_exact(files['.expected.etf']) == wanted, 'Native result differs from static independent term')
        payload = files['.payload.bin']
        actual_chunks = chunks(files['.beam'])
        expected_chunks = [(identifier, data) for identifier, data in original_chunks if authored['PayloadKind'] != 'absent' or identifier != authored['Chunk']]
        if authored['PayloadKind'] != 'absent':
            expected_chunks = [(identifier, payload if identifier == authored['Chunk'] else data) for identifier, data in expected_chunks]
        require(actual_chunks == expected_chunks, 'Only selected metadata chunk may change')
        kind = authored['PayloadKind']
        if kind in ('absent', 'zero'):
            require(payload == b'', 'Absent/zero payload')
        elif kind == 'truncated':
            require(payload == bytes([VERSION]), 'Exact truncated-version control')
        elif kind == 'compressed':
            require(len(payload) > 6 and payload[:2] == bytes([VERSION, 80]), 'Exact compressed metadata control')
            size = int.from_bytes(payload[2:6], 'big')
            require(size <= MAX_BYTES, 'Compressed diagnostic size bound')
            inflater = zlib.decompressobj()
            unpacked = inflater.decompress(payload[6:], MAX_BYTES + 1)
            require(len(unpacked) <= MAX_BYTES and inflater.eof and not inflater.unused_data and not inflater.unconsumed_tail, 'Compressed control bounded complete stream')
            require(len(unpacked) == size, 'Compressed control declared size')
            require(decode_exact(bytes([VERSION]) + unpacked) == ('list', tuple(('integer', 73) for _ in range(256))), 'Compressed control source binding')
        else:
            value, used = decode_one(payload)
            require(value == wanted[1][1], 'Metadata first term source binding')
            require(payload[used:] == (bytes([170, 187]) if kind == 'trailing' else b''), 'Payload used/trailing binding')
        evaluation = files['.eval.erl'].decode('utf-8-sig')
        require('code:load_binary(beam_fixture_constant' in evaluation and 'Md5=:=OriginalMd5' in evaluation and 'erlang:get_module_info(beam_fixture_constant' in evaluation, 'Actual original load/query/checksum evaluation binding')
        text = safe_file(mutation_root, name + '.actual.term').read_bytes()
        stdout = safe_file(mutation_root, name + '.oracle.stdout.txt').read_bytes()
        stderr = safe_file(mutation_root, name + '.oracle.stderr.txt').read_bytes()
        rows.append({'Name': name, 'ActualSha256': observed['ActualEtfSha256'], 'RawTextSha256': digest(text), 'StdoutSha256': digest(stdout), 'StderrSha256': digest(stderr)})
    result = {'Status': 'SYNTHETIC_VERIFIER_CONTROL_PASS' if control_only else 'RAW24_ORIGINAL_EVIDENCE_PASS', 'SourcePin': PIN, 'HarnessCommit': expected_head, 'RunId': str(expected_run), 'RunAttempt': str(expected_attempt), 'DeclaredCases': 24, 'ActualCases': 24, 'RawRows': rows, 'RetainedCompilerFixtures': 6, 'RetainedBodyExecutions': 5, 'OnLoadMutationExecution': False, 'WholeRuntimeParityClaim': False}
    result['OriginalObserved'] = not control_only
    if not control_only:
        (mutation_root / 'metadata-mutation-verifier-results.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    if not control_only:
        print('PASS actual24 raw metadata cases bound to static authoring terms and original run/head/pin; retained six/five contracts exact')
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--expected-head', required=True)
    parser.add_argument('--expected-run', required=True)
    parser.add_argument('--expected-attempt', required=True)
    args = parser.parse_args()
    verify(args.output, Path(__file__).with_name('metadata-mutation-declarations.json'), args.expected_head, args.expected_run, args.expected_attempt)


if __name__ == '__main__':
    main()
