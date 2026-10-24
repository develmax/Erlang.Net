using Erlang;
using static Erlang.Tests.BeamCatchProtocolControlNames;

namespace Erlang.Tests;

internal static class BeamCatchProtocolControlHostChecks
{
    internal static Task Refusals()
    {
        foreach (BeamCatchProtocolFault fault in Enum.GetValues<BeamCatchProtocolFault>())
        {
            bool refused = false;
            try
            {
                refused = !BeamModule.Prepare(BeamCatchProtocolControlBytes.Negative(fault))
                    .Supports(BeamOpcodeEdgeNames.Run, 1);
            }
            catch (Exception error) when (error is InvalidDataException or NotSupportedException)
            {
                refused = true;
            }
            Require(refused, fault.ToString());
        }

        return Task.CompletedTask;
    }

    internal static async Task NormalAndExceptional()
    {
        foreach (bool trim in new[] { false, true })
            foreach (bool readBlank in new[] { false, true })
            {
                BeamModule module = BeamModule.Prepare(BeamCatchProtocolControlBytes.Positive(trim, readBlank));
                Require(module.Supports(BeamOpcodeEdgeNames.Run, 1), "positive-admission");
                await using var runtime = new ProcessRuntime();
                await using var actor = new RuntimeNodeFoundationHostActor(runtime);
                Term payload = Term.Tuple(Term.A(Payload), Term.List(Term.I(1), Term.I(2)));
                Term trace = Term.List(Term.Tuple(
                    Term.A(BeamOpcodeEdgeNames.Module),
                    Term.A(BeamOpcodeEdgeNames.Run),
                    Term.I(1),
                    Nil.Value
                ));
                foreach (string? exceptionClass in new string?[] { null, ErlangExceptionClasses.Throw, ErlangExceptionClasses.Exit, ErlangExceptionClasses.Error })
                {
                    int entered = 0;
                    var callback = new FunctionTerm(
                        0,
                        async (_, _) =>
                    {
                        entered++;
                        await Task.Yield();
                        if (exceptionClass is not null)
                            throw new ErlangException(payload, exceptionClass)
                            {
                                ExplicitStackTrace = exceptionClass == ErlangExceptionClasses.Error,
                                StackTraceTerm = exceptionClass == ErlangExceptionClasses.Error ? trace : Nil.Value
                            };

                        return payload;
                    }
                    );
                    Term result = await actor.InvokeAsync(async context =>
                        await module.Invoke(context, BeamOpcodeEdgeNames.Run, callback)).WaitAsync(Deadline);
                    Term expected = exceptionClass == ErlangExceptionClasses.Error
                        ? Term.Tuple(Term.A(Exit), Term.Tuple(payload, trace))
                        : exceptionClass == ErlangExceptionClasses.Exit ? Term.Tuple(Term.A(Exit), payload) : payload;
                    Require(result.Equals(readBlank ? Nil.Value : expected) && entered == 1, "whole-result-blank-and-once");
                }
            }
    }

    private static void Require(bool condition, string branch)
    {
        if (!condition) throw new InvalidOperationException(Failure + ": " + branch);
    }
}
