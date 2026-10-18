"""Passive complete original/SDK difference inventory for historical capture1."""
from pathlib import Path
import collections, hashlib, importlib.util, json

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
TOOL = ROOT / 'artifacts/binary-original369-original1/tool'
NATIVE = ROOT / 'artifacts/binary369-original-remote1/run-39083021286/artifact-12119578493/capture'
SDK = ROOT / 'artifacts/binary-integration369/capture1/sdk'

def load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

profile = json.loads((TOOL / 'profile.json').read_bytes())
plan = json.loads((TOOL / 'source-inputs.corrected.json').read_bytes())
assert sha(TOOL / 'source-inputs.corrected.json') == 'A54B3C119311C183D635FD8112507E979FE574031584240D78639BF637A4CBC0'
decoder_path = TOOL / 'passive_etf_family.py'
assert sha(decoder_path) == next(r['Sha256'] for r in plan['ToolInputs'] if r['Path'] == 'passive_etf_family.py')
decoder = load(decoder_path, 'trusted_binary_decoder_exploratory')
strict = load(ROOT / 'artifacts/binary369-root-execution-proposal1/compare-binary.py', 'strict_binary_exploratory')
comparison = json.loads((OUT / 'comparison.json').read_bytes())
different = {(r['Group'], r['Case'], r['Depth']):r for r in comparison['Rows'] if not r['StrictEqual']}
all_rows = []
counts = collections.Counter()
top_frames = collections.Counter()

def top(outcome):
    if outcome[1][0] != ('atom', 'error') or len(outcome[1]) != 5:
        return None
    frames = outcome[1][3]
    return repr(frames[1][0]) if frames[1] else '[]'

for group in profile['Fixtures']:
    cases = {r['Name']:r for r in group['Cases']}
    for filename in ['returned.etf', 'stack-observations.etf']:
        left = strict.named(decoder.decode_exact((NATIVE / group['Id'] / filename).read_bytes()))
        right = strict.named(decoder.decode_exact((SDK / group['Id'] / filename).read_bytes()))
        pairs = [('values', name, left[name], right[name]) for name in left] if filename == 'returned.etf' else [(repr(depth), name, a, strict.named(right[depth])[name]) for depth in left for name, a in strict.named(left[depth]).items()]
        for depth, name, a, b in pairs:
            if a == b:
                continue
            previous = different[(group['Id'], name[1], depth)]
            counts[(group['Id'], 'opaque-only' if previous['OpaqueProvenancePending'] else 'other')] += 1
            original_top, sdk_top = top(a), top(b)
            if original_top != sdk_top:
                top_frames[(group['Id'], original_top, sdk_top)] += 1
            all_rows.append(dict(Group=group['Id'], Case=name[1], Depth=depth, Mfas=cases[name[1]]['Mfas'], OpaqueOnly=previous['OpaqueProvenancePending'], OriginalWhole=repr(a), SdkWhole=repr(b), OriginalTopFrame=original_top, SdkTopFrame=sdk_top))

by_case = collections.defaultdict(list)
for row in all_rows:
    by_case[(row['Group'], row['Case'], tuple(row['Mfas']), row['OpaqueOnly'])].append(row['Depth'])
summary = dict(Status='HISTORICAL_CAPTURE1_EXPLORATORY_DIFFERENCES_NOT_FINAL_ACCEPTANCE', ExpectedRows=3510, ActualRows=comparison['ActualRows'], StrictEqual=comparison['StrictEqual'], OpaqueOnlyUnaccepted=comparison['OpaqueRowsNotAccepted'], OtherDifferences=len(all_rows)-comparison['OpaqueRowsNotAccepted'], GroupCounts=[dict(Group=k[0], Kind=k[1], Count=v) for k,v in sorted(counts.items())], Cases=[dict(Group=k[0], Case=k[1], Mfas=k[2], OpaqueOnly=k[3], Depths=v) for k,v in sorted(by_case.items())], TopFrameDifferences=[dict(Group=k[0], Original=k[1], Sdk=k[2], Count=v) for k,v in top_frames.most_common()], OriginalAdmissionSha256=sha(ROOT / 'artifacts/binary369-original-admission-proposal2/actual1/acceptance.json'), ComparisonSha256=sha(OUT / 'comparison.json'), Projection=False, Normalization=False, CompatibilityPromoted=False)
(OUT / 'whole-differences.json').write_text(json.dumps(dict(Rows=all_rows, FieldsOmitted=False, Representation='Complete Python typed ETF tree repr; source raw bytes remain immutable.'), indent=2) + '\n', encoding='utf-8')
(OUT / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
print(json.dumps({k:summary[k] for k in ['StrictEqual', 'OpaqueOnlyUnaccepted', 'OtherDifferences', 'GroupCounts']}))
for row in summary['Cases']:
    if not row['OpaqueOnly']:
        print(row['Group'] + '/' + row['Case'] + ': ' + ','.join(row['Mfas']) + ' ' + str(len(row['Depths'])))
