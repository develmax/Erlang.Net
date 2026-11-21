using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedLeafBranchChecks
{
    internal const string TestName = "compiler/beam/reallocates-owned-linear-leaf-branches";
    private const string WrongCode = "The constructor branch retained a frame or the ordinary call lost its frame.";
    private const string WrongValue = "The constructor branch or unframed fallback changed its result.";
    private const string Source = """
        -module(beam_owned_leaf_branch).
        -export([project/1, distinct/2, ordinary/2]).

        project({A,B,C}) when is_atom(A), is_integer(C) -> {B,C};
        project(T) -> {fallback,T}.

        distinct({Tag,Value}, Other) when is_atom(Other) -> {Tag,Value,Other};
        distinct(T, Other) -> {fallback,T,Other}.

        ordinary({A,B}, Keep) when is_atom(A) -> leaf(tuple_to_list(B), Keep);
        ordinary(T, Keep) -> {fallback,T,Keep}.
        leaf(Values, Keep) -> {Values,Keep}.
        """;

    private const string SelectSource = """
        -module(beam_select_list_tail).
        -export([run/4]).
        -record(route, {extra}).

        run(Tag, Args, Selector, Extension) ->
            dispatch(Tag, Args, Selector, #route{extra=Extension}).

        dispatch(Tag, Args, undefined, Arg) ->
            choose(Tag, Args, Arg);
        dispatch(Tag, Args, false, _) ->
            apply(?MODULE, Tag, Args);
        dispatch(_, Args, Handler, #route{extra=Extra}) ->
            beam_select_peer:consume([[Handler|Extra]|Args]).

        choose(Tag, Args, Arg) ->
            {Tag, Args, Arg}.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "beam_owned_leaf_branch.erl");
        var code = Code(bytes);
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>()
            .OrderBy(function => (int)Property(function, "FirstInstructionIndex"))
            .ToArray();
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        if (Opcodes("project").Any(IsFrame) || Opcodes("distinct").Any(IsFrame)
            || !Opcodes("project").Contains(TestHeap) || !Opcodes("distinct").Contains(TestHeap)
            || !Opcodes("ordinary").Any(IsFrame))
        {
            throw new InvalidOperationException(WrongCode);
        }

        object selectCode = Code(CodeGeneration.CompileBeamModule(SelectSource, "beam_select_list_tail.erl"));
        var select = ((IEnumerable)Property(selectCode, "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        byte[] selectFallback = [TestHeap, GetTupleElement, PutList, PutList, Line, TailExternalCall];
        if (select.AsSpan().IndexOf(selectFallback) < 0)
        {
            throw new InvalidOperationException(WrongCode);
        }

        var module = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("tag");
            Term value = Term.A("value");
            Term marker = Term.A("marker");
            Term count = Term.I(4);
            Term bad = Term.Tuple(Term.I(3), value, count);
            Term two = Term.Tuple(tag, value);
            if (!Term.Tuple(value, count).Equals(await module.Invoke(context, "project", Term.Tuple(tag, value, count)))
                || !Term.Tuple(Term.A("fallback"), bad).Equals(await module.Invoke(context, "project", bad))
                || !Term.Tuple(tag, value, marker).Equals(await module.Invoke(
                    context,
                    "distinct",
                    two,
                    marker
                ))
                || !Term.Tuple(Term.A("fallback"), two, count).Equals(
                    await module.Invoke(
                        context,
                        "distinct",
                        two,
                        count
                    )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });

        byte[] Opcodes(string name)
        {
            int entry = Array.FindIndex(functions, function => (string)Property(function, "Name") == name);
            int first = (int)Property(functions[entry], "FirstInstructionIndex");
            int end = entry + 1 < functions.Length
                ? (int)Property(functions[entry + 1], "FirstInstructionIndex") : instructions.Length;

            return instructions[first..end]
                .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode"))
                .ToArray();
        }

        static bool IsFrame(byte opcode) =>
            opcode is Allocate or AllocateHeap or Deallocate or TailLocalCallWithFrame or TailExternalCallWithFrame;
    }
}
