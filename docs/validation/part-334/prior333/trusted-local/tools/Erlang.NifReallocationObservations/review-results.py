import argparse
import base64
import json
import pathlib
import sys
from reviewed_common import CASES, PIN, bound_helpers, sha, write_fresh

sys.dont_write_bytecode = True


def strict_object(pairs):
    value = {}
    for key, item in pairs:
        if key in value:
            raise ValueError('Duplicate JSON key')
        value[key] = item
    return value


def read(path):
    return json.loads(path.read_bytes(), object_pairs_hook=strict_object)


def exact_files(root, receipt_name, receipt):
    paths = set()
    for item in receipt['Files']:
        name = item['Path']
        if name in paths:
            raise ValueError('Duplicate receipt file')
        paths.add(name)
        path = (root / name).resolve()
        if not path.is_relative_to(root) or not path.is_file() or path.stat().st_size != item['Bytes'] or sha(path.read_bytes()) != item['Sha256']:
            raise ValueError('Receipt file changed')
    actual = {path.relative_to(root).as_posix() for path in root.rglob('*') if path.is_file()}
    if actual != paths | {receipt_name}:
        raise ValueError('Unexpected/missing captured artifact file')


def tuple_items(node, count):
    if node[0] != 'tuple' or len(node[1]) != count:
        raise ValueError('Exact tuple shape required')
    return node[1]


def atom(node, value):
    if node != ('atom', value):
        raise ValueError('Exact atom required')


def integer(node):
    if node[0] != 'integer' or type(node[1]) is not int:
        raise ValueError('Typed integer required')
    return node[1]


def named(node, name):
    first, value = tuple_items(node, 2)
    atom(first, name)
    return value


def enabled_driver(node):
    if node[0] != 'list' or not node[1]:
        raise ValueError('Actual enabled driver allocator instances required')
    for instance in node[1]:
        tag, number, details = tuple_items(instance, 3)
        atom(tag, 'instance')
        if integer(number) < 0 or details[0] != 'list':
            raise ValueError('Invalid allocator instance')
        options = [named(item, 'options') for item in details[1] if item[0] == 'tuple' and item[1][0] == ('atom', 'options')]
        if len(options) != 1 or options[0][0] != 'list':
            raise ValueError('Exact allocator options required')
        flags = [named(item, 'e') for item in options[0][1] if item[0] == 'tuple' and item[1][0] == ('atom', 'e')]
        if flags != [('atom', 'true')]:
            raise ValueError('Driver allocator enabled=true prerequisite failed')


def original_case(raw, spec, decoder):
    values = tuple_items(decoder.decode_exact(raw), 6)
    atom(values[0], 'reallocation_probe_case')
    atom(values[1], spec['Case'])
    if integer(named(values[2], 'selector')) != spec['Selector']:
        raise ValueError('Original selector changed')
    sizes = named(values[3], 'requested_sizes_authored')
    if sizes[0] != 'list' or [integer(item) for item in sizes[1]] != spec['RequestedSizes']:
        raise ValueError('Authored controls changed')
    enabled_driver(named(values[4], 'enabled_driver_before_call'))
    mask = integer(named(values[5], 'returned_mask'))
    if mask != spec['ExpectedReturnedMask']:
        raise ValueError('Actual original mask failed')
    return mask


def sdk_snapshot(snapshot, spec):
    sizes = spec['RequestedSizes']
    expected = {'Count': 0, 'RequestedBytes': 0, 'BackingRequestBytes': 0,
        'PeakRequestedBytes': max(sizes), 'PeakBackingRequestBytes': max(max(sizes), 1),
        'AllocatedRequestedBytes': sum(sizes), 'ReleasedRequestedBytes': sum(sizes),
        'AllocatedBackingRequestBytes': sum(max(size, 1) for size in sizes),
        'ReleasedBackingRequestBytes': sum(max(size, 1) for size in sizes),
        'SuccessfulAllocations': 1, 'ReleasedAllocations': 1}
    if snapshot != expected or any(type(value) is not int for value in snapshot.values()):
        raise ValueError('Actual SDK requested/backing history mismatch')


