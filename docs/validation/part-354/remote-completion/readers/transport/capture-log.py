"""Read one mapped GitHub job log, retain capture and append an audited derivative."""
import argparse, hashlib, json, shutil, subprocess, urllib.request, urllib.error
from pathlib import Path

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None

def main():
    p = argparse.ArgumentParser()
    p.add_argument('--capture', type=Path, required=True)
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    mapping = json.loads((a.capture / 'run-map.json').read_bytes())
    assert mapping['Head'] == 'a54bb96cdd91c57052e953fd2705630f16cfe92e'
    jobs = json.loads((a.capture / 'jobs.json').read_bytes())['jobs']
    assert len(jobs) == 1 and jobs[0]['id'] == mapping['Job']
    assert jobs[0]['status'] == 'completed' and jobs[0]['conclusion'] == 'success'
    assert not a.output.exists()
    completed = subprocess.run(['git', 'credential', 'fill'], input=b'protocol=https\nhost=github.com\n\n', stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, timeout=30, check=True)
    passwords = [x[9:] for x in completed.stdout.splitlines() if x.startswith(b'password=')]
    assert len(passwords) == 1
    req = urllib.request.Request('https://api.github.com/repos/develmax/Erlang.Net/actions/jobs/' + str(mapping['Job']) + '/logs', headers={'Authorization': 'Bearer ' + passwords[0].decode(), 'User-Agent': 'ErlangNet-Root-354', 'Accept': 'application/vnd.github+json'})
    try:
        response = urllib.request.build_opener(NoRedirect()).open(req, timeout=60)
    except urllib.error.HTTPError as e:
        assert e.code == 302
        url = e.headers['Location']
        assert url.startswith('https://')
        response = urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'ErlangNet-Root-354'}), timeout=60)
    with response:
        assert response.status == 200
        raw = response.read(64 * 1024 * 1024 + 1)
    assert 0 < len(raw) <= 64 * 1024 * 1024
    shutil.copytree(a.capture, a.output)
    (a.output / ('job-' + str(mapping['Job']) + '.log')).write_bytes(raw)
    rows = []
    for f in sorted(a.output.rglob('*')):
        if f.is_file() and f.name not in ('download-manifest.json', 'run-map.json'):
            data = f.read_bytes()
            rows.append(dict(Path=f.relative_to(a.output).as_posix(), Bytes=len(data), Sha256=hashlib.sha256(data).hexdigest().upper()))
    (a.output / 'download-manifest.json').write_text(json.dumps(rows, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(Job=mapping['Job'], Bytes=len(raw), Sha256=hashlib.sha256(raw).hexdigest().upper(), Output=str(a.output))))

if __name__ == '__main__':
    try:
        main()
    except Exception as e:
        raise SystemExit('Log capture failed: ' + type(e).__name__ + '; details suppressed') from None
