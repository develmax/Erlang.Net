using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamStaticListClosureMappingChecks
{
    internal const string TestName = "compiler/beam/calls-proven-closed-list-mapper-directly";
    private const string Fixture = "beam_static_list_closure_mapping.erl";
    private const string WrongLayout = "Only the proven closed mapper should call its lifted function directly.";
    private const string WrongValue = "The list mapper changed its result or improper generator failure.";
    private const string Source = """
        -module(beam_static_list_closure_mapping).
        -export([known/1, captured/2, unknown/2, separate_line/1]).
        known(Inputs) ->
            F = fun(X) -> case X of
                Pair = {19,_} -> {1,Pair};
                Pair = {20,_} -> {2,Pair};
                Other -> {3,Other}
            end end,
            [F(Value) || Value <- Inputs].
        captured(Inputs, Prefix) ->
            F = fun(Value) -> {Prefix,Value} end,
            [F(Value) || Value <- Inputs].
        unknown(Inputs, F) ->
            [F(Value) || Value <- Inputs].
        separate_line(Inputs) ->
            F = fun(Value) -> {same,Value} end,
            [F(Value)
             || Value <- Inputs].
        """;

    private const string StructuralSource = """
        -module(beam_structural_closure_generators).
        -export([tuple_branch/1, cons_branch/2]).

        tuple_branch({entries, Rows}) ->
            Pick = fun({row, Value}) -> Value; (Value) -> {other, Value} end,
            [Pick(Row) || Row <- Rows];
        tuple_branch({status, Value}) ->
            {status, Value};
        tuple_branch(Value) ->
            {unknown, Value}.

        cons_branch([], _Tag) ->
            [];
        cons_branch([_|_]=Rows, Tag) ->
            Attach = fun(Row) -> {Tag, Row} end,
            [Attach(Row) || Row <- Rows].
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        var module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term payload = Term.A("payload");
            Term input = Term.Tuple(Term.I(19), payload);
            Term expected = Term.List(Term.Tuple(Term.I(1), input), Term.Tuple(Term.I(3), payload));
            if (!expected.Equals(await module.Invoke(context, "known", Term.List(input, payload)))
                || !Nil.Value.Equals(await module.Invoke(context, "known", Nil.Value))
                || !Term.List(Term.Tuple(payload, input)).Equals(await module.Invoke(
                    context,
                    "captured",
                    Term.List(input),
                    payload
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                await module.Invoke(context, "known", new Cons(input, payload));
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("bad_generator"), payload)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);
        });

        if (!UsesMappingCall(image, "known", direct: true)
            || !UsesMappingCall(image, "captured", direct: false) || !UsesMappingCall(image, "unknown", direct: false)
            || !UsesMappingCall(image, "separate_line", direct: false))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        await VerifyStructuralBranches();
    }

    private static async Task VerifyStructuralBranches()
    {
        const string structuralFixture = "beam_structural_closure_generators.erl";
        byte[] image = CodeGeneration.CompileBeamModule(StructuralSource, structuralFixture);
        string captureLaterArm = StructuralSource.Replace(
            "(Value) -> {other, Value} end",
            "(Value) -> {other, Rows, Value} end",
            StringComparison.Ordinal
        );
        byte[] captured = CodeGeneration.CompileBeamModule(captureLaterArm, structuralFixture);
        object code = Code(image);
        var functions = Items(code, "Functions")
            .OrderBy(function => (int)Property(function, "FirstInstructionIndex"))
            .Select(function => (Name: (string)Property(function, "Name"), Arity: (int)Property(function, "Arity")))
            .Where(function => function.Name.StartsWith("-", StringComparison.Ordinal));
        if (!functions.SequenceEqual([
                ("-cons_branch/2-lc$^1/1-0-", 2),
                ("-cons_branch/2-fun-0-", 2),
                ("-tuple_branch/1-lc$^1/1-0-", 2),
                ("-tuple_branch/1-fun-0-", 1)
            ])
            || !UsesMappingCall(image, "tuple_branch", direct: true)
            || !UsesMappingCall(image, "cons_branch", direct: false)
            || !UsesMappingCall(captured, "tuple_branch", direct: false))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term last = Term.A("last");
            Term tag = Term.A("tag");
            Term input = Term.List(Term.Tuple(Term.A("row"), first), last);
            Term entries = Term.Tuple(Term.A("entries"), input);
            Term untouched = Term.Tuple(Term.A("status"), last);
            if (!Term.List(first, Term.Tuple(Term.A("other"), last)).Equals(await module.Invoke(context, "tuple_branch", entries))
                || !untouched.Equals(await module.Invoke(context, "tuple_branch", untouched))
                || !Nil.Value.Equals(await module.Invoke(
                    context,
                    "cons_branch",
                    Nil.Value,
                    tag
                ))
                || !Term.List(Term.Tuple(tag, first), Term.Tuple(tag, last)).Equals(await module.Invoke(
                    context,
                    "cons_branch",
                    Term.List(first, last),
                    tag
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                await module.Invoke(
                    context,
                    "cons_branch",
                    new Cons(first, last),
                    tag
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("bad_generator"), last)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);
        });
    }

    private static bool UsesMappingCall(byte[] image, string parent, bool direct)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)].StartsWith($"-{parent}/", StringComparison.Ordinal)
                && atoms[Value(name)].Contains("-lc$", StringComparison.Ordinal) && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[] operations = instructions.Skip(start + 1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo).Select(Opcode).ToArray();

        return direct
            ? operations.Count(op => op == LocalCall) == 2 && operations.Contains(TrimFrame) && !operations.Contains(CallFun)
            : operations.Contains(CallFun) || operations.Contains(CallFunTyped);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
