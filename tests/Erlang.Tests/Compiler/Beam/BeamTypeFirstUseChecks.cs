using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTypeFirstUseChecks
{
    internal const string TestName = "compiler/beam/type-ordinals-follow-final-code-operand-order";
    private const string WrongOrder = "Expected each type index to follow its first encoded use.";
    private const string WrongValue = "Reassigning type indices changed the compiled expression.";
    private const int IntegerType = 1 << 5;
    private const int MapType = 1 << 6;
    private const long OtpSizeUpperBound = (1L << 58) - 1;
    private const string Source = """
        -module(type_first_use_probe).
        -export([late/1, pick/1, again/1]).

        late(Value) -> map_size(Value) - 1.
        pick(#{alpha:=A,beta:=B}) -> {A,B};
        pick(_) -> missing.
        again(Value) -> map_size(Value) - 2.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "type_first_use_probe.erl");
        object code = Code(image);
        object structure = Property(code, "Structure");
        object table = Call(Type("BeamTypeTableReader"), "Read", structure);
        var types = ((IEnumerable)Property(table, "Entries")).Cast<object>().ToArray();
        int[] uses = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .SelectMany(instruction => ((IEnumerable)Property(instruction, "Operands")).Cast<object>())
            .Select(operand => Property(operand, "TypeIndex")).OfType<int>().ToArray();
        if (types.Length != 3 || !uses.SequenceEqual([1, 2, 2, 1])
            || (int)Property(types[1], "TypeUnion") != IntegerType
            || (long)Property(types[1], "Minimum") != 0
            || (long)Property(types[1], "Maximum") != OtpSizeUpperBound
            || (int)Property(types[2], "TypeUnion") != MapType)
        {
            throw new InvalidOperationException(WrongOrder);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term map = new MapTerm([new(Term.A("alpha"), Term.I(3)), new(Term.A("beta"), Term.I(4))]);
            if (!(await module.Invoke(context, "late", map)).Equals(Term.I(1))
                || !(await module.Invoke(context, "again", map)).Equals(Term.I(0))
                || !(await module.Invoke(context, "pick", map)).Equals(Term.Tuple(Term.I(3), Term.I(4)))
                || !(await module.Invoke(context, "pick", Term.A("atom"))).Equals(Term.A("missing")))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });
    }
}
