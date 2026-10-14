import hashlib
import json
from pathlib import Path
import tarfile

repo = Path(__file__).resolve().parent.parent
artifacts = repo / 'artifacts'
original_path = artifacts / 'distribution-pending-source-freeze.json'
original_bytes = original_path.read_bytes()
assert hashlib.sha256(original_bytes).hexdigest() == 'bef32838144b64c7610297264eb90bad74b8a107d8fa91a42583526895d72ff2'
original = json.loads(original_bytes)
candidate = artifacts / 'distribution-pending-promotion-prototype'
original_files = {item['Path']: item for item in original['OwnedPaths']}
owned = []
for name, item in original_files.items():
    if not name.startswith('src/Erlang.Runtime/'):
        continue
    content = item['NewSource'].encode('utf-8')
    assert hashlib.sha256(content).hexdigest() == item['NewSha256']
    (candidate / name).write_bytes(content)
    owned.append(name)
assert len(owned) == 15
for name in ['DistributionPendingChecks', 'DistributionPendingTestNames']:
    previous_name = f'tools/Erlang.DistributionPendingFocus/{name}.cs'
    source = original_files[previous_name]['NewSource']
    assert 'namespace Erlang.Tests;' in source
    if name == 'DistributionPendingTestNames':
        source = source.replace('    internal const string FirstNode =', '    internal const string Test = "runtime/distribution-pending-owned-resource-generation-kernel-controller-lifecycle";\n    internal const string FirstNode =', 1)
    destination = f'tests/Erlang.Tests/{name}.cs'
    (candidate / destination).write_text(source, encoding='utf-8', newline='\n')
    owned.append(destination)
program_name = 'tests/Erlang.Tests/Program.cs'
with tarfile.open(artifacts / 'distribution-pending-seed.tar') as archive:
    program = archive.extractfile(program_name).read().decode('utf-8')
