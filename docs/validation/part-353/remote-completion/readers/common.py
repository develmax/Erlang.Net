"""Passive parameterized transport/source helpers, adapted from reviewed351/352 readers."""
import argparse
import base64
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import subprocess
import zipfile

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
PLAN_SHA = '22B512D445EF46BE807E4DBD8F989F5C9C90FD9416C589942B04C5C656949769'
EXPECTED_INTEGRATIONS = dict(Readiness='Passed', PackagedReadiness='Passed', Build='Passed', Tests='Passed',
    OracleUnavailableReport='Expected failure recorded', HybridOutput='Hello World', Incremental='Passed',
    DisabledPreprocessing='Expected failure', CleanRebuild='Passed', PackageConsumer='Passed', LocalTool='Passed')


def need(value, message):
    if not value:
        raise ValueError(message)


def digest(data):
    return hashlib.sha256(data).hexdigest().upper()


def sha(path):
    return digest(Path(path).read_bytes())


def read(path):
    return json.loads(Path(path).read_bytes())


def fresh(path, value):
    with path.open('xb') as stream:
        stream.write((json.dumps(value, indent=2) + '\n').encode())


def test_names(report, expected):
    names = [row['Name'] for row in report['Tests']]
    need(report['Passed'] == len(names) == len(set(names)) == expected and report['Failed'] == 0
         and all(row['Status'] == 'Passed' for row in report['Tests']), 'actual all-passed unique test count')
    return names


def relative(name):
    need(isinstance(name, str) and name and not name.startswith('/') and ':' not in name and '\\' not in name
         and all(p not in ('', '.', '..') for p in name.split('/')), 'safe relative path')
    return name


def blobs(repository, head, paths):
    need(re.fullmatch('[0-9a-f]{40}', head), 'exact head')
    paths = list(dict.fromkeys(paths))
    need(all('\n' not in p and '\r' not in p and '\0' not in p for p in paths), 'batch path')
    result = subprocess.run(['git', '-C', str(repository), 'cat-file', '--batch'],
        input=''.join(head + ':' + relative(p) + '\n' for p in paths).encode(), capture_output=True, timeout=120)
    need(result.returncode == 0, 'read-only local Git blobs')
    raw, offset, values = result.stdout, 0, {}
    for path in paths:
        end = raw.index(b'\n', offset)
        header = raw[offset:end].split()
        need(len(header) == 3 and header[1] == b'blob', 'existing published blob ' + path)
        size, start = int(header[2]), end + 1
        data = raw[start:start + size]
        need(len(data) == size and raw[start + size:start + size + 1] == b'\n', 'batch framing')
        values[path], offset = data, start + size + 1
    need(offset == len(raw), 'batch trailing bytes')
    return values


def transport(args, kind):
    directory = args.capture.resolve()
    mapping, run = read(directory / 'run-map.json'), read(directory / 'run.json')
    jobs, artifacts = read(directory / 'jobs.json')['jobs'], read(directory / 'artifacts.json')['artifacts']
    workflow, event, artifact_name = {'ci': ('validate.yml', 'push', 'validation'),
                                    'ordinary': ('differential.yml', 'workflow_dispatch', 'differential')}[kind]
    need(mapping['Head'] == run['head_sha'] == args.expected_head and mapping['Run'] == run['id'] == args.run
         and mapping['Attempt'] == run['run_attempt'] == 1, 'mapped head/run/attempt')
    need(run['path'] == '.github/workflows/' + workflow and run['event'] == event
         and run['head_branch'] == 'main' and run['status'] == 'completed' and run['conclusion'] == 'success', 'terminal exact workflow')
    need(len(jobs) == 1 and jobs[0]['id'] == mapping['Job'] and jobs[0]['run_id'] == args.run
         and jobs[0]['head_sha'] == args.expected_head and jobs[0]['run_attempt'] == 1
         and jobs[0]['status'] == 'completed' and jobs[0]['conclusion'] == 'success'
         and all(s['conclusion'] in ('success', 'skipped') for s in jobs[0]['steps']), 'successful exact job')
    need(len(artifacts) == 1, 'one artifact')
    artifact = artifacts[0]
    need(artifact['id'] == args.artifact and artifact['name'] == artifact_name and artifact['expired'] is False
         and artifact['workflow_run']['id'] == args.run and artifact['workflow_run']['head_sha'] == args.expected_head, 'artifact subject')
    rows = read(directory / 'download-manifest.json')
    need(len(rows) == len({r['Path'] for r in rows}), 'download duplicates')
    need({p.relative_to(directory).as_posix() for p in directory.rglob('*') if p.is_file()
          and p not in (directory / 'run-map.json', directory / 'download-manifest.json')}
         == {r['Path'] for r in rows}, 'exact download closure')
    for row in rows:
        file = (directory / relative(row['Path'])).resolve(strict=True)
        need(file.is_relative_to(directory) and file.stat().st_size == row['Bytes'] and sha(file) == row['Sha256'], 'download bytes')
    zipped, extracted = directory / f'artifact-{args.artifact}.zip', directory / f'artifact-{args.artifact}'
    need(zipped.stat().st_size == artifact['size_in_bytes'] and artifact['digest'] == 'sha256:' + sha(zipped).lower(), 'API ZIP digest')
    with zipfile.ZipFile(zipped) as archive:
        infos = archive.infolist()
        need(len(infos) == len({i.filename for i in infos}) and sum(i.file_size for i in infos) <= 1024**3, 'bounded unique ZIP')
        need({p.relative_to(extracted).as_posix() for p in extracted.rglob('*') if p.is_file()}
             == {i.filename for i in infos if not i.is_dir()}, 'exact extracted closure')
        for item in infos:
            need(((item.external_attr >> 16) & 0o170000) != 0o120000, 'no ZIP symlink')
            relative(item.filename.rstrip('/'))
            if not item.is_dir():
                need((extracted / item.filename).read_bytes() == archive.read(item), 'exact extracted ZIP bytes')
    log = directory / ('job-' + str(jobs[0]['id']) + '.log')
    need(log.is_file(), 'raw job log must be downloaded and included in download manifest')
    return extracted, run, jobs[0], log.read_text(encoding='utf-8-sig')


