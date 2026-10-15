import argparse
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
parser = argparse.ArgumentParser()
parser.add_argument('--repository', type=Path, required=True)
parser.add_argument('--report', type=Path, required=True)
args = parser.parse_args()
if sys.flags.optimize != 0 or args.report.exists():
    raise RuntimeError('Unoptimized Python and fresh synthetic report required')
spec = importlib.util.spec_from_file_location('synthetic_allocator_guard', Path(__file__).with_name('verify-original19.py'))
g = importlib.util.module_from_spec(spec)
spec.loader.exec_module(g)
s = g.helper(args.repository / 'tools/Erlang.RawOnLoadVisibilityObservations/guard/verify-original9.py', 'synthetic_bound_stack', g.STACK_SHA)
d = g.helper(args.repository / 'tools/Erlang.RawOnLoadVisibilityObservations/guard/passive_etf.py', 'synthetic_bound_decoder', g.DECODER_SHA)

def a(n): return ('atom', n)
def i(n): return ('integer', n)
def t(*xs): return ('tuple', tuple(xs))
def l(*xs): return ('list', tuple(xs))
def returned(v): return t(a('return'), v)
def failure(reason, stack=None): return t(a('exception'), a('error'), a(reason), l(t(a('erlang'), a('f'), i(1), l())) if stack is None else stack)

rows = []
def accept(name, value, expected):
    g.projection(value, expected, s)
    rows.append(dict(Name=name, Expected='ACCEPTED'))
def refuse(name, value, expected):
    try: g.projection(value, expected, s)
    except (ValueError, TypeError, IndexError): rows.append(dict(Name=name, Expected='REFUSED')); return
    raise RuntimeError('Malformed projection unexpectedly accepted: ' + name)

