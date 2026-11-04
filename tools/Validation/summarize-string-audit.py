"""Bind all 71 source contracts to complete, separate backend observations."""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--catalog', required=True)
p.add_argument('--replay', required=True)
p.add_argument('--edges', required=True)
p.add_argument('--numbers', required=True)
p.add_argument('--jaro', required=True)
p.add_argument('--depths', required=True)
p.add_argument('--output', required=True)
p.add_argument('--assembly', required=True)
p.add_argument('--tests')
a = p.parse_args()
root = Path(__file__).resolve().parents[2]
catalog = json.loads(Path(a.catalog).read_text(encoding='utf-8'))
expected = {(r['Function'], r['Arity']) for r in catalog['Apis']}
assert len(expected) == 71
for row in catalog['Apis']:
    actual = hashlib.sha256((root / row['CSharpHandler']).read_bytes()).hexdigest().upper()
    assert actual == row['HandlerSha256'], f'Stale source catalog: {row["CSharpHandler"]}'

out = Path(a.output)
out.mkdir(parents=True, exist_ok=True)
reports = []
coverage = {}
totals = {}
selected_assembly_sha256 = hashlib.sha256(Path(a.assembly).read_bytes()).hexdigest().upper()
chunks = sorted(Path(a.replay).glob('audit-*'))
assert len(chunks) == 7, 'All seven acquisition chunks are required'
for backend in ('managed', 'beam'):
    counts = {key: {'Cases': 0, 'Values': 0, 'Errors': 0} for key in expected}
    totals[backend] = {'Observations': 0, 'Values': 0, 'ErrorsAndFullStacks': 0, 'BackingProfiles': 0}
    identities = set()
    for directory in [*chunks, Path(a.edges), Path(a.numbers), Path(a.jaro)]:
        path = directory / (backend + '-report.json')
        raw = path.read_bytes()
        r = json.loads(raw)
        assert r['AssemblySha256'] == selected_assembly_sha256, f'Report is not from the selected assembly: {path}'
        assert r['Backend'] == backend and r['CompleteOutcomesPass'] and r['BackingMatched'], path
        assert r['ExportCount'] == r['ExercisedExports'] == 71, path
        assert r['Failed'] == r['StackDifferences'] == 0, path
        assert r['ErrorReasonsMatched'] == r['FullStacksMatched'], path
        assert r['Cases'] == r['ValuesMatched'] + r['ErrorReasonsMatched'], path
        assert {(x['Function'], x['Arity']) for x in r['ApiCoverage']} == expected, path
        assert sum(x['Cases'] for x in r['ApiCoverage']) == r['Cases'], path
        for x in r['ApiCoverage']:
            for field in counts[(x['Function'], x['Arity'])]:
                counts[(x['Function'], x['Arity'])][field] += x[field]
        identities.add(r['AssemblySha256'])
        totals[backend]['Observations'] += r['Cases']
        totals[backend]['Values'] += r['ValuesMatched']
        totals[backend]['ErrorsAndFullStacks'] += r['FullStacksMatched']
        totals[backend]['BackingProfiles'] += 1
        reports.append({'Backend': backend, 'Corpus': directory.name, 'Cases': r['Cases'],
                        'ReportSha256': hashlib.sha256(raw).hexdigest().upper(),
                        'CorpusSha256': r['CorpusSha256']})
    assert len(identities) == 1, 'Do not combine different implementation assemblies'
    totals[backend]['AssemblySha256'] = identities.pop()
    coverage[backend] = counts
assert coverage['managed'] == coverage['beam']
depth_profiles = []
for depth in (0, 1, 2, 3, 8, 32):
    for backend in ('managed', 'beam'):
        path = Path(a.depths) / ('depth-' + str(depth)) / (backend + '-report.json')
        raw = path.read_bytes()
        r = json.loads(raw)
        assert r['Backend'] == backend and r['BacktraceDepth'] == depth, path
        assert r['CompleteOutcomesPass'] and r['BackingMatched'], path
        assert r['Cases'] == 23 and r['ValuesMatched'] == 1, path
        assert r['ErrorReasonsMatched'] == r['FullStacksMatched'] == 22, path
        assert r['AssemblySha256'] == totals[backend]['AssemblySha256'], path
        depth_profiles.append({'Backend': backend, 'Depth': depth, 'Cases': r['Cases'],
                               'Values': r['ValuesMatched'], 'ErrorsAndFullStacks': r['FullStacksMatched'],
                               'ReportSha256': hashlib.sha256(raw).hexdigest().upper()})
tests = None
if a.tests:
    test_path = Path(a.tests)
    tests = json.loads(test_path.read_bytes())
    assert not tests.get('Partial') and not tests.get('Listed')
    assert tests['Failed'] == 0 and tests['Passed'] == len(tests['Tests']) > 0
    tests = {'Passed': tests['Passed'], 'Failed': 0,
             'ReportSha256': hashlib.sha256(test_path.read_bytes()).hexdigest().upper()}
summary = {'Baseline': catalog['Baseline'], 'OtpSourceCommit': catalog['Commit'],
           'WorkspaceHead': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip(),
           'WorkspaceDirty': bool(subprocess.check_output(['git', 'status', '--porcelain'], cwd=root)),
           'AssemblySha256': selected_assembly_sha256,
           'SourceContracts': 71, 'PublicSourceClauses': sum(len(x['OriginalClauses']) for x in catalog['Apis']),
           'CatalogSha256': hashlib.sha256(Path(a.catalog).read_bytes()).hexdigest().upper(),
           'Backends': totals, 'Reports': reports, 'DepthProfiles': depth_profiles, 'FullTests': tests,
           'UniversalAllInputsOrPlatformsClaimed': False, 'MfaStatusesPromoted': 0}
(out / 'audit-summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
lines = ['# All 71 string API contracts', '',
         'Pinned OTP 29.1.1; source commit `' + catalog['Commit'] + '`.', '',
         'Each row links a source clause review to actual complete outcomes for **each** selected backend.',
         'Counts include repeated smoke rows between bounded chunks. They are observations, not unique inputs',
         'or a proof over every possible term, host or resource limit. Compatibility statuses remain unchanged.', '',
         'Returned `{error, Reason}` tuples count as returned values; exceptions require their class, reason and full stack.', '',
         '| API | Source clause lines | Default arguments | Contract reviewed | Returned values / exceptions per backend |',
         '| --- | --- | --- | --- | --- |']
for row in catalog['Apis']:
    key = row['Function'], row['Arity']
    c = coverage['managed'][key]
    lines.append('| `' + key[0] + '/' + str(key[1]) + '` | ' +
                 ', '.join(str(x['Line']) for x in row['OriginalClauses']) + ' | ' +
                 row['Defaults'] + ' | ' + row['ContractReview'] + ' | ' +
                 str(c['Values']) + ' / ' + str(c['Errors']) + ' |')
lines += ['', 'The source catalog records exact handler SHA256 identities and original heads/guards.',
          'Native `list_to_float/1` is reviewed against the C implementation, not its Erlang NIF stub.',
          'All error observations include their full source stack; live compiled resources require actual',
          'native queries and bijective binding. Arbitrary input references remain exact input values.', '']
(out / 'api-audit.md').write_text('\n'.join(lines), encoding='utf-8')
print(json.dumps({'Exports': 71, 'Backends': totals, 'FullTests': tests}))
