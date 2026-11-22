using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAliasedEntryFieldUseChecks
{
    internal const string TestName = "compiler/beam/omits-unused-fields-from-aliased-tuple-entry";
    private const string Fixture = "beam_aliased_entry_field_use.erl";
    private const string WrongProjection = "An aliased tuple must project its guard fields and may omit its unused binding.";
    private const string WrongValue = "The matched tuple alias, guard or repeated binding changed value.";

    private const string Source = """
        -module(beam_aliased_entry_field_use).
        -export([unused/2, guarded/2, repeated/2]).

        unused({entry, _Line, Name, 0}=Form, Fallback) ->
            Text = atom_to_list(Name),
            case Text of
                "kept" -> Form;
                _ -> Fallback
            end;
        unused({entry, _, wrapper, 1}, _) -> wrapper_match;
        unused(_, Fallback) -> Fallback.

        guarded({entry, Line, Name, 0}=Form, Fallback) when is_integer(Line) ->
            Text = atom_to_list(Name),
            case Text of
                "kept" -> Form;
                _ -> Fallback
            end;
        guarded({entry, _, wrapper, 1}, _) -> wrapper_match;
        guarded(_, Fallback) -> Fallback.

        repeated({entry, Line, Name, 0}=Form, Line) ->
            Text = atom_to_list(Name),
            case Text of
                "kept" -> Form;
                _ -> Line
            end;
        repeated(_, Fallback) -> Fallback.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        if (ProjectsLine(image, "unused") || !ProjectsLine(image, "guarded")
            || !ProjectsLine(image, "repeated"))
        {
            throw new InvalidOperationException(WrongProjection);
        }

        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term entry = Term.A("entry");
            Term kept = Term.A("kept");
            Term opaque = Term.A("opaque");
            Term integer = Term.I(7);
            Term fallback = Term.A("fallback");
            Term free = Term.Tuple(
                entry,
                opaque,
                kept,
                Term.I(0)
            );
            Term checkedLine = Term.Tuple(
                entry,
                integer,
                kept,
                Term.I(0)
            );
            await Check(
                "unused",
                free,
                fallback,
                free
            );
            await Check(
                "guarded",
                free,
                fallback,
                fallback
            );
            await Check(
                "guarded",
                checkedLine,
                fallback,
                checkedLine
            );
            await Check(
                "repeated",
                checkedLine,
                integer,
                checkedLine
            );
            await Check(
                "repeated",
                checkedLine,
                fallback,
                fallback
            );

            return Nil.Value;

            async Task Check(
                string function,
                Term value,
                Term otherwise,
                Term expected
            )
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    value,
                    otherwise
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }
        });
    }

    private static bool ProjectsLine(byte[] image, string name)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var function, _]
                && atoms[Value(function)] == name
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(WrongProjection);
        }

        return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Any(instruction => Opcode(instruction) == GetTupleElement
                && Items(instruction, "Operands") is [_, var field, _] && Value(field) == 1);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
