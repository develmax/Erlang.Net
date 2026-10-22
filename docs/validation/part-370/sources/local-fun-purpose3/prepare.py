import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
PREVIOUS = ROOT / 'artifacts/erlang370-termorder-purpose-proposal1'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def write(path, value):
    path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8', newline='\n')


old_plan = json.loads((PREVIOUS / 'copy-plan.json').read_text(encoding='utf-8'))
rows = []
for row in old_plan['Files']:
    target = ROOT / row['Target']
    actual = sha(target) if target.exists() else None
    assert actual == row['ExistingTargetSha256'], (row['Target'], actual)
    old = ROOT / row['Source']
    assert sha(old) == row['Sha256']
    new = OUT / 'source' / row['Target']
    assert not new.exists(), 'Do not overwrite a sealed source proposal'
    new.parent.mkdir(parents=True, exist_ok=True)
    text = old.read_text(encoding='utf-8')
    if new.name == 'TermOrder.cs':
        text = text.replace('public static bool ExactEquals(', 'internal static bool ExactEquals(')
        text = text.replace('public static bool NumericEquals(', 'internal static bool NumericEquals(')
    new.write_text(text, encoding='utf-8', newline='\n')
    rows.append({'Source': new.relative_to(ROOT).as_posix(), 'Target': row['Target'],
                 'Sha256': sha(new), 'ExistingTargetSha256': actual})

functions = ['==', '/=', '=:=', '=/=', '<', '>', '=<', '>=', 'min', 'max']
expected = [
    {'Pair': 'olduniq1_vs2_newuniqFF_vs00',
     'Results': [False, True, False, True, True, False, True, False, 'A', 'B']},
    {'Pair': 'olduniq1_vs1_newuniqFF_vs00',
     'Results': [True, False, False, True, False, False, True, True, 'A', 'A']}
]
for row in expected:
    row['Functions'] = functions
    row['Status'] = 'SOURCE_DERIVED_PREDICTION_NOT_EXECUTION_EXPECTATIONS_INJECTED_INTO_CAPTURE'
write(OUT / 'source-branches.json', {
    'Otp': 'OTP-29.1.1', 'Commit': 'ad05823719d77c8faee87348ea39513d4e2f99c5',
    'Authorities': [
        {'Path': 'artifacts/erlang370-numeric-proposal1/authority/erts/emulator/beam/utils.c',
         'Lines': '2157-2191 ordering local FUN; 1243-1281 exact eq local FUN'},
        {'Path': 'artifacts/erlang370-termorder-addendum1/authority/erts/emulator/beam/erl_fun.c',
         'Lines': '90-97 canonical entry key'},
        {'Path': 'artifacts/erlang370-numeric-proposal1/authority/erts/emulator/beam/erl_bif_guard.c',
         'Lines': '475-484 min/max retain first argument on comparator tie'}
    ],
    'Predictions': expected,
    'CorrectedFixture': {
        'Manifest': 'artifacts/erlang370-local-fun-fixture-successor2/fixture-manifest.json',
        'Sha256': sha(ROOT / 'artifacts/erlang370-local-fun-fixture-successor2/fixture-manifest.json'),
        'Policy': 'Use distinct stub-module descriptor for each pair kind. Do not reuse old input-module source identity relation for the successor. Bind actual min/max returned FUN and fun_info to the declared actual input entry; no native IDs injected.'
    },
    'Proof': {
        'OrderingAndNumericEquality': 'module,index,OldUnique,free count,environment; per-child numeric exactness remains independent from semantic purpose.',
        'ExactEquality': 'module,index,NewUnique,arity,free count,exact environment; preserves existing managed canonical-entry equivalence and hash inputs.',
        'CanonicalStorage': 'Keeps previous managed NewUnique/index/arity sorting, explicitly an implementation detail, not native semantic map order.',
        'DeepTraversal': 'Existing single CompareSnapshot while-loop, Stack<IEnumerator<TermComparisonStep>>, step dispatch and finally disposal retained. Purpose is a constant per traversal, not a recursive call. Hash code is unchanged.',
        'EntryFactory': 'RuntimeFunctionEntries remains untouched; first canonical entry metadata preservation is correct native behavior.'
    },
    'Unresolved': [
        'Compound-map order uses existing canonical storage order and remains incomplete for local FUN key comparator ties; no complete eight-MFA classification from this patch.',
        'Native flatmap comparator-based construction can collapse non-exact local FUN keys and replace last key object; current Dictionary construction differs.',
        'Native HAMT comparison/layout protocol for opaque-key ties remains outside this narrow patch.',
        'Build, corrected native/SDK observations, deep32768 and exact equality/hash regression validation remain Root responsibilities.'
    ]
})
write(OUT / 'copy-plan.json', {'Status': 'BOUNDED_LOCAL_FUN_FIX_NOT_COMPLETE_COMPOUND_MAP_COMPATIBILITY', 'Files': rows})
write(OUT / 'freeze.json', {'Status': 'UNBUILT_UNEXECUTED_SOURCE_PROPOSAL', 'Files': [
    {'Path': p.relative_to(OUT).as_posix(), 'Sha256': sha(p), 'Bytes': p.stat().st_size}
    for p in sorted(OUT.rglob('*')) if p.is_file() and p.name != 'freeze.json'
]})
print(json.dumps({'CopyPlan': sha(OUT / 'copy-plan.json'), 'Branches': sha(OUT / 'source-branches.json'), 'Freeze': sha(OUT / 'freeze.json'), 'Files': len(rows)}))
