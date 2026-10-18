from pathlib import Path
import ast,json,hashlib
O=Path(__file__).resolve().parent;R=O.parents[1]
base=R/'docs/validation/part-371/evidence/erlang371-retained-comparison2/compare.py'
s=base.read_text()
old="ap.add_argument('--output',type=Path,required=True);a=ap.parse_args()"
new="ap.add_argument('--output',type=Path,required=True);ap.add_argument('--source-binding-proof',type=Path,required=True);ap.add_argument('--source-binding-proof-sha256',required=True);a=ap.parse_args()"
assert old in s;s=s.replace(old,new)
old=" proof=J(O/'source-binding.json')\n assert H(O/'source-binding.json')=='D6A5E56DE1195317DE530BBAA15918A9B6AE3EDCA4486C0A3CA5065205BAA204'"
new=" assert H(a.source_binding_proof)==a.source_binding_proof_sha256.upper()\n proof=J(a.source_binding_proof)"
assert old in s;s=s.replace(old,new)
s=s.replace("proof['GuardCount']==496","proof['GuardCount']>=499")
s=s.replace("len(guards)==496 and len({x['Path'] for x in guards})==496","len(guards)==proof['GuardCount'] and len({x['Path'] for x in guards})==proof['GuardCount']")
(O/'compare.py').write_text(s,encoding='utf-8')
ast.parse(s)
json.dump({'BaseScript':str(base.relative_to(R)),'BaseScriptSha256':hashlib.sha256(base.read_bytes()).hexdigest().upper(),'OnlyPolicyDelta':'Require explicit externally hash-approved source guard proof with >=499 preserved guards; no comparison, relation, diagnostic or mutation-control algorithm is changed. Existing captured/current DLL equality remains mandatory.','NoSubjectExecuted':True},(O/'successor-delta.json').open('w'),indent=2)
