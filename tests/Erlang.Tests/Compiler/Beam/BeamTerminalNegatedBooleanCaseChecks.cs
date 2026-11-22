using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalNegatedBooleanCaseChecks
{
    internal const string TestName = "compiler/beam/selects-terminal-negated-boolean-case-with-badarg";
    private const string Fixture = "beam_terminal_negated_boolean_case.erl";
    private const string WrongLayout = "The negated Boolean selector must preserve the source line and its explicit badarg exit.";
    private const string WrongResult = "Negated Boolean case selection changed the result, error, or evaluation order.";

    private const string Source = """
        -module(beam_terminal_negated_boolean_case).
        %% options
        -export([choose/5, opaque/4]).

        choose({source, Name, 0} = Original, Input, Suffix, Prefix, Tail) ->
            Values = atom_to_list(Name),
            case not probe(Input)
              andalso (lists:suffix(Suffix, Values) orelse
                       lists:prefix(Prefix, Values))
            of
                true -> Values;
                false -> [Original | Tail]
            end;
        choose({source, wrapped, 1}, _, _, _, Tail) -> Tail;
        choose(_, _, _, _, Tail) -> Tail.

        opaque(Input, Prefix, Values, Other) ->
            case not probe(Input) andalso (lists:prefix(Prefix, Values) orelse probe(Other)) of
                true -> Input;
                false -> Other
            end.

        probe(Value) -> erlang:put(terminal_boolean_probe, Value), Value.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] disabled = CodeGeneration.CompileBeamModule(
            Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            Fixture
        );
        int negationLine = Source[..Source.IndexOf("case not probe(Input)", StringComparison.Ordinal)]
            .Count(character => character == '\n') + 1;
        object code = Code(image);
        object[] selected = Function(code, "choose");
        int errorExit = Array.FindIndex(selected, instruction => Opcode(instruction) == TailExternalCallWithFrame);
        var positions = (IReadOnlyList<Term>)Call(Type("BeamLineTableReader"), "Read", Property(code, "Structure"));
        if (errorExit < 1 || Opcode(selected[errorExit - 1]) != Line
            || !Cons.Items(positions[Value(Items(selected[errorExit - 1], "Operands")[0])])
                .Contains(Term.Tuple(Term.A("line"), Term.I(negationLine)))
            || selected.Count(instruction => Opcode(instruction) == Deallocate) != 2
            || selected.Any(instruction => Opcode(instruction) is BifOne or CaseEnd)
            || !Function(code, "opaque").Any(instruction => Opcode(instruction) == BifOne)
            || !Function(Code(disabled), "choose").Any(instruction => Opcode(instruction) == BifOne))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        var libraries = new RuntimeLibrarySelection(
            [RuntimeModuleSelection.Managed("erlang"), RuntimeModuleSelection.Managed("lists")]
        );
        await using var runtime = await ProcessRuntime.CreateAsync(
            libraries,
            RuntimeNativePrimitives.InternalScalars413,
            CancellationToken.None
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term truth = Term.A("true");
            Term falsity = Term.A("false");
            Term invalid = Term.A("invalid");
            Term values = Term.String("ab");
            Term last = Term.String("b");
            Term first = Term.String("a");
            Term missing = Term.String("c");
            Term source = Term.Tuple(Term.A("source"), Term.A("ab"), Term.I(0));
            await Check(
                truth,
                invalid,
                invalid,
                Term.List(source)
            );
            await Check(
                falsity,
                last,
                invalid,
                values
            );
            await Check(
                falsity,
                missing,
                first,
                values
            );
            await Check(
                falsity,
                missing,
                missing,
                Term.List(source)
            );
            try
            {
                await Invoke(invalid, invalid, invalid);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                if (!Cons.Items(error.StackTraceTerm).OfType<TupleTerm>().Any(frame =>
                    frame.Items is [Atom { Name: "beam_terminal_negated_boolean_case" }, Atom { Name: "choose" }, _, var location]
                    && Cons.Items(location).Contains(Term.Tuple(Term.A("line"), Term.I(negationLine)))))
                {
                    throw new InvalidOperationException(WrongResult);
                }

                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);

            async Task Check(
                Term input,
                Term suffix,
                Term prefix,
                Term expected
            )
            {
                if (!expected.Equals(await Invoke(input, suffix, prefix)))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            ValueTask<Term> Invoke(Term input, Term suffix, Term prefix) => module.Invoke(
                context,
                "choose",
                source,
                input,
                suffix,
                prefix,
                Nil.Value
            );
        });
    }

    private static object[] Function(object code, string name)
    {
        object[] instructions = Items(code, "Instructions");
        object[] atoms = Items(Property(code, "Structure"), "Atoms");
        int info = Array.FindIndex(
            instructions,
            instruction =>
            Opcode(instruction) == FunctionInfo && Items(instruction, "Operands") is [_, var function, _]
            && Equals(atoms[Value(function)], name)
        );
        if (info < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
