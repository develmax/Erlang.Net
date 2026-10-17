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
PLAN_SHA = 'C36A67D0B759B4E4B8EC4341D1995498B4C48AFFC4C0C25E58C439486AE6037F'
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


def constants(text):
    values = {name: json.loads(value) for name, value in re.findall(
        r'public const string (\w+)\s*=\s*("(?:[^"\\]|\\.)*")\s*;', text)}
    for match in re.finditer(r'public const string (\w+)\s*=\s*"""\r?\n(.*?)\r?\n([ \t]*)"""\s*;', text, re.S):
        name, body, indent = match.groups()
        need(name not in values, 'constant duplicate')
        need(all(not line or line.startswith(indent) for line in body.split('\n')), 'raw literal indentation')
        values[name] = '\n'.join(line[len(indent):] if line else '' for line in body.split('\n'))
    return values


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

HEAD='a54bb96cdd91c57052e953fd2705630f16cfe92e'
RUNS={'ci':39007596959,'ordinary':39007643419}
_original_transport=transport
def transport(args,kind):
 need(args.expected_head==HEAD and args.run==RUNS[kind],'fixed published354 subject')
 return _original_transport(args,kind)
def publication(args):
 need(sha(args.promotion_plan)==PLAN_SHA,'exact final publication7 plan')
 plan=read(args.promotion_plan)
 need(plan['IndependentFullGuardSha256']=='5DE0409B787679B78A7780DC30F941991B1A292E859C7D5F74AAA65EA9191C78','actual full4')
 need(sha(args.stage_guard)==args.stage_guard_sha256.upper()=='4FEBFE38E11CAEA5C67A608179656BEAE7E21EFF567A983F85FF3700ACCEB940','final index audit')
 stage=read(args.stage_guard);need(stage['Paths']==2260 and stage['RawExactPaths']==2256 and stage['SourceGuards']==115,'2260 staged records')
 values=blobs(args.repository,args.expected_head,[x['TargetPath'] for x in plan['Files']]);need(len(values)==len(plan['Files'])==2260,'exact2260 plan paths')
 differences=[]
 for row in plan['Files']:
  data=values[row['TargetPath']]
  if digest(data)!=row['Sha256'] or len(data)!=row['Bytes']:
   original=Path(row['SourcePath']).read_bytes()
   need(row['TargetPath'] in stage['TextLfOnlyPaths'] and digest(original)==row['Sha256'] and original.replace(b'\r\n',b'\n')==data,'closed four publication LF changes')
   differences.append(row['TargetPath'])
 need(set(differences)==set(stage['TextLfOnlyPaths']) and len(differences)==4,'exact4 LF differences')
 return plan,values,len(values)
