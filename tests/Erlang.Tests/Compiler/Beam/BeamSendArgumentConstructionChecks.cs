using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSendArgumentConstructionChecks
{
    internal const string TestName = "compiler/beam/constructs-a-tuple-message-directly-in-x1";
    private const string IncorrectSchedule = "The send argument constructor should keep the original recipient and a zero-slot frame.";
    private const string UnsafeSchedule = "A retained stack value or intervening heap check must prevent this send construction rewrite.";
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte ReturnAtom = 0x12;
    private const byte MessageTag = 0x22;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;

    internal static async Task Run()
    {
        byte[] allocate = [Label, One, AllocateHeap, One, Three, Two, Move, X0, Y0];
        byte[] message = [PutTuple, X0, ExtendedListOperand, Two, MessageTag, X1];
        byte[] copies = [Move, X0, X1, Move, Y0, X0];
        byte[] finish = [Move, ReturnAtom, X0, Deallocate, One, Return, EndCode];
        byte[] source = [.. allocate, .. message, .. copies, Line, One, Send, .. finish];
        byte[] expected =
        [
            Label, One, AllocateHeap, Zero, Three, Two,
            PutTuple, X1, ExtendedListOperand, Two, MessageTag, X1,
            Line, One, Send, Move, ReturnAtom, X0, Deallocate, Zero, Return, EndCode
        ];
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(IncorrectSchedule);
        }

        byte[] retained =
        [
            .. allocate, .. message, .. copies, Send, Move, Y0, X0,
            Deallocate, One, Return, EndCode
        ];
        byte[] usedInTuple =
        [
            .. allocate, PutTuple, X0, ExtendedListOperand, Two, MessageTag, Y0,
            .. copies, Send, .. finish
        ];
        byte[] collecting =
        [
            .. allocate, .. message, .. copies, TestHeap, Three, Two, Send, .. finish
        ];
        if (Rewrite(retained) is not null || Rewrite(usedInTuple) is not null || Rewrite(collecting) is not null)
        {
            throw new InvalidOperationException(UnsafeSchedule);
        }

        await CheckEntrySends();
    }

    private static async Task CheckEntrySends()
    {
        const string fixture = """
            -module(beam_register_send).
            -export([literal/1, nested/3, variable/2, preserve/2]).

            literal(Pid) -> Pid ! {user,stop}.
            nested(Pid, Tag, Payload) -> Pid ! {user,{Tag,Payload}}.
            variable(Pid, Payload) -> Pid ! Payload.
            preserve(Pid, Payload) ->
                Pid ! {user,Payload},
                Payload.
            """;

        byte[] image = CodeGeneration.CompileBeamModule(fixture, "beam_register_send.erl");
        object code = Code(image);
        object structure = Property(code, "Structure");
        string[] atoms = Values(structure, "Atoms").Cast<string>().ToArray();
        object[] instructions = Values(code, "Instructions");
        foreach (string function in new[] { "literal", "nested", "variable" })
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Values(instruction, "Operands")[1])] == function);
            if (entry < 0)
            {
                throw new InvalidOperationException(IncorrectSchedule);
            }

            object[] body = instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            if (body.Count(instruction => Opcode(instruction) == Send) != 1
                || body.Where(instruction => Opcode(instruction) is Allocate or AllocateHeap or Deallocate)
                    .Select(instruction => Value(Values(instruction, "Operands")[0])).ToArray() is not [0, 0])
            {
                throw new InvalidOperationException(IncorrectSchedule);
            }
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term user = Term.A("user");
            Term tag = Term.A("event");
            Term payload = Term.List(Term.I(1), Term.I(2));
            Term literalMessage = Term.Tuple(user, Term.A("stop"));
            Term constructedMessage = Term.Tuple(user, Term.Tuple(tag, payload));
            Term retainedMessage = Term.Tuple(user, payload);
            await Verify(
                "literal",
                literalMessage,
                literalMessage,
                [context.Self]
            );
            await Verify(
                "nested",
                constructedMessage,
                constructedMessage,
                [context.Self, tag, payload]
            );
            await Verify(
                "variable",
                payload,
                payload,
                [context.Self, payload]
            );
            await Verify(
                "preserve",
                retainedMessage,
                payload,
                [context.Self, payload]
            );

            return Nil.Value;

            async Task Verify(
                string name,
                Term expectedMessage,
                Term expectedResult,
                Term[] args
            )
            {
                Term returned = await module.Invoke(context, name, args);
                Term? received = await context.ReceiveAsync(message => message, TimeSpan.FromSeconds(1));
                if (!expectedResult.Equals(returned) || !expectedMessage.Equals(received))
                {
                    throw new InvalidOperationException(IncorrectSchedule);
                }
            }
        });

        static object[] Values(object item, string property) => ((IEnumerable)Property(item, property)).Cast<object>().ToArray();

        static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

        static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            2,
            1,
            instructions
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "ConstructSendTupleInArgument",
            instructions,
            body
        );
    }
}
