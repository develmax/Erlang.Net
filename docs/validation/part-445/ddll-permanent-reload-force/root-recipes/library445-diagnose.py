from pathlib import Path
import hashlib, importlib.util, json, sys
r = Path.cwd()
p = r / 'tools/Erlang.NifTermCallbackOriginalObservations/passive_etf.py'
s = importlib.util.spec_from_file_location('etf445diagnostic', p)
m = importlib.util.module_from_spec(s); s.loader.exec_module(m)
original = r / 'artifacts/library445-ddll-final-original1'
managed = r / sys.argv[1] / 'observations/ddll-final-modes445'
rows = []
for group in ['values', 'default', 'depth0', 'depth1', 'depth2', 'depth8']:
    expected = m.decode_exact((original / group / 'main.etf').read_bytes())
    actual = m.decode_exact((managed / group / 'actual-main.etf').read_bytes())
    assert expected[0] == actual[0] == 'tuple' and expected[1][:2] == actual[1][:2]
    erows, arows = expected[1][2][1], actual[1][2][1]
    assert len(erows) == len(arows) == 10
    for erow, arow in zip(erows, arows):
        assert erow[1][0] == arow[1][0]
        row = dict(Group=group, Case=erow[1][0][1], Equal=erow == arow)
        if erow != arow:
            row.update(Expected=repr(erow), Actual=repr(arow))
        rows.append(row)
report = dict(DiagnosticOnly=True, WholeRowsExamined=len(rows), Equal=sum(x['Equal'] for x in rows),
              Different=sum(not x['Equal'] for x in rows), Rows=rows,
              OriginalFilesRewritten=False, ComparisonNormalization=False)
destination = r / sys.argv[2]
assert not destination.exists()
destination.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='Rows'}))
for row in rows:
    if not row['Equal']: print(row['Group'], row['Case'])
