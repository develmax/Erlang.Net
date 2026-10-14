from pathlib import Path
import json,hashlib,shutil,subprocess
r=Path.cwd();a=r/'artifacts';root=a/'process-info-logical-prototype';test=root/'tests/Erlang.Tests';test.mkdir(parents=True,exist_ok=True)
names='''namespace Erlang.Tests;

internal static class ProcessInfoLogicalHostNames
{
    internal const string Test = "runtime/process-info-logical-mfa-defaults-cumulative-reductions";
    internal const string Failure = "Process logical information contract failed";
    internal const int ChargedCalls = 5000;
}
'''
checks='''namespace Erlang.Tests;

internal static class ProcessInfoLogicalHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var worker = runtime.Spawn(async context =>
        {
            for (int index = 0; index < ProcessInfoLogicalHostNames.ChargedCalls; index++)
                await context.ReduceAsync();
            Term count = await Query(context, context.Self, ProcessInfoLogicalNames.Reductions);
            Require(count is TupleTerm { Items: [Atom, Integer value] } && value.Value >= ProcessInfoLogicalHostNames.ChargedCalls);
            Term defaults = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessInfoNames.Function,
                context.Self,
                Term.List(Term.A(ProcessInfoLogicalNames.Priority), Term.A(ProcessInfoLogicalNames.ErrorHandler))
            );
            Require(defaults.Equals(Term.List(
                Term.Tuple(Term.A(ProcessInfoLogicalNames.Priority), Term.A(ProcessInfoLogicalNames.NormalPriority)),
                Term.Tuple(Term.A(ProcessInfoLogicalNames.ErrorHandler), Term.A(ProcessInfoLogicalNames.DefaultErrorHandler))
            )));
            foreach (string key in new[] { ProcessInfoLogicalNames.InitialCall, ProcessInfoLogicalNames.Reductions, ProcessInfoLogicalNames.Priority, ProcessInfoLogicalNames.ErrorHandler })
            {
                try
                {
                    await Query(context, Term.I(0), key);
                    throw new InvalidOperationException(ProcessInfoLogicalHostNames.Failure);
                }
                catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                {
                }
            }

            return Nil.Value;
        });
        Require((await worker.Completion).Equals(Term.A(ProcessExitReasons.Normal)));
        var observer = runtime.Spawn(async context =>
        {
            foreach (string key in new[] { ProcessInfoLogicalNames.InitialCall, ProcessInfoLogicalNames.Reductions, ProcessInfoLogicalNames.Priority, ProcessInfoLogicalNames.ErrorHandler })
                Require((await Query(context, worker.Pid, key)).Equals(Term.A(ProcessRegistryAtoms.Undefined)));

            return Nil.Value;
        });
        Require((await observer.Completion).Equals(Term.A(ProcessExitReasons.Normal)));
    }

    private static ValueTask<Term> Query(ProcessContext context, Term pid, string key) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        ProcessInfoNames.Function,
        pid,
        Term.A(key)
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ProcessInfoLogicalHostNames.Failure);
    }
}
'''
(test/'ProcessInfoLogicalHostNames.cs').write_bytes(names.encode());(test/'ProcessInfoLogicalHostChecks.cs').write_bytes(checks.encode());program=(r/'tests/Erlang.Tests/Program.cs').read_text();program=program.replace('var tests = new List<(string Name, Func<Task> Body)>();','var tests = new List<(string Name, Func<Task> Body)>();\nTest(Erlang.Tests.ProcessInfoLogicalHostNames.Test, Erlang.Tests.ProcessInfoLogicalHostChecks.Run);',1);(test/'Program.cs').write_bytes(program.encode())
focus=root/'tools/ProcessInfoLogicalFocus';focus.mkdir(exist_ok=True);(focus/'ProcessInfoLogicalFocus.csproj').write_bytes(b'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType></PropertyGroup><ItemGroup><ProjectReference Include="../Erlang.Differential/Erlang.Differential.csproj" /><Compile Include="../../tests/Erlang.Tests/ProcessInfoLogicalHostNames.cs" Link="ProcessInfoLogicalHostNames.cs" /><Compile Include="../../tests/Erlang.Tests/ProcessInfoLogicalHostChecks.cs" Link="ProcessInfoLogicalHostChecks.cs" /></ItemGroup></Project>\n');(focus/'Program.cs').write_bytes(b'''using Erlang;
using Erlang.Compiler;
using Erlang.Differential;
using Erlang.Tests;
using System.Text.Json;
await ProcessInfoLogicalHostChecks.Run();
var rows = new List<object>();
int failures = 0;
foreach (var fixture in ProcessInfoLogicalCases.Modules)
{
    using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
    Term actual = await CompiledModuleExecution.Run(artifact, fixture.CallerContext);
    bool pass = actual.Equals(fixture.Expected);
    if (!pass) failures++;
    rows.Add(new { fixture.Name, Pass = pass, Actual = actual.ToString() });
}
Console.WriteLine(JsonSerializer.Serialize(new { Host = true, Rows = rows, Failures = failures, NativeExecuted = false }, new JsonSerializerOptions { WriteIndented = true }));
return failures == 0 ? 0 : 1;
''')
p=a/'process-info-logical-source-freeze-initial.json';x=json.loads(p.read_text());sha=lambda b:hashlib.sha256(b).hexdigest().upper()
for path in ['tests/Erlang.Tests/ProcessInfoLogicalHostNames.cs','tests/Erlang.Tests/ProcessInfoLogicalHostChecks.cs','tests/Erlang.Tests/Program.cs']:
 old=r/path;new=root/path;x['Files'].append({'Path':path,'BeforeSha256':sha(old.read_bytes()) if old.exists() else None,'BeforeSource':old.read_text() if old.exists() else None,'Sha256':sha(new.read_bytes()),'SourceContent':new.read_text()})
x['BaseHead']=subprocess.check_output(['git','rev-parse','HEAD']).decode().strip();x['Stage']='CompleteBeforeAnyTargetExecution';out=a/'process-info-logical-source-freeze-revision2.json';out.write_bytes((json.dumps(x,indent=2)+'\n').encode());print(len(x['Files']),sha(out.read_bytes()))