def publication(args):
    need(sha(args.promotion_plan) == PLAN_SHA, 'reviewed promotion plan')
    plan = read(args.promotion_plan)
    need(plan['IndependentGuardSha256'] == 'AC6719076493E263AD89A55311A5B2BE79E7DB0F78946BE89EB0AE92CF260703', 'accepted full2 authority')
    values = blobs(args.repository, args.expected_head, [r['TargetPath'] for r in plan['Files']])
    whitespace = whitespace_correction(args, values)
    need(sha(args.stage_guard) == args.stage_guard_sha256.upper()
         == '54ED0A88E11466C8BC7BB347ECE214E01734DF7ECFB2D901F15A7D0F91CE3B1A', 'actual final634 stage guard binding')
    stage = read(args.stage_guard)
    need(stage['Status'] == 'PASS_EXACT353_FINAL634_STAGE_WITH_COMPILED_LITERAL_WHITESPACE_IDENTITY_NO_COMMIT'
         and stage['ParentHead'] == '0e739220c2d5d7afc66b6fb94c1f2b15d156bd90'
         and stage['OriginalApplyExitedZero'] is False and stage['OriginalIgnoreResumeExitedZero'] is False
         and stage['StagedPaths'] == 634 and stage['FiveEmittedFixtureValuesUnchanged'] is True
         and stage['Full2RawSourceAndRawCliArchivesPreserved'] is True
         and stage['BothNativePlansAndSealsUnchanged'] is True
         and stage['RuntimeGuard89Registry587Compatibility16_24_547Preserved'] is True
         and stage['CurrentIndexMatrixAnd0055PrefixPreserved'] is True
         and stage['CorrectionPlanSha256'] == '9DB0DF5E01913C7B5E7CA915A22D277C43CE7F85C1AA9CBFEE054D8DC7337BEA'
         and stage['JournalAppendPlanSha256'] == '2395D8E3C9702C60B12C5E3F706C03ECE4117D4A53E5F77FF34A958627BB4C2A'
         and stage['CompiledMetadataSha256'] == '91D9551B2F587B5A444EC4E280AD0EE6D5CD8C4880ABD9DA3ECA33FAA09AF53C', 'actual final634 history and whitespace admission')
    history_path = 'docs/validation/part-353/root-delivery/staging-ignore-correction.json'
    history_data = blobs(args.repository, args.expected_head, [history_path])[history_path]
    need(digest(history_data) == '7EF0637FAC08B678DC8AD9CFCEF720A98C0A0EE3C99F171290EE8702B2D84C64', 'immutable staging failure history')
    history = json.loads(history_data)
    need(history['Status'] == 'STAGING_IGNORE_CORRECTION_HISTORY_ORIGINAL_HELPER_DID_NOT_EXIT_ZERO'
         and history['OriginalPlannedPaths'] == 631 and history['OriginalPartialStagedPaths'] == 624
         and len(history['IgnoredPlannedPaths']) == 7 and history['SourceTestsPackagesChanged'] is False
         and history['ResumeSucceededClaimedByThisHistory'] is False, 'original staging failure not relabelled successful')
    partial = read(args.repository / 'artifacts/part353-stage-ignore-resume-proposal/partial-state.json')
    need(sha(args.repository / 'artifacts/part353-stage-ignore-resume-proposal/partial-state.json')
         == history['PartialStateSha256'] == '0E0765B9D359C73471CAB134B16EF823F9FB31667CA9E5A8710A27E58232D380'
         and set(stage['Paths']) == set(partial['ExpectedOriginalPaths']) | {history_path,
             'docs/validation/part-353/root-delivery/whitespace-correction.json',
             'docs/validation/part-353/root-delivery/compiled-fixture-constants.json'}, 'original631 plus history and two whitespace proofs exactly634')
    stage_rows = {row['Path']: row for row in stage['Records']}
    need(len(stage['Records']) == len(stage_rows) == 634 and set(stage_rows) == set(stage['Paths']), 'actual634 unique final blobs')
    # Explicit Root-authored postpublication delta receipt, not broad normalization.
    deltas = read(args.publication_deltas)['Files'] if args.publication_deltas else []
    need(len(deltas) == len({r['Path'] for r in deltas}), 'publication delta duplicates')
    by_path = {r['Path']: r for r in deltas}
    used = set()
    for row in plan['Files']:
        path, data = row['TargetPath'], values[row['TargetPath']]
        if digest(data) != row['Sha256'] or len(data) != row['Bytes']:
            if path == 'tools/Erlang.Differential/SchedulerAffinityCaseSources.cs':
                need(row['Sha256'] == whitespace['BeforeFixtureSourceSha256']
                     and digest(data) == whitespace['AfterFixtureSourceSha256'], 'only exact postfull fixture representation correction')
                continue
            if path == 'docs/changelog/0055.md':
                journal = args.repository / 'artifacts/part353-whitespace-correction-proposal2/0055-with-whitespace-note.md'
                need(sha(journal) == '50BEE2F51D999A15D29C52AC8EA789B6173962939D7E5F0548209B8E96305272', 'exact declared journal amendment')
                staged_data = journal.read_bytes()
                if stage_rows[path]['ComparisonMode'] == 'TextLF': staged_data = staged_data.replace(b'\r\n', b'\n')
                need(data == staged_data and data.startswith(blobs(args.repository, stage['ParentHead'], [path])[path]), 'journal actual append only')
                continue
            record = stage_rows.get(path)
            if record and record['ComparisonMode'] == 'TextLF':
                need(not path.endswith('.cs') and not path.startswith('docs/validation/')
                     and record['WorkingSha256'] == row['Sha256'] and record['StagedSha256'] == digest(data), 'declared source-bound TextLF policy')
                original = (args.repository / row['SourcePath']).read_bytes()
                need(digest(original) == row['Sha256'] and original.replace(b'\r\n', b'\n') == data, 'actual LF conversion only')
                continue
            need(path in by_path and not path.endswith('.cs'), 'unapproved published byte difference ' + path)
            delta = by_path[path]
            need(delta['FrozenSha256'] == row['Sha256'] and delta['PublishedSha256'] == digest(data), 'exact delta hashes')
            if delta['Reason'] == 'LFOnly':
                original = (args.repository / row['SourcePath']).read_bytes()
                need(digest(original) == row['Sha256'] and original.replace(b'\r\n', b'\n') == data, 'LF-only source bytes')
            else:
                need(delta['Reason'] == 'PostFullDocumentation' and path.startswith('docs/'), 'narrow metadata difference')
            used.add(path)
    need(used == set(by_path), 'no unused delta permission')
    records = stage.get('Files', stage.get('Records'))
    need(isinstance(records, list) and len(records) == len({r['Path'] for r in records}), 'unique stage records')
    staged = blobs(args.repository, args.expected_head, [r['Path'] for r in records])
    for row in records:
        data = staged[row['Path']]
        need(len(data) == row.get('StagedBytes', row.get('Bytes'))
             and digest(data) == row.get('StagedSha256', row.get('Sha256')), 'exact published stage blob')
    unchanged = set(values) - set(staged)
    if unchanged:
        parent_values = blobs(args.repository, stage['ParentHead'], unchanged)
        need(all(values[path] == parent_values[path] for path in unchanged), 'unstaged promotion paths raw-identical to parent only')
    return plan, values, len(staged)


