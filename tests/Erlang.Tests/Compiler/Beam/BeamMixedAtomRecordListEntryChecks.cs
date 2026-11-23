using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMixedAtomRecordListEntryChecks
{
    internal const string TestName = "compiler/beam/admits-variable-list-heads-in-mixed-atom-record-entries";
    private const string File = "beam_mixed_atom_record_list_entry.erl";
    private const string WrongShape = "The mixed entry did not dispatch atom and tuple arms with an early list split.";
    private const string WrongValue = "The mixed entry changed a matched field, clause retry, or body failure.";
    private const string Source = """
        -module(beam_mixed_atom_record_list_entry).
        -export([choose/2, repeat/2]).
        -record(state, {markers}).

        choose({two,A}, State) -> {two,A,State};
        choose({three,A,B}, State) -> {three,A,B,State};
        choose({four,A,B,C}, State) -> {four,A,B,C,State};
        choose(done, #state{markers=[Head|Tail]} = State) ->
            Head = sentinel,
            State#state{markers=Tail};
        choose(scan, #state{markers=[{_,_}|_]} = State) -> {scanned,State};
        choose(Event, State) -> {fallback,Event,State}.

        repeat({two,A}, State) -> {two,A,State};
        repeat({three,A,B}, State) -> {three,A,B,State};
        repeat({four,A,B,C}, State) -> {four,A,B,C,State};
        repeat(done, #state{markers=[Head|Head]}) -> equal;
        repeat(Event, State) -> {fallback,Event,State}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, File);
        CheckShape(beam);
        BeamModule module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term done = Term.A("done");
            Term scan = Term.A("scan");
            Term stateTag = Term.A("state");
            Term sentinel = Term.A("sentinel");
            Term other = Term.A("other");
            Term valid = Term.Tuple(stateTag, new Cons(sentinel, other));
            Term scanned = Term.Tuple(stateTag, Term.List(Term.Tuple(other, sentinel)));
            if (!Term.Tuple(stateTag, other).Equals(await module.Invoke(
                context,
                "choose",
                done,
                valid
            ))
                || !Term.Tuple(Term.A("scanned"), scanned).Equals(await module.Invoke(
                    context,
                    "choose",
                    scan,
                    scanned
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            foreach (Term invalid in new[]
            {
                Term.Tuple(stateTag, Nil.Value),
                Term.Tuple(stateTag, other),
                Term.Tuple(other, Term.List(sentinel)),
                Term.Tuple(stateTag)
            })
            {
                if (!Term.Tuple(Term.A("fallback"), done, invalid).Equals(await module.Invoke(
                    context,
                    "choose",
                    done,
                    invalid
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            Term same = Term.Tuple(stateTag, new Cons(sentinel, sentinel));
            Term different = Term.Tuple(stateTag, Term.List(sentinel));
            if (!Term.A("equal").Equals(await module.Invoke(
                context,
                "repeat",
                done,
                same
            ))
                || !Term.Tuple(Term.A("fallback"), done, different).Equals(await module.Invoke(
                    context,
                    "repeat",
                    done,
                    different
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term wrongHead = Term.Tuple(stateTag, Term.List(other));
            try
            {
                await module.Invoke(
                    context,
                    "choose",
                    done,
                    wrongHead
                );
                throw new InvalidOperationException(WrongValue);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("badmatch"), sentinel)))
            {
                return Nil.Value;
            }
        });
    }

    private static void CheckShape(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == "choose"
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(WrongShape);
        }

        byte[] body = instructions.Skip(entry + 1).Select(Opcode).TakeWhile(opcode => opcode != FunctionInfo).ToArray();
        int atomDispatch = Array.IndexOf(body, SelectValue);
        if (!body.Contains(SelectTupleArity) || atomDispatch < 0
            || body.Skip(atomDispatch + 1).Count(opcode => opcode == GetList) < 2)
        {
            throw new InvalidOperationException(WrongShape);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object instance, string name) => ((IEnumerable)Property(instance, name)).Cast<object>();
}
