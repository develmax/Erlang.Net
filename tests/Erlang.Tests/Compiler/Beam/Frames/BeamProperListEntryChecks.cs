using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProperListEntryChecks
{
    internal const string TestName = "compiler/beam/inherits-proper-list-entry-facts-through-recursion";
    private const string EntryFactsFailure = "Expected only the closed proper-list caller graph to prove a proper list.";
    private const string FailureRewrite = "Expected the verified terminal list match to encode nil and keep its source line.";
    private const string RedundantNilCheck = "A proper-list rejection must enter the nil clause with its original source line.";
    private const string RejectedRewrite = "An open entry, escaping fun, overwritten argument, or shared failure must keep the operand.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Module = 0x12;
    private const byte Pop = 0x22;
    private const byte Open = 0x32;
    private const byte Escaped = 0x42;
    private const byte Failed = 0x35;
    private const byte Nil = 0x02;

    private const string Source = """
        -module(proper_list_scc_probe).
        -export([parse/1, open/2, escape/1, remote/0, flatten/1, foreign/1, list_result/1, difference/2]).
        parse(Tokens) -> step(Tokens, []).
        step([{push, V} | Rest], Stack) -> step(Rest, [V | Stack]);
        step([pop | Rest], Stack) -> step(Rest, pop(known, Stack));
        step([], Stack) -> Stack.
        pop(_, Stack) -> [_ | Tail] = Stack, Tail.
        open(Tokens, Stack) -> open_step(Tokens, Stack).
        open_step([pop | Rest], Stack) -> open_step(Rest, open_pop(known, Stack));
        open_step([], Stack) -> Stack.
        open_pop(_, Stack) -> [_ | Tail] = Stack, Tail.
        escape(call) -> escaped(known, []);
        escape(reference) -> fun escaped/2.
        escaped(_, Stack) -> [_ | Tail] = Stack, Tail.
        remote() -> remote_pop(known, other:tl([value])).
        remote_pop(_, Stack) -> [_ | Tail] = Stack, Tail.
        flatten(Nested) -> flattened(known, lists:flatten(Nested)).
        flattened(_, Stack) -> [_ | Tail] = Stack, Tail.
        foreign(Nested) -> foreign_list(known, other:flatten(Nested)).
        foreign_list(_, Stack) -> [_ | Tail] = Stack, Tail.
        list_result(Items) -> completion(known, [X || X <- Items]).
        completion(_, Stack) -> [_ | Tail] = Stack, Tail.
        difference(L, R) -> remaining(known, lists:subtract(L, R)).
        remaining(_, Stack) -> [_ | Tail] = Stack, Tail.
        """;

    internal static Task Run()
    {
        ModuleDefinition module = new Parser(Source).ParseModule();
        var compiler = new BeamModuleCompiler(module.Name, sourceModule: module);
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(BeamModuleCompiler).GetField("properListSourceFunctions", fields)!.SetValue(
            compiler,
            module.Functions.ToDictionary(function => (function.Name, function.Arity))
        );
        typeof(BeamModuleCompiler).GetField("exportedFunctions", fields)!.SetValue(compiler, module.Exports.ToHashSet());

        if (!Proves("pop", 1) || Proves("pop", 0) || Proves("open_pop", 1) || Proves("escaped", 1)
            || Proves("remote_pop", 1) || !Proves("flattened", 1) || Proves("foreign_list", 1)
            || !Proves("completion", 1) || !Proves("remaining", 1))
        {
            throw new InvalidOperationException(EntryFactsFailure);
        }

        byte[] test = [IsNonemptyList, Failed, X1];
        byte[] error = [Label, Three, Move, X1, X0, Line, One, BadMatch, X0, EndCode];
        byte[] original = [.. Entry(Pop), .. test, Return, .. error];
        byte[] expected = [.. Entry(Pop), .. test, Return, Label, Three, Line, One, BadMatch, Nil, EndCode];
        if (Rewrite(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(FailureRewrite);
        }

        byte[] open = [.. Entry(Open), .. test, Return, .. error];
        byte[] escaped = [.. Entry(Escaped), .. test, Return, .. error];
        byte[] clobber = [.. Entry(Pop), Move, X0, X1, .. test, Return, .. error];
        byte[] shared = [.. Entry(Pop), .. test, Jump, Failed, .. error];
        if (Rewrite(open) is not null || Rewrite(escaped) is not null || Rewrite(clobber) is not null
            || Rewrite(shared) is not null)
        {
            throw new InvalidOperationException(RejectedRewrite);
        }

        const byte FunctionFailure = 0x15;
        byte[] nilClause = [Label, Three, Line, One, IsNil, FunctionFailure, X1, Return, EndCode];
        byte[] withNilCheck = [.. Entry(Pop), .. test, Return, .. nilClause];
        byte[] withoutNilCheck = [.. Entry(Pop), .. test, Return, Label, Three, Line, One, Return, EndCode];
        byte[] openNil = [.. Entry(Open), .. test, Return, .. nilClause];
        byte[] sharedNil = [.. Entry(Pop), .. test, Jump, Failed, .. nilClause];
        byte[] unrelated = [.. Entry(Pop), .. test, Return, Label, Three, IsNil, FunctionFailure, X0, Return, EndCode];
        if (Rewrite(withNilCheck) is not { } exactNil || !exactNil.AsSpan().SequenceEqual(withoutNilCheck)
            || Rewrite(openNil) is not null || Rewrite(sharedNil) is not null || Rewrite(unrelated) is not null)
        {
            throw new InvalidOperationException(RedundantNilCheck);
        }

        _ = Call(compiler, "Intern", "pop");
        _ = Call(compiler, "Intern", "open_pop");
        _ = Call(compiler, "Intern", "escaped");
        byte[] originalTail = [.. Entry(Pop), .. test, GetTail, X1, X1, IsNil, Failed, X1, Return, .. error];
        byte[] typedTail =
        [
            .. Entry(Pop), .. test, GetTail, X1, X1,
            IsNil, Failed, ExtendedTypedRegisterOperand, X1, One, Return, .. error
        ];
        byte[] openTail = [.. Entry(Open), .. test, GetTail, X1, X1, IsNil, Failed, X1, Return, .. error];
        byte[] head = [.. Entry(Pop), .. test, GetHead, X1, X1, IsNil, Failed, X1, Return, .. error];
        if (Annotate(originalTail) is not { } derived || !derived.AsSpan().SequenceEqual(typedTail)
            || Annotate(openTail) is not null || Annotate(head) is not null)
        {
            throw new InvalidOperationException(EntryFactsFailure);
        }

        return Task.CompletedTask;

        byte[]? Annotate(byte[] source) => (byte[]?)Call(
            compiler,
            "AnnotateProvenListOperands",
            new ReadOnlyMemory<byte>(source),
            4,
            new Dictionary<int, int>()
        );

        bool Proves(string function, int argument) =>
            (bool)Call(
                compiler,
                "HasProperListEntryFact",
                (function, 2),
                argument
            );

        byte[]? Rewrite(byte[] source)
        {
            ReadOnlyMemory<byte> instructions = source;
            object header = New(
                "BeamCodeHeader",
                0,
                (int)InitYRegisters,
                4,
                1,
                instructions
            );
            string[] atoms = [module.Name, "pop", "open_pop", "escaped"];
            object body = Call(
                Type("BeamInstructionReader"),
                "Read",
                header,
                atoms.Length + 1,
                0,
                null,
                null
            );

            return (byte[]?)Call(
                compiler,
                "SpecializeProvenListMatchFailures",
                instructions,
                body,
                atoms
            );
        }
    }

    private static byte[] Entry(byte function) => [Label, One, FunctionInfo, Module, function, Two, Label, Two];
}
