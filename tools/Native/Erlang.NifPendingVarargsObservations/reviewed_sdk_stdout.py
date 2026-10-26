import json
import re


MANAGED_PASS = 'Managed backend checks passed; native ABI and original OTP NOT executed.'
NATIVE_PASS = 'Internal trusted native fixture checks passed; general NIF ABI NOT implemented.'
ROUTE_PASS = 'Trusted native generated-source entry routes passed; BEAM/old-image/general ABI NOT established.'
ABI_PASS = 'Bounded native ABI cursor/pending checks passed; original OTP NOT executed by this harness.'
HASH_PREFIX = 'Native generated source SHA256: '
VETO_ROLE = 'actual-native-load-veto-before-payload-allocation-domain-closed'
LOADED_ROLE = 'actual-native-load-callback-payload-matches-library-owner-ledger'
UNLOADED_ROLE = 'actual-native-retirement-unload-releases-payload-and-domain-membership'
ALLOCATION_FIELDS = frozenset([
    'Count', 'RequestedBytes', 'BackingRequestBytes', 'PeakRequestedBytes',
    'PeakBackingRequestBytes', 'AllocatedRequestedBytes', 'ReleasedRequestedBytes',
    'AllocatedBackingRequestBytes', 'ReleasedBackingRequestBytes',
    'SuccessfulAllocations', 'ReleasedAllocations',
])
LONG_FIELDS = frozenset(['SuccessfulAllocations', 'ReleasedAllocations'])
INT_MAX = (1 << 31) - 1
LONG_MAX = (1 << 63) - 1
ULONG_MAX = (1 << 64) - 1


def require(value, message):
    if not value:
        raise ValueError('SDK stdout contract: ' + message)


def strict_object(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, 'duplicate JSON member')
        result[key] = value
    return result


def reject_constant(value):
    raise ValueError('SDK stdout contract: non-finite JSON number ' + value)


def exact_object(value, fields, label):
    require(type(value) is dict and set(value) == set(fields), label + ' exact fields')


def bounded_integer(value, maximum, label):
    require(type(value) is int and 0 <= value <= maximum, label + ' integer range')


def allocation(value):
    exact_object(value, ALLOCATION_FIELDS, 'allocation snapshot')
    for name, number in value.items():
        maximum = INT_MAX if name == 'Count' else LONG_MAX if name in LONG_FIELDS else ULONG_MAX
        bounded_integer(number, maximum, name)
    require(value['RequestedBytes'] <= value['BackingRequestBytes'], 'current requested/backing ordering')
    require(value['ReleasedRequestedBytes'] <= value['AllocatedRequestedBytes'], 'released requested exceeds allocated')
    require(value['ReleasedBackingRequestBytes'] <= value['AllocatedBackingRequestBytes'], 'released backing exceeds allocated')
    require(value['ReleasedAllocations'] <= value['SuccessfulAllocations'], 'released count exceeds successful')
    require(value['AllocatedRequestedBytes'] - value['ReleasedRequestedBytes'] == value['RequestedBytes'], 'requested conservation')
    require(value['AllocatedBackingRequestBytes'] - value['ReleasedBackingRequestBytes'] == value['BackingRequestBytes'], 'backing conservation')
    require(value['SuccessfulAllocations'] - value['ReleasedAllocations'] == value['Count'], 'block conservation')
    require(value['RequestedBytes'] <= value['PeakRequestedBytes'] <= value['AllocatedRequestedBytes'], 'requested peak bounds')
    require(value['BackingRequestBytes'] <= value['PeakBackingRequestBytes'] <= value['AllocatedBackingRequestBytes'], 'backing peak bounds')
    require(value['PeakRequestedBytes'] <= value['PeakBackingRequestBytes'], 'peak requested/backing ordering')
    require(value['AllocatedRequestedBytes'] <= value['AllocatedBackingRequestBytes'], 'allocated requested/backing ordering')
    require(value['ReleasedRequestedBytes'] <= value['ReleasedBackingRequestBytes'], 'released requested/backing ordering')
    return value


