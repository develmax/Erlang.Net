"""Build and seal original installation; execution belongs to the separate reviewed workflow."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import sys
import threading
import time
from source_inputs import PIN, digest, require_normal_receipt, sanitized_environment, source_identity, verify, write


def main():
    assert sys.platform.startswith('linux')
    parser = argparse.ArgumentParser()
    for name in ['repository', 'manifest-sha256', 'expected-head', 'otp-source', 'prefix', 'output']:
        parser.add_argument('--' + name, required=True)
    args = parser.parse_args()
    repo, tool, manifest = verify(args.repository, args.manifest_sha256, args.expected_head)
    source = Path(args.otp_source).resolve()
    prefix = Path(args.prefix).resolve()
    output = Path(args.output).resolve()
    assert not prefix.exists() and not output.exists(), 'fresh build prefix and evidence required'
    before = source_identity(source)
    output.mkdir(parents=True)
    write(output / 'source-before.json', before)
    dependency = repo / manifest['Dependencies'][0]['Path']
    spec = importlib.util.spec_from_file_location('scheduler_build_owned', dependency)
    owned = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(owned)
    environment = sanitized_environment(os.environ)
    environment['ERLC_USE_SERVER'] = 'false'
    stages = [
        ('configure', [str(source / 'configure'), '--prefix=' + str(prefix), '--without-javac',
                       '--without-wx', '--without-odbc', '--without-debugger', '--without-observer', '--without-et'], 180),
        ('make', ['make', '-j2'], 2100),
        ('install', ['make', 'install'], 180),
    ]
    receipts = []
    for name, arguments, timeout in stages:
        process = None
        failure = None
        try:
            process = owned.OwnedProcess(arguments, source, environment, output / name)
            process.wait(time.monotonic() + timeout, threading.Event().is_set)
        except BaseException as error:
            failure = repr(error)
        finally:
            if process is not None:
                try:
                    process.close()
                except BaseException as error:
                    failure = (failure or '') + '; cleanup: ' + repr(error)
            write(output / (name + '.status.json'), {'Failure': failure, 'Started': process is not None})
        if failure:
            raise RuntimeError(name + ': ' + failure)
        receipt_path = output / (name + '.process.json')
        receipt = json.loads(receipt_path.read_bytes())
        require_normal_receipt(receipt)
        receipts.append({'Name': name, 'ReceiptPath': str(receipt_path), 'ReceiptSha256': digest(receipt_path),
                         'StdoutSha256': digest(output / (name + '.stdout')), 'StderrSha256': digest(output / (name + '.stderr'))})
    after = source_identity(source)
    write(output / 'source-after.json', after)
    assert after == before
    runtime = prefix / 'lib/erlang'
    assert runtime.is_dir()
    versions = list((runtime / 'releases').glob('*/OTP_VERSION'))
    assert any(path.read_text().strip() == '29.1.1' for path in versions)
    binaries = [runtime / 'bin/erl', runtime / 'bin/erlc']
    for name in ['beam.smp', 'erlexec']:
        matches = list(runtime.glob('erts-*/bin/' + name))
        assert len(matches) == 1
        binaries += matches
    for name in ['erlang', 'init', 'erl_eval', 'compile', 'beam_lib', 'lists']:
        matches = list(runtime.rglob(name + '.beam'))
        assert len(matches) == 1, name
        binaries += matches
    binaries += versions
    write(output / 'build-receipt.json', {
        'Status': 'PASS_OWNED_PINNED_ORIGINAL_BUILD', 'Commit': PIN, 'SubjectHead': args.expected_head,
        'SourceManifestSha256': args.manifest_sha256.upper(), 'SourceRoot': str(source), 'Prefix': str(prefix),
        'RuntimeRoot': str(runtime), 'SourceBeforeSha256': digest(output / 'source-before.json'),
        'SourceAfterSha256': digest(output / 'source-after.json'), 'Stages': receipts,
        'InstalledFiles': [{'Path': str(path.relative_to(runtime)), 'Sha256': digest(path)} for path in binaries],
    })


if __name__ == '__main__':
    main()
