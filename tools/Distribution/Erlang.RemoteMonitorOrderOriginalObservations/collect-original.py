"""Reuse the published owned original two-node collector for six remote monitor-order contracts."""
from pathlib import Path, PurePosixPath
import argparse, hashlib, importlib.util, json, re, signal, subprocess, sys

sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL = 'tools/Distribution/Erlang.RemoteMonitorOrderOriginalObservations'
CASES = ('spawn_plain_last', 'spawn_custom_last', 'request_plain_last', 'request_custom_last', 'spawn_empty_tuple_last', 'request_empty_tuple_last')
H = lambda p: hashlib.sha256(p.read_bytes()).hexdigest().upper()
J = lambda p: json.loads(p.read_bytes())

def require(value, message):
    if not value:
        raise ValueError(message)

def checked(root, name):
    value = PurePosixPath(name)
    require(isinstance(name, str) and not value.is_absolute() and '..' not in value.parts and ':' not in name and '\\' not in name, 'Safe source path')
    q = (root / name).resolve(strict=True)
    require(q.is_relative_to(root) and q.is_file(), 'Source escape')
    return q

def binding(q, root):
    return dict(Path=q.relative_to(root).as_posix(), Bytes=q.stat().st_size, Sha256=H(q))

def verify(a):
    root = a.repository.resolve(strict=True)
    tool = root / TOOL
    manifest = tool / 'source-manifest.json'
    require(re.fullmatch('[A-Fa-f0-9]{64}', a.manifest_sha256) and H(manifest) == a.manifest_sha256.upper(), 'Explicit reviewed source manifest')
    m = J(manifest)
    require(m['PinnedOtpCommit'] == PIN and m['Cases'] == list(CASES), 'Pinned six-case scope')
    require({q.relative_to(tool).as_posix() for q in tool.rglob('*') if q.is_file() and q != manifest} == {row['Path'] for row in m['Files']}, 'Exact new tool source closure')
    for row in m['Files']:
        require(binding(checked(tool, row['Path']), tool) == row, 'Collector source drift')
    for row in m['ExternalDependencies'] + [m['ReusedCollector'], m['Workflow']]:
        require(binding(checked(root, row['Path']), root) == row, 'Published dependency or workflow drift')
    require(re.fullmatch('[0-9a-f]{40}', a.expected_head) and subprocess.check_output(['git', '-C', str(root), 'rev-parse', 'HEAD'], timeout=30).decode().strip() == a.expected_head, 'Exact checkout')
    return root, tool, m

def selected_contracts(a, existing, m):
    root = a.repository.resolve(strict=True)
    decoder = existing.load(root, m, 'passive_etf')
    output = a.output.resolve(strict=True)
    rows = []
    for case in CASES:
        folder = output / case
        record = existing.map_fields(existing.decode(decoder, folder / 'monitor-order.etf'))
        get = lambda key: record[('atom', key)]
        request = case.startswith('request_')
        custom_last = case.endswith('custom_last')
        empty_tuple_last = case.endswith('empty_tuple_last')
        origin, remote, child, reference = (get(x) for x in ('observer_node', 'remote_node', 'child', 'primary_ref'))
        require(origin[0] == remote[0] == 'atom' and origin != remote, 'Actual two distinct original nodes')
        require(child[0] == 'pid' and child[1] == remote and reference[0] == 'reference' and reference[1] == origin, 'Foreign child and origin-owned reference')
        down = get('down')
        require(down[0] == 'tuple' and len(down[1]) == 5, 'Actual raw five-field monitor message')
        tag, down_ref, kind, down_child, reason = down[1]
        require(tag == ('atom', 'DOWN' if empty_tuple_last else 'custom_monitor_order'), 'Pinned remote atom retention and tuple reset contract')
        require(down_ref == reference and down_child == child and kind == ('atom', 'process') and reason == ('atom', 'normal'), 'Actual DOWN identity, kind and reason')
        api = 'spawn_request' if request else 'spawn_opt'
        require(get('api') == ('atom', api) and get('custom_last') == ('atom', 'true' if custom_last else 'false') and get('empty_tuple_last') == ('atom', 'true' if empty_tuple_last else 'false'), 'Source case binding')
        custom = ('tuple', (('atom', 'monitor'), ('list', (('tuple', (('atom', 'tag'), ('atom', 'custom_monitor_order'))),))))
        empty_monitor = ('tuple', (('atom', 'monitor'), ('list', ())))
        opts = ('list', (custom, empty_monitor) if empty_tuple_last else (('atom', 'monitor'), custom) if custom_last else (custom, ('atom', 'monitor')))
        require(get('options') == opts, 'Actual source option order')
        if request:
            require(get('spawn_return') == reference and get('request_reply') == ('tuple', (('atom', 'spawn_reply'), reference, ('atom', 'ok'), child)), 'Request return/reply correlation')
        else:
            require(get('spawn_return') == ('tuple', (child, reference)) and get('request_reply') == ('atom', 'none'), 'Spawn result correlation')
        require(existing.decode(decoder, folder / 'primary-raw-down.etf') == down, 'Raw DOWN capture equals selected field')
        require(existing.decode(decoder, folder / 'child-ready.etf') == ('tuple', (child, remote)), 'Child reports executing on remote node')
        rows.append(dict(Name=case, Api=api, CustomLast=custom_last, EmptyTupleLast=empty_tuple_last, ObservedTag=tag[1], DownReferenceMatchesReturn=True, DownPidMatchesRemoteChild=True, ObserverAndRemoteDiffer=True, Kind='process', Reason='normal', RequestReplyCorrelated=request,
                         DynamicIdentities=dict(Observer=origin, Remote=remote, Child=child, Reference=reference),
                         RawRecord=binding(folder / 'monitor-order.etf', output), RawDown=binding(folder / 'primary-raw-down.etf', output)))
    existing.save(output / 'selected-contracts.json', dict(Status='OBSERVED_SIX_ACTUAL_REMOTE_MONITOR_ORDER_CONTRACTS', Cases=rows, PinnedOtpCommit=PIN, CollectorHead=a.expected_head, SourceManifestSha256=a.manifest_sha256.upper(), SdkExecuted=False, NativeTcpPeers=True, OriginalWirePacketsCaptured=False, FullTraceCompared=False, Scope='Exact selected tag/identity/foreign-node relations; dynamic original identifiers retained, not substituted or claimed byte-equal to SDK traces.'))

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['verify', 'collect'])
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--manifest-sha256', required=True)
    parser.add_argument('--expected-head', required=True)
    parser.add_argument('--otp-source', type=Path)
    parser.add_argument('--erl', type=Path)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--timeout-seconds', type=int, default=60)
    a = parser.parse_args()
    require(1 <= a.timeout_seconds <= 120, 'Bounded process timeout')
    root, tool, m = verify(a)
    if a.mode == 'verify':
        print('SOURCE_VERIFIED_NOT_EXECUTED')
        return 0
    require(a.otp_source and a.erl and a.output, 'Original execution arguments')
    dependency = checked(root, m['ReusedCollector']['Path'])
    spec = importlib.util.spec_from_file_location('reviewed_owned_peer358', dependency)
    existing = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(existing)
    # Reuse ownership, provenance, compilation, peer joins and cleanup implementation unchanged.
    # Its two fixture module names stay isolated to each fresh owned original VM pair.
    existing.TOOL, existing.CASES, existing.verify = TOOL, CASES, verify
    signal.signal(signal.SIGTERM, existing.stop)
    signal.signal(signal.SIGINT, existing.stop)
    result = existing.collect(a)
    if result:
        return result
    selected_contracts(a, existing, m)
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