accept('badarg-full-stack', failure('badarg'), 'badarg')
accept('function-clause-full-stack', failure('function_clause'), 'function_clause')
refuse('wrong-error-class', t(a('exception'), a('throw'), a('badarg'), l()), 'badarg')
refuse('wrong-error-reason', failure('badarith'), 'badarg')
refuse('missing-stack', failure('badarg', l()), 'badarg')
refuse('malformed-stack-frame', failure('badarg', l(t(i(1), a('f'), i(0)))), 'badarg')
instance = lambda n: t(a('instance'), i(n), l(t(a('blocks'), l())))
accept('actual-shaped-instances', returned(l(instance(0), instance(2))), 'allocator_atom')
accept('actual-shaped-named-instance', returned(l(t(a('default_mmap'), l()))), 'allocator_atom')
accept('disabled-allocator-distinct-false', returned(a('false')), 'allocator_atom')
refuse('descending-instances', returned(l(instance(2), instance(0))), 'allocator_atom')
refuse('duplicate-instances', returned(l(instance(1), instance(1))), 'allocator_atom')
refuse('negative-instance', returned(l(instance(-1))), 'allocator_atom')
refuse('wrong-info-container', returned(l(t(a('instance'), i(0), i(0)))), 'allocator_atom')
full_data = l(t(a('versions'), l(i(49)), l(i(50))), t(a('options'), l()), t(a('mbcs'), l()), t(a('sbcs'), l()), t(a('calls'), l()))
full = returned(l(t(a('instance'), i(0), full_data)))
accept('positive-full-info-tree', full, 'allocator_full')
refuse('full-info-disabled-not-success', returned(a('false')), 'allocator_full')
refuse('full-info-empty-list-not-success', returned(l()), 'allocator_full')
refuse('full-info-empty-instance-not-success', returned(l(instance(0))), 'allocator_full')
refuse('full-info-missing-calls', returned(l(t(a('instance'), i(0), l(*full_data[1][:-1])))), 'allocator_full')
refuse('full-info-version-arity', returned(l(t(a('instance'), i(0), l(t(a('versions'), l(i(49))), *full_data[1][1:])))), 'allocator_full')
ordered = l(t(a('binary_alloc'), l(instance(0))), t(a('ets_alloc'), a('false')))
accept('allocator-request-order', returned(ordered), 'allocator_order')
refuse('allocator-reversed-order', returned(l(*reversed(ordered[1]))), 'allocator_order')
refuse('allocator-missing-result', returned(l(ordered[1][0])), 'allocator_order')
keys = ['total', 'processes', 'processes_used', 'system', 'atom', 'atom_used', 'binary', 'code', 'ets']
nums = [100, 30, 20, 70, 10, 9, 4, 5, 6]
mem = l(*(t(a(k), i(n)) for k, n in zip(keys, nums)))
accept('nine-memory-partition', returned(mem), 'memory_all')
refuse('memory-key-order', returned(l(*reversed(mem[1]))), 'memory_all')
refuse('memory-missing-key', returned(l(*mem[1][:-1])), 'memory_all')
refuse('memory-partition-contradiction', returned(l(t(a('total'), i(101)), *mem[1][1:])), 'memory_all')
refuse('memory-negative-bytes', returned(l(t(a('total'), i(-1)), *mem[1][1:])), 'memory_all')
refuse('memory-used-exceeds-capacity', returned(l(*mem[1][:2], t(a('processes_used'), i(31)), *mem[1][3:])), 'memory_all')
accept('memory-empty', returned(l()), 'empty')
refuse('memory-nonempty-emptycase', returned(l(i(1))), 'empty')
dups = l(t(a('ets'), i(4)), t(a('total'), i(100)), t(a('ets'), i(4)))
accept('memory-ordered-duplicates', returned(dups), 'memory_order')
refuse('memory-duplicate-not-same-snapshot', returned(l(*dups[1][:2], t(a('ets'), i(5)))), 'memory_order')
message = 'load_nif/2 must be explicitly called from the NIF module. It cannot be called through apply/3.'
nif = returned(t(a('error'), t(a('bad_lib'), l(*(i(ord(c)) for c in message)))))
accept('dynamic-nif-special-rejection', nif, 'dynamic_nif')
refuse('dynamic-nif-wrong-shape', returned(a('bad_lib')), 'dynamic_nif')
refuse('dynamic-nif-wrong-code', returned(t(a('error'), t(a('badarg'), l()))), 'dynamic_nif')
refuse('dynamic-nif-text-drift', returned(t(a('error'), t(a('bad_lib'), l(i(65))))), 'dynamic_nif')
keys = ['current_function', 'initial_call', 'status', 'message_queue_len', 'links', 'dictionary', 'trap_exit', 'error_handler', 'priority', 'group_leader', 'total_heap_size', 'heap_size', 'stack_size', 'reductions', 'garbage_collection', 'suspending']
proc = l(*(t(a(k), l()) for k in keys))
accept('native-default-key-order', returned(proc), 'process_default')
refuse('native-default-reordered', returned(l(*reversed(proc[1]))), 'process_default')
plan = {'PinnedCommit': g.PIN, 'Cases': [{'Name': n} for n in g.CASE_NAMES]}
policy = {'PinnedCommit': g.PIN, 'Cases': plan['Cases']}
receipt = {'PinnedCommit': g.PIN, 'Complete': True, 'SemanticAssertionsExecuted': False, 'SdkExecuted': False, 'SdkCompared': False, 'NativeFixtureUsed': False, 'OrderedFreshVmCases': g.CASE_NAMES, 'ExpectedFreshVmCases': 19, 'SourcePlanSha256': g.PLAN_SHA, 'Exclusions': []}
g.validate_contract(plan, policy, receipt)
rows.append(dict(Name='valid-raw-only-envelope', Expected='ACCEPTED'))
for name, change in [('incomplete-receipt', {'Complete': False}), ('collector-semantic-pass-claim', {'SemanticAssertionsExecuted': True}), ('collector-sdk-claim', {'SdkCompared': True}), ('collector-native-fixture-claim', {'NativeFixtureUsed': True}), ('wrong-source-plan', {'SourcePlanSha256': '0'*64}), ('wrong-case-count', {'ExpectedFreshVmCases': 17}), ('wrong-case-order', {'OrderedFreshVmCases': list(reversed(g.CASE_NAMES))}), ('missing-case', {'OrderedFreshVmCases': g.CASE_NAMES[:-1]}), ('wrong-pin', {'PinnedCommit': '0'*40}), ('hidden-exclusion', {'Exclusions': ['x']})]:
    try: g.validate_contract(plan, policy, dict(receipt, **change))
    except ValueError: rows.append(dict(Name=name, Expected='REFUSED'))
    else: raise RuntimeError('Invalid raw-only envelope accepted: ' + name)
try: g.helper(args.repository / 'tools/Erlang.RawOnLoadVisibilityObservations/guard/passive_etf.py', 'must_not_import_drifted_helper', '0'*64)
except ValueError: rows.append(dict(Name='decoder-source-hash-drift', Expected='REFUSED'))
else: raise RuntimeError('Unreviewed decoder imported')
try: d.decode_exact(bytes([131, 97, 1, 0]))
except ValueError: rows.append(dict(Name='raw-trailing-byte-refused', Expected='REFUSED'))
else: raise RuntimeError('Trailing raw byte accepted')
try: d.decode_exact(bytes([131, 104, 2, 97, 1]))
except ValueError: rows.append(dict(Name='raw-truncation-refused', Expected='REFUSED'))
else: raise RuntimeError('Truncated raw tuple accepted')
args.report.write_bytes((json.dumps(dict(Passed=True, Count=len(rows), OriginalExecuted=False, SdkCompared=False, SyntheticOnly=True, Controls=rows), indent=2) + '\n').encode())
print(json.dumps(dict(Passed=True, Count=len(rows), OriginalExecuted=False, SdkCompared=False)))
