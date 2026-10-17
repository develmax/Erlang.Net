from pathlib import Path
import hashlib,json

r=Path.cwd();c=r/'artifacts/spawn-priority351-corrected-candidate2'
p=c/'tests/Erlang.Priority351.Corrections';p.mkdir(exist_ok=False)
(p/'Erlang.Priority351.Corrections.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType></PropertyGroup><ItemGroup><ProjectReference Include="../Erlang.Tests/Erlang.Tests.csproj" /></ItemGroup></Project>\n''',encoding='utf-8')
(p/'Program.cs').write_text('''using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang.Differential;

bool passed = false;
string? actual = null, expected = null, error = null, source = null, generated = null;
try
{
    if (args[0].StartsWith("host:", StringComparison.Ordinal))
    {
        string[] parts = args[0].Split(':');
        var type = typeof(Erlang.Tests.PriorityPermissionHostChecks).Assembly.GetType("Erlang.Tests." + parts[1], true)!;
        var method = type.GetMethod(parts[2], BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        await ((Task)method.Invoke(null, null)!).WaitAsync(TimeSpan.FromSeconds(90));
        passed = true;
    }
    else
    {
        var fixture = CompiledModuleCases.All.Single(value => value.Name == args[0]);
        if (fixture.CompilerPolicy != CompiledModuleCompilerPolicy.Default)
            throw new InvalidOperationException("Only default compiler policy accepted by this focus runner");
        source = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fixture.Source)));
        using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
        generated = artifact.CSharpSha256;
        var result = await CompiledModuleExecution.Run(artifact, fixture.CallerContext).WaitAsync(TimeSpan.FromSeconds(40));
        actual = result.ToString();
        expected = fixture.Expected.ToString();
        passed = result.Equals(fixture.Expected);
    }
}
catch (Exception exception)
{
    error = exception.ToString();
}
File.WriteAllText(args[1], JsonSerializer.Serialize(new
{
    Name = args[0], Passed = passed, Actual = actual, Expected = expected, Error = error,
    SourceSha256 = source, GeneratedSha256 = generated,
    Loaded = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && a.GetName().Name!.StartsWith("Erlang.")).Select(a => new { a.Location, Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(a.Location))) })
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(passed ? "PASS " + args[0] : "FAIL " + args[0] + ": " + (error ?? actual));
return passed ? 0 : 1;
''',encoding='utf-8')
hosts=[('HaltLifecycleHostChecks','Run'),('PriorityPermissionHostChecks','Run'),('RuntimeActorDispatchHostChecks','Run'),('SpawnAsyncDistributionHostChecks','RunDeadParent'),('SpawnPriorityAdmissionHostChecks','AllReadyPriorityOrder'),('SpawnPriorityGuestReentryHostChecks','EscapedCallbackMustReadmitOwner'),('SpawnPrioritySynchronousBorrowHostChecks','SameTurnCallbackStaysSynchronous'),('SpawnPriorityEpochHostChecks','LowPaysOncePerLogicalReduction'),('SpawnPriorityWaitingHostChecks','WaitingHighDoesNotHoldCapacity'),('SpawnPriorityQueuedChangeHostChecks','ReprioritizesAlreadyQueuedOwner'),('SpawnPriorityRetirementHostChecks','HaltSettlesEscapedCallbackWithoutInvokingTarget'),('SpawnPriorityRetirementHostChecks','KilledOwnerStillSettlesQueuedCleanup')]
tests=json.loads((r/'artifacts/spawn-priority351-full-actual1/tests.json').read_bytes())['Tests']
compiled=[row['Name'].removeprefix('oracle/compiled-module/') for row in tests if row['Name'].startswith(('oracle/compiled-module/erlang-port-native-option-','oracle/compiled-module/erlang-port-pressure-','oracle/compiled-module/spawn-priority-'))]
targets=['host:'+name+':'+method for name,method in hosts]+compiled
assert len(targets)==len(set(targets))
o=r/'artifacts/spawn-priority351-corrected-preparation'
(o/'targets.json').write_text(json.dumps({'Targets':targets},indent=2)+'\n',encoding='utf-8')
paths=[c/row['Path'] for row in json.loads((r/'artifacts/spawn-priority351-full-source2/source-resource-closure.json').read_bytes())['Files']]+list(p.glob('*'))
rows=[{'Path':q.relative_to(c).as_posix(),'Bytes':q.stat().st_size,'Sha256':hashlib.sha256(q.read_bytes()).hexdigest().upper()} for q in paths]
(o/'focus-source-freeze.json').write_text(json.dumps({'Candidate':str(c),'Files':rows,'TargetsSha256':hashlib.sha256((o/'targets.json').read_bytes()).hexdigest().upper()},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Targets':len(targets),'Frozen':len(rows)}))
