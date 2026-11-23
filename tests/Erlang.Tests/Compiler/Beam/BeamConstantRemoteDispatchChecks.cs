using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamConstantRemoteDispatchChecks
{
    internal const string TestName = "compiler/beam/switches-closed-remote-dispatch-with-constant-exit";
    private const string Fixture = "beam_constant_remote_dispatch_probe.erl";
    private const string Peer = "beam_constant_remote_dispatch_peer";
    private const string Direct = "direct";
    private const string Prepared = "prepared";
    private const string Guarded = "guarded";
    private const string WrongLayout = "Disjoint closed atom arms should select directly without changing the guarded path.";
    private const string WrongValue = "The closed dispatch changed its remote argument, exit, or unknown selector.";

    private const string Source = """
        -module(beam_constant_remote_dispatch_probe).
        -export([direct/3, prepared/3, guarded/3]).

        direct(action, Payload, _) ->
            beam_constant_remote_dispatch_peer:run(Payload,[1]);
        direct(reject, _, _) ->
            exit({rejected,direct}).

        prepared(action, Payload, _) ->
            Value = prepare(Payload),
            beam_constant_remote_dispatch_peer:run(Value,[2]);
        prepared(reject, _, _) ->
            exit({rejected,prepared}).

        guarded(action, Payload, Guard) when is_atom(Guard) ->
            beam_constant_remote_dispatch_peer:run(Payload,[3]);
        guarded(reject, _, _) ->
            exit({rejected,guarded}).

        prepare(Payload) -> {converted,Payload}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        var module = BeamModule.Prepare(image, new BeamDependencySelection([Peer, "erlang"]));
        await using var runtime = new ProcessRuntime();
        runtime.Modules.Register(
            Peer,
            "run",
            2,
            (_, args) => ValueTask.FromResult<Term>(Term.Tuple(args[0], args[1]))
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term option = Term.A("option");
            Term payload = Term.A("payload");
            Term action = Term.A("action");
            if (!Term.Tuple(payload, Term.List(Term.I(1))).Equals(await module.Invoke(
                context,
                Direct,
                action,
                payload,
                option
            ))
                || !Term.Tuple(Term.Tuple(Term.A("converted"), payload), Term.List(Term.I(2))).Equals(await module.Invoke(
                    context,
                    Prepared,
                    action,
                    payload,
                    option
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await ExpectError(
                Direct,
                Term.A("reject"),
                option,
                Term.Tuple(Term.A("rejected"), Term.A(Direct))
            );
            await ExpectError(
                Prepared,
                Term.A("unrecognised"),
                option,
                Term.A("function_clause")
            );
            await ExpectError(
                Guarded,
                action,
                Term.I(1),
                Term.A("function_clause")
            );

            return Nil.Value;

            async Task ExpectError(
                string function,
                Term tag,
                Term last,
                Term expected
            )
            {
                try
                {
                    await module.Invoke(
                        context,
                        function,
                        tag,
                        payload,
                        last
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(expected))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }
        });

        if (!HasSelector(image, Direct) || !HasSelector(image, Prepared) || HasSelector(image, Guarded))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static bool HasSelector(byte[] image, string function)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == function && Value(arity) == 3
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Any(instruction => Opcode(instruction) == SelectValue);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
