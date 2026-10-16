"""Original-only connected spawn controller observations; never runs SDK or writes expected values."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path, PurePosixPath
import re
import signal
import subprocess
import sys
import time

sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
VERSION = '29.1.1'
TOOL = 'tools/Erlang.ConnectedSpawnOriginalObservations'
MODULE = 'connected_spawn_probe'
CASES = ('inbound_plain', 'inbound_trace', 'inbound_badopt_trace', 'outbound_plain', 'outbound_trace', 'outbound_monitor_options', 'alias_input_explicit', 'alias_input_demonitor', 'alias_input_reply_demonitor', 'alias_input_trace', 'alias_output_send2', 'alias_output_trace_send3')
MAX_RAW_BYTES = 16 * 1024 * 1024
STOP = False


def require(value, message):
    if not value:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def binding(path, root):
    return dict(Path=path.relative_to(root).as_posix(), Bytes=path.stat().st_size, Sha256=sha(path))


def save(path, value):
    with path.open('xb') as stream:
        stream.write((json.dumps(value, indent=2) + '\n').encode())


def safe_file(root, name):
    require(isinstance(name, str) and name and not PurePosixPath(name).is_absolute() and '..' not in PurePosixPath(name).parts and ':' not in name and '\\' not in name, 'Unsafe source path')
    path = (root / name).resolve(strict=True)
    require(path.is_relative_to(root) and path.is_file(), 'Missing or escaping source')
    return path


def git(root, *args):
    return subprocess.check_output(['git', '-C', str(root), *args], timeout=30)


def verify(args):
    root = args.repository.resolve(strict=True)
    tool = root / TOOL
    manifest_path = safe_file(tool, 'source-manifest.json')
    require(re.fullmatch('[A-Fa-f0-9]{64}', args.manifest_sha256 or ''), 'Explicit source manifest SHA required')
    require(sha(manifest_path) == args.manifest_sha256.upper(), 'Manifest hash mismatch')
    manifest = json.loads(manifest_path.read_bytes())
    require(manifest['PinnedOtpCommit'] == PIN and manifest['OriginalExecuted'] is False and manifest['SdkExecuted'] is False, 'Manifest scope mismatch')
    require(manifest['Cases'] == list(CASES), 'Reviewed twelve scenarios required')
    actual = {path.relative_to(tool).as_posix() for path in tool.rglob('*') if path.is_file() and path != manifest_path}
    require(actual == {row['Path'] for row in manifest['Files']}, 'Exact local source closure mismatch')
    for row in manifest['Files']:
        require(binding(safe_file(tool, row['Path']), tool) == row, 'Local source drift')
    for row in manifest['ExternalDependencies'] + [manifest['Workflow']]:
        require(binding(safe_file(root, row['Path']), root) == row, 'External dependency/workflow drift')
    require(re.fullmatch('[0-9a-f]{40}', args.expected_head or '') and git(root, 'rev-parse', 'HEAD').decode().strip() == args.expected_head, 'Exact source checkout required')
    return root, tool, manifest


def load_dependency(root, manifest, suffix):
    rows = [row for row in manifest['ExternalDependencies'] if row['Path'].endswith('/' + suffix + '.py')]
    require(len(rows) == 1, 'Exactly one reviewed dependency required')
    path = safe_file(root, rows[0]['Path'])
    require(binding(path, root) == rows[0], 'Dependency changed before import')
    spec = importlib.util.spec_from_file_location('connected_spawn_original_' + suffix, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def environment(directory):
    result = os.environ.copy()
    for key in ('ERL_FLAGS', 'ERL_AFLAGS', 'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS'):
        result.pop(key, None)
    result.update(ERLC_USE_SERVER='false', ERL_CRASH_DUMP=str(directory / 'erl_crash.dump'), ERLANG_NET_SPAWN_CAPTURE=str(directory))
    return result


def execute(owned, directory, label, arguments, timeout):
    prefix = directory / label
    save(directory / (label + '.attempt.json'), dict(Arguments=arguments, WorkingDirectory=str(directory), TimeoutSeconds=timeout, StartedUnix=time.time(), NewOwnedSession=True))
    process = owned.OwnedProcess(arguments, directory, environment(directory), prefix)
    try:
        process.wait(time.monotonic() + timeout, lambda: STOP)
    finally:
        process.close()
    receipt = json.loads(prefix.with_suffix('.process.json').read_bytes())
    require(receipt['ExitCode'] == 0 and receipt['ParentReaped'] and receipt['StreamsClosed'] and receipt['CleanupFailure'] is None, 'Owned original process did not complete cleanly')
    return binding(prefix.with_suffix('.process.json'), directory)


def decode_file(decoder, path):
    data = path.read_bytes()
    require(1 < len(data) <= MAX_RAW_BYTES, 'Raw capture size bound')
    value, consumed = decoder.decode_one(data)
    require(consumed == len(data), 'Trailing ETF bytes')
    return value


def sequence(value, kind='tuple'):
    require(type(value) is tuple and len(value) == 2 and value[0] == kind, 'Typed ETF sequence required')
    return value[1]


def text(value):
    values = sequence(value, 'list')
    require(all(item[0] == 'integer' for item in values), 'Typed charlist required')
    return ''.join(chr(item[1]) for item in values)


def provenance(args, root, tool, out, owned):
    require(sys.platform == 'linux', 'Reviewed Linux process-group collector only')
    otp = args.otp_source.resolve(strict=True)
    require(git(otp, 'rev-parse', 'HEAD').decode().strip() == PIN and not git(otp, 'status', '--porcelain', '--untracked-files=no').strip(), 'Pinned OTP tracked source mismatch')
    resolved_erl = args.erl.resolve(strict=True)
    installed = resolved_erl.parent.parent
    versions = [path for path in (installed / 'releases').glob('*/OTP_VERSION') if path.read_text().strip() == VERSION]
    require(len(versions) == 1, 'Exact installed OTP_VERSION missing from resolved installation')
    erlc = args.erl.parent / 'erlc'
    require(erlc.is_file() and erlc.resolve().is_relative_to(installed), 'Compiler outside exact original installation')
    beams = list(installed.glob('erts-*/bin/beam.smp'))
    require(len(beams) == 1 and beams[0].with_name('erlexec').is_file(), 'Installed VM/launcher missing')
    sources = []
    for name in ('erts/emulator/beam/dist.c', 'erts/emulator/beam/erl_proc_sig_queue.c', 'erts/preloaded/src/erlang.erl', 'erts/preloaded/src/erts_internal.erl', 'lib/stdlib/src/proc_lib.erl'):
        path = safe_file(otp, name)
        require(path.read_bytes() == git(otp, 'show', PIN + ':' + name), 'Pinned source content mismatch')
        sources.append(binding(path, otp))
    expression = 'io:format("ROOT ~s~nOTP ~s~nERTS ~s~nPRINTABLE ~p~n",[code:root_dir(),erlang:system_info(otp_release),erlang:system_info(version),io:printable_range()]),lists:foreach(fun(M)->io:format("MODULE ~s ~s~n",[atom_to_list(M),code:which(M)]) end,[proc_lib,seq_trace]),halt(0).'
    receipt = execute(owned, out, 'runtime-control', [str(args.erl), '+S', '2:2', '+pc', 'latin1', '-start_epmd', 'false', '-noshell', '-noinput', '-boot', 'start_clean', '-eval', expression], args.timeout_seconds)
    control = (out / 'runtime-control.stdout').read_text()
    require(re.search(r'(?m)^ROOT ' + re.escape(str(installed)) + '$', control) and re.search(r'(?m)^OTP 29$', control) and re.search(r'(?m)^PRINTABLE latin1$', control), 'Actual original runtime identity mismatch')
    modules = []
    for name in ('proc_lib', 'seq_trace'):
        paths = re.findall(r'(?m)^MODULE ' + name + r' (.+)$', control)
        require(len(paths) == 1, 'Module provenance missing')
        path = Path(paths[0]).resolve(strict=True)
        require(path.is_relative_to(installed), 'Original module loaded outside pinned installation')
        modules.append(binding(path, installed))
    result = dict(PinnedOtpCommit=PIN, Source=sources, ResolvedRoot=str(installed), OtpVersion=binding(versions[0], installed), Erl=binding(resolved_erl, installed), Erlc=binding(erlc.resolve(), installed), Beam=binding(beams[0], installed), Erlexec=binding(beams[0].with_name('erlexec'), installed), Modules=modules, ActualRuntimeReceipt=receipt, CollectorHead=args.expected_head)
    save(out / 'original-provenance.json', result)
    return erlc, installed


def collect(args):
    root, tool, manifest = verify(args)
    out = args.output.resolve()
    require(not out.exists(), 'Use a fresh observation directory')
    out.mkdir(parents=True)
    owned = load_dependency(root, manifest, 'owned_process')
    decoder = load_dependency(root, manifest, 'passive_etf')
    erlc, installed = provenance(args, root, tool, out, owned)
    rows = []
    for name in CASES:
        directory = out / name
        directory.mkdir()
        (directory / 'beam').mkdir()
        (directory / 'source').mkdir()
        source = directory / 'source' / (MODULE + '.erl')
        source.write_bytes((tool / (MODULE + '.erl')).read_bytes())
        row = dict(Name=name, Status='NOT_EXECUTED', Source=binding(source, directory), SdkExecuted=False, ExpectedValuesPresent=False, RawNormalization=False)
        try:
            row['CompileReceipt'] = execute(owned, directory, 'compile', [str(erlc), '-o', str(directory / 'beam'), str(source)], args.timeout_seconds)
            image = directory / 'beam' / (MODULE + '.beam')
            require(image.is_file(), 'Compiled fixture image missing')
            row['Beam'] = binding(image, directory)
            expression = 'D=os:getenv("ERLANG_NET_SPAWN_CAPTURE"),{module,connected_spawn_probe}=code:ensure_loaded(connected_spawn_probe),{connected_spawn_probe,Image,_}=code:get_object_code(connected_spawn_probe),ok=file:write_file(filename:join(D,"runtime.etf"),term_to_binary({runtime,erlang:system_info(otp_release),code:root_dir(),io:printable_range(),erlang:md5(Image),code:which(connected_spawn_probe)})),connected_spawn_probe:run(' + name + ',D),halt(0).'
            row['RunReceipt'] = execute(owned, directory, 'run', [str(args.erl), '+S', '2:2', '+pc', 'latin1', '-start_epmd', 'false', '-noshell', '-noinput', '-boot', 'start_clean', '-pa', str(directory / 'beam'), '-eval', expression], args.timeout_seconds)
            runtime = sequence(decode_file(decoder, directory / 'runtime.etf'))
            require(runtime[:4] == (('atom', 'runtime'), ('list', (('integer', 50), ('integer', 57))), ('list', tuple(('integer', ord(c)) for c in str(installed))), ('atom', 'latin1')), 'Per-case original runtime identity mismatch')
            require(runtime[4] == ('binary', hashlib.md5(image.read_bytes()).digest()), 'Per-case loaded probe image mismatch')
            require(len(runtime) == 6 and Path(text(runtime[5])).resolve() == image.resolve(), 'Per-case loaded fixture path mismatch')
            require(decode_file(decoder, directory / 'outer.etf') == ('tuple', (('atom', 'result'), ('atom', 'ok'))), 'Original scenario raised; raw observation retained')
            require(decode_file(decoder, directory / 'stage.etf') == ('tuple', (('atom', 'complete'), ('atom', name))), 'Original stage incomplete')
            cleanup = ['controller-cleanup.etf', 'tracer-cleanup.etf'] + (['child-cleanup.etf'] if (name in ('inbound_plain', 'inbound_trace') or name.startswith('alias_input_')) else [])
            for path in cleanup:
                require(decode_file(decoder, directory / path) == ('tuple', (('atom', 'observed'), ('atom', 'normal'))), 'Scenario actor cleanup not observed normal')
            raw = []
            for path in sorted(directory.iterdir()):
                if path.suffix == '.etf':
                    decode_file(decoder, path)
                    raw.append(binding(path, directory))
                elif path.suffix == '.packet':
                    data = path.read_bytes()
                    require(1 < len(data) <= MAX_RAW_BYTES and data[0] == 112, 'Pass-through packet framing/size')
                    _, consumed = decoder.decode_one(data[1:])
                    if consumed + 1 != len(data):
                        _, used = decoder.decode_one(data[consumed + 1:])
                        require(consumed + used + 1 == len(data), 'Packet trailing bytes')
                    raw.append(binding(path, directory))
            require(set(manifest['RequiredRawFiles'][name]) <= {item['Path'] for item in raw}, 'Required scenario raw observation missing')
            row.update(Status='OBSERVED_AND_CLEANED', RawFiles=raw, ActorCleanupFiles=cleanup)
        except Exception as error:
            row.update(Status='FAILED_RAW_RETAINED', Error=repr(error))
        save(directory / 'observation.json', row)
        rows.append(row)
        save(out / ('progress-' + str(len(rows)) + '.json'), dict(Completed=len(rows), Planned=len(CASES), Cases=rows))
        print(json.dumps(dict(Name=name, Status=row['Status'])), flush=True)
    verify(args)
    require(git(args.otp_source, 'rev-parse', 'HEAD').decode().strip() == PIN and not git(args.otp_source, 'status', '--porcelain', '--untracked-files=no').strip(), 'OTP source changed during capture')
    passed = all(row['Status'] == 'OBSERVED_AND_CLEANED' for row in rows)
    save(out / 'observations.json', dict(Status='ORIGINAL_CONNECTED_SPAWN_OBSERVATIONS_COMPLETE' if passed else 'ORIGINAL_CONNECTED_SPAWN_OBSERVATIONS_FAILED', CollectorHead=args.expected_head, PinnedOtpCommit=PIN, SourceManifestSha256=args.manifest_sha256.upper(), OriginalCases=len(rows), Cases=rows, SdkExecuted=False, ExpectedValuesPresent=False, RawNormalization=False, Scope='Original VM plus host-adopted simulated peer controller packets, not two physical VM TCP interoperability. Harness RPC can add trace events; raw event lists are observations, not pristine SDK baselines.'))
    return 0 if passed else 1


def stop(signum, frame):
    global STOP
    STOP = True


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=('verify', 'collect'))
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--manifest-sha256', required=True)
    parser.add_argument('--expected-head', required=True)
    parser.add_argument('--otp-source', type=Path)
    parser.add_argument('--erl', type=Path)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--timeout-seconds', type=int, default=45)
    args = parser.parse_args()
    require(1 <= args.timeout_seconds <= 120, 'Bounded subprocess timeout required')
    signal.signal(signal.SIGTERM, stop)
    signal.signal(signal.SIGINT, stop)
    if args.mode == 'verify':
        root, tool, manifest = verify(args)
        print(json.dumps(dict(Status='SOURCE_VERIFIED', Head=args.expected_head, Cases=len(CASES), SdkExecuted=False, OriginalExecuted=False)))
    else:
        require(args.otp_source and args.erl and args.output, 'Original source, VM and fresh output required')
        raise SystemExit(collect(args))
