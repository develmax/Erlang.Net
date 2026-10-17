"""ROOT-ONLY network template. Author has not executed network actions.

dispatch: one exclusive intent before one POST; unknown outcomes NEVER retried.
map: explicit human-selected run id; no ambiguous automatic run discovery.
poll: bounded GET-only polling of that exact run. capture: terminal raw API/ZIP snapshot.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import time
import urllib.request
import urllib.error
import zipfile

API = 'https://api.github.com/repos/develmax/Erlang.Net'
MANIFEST = '4C9A79CD6E8BC18F68316D42B2F30DB171E67A242CA81CA84BB8A022E8BA5BFB'
KINDS = {
    'native': ('scheduler-affinity-original.yml', 'scheduler-affinity-original-five', 'workflow_dispatch'),
    'ordinary': ('differential.yml', 'differential', 'workflow_dispatch'),
    'ci': ('validate.yml', 'validation', 'push'),
}


def need(value, message):
    if not value:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def write_new(path, value):
    with path.open('xb') as stream:
        stream.write((json.dumps(value, indent=2) + '\n').encode())
        stream.flush()
        os.fsync(stream.fileno())


def git(repository, *args):
    return subprocess.check_output(['git', '-C', str(repository), *args], timeout=30, stderr=subprocess.DEVNULL)


def validate_local(args):
    need(re.fullmatch('[0-9a-f]{40}', args.head), 'exact published SHA required')
    if args.mode == 'dispatch':
        need(git(args.repository, 'rev-parse', 'HEAD').decode().strip() == args.head, 'local published HEAD')
        need(not git(args.repository, 'status', '--porcelain', '--untracked-files=no').strip(), 'tracked worktree must be clean')
    manifest = 'tools/Erlang.SchedulerAffinityOriginalObservations/manifest.json'
    raw = git(args.repository, 'show', args.head + ':' + manifest)
    need(hashlib.sha256(raw).hexdigest().upper() == MANIFEST, 'published native manifest')
    model = json.loads(raw)
    need(model['Flags'] == ['+S', '2:2'] and len(model['Cases']) == 5, 'native scope')
    for row in model['Files']:
        actual = git(args.repository, 'show', args.head + ':tools/Erlang.SchedulerAffinityOriginalObservations/' + row['Path'])
        need(hashlib.sha256(actual).hexdigest().upper() == row['Sha256'], 'published tool row')
    for row in model['Dependencies'] + [model['Workflow']]:
        actual = git(args.repository, 'show', args.head + ':' + row['Path'])
        need(hashlib.sha256(actual).hexdigest().upper() == row['Sha256'], 'published dependency/workflow row')


def validate_run(run, args):
    workflow, _, event = KINDS[args.kind]
    need(run['id'] == args.run and run['head_sha'] == args.head and run['head_branch'] == 'main'
         and run['path'] == '.github/workflows/' + workflow and run['event'] == event
         and run['run_attempt'] == 1, 'explicit same-head run identity')


def main(args):
    validate_local(args)
    workflow, artifact_name, event = KINDS[args.kind]
    secret = credential = headers = None
    try:
        # Never emit helper stdout/stderr or exception details containing Authorization.
        completed = subprocess.run(['git', '-C', str(args.repository), 'credential', 'fill'],
                                   input=b'protocol=https\nhost=github.com\n\n',
                                   stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, timeout=30)
        need(completed.returncode == 0, 'credential helper failed (details suppressed)')
        credential = completed.stdout
        passwords = [line[9:] for line in credential.splitlines() if line.startswith(b'password=')]
        need(len(passwords) == 1 and passwords[0], 'credential absent (details suppressed)')
        secret = passwords[0].decode()
        headers = {'Authorization': 'Bearer ' + secret, 'Accept': 'application/vnd.github+json',
                   'User-Agent': 'ErlangNet-Root-Exact353', 'X-GitHub-Api-Version': '2022-11-28'}

        def request(endpoint, method='GET', body=None):
            data = None if body is None else json.dumps(body).encode()
            req = urllib.request.Request(API + endpoint, data=data, method=method,
                                         headers={**headers, **({'Content-Type': 'application/json'} if data else {})})
            try:
                with urllib.request.urlopen(req, timeout=60) as response:
                    return response.status, response.read()
            except Exception:
                raise RuntimeError('GitHub request failed; inspect persisted intent/raw state; never automatically retry POST') from None

        def get(endpoint):
            code, raw = request(endpoint)
            need(code == 200, 'GET status')
            return json.loads(raw)

        # Authenticated read and default branch fence immediately before any dispatch.
        repo = get('')
        need(repo['full_name'] == 'develmax/Erlang.Net' and repo['default_branch'] == 'main', 'repository identity')
        if args.mode == 'dispatch':
            branch = get('/branches/main')
            need(branch['commit']['sha'] == args.head, 'default branch moved; do not POST')
        wf = get('/actions/workflows/' + workflow)
        need(wf['path'] == '.github/workflows/' + workflow and wf['state'] == 'active', 'active exact workflow')
        args.output.mkdir(parents=True, exist_ok=True)
        if args.mode == 'dispatch':
            need(args.kind != 'ci', 'CI is automatic; do not dispatch')
            body = {'ref': 'main'}
            if args.kind == 'native':
                body['inputs'] = {'source_manifest_sha256': MANIFEST}
            intent = {'Head': args.head, 'Kind': args.kind, 'Workflow': wf['path'], 'WorkflowId': wf['id'],
                      'CreatedUnix': time.time(), 'Body': body, 'AutomaticRetryAllowed': False,
                      'UnknownOutcomeRequiresManualMapping': True,
                      'ExpectedScope': '5 explicit +S2:2' if args.kind == 'native' else '6321 ordinary + fd8; default flags unchanged'}
            # Exclusive, durable intent is the repeat guard. No output overwrite even after an exception.
            write_new(args.output / 'dispatch-intent.json', intent)
            code, _ = request('/actions/workflows/' + str(wf['id']) + '/dispatches', 'POST', body)
            need(code == 204, 'dispatch outcome unknown or rejected; no redispatch')
            write_new(args.output / 'dispatch-accepted.json', {'Status': 'ACCEPTED_ONCE', 'Head': args.head, 'HttpStatus': code})
            return
        need(args.run is not None and args.run > 0, 'explicit mapped run required')
        if args.mode in ('poll', 'capture'):
            mapped = json.loads((args.output / 'run-map.json').read_bytes())
            need(mapped['Head'] == args.head and mapped['Run'] == args.run and mapped['Attempt'] == 1
                 and mapped['Kind'] == args.kind, 'previously mapped exact run required')
        run = get('/actions/runs/' + str(args.run))
        validate_run(run, args)
        if args.mode == 'poll':
            # At most 55 seconds; user/Root can inspect between bounded calls.
            deadline, delay = time.monotonic() + 55, 5
            while run['status'] != 'completed' and time.monotonic() + delay < deadline:
                time.sleep(delay)
                run = get('/actions/runs/' + str(args.run))
                validate_run(run, args)
                delay = min(20, delay * 2)
            write_new(args.output / ('poll-' + str(time.time_ns()) + '.json'), run)
            return
        jobs = get('/actions/runs/' + str(args.run) + '/jobs?per_page=100')
        need(jobs['total_count'] == len(jobs['jobs']) == 1, 'one exact job; no pagination truncation')
        job = jobs['jobs'][0]
        need(job['run_id'] == args.run and job['head_sha'] == args.head and job['run_attempt'] == 1, 'job subject')
        mapping = {'Head': args.head, 'Run': args.run, 'Attempt': 1, 'Job': job['id'], 'Kind': args.kind}
        if args.mode == 'capture':
            need(mapping == mapped, 'mapped job changed')
        if args.mode == 'map':
            if args.kind != 'ci':
                intent = json.loads((args.output / 'dispatch-intent.json').read_bytes())
                need(intent['Head'] == args.head and intent['Kind'] == args.kind, 'intent subject')
            write_new(args.output / 'run-map.json', mapping)
            return
        need(args.mode == 'capture' and run['status'] == 'completed', 'terminal capture only')
        artifacts = get('/actions/runs/' + str(args.run) + '/artifacts?per_page=100')
        need(artifacts['total_count'] == len(artifacts['artifacts']) == 1, 'one complete artifact response')
        artifact = artifacts['artifacts'][0]
        need(artifact['name'] == artifact_name and artifact['expired'] is False
             and artifact['workflow_run']['head_sha'] == args.head and artifact['workflow_run']['id'] == args.run, 'artifact subject')
        destination = args.output / ('capture-' + str(artifact['id']))
        destination.mkdir(exist_ok=False)
        for name, value in [('run-map', mapping), ('run', run), ('jobs', jobs), ('artifacts', artifacts)]:
            write_new(destination / (name + '.json'), value)
        # Do not forward Authorization across the artifact redirect to object storage.
        class NoRedirect(urllib.request.HTTPRedirectHandler):
            def redirect_request(self, req, fp, code, msg, hdrs, newurl):
                return None
        def redirected_get(endpoint, limit):
            req = urllib.request.Request(API + endpoint, headers=headers)
            try:
                response = urllib.request.build_opener(NoRedirect).open(req, timeout=60)
            except urllib.error.HTTPError as error:
                need(error.code == 302, 'download redirect status')
                location = error.headers['Location']
                need(location.startswith('https://'), 'HTTPS download redirect')
                response = urllib.request.urlopen(location, timeout=60)
            with response:
                data = response.read(limit + 1)
            need(len(data) <= limit, 'bounded download')
            return data
        data = redirected_get('/actions/artifacts/' + str(artifact['id']) + '/zip', 512 * 1024 * 1024)
        need(len(data) <= 512 * 1024 * 1024 and len(data) == artifact['size_in_bytes']
             and artifact['digest'] == 'sha256:' + hashlib.sha256(data).hexdigest(), 'artifact API digest')
        zipped = destination / ('artifact-' + str(artifact['id']) + '.zip')
        with zipped.open('xb') as stream:
            stream.write(data)
        # Raw job log is a separate authenticated endpoint; preserve it in the same
        # immutable download manifest for checkout and exact PASS-order auditing.
        log = redirected_get('/actions/jobs/' + str(job['id']) + '/logs', 128 * 1024 * 1024)
        with (destination / ('job-' + str(job['id']) + '.log')).open('xb') as stream:
            stream.write(log)
        extracted = destination / ('artifact-' + str(artifact['id']))
        extracted.mkdir()
        with zipfile.ZipFile(zipped) as archive:
            names = archive.namelist()
            need(len(names) == len(set(names)) and sum(i.file_size for i in archive.infolist()) <= 1024**3, 'bounded unique ZIP')
            for item in archive.infolist():
                name = item.filename
                need(name and not name.startswith('/') and ':' not in name and '\\' not in name
                     and '..' not in PurePosixPath(name).parts
                     and ((item.external_attr >> 16) & 0o170000) != 0o120000, 'safe ZIP entry')
                target = extracted / name
                if item.is_dir():
                    target.mkdir(parents=True, exist_ok=True)
                else:
                    target.parent.mkdir(parents=True, exist_ok=True)
                    with target.open('xb') as stream:
                        stream.write(archive.read(item))
        rows = [{'Path': p.relative_to(destination).as_posix(), 'Bytes': p.stat().st_size, 'Sha256': sha(p)}
                for p in sorted(destination.rglob('*')) if p.is_file() and p != destination / 'run-map.json']
        write_new(destination / 'download-manifest.json', rows)
    finally:
        secret = credential = headers = None
        passwords = completed = None


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('mode', choices=('dispatch', 'map', 'poll', 'capture'))
    parser.add_argument('--kind', choices=tuple(KINDS), required=True)
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--head', required=True)
    parser.add_argument('--run', type=int)
    main(parser.parse_args())
