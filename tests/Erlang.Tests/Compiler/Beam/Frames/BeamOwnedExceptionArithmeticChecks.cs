using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedExceptionArithmeticChecks
{
    internal const string TestName = "compiler/beam/keeps-owned-exception-arithmetic-roots-in-registers";
    private const string Fixture = "beam_owned_exception_arithmetic.erl";
    private const string WrongFrame = "The arithmetic arm must keep its collected values in X and avoid a returning frame.";
    private const string WrongResult = "The arithmetic arm changed its constructed value, fallback, or original exception.";
    private const string Source = """
        -module(beam_owned_exception_arithmetic).
        -export([run/2, checked/2]).
        -record(state, {count, entries, payload}).
        -record(entry, {value}).

        run({bump, Value}, #state{count=[N|Ns],entries=[#entry{value=Previous}=Entry|Rest]}=State) ->
            State#state{count=[N+1|Ns],entries=[Entry#entry{value={Previous,Value}}|Rest]};
        run({keep, Value}, #state{payload=[Head|Rest]}=State) ->
            State#state{payload=[Head,Value|Rest]};
        run({legacy}, State) ->
            {legacy,State};
        run(Event, State) ->
            {outside,Event,State}.

        checked(Event, State) ->
            try run(Event, State)
            catch error:Reason -> Reason
            end.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckFrame(beam);

        BeamModule module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term state = Term.A("state");
            Term entry = Term.A("entry");
            Term old = Term.A("old");
            Term updated = Term.A("updated");
            Term bump = Term.Tuple(Term.A("bump"), updated);
            Term entries = Term.List(Term.Tuple(entry, old));
            Term good = Term.Tuple(
                state,
                Term.List(Term.I(2)),
                entries,
                Nil.Value
            );
            Term bad = Term.Tuple(
                state,
                Term.List(old),
                entries,
                Nil.Value
            );
            Term result = Term.Tuple(
                state,
                Term.List(Term.I(3)),
                Term.List(Term.Tuple(entry, Term.Tuple(old, updated))),
                Nil.Value
            );

            if (!result.Equals(await module.Invoke(
                context,
                "run",
                bump,
                good
            ))
                || !Term.A("badarith").Equals(await module.Invoke(
                    context,
                    "checked",
                    bump,
                    bad
                ))
                || !Term.Tuple(Term.A("outside"), bump, old).Equals(await module.Invoke(
                    context,
                    "run",
                    bump,
                    old
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static void CheckFrame(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Operands(instruction)[1])] == "run");
        if (entry < 0)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        object[] body = instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int arithmetic = Array.FindIndex(body, instruction => Opcode(instruction) == GcBifTwo);
        int branch = arithmetic < 0 ? -1 : Array.FindLastIndex(body, arithmetic, instruction => Opcode(instruction) == Label);
        if (branch < 0 || Property(Operands(body[arithmetic])[0], "Kind") is not "failure"
            || Value(Operands(body[arithmetic])[1]) == 0
            || body.Skip(branch).Take(arithmetic - branch).Any(instruction => Opcode(instruction) is Allocate or AllocateHeap)
            || !body.Skip(arithmetic + 1).TakeWhile(instruction => Opcode(instruction) is not (Label or Return))
                .Any(instruction => Opcode(instruction) == UpdateRecord))
        {
            throw new InvalidOperationException(WrongFrame);
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
