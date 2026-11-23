using System.Collections;
using System.Numerics;
using System.Text;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFlatTupleListHelperChecks
{
    internal const string TestName = "compiler/beam/flat-tuple-list-generator-constructs-after-recursion";
    private const string Module = "beam_flat_tuple_list_helper";
    private const string Fixture = Module + ".erl";
    private const string ChangedSchedule = "The flat tuple list traversal helper was not emitted with its deferred tuple.";
    private const string ChangedHelper = "Flat list output did not use the expected recursive helper topology.";
    private const string ChangedBehavior = "The flat tuple list helper changed result order, capture or failure behavior.";

    internal static async Task Run()
    {
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(Fixture));
        byte[] image = CodeGeneration.CompileBeamModule(source, Fixture);
        object code = Code(image);
        AssertSchedule(code, "-transform/1-lc$^0/1-0-", 7);
        AssertSchedule(code, "-boxed/1-lc$^0/1-0-", 4);

        var helpers = Items(code, "Functions").Select(function =>
            ((string)Property(function, "Name"), (int)Property(function, "Arity"))).ToHashSet();
        if (!helpers.Contains(("-prefixed/2-lc$^0/1-0-", 2))
            || !helpers.Contains(("-paired/2-lc$^0/1-0-", 2))
            || !helpers.Contains(("-branch_element/2-lc$^0/1-0-", 2)))
        {
            throw new InvalidOperationException(ChangedHelper);
        }

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);

        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term beta = Term.A("beta");
            Term tail = Term.A("tail");
            Term tag = Term.A("tag");
            Term seven = new Integer(7);
            Term values = Term.List(alpha, beta);
            Term improper = new Cons(alpha, new Cons(beta, tail));
            Term transformed = Term.List(
                Term.Tuple(
                    tag,
                    alpha,
                    seven,
                    alpha
                ),
                Term.Tuple(
                    tag,
                    beta,
                    seven,
                    beta
                )
            );
            (string Function, Term[] Arguments, Term Expected)[] cases =
            [
                ("transform", [values], transformed),
                ("boxed", [values], Term.List(Term.Tuple(alpha), Term.Tuple(beta))),
                ("captured", [values, tag], Term.List(Term.Tuple(tag, alpha), Term.Tuple(tag, beta))),
                ("transform", [Nil.Value], Nil.Value),
                ("prefixed", [Nil.Value, Term.List(tag)], Nil.Value),
                ("paired", [Nil.Value, tag], Nil.Value),
                ("branch_element", [Term.List(Term.Tuple(Term.A("value"), alpha), beta), tag],
                    Term.List(Term.Tuple(tag, alpha), Term.Tuple(tag, beta))),
                ("bad_input", [improper], tail),
                ("bad_input", [tail], tail),
                ("observed", [improper], Term.Tuple(Term.Tuple(Term.A("bad_generator"), tail), Term.List(beta, alpha))),
                ("observed", [Term.List(alpha, Term.A("stop"), beta)],
                    Term.Tuple(Term.A("selected"), Term.List(Term.A("stop"), alpha)))
            ];
            foreach (var (function, arguments, expected) in cases)
            {
                if (!expected.Equals(await module.Invoke(context, function, arguments)))
                {
                    throw new InvalidOperationException(ChangedBehavior + " " + function);
                }
            }

            Term prefix = Term.List(tag);
            Term badGenerator = Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), tail);
            Term badInput = Term.Tuple(badGenerator, Term.List(beta, alpha));
            Term earlyFailure = Term.Tuple(Term.A("selected"), Term.List(Term.A("stop"), alpha));
            Term prefixed = Term.List(Term.List(tag, alpha), Term.List(tag, beta));
            Term paired = Term.List(Term.Tuple(Term.List(alpha), tag), Term.Tuple(Term.List(beta), tag));
            if (!badInput.Equals(await module.Invoke(
                context,
                "flat_failure",
                improper,
                prefix
            ))
                || !earlyFailure.Equals(await module.Invoke(
                    context,
                    "flat_failure",
                    Term.List(alpha, Term.A("stop"), beta),
                    prefix
                ))
                || !prefixed.Equals(await module.Invoke(
                    context,
                    "prefixed",
                    values,
                    prefix
                ))
                || !paired.Equals(await module.Invoke(
                    context,
                    "paired",
                    values,
                    tag
                )))
            {
                throw new InvalidOperationException(ChangedBehavior);
            }

            return Nil.Value;
        });
    }

    private static void AssertSchedule(object code, string name, int words)
    {
        object? function = Items(code, "Functions").SingleOrDefault(function => (string)Property(function, "Name") == name);
        if (function is null)
        {
            throw new InvalidOperationException(ChangedSchedule);
        }

        int first = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        object[] body = Items(code, "Instructions")[first..end];
        int call = Array.FindIndex(body, instruction => IsOpcode(instruction, BeamEmissionFormat.LocalCall));
        object frame = body.Single(instruction => IsOpcode(instruction, BeamEmissionFormat.Allocate));
        if (call < 0 || call + 3 >= body.Length
            || !IsOpcode(body[call + 1], BeamEmissionFormat.TestHeap)
            || !IsOpcode(body[call + 2], BeamEmissionFormat.PutTuple)
            || !IsOpcode(body[call + 3], BeamEmissionFormat.PutList)
            || Value(body[call + 1], 0) != words || Value(body[call + 1], 1) != BigInteger.One
            || Value(frame, 0) != BigInteger.One || Value(frame, 1) != BigInteger.One)
        {
            throw new InvalidOperationException(ChangedSchedule);
        }
    }

    private static bool IsOpcode(object instruction, byte opcode) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode") == opcode;

    private static BigInteger Value(object instruction, int index) =>
        (BigInteger)Property(Items(instruction, "Operands")[index], "Value");

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();
}