def observation(line, role, has_owner):
    value = json.loads(line, object_pairs_hook=strict_object, parse_constant=reject_constant)
    exact_object(value, ['Role', 'Snapshot', 'OwnerSnapshot'] if has_owner else ['Role', 'Snapshot'], 'role envelope')
    require(value['Role'] == role, 'unknown, duplicate or misordered role')
    snapshot = value['Snapshot']
    exact_object(snapshot, ['JoinedOwners', 'Allocations'], 'runtime snapshot')
    bounded_integer(snapshot['JoinedOwners'], INT_MAX, 'JoinedOwners')
    blocks = allocation(snapshot['Allocations'])
    if has_owner:
        owner = allocation(value['OwnerSnapshot'])
        require(owner == blocks, 'loaded owner/runtime snapshot mismatch')
    return value


def parse_sdk_stdout(stdout, stderr, require_abi=True):
    """Validate actual capture bytes; this pure parser does not execute native/original code."""
    require(type(stdout) is bytes and type(stderr) is bytes and type(require_abi) is bool, 'explicit byte capture/profile')
    require(not stderr, 'stderr is not empty')
    text = stdout.decode('utf-8', errors='strict').replace('\r\n', '\n')
    require('\r' not in text, 'unsupported line delimiter')
    lines = text.split('\n')
    if lines[-1] == '':
        lines.pop()
    require(len(lines) == (8 if require_abi else 7), 'exact output line count')
    require(lines[0] == MANAGED_PASS and lines[4] == NATIVE_PASS and lines[6] == ROUTE_PASS, 'legacy ordered success lines')
    require(lines[5].startswith(HASH_PREFIX), 'legacy generated-source hash prefix')
    generated_hash = lines[5][len(HASH_PREFIX):]
    require(re.fullmatch('[0-9A-F]{64}', generated_hash) is not None, 'legacy generated-source hash format')
    if require_abi:
        require(lines[7] == ABI_PASS, 'legacy final ABI cursor/pending success')
    veto = observation(lines[1], VETO_ROLE, False)
    loaded = observation(lines[2], LOADED_ROLE, True)
    unloaded = observation(lines[3], UNLOADED_ROLE, False)
    require(veto['Snapshot']['JoinedOwners'] == 0 and all(value == 0 for value in veto['Snapshot']['Allocations'].values()), 'veto before allocation must retain exact empty domain/history')
    current = loaded['Snapshot']['Allocations']
    require(loaded['Snapshot']['JoinedOwners'] == 1 and current['Count'] == 1 and current['RequestedBytes'] > 0, 'loaded positive actual payload/one owner')
    require(current['SuccessfulAllocations'] == 1 and current['ReleasedAllocations'] == 0, 'loaded single successful allocation')
    require(current['AllocatedRequestedBytes'] == current['RequestedBytes'] and current['AllocatedBackingRequestBytes'] == current['BackingRequestBytes'], 'loaded allocation history equals current payload')
    released = unloaded['Snapshot']['Allocations']
    require(unloaded['Snapshot']['JoinedOwners'] == 0 and released['Count'] == released['RequestedBytes'] == released['BackingRequestBytes'] == 0, 'unloaded current counts/bytes/membership must drain')
    for name in ('AllocatedRequestedBytes', 'AllocatedBackingRequestBytes', 'SuccessfulAllocations', 'PeakRequestedBytes', 'PeakBackingRequestBytes'):
        require(released[name] == current[name], 'unloaded history differs from loaded: ' + name)
    require(released['ReleasedRequestedBytes'] == released['AllocatedRequestedBytes'] and released['ReleasedBackingRequestBytes'] == released['AllocatedBackingRequestBytes'] and released['ReleasedAllocations'] == released['SuccessfulAllocations'], 'unloaded exact allocation release history')
    return {
        'GeneratedCSharpSha256': generated_hash,
        'NativePayloadDomainObservations': [veto, loaded, unloaded],
        'NativePayloadDomainRolesValidated': True,
        'LoadedOwnerSnapshotMatchesRuntime': True,
        'LegacyOrderedSuccessLines': [lines[0], lines[4], lines[5], lines[6]] + ([lines[7]] if require_abi else []),
    }
