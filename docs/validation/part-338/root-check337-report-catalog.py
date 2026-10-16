from pathlib import Path
import base64
import hashlib
import json
import re
import subprocess

r = Path.cwd()
head = 'add206c03c801cbd68c51d23dbbdb8e36f2d581f'
path = 'tools/Erlang.Differential/ProcLibReportCaseData.cs'
raw = subprocess.check_output(['git', 'show', head + ':' + path])
quoted = r'"(?:[^"\\]|\\.)*"'
pattern = r'\((' + quoted + r'),\s*(' + quoted + r'),\s*(' + quoted + r')\)'
rows = [tuple(json.loads(v) for v in match) for match in re.findall(pattern, raw.decode())]
assert len(rows) == len({x[0] for x in rows}) == 57
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
j = lambda p: json.loads(p.read_bytes())
comparison = j(r / 'artifacts/root-formatter-original-full-comparison4.json')
assert comparison['Status'] == 'MATCH' and comparison['Passed'] == comparison['Compared'] == 76
prior = {x['Name']: x for x in comparison['Rows'] if x['Track'] == 'report'}
sdk = j(r / 'artifacts/proc-lib337-all-capture4/report-observations.json')
assert len(sdk) == 57 and [x['Name'] for x in sdk] == [x[0] for x in rows]
original = r / 'artifacts/part336-remote-new-review/format/artifact-12063070500/capture'
for name, source, expected in rows:
    expected_bytes = base64.b64decode(expected, validate=True)
    actual_original_bytes = (original / ('report--' + name) / 'returned.etf').read_bytes()
    assert expected_bytes == actual_original_bytes
    assert prior[name]['OriginalEtfSha256'] == sha(expected_bytes)
    assert prior[name]['SourceSha256'] == sha(source.encode())
# Reader controls distinguish escaped quotes, backslashes, surrogate pairs and missing tuple members.
examples = [('a', 'x"y\\z\n', 'g2E='), ('unicode', '\U0001f600', 'g2E=')]
for row in examples:
    text = '(' + ','.join(json.dumps(x, ensure_ascii=True) for x in row) + ')'
    assert [tuple(json.loads(x) for x in match) for match in re.findall(pattern, text)] == [row]
assert not re.findall(pattern, '("one","two")')
guard = dict(Status='PASS_PUBLISHED337_REPORT_CATALOG57_BOUND_TO_INDEPENDENT_ORIGINAL_ETF',
             Head=head, CatalogSha256=sha(raw), Cases=57, ReaderControls=3,
             OriginalComparisonSha256=sha((r / 'artifacts/root-formatter-original-full-comparison4.json').read_bytes()),
             SdkOrNativeExecuted=False)
out = r / 'artifacts/root-337-report-catalog-guard.json'
assert not out.exists()
out.write_text(json.dumps(guard, indent=2) + '\n')
print(json.dumps(guard))
