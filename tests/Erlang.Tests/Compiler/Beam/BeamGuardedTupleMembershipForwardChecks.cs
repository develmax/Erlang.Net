using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedTupleMembershipForwardChecks
{
    internal const string TestName = "compiler/beam/forwards-tuple-aliases-after-a-shared-atom-guard";
    private const string Module = "beam_tuple_guard_forward";
    private const string Peer = "beam_tuple_guard_peer";
    private const string LayoutFailure = "Expected the common field test and atom selection in one scratch register.";
    private const string ValueFailure = "Tuple forwarding changed the accepted value or original function clause arguments.";

    private const string Source = """
        -module(beam_tuple_guard_forward).
        -export([run/2, numeric/2, cross/2]).
        run(Name, {Kind, Opts} = Whole)
          when is_list(Opts), (Kind == zeta orelse alpha =:= Kind) ->
            beam_tuple_guard_peer:forward(Name, Whole).
        numeric(Name, {Kind, Opts} = Whole)
          when is_list(Opts), (Kind == 1 orelse Kind =:= zeta) ->
            beam_tuple_guard_peer:forward(Name, Whole).
        cross(Name, {Kind, Opts} = Whole)
          when is_list(Opts), (Kind == zeta orelse Name =:= alpha) ->
            beam_tuple_guard_peer:forward(Name, Whole).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        CheckCode(image);

        var module = BeamModule.Prepare(image, new BeamDependencySelection([Peer]));
        await using var runtime = new ProcessRuntime();
        runtime.Modules.Register(
            Peer,
            "forward",
            2,
            (_, arguments) =>
            ValueTask.FromResult<Term>(Term.Tuple(arguments[0], arguments[1]))
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term name = Term.A("name");
            Term alpha = Term.A("alpha");
            Term zeta = Term.A("zeta");
            Term other = Term.A("other");
            Term options = Term.List(Term.A("option"));
            await Accepted("run", name, Term.Tuple(alpha, Nil.Value));
            await Accepted("run", name, Term.Tuple(zeta, options));
            await Accepted("numeric", name, Term.Tuple(new FloatTerm(1.0), options));
            await Accepted("cross", alpha, Term.Tuple(other, options));
            await Rejected(Term.Tuple(other, options));
            await Rejected(Term.Tuple(alpha, other));
            await Rejected(Term.Tuple(zeta, options, Nil.Value));

            return Nil.Value;

            async Task Accepted(string function, Term first, Term second)
            {
                Term value = await module.Invoke(
                    context,
                    function,
                    first,
                    second
                );
                if (!value.Equals(Term.Tuple(first, second)))
                {
                    throw new InvalidOperationException(ValueFailure);
                }
            }

            async Task Rejected(Term argument)
            {
                try
                {
                    await module.Invoke(
                        context,
                        "run",
                        name,
                        argument
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause"))
                    && error.StackTraceTerm is Cons
                    {
                        Head: TupleTerm { Items: [Atom { Name: Module }, Atom { Name: "run" }, var inputs, _] }
                    } && inputs.Equals(Term.List(name, argument)))
                {
                    return;
                }

                throw new InvalidOperationException(ValueFailure);
            }
        });
    }

    private static void CheckCode(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int info = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && Operands(instruction) is [_, var name, _] && atoms[Value(name)] == "run"
        );
        object[] body = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) is not (Label or Line or EndCode)).ToArray();
        if (info < 1 || body.Select(Opcode).ToArray() is not
            [IsTuple, TestArity, GetTupleElement, IsList, GetTupleElement, SelectValue, TailExternalCall]
            || Operands(body[2]).Select(Value).ToArray() is not [1, 1, 2]
            || Operands(body[4]).Select(Value).ToArray() is not [1, 0, 2]
            || Operands(body[5]) is not [var scratch, var failure, _, var first, var target, var second, var same]
            || Value(scratch) != 2 || Value(failure) != Value(Operands(body[0])[0]) || Value(target) != Value(same)
            || atoms[Value(first)] != "alpha" || atoms[Value(second)] != "zeta")
        {
            throw new InvalidOperationException(LayoutFailure);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
