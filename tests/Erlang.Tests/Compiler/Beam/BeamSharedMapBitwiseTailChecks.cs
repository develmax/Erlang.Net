using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSharedMapBitwiseTailChecks
{
    internal const string TestName = "compiler/beam/shares-two-checked-map-fields-before-a-bitwise-tail";
    private const string WrongLayout = "The checked map arguments or their frameless tail are incorrectly lowered.";
    private const string WrongBehavior = "The guarded map entry changed a returned value or input failure.";
    private const string Source = """
        -module(beam_shared_map_bitwise_tail).
        -export([entry/3, unchecked/3]).

        entry(Map, Options, Time) when is_map(Map) andalso is_list(Options) ->
            checked(Map, Options, Time).

        checked(#{addr := {A,B,C,D}, port := P} = Map, Options, Time)
          when ((A bor B bor C bor D) band bnot 255) =:= 0 andalso
               (P band bnot 65535) =:= 0 ->
            forward(Map, Options, Time);
        checked(#{addr := Address, port := P} = Map, Options, Time)
          when Address =:= alternate andalso (P band bnot 65535) =:= 0 ->
            forward(Map, Options, Time).

        unchecked(#{addr := {A,B,C,D}, port := P} = Map, Options, Time)
          when ((A bor B bor C bor D) band bnot 255) =:= 0 andalso
               (P band bnot 65535) =:= 0 ->
            forward(Map, Options, Time);
        unchecked(#{addr := Address, port := P} = Map, Options, Time)
          when Address =:= alternate andalso (P band bnot 65535) =:= 0 ->
            forward(Map, Options, Time).

        forward(Map, Options, Time) -> {Map,Options,Time}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_shared_map_bitwise_tail.erl");
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Check("checked", explicitMapTest: false);
        Check("unchecked", explicitMapTest: true);

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term timeout = Term.I(7);
            Term options = Nil.Value;
            Term paired = MakeMap(Term.Tuple(
                Term.I(127),
                Term.I(0),
                Term.I(0),
                Term.I(1)
            ), Term.I(80));
            Term alternative = MakeMap(Term.A("alternate"), Term.I(0));
            foreach (Term map in new[] { paired, alternative })
            {
                Term expected = Term.Tuple(map, options, timeout);
                if (!expected.Equals(await module.Invoke(
                    context,
                    "entry",
                    map,
                    options,
                    timeout
                )))
                {
                    throw new InvalidOperationException(WrongBehavior);
                }
            }

            await CheckFailure("entry", Term.A("non_map"));
            await CheckFailure("unchecked", Term.A("non_map"));
            await CheckFailure("entry", new MapTerm([]));
            await CheckFailure("entry", MakeMap(Term.A("alternate"), Term.A("invalid_port")));
            await CheckFailure("entry", MakeMap(Term.Tuple(
                Term.I(127),
                Term.A("invalid_octet"),
                Term.I(0),
                Term.I(1)
            ), Term.I(80)));

            return Nil.Value;

            async Task CheckFailure(string name, Term argument)
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        name,
                        argument,
                        options,
                        timeout
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongBehavior);
            }
        });

        void Check(string name, bool explicitMapTest)
        {
            int info = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands")[1])] == name);
            if (info < 0)
            {
                throw new InvalidOperationException(WrongLayout);
            }

            object[] body = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            byte[] ops = body.Select(Opcode).ToArray();
            if (ops.Contains(Allocate) || ops.Contains(AllocateHeap) || ops.Contains(IsMap) != explicitMapTest
                || ops.Count(opcode => opcode == GetMapElements) != 1
                || ops.Count(opcode => opcode == TailLocalCall) != 1)
            {
                throw new InvalidOperationException(WrongLayout);
            }

            object[] fields = Items(body.Single(instruction => Opcode(instruction) == GetMapElements), "Operands");
            if (fields.Length != 7 || atoms[Value(fields[3])] != "port" || Value(fields[4]) != 4
                || atoms[Value(fields[5])] != "addr" || Value(fields[6]) != 3)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }
    }

    private static Term MakeMap(Term address, Term port) =>
        new MapTerm([new(Term.A("addr"), address), new(Term.A("port"), port)]);

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