def constants(text):
    values = {name: json.loads(value) for name, value in re.findall(
        r'public const string (\w+)\s*=\s*("(?:[^"\\]|\\.)*")\s*;', text)}
    for match in re.finditer(r'public const string (\w+)\s*=\s*"""\r?\n(.*?)\r?\n([ \t]*)"""\s*;', text, re.S):
        name, body, indent = match.groups()
        need(name not in values, 'constant duplicate')
        need(all(not line or line.startswith(indent) for line in body.split('\n')), 'raw literal indentation')
        values[name] = '\n'.join(line[len(indent):] if line else '' for line in body.split('\n'))
    return values


def exact_five_fixture_blank_lines(before, after):
    """One declared source representation correction, not general C# normalization."""
    old_lines, new_lines = before.splitlines(keepends=True), after.splitlines(keepends=True)
    need(len(old_lines) == len(new_lines), 'fixture line count unchanged')
    differences = [(old, new) for old, new in zip(old_lines, new_lines) if old != new]
    need(differences == [(b'        \n', b'\n')] * 5, 'exact five indent-only blank lines')
    old_values, new_values = constants(before.decode()), constants(after.decode())
    need(len(old_values) == 5 and old_values == new_values, 'all five rawliteral values identical')


def whitespace_correction(args, published):
    prefix = 'docs/validation/part-353/root-delivery/'
    paths = [prefix + 'whitespace-correction.json', prefix + 'compiled-fixture-constants.json',
             'tools/Erlang.Differential/SchedulerAffinityCaseSources.cs', 'docs/API_COMPATIBILITY.md']
    values = blobs(args.repository, args.expected_head, paths)
    need(digest(values[paths[0]]) == '483B3CBD9B94C0F93397CB3DE9E729A2AEAC820D6A5FDCEAF3669519CA65A956'
         and digest(values[paths[1]]) == '91D9551B2F587B5A444EC4E280AD0EE6D5CD8C4880ABD9DA3ECA33FAA09AF53C', 'closed whitespace/compiled metadata proof')
    proof, metadata = json.loads(values[paths[0]]), json.loads(values[paths[1]])
    source_path = 'tools/Erlang.Differential/SchedulerAffinityCaseSources.cs'
    before = published['docs/validation/part-353/tested-source/' + source_path]
    after = values[source_path]
    need(digest(before) == proof['BeforeFixtureSourceSha256'] == 'E114788DBC9B11777C0DE9E98396DF8FDC0AA94F40D093AE7A64C7A6805B08AE'
         and digest(after) == proof['AfterFixtureSourceSha256'] == 'B626F4B71D1605ABBD171A655B1B99D385DF6D12DBAF350EC3A23A8996596F49', 'before/after exact source hashes')
    exact_five_fixture_blank_lines(before, after)
    fields = metadata['Fields']
    need(len(fields) == len({r['Name'] for r in fields}) == 5 and proof['RawCSharpTokenIdentityClaimed'] is False, 'five actual compiled fields; no token identity claim')
    emitted = constants(after.decode())
    for field in fields:
        data = base64.b64decode(field['Base64'], validate=True)
        need(data == emitted[field['Name']].encode() and len(data) == field['Bytes']
             and digest(data) == field['Sha256'], 'actual compiled GetRawConstantValue equals published literal')
    captured = json.loads(published['docs/validation/part-353/full/raw/captured-outputs.json'])
    actual_dlls = [r for r in captured['TestDlls'] if r['Path'].endswith('/Erlang.Differential.dll')]
    need(len(actual_dlls) == 1 and actual_dlls[0]['Sha256'] == metadata['AssemblySha256']
         == proof['CompiledAssemblySha256'], 'actual full2 assembly metadata receipt identity')
    original_md = published['docs/validation/part-353/full/raw/artifacts/readiness/API_COMPATIBILITY.md']
    need(digest(original_md) == proof['RawCliGeneratedMdSha256']
         and original_md.endswith(b'\n\n') and values['docs/API_COMPATIBILITY.md'] == original_md[:-1]
         and digest(values['docs/API_COMPATIBILITY.md']) == proof['DisplayMdSha256']
         == '01F1B402EDFE3A461251EEA362C1179D4AA24D3AD6036683088D0AE9C5F7A9C0', 'only one extra display EOF LF removed; raw archive preserved')
    for row in proof['NativePlans']:
        actual = blobs(args.repository, args.expected_head, [row['Path']])[row['Path']]
        body = json.loads(actual)
        need(digest(actual) == row['Sha256'] and len(body['Files']) == row['Rows']
             and source_path not in {r['Path'] for r in body['Files']}
             and row['NoAffectedRowOrSeal'] is True, 'no invented native manifest row/change')
    return proof


def parser():
    p = argparse.ArgumentParser()
    for name in ('repository', 'capture', 'report', 'promotion-plan', 'stage-guard'):
        p.add_argument('--' + name, type=Path, required=True)
    p.add_argument('--publication-deltas', type=Path)
    p.add_argument('--stage-guard-sha256', required=True)
    p.add_argument('--expected-head', required=True)
    p.add_argument('--run', type=int, required=True)
    p.add_argument('--artifact', type=int, required=True)
    return p