def main():
    parser = argparse.ArgumentParser()
    for name in ('repository', 'plan', 'original', 'sdk', 'output'):
        parser.add_argument('--' + name, type=pathlib.Path, required=True)
    for name in ('expected-head', 'approved-plan-sha256', 'approved-original-receipt-sha256', 'approved-sdk-receipt-sha256'):
        parser.add_argument('--' + name, required=True)
    args = parser.parse_args()
    root, original, sdk = args.repository.resolve(), args.original.resolve(), args.sdk.resolve()
    if args.output.exists() or sha(args.plan.read_bytes()) != args.approved_plan_sha256.upper():
        raise ValueError('Fresh result and exact approved plan required')
    plan = read(args.plan)
    helpers = bound_helpers(root, plan)
    decoder = helpers['passive_etf']
    schema = read(root / 'tools/Erlang.NifReallocationObservations/result-schema.json')
    if sha((root / 'tools/Erlang.NifReallocationObservations/result-schema.json').read_bytes()) != plan['ResultSchemaSha256']:
        raise ValueError('Reviewed result schema changed')
    one, two = read(original / 'receipt.json'), read(sdk / 'sdk-receipt.json')
    if sha((original / 'receipt.json').read_bytes()) != args.approved_original_receipt_sha256.upper() or sha((sdk / 'sdk-receipt.json').read_bytes()) != args.approved_sdk_receipt_sha256.upper():
        raise ValueError('Exact recorded receipt digests required')
    if one['Complete'] is not True or two['Complete'] is not True or one['Head'] != args.expected_head or two['Head'] != args.expected_head or one['PinnedOtpCommit'] != PIN:
        raise ValueError('Completion/head/pin mismatch')
    if one['PlanSha256'] != args.approved_plan_sha256.upper() or two['PlanSha256'] != args.approved_plan_sha256.upper() or one['Cases'] != CASES:
        raise ValueError('Plan/case provenance mismatch')
    exact_files(original, 'receipt.json', one)
    exact_files(sdk, 'sdk-receipt.json', two)
    if sha((original / 'approved-source-build-receipt.json').read_bytes()) != one['IndependentlyApprovedSourceBuildReceiptSha256']:
        raise ValueError('Source-build proof copy changed')
    metadata = tuple_items(decoder.decode_exact((original / 'metadata.etf').read_bytes()), 7)
    atom(metadata[0], 'reallocation_probe_metadata')
    if integer(named(metadata[4], 'wordsize')) != 8:
        raise ValueError('Original x64 wordsize required')
    enabled_driver(named(metadata[6], 'driver_allocator'))

    if (sdk / 'sdk.stderr').read_bytes() or read(sdk / 'sdk.process.json')['ExitCode'] != 0:
        raise ValueError('SDK child failed')
    lines = (sdk / 'sdk.stdout').read_text().splitlines()
    if len(lines) != len(CASES) + 1:
        raise ValueError('Exact SDK stdout JSON rows required')
    captured = [json.loads(line, object_pairs_hook=strict_object) for line in lines]
    compared = []
    bindings = read(original / 'result-bindings.json')['Bindings']
    if [item['Prefix'] for item in bindings] != ['metadata', *CASES]:
        raise ValueError('Exact original capture linkage required')
    for binding in bindings:
        prefix = binding['Prefix']
        for key, path in [('EtfSha256', original / binding['Target']), ('StdoutSha256', original / (prefix + '.stdout')), ('StderrSha256', original / (prefix + '.stderr')), ('ProcessSha256', original / (prefix + '.process.json'))]:
            if sha(path.read_bytes()) != binding[key]:
                raise ValueError('Original raw/process linkage mismatch')
        process = read(original / (prefix + '.process.json'))
        if process['ExitCode'] != 0 or process['TimedOut'] or not process['PipesClosed'] or not process['ParentReaped'] or (original / (prefix + '.stderr')).read_bytes():
            raise ValueError('Original child failed')
        if process['Arguments'][-2:] != ['-eval', binding['Evaluation']]:
            raise ValueError('Original authored evaluation mismatch')
    for spec, actual in zip(schema['Cases'], captured[:-1], strict=True):
        required = {'Role', 'Case', 'Selector', 'ReturnedMask', 'ReturnedEtfBase64', 'RequestedSizes', 'RequestedSizesKind', 'HeldAdmissionDelayedUnload', 'OwnerSnapshot', 'UnloadedDomainSnapshot'}
        if set(actual) != required or actual['Role'] != 'actual-native-reallocation-case' or actual['Case'] != spec['Case'] or type(actual['Selector']) is not int or actual['Selector'] != spec['Selector']:
            raise ValueError('SDK case schema mismatch')
        mask = original_case((original / (spec['Case'] + '.etf')).read_bytes(), spec, decoder)
        if type(actual['ReturnedMask']) is not int or actual['ReturnedMask'] != mask or decoder.decode_exact(base64.b64decode(actual['ReturnedEtfBase64'], validate=True)) != ('integer', mask):
            raise ValueError('Actual original/SDK masks differ')
        if any(type(value) is not int for value in actual['RequestedSizes']) or actual['RequestedSizes'] != spec['RequestedSizes'] or actual['RequestedSizesKind'] != 'AuthoredControl' or actual['HeldAdmissionDelayedUnload'] is not True:
            raise ValueError('SDK controls or held admission missing')
        sdk_snapshot(actual['OwnerSnapshot'], spec)
        if set(actual['UnloadedDomainSnapshot']) != {'JoinedOwners', 'Allocations'} or type(actual['UnloadedDomainSnapshot']['JoinedOwners']) is not int or actual['UnloadedDomainSnapshot']['JoinedOwners'] != 0 or actual['UnloadedDomainSnapshot']['Allocations'] != actual['OwnerSnapshot']:
            raise ValueError('Actual unload domain not drained')
        compared.append({'Case': spec['Case'], 'ActualOriginalMask': mask, 'ActualSdkMask': actual['ReturnedMask'], 'RequestedSizesKind': 'AuthoredControl'})
    control = captured[-1]
    expected_controls = {'Role': 'actual-native-reallocation-controls', 'OneShotInitialization': True, 'OldFiveArgumentConstructor': True,
        'ActualOldLibraryOwnerMixedPermissionDenied': True, 'NestedPermissionRestored': True, 'LinuxGlobalSymbolResident': True,
        'WindowsHeaderLayoutVerified': False, 'ActualAllocatorOom': False}
    if control != expected_controls or any(type(value) is not bool for key, value in control.items() if key != 'Role'):
        raise ValueError('Linux exact control result mismatch')
    write_fresh(args.output, {'Passed': True, 'Head': args.expected_head, 'PinnedOtpCommit': PIN, 'Cases': compared, 'ActualAllocatorOom': False, 'PhysicalMemoryCompared': False, 'DisabledProfileCompared': False,
        'OriginalReceiptSha256': sha((original / 'receipt.json').read_bytes()), 'SdkReceiptSha256': sha((sdk / 'sdk-receipt.json').read_bytes())})


if __name__ == '__main__':
    main()
