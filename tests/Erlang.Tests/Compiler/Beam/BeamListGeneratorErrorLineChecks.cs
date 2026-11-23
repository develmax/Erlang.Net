using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamListGeneratorErrorLineChecks
{
    internal const string TestName = "compiler/beam/list-generator-error-line";
    private const string MissingLocation = "The invalid list generator did not use the source expression line.";
    private const string DuplicateLocation = "The same-line list generator emitted a redundant error line.";
    private const string UnreachableFailure = "The proper literal list generator retained an invalid source branch.";
    private const string WrongLiteralHelpers = "Fixed literal list bindings did not retain the expected generator helpers.";

    private const string Source = """
        -module(beam_list_generator_error_line).
        -export([literal/1, computed/1, direct/2, same_line/2, split_input/2, grouped_direct/2, grouped_call/1,
                 literal_siblings/1, improper_sibling/2]).

        literal(Sink) ->
            [event:put(Sink, "Item " ++ integer_to_list(I)) ||
                I <- [1,2,3]],
            ok.

        computed(Sink) ->
            [event:put(Sink, "Item " ++ integer_to_list(I)) ||
                I <- event:rows(Sink)],
            ok.

        direct(Sink, Rows) ->
            [event:put(Sink, "Item " ++ integer_to_list(I)) ||
                I <- Rows],
            ok.

        same_line(Sink, Rows) ->
            [event:put(Sink, integer_to_list(I)) || I <- Rows],
            ok.

        split_input(Sink, Rows) ->
            [event:put(Sink, integer_to_list(I)) ||
                I <-
                    Rows],
            ok.

        grouped_direct(Sink, Rows) ->
            [event:put(Sink, integer_to_list(I)) ||
                I <- ((
                    Rows))],
            ok.

        grouped_call(Sink) ->
            [event:put(Sink, integer_to_list(I)) ||
                I <- (
                    event:rows(Sink))],
            ok.

        literal_siblings(Sink) ->
            [A, B] = [event:put(Sink, V) || V <- [first, second]],
            [C, D, E] = [event:put(Sink, V) || V <- [third, fourth, fifth]],
            {A, B, C, D, E}.

        improper_sibling(Sink, Tail) ->
            [A, B] = [event:put(Sink, V) || V <- [first, second]],
            [C, D] = [event:put(Sink, V) || V <- [third, fourth | Tail]],
            {A, B, C, D}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_list_generator_error_line.erl"));
        object[] instructions = Items(code, "Instructions");
        object[] functions = Items(code, "Functions");
        Term[] locations = Items(code, "SourceLocations").Cast<Term>().ToArray();

        RequireErrorLine("computed", 12);
        RequireErrorLine("direct", 17);
        RequireErrorLine("split_input", 27);
        RequireErrorLine("grouped_direct", 33);
        RequireErrorLine("grouped_call", 39);

        object[] sameLine = Body("same_line");
        if (sameLine.Length < 2 || Opcode(sameLine[^2]) != PutTuple || Opcode(sameLine[^1]) != TailExternalCall)
        {
            throw new InvalidOperationException(DuplicateLocation);
        }

        if (Body("literal").Any(instruction => Opcode(instruction) == PutTuple))
        {
            throw new InvalidOperationException(UnreachableFailure);
        }

        if (!HasHelper("-literal_siblings/1-lc$^0/1-0-")
            || !HasHelper("-literal_siblings/1-lc$^1/1-1-")
            || !HasHelper("-improper_sibling/2-lc$^0/1-0-")
            || HasHelper("-improper_sibling/2-lc$^1/1-1-"))
        {
            throw new InvalidOperationException(WrongLiteralHelpers);
        }

        bool HasHelper(string name) => functions.Any(function => (string)Property(function, "Name") == name);

        void RequireErrorLine(string name, int line)
        {
            object[] body = Body(name);
            if (body.Length < 3 || Opcode(body[^3]) != PutTuple || Opcode(body[^2]) != Line
                || Opcode(body[^1]) != TailExternalCall)
            {
                throw new InvalidOperationException(MissingLocation);
            }

            int index = (int)(BigInteger)Property(Items(body[^2], "Operands")[0], "Value");
            if (!Cons.Items(locations[index]).Contains(Term.Tuple(Term.A("line"), Term.I(line))))
            {
                throw new InvalidOperationException(MissingLocation);
            }
        }

        object[] Body(string name)
        {
            object function = functions.Single(candidate =>
                ((string)Property(candidate, "Name")).StartsWith($"-{name}/", StringComparison.Ordinal));
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");

            return instructions[start..end];
        }
    }

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
