"""Prepare SOURCE ONLY bounded selector runner derived from exercised327 controls."""
from pathlib import Path
import json, hashlib, ast
r = Path.cwd()
src = r / 'artifacts/process-error-handler327-reusable-focus-proposal1'
d = r / 'artifacts/undefined-lambda328-root-focus-proposal1'
assert not d.exists()
original = (src / 'focus.py').read_text()
changes = [
    ("nested=[s for s in selectors if s.endswith(':RunNestedFrames')];assert len(nested)==1", "assert a.mode != 'expected-red', 'Historical semantic RED requires separate explicit discriminator; no failure is automatically accepted'"),
    ("enumerate([nested[0]]+[s for s in selectors if s!=nested[0]])", "enumerate(selectors)"),
    ("        if selector==nested[0] and code==1 and case['Status']=='FAIL' and all(s in error for s in ['System.InvalidOperationException','Process error handler assertion failed','CheckFrame','RunNestedFrames']) and not any(s in error for s in ['TimeoutException','AggregateException','TypeLoadException','MissingMethodException']):return 'GENUINE_NESTED_ASSERTION_RED_PENDING_ROOT_REVIEW'", "        # Every failure is preserved as actual diagnostic evidence; none is accepted as historical RED."),
    ("nestedresult=results[0]['Classification'];modeok=a.mode=='diagnostic' and nestedresult in ['SELECTOR_PASS','GENUINE_NESTED_ASSERTION_RED_PENDING_ROOT_REVIEW'] or a.mode=='expected-red' and nestedresult=='GENUINE_NESTED_ASSERTION_RED_PENDING_ROOT_REVIEW' or a.mode=='expected-green' and nestedresult=='SELECTOR_PASS'\npassed=modeok and not any(x['ValidationError'] for x in results) and all(x['Classification']=='SELECTOR_PASS' for x in results[1:])", "passed=not any(x['ValidationError'] for x in results) and all(x['Classification']=='SELECTOR_PASS' for x in results)"),
]
for old, new in changes:
    assert original.count(old) == 1, old
    original = original.replace(old, new)
assert 'nested[0]' not in original and 'GENUINE_NESTED_ASSERTION_RED' not in original
ast.parse(original)
d.mkdir()
(d / 'focus.py').write_text(original, encoding='utf-8', newline='\n')
(d / 'references.json').write_bytes((src / 'references.json').read_bytes())
(d / 'README.md').write_text('''# Root328 focus recipe

Source-only adaptation of exercised327 bounded formatting, token, style, build and independently owned selector processes. Actual candidate path and SHA must be supplied. All source closure and tool references are verified before and after execution. All selectors from the candidate freeze run in separate processes. Target180s and selector120s deadlines retain bounded22s cleanup. Actual Tests/Runtime assemblies are mandatory; Compiler is verified when loaded. No failing selector is automatically accepted as semantic RED. Diagnostic failures retain process envelopes and raw reports; expected-green requires every selector PASS. Historical baseline comparisons need a separate explicit public-semantic discriminator. No source has been compiled or executed by preparation.
''', encoding='utf-8')
h = lambda p: hashlib.sha256(p.read_bytes()).hexdigest().upper()
receipt = {'Status': 'SOURCE_ONLY_UNBUILT_UNEXECUTED_328_SELECTOR_RECIPE',
    'ParentRecipeFreezeSha256': h(src / 'proposal-freeze.json'),
    'AllSelectorsFromCandidateFreeze': True, 'NoFailureAcceptedAsHistoricalRed': True,
    'TargetDeadlineSeconds': 180, 'SelectorDeadlineSeconds': 120,
    'Files': [{'Path': p.name, 'Bytes': p.stat().st_size, 'Sha256': h(p)} for p in sorted(d.iterdir())]}
(d / 'proposal-freeze.json').write_text(json.dumps(receipt, indent=2) + '\n')
print(json.dumps({'Directory': str(d), 'FreezeSha256': h(d / 'proposal-freeze.json'), 'Execution': False}))
