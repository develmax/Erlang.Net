"""Passive synthetic negative controls; identities here are never oracle evidence."""
from pathlib import Path
import copy, hashlib, importlib.util, json
import reference_relation as relation

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
TOOL = ROOT / 'artifacts/binary-original369-original1/tool'
plan = json.loads((TOOL / 'source-inputs.corrected.json').read_bytes())
assert hashlib.sha256((TOOL / 'source-inputs.corrected.json').read_bytes()).hexdigest().upper() == 'A54B3C119311C183D635FD8112507E979FE574031584240D78639BF637A4CBC0'
decoder_path = TOOL / 'passive_etf_family.py'
assert hashlib.sha256(decoder_path.read_bytes()).hexdigest().upper() == next(r['Sha256'] for r in plan['ToolInputs'] if r['Path'] == 'passive_etf_family.py')
spec = importlib.util.spec_from_file_location('trusted_binary_controls', decoder_path)
decoder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(decoder)

def atom(s): return ('atom', s)
def integer(n): return ('integer', n)
def tup(*items): return ('tuple', tuple(items))
def lst(*items): return ('list', tuple(items))
def binary(s): return ('binary', s)
def reference(n): return ('reference', atom('synthetic-control-only'), 0, (n, n + 1), 90)

def encode(node):
    def body(n):
        if n[0] == 'atom':
            b = n[1].encode(); return bytes([119, len(b)]) + b
        if n[0] == 'integer': return bytes([98]) + n[1].to_bytes(4, 'big', signed=True)
        if n[0] == 'binary': return bytes([109]) + len(n[1]).to_bytes(4, 'big') + n[1]
        if n[0] == 'tuple': return bytes([104, len(n[1])]) + b''.join(body(x) for x in n[1])
        if n[0] == 'list': return bytes([108]) + len(n[1]).to_bytes(4, 'big') + b''.join(body(x) for x in n[1]) + bytes([106])
        if n[0] == 'reference': return bytes([90]) + len(n[3]).to_bytes(2, 'big') + body(n[1]) + n[2].to_bytes(4, 'big') + b''.join(x.to_bytes(4, 'big') for x in n[3])
        raise AssertionError(n[0])
    return (bytes([131]) + body(node)).hex().upper()

def observed(ref, role='compiled'):
    witness = tup(atom(role), atom('bm'), ref, binary(b'a'), binary(b'abcabc'), tup(integer(0), integer(1))) if role == 'compiled' else tup(atom(role), ref)
    frame = tup(atom('binary'), atom('match'), lst(binary(b'abcabc'), tup(atom('ac' if role == 'compiled' else 'bm'), ref)), lst())
    return tup(atom('error'), atom('error'), atom('badarg'), lst(frame), tup(lst(), lst(witness)))

native, sdk = observed(reference(1)), observed(reference(3))
proof = dict(Role='compiled', LocalIdentityBound=True, RegistryAcquired=True, ResourceKind='BinarySearchPattern', PayloadTag='bm', ReferenceEtfHex=encode(reference(3)), ProvenanceEtfHex=encode(sdk[1][-1][1][1][1][0]), ReplayedMatchEtfHex=encode(tup(integer(0), integer(1))))

def accepted(a, b, binding):
    try:
        x, y = relation.check_binding(a, b, binding, decoder)
        return relation.equal(a, b, x, y)
    except (AssertionError, ValueError, KeyError, TypeError):
        return False

rows = []
def check(name, result):
    assert result, name
    rows.append(dict(Name=name, Passed=True))

check('source-bound-synthetic-reference-alias-positive', accepted(native, sdk, proof))
for field, value in [('LocalIdentityBound', False), ('RegistryAcquired', False), ('ResourceKind', 'BinarySnapshot'), ('PayloadTag', 'ac'), ('Role', 'ordinary'), ('ReplayedMatchEtfHex', encode(tup(integer(1), integer(1)))), ('ReferenceEtfHex', encode(reference(9))), ('ProvenanceEtfHex', '835A')]:
    changed = dict(proof, **{field:value})
    check('reject-' + field, not accepted(native, sdk, changed))
parts = list(sdk[1]); frames = list(parts[3][1]); original_frame = frames[0]
frame_parts = list(original_frame[1]); frame_parts[2] = lst(binary(b'abcabc'), tup(atom('ac'), reference(9)))
frames[0] = tup(*frame_parts); parts[3] = lst(*frames)
check('reject-reference-alias-mismatch-in-reason-frame', not accepted(native, tup(*parts), proof))
parts = list(sdk[1]); parts[3] = lst()
check('reject-dropped-caller-frame', not accepted(native, tup(*parts), proof))
parts = list(sdk[1]); parts[3] = lst(original_frame, original_frame)
check('reject-inserted-caller-frame', not accepted(native, tup(*parts), proof))
parts = list(sdk[1]); frame_parts = list(original_frame[1]); frame_parts[3] = lst(tup(atom('error_info'), atom('corrupt'))); parts[3] = lst(tup(*frame_parts))
check('reject-modified-error-info', not accepted(native, tup(*parts), proof))
parts = list(sdk[1]); parts[-1] = tup(lst(atom('unexpected-callback')), parts[-1][1][1])
check('reject-modified-effects', not accepted(native, tup(*parts), proof))
parts = list(sdk[1]); parts[2] = atom('system_limit')
check('reject-error-reason-change', not accepted(native, tup(*parts), proof))
parts = list(sdk[1]); source = list(parts[-1][1][1][1][0][1]); source[3] = binary(b'b'); parts[-1] = tup(lst(), lst(tup(*source)))
changed = dict(proof, ProvenanceEtfHex=encode(tup(*source)))
check('reject-pattern-substitution-even-when-proof-agrees', not accepted(native, tup(*parts), changed))
ordinary_native, ordinary_sdk = observed(reference(11), 'ordinary'), observed(reference(13), 'ordinary')
ordinary_proof = dict(Role='ordinary', LocalIdentityBound=True, RegistryAcquired=False, ResourceKind=None, PayloadTag=None, ReferenceEtfHex=encode(reference(13)), ProvenanceEtfHex=encode(tup(atom('ordinary'), reference(13))), ReplayedMatchEtfHex=None)
check('ordinary-source-reference-positive', accepted(ordinary_native, ordinary_sdk, ordinary_proof))
check('reject-ordinary-substituted-for-compiled', not accepted(native, ordinary_sdk, ordinary_proof))
result = dict(Status='PASS_PASSIVE_SYNTHETIC_COMPARATOR_CONTROLS', Controls=rows, Count=len(rows), NativeEvidence=False, RuntimeRegistryExecuted=False, NoOracleIdentityWordsUsed=True)
(HERE / 'controls.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps(dict(Status=result['Status'], Count=len(rows))))
