using System.Buffers.Binary;
using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAtomOrTupleEntryChecks
{
    internal const string TestName = "compiler/beam/joins-private-atom-and-tuple-selector-entry-types";
    private const string Fixture = "beam_atom_tuple_entry_probe.erl";
    private const string WrongType = "Only the closed mixed callers should annotate the selector's entry operand.";
    private const string WrongResult = "The entry type join changed the accepted or rejected value.";
    private const int AtomOrTupleType = 0x0801;
    private const int TypeLowerBound = 0x2000;
    private const int TypeUpperBound = 0x4000;
    private const int TypeUnit = 0x8000;
    private const string Source = """
        -module(beam_atom_tuple_entry_probe).
        -export([route/2, open/2, open_known/1, use_escaped/1, escape/0, exposed/2]).

        route(Scope, Item) when is_atom(Scope) -> relay(Scope, Item);
        route({Scope,Rest}, Item) when is_atom(Scope) -> relay({Scope,Rest}, Item).

        relay(Scope, Item) -> closed(Scope, Item).
        open(Scope, Item) -> from_unknown(Scope, Item).
        open_known(Item) -> from_unknown(event, Item).
        use_escaped(Item) -> escaped(event, Item).
        escape() -> fun escaped/2.

        closed(Scope, Item) ->
            case Scope of
                event -> case Item of [7,4] -> four; [7,2] -> two; [7,0] -> zero end
            end.

        from_unknown(Scope, Item) ->
            case Scope of
                event -> case Item of [7,4] -> four; [7,2] -> two; [7,0] -> zero end
            end.

        escaped(Scope, Item) ->
            case Scope of
                event -> case Item of [7,4] -> four; [7,2] -> two; [7,0] -> zero end
            end.

        exposed(Scope, Item) ->
            case Scope of
                event -> case Item of [7,4] -> four; [7,2] -> two; [7,0] -> zero end
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        byte[] types = BeamDocumentationEmissionChecks.Chunks(image).Single(chunk => chunk.Id == "Type").Data;
        if (ReadScopeType("closed") != AtomOrTupleType || ReadScopeType("from_unknown") is not null
            || ReadScopeType("escaped") is not null || ReadScopeType("exposed") is not null)
        {
            throw new InvalidOperationException(WrongType);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term input = Term.List(Term.I(7), Term.I(4));
            Term scope = Term.A("event");
            Term tuple = Term.Tuple(scope, Term.A("rest"));
            Term invalid = Term.I(14);
            Term four = Term.A("four");
            if (!four.Equals(await module.Invoke(
                context,
                "route",
                scope,
                input
            ))
                || !four.Equals(await module.Invoke(context, "open_known", input))
                || !four.Equals(await module.Invoke(context, "use_escaped", input)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            await Reject("route", tuple, input);
            await Reject("open", invalid, input);
            await Reject("exposed", invalid, input);

            return Nil.Value;

            async Task Reject(string name, Term rejected, Term value)
            {
                try
                {
                    await module.Invoke(
                        context,
                        name,
                        rejected,
                        value
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.Tuple(Term.A("case_clause"), rejected)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });

        int? ReadScopeType(string function)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == function
            );
            if (entry < 0)
            {
                throw new InvalidOperationException(WrongType);
            }

            object comparison = instructions.Skip(entry + 1)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .First(instruction => Opcode(instruction) == ExactEqualTest);
            object tested = Items(comparison, "Operands").ElementAt(1);
            if (tested.GetType().GetProperty("TypeIndex")!.GetValue(tested) is not int type)
            {
                return null;
            }

            int offset = sizeof(int) * 2;
            for (int index = 0; index < type; index++)
            {
                int flags = BinaryPrimitives.ReadUInt16BigEndian(types.AsSpan(offset));
                offset += sizeof(ushort)
                    + ((flags & TypeLowerBound) != 0 ? sizeof(long) : 0)
                    + ((flags & TypeUpperBound) != 0 ? sizeof(long) : 0)
                    + ((flags & TypeUnit) != 0 ? sizeof(byte) : 0);
            }

            return BinaryPrimitives.ReadUInt16BigEndian(types.AsSpan(offset));
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
