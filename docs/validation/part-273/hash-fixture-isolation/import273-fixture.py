from pathlib import Path
import hashlib, json, shutil, subprocess

r = Path(__file__).resolve().parents[1]
a = r / 'artifacts'
digest = lambda data: hashlib.sha256(data).hexdigest().upper()
read = lambda path: json.loads(path.read_bytes())
guardpath = a / 'part273-main-import-guard.json'
guard = read(guardpath)
assert guard['Imported'] == len(guard['Files']) == 67
assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=r).decode().strip() == guard['Head']
assert digest((a / 'hash-allocation-fixture-independent-source-review.json').read_bytes()) == '3373AD7C171EF7FE914119FA0F1536A43CA95189FEA2B5D907DCFAA8D6CEBDC4'
assert digest((a / 'hash-cast-cache-diagnostic-final-receipt.json').read_bytes()) == '4A826345DD297BD59906471BCE2464AC4550C834343117E04E8A8BDC563FA0EF'
for row in guard['Files']:
    assert digest((r / row['Path']).read_bytes()) == row['AfterSha256'], row['Path']
assert 'Source layout: 0 files need formatting' in (a / 'hash-allocation-fixture-style-initial.log').read_text()
plans = []
for name in ['ErlangHashHostChecks.cs', 'ErlangHashCheckDiagnostics.cs', 'ErlangHashAllocationControlNames.cs']:
    relative = 'tests/Erlang.Tests/' + name
    source = a / 'hash-allocation-fixture-proposal' / name
    assert source.read_bytes() == (a / 'hash-allocation-fixture-style-root' / relative).read_bytes()
    target = r / relative
    if name == 'ErlangHashAllocationControlNames.cs':
        assert not target.exists()
    plans.append((relative, target.read_bytes() if target.exists() else None, source.read_bytes()))
relative = 'tests/Erlang.Tests/Program.cs'
before = (r / relative).read_bytes()
text = before.decode('utf-8-sig')
anchor = 'Test(Erlang.Tests.ProcessInfoLogicalHostNames.Test, Erlang.Tests.ProcessInfoLogicalHostChecks.Run);'
assert text.count(anchor) == 1
newline = '\r\n' if '\r\n' in text else '\n'
addition = newline + 'Test(' + newline + '    Erlang.Tests.ErlangHashAllocationControlNames.Test,' + newline + '    Erlang.Tests.ErlangHashHostChecks.AllocationPositiveControl' + newline + ');'
plans.append((relative, before, text.replace(anchor, anchor + addition).encode()))
archive = a / 'part273-fixture-import-evidence'
assert not archive.exists()
assert not (a / 'part273-main-import-guard-before-fixture70.json').exists()
archive.mkdir()
shutil.copyfile(guardpath, a / 'part273-main-import-guard-before-fixture70.json')
rows = {row['Path']: dict(row) for row in guard['Files']}
phase = []
for relative, old, new in plans:
    if old is not None:
        p = archive / 'before' / relative
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_bytes(old)
    p = archive / 'after' / relative
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_bytes(new)
    phase.append({'Path': relative, 'BeforeSha256': digest(old) if old is not None else None, 'AfterSha256': digest(new)})
    rows[relative] = {**rows.get(relative, {'Path': relative, 'BeforeSha256': digest(old) if old is not None else None}), 'AfterSha256': digest(new)}
# Every dependency/source assertion above precedes the first Main write.
for relative, old, new in plans:
    target = r / relative
    assert (target.read_bytes() if target.exists() else None) == old
    target.write_bytes(new)
guard.update(Imported=70, Files=sorted(rows.values(), key=lambda row: row['Path']), FixtureExtraPaths=[row['Path'] for row in phase if row['Path'] != 'tests/Erlang.Tests/Program.cs'], CurrentCallerCastCacheReplacementObserved=True, HistoricalAllocationCauseProved=False)
assert len(guard['Files']) == 70
guardpath.write_bytes((json.dumps(guard, indent=2) + '\n').encode())
(archive / 'receipt.json').write_bytes((json.dumps({'Status': 'IMPORTED_SOURCE_ONLY', 'MainFullPassed': False, 'ExpectedTests': 6527, 'Budget': 4096, 'WarmupCalls': 1, 'Files': phase}, indent=2) + '\n').encode())
print('Imported 70 guarded source paths; full 6527 validation remains required.')
