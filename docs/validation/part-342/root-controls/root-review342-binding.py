from pathlib import Path
import hashlib, json, subprocess

root = Path.cwd()
candidate = root/'artifacts/runtime342-candidate'
full = root/'artifacts/runtime342-full-actual3'
read = lambda path: json.loads(path.read_bytes())
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
freeze_path = full/'source-freeze.json'
assert sha(freeze_path.read_bytes()) == '07F9E80B6E43264A7D108C146F6E650CBE101232DD710D1FDCAEC42DDDA08CED'
freeze = read(freeze_path)
assert freeze['ParentHead'] == '7b43e00608c41303c1b03a25ebd5385d12517ee5'
for row in freeze['Files']:
    data = (candidate/row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256'], row['Path']
peer = read(candidate/'docs/validation/part-342/current-verified-source-review.json')
styler_deltas = []
for row in peer['ReviewedFiles']:
    data = (candidate/row['Path']).read_bytes()
    if sha(data) != row['Sha256']:
        assert row['Path'] == 'tests/Erlang.Tests/Program.cs', row['Path']
        for name, method in [('source-fun-current-old-purged-sibling-captures', 'Erlang.Tests.RemoteSourceFunImageHostChecks.CurrentOldPurgedAndSiblingCaptures'), ('source-fun-fresh-receiver-lexical-boundaries', 'Erlang.Tests.RemoteSourceFunLexicalHostChecks.FreshReceiverLexicalCaptureBoundaries')]:
            formatted = f'Test(\n    "{name}",\n    {method}\n);'
            compact = f'Test("{name}", {method});'
            assert data.count(formatted.encode()) == 1
            data = data.replace(formatted.encode(), compact.encode())
        earlier = (root/'artifacts/runtime342-rebase-source/source'/row['Path']).read_bytes()
        assert sha(earlier) == row['Sha256']
        assert data.replace(b'\r\n', b'\n') == earlier.replace(b'\r\n', b'\n'), 'Unexpected non-layout source change'
        styler_deltas.append(row['Path'])
assert styler_deltas == ['tests/Erlang.Tests/Program.cs']
protected = read(root/'docs/validation/part-341/reporting-final-source-freeze.json')['Files']
changes = []
for row in protected:
    path = row['Path']
    if path != 'docs/supported-mfas.json' and (root/path).read_bytes() != ((candidate/path) if (candidate/path).exists() else (root/path)).read_bytes():
        changes.append(path)
assert set(changes) == {'docs/mfa-compatibility.json','tests/Erlang.Tests/Program.cs','tests/Erlang.Tests/Erlang.Tests.csproj'}
csproj = 'tests/Erlang.Tests/Erlang.Tests.csproj'
assert (root/csproj).read_bytes().replace(b'\r\n',b'\n') == (candidate/csproj).read_bytes().replace(b'\r\n',b'\n')
before = read(root/'docs/mfa-compatibility.json')
after = read(candidate/'docs/mfa-compatibility.json')
assert len(before['Entries']) == len(after['Entries']) == 587
supplement = 'validation/part-342/current-verified-source-review.json'
for old, new in zip(before['Entries'], after['Entries'], strict=True):
    if old['Status'] == 'Verified compatible':
        assert {k:v for k,v in old.items() if k!='Evidence'} == {k:v for k,v in new.items() if k!='Evidence'}
        assert new['Evidence'][:-1] == old['Evidence']
        added = new['Evidence'][-1]
        assert added['Kind'] == 'SourceReview' and added['Path'] == supplement
        assert sha((candidate/'docs'/supplement).read_bytes()) == added['Sha256']
    else:
        assert old == new
for old, new in zip(before['VerifiedSourceFiles'],after['VerifiedSourceFiles'],strict=True):
    assert old['Path'] == new['Path']
    if old['Path'] != 'src/Erlang.Compiler/Semantics.cs':
        assert old == new
    assert sha((candidate/new['Path']).read_bytes()) == new['Sha256']
semantics = 'src/Erlang.Compiler/Semantics.cs'
old_code = subprocess.check_output(['git','show',freeze['ParentHead']+':'+semantics])
new_code = (candidate/semantics).read_bytes()
insertion = b'                ClosureCaptures.Remember(f, bound);\n'
assert new_code.count(insertion) == 1 and new_code.replace(insertion,b'') == old_code
assert b'Test(Erlang.Tests.MfaCompatibilityHostNames.Test, Erlang.Tests.MfaCompatibilityHostChecks.Run);' in (candidate/'tests/Erlang.Tests/Program.cs').read_bytes()
assert b'Status = Erlang.Tests.MfaRegistryMetadataNames.Implemented' in (candidate/'tests/Erlang.Tests/Program.cs').read_bytes()
result = dict(Passed=True, Status='PASS_FINAL342_SOURCE_BINDING_AND_PROTECTED341_DELTA', SourceFreezeSha256=sha(freeze_path.read_bytes()), Protected341ChangedPaths=sorted(changes), ReviewedOwnedRows=len(freeze['Files']), EolOnlyProtectedPaths=[csproj], ActualCandidateSourcePeerRows=len(peer['ReviewedFiles']), PeerToFinalTwoRegistrationLineWrapsOnly=styler_deltas, All587StatusesScopesLimitsPreserved=True, FiveVerifiedEvidenceSupplementOnly=True, SemanticsOnlyCapturePlanInsertion=True, OtherNineVerifiedSourcesUnchanged=True, FullValidationClaimed=False, OriginalExecutionClaimed=False)
out = root/'artifacts/root-part342-binding-update-review.json'
assert not out.exists()
out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(result))
