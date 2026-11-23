using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMixedPairEntryChecks
{
    private const string WrongSchedule = "The second-field selector lost its checked outer payload or typed branch order.";
    private const string WrongResult = "The second-field entry changed a selected branch, failure, or fallback.";
    private const string Source = """
        -module(beam_mixed_pair_entry_probe).
        -export([describe/1, summarize/2, guarded/1]).

        describe({Payload, alpha}) -> relay(alpha, [Payload]);
        describe({Payload, zulu}) -> relay(zulu, [Payload]);
        describe({Payload, {Name, Count}}) -> relay(pair, [Payload, Name, Count]);
        describe([]) -> relay(empty, []);
        describe(Value) when is_list(Value) -> Value ++ [stop].

        summarize(undefined, _) -> empty;
        summarize({Payload, alpha}, _) -> {alpha, [Payload]};
        summarize({Payload, zulu}, _) -> {zulu, [Payload, erlang:tuple_size(Payload)]};
        summarize({_Unused, {Name, Value}}, State) ->
            Seen = observe(Name, State),
            {nested, Seen, Value}.

        guarded({Payload, alpha}) when is_atom(Payload) -> {guarded, Payload};
        guarded({Payload, alpha}) -> {fallback, Payload};
        guarded({Payload, zulu}) -> {zulu, Payload};
        guarded({Payload, {Name, Count}}) -> {pair, Payload, Name, Count};
        guarded([]) -> empty;
        guarded(Value) when is_list(Value) -> {list, Value}.

        relay(Key, Values) -> {Key, Values}.
        observe(Name, State) -> {Name, State}.
        """;

    internal static async Task Check()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, "beam_mixed_pair_entry_probe.erl");
        ValidateSelectors(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term zulu = Term.A("zulu");
            Term pair = Term.A("pair");
            Term name = Term.A("name");
            Term token = Term.A("token");
            Term state = Term.A("state");
            Term count = Term.I(8);
            Term payload = Term.Tuple(name, count);
            Term paired = Term.Tuple(token, payload);
            Term marker = Term.A("stop");
            Term list = Term.List(token);
            var cases = new (string Function, Term[] Arguments, Term Expected)[]
            {
                ("describe", [Term.Tuple(token, alpha)], Term.Tuple(alpha, Term.List(token))),
                ("describe", [Term.Tuple(token, zulu)], Term.Tuple(zulu, Term.List(token))),
                ("describe", [paired], Term.Tuple(pair, Term.List(token, name, count))),
                ("describe", [Nil.Value], Term.Tuple(Term.A("empty"), Nil.Value)),
                ("describe", [list], Term.List(token, marker)),
                ("summarize", [Term.A("undefined"), state], Term.A("empty")),
                ("summarize", [Term.Tuple(payload, zulu), state], Term.Tuple(zulu, Term.List(payload, Term.I(2)))),
                ("summarize", [paired, state], Term.Tuple(Term.A("nested"), Term.Tuple(name, state), count)),
                ("guarded", [Term.Tuple(token, alpha)], Term.Tuple(Term.A("guarded"), token)),
                ("guarded", [Term.Tuple(payload, alpha)], Term.Tuple(Term.A("fallback"), payload))
            };
            foreach (var (function, arguments, expected) in cases)
            {
                if (!expected.Equals(await module.Invoke(context, function, arguments)))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            await CheckFailure("describe", ErlangErrorReasons.FunctionClause, Term.Tuple(token, Term.Tuple(name)));
            await CheckFailure("describe", ErlangErrorReasons.BadArgument, new Cons(token, state));
            await CheckFailure(
                "summarize",
                ErlangErrorReasons.BadArgument,
                Term.Tuple(token, zulu),
                state
            );

            return Nil.Value;

            async Task CheckFailure(string function, string reason, params Term[] arguments)
            {
                try
                {
                    _ = await module.Invoke(context, function, arguments);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(reason)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static void ValidateSelectors(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int info = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Items(instruction, "Operands")[1])] == "describe"
        );
        if (info < 0)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        object[] body = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int firstProjection = Array.FindIndex(body, instruction => Opcode(instruction) == GetTupleElement);
        int selection = Array.FindIndex(body, instruction => Opcode(instruction) == SelectValue);
        const int payloadField = 0;
        const int modeRegister = 2;
        if (firstProjection < 0 || selection <= firstProjection
            || body.Take(firstProjection).Count(instruction => Opcode(instruction) == IsTuple) != 1
            || Value(Items(body[firstProjection], "Operands")[1]) != payloadField
            || Value(Items(body[selection], "Operands")[0]) != modeRegister)
        {
            throw new InvalidOperationException(WrongSchedule);
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
