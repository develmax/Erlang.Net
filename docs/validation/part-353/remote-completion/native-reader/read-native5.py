"""Passive native five reader. Never executes downloaded scripts, BEAM, or VMs."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path, PurePosixPath
import re
import struct
import zipfile

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
MANIFEST = '4C9A79CD6E8BC18F68316D42B2F30DB171E67A242CA81CA84BB8A022E8BA5BFB'
DECODER = 'A069AD650B3EA1BE6B90DE60117B84ED7A1A6B67128CC41E7ECAEFD2889C1713'
MODULES = ('scheduler_first_instruction', 'scheduler_migration_previous_binding',
           'scheduler_invalid_and_duplicate', 'scheduler_offline_bound_resume',
           'scheduler_offline_pending_kill')


def need(value, message):
    if not value:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def read(path):
    return json.loads(Path(path).read_bytes())


def local(root, name):
    need(isinstance(name, str) and name and '\\' not in name and ':' not in name
         and not name.startswith('/') and all(p not in ('', '.', '..') for p in name.split('/')), 'unsafe path')
    result = (root / name).resolve(strict=True)
    need(result.is_relative_to(root.resolve()) and result.is_file(), 'path outside closure')
    return result


def normal(path):
    receipt = read(path)
    need(receipt['ExitCode'] == 0 and receipt['ParentReaped'] is True
         and receipt['StreamsClosed'] is True and receipt['CleanupFailure'] is None
         and receipt['TerminationRequested'] is False and receipt['RemainingGroupKillRequested'] is False,
         'owned process did not exit normally: ' + str(path))
    need(receipt['OwnedPid'] > 0 and receipt['OwnedProcessGroup'] == receipt['OwnedPid'], 'owned generation')
    return receipt


def beam_md5(data):
    # Pinned beam_lib.erl md5/1: chunks in md5_chunks order, FunT OldUniq zeroed.
    need(data[:4] == b'FOR1' and data[8:12] == b'BEAM'
         and int.from_bytes(data[4:8], 'big') + 8 == len(data), 'BEAM form size')
    chunks = {}
    offset = 12
    while offset < len(data):
        need(offset + 8 <= len(data), 'truncated BEAM chunk')
        tag, size = data[offset:offset + 4], int.from_bytes(data[offset + 4:offset + 8], 'big')
        need(tag not in chunks and offset + 8 + size <= len(data), 'duplicate/truncated BEAM chunk')
        chunks[tag] = data[offset + 8:offset + 8 + size]
        offset += 8 + ((size + 3) // 4) * 4
    need(offset == len(data) and {b'Code', b'ExpT', b'ImpT', b'StrT'} <= chunks.keys(), 'mandatory BEAM chunks')
    if b'FunT' in chunks:
        value = chunks[b'FunT']
        need(len(value) >= 4, 'FunT count')
        body = bytearray(value[4:])
        # Matches filter_funtab_1: full 24-byte records; any final tail retained.
        for start in range(0, len(body) - 23, 24):
            body[start + 20:start + 24] = bytes(4)
        chunks[b'FunT'] = value[:4] + body
    order = (b'Atom', b'AtU8', b'Code', b'StrT', b'ImpT', b'ExpT', b'FunT', b'LitT', b'Meta', b'Recs', b'DbgB')
    return hashlib.md5(b''.join(chunks[x] for x in order if x in chunks)).digest()


def transport(directory, head, run_id, artifact_id):
    mapping, run = read(directory / 'run-map.json'), read(directory / 'run.json')
    jobs, artifacts = read(directory / 'jobs.json')['jobs'], read(directory / 'artifacts.json')['artifacts']
    need(mapping['Head'] == run['head_sha'] == head and mapping['Run'] == run['id'] == run_id
         and mapping['Attempt'] == run['run_attempt'] == 1, 'exact mapped subject')
    need(run['path'] == '.github/workflows/scheduler-affinity-original.yml'
         and run['event'] == 'workflow_dispatch' and run['head_branch'] == 'main'
         and run['status'] == 'completed' and run['conclusion'] == 'success', 'terminal native workflow')
    need(len(jobs) == 1 and jobs[0]['id'] == mapping['Job'] and jobs[0]['head_sha'] == head
         and jobs[0]['run_id'] == run_id and jobs[0]['run_attempt'] == 1
         and jobs[0]['status'] == 'completed' and jobs[0]['conclusion'] == 'success', 'exact successful job')
    need(len(artifacts) == 1, 'one native artifact')
    artifact = artifacts[0]
    need(artifact['id'] == artifact_id and artifact['name'] == 'scheduler-affinity-original-five'
         and artifact['expired'] is False and artifact['workflow_run']['id'] == run_id
         and artifact['workflow_run']['head_sha'] == head, 'exact artifact subject')
    rows = read(directory / 'download-manifest.json')
    need(len(rows) == len({r['Path'] for r in rows}), 'download duplicates')
    actual = {p.relative_to(directory).as_posix() for p in directory.rglob('*') if p.is_file()
              and p not in (directory / 'run-map.json', directory / 'download-manifest.json')}
    need(actual == {r['Path'] for r in rows}, 'exact downloaded closure')
    for row in rows:
        file = local(directory, row['Path'])
        need(file.stat().st_size == row['Bytes'] and sha(file) == row['Sha256'], 'download drift')
    zipped, extracted = directory / f'artifact-{artifact_id}.zip', directory / f'artifact-{artifact_id}'
    need(artifact['digest'] == 'sha256:' + sha(zipped).lower()
         and artifact['size_in_bytes'] == zipped.stat().st_size, 'API artifact digest')
    with zipfile.ZipFile(zipped) as archive:
        names = archive.namelist()
        need(len(names) == len(set(names)) and sum(i.file_size for i in archive.infolist()) <= 1024**3, 'bounded unique ZIP')
        need({p.relative_to(extracted).as_posix() for p in extracted.rglob('*') if p.is_file()}
             == {n for n in names if not n.endswith('/')}, 'exact extracted closure')
        for item in archive.infolist():
            need((item.external_attr >> 16) & 0o170000 != 0o120000, 'ZIP symlink')
            if not item.is_dir():
                need(local(extracted, item.filename).read_bytes() == archive.read(item), 'ZIP extracted bytes')
    return extracted


def main(args):
    need(re.fullmatch('[0-9a-f]{40}', args.expected_head), 'explicit head')
    repository, directory = args.repository.resolve(), args.capture.resolve()
    manifest_path = args.source_manifest.resolve()
    need(sha(manifest_path) == MANIFEST, 'manifest drift')
    manifest = read(manifest_path)
    need(manifest['OriginalCommit'] == PIN and manifest['Flags'] == ['+S', '2:2']
         and tuple(c['Module'] for c in manifest['Cases']) == MODULES, 'source contract')
    tool = manifest_path.parent
    for row in manifest['Files']:
        need(sha(local(tool, row['Path'])) == row['Sha256'], 'reviewed tool source drift')
    decoder = repository / 'tools/Erlang.GuestConnectedSendObservations/passive_etf.py'
    need(sha(decoder) == DECODER, 'trusted decoder drift')
    spec = importlib.util.spec_from_file_location('trusted_etf', decoder)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    extracted = transport(directory, args.expected_head, args.run, args.artifact)
    captures = list(extracted.rglob('completion.json'))
    need(len(captures) == 1, 'one complete capture')
    output = captures[0].parent
    builds = list(extracted.rglob('build-receipt.json'))
    need(len(builds) == 1, 'one build receipt')
    build_path = builds[0]
    build = read(build_path)
    need(build['Status'] == 'PASS_OWNED_PINNED_ORIGINAL_BUILD' and build['Commit'] == PIN
         and build['SubjectHead'] == args.expected_head and build['SourceManifestSha256'] == MANIFEST,
         'pinned original build')
    runtime = PurePosixPath(build['RuntimeRoot'])
    need(runtime.is_absolute() and runtime == PurePosixPath(build['Prefix']) / 'lib/erlang', 'sealed install root')
    before, after = build_path.parent / 'source-before.json', build_path.parent / 'source-after.json'
    need(sha(before) == build['SourceBeforeSha256'] and sha(after) == build['SourceAfterSha256']
         and read(before) == read(after), 'tracked source unchanged')
    identity = read(before)
    need(identity['Commit'] == PIN and identity['TrackedFiles'], 'pinned source inventory')
    need(len(identity['TrackedFiles']) == len({r['Path'] for r in identity['TrackedFiles']}), 'source duplicates')
    for row in identity['TrackedFiles']:
        need(row['Mode'] in ('100644', '100755', '120000') and row['Bytes'] >= 0
             and re.fullmatch('[A-F0-9]{64}', row['Sha256']), 'mode-aware source receipt')
    need([s['Name'] for s in build['Stages']] == ['configure', 'make', 'install'], 'build stages')
    for stage in build['Stages']:
        name = stage['Name']
        receipt = build_path.parent / (name + '.process.json')
        need(PurePosixPath(stage['ReceiptPath']).name == receipt.name and sha(receipt) == stage['ReceiptSha256']
             and sha(build_path.parent / (name + '.stdout')) == stage['StdoutSha256']
             and sha(build_path.parent / (name + '.stderr')) == stage['StderrSha256'], 'stage raw binding')
        process = normal(receipt)
        commands = {'configure': [str(PurePosixPath(build['SourceRoot']) / 'configure'),
                    '--prefix=' + build['Prefix'], '--without-javac', '--without-wx', '--without-odbc',
                    '--without-debugger', '--without-observer', '--without-et'],
                    'make': ['make', '-j2'], 'install': ['make', 'install']}
        need(process['Arguments'] == commands[name], 'actual build command')
        need(read(build_path.parent / (name + '.status.json')) == {'Failure': None, 'Started': True}, 'stage status')
    installed = build['InstalledFiles']
    need(len(installed) >= 11 and len(installed) == len({r['Path'] for r in installed}), 'installed identities')
    for row in installed:
        need(not PurePosixPath(row['Path']).is_absolute() and '..' not in PurePosixPath(row['Path']).parts
             and re.fullmatch('[A-F0-9]{64}', row['Sha256']), 'installed hash record')
    by_name = {PurePosixPath(r['Path']).name: r for r in installed}
    need({'erl', 'erlc', 'beam.smp', 'erlexec', 'erlang.beam', 'init.beam', 'erl_eval.beam',
          'compile.beam', 'beam_lib.beam', 'lists.beam', 'OTP_VERSION'} <= by_name.keys(), 'required installed identities')
    inputs, completion, observations = read(output / 'inputs.json'), read(captures[0]), read(output / 'observations.json')
    need(inputs['ManifestSha256'] == MANIFEST and inputs['OriginalCommit'] == PIN
         and inputs['SubjectHead'] == args.expected_head and int(inputs['WorkflowRunId']) == args.run
         and inputs['WorkflowRunAttempt'] == '1' and inputs['BuildReceiptSha256'] == sha(build_path)
         and inputs['ExplicitFlags'] == ['+S', '2:2'] and inputs['OrdinaryCorpusModified'] is False,
         'capture identity')
    need(PurePosixPath(inputs['ErlPath']) == runtime / 'bin/erl'
         and inputs['ErlSha256'] == by_name['erl']['Sha256']
         and inputs['BeamSha256'] == by_name['beam.smp']['Sha256'], 'capture installed binding')
    need(completion['Status'] == 'PASS_ORIGINAL_EXPLICIT_TWO_LANE_PROFILE'
         and completion['OrdinaryCorpusModified'] is False and completion['ManagedInteropExecuted'] is False
         and completion['Cases'] == observations and tuple(r['Module'] for r in observations) == MODULES, 'five complete cases')
    results = []
    for case, row in zip(manifest['Cases'], observations):
        name = case['Module']
        cd = output / name
        need(row['Passed'] is True and row['Configured'] == row['OnlineAtEntry'] == 2
             and sha(cd / (name + '.erl')) == row['SourceSha256'] == case['SourceSha256'], 'exact fixture')
        need(sha(cd / 'outcome.etf') == row['OutcomeSha256']
             and sha(cd / 'vm.process.json') == row['ProcessReceiptSha256'], 'case receipt binding')
        receipt = normal(cd / 'vm.process.json')
        attempt = read(cd / 'attempt.json')
        need(attempt['TimeoutSeconds'] == 45 and attempt['SourceSha256'] == case['SourceSha256']
             and receipt['Arguments'] == attempt['Arguments'] and attempt['Arguments'][:4]
             == [inputs['ErlPath'], '+S', '2:2', '-noshell'], 'actual explicit profile arguments')
        need(read(cd / 'capture-status.json') == {'Failure': None, 'Started': True}
             and not (cd / 'raised.etf').exists(), 'normal return status')
        for filename in ('profile.etf', 'returned.etf', 'compiled-protocol.etf', 'outcome.etf'):
            need((cd / filename).read_bytes() == (tool / 'golden' / filename).read_bytes(), 'exact typed golden ' + filename)
        root_term = module.decode_exact((cd / 'runtime-root.etf').read_bytes())
        need(root_term == ('tuple', (('atom', 'runtime_root'), ('binary', str(runtime).encode()),
                                    ('binary', str(runtime).encode()))), 'actual VM root')
        image = module.decode_exact((cd / 'image.etf').read_bytes())
        need(image[0] == 'tuple' and len(image[1]) == 3, 'image schema')
        atom, filename, checksum = image[1]
        need(filename[0] == 'list' and all(c[0] == 'integer' for c in filename[1]), 'image charlist')
        loaded = PurePosixPath(''.join(chr(c[1]) for c in filename[1]))
        need(atom == ('atom', name) and loaded.is_absolute() and loaded.name == name + '.beam'
             and loaded.parent.name == name and checksum == ('binary', beam_md5((cd / (name + '.beam')).read_bytes())),
             'loaded module MD5 equals retained significant BEAM code')
        results.append({'Module': name, 'SourceSha256': case['SourceSha256'], 'BeamSha256': sha(cd / (name + '.beam')),
                        'OutcomeSha256': row['OutcomeSha256'], 'ProcessReceiptSha256': row['ProcessReceiptSha256']})
    report = {'Status': 'PASS_PASSIVE_ORIGINAL_NATIVE_FIVE', 'Head': args.expected_head, 'Run': args.run,
              'Artifact': args.artifact, 'ManifestSha256': MANIFEST, 'Cases': results,
              'BuildReceiptSha256': sha(build_path), 'RawInstalledBinariesRehashedByReader': False,
              'InstalledIdentityScope': 'Remote build/capture receipts bound; installed binaries not included in artifact.',
              'ManagedInteropClaimed': False, 'OrdinaryCorpusModified': False}
    with args.report.open('xb') as stream:
        stream.write((json.dumps(report, indent=2) + '\n').encode())


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    for name in ('repository', 'capture', 'source-manifest', 'report'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--expected-head', required=True)
    parser.add_argument('--run', type=int, required=True)
    parser.add_argument('--artifact', type=int, required=True)
    main(parser.parse_args())
