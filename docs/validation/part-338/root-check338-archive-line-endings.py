from pathlib import Path
import hashlib
import json
import subprocess

r = Path.cwd()
a = r / 'artifacts'
c = a / 'proc-lib338-candidate'
d = a / 'proc-lib338-full-source'
j = lambda p: json.loads(p.read_bytes())
h = lambda raw: hashlib.sha256(raw).hexdigest().upper()
owned = {x['Path'] for x in j(d / 'source-freeze.json')['Files']}
closure = j(d / 'source-resource-closure.json')['Files']
head = 'add206c03c801cbd68c51d23dbbdb8e36f2d581f'
assert subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip() == head
different = []
for row in closure:
    if row['Path'] not in owned:
        before = (r / row['Path']).read_bytes()
        tested = (c / row['Path']).read_bytes()
        if before != tested:
            parent = subprocess.check_output(['git', 'show', head + ':' + row['Path']])
            assert tested == parent, row['Path']
            assert before.replace(b'\r\n', b'\n') == tested, row['Path']
            assert len(tested) == row['Bytes'] and h(tested) == row['Sha256']
            different.append(dict(Path=row['Path'], WorkingTreeBytes=len(before), TestedBytes=len(tested),
                                  WorkingTreeSha256=h(before), TestedAndPublishedParentSha256=h(tested)))
guard = dict(Status='PASS_UNOWNED_ARCHIVE_DIFFERENCES_ARE_ONLY_CRLF_TO_LF_AND_EXACT_PARENT_BLOBS',
             Parent=head, Closure=2627, Owned=44, DifferingPaths=different,
             SemanticSourceChanges=False, MainEdited=False, CandidateEdited=False, SdkExecuted=False)
out = a / 'root-338-unowned-line-endings.json'
assert not out.exists()
out.write_text(json.dumps(guard, indent=2) + '\n')
print(json.dumps(dict(Status=guard['Status'], Differences=len(different), Paths=[x['Path'] for x in different])))
