using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamIntegerTailSelectorChecks
{
    internal const string TestName = "compiler/beam/groups-bounded-integer-tail-selector-destinations";
    private const string Fixture = "beam_integer_tail_selector_probe.erl";
    private const string WrongLayout = "The bounded integer selector did not share terminal labels in reverse postorder.";
    private const string WrongControl = "An unproved selector acquired a bounded operand.";
    private const string WrongValue = "Integer selector layout changed the returned term or failure.";

    internal const string Source = """
        -module(beam_integer_tail_selector_probe).
        -export([start/2, open/2, escaping/0, escaped_entry/1, unknown_entry/2]).

        start(one, V) -> forward([1], V);
        start(two, V) -> forward([2], V);
        start(three, V) -> forward([3], V);
        start(four, V) -> forward([4], V);
        start(other, V) -> forward([5], V).
        forward(Stack, V) -> choice(hd(Stack), V).
        choice(1=S, V) -> shared(S,V);
        choice(2=S, V) -> shared(S,V);
        choice(3, V) -> alternate(17,V);
        choice(4=S, V) -> shared(S,V).

        open(1=S, V) -> shared(S,V);
        open(2=S, V) -> shared(S,V);
        open(3, V) -> alternate(17,V);
        open(4=S, V) -> shared(S,V).

        escaping() -> fun escaped/2.
        escaped_entry(V) -> escaped(1, V).
        escaped(1=S, V) -> shared(S,V);
        escaped(2=S, V) -> shared(S,V);
        escaped(3, V) -> alternate(17,V);
        escaped(4=S, V) -> shared(S,V).

        unknown_entry(Stack,V) -> unknown_choice(hd(Stack),V).
        unknown_choice(1=S, V) -> shared(S,V);
        unknown_choice(2=S, V) -> shared(S,V);
        unknown_choice(3, V) -> alternate(17,V);
        unknown_choice(4=S, V) -> shared(S,V).

        shared(State,V) -> {shared,State,V}.
        alternate(State,V) -> {alternate,State,V}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        CheckDeferredClosureEntryMapping();
        byte[] image = CompileFixture();
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] types = Items(Property(code, "Types"), "Entries").ToArray();
        object[] choice = Body("choice");
        object[] selector = Operands(choice.Single(operation => Opcode(operation) == SelectValue));
        int[] labels = selector.Skip(4).Where((_, index) => index % 2 == 0).Select(Value).ToArray();
        int[] destinations = choice.Where(operation => Opcode(operation) == TailLocalCall)
            .Select(operation => Value(Operands(operation)[1])).ToArray();
        if (Property(selector[0], "TypeIndex") is not int type
            || Property(types[type], "Minimum") is not 1L || Property(types[type], "Maximum") is not 5L
            || labels.Length != 4 || labels[0] != labels[1] || labels[0] == labels[2] || labels[0] != labels[3]
            || !destinations.SequenceEqual([Entry("alternate"), Entry("shared")]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        if (new[] { "open", "escaped", "unknown_choice" }.SelectMany(Body)
            .Where(operation => Opcode(operation) == SelectValue)
            .Any(operation => Property(Operands(operation)[0], "TypeIndex") is not null))
        {
            throw new InvalidOperationException(WrongControl);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term payload = Term.A("payload");
            Term first = Term.Tuple(Term.A("shared"), Term.I(1), payload);
            Term second = Term.Tuple(Term.A("shared"), Term.I(2), payload);
            Term alternate = Term.Tuple(Term.A("alternate"), Term.I(17), payload);
            if (!first.Equals(await module.Invoke(
                context,
                "start",
                Term.A("one"),
                payload
            ))
                || !second.Equals(await module.Invoke(
                    context,
                    "start",
                    Term.A("two"),
                    payload
                ))
                || !alternate.Equals(await module.Invoke(
                    context,
                    "start",
                    Term.A("three"),
                    payload
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "start",
                    Term.A("other"),
                    payload
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);
        });

        int Info(string name) => Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && Operands(instruction) is [_, var atom, _] && atoms[Value(atom)] == name
        );

        int Entry(string name) => Value(Operands(instructions[Info(name) + 1])[0]);

        object[] Body(string name) =>
            instructions.Skip(Info(name) + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static void CheckDeferredClosureEntryMapping()
    {
        byte[] prefix = Start(
            1,
            2,
            2,
            2
        );
        byte[] suffix =
        [
            .. Start(
                5,
                6,
                3,
                2
            ),
            Return,
            .. Start(
                7,
                8,
                4,
                2
            ),
            Return,
            EndCode
        ];
        byte[] comparisonBytes =
        [
            .. prefix,
            ExactEqualTest, F(3), X(0), I(1),
            TailLocalCall, U(2), F(8),
            Label, U(3),
            ExactEqualTest, F(4), X(0), I(2),
            TailLocalCall, U(2), F(8),
            Label, U(4), Jump, F(1),
            .. suffix
        ];
        ReadOnlyMemory<byte> comparisons = comparisonBytes;
        byte[] expected =
        [
            .. prefix,
            SelectValue, ExtendedTypedRegisterOperand, X(0), U(1), F(1), ExtendedListOperand, U(4),
            I(1), F(3), I(2), F(3),
            Label, U(3), TailLocalCall, U(2), F(8),
            .. suffix
        ];

        byte[]? distinctClosure = GroupWithClosure(6);
        byte[]? retainedArm = GroupWithClosure(4);
        if (distinctClosure is null || !distinctClosure.AsSpan().SequenceEqual(expected)
            || retainedArm is null || retainedArm[prefix.Length + 1] != X(0))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[]? GroupWithClosure(int mappedEntry)
        {
            const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.NonPublic;
            var module = new Parser(Source).ParseModule();
            var compiler = new BeamModuleCompiler(module.Name, sourceModule: module);
            Type compilerType = compiler.GetType();
            var atoms = (Dictionary<string, int>)compilerType.GetField("atoms", InstanceMembers)!.GetValue(compiler)!;
            atoms.Add("choice", 2);
            atoms.Add("escaped", 3);
            atoms.Add("shared", 4);
            compilerType.GetField("properListSourceFunctions", InstanceMembers)!.SetValue(
                compiler,
                module.DeclaredFunctions.ToDictionary(function => (function.Name, function.Arity))
            );
            compilerType.GetField("exportedFunctions", InstanceMembers)!.SetValue(compiler, module.DeclaredExports.ToHashSet());

            var deferred = (Dictionary<int, int>)compilerType.GetField("deferredEntries", InstanceMembers)!
                .GetValue(compiler)!;
            deferred.Add(-1, 8);
            int[] remapped = [0, 1, 2, 3, 3, 3, 4, 5, mappedEntry, 7, 8];
            compilerType.GetField("emittedLabels", InstanceMembers)!.SetValue(compiler, remapped);
            FieldInfo closureField = compilerType.GetField("closures", InstanceMembers)!;
            var closures = (IList)closureField.GetValue(compiler)!;
            object closure = RuntimeHelpers.GetUninitializedObject(closureField.FieldType.GetGenericArguments()[0]);
            closure.GetType().GetProperty("Entry", InstanceMembers)!.SetValue(closure, -1);
            closures.Add(closure);

            return (byte[]?)Call(
                compiler,
                "GroupEntryLiteralTests",
                comparisons,
                9
            );
        }
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
