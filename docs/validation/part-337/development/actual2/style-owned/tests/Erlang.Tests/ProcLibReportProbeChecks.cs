using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Erlang.Differential;
using Erlang.Otp;

namespace Erlang.Tests;

public static class ProcLibReportProbeChecks
{
    public static async Task CaptureReports()
    {
        foreach (var probe in ProcLibReportProbeData.Reports)
        {
            try
            {
                new Erlang.Compiler.Parser(probe.Source).ParseModule();
            }
            catch (Exception failure)
            {
                throw new InvalidOperationException(probe.Name, failure);
            }
        }
        foreach (var probe in ProcLibReportProbeData.Reports)
        {
            using var artifact = GeneratedModuleCompiler.Compile(probe.Source);
            Term actual = await CompiledModuleExecution.Run(artifact);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                probe.Name,
                Role = ProcLibReportProbeNames.SdkObservation,
                Actual = actual.ToString(),
                RawEtfBase64 = Convert.ToBase64String(ExternalTermFormat.Encode(actual)),
                ComparedWithOriginal = false
            }));
        }
    }

    public static async Task PrettySourceContracts()
    {
        foreach (var probe in ProcLibReportProbeData.Pretty)
            new Erlang.Compiler.Parser(probe.Source).ParseModule();

        await using var runtime = new ProcessRuntime();
        Type printerType = typeof(GenServer).Assembly.GetType(ProcLibReportProbeNames.PrettyPrinterType, true)!;
        object printer = Activator.CreateInstance(printerType, runtime)!;
        foreach (var probe in ProcLibReportProbeData.Pretty)
        {
            using var artifact = GeneratedModuleCompiler.Compile(probe.Source);
            Term result = await CompiledModuleExecution.Run(artifact);
            if (result is not TupleTerm { Items: [Atom { Name: ProcLibReportProbeNames.Success }, var term] })
                throw new InvalidOperationException($"{probe.Name}: {result}");

            var arguments = probe.Mode == ProcLibReportProbeNames.Write
                ? new object[] { term, Term.A(probe.Encoding) }
                : new object[] { term, Term.I(probe.Depth), Term.A(probe.Encoding), Term.I(probe.Limit), probe.Width, probe.Column };
            var actual = (Term)Invoke(printerType.GetMethod(probe.Mode)!, printer, arguments)!;
            string text = Flatten(actual);
            Console.WriteLine(JsonSerializer.Serialize(new { probe.Name, ExpectedFromPinnedSource = probe.Expected, Actual = text, Passed = text == probe.Expected, OriginalExecuted = false }));
            if (text != probe.Expected)
                throw new InvalidOperationException($"{probe.Name}: expected={probe.Expected}; actual={text}");
        }
    }

    public static async Task CapturePrimitives()
    {
        foreach (var probe in ProcLibReportProbeData.Primitives)
            new Erlang.Compiler.Parser(probe.Source).ParseModule();

        await using var runtime = new ProcessRuntime();
        var assembly = typeof(GenServer).Assembly;
        Type printerType = assembly.GetType(ProcLibReportProbeNames.PrettyPrinterType, true)!;
        object printer = Activator.CreateInstance(printerType, runtime)!;
        Type controlsType = assembly.GetType(ProcLibReportProbeNames.ControlsType, true)!;
        object controls = Activator.CreateInstance(controlsType, printer)!;
        Type charactersType = assembly.GetType(ProcLibReportProbeNames.CharactersType, true)!;
        foreach (var probe in ProcLibReportProbeData.Primitives)
        {
            using var artifact = GeneratedModuleCompiler.Compile(probe.Source);
            Term operand = await CompiledModuleExecution.Run(artifact);
            if (operand is not TupleTerm { Items: [Atom { Name: ProcLibReportProbeNames.Success }, var input] })
                throw new InvalidOperationException($"{probe.Name}: {operand}");

            Term observation;
            try
            {
                Term value;
                if (probe.Kind == ProcLibReportProbeNames.FormatKind)
                {
                    var tuple = (TupleTerm)input;
                    Term limit = Term.A(ProcLibReportProbeNames.Unlimited);
                    if (tuple.Items.Count == 3)
                        limit = Cons.Items(tuple.Items[2]).Cast<TupleTerm>().Single(item => item.Items[0].Equals(Term.A(ProcLibReportProbeNames.CharsLimit))).Items[1];
                    value = (Term)Invoke(
                        controlsType.GetMethod(ProcLibReportProbeNames.FormatMethod)!,
                        controls,
                        [Term.Text(tuple.Items[0]), Cons.Items(tuple.Items[1]).ToArray(), limit]
                    )!;
                }
                else
                {
                    string encoding = probe.Kind == ProcLibReportProbeNames.MeasureLatin1
                        ? ProcLibReportProbeNames.Latin1 : ProcLibReportProbeNames.Unicode;
                    var measured = (System.Numerics.BigInteger)Invoke(charactersType.GetMethod(ProcLibReportProbeNames.Measure)!, null, [input, Term.A(encoding)])!;
                    value = new Integer(measured);
                }
                observation = Term.Tuple(Term.A(ProcLibReportProbeNames.Value), value);
            }
            catch (ErlangException failure)
            {
                observation = Term.Tuple(Term.A(ProcLibReportProbeNames.Exception), Term.A(failure.ExceptionClass), failure.Reason);
            }
            Term actual = Term.Tuple(Term.A(ProcLibReportProbeNames.Success), observation);
            Console.WriteLine(JsonSerializer.Serialize(new { probe.Name, Role = ProcLibReportProbeNames.SdkObservation, Actual = actual.ToString(), RawEtfBase64 = Convert.ToBase64String(ExternalTermFormat.Encode(actual)), ComparedWithOriginal = false }));
        }
    }

    public static async Task CallbackAwaitControl()
    {
        await using var runtime = new ProcessRuntime();
        OtpModules.Register(runtime.Modules);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stack = Term.List(Term.Tuple(
            Term.A(ProcLibReportProbeNames.CallbackModule),
            Term.A(ProcLibReportProbeNames.CallbackFunction),
            Term.I(0),
            Term.List(Term.Tuple(
                Term.A(ProcLibReportProbeNames.ErrorInfo),
                new MapTerm([
                new(Term.A(ProcLibReportProbeNames.Module), Term.A(ProcLibReportProbeNames.CallbackModule))
            ])
            ))
        ));
        runtime.Modules.Register(
            ProcLibReportProbeNames.CallbackModule,
            ProcLibReportProbeNames.FormatError,
            2,
            async (context, arguments) =>
            {
                if (!arguments[0].Equals(Term.A(ProcLibReportProbeNames.Reason)) || !arguments[1].Equals(stack))
                    throw new InvalidOperationException(ProcLibReportProbeNames.CallbackMismatch);
                entered.SetResult();
                await released.Task.WaitAsync(ProcLibReportProbeNames.Deadline);

                return new MapTerm([new(Term.A(ProcLibReportProbeNames.ReasonKey), Term.String(ProcLibReportProbeNames.Replacement))]);
            }
        );
        Term report = Term.List(
            Term.List(Term.Tuple(
                Term.A(ProcLibReportProbeNames.ErrorInfo),
                Term.Tuple(Term.A(ErlangExceptionClasses.Error), Term.A(ProcLibReportProbeNames.Reason), stack)
            )),
            Nil.Value
        );
        Term? returned = null;
        var caller = runtime.Spawn(async context =>
        {
            returned = await runtime.Modules.Call(
                context,
                ProcLibReportProbeNames.ProcLib,
                ProcLibReportProbeNames.Format,
                report
            );

            return returned;
        });
        try
        {
            await entered.Task.WaitAsync(ProcLibReportProbeNames.Deadline);
            if (caller.Completion.IsCompleted)
                throw new InvalidOperationException(ProcLibReportProbeNames.CallbackNotSuspended);
        }
        finally
        {
            released.TrySetResult();
        }
        Term reason = await caller.Completion.WaitAsync(ProcLibReportProbeNames.Deadline);
        if (!reason.Equals(Term.A(ProcessExitReasons.Normal)) || returned is null ||
            !Flatten(returned).Contains(ProcLibReportProbeNames.Replacement, StringComparison.Ordinal))
            throw new InvalidOperationException(ProcLibReportProbeNames.CallbackMismatch);

        Console.WriteLine(JsonSerializer.Serialize(new { Role = ProcLibReportProbeNames.CallbackAwait, Passed = true, ActualIncompleteBeforeRelease = true, OriginalExecuted = false }));
    }

    private static string Flatten(Term term)
    {
        Type characters = typeof(GenServer).Assembly.GetType(ProcLibReportProbeNames.CharactersType, true)!;

        return (string)Invoke(characters.GetMethod(ProcLibReportProbeNames.FlattenUnicode)!, null, [term])!;
    }

    private static object? Invoke(MethodInfo method, object? instance, object[] arguments)
    {
        try
        {
            return method.Invoke(instance, arguments);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(failure.InnerException).Throw();
            throw;
        }
    }
}
