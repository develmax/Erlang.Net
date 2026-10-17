import hashlib
import json
import pathlib

root = pathlib.Path(__file__).resolve().parents[2]
out = pathlib.Path(__file__).resolve().parent
view = root / 'artifacts/size-bif354-owned-style-view'
path = 'src/Erlang.Runtime/ModuleRegistry.SourceLoadNif.cs'
data = (view / path).read_bytes()
assert b'SourceLoadNifFilename.IsValidUtf8' not in data


def sha(value):
    return hashlib.sha256(value).hexdigest().upper()


report = {
    'Status': 'REFUSED_STYLED_PERMANENT2_DEFAULT_FILENAME_REPAIR_NOT_COMPOSED',
    'Path': path,
    'StyledBytes': len(data),
    'StyledSha256': sha(data),
    'BeforeSnapshotSha256': sha((root / 'artifacts/size-bif354-permanent-test-proposal2/runtime-snapshot' / path).read_bytes()),
    'StyleTokenProofSha256': sha((root / 'artifacts/root-size354-style-token-identity.json').read_bytes()),
    'StyledFixtureProofSha256': sha((root / 'artifacts/root-size354-styled-fixtures.json').read_bytes()),
    'Finding': 'Source13 complete ModuleRegistry.SourceLoadNif snapshot overwrote reviewed default-only filename repair in permanent composition. The styled handler-null branch directly calls indirect stub; SourceLoadNifFilename validator exists but is not referenced. Root style/token evidence correctly preserves this incomplete composition and cannot detect this semantic omission.',
    'ExpectedActualFocusRepair': 'Actualfocus6 used source12 plus three defaultfilename files. Reapply reviewed default-only branch on top of source13 rather than choosing either whole file.',
    'RequiredCorrection': 'New immutable permanent/styled successor with only default branch repair, source binding and token/style checks updated honestly; keep previous2 and current50 evidence unchanged.',
    'CurrentSourceCopy': data.decode(),
    'SdkExecuted': False,
    'NetworkExecuted': False,
    'MainMutation': False,
}
target = out / 'styled-permanent2-initial-finding.json'
target.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode())
print(sha(target.read_bytes()))
