using Erlang;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class ListsOpcodeEdgeHostChecks
{
    internal static async Task BinaryBif()
    {
        // Alias Dst with the tuple source; all input operands must be read first.
        byte[] body = [.. Start(
            1,
            2,
            2,
            2
        ), 11, F(0), U(0), X(0), X(1), X(1), 64, X(1), X(0), 19];
        byte[] image = Image(
            body,
            arity: 2,
            importFunction: ListsOpcodeEdgeNames.Element,
            importArity: 2
        );
        Term tuple = Term.Tuple(Term.I(7), Term.I(9));
        Require((await Invoke(image, Term.I(2), tuple)).Equals(Term.I(9)));
        try
        {
            await Invoke(image, Term.I(3), tuple);
            throw new InvalidOperationException(BeamOpcodeEdgeNames.Failure);
        }
        catch (ErlangException error)
        {
            Require(error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
            var frame = (TupleTerm)((Cons)error.StackTraceTerm).Head;
            Require(frame.Items[0].Equals(Term.A(ErlangModuleNames.Module)) && frame.Items[1].Equals(Term.A(ListsOpcodeEdgeNames.Element)));
            Require(frame.Items[2].Equals(Term.List(Term.I(3), tuple)) && frame.Items[3] is Cons);
        }
        byte[] guard = [.. Start(
            1,
            2,
            2,
            2
        ), 12, U(1), U(2), 64, A(7), Y(0),
            11, F(3), U(0), X(0), X(1), Y(0), 64, Y(0), X(0), 18, U(1), 19,
            1, U(3), 64, Y(0), X(0), 18, U(1), 19];
        image = Image(
            guard,
            arity: 2,
            importFunction: ListsOpcodeEdgeNames.Element,
            importArity: 2
        );
        Require((await Invoke(image, Term.I(3), tuple)).Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
        Require((await Invoke(image, Term.I(1), tuple)).Equals(Term.I(7)));
    }

    internal static async Task DynamicFun()
    {
        byte[] body = [.. Start(
            1,
            2,
            2,
            3
        ), 12, U(0), U(3), 75, U(2),
            164, X(0), 0x17, U(2), X(0), A(7), 18, U(0), 19];
        byte[] image = Image(body, arity: 3);
        var callback = new FunctionTerm(2, (_, values) => ValueTask.FromResult<Term>(Term.Tuple(values[0], values[1])));
        Require((await Invoke(
            image,
            Term.I(1),
            Term.I(2),
            callback
        )).Equals(Term.Tuple(Term.Tuple(Term.I(1), Term.I(2)), Term.A(BeamOpcodeEdgeNames.Marker))));
        foreach (Term invalid in new Term[] { Nil.Value, new FunctionTerm(1, (_, _) => ValueTask.FromResult<Term>(Nil.Value)) })
        {
            try
            {
                await Invoke(
                    image,
                    Term.I(1),
                    Term.I(2),
                    invalid
                );
                throw new InvalidOperationException(BeamOpcodeEdgeNames.Failure);
            }
            catch (ErlangException error)
            {
                var reason = (TupleTerm)error.Reason;
                Require(reason.Items[0].Equals(Term.A(invalid is FunctionTerm ? ErlangErrorReasons.BadArity : ErlangErrorReasons.BadFunction)));
            }
        }
    }

    internal static async Task Equality()
    {
        Term integer = Term.Tuple(Term.List(Term.I(1)));
        Term floating = Term.Tuple(Term.List(new FloatTerm(1)));
        foreach (byte opcode in new byte[] { 41, 44 })
        {
            byte[] body = [.. Start(
                1,
                2,
                2,
                2
            ), opcode, F(3), X(0), X(1), 64, A(7), X(0), 19,
                1, U(3), 64, A(8), X(0), 19];
            byte[] image = Image(body, arity: 2);
            Require((await Invoke(image, integer, floating)).Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
            Require((await Invoke(image, integer, integer)).Equals(Term.A(opcode == 41 ? BeamOpcodeEdgeNames.Marker : BeamOpcodeEdgeNames.Other)));
        }
        byte[] floatBody = [.. Start(
            1,
            2,
            2,
            1
        ), 46, F(3), X(0), 64, A(7), X(0), 19,
            1, U(3), 64, A(8), X(0), 19];
        Require((await Invoke(Image(floatBody), new FloatTerm(1))).Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
        Require((await Invoke(Image(floatBody), Term.I(1))).Equals(Term.A(BeamOpcodeEdgeNames.Other)));
    }

    internal static async Task Maps()
    {
        Term key = Term.I(1);
        Term otherKey = new FloatTerm(1);
        var map = new MapTerm([new(key, Term.I(7)), new(otherKey, Term.I(8))]);
        byte[] update = [.. Start(
            1,
            2,
            2,
            2
        ), 64, X(0), X(2),
            154, F(0), X(0), X(0), U(2), 0x17, U(2), X(1), X(0),
            164, X(0), 0x17, U(2), X(2), X(0), 19];
        Term result = await Invoke(Image(update, arity: 2), map, key);
        var pair = (TupleTerm)result;
        Require(pair.Items[0].Equals(map));
        var changed = (MapTerm)pair.Items[1];
        Require(changed.Get(key).Equals(map) && changed.Get(otherKey).Equals(Term.I(8)) && map.Get(key).Equals(Term.I(7)));
        byte[] fields = [.. Start(
            1,
            2,
            2,
            2
        ), 157, F(3), X(0), 0x17, U(1), X(1), 64, A(7), X(0), 19,
            1, U(3), 64, A(8), X(0), 19];
        var onlyInteger = new MapTerm([new(key, Nil.Value)]);
        Require((await Invoke(Image(fields, arity: 2), onlyInteger, key)).Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
        Require((await Invoke(Image(fields, arity: 2), onlyInteger, otherKey)).Equals(Term.A(BeamOpcodeEdgeNames.Other)));
    }

    internal static Task MapRefusals()
    {
        foreach (byte[] instruction in new byte[][]
        {
            [157, F(0), X(0), 0x17, U(0)],
            [157, F(0), X(0), 0x17, U(2), I(1), I(1)],
            [157, F(0), X(0), 0x17, U(2), X(0), X(1)],
            [154, F(3), X(0), X(0), U(1), 0x17, U(2), I(1), I(2)],
            [154, F(0), X(0), X(0), U(1), 0x17, U(0)],
            [154, F(0), X(0), X(0), U(1), 0x17, U(1), I(1)],
            [12, U(1), U(1), 154, F(0), X(0), X(0), U(1), 0x17, U(2), I(1), I(2), 18, U(1)]
        })
        {
            byte[] body = [.. Start(
                1,
                2,
                2,
                1
            ), .. instruction, 19, 1, U(3), 19];
            bool rejected = false;
            try
            {
                BeamModule module = BeamModule.Prepare(Image(body));
                rejected = !module.Supports(BeamOpcodeEdgeNames.Run, 1);
            }
            catch (Exception error) when (error is InvalidDataException or NotSupportedException)
            {
                rejected = true;
            }
            Require(rejected);
        }

        return Task.CompletedTask;
    }

    private static async Task<Term> Invoke(byte[] bytes, params Term[] arguments)
    {
        BeamModule module = BeamModule.Prepare(bytes);
        Require(module.Supports(BeamOpcodeEdgeNames.Run, arguments.Length));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);

        return await actor.InvokeAsync(async context => await module.Invoke(context, BeamOpcodeEdgeNames.Run, arguments));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(BeamOpcodeEdgeNames.Failure);
    }
}
