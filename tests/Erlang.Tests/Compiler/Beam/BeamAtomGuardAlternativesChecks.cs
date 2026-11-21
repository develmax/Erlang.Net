using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAtomGuardAlternativesChecks
{
    internal const string TestName = "compiler/beam/dispatches-atom-only-guard-alternatives";
    private const string Fixture = "beam_guard_atom_alternatives.erl";
    private const string Entry = "dispatch";
    private const string EntryFixture = "frame_distance_guard_probe.erl";
    private const string WrongEntryFrame = "A projected entry guard must keep its X selector and existing frame width.";
    // Adapted from OTP 29.1.1 crashdump_viewer and ets. Copyright Ericsson AB 1996–2026. Apache-2.0.
    private const string EntrySource = """
        -module(frame_distance_guard_probe).
        -export([run_find/1, verify_count/1]).

        run_find(Entry) -> find(Entry).

        find({Tag,_Id}) when Tag==atoms;
                             Tag==binary;
                             Tag==instr_data;
                             Tag==memory_status;
                             Tag==memory_map ->
            put(truncated_proc,false);
        find({Tag,Pid}) ->
            case is_proc_tag(Tag) of
                true -> put(truncated_proc,Pid);
                false -> put(truncated_proc,"<0.0.0>")
            end.

        is_proc_tag(Tag) when Tag==proc;
                              Tag==proc_dictionary;
                              Tag==proc_messages;
                              Tag==proc_stack;
                              Tag==proc_heap -> true;
        is_proc_tag(_) -> false.

        count([]) ->
            0;
        count([{Tag,_}|T]) when Tag =:= name;
                               Tag =:= type;
                               Tag =:= protection;
                               Tag =:= named_table;
                               Tag =:= keypos;
                               Tag =:= size ->
            1+count(T);
        count([_|T]) ->
            count(T).

        verify_count(L) ->
            count(L) =:= 6.

        """;

    private const int IfLine = 6;
    private const string WrongLayout = "Atom guard alternatives did not retain their selector, common destination and failure edge.";
    private const string WrongResult = "Guard selection changed an alternative, its fallback, or the if failure location.";
    private const string SourceGuard = "Key == zeta; alpha =:= Key; Key == beta";

    private const string Source = """
        -module(beam_guard_atom_alternatives).
        %% options
        -export([dispatch/2]).

        dispatch({wrapped, Key}, GuardInput) ->
            if
                Key == zeta; alpha =:= Key; Key == beta ->
                    {selected, Key, GuardInput}
            end;
        dispatch(Value, GuardInput) ->
            {fallback, Value, GuardInput}.
        """;

    internal static async Task Run()
    {
        byte[] entry = CodeGeneration.CompileBeamModule(EntrySource, EntryFixture);
        if (!HasEntryProjectionSelection(entry, "find", 1) || !HasEntryProjectionSelection(entry, "count", 2))
        {
            throw new InvalidOperationException(WrongEntryFrame);
        }

        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] disabled = BeamAdjacentNumericResultChecks.Compile(
            Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            disabled: true
        );
        byte[] compound = CodeGeneration.CompileBeamModule(
            Source.Replace(
                SourceGuard,
                "element(1, GuardInput) =:= yes, Key == zeta; alpha =:= Key; Key == beta",
                StringComparison.Ordinal
            ),
            Fixture
        );
        byte[] numeric = CodeGeneration.CompileBeamModule(
            Source.Replace(SourceGuard, "Key == 1; alpha =:= Key; Key == beta", StringComparison.Ordinal),
            Fixture
        );

        if (!HasMembershipTable(image) || HasMembershipTable(disabled) || HasMembershipTable(compound)
            || HasMembershipTable(numeric))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        var noSsa = BeamModule.Prepare(disabled);
        var mixed = BeamModule.Prepare(compound);
        var numericModule = BeamModule.Prepare(numeric);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (string value in new[] { "alpha", "beta", "zeta" })
            {
                Term key = Term.A(value);
                Term wrapped = Wrap(key);
                Term expected = Selected(key, Nil.Value);
                if (!expected.Equals(await module.Invoke(
                    context,
                    Entry,
                    wrapped,
                    Nil.Value
                ))
                    || !expected.Equals(await noSsa.Invoke(
                        context,
                        Entry,
                        wrapped,
                        Nil.Value
                    )))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            Term alpha = Term.A("alpha");
            Term zeta = Term.A("zeta");
            Term passInput = Term.Tuple(Term.A("yes"));
            Term floatKey = new FloatTerm(1.0);
            Term other = Term.A("other");
            Term fallback = Term.Tuple(Term.A("fallback"), other, Nil.Value);
            if (!Selected(alpha, Nil.Value).Equals(await mixed.Invoke(
                context,
                Entry,
                Wrap(alpha),
                Nil.Value
            ))
                || !Selected(zeta, passInput).Equals(await mixed.Invoke(
                    context,
                    Entry,
                    Wrap(zeta),
                    passInput
                ))
                || !Selected(floatKey, Nil.Value).Equals(await numericModule.Invoke(
                    context,
                    Entry,
                    Wrap(floatKey),
                    Nil.Value
                ))
                || !fallback.Equals(await module.Invoke(
                    context,
                    Entry,
                    other,
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            await ExpectIfFailure(module, other);
            await ExpectIfFailure(noSsa, other);
            await ExpectIfFailure(mixed, zeta);

            return Nil.Value;

            async Task ExpectIfFailure(BeamModule target, Term key)
            {
                try
                {
                    _ = await target.Invoke(
                        context,
                        Entry,
                        Wrap(key),
                        Nil.Value
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(ErlangErrorReasons.IfClause))
                    && error.StackTraceTerm is Cons { Head: TupleTerm { Items: [_, Atom { Name: Entry }, _, var position] } }
                    && Cons.Items(position).Contains(Term.Tuple(Term.A("file"), Term.String(Fixture)))
                    && Cons.Items(position).Contains(Term.Tuple(Term.A("line"), Term.I(IfLine))))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static bool HasEntryProjectionSelection(byte[] image, string function, int width)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        bool inFunction = false;
        bool foundFrame = false;
        bool foundSelector = false;
        foreach (object instruction in Items(code, "Instructions"))
        {
            byte opcode = Opcode(instruction);
            if (opcode == FunctionInfo)
            {
                if (inFunction)
                {
                    break;
                }

                var metadata = Items(instruction, "Operands");
                inFunction = atoms[Value(metadata[1])] == function && Value(metadata[2]) == 1;
                continue;
            }

            if (!inFunction)
            {
                continue;
            }

            if (opcode is Allocate or AllocateHeap)
            {
                if (foundFrame || Value(Items(instruction, "Operands")[0]) != width)
                {
                    return false;
                }

                foundFrame = true;
            }

            if (opcode == SelectValue)
            {
                var selector = Items(instruction, "Operands")[0];
                foundSelector = Equals(Property(selector, "Kind"), "x");
            }
        }

        return foundFrame && foundSelector;
    }

    private static bool HasMembershipTable(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        foreach (object instruction in instructions.Where(instruction => Opcode(instruction) == SelectValue))
        {
            if (Items(instruction, "Operands") is not
                [var selector, var failure, _, var first, var firstTarget, var second, var secondTarget, var third, var thirdTarget]
                || Property(selector, "Kind") is not ("x" or "y")
                || Value(firstTarget) != Value(secondTarget) || Value(firstTarget) != Value(thirdTarget)
                || Value(firstTarget) == Value(failure))
            {
                continue;
            }

            if (new[] { first, second, third }.All(item => Equals(Property(item, "Kind"), "atom"))
                && new[] { atoms[Value(first)], atoms[Value(second)], atoms[Value(third)] }
                    .SequenceEqual(new[] { "alpha", "beta", "zeta" }))
            {
                return true;
            }
        }

        return false;
    }

    private static Term Wrap(Term key) => Term.Tuple(Term.A("wrapped"), key);

    private static Term Selected(Term key, Term guardInput) => Term.Tuple(Term.A("selected"), key, guardInput);

    private static int Value(object value) => (int)(BigInteger)Property(value, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