assert program.count('\nint failed = 0;\n') == 1
program = program.replace('\nint failed = 0;\n', '\nTest(Erlang.Tests.DistributionPendingTestNames.Test, Erlang.Tests.DistributionPendingChecks.Run);\n\nint failed = 0;\n', 1)
(candidate / program_name).write_text(program, encoding='utf-8', newline='\n')
owned.append(program_name)
focus = candidate / 'tools/Erlang.DistributionPendingPromotionFocus'
focus.mkdir(parents=True, exist_ok=True)
(focus / 'Program.cs').write_text('''using System.Globalization;
using System.Reflection;
using System.Text.Json;

var assembly = typeof(Erlang.Tests.BeamLoaderLifecycleChecks).Assembly;
var deadline = TimeSpan.FromSeconds(int.Parse(args[0], CultureInfo.InvariantCulture));
var controls = new List<object>();
foreach (string name in args.Skip(1))
{
    var type = assembly.GetType(name, true)!;
    var method = type.GetMethod(
        nameof(Erlang.Tests.BeamLoaderLifecycleChecks.Run),
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
    )
        ?? throw new MissingMethodException(name, nameof(Erlang.Tests.BeamLoaderLifecycleChecks.Run));
    var task = (Task)method.Invoke(null, null)!;
    await task.WaitAsync(deadline);
    controls.Add(new { Name = name, Passed = true });
}
Console.WriteLine(JsonSerializer.Serialize(new
{
    Passed = true,
    NativeOtpExecutions = 0,
    CoreRegistrationsAdded = 0,
    Controls = controls
}));
''', encoding='utf-8', newline='\n')
(focus / 'Erlang.DistributionPendingPromotionFocus.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>
  <ItemGroup><ProjectReference Include="../../tests/Erlang.Tests/Erlang.Tests.csproj" /></ItemGroup>
</Project>
''', encoding='utf-8', newline='\n')
auxiliary = ['tools/Erlang.DistributionPendingPromotionFocus/Program.cs', 'tools/Erlang.DistributionPendingPromotionFocus/Erlang.DistributionPendingPromotionFocus.csproj']
baseline = {}
with tarfile.open(artifacts / 'distribution-pending-seed.tar') as archive:
    for entry in archive.getmembers():
        if entry.isfile():
            baseline[entry.name] = archive.extractfile(entry).read()
source_entries = []
unchanged = 0
resources = []
for name, previous in baseline.items():
    current = (candidate / name).read_bytes()
    if name in owned:
        source_entries.append({'Path': name, 'OldSource': previous.decode('utf-8'), 'NewSource': current.decode('utf-8'), 'OldSha256': hashlib.sha256(previous).hexdigest(), 'NewSha256': hashlib.sha256(current).hexdigest(), 'ImportIntoMain': True})
    else:
        assert current == previous, name
        unchanged += 1
    if name.startswith('tests/Erlang.Tests/Resources/'):
        resources.append({'Path': name, 'Sha256': hashlib.sha256(previous).hexdigest()})
for name in sorted(set(owned) - set(baseline)):
    current = (candidate / name).read_bytes()
    item = {'Path': name, 'OldSource': None, 'NewSource': current.decode('utf-8'), 'NewSha256': hashlib.sha256(current).hexdigest(), 'ImportIntoMain': True}
    if name.startswith('tests/'):
        previous_name = 'tools/Erlang.DistributionPendingFocus/' + Path(name).name
        item['PreviousIsolatedPath'] = previous_name
        item['PreviousIsolatedSource'] = original_files[previous_name]['NewSource']
        item['PreviousIsolatedSha256'] = original_files[previous_name]['NewSha256']
    source_entries.append(item)
for name in auxiliary:
    content = (candidate / name).read_bytes()
    source_entries.append({'Path': name, 'OldSource': None, 'NewSource': content.decode('utf-8'), 'NewSha256': hashlib.sha256(content).hexdigest(), 'ImportIntoMain': False})
assert len(source_entries) == 20
for item in source_entries:
    if item['Path'].startswith('src/'):
        assert item['NewSha256'] == original_files[item['Path']]['NewSha256']
report = json.loads((candidate / 'docs/validation/part-267/tests.json').read_text(encoding='utf-8'))
assert report['Passed'] == 6484 and report['Failed'] == 0
evidence = []
for name in ['distribution_pending_probe.erl', 'distribution-pending-native-declaration.json', 'distribution-pending-scope.md']:
    path = 'artifacts/' + name
    evidence.append({'Path': path, 'NewSource': original_files[path]['NewSource'], 'Sha256': original_files[path]['NewSha256'], 'ImportIntoMainProduction': False, 'NativeExecuted': False})
result = {'Seed': original['Seed'], 'Candidate': str(candidate), 'OriginalRuntimeSourceFreezeSha256': hashlib.sha256(original_bytes).hexdigest(), 'ProductionAndPermanentTestImportCount': 18, 'AuxiliarySourceCount': 2, 'Sources': sorted(source_entries, key=lambda item: item['Path']), 'NativeScopeArtifacts': evidence, 'UnchangedOtherSeedFiles': unchanged, 'Resources': resources, 'OldPassedTestCount': 6484, 'OldTestNames': [item['Name'] for item in report['Tests']], 'AddedPermanentTestName': 'runtime/distribution-pending-owned-resource-generation-kernel-controller-lifecycle', 'ExpectedTotalAfterMainFullNotAnObservedResult': 6485, 'Builds': 0, 'NativeOtpExecutions': 0, 'CoreRegistrationsAdded': 0, 'Original15RuntimePathsExact': True, 'ActualFullSuiteGate': 'Root after Main import; not yet run', 'InheritedScope': 'pending identity/runtime hooks/setup reservations, not true local kernel admission/connected channel/native APIs'}
output = artifacts / 'distribution-pending-promotion-source-freeze.json'
output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'Path': str(output), 'Sha256': hashlib.sha256(output.read_bytes()).hexdigest(), 'ImportPaths': 18, 'AuxiliaryPaths': 2, 'UnchangedSeedFiles': unchanged, 'Resources': len(resources)}))
