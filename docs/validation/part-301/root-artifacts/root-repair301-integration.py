from pathlib import Path
import hashlib
import json
import shutil

root = Path.cwd().resolve()
artifacts = root / 'artifacts'
isolated = artifacts / 'root-process-info301-isolated'
candidate = isolated / 'candidate'
revision = artifacts / 'process-info301-integration-revision1'
assert not revision.exists()
digest = lambda data: hashlib.sha256(data).hexdigest().upper()
paths = ['tools/Erlang.Differential/ProcessInfoDefaultSubsetCases.cs', 'tests/Erlang.Tests/FunctionalMfaContracts.cs', 'tests/Erlang.Tests/HaltSubprocessRegistrationCounts.cs']
parents = {}
for name in paths:
    data = (candidate / name).read_bytes()
    assert data == (root / name).read_bytes()
    parents[name] = digest(data)
    target = revision / 'parents' / name
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(data)
names = [
    ('CommonFields', 'process-info-default-subset-common-fields'),
    ('CommonTypes', 'process-info-default-subset-common-types'),
    ('RegisteredName', 'process-info-default-subset-registered-name'),
    ('Query2StableProjection', 'process-info-default-subset-query2-stable-projection'),
    ('InvalidTargets', 'process-info-default-subset-invalid-targets'),
    ('DeadLocal', 'process-info-default-subset-dead-local'),
    ('StaleLocal', 'process-info-default-subset-stale-local'),
    ('RemoteTarget', 'process-info-default-subset-remote-target'),
]
catalog = candidate / paths[0]
text = catalog.read_text(encoding='utf-8-sig')
for identifier, value in names:
    literal = '"' + value + '"'
    assert text.count(literal) == 1
    text = text.replace(literal, 'ProcessInfoDefaultSubsetCaseNames.' + identifier)
catalog.write_text(text, encoding='utf-8')
name_path = 'tools/Erlang.Differential/ProcessInfoDefaultSubsetCaseNames.cs'
assert not (candidate / name_path).exists()
(candidate / name_path).write_text('namespace Erlang.Differential;\n\npublic static class ProcessInfoDefaultSubsetCaseNames\n{\n' + ''.join('    public const string ' + identifier + ' = "' + value + '";\n' for identifier, value in names) + '}\n', encoding='utf-8')
parents[name_path] = None
paths.append(name_path)
functional = candidate / paths[1]
text = functional.read_text(encoding='utf-8-sig')
needle = '        ("erlang", "is_record", 1)'
assert text.count(needle) == 1
text = text.replace(needle, '        (ErlangModuleNames.Module, ProcessInfoNames.Function, 1) => ProcessInfoDefaultSubsetCaseNames.CommonFields,\n' + needle)
functional.write_text(text, encoding='utf-8')
counts = candidate / paths[2]
text = counts.read_text(encoding='utf-8-sig')
assert text.count('DefaultCoreAndOtp = 502;') == text.count('WithOptionalIo = 504;') == 1
counts.write_text(text.replace('DefaultCoreAndOtp = 502;', 'DefaultCoreAndOtp = 503;').replace('WithOptionalIo = 504;', 'WithOptionalIo = 505;'), encoding='utf-8')
records = []
for name in paths:
    target = revision / 'overlay' / name
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(candidate / name, target)
    records.append({'Path': name, 'ParentSha256': parents[name], 'Sha256': digest(target.read_bytes())})
receipt = {'Status': 'SOURCE_ONLY_INTEGRATION_REPAIR_NOT_YET_BUILT', 'RuntimeAlgorithmChanged': False, 'MfaFixtureBinding': {'Mfa': 'erlang:process_info/1', 'Fixture': names[0][1]}, 'ExactExportCounts': {'Previous': [502, 504], 'Current': [503, 505], 'AddedMfas': 1, 'OptionalIoDeltaUnchanged': 2}, 'ComparableCaseNamesAndSourcesUnchanged': True, 'Files': records}
(revision / 'source-freeze.json').write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
paths11 = json.loads((isolated / 'paths8.json').read_bytes()) + paths[1:]
assert len(paths11) == len(set(paths11)) == 11
(isolated / 'paths11.json').write_text(json.dumps(paths11, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Status': receipt['Status'], 'SourcePaths': 11, 'AdditionalPaths': paths[1:]}))
