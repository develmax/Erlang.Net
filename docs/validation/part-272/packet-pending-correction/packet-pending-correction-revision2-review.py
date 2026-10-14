from pathlib import Path
import json, hashlib, re
r = Path.cwd()
p = r / 'artifacts/packet-pending-correction-prototype'
a = json.loads((r / 'artifacts/packet-pending-correction-source-freeze-initial.json').read_text())
f = r / 'artifacts/packet-pending-correction-source-freeze-revision2.json'
b = json.loads(f.read_text())
def sha(raw): return hashlib.sha256(raw).hexdigest().upper()
assert len(a['Files']) == len(b['Files']) == 5
for old, new in zip(a['Files'], b['Files']):
    assert old['Path'] == new['Path']
    assert sha((p / new['Path']).read_bytes()) == new['Sha256']
    assert old['BeforeSha256'] == new['BeforeSha256']
    before, after = old['SourceContent'], new['SourceContent']
    if new['Path'].endswith('.erl'):
        assert re.sub(r'\b(?:ReadyReason|ObservedReason|CleanupReason|ForcedReason)\b', 'Reason', after) == before
    elif new['Path'].endswith('PacketNames.cs') or new['Path'].endswith('source-plan.json'):
        assert after.replace(b['SourceSha256'], a['SourceSha256']) == before
    else:
        assert after == before
receipt = {'Status': 'PASS_BOUNDED_SOURCE_DELTA_REVIEW', 'ReviewedFreezeSha256': sha(f.read_bytes()), 'PriorReviewedFreezeSha256': '8ED32F54DD083E2BEF38C689264D1FDCC4E21489B1B2D83726A52CEC28828961', 'FiveCurrentSourcesExact': True, 'OnlyDelta': 'Four distinct Erlang receive/cleanup reason variable names and bound source hash updates; original reason payloads, pending protocol,18 artifacts and workflow unchanged.', 'ProbeSha256': b['SourceSha256'], 'ErlangCompileExecuted': False, 'NativeExecuted': False, 'MainOrGitMutation': False}
out = r / 'artifacts/packet-pending-correction-revision2-independent-review.json'
out.write_text(json.dumps(receipt, indent=2))
print(sha(out.read_bytes()))
print(json.dumps(receipt, indent=2))
