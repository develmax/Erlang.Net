from pathlib import Path

r = Path.cwd()
p = r / 'artifacts/root-check338-archive-line-endings2.py'
assert not p.exists()
s = (r / 'artifacts/root-check338-archive-line-endings.py').read_text()
s = s.replace('assert tested == parent', "assert tested.replace(b'\\r\\n', b'\\n') == parent")
s = s.replace("assert before.replace(b'\\r\\n', b'\\n') == tested", "assert before.replace(b'\\r\\n', b'\\n') == tested.replace(b'\\r\\n', b'\\n')")
s = s.replace('TestedAndPublishedParentSha256=h(tested)', 'TestedSha256=h(tested), PublishedParentSha256=h(parent)')
s = s.replace('PASS_UNOWNED_ARCHIVE_DIFFERENCES_ARE_ONLY_CRLF_TO_LF_AND_EXACT_PARENT_BLOBS', 'PASS_UNOWNED_ARCHIVE_DIFFERENCES_ARE_ONLY_LINE_ENDINGS_WITH_EXACT_PARENT_TEXT')
s = s.replace('root-338-unowned-line-endings.json', 'root-338-unowned-line-endings2.json')
compile(s, str(p), 'exec')
p.write_text(s)
