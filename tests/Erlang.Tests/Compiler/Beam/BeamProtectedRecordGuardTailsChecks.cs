using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProtectedRecordGuardTailsChecks
{
    internal const string TestName = "compiler/beam/joins-protected-record-guard-alternatives-before-tail";
    private const string WrongLayout = "The guarded record lookup did not retain the protected value joins and tail setup.";
    private const string WrongValue = "The guarded record lookup changed a matched arm or its original rejection.";
    private const string Source = """
        -module(beam_protected_record_guard_tails).
        -export([run/2]).
        run(N, Options) ->
            case produce([{index,N}|Options], named) of
                {error, Reason} -> leave(Reason);
                {ok, {config, Addr, Port, Fd, Meta}}
                  when is_map(Addr);
                       Port =:= -1, tuple_size(Addr) =:= 2 andalso
                           ((element(1,Addr) bor element(2,Addr)) band bnot 255) =:= 0;
                       Port =:= -1, Addr =:= unset;
                       (Port band bnot 255) =:= 0, tuple_size(Addr) =:= 2 andalso
                           ((element(1,Addr) bor element(2,Addr)) band bnot 255) =:= 0;
                       (Port band bnot 255) =:= 0, Addr =:= unset ->
                    finish(Fd, Addr, Port, Meta, alpha, beta, gamma, delta);
                {ok, _} -> leave(malformed)
            end.
        produce([_,Result|_], named) -> Result.
        leave(Reason) -> {rejected,Reason}.
        finish(Fd, Addr, Port, Meta, _, _, _, _) -> {accepted,Fd,Addr,Port,Meta}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_protected_record_guard_tails.erl");
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        object structure = Property(code, "Structure");
        string[] atoms = Items(structure, "Atoms").Cast<string>().ToArray();
        object[] imports = Items(structure, "Imports");
        int info = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Operands(instruction)[1])] == "run");
        if (info < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] body = instructions.Skip(info + 1).TakeWhile(operation => Opcode(operation) != FunctionInfo).ToArray();
        if (body.Count(operation => Opcode(operation) == Swap) != 3
            || body.Count(operation => Opcode(operation) == BifTwo && IsImport(Operands(operation)[1], "or")) != 2
            || body.Count(operation => Opcode(operation) == BifTwo && IsImport(Operands(operation)[1], "and")) != 4
            || body.Where(operation => Opcode(operation) == AllocateHeap)
                .Select(operation => Value(Operands(operation)[0])).SingleOrDefault(-1) != 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term unset = Term.A("unset");
            Term pair = Term.Tuple(Term.I(40), Term.I(11));
            Term malformed = Term.Tuple(Term.I(40), Term.A("item"));
            Term bad = Term.A("wrong");
            Term map = new MapTerm([]);
            await Accepted(map, bad);
            await Accepted(pair, Term.I(-1));
            await Accepted(pair, Term.I(7));
            await Accepted(unset, Term.I(-1));
            await Accepted(unset, Term.I(7));
            await Rejected(malformed, Term.I(-1));
            await Rejected(Term.Tuple(Term.I(1)), Term.I(7));
            await Rejected(unset, bad);
            await Rejected(unset, Term.I(256));
            Term error = Term.Tuple(Term.A("error"), bad);
            if (!Term.Tuple(Term.A("rejected"), bad).Equals(await Invoke(error)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;

            async Task Accepted(Term address, Term port)
            {
                Term expected = Term.Tuple(
                    Term.A("accepted"),
                    Term.I(9),
                    address,
                    port,
                    Term.A("marker")
                );
                if (!expected.Equals(await Invoke(Result(address, port))))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            async Task Rejected(Term address, Term port)
            {
                if (!Term.Tuple(Term.A("rejected"), Term.A("malformed")).Equals(await Invoke(Result(address, port))))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            Task<Term> Invoke(Term value) => module.Invoke(
                context,
                "run",
                Term.I(1),
                Term.List(value)
            ).AsTask();
        });

        bool IsImport(object index, string name) =>
            atoms[(int)Property(imports[Value(index)], "FunctionAtomIndex")] == name;
    }

    private static Term Result(Term address, Term port) =>
        Term.Tuple(Term.A("ok"), Term.Tuple(
            Term.A("config"),
            address,
            port,
            Term.I(9),
            Term.A("marker")
        ));

    private static object[] Items(object value, string name) =>
        ((IEnumerable)Property(value, name)).Cast<object>().ToArray();

    private static object[] Operands(object value) => Items(value, "Operands");

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
