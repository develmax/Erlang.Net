using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamDictionaryListArgumentOrderChecks
{
    internal const string TestName = "compiler/beam/orders-constant-arguments-after-dictionary-list-producers";
    private const string WrongOrder = "Only independent constant arguments may move after the checked dictionary lookup and cons construction.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_dictionary_list_argument_order");
        _ = Call(compiler, "Intern", "key");
        _ = Call(compiler, "Intern", "first");
        _ = Call(compiler, "Intern", "second");
        _ = Call(
            compiler,
            "Import",
            "erlang",
            "get",
            1
        );
        _ = Call(
            compiler,
            "Import",
            "probe",
            "consume",
            5
        );
        _ = Call(
            compiler,
            "Import",
            "probe",
            "consume",
            4
        );
        if (Apply(Fixture()) is not byte[] ordered || !ordered.AsSpan().SequenceEqual(Fixture(ordered: true))
            || Apply(Fixture(ordered: true)) is not null
            || Apply(Fixture(narrow: true, splitHeap: true, twoCons: true)) is not byte[] nested
            || !nested.AsSpan().SequenceEqual(Fixture(
                ordered: true,
                narrow: true,
                splitHeap: true,
                twoCons: true
            ))
            || Apply(Fixture(overwritesLookup: true)) is not null || Apply(Fixture(otherImport: true)) is not null
            || Apply(Fixture(lineBarrier: true)) is not null || Apply(Fixture(dependsOnArgument: true)) is not null)
        {
            throw new InvalidOperationException(WrongOrder);
        }

        return Task.CompletedTask;

        object? Apply(ReadOnlyMemory<byte> code) => Call(
            compiler,
            "OrderDictionaryListArguments",
            code,
            3
        );
    }

    private static byte[] Fixture(
        bool ordered = false,
        bool narrow = false,
        bool splitHeap = false,
        bool twoCons = false,
        bool overwritesLookup = false,
        bool otherImport = false,
        bool lineBarrier = false,
        bool dependsOnArgument = false
    )
    {
        int arity = narrow ? 4 : 5;
        byte[] first = [Move, dependsOnArgument ? X(2) : A(2), X(1)];
        byte[] second = [Move, A(3), X(2)];
        byte[] third = narrow ? [] : [Move, A(4), X(3)];
        byte[] arguments = ordered
            ? [.. second, .. third, .. first]
            : narrow ? [.. first, .. second] : [.. second, .. first, .. third];
        byte[] heap = splitHeap ? [TestHeap, U(twoCons ? 4 : 2), U(1)] : [];
        byte[] prefix = twoCons ? [PutList, X(0), A(0), X(1)] : [];
        byte[] line = lineBarrier ? [Line, U(1)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            BifOne, F(0), U(otherImport ? 1 : 0), A(2), X(0), .. heap, .. prefix,
            PutList, X(0), twoCons ? X(1) : A(0), X(overwritesLookup ? 0 : arity - 1),
            .. line, .. arguments, Line, U(2), ExternalCall, U(arity), U(narrow ? 2 : 1), Return, EndCode
        ];
    }
}
