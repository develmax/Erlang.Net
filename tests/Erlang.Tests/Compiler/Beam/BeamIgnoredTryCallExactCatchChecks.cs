using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamIgnoredTryCallExactCatchChecks
{
    internal const string TestName = "compiler/beam/preserves-exact-catch-inputs-and-boolean-defaults";
    private const string Fixture = "beam_ignored_try_call_exact_catch_probe.erl";
    private const string IncorrectCatch = "The ignored result and two exact catch operands did not stay in their entry registers.";
    private const string IncorrectValue = "The default shortcut, branch value or reraised exception changed.";
    private const string Source = """
        -module(beam_ignored_try_call_exact_catch_probe).
        -export([choice/1, guarded/1, atoms/1, reason_only/1]).
        choice(default) ->
            true;
        choice(Value) ->
            try read(Value) of
                _ -> true
            catch
                throw:false -> false
            end.
        guarded(default) ->
            true;
        guarded(Value) when Value =:= 20 ->
            true;
        guarded(_) ->
            false.
        atoms(default) ->
            true;
        atoms(Value) when Value =:= ready ->
            true;
        atoms(_) ->
            false.
        reason_only(Value) ->
            try read(Value) of
                accepted -> admitted
            catch
                _:Reason -> {caught, Reason}
            end.
        read(accepted) -> accepted;
        read(known) -> found;
        read(reject) -> throw(false);
        read(other_throw) -> throw(rejected);
        read(fault) -> erlang:error(selected_error);
        read(stop) -> exit(selected_exit).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] choice = Function("choice");
        object[] guarded = Function("guarded");
        object[] atomSelector = Function("atoms");
        object[] reasonBody = Function("reason_only");
        int handler = Array.FindIndex(reasonBody, instruction => Opcode(instruction) == TryCase);
        if (handler < 0 || reasonBody.Length <= handler + 2
            || Opcode(reasonBody[handler + 1]) != TestHeap
            || Items(reasonBody[handler + 1], "Operands").ToArray() is not [var words, var roots]
            || Value(words) != 3 || Value(roots) != 2
            || Opcode(reasonBody[handler + 2]) != PutTuple
            || Items(reasonBody[handler + 2], "Operands").ToArray() is not [_, _, _, var input]
            || (string)Property(input, "Kind") != "x" || Value(input) != 1)
        {
            throw new InvalidOperationException(IncorrectCatch);
        }

        object[] frames = choice.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        if (frames is not [var frame] || Items(frame, "Operands").ToArray() is not [var slots, var live]
            || Value(slots) != 1 || Value(live) != 1
            || choice.Count(instruction => Opcode(instruction) == TryCase) != 1
            || choice.Count(instruction => Opcode(instruction) == Raise) != 1
            || choice.Any(instruction => Opcode(instruction) == InitYRegisters)
            || guarded.Count(instruction => Opcode(instruction) == ExactEqualTest) != 1
            || guarded.Count(instruction => Opcode(instruction) == BifTwo) != 1
            || atomSelector.Count(instruction => Opcode(instruction) == SelectValue) != 1)
        {
            throw new InvalidOperationException(IncorrectCatch);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term yes = Term.A(ErlangBooleanAtoms.True);
            Term no = Term.A(ErlangBooleanAtoms.False);
            if (!yes.Equals(await module.Invoke(context, "choice", Term.A("default")))
                || !yes.Equals(await module.Invoke(context, "choice", Term.A("known")))
                || !no.Equals(await module.Invoke(context, "choice", Term.A("reject")))
                || !yes.Equals(await module.Invoke(context, "guarded", Term.I(20)))
                || !no.Equals(await module.Invoke(context, "guarded", new FloatTerm(20.0))))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            await ExpectError("other_throw", ErlangExceptionClasses.Throw, "rejected");
            await ExpectError("fault", ErlangExceptionClasses.Error, "selected_error");
            await ExpectError("stop", ErlangExceptionClasses.Exit, "selected_exit");
            if (!Term.A("admitted").Equals(await module.Invoke(context, "reason_only", Term.A("accepted")))
                || !Term.Tuple(Term.A("caught"), no).Equals(await module.Invoke(context, "reason_only", Term.A("reject")))
                || !Term.Tuple(Term.A("caught"), Term.A("selected_error")).Equals(await module.Invoke(context, "reason_only", Term.A("fault")))
                || !Term.Tuple(Term.A("caught"), Term.A("selected_exit")).Equals(await module.Invoke(context, "reason_only", Term.A("stop"))))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                _ = await module.Invoke(context, "reason_only", Term.A("known"));
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("try_clause"), Term.A("found"))))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(IncorrectValue);

            async Task ExpectError(string input, string kind, string reason)
            {
                try
                {
                    _ = await module.Invoke(context, "choice", Term.A(input));
                }
                catch (ErlangException error) when (error.ExceptionClass == kind && error.Reason.Equals(Term.A(reason)))
                {
                    return;
                }

                throw new InvalidOperationException(IncorrectValue);
            }
        });

        object[] Function(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                    && atoms[Value(atom)] == name && Value(arity) == 1
            );
            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectCatch);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
