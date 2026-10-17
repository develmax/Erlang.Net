"""ROOT-ONLY GET discovery template; never dispatches, maps, retries POST or chooses a run."""
import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import time
import urllib.parse
import urllib.request

API = 'https://api.github.com/repos/develmax/Erlang.Net'
SUBJECTS = (('native', 'scheduler-affinity-original.yml', 'workflow_dispatch'),
            ('ordinary', 'differential.yml', 'workflow_dispatch'), ('ci', 'validate.yml', 'push'))


def need(value, message):
    if not value:
        raise ValueError(message)


def main(args):
    need(re.fullmatch('[0-9a-f]{40}', args.head), 'explicit published40head')
    secret = credentials = completed = headers = passwords = None
    try:
        completed = subprocess.run(['git', '-C', str(args.repository), 'credential', 'fill'],
            input=b'protocol=https\nhost=github.com\n\n', stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL, timeout=30)
        need(completed.returncode == 0, 'credential helper failure; suppressed')
        credentials = completed.stdout
        passwords = [line[9:] for line in credentials.splitlines() if line.startswith(b'password=')]
        need(len(passwords) == 1 and passwords[0], 'credential absent; suppressed')
        secret = passwords[0].decode()
        headers = {'Authorization': 'Bearer ' + secret, 'Accept': 'application/vnd.github+json',
                   'User-Agent': 'ErlangNet-Root353-GET-Discovery', 'X-GitHub-Api-Version': '2022-11-28'}
        def get(endpoint):
            request = urllib.request.Request(API + endpoint, headers=headers, method='GET')
            try:
                with urllib.request.urlopen(request, timeout=45) as response:
                    need(response.status == 200, 'GET status')
                    return json.loads(response.read())
            except Exception:
                raise RuntimeError('GET discovery failed; request/credentials suppressed; no dispatch performed') from None
        repo = get('')
        need(repo['full_name'] == 'develmax/Erlang.Net' and repo['default_branch'] == 'main', 'repository identity')
        snapshots, candidates = [], []
        for kind, workflow, event in SUBJECTS:
            model = get('/actions/workflows/' + workflow)
            need(model['path'] == '.github/workflows/' + workflow, 'workflow identity')
            rows = []
            total = None
            for page in range(1, 11):
                query = urllib.parse.urlencode({'branch': 'main', 'event': event, 'head_sha': args.head,
                                                'per_page': 100, 'page': page})
                result = get('/actions/workflows/' + str(model['id']) + '/runs?' + query)
                if total is None: total = result['total_count']
                need(result['total_count'] == total and total <= 1000, 'stable bounded discovery pagination')
                rows.extend(result['workflow_runs'])
                if len(rows) >= total: break
            need(len(rows) == total and len(rows) == len({r['id'] for r in rows}), 'complete unique run discovery')
            for run in rows:
                need(run['head_sha'] == args.head and run['head_branch'] == 'main'
                     and run['path'] == '.github/workflows/' + workflow and run['event'] == event
                     and run['workflow_id'] == model['id'], 'API result exact subject')
                candidates.append({'Kind': kind, 'Run': run['id'], 'Attempt': run['run_attempt'],
                    'Head': args.head, 'Status': run['status'], 'Conclusion': run['conclusion'],
                    'CreatedAt': run['created_at'], 'HtmlUrl': run['html_url']})
            snapshots.append({'Kind': kind, 'Workflow': model, 'TotalCount': total, 'Runs': rows})
        # Multiple or zero candidates are observations, never an automatic run choice or reason to POST again.
        value = {'Status': 'GET_ONLY_EXACT_HEAD_DISCOVERY_NOT_MAPPING', 'Head': args.head,
            'ObservedUnix': time.time(), 'Candidates': candidates, 'RawSnapshots': snapshots,
            'AutomaticRunSelection': False, 'PostOrDispatchPerformed': False,
            'NextAction': 'Root explicitly selects and maps each intended run; never redispatch unknown prior POST.'}
        with args.report.open('xb') as stream:
            stream.write((json.dumps(value, indent=2) + '\n').encode())
            stream.flush()
            os.fsync(stream.fileno())
    finally:
        secret = credentials = completed = headers = passwords = None


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--head', required=True)
    parser.add_argument('--report', type=Path, required=True)
    main(parser.parse_args())
