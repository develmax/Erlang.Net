using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWritableMappedBinaryListChecks
{
    internal const string TestName = "compiler/beam/keeps-owned-writable-roots-across-an-element-call";
    private const string Module = "beam_writable_mapped_list";
    private const string NibbleHelper = "-nibbles/1-lbc$^0/2-0-";
    private const string ByteHelper = "-bytes/1-lbc$^0/2-0-";
    private const string DynamicHelper = "-dynamic/1-lbc$^0/2-0-";
    private const string WrongLayout = "Expected the protected accumulator and list tail across one local element call.";
    private const string WrongValue = "The fixed-width element call changed input order or an Erlang failure.";
    private const string Source = """
        -module(beam_writable_mapped_list).
        -export([nibbles/1, bytes/1, dynamic/1]).
        nibbles(Items) ->
            << <<(nibble_value(Item)):4>> || Item <- Items >>.
        bytes(Items) ->
            << <<(byte_value(Item)):8>> || Item <- Items >>.
        dynamic(Items) ->
            << <<1:Width>> || Width <- Items >>.
        nibble_value(C) when C >= $0, C =< $9 -> C-$0;
        nibble_value(C) when C >= $A, C =< $F -> C-($A - 10).
        byte_value(alpha) -> 0;
        byte_value(beta) -> 1.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        var module = BeamModule.Prepare(image);
        if (!module.Functions.Any(function => function.Name == NibbleHelper)
            || !module.Functions.Any(function => function.Name == ByteHelper)
            || module.Functions.Any(function => function.Name == DynamicHelper))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object code = Code(image);
        CheckLayout(
            code,
            "nibbles",
            NibbleHelper,
            4
        );
        CheckLayout(
            code,
            "bytes",
            ByteHelper,
            8
        );
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term beta = Term.A("beta");
            Term invalid = Term.A("invalid");
            if (!new BitString([0xA0, 0xF0], 12).Equals(
                    await module.Invoke(context, "nibbles", Term.List(Term.I(65), Term.I(48), Term.I(70))))
                || !new BitString([1, 0, 1]).Equals(await module.Invoke(context, "bytes", Term.List(beta, alpha, beta)))
                || !new BitString([]).Equals(await module.Invoke(context, "bytes", Nil.Value))
                || !new BitString([0x20], 3).Equals(await module.Invoke(context, "dynamic", Term.List(Term.I(3)))))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await Fails(
                "bytes",
                new Cons(alpha, invalid),
                Term.Tuple(Term.A(ComprehensionErrorReasons.BadGenerator), invalid),
                ByteHelper
            );
            await Fails(
                "bytes",
                Term.List(invalid),
                Term.A("function_clause"),
                "byte_value"
            );
            await Fails(
                "nibbles",
                Term.List(new FloatTerm(48.0)),
                Term.A("badarg"),
                NibbleHelper
            );
            await Fails(
                "nibbles",
                Term.List(new FloatTerm(65.5)),
                Term.A("badarg"),
                NibbleHelper
            );

            return Nil.Value;

            async Task Fails(
                string function,
                Term input,
                Term reason,
                string stackFunction
            )
            {
                try
                {
                    await module.Invoke(context, function, input);
                }
                catch (ErlangException error) when (error.Reason.Equals(reason)
                    && error.StackTraceTerm is Cons
                    {
                        Head: TupleTerm { Items: [Atom { Name: Module }, Atom failedFunction, _, _] }
                    } && failedFunction.Name == stackFunction)
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }
        });
    }

    private static void CheckLayout(
        object code,
        string owner,
        string helper,
        int width
    )
    {
        object[] functions = Items(code, "Functions").ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] entry = Read(owner);
        object[] body = Read(helper);
        object allocate = body.Single(operation => Opcode(operation) == Allocate);
        object construct = body.Single(operation => Opcode(operation) == CreateBinary);
        object[] operands = Items(construct, "Operands").ToArray();
        int test = Array.FindIndex(body, operation => Opcode(operation) == IsNonemptyList);
        if (entry.Count(operation => Opcode(operation) == GcBifOne) != 1
            || entry.Count(operation => Opcode(operation) == GcBifTwo) != (width == 4 ? 3 : 0)
            || test < 0 || test + 1 >= body.Length || body[test + 1] != allocate
            || Items(allocate, "Operands").Select(Value).ToArray() is not [2, 2]
            || body.Count(operation => Opcode(operation) == LocalCall) != 1
            || body.Count(operation => Opcode(operation) == TailLocalCallWithFrame) != 1
            || Value(operands[2]) != 1 || Value(operands[3]) != width || Value(operands[4]) != 1)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] Read(string name)
        {
            object selected = functions.Single(function => (string)Property(function, "Name") == name);
            int start = (int)Property(selected, "FirstInstructionIndex");
            int end = (int)Property(selected, "EndInstructionIndex");

            return instructions.Skip(start).Take(end - start).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
