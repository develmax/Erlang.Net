using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOrderedConsHeadAliasChecks
{
    internal const string TestName = "compiler/beam/reuses-matched-tuple-in-ordered-list-head";
    private const string Module = "ordered_token_alias_fixture";
    private const string Provider = "ordered_token_alias_provider";
    private const string TailFailure = "tail_failed";
    private const int MatchedHeapWords = 7;
    private const int ChangedHeapWords = 11;
    private const int TailFailureLine = 5;

    private const string Source = """
        -module(ordered_token_alias_fixture).
        -export([same/1,changed/1,single/1]).

        same([{dash,_},{token,Anno,word}|Tail]) ->
            [{prefix,Anno},{token,Anno,word}|ordered_token_alias_provider:tail(Tail)];
        same(_) -> fallback.

        changed([{dash,_},{token,Anno,word}|Tail]) ->
            [{prefix,Anno},{token,Anno,other}|ordered_token_alias_provider:tail(Tail)];
        changed(_) -> fallback.

        single([{dash,_},{token,Anno,word}|Tail]) ->
            [{token,Anno,word}|ordered_token_alias_provider:tail(Tail)];
        single(_) -> fallback.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(beam);
        VerifyConstruction(
            code,
            "same",
            MatchedHeapWords,
            headIsSavedTuple: true
        );
        VerifyConstruction(
            code,
            "changed",
            ChangedHeapWords,
            headIsSavedTuple: false
        );

        var image = BeamModule.Prepare(beam, new BeamDependencySelection([Provider]));
        Term fail = Term.A("fail");
        Term? observed = null;
        await using var runtime = new ProcessRuntime();
        runtime.Modules.Register(
            Provider,
            "tail",
            1,
            (_, args) =>
            {
                observed = args[0];
                if (fail.Equals(observed))
                {
                    throw new ErlangException(Term.A(TailFailure));
                }

                return ValueTask.FromResult(args[0]);
            }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term dash = Term.Tuple(Term.A("dash"), Term.I(0));
            Term annotation = Term.Tuple(Term.A("line"), Term.I(3));
            Term word = Term.Tuple(Term.A("token"), annotation, Term.A("word"));
            Term changed = Term.Tuple(Term.A("token"), annotation, Term.A("other"));
            Term prefix = Term.Tuple(Term.A("prefix"), annotation);
            Term rest = Term.A("end_token");
            Term input = Term.List(dash, word, rest);
            Term same = await image.Invoke(context, "same", input);
            Term different = await image.Invoke(context, "changed", input);
            Term single = await image.Invoke(context, "single", input);
            if (!Term.List(prefix, word, rest).Equals(same)
                || !Term.List(prefix, changed, rest).Equals(different)
                || !Term.List(word, rest).Equals(single)
                || !Term.List(rest).Equals(observed))
            {
                throw new InvalidOperationException(BeamOrderedConsHeadAliasDiagnostics.BranchResult);
            }

            observed = null;
            Term fallback = await image.Invoke(context, "same", Term.List(dash, changed));
            if (!Term.A("fallback").Equals(fallback) || observed is not null)
            {
                throw new InvalidOperationException(BeamOrderedConsHeadAliasDiagnostics.BranchResult);
            }

            try
            {
                await image.Invoke(context, "same", new Cons(dash, new Cons(word, fail)));
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(TailFailure))
                && fail.Equals(observed) && HasFailureLocation(error))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(BeamOrderedConsHeadAliasDiagnostics.BranchResult);
        });
    }

    private static bool HasFailureLocation(ErlangException error) =>
        Cons.Items(error.StackTraceTerm).Any(frame => frame is TupleTerm
        {
            Items: [Atom { Name: Module }, Atom { Name: "same" }, _, var source]
        } && Cons.Items(source).Contains(Term.Tuple(Term.A("line"), Term.I(TailFailureLine))));

    private static void VerifyConstruction(
        object code,
        string function,
        int heapWords,
        bool headIsSavedTuple
    )
    {
        object entry = Items(code, "Functions").Single(item => (string)Property(item, "Name") == function);
        int start = (int)Property(entry, "FirstInstructionIndex");
        int end = (int)Property(entry, "EndInstructionIndex");
        object[] instructions = Items(code, "Instructions")[start..end];
        int call = Array.FindIndex(instructions, instruction => Opcode(instruction) == ExternalCall);
        if (call < 0 || call + 2 >= instructions.Length || Opcode(instructions[call + 1]) != TestHeap
            || (BigInteger)Property(Items(instructions[call + 1], "Operands")[0], "Value") != heapWords)
        {
            throw new InvalidOperationException(BeamOrderedConsHeadAliasDiagnostics.Reconstruction);
        }

        object first = instructions[call + 2];
        bool savedHead = Opcode(first) == PutList && Property(Items(first, "Operands")[0], "Kind") is "y";
        if (savedHead != headIsSavedTuple || !savedHead && Opcode(first) != PutTuple)
        {
            throw new InvalidOperationException(BeamOrderedConsHeadAliasDiagnostics.Reconstruction);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
