using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using Erlang.Compiler;
using Erlang.Differential;
using MfaContractVerification;

if (args.Length != 3 || args[0] is not (VerificationStages.Parse or VerificationStages.Sdk))
{
    return 2;
}
string stage = args[0];
string request = Path.GetFullPath(args[1]);
string output = Path.GetFullPath(args[2]);
if (Directory.Exists(output))
{
    return 2;
}
Directory.CreateDirectory(output);
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
var fixtures = JsonSerializer.Deserialize<ContractFixture[]>(await File.ReadAllTextAsync(request), options)!;
var results = new List<object>();
int failed = 0;
foreach (var fixture in fixtures)
{
    string directory = Path.Combine(output, fixture.Id);
    Directory.CreateDirectory(directory);
    try
    {
        if (Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(fixture.SourcePath))) != fixture.SourceSha256)
        {
            throw new InvalidOperationException(VerificationDiagnostics.SourceHashMismatch + fixture.Id);
        }
        string source = await File.ReadAllTextAsync(fixture.SourcePath);
        if (stage == VerificationStages.Parse)
        {
            var parsed = new Parser(source).ParseModule();
            if (parsed.Name != fixture.Module)
            {
                throw new InvalidOperationException(VerificationDiagnostics.DeclaredModuleDiffers);
            }
            results.Add(new { fixture.Id, Status = VerificationNames.Passed, fixture.SourceSha256, fixture.DeclaredSourceFile });
            continue;
        }
        if (fixture.CompilerPolicy != ContractCompilerPolicies.Default)
        {
            throw new InvalidOperationException(VerificationDiagnostics.UnsupportedCompilerPolicy);
        }
        using var compiled = GeneratedModuleCompiler.Compile(source, fixture.DeclaredSourceFile);
        if (compiled.Definition.Name != fixture.Module)
        {
            throw new InvalidOperationException(VerificationDiagnostics.DeclaredModuleDiffers);
        }
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        var completed = new TaskCompletionSource<(Term Raw, Term Protocol)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var process = runtime.Spawn(async context =>
        {
            context.TrapExits = fixture.TrapExits;
            try
            {
                var arguments = new List<Term>();
                foreach (string text in fixture.Arguments)
                {
                    arguments.Add(await Execution.EvaluateAsync(new Parser(text).ParseExpression(), context));
                }
                Term raw, protocol;
                try
                {
                    Term value = await Execution.InvokeAsync(
                        compiled.Definition,
                        fixture.EntryPoint,
                        context,
                        arguments
                    );
                    protocol = Term.Tuple(Term.A(VerificationNames.Ok), value);
                    raw = Term.Tuple(Term.A(VerificationNames.Returned), value);
                }
                catch (ErlangException error)
                {
                    protocol = Term.Tuple(Term.A(VerificationNames.Error), Term.A(error.ExceptionClass), error.Reason);
                    raw = Term.Tuple(
                        Term.A(VerificationNames.Raised),
                        Term.A(error.ExceptionClass),
                        error.Reason,
                        error.StackTraceTerm
                    );
                }
                var effects = new List<Term>();
                foreach (string text in fixture.EffectKeys)
                {
                    Term key = await Execution.EvaluateAsync(new Parser(text).ParseExpression(), context);
                    effects.Add(Term.Tuple(key, await runtime.Modules.Call(
                        context,
                        VerificationNames.Erlang,
                        VerificationNames.Get,
                        key
                    )));
                }
                completed.TrySetResult((Term.Tuple(raw, Cons.From(effects)), protocol));
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        var captured = await completed.Task.WaitAsync(TimeSpan.FromSeconds(fixture.DeadlineSeconds));
        await process.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        await File.WriteAllBytesAsync(Path.Combine(directory, VerificationFiles.RawTerm), ExternalTermFormat.Encode(captured.Raw));
        await File.WriteAllBytesAsync(Path.Combine(directory, VerificationFiles.ProtocolTerm), ExternalTermFormat.Encode(captured.Protocol));
        bool expected = true;
        if (fixture.ExpectedProtocolPath is not null)
        {
            byte[] bytes = await File.ReadAllBytesAsync(fixture.ExpectedProtocolPath);
            if (Convert.ToHexString(SHA256.HashData(bytes)) != fixture.ExpectedProtocolSha256)
            {
                throw new InvalidOperationException(VerificationDiagnostics.ExpectedProtocolHashMismatch);
            }
            expected = captured.Protocol.Equals(ExternalTermFormat.Decode(bytes));
        }
        if (!expected)
        {
            failed++;
        }
        results.Add(new { fixture.Id, Status = expected ? VerificationNames.Passed : VerificationNames.Difference, HasExpectedProtocol = fixture.ExpectedProtocolPath is not null, compiled.CSharpSha256, fixture.SourceSha256 });
    }
    catch (TimeoutException error)
    {
        failed++;
        await File.WriteAllTextAsync(Path.Combine(directory, VerificationFiles.Failure), error.ToString());
        results.Add(new { fixture.Id, Status = VerificationNames.Timeout, Error = error.ToString() });
    }
    catch (Exception error)
    {
        failed++;
        await File.WriteAllTextAsync(Path.Combine(directory, VerificationFiles.Failure), error.ToString());
        results.Add(new { fixture.Id, Status = VerificationNames.Blocker, Error = error.ToString() });
    }
}
await File.WriteAllTextAsync(
    Path.Combine(output, VerificationFiles.Results),
    JsonSerializer.Serialize(new { Stage = stage, Total = fixtures.Length, Failed = failed, Results = results }, options)
);
return failed == 0 ? 0 : 1;
