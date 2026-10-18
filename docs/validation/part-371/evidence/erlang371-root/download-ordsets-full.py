from pathlib import Path
import hashlib
import json
import subprocess
import urllib.request
import zipfile

r = Path.cwd()
run_id = 39113248779
head = '5baf7226c5a0bb7716968394d12c73ee11e0f1c6'
base = 'https://api.github.com/repos/develmax/Erlang.Net'
credential = subprocess.run(['git', 'credential', 'fill'], input=b'protocol=https\nhost=github.com\n\n', stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, check=True, timeout=30)
password = [x[9:] for x in credential.stdout.splitlines() if x.startswith(b'password=')]
assert len(password) == 1
class ArtifactRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, url):
        redirected = super().redirect_request(request, response, code, message, headers, url)
        from urllib.parse import urlsplit
        assert urlsplit(url).scheme == 'https'
        if urlsplit(request.full_url).netloc != urlsplit(url).netloc:
            redirected.remove_header('Authorization')
        return redirected
opener = urllib.request.build_opener(ArtifactRedirect())
headers = {'Authorization': 'Bearer ' + password[0].decode(), 'Accept': 'application/vnd.github+json', 'User-Agent': 'ErlangNet-Original371'}
def get(path):
    with urllib.request.urlopen(urllib.request.Request(base + path, headers=headers), timeout=60) as response:
        return json.load(response)
run = get('/actions/runs/' + str(run_id))
jobs = get('/actions/runs/' + str(run_id) + '/jobs?per_page=100')
assert run['head_sha'] == head
if run['status'] != 'completed':
    print(json.dumps(dict(Status=run['status'], Run=run_id, Steps=[x['name'] for j in jobs['jobs'] for x in j['steps'] if x['status'] == 'in_progress'])))
    raise SystemExit(0)
artifacts = get('/actions/runs/' + str(run_id) + '/artifacts?per_page=100')
o = r / f'artifacts/ordsets371-original1/download/run-{run_id}'
o.mkdir(parents=True, exist_ok=False)
for name, value in [('run', run), ('jobs', jobs), ('artifacts', artifacts)]:
    (o / (name + '.json')).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')
rows = []
for artifact in artifacts['artifacts']:
    assert not artifact['expired']
    identifier = artifact['id']
    request = urllib.request.Request(base + '/actions/artifacts/' + str(identifier) + '/zip?fresh=' + str(__import__('time').time_ns()), headers=headers)
    with opener.open(request, timeout=120) as response:
        data = response.read(512 * 1024 * 1024 + 1)
    assert len(data) <= 512 * 1024 * 1024
    archive = o / f'artifact-{identifier}.zip'
    archive.write_bytes(data)
    target = o / f'artifact-{identifier}'
    target.mkdir()
    with zipfile.ZipFile(archive) as z:
        names = [x.filename for x in z.infolist()]
        assert len(names) == len(set(names))
        for entry in z.infolist():
            p = (target / entry.filename).resolve()
            assert p.is_relative_to(target.resolve())
            assert (entry.external_attr >> 16) & 0o170000 != 0o120000
            if entry.is_dir():
                p.mkdir(parents=True, exist_ok=True)
            else:
                p.parent.mkdir(parents=True, exist_ok=True)
                p.write_bytes(z.read(entry))
    rows.append(dict(Id=identifier, ZipSha256=hashlib.sha256(data).hexdigest().upper(), Directory=str(target)))
(o / 'download.json').write_text(json.dumps(rows, indent=2) + '\n', encoding='utf-8')
print(json.dumps(dict(Status=run['conclusion'], Run=run_id, Artifacts=rows)))
