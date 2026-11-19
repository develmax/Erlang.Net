using System.Collections;
using System.Numerics;
using System.Text;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDirectNamedSelfChecks
{
    internal const string TestName = "compiler/beam/named-self-materializes-only-on-value-use";
    private const string Module = "beam_direct_named_self";
    private const string Fixture = Module + ".erl";
    private const string SelfCreation = "The named recursion body did not preserve its self-value materialization contract.";
    private const string ChangedResult = "The named closure changed its recursion, scope or exception behavior.";

    internal static async Task Run()
    {
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(Fixture));
        byte[] image = CodeGeneration.CompileBeamModule(source, Fixture);
        object code = Code(image);
        object[] body = Items(code, "Instructions");
        object[] functions = Items(code, "Functions");
        object[] lambdas = Items(code, "Lambdas");
        string[] direct = ["direct", "captured", "outer_shadow", "nested_shadow", "protected"];
        string[] escaped = ["self_escape", "nested_capture", "dynamic", "wrong_arity"];
        foreach (string name in direct.Concat(escaped))
        {
            object function = functions.Single(function =>
                ((string)Property(function, "Name")).StartsWith("-" + name + "/", StringComparison.Ordinal)
                && ((string)Property(function, "Name")).Contains("-Loop/1-", StringComparison.Ordinal));
            int label = (int)Property(function, "EntryLabel");
            int lambda = Array.FindIndex(lambdas, candidate => (int)Property(candidate, "Label") == label);
            if (lambda < 0)
            {
                throw new InvalidOperationException(SelfCreation + " " + name);
            }

            int first = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");
            bool createsSelf = body[first..end].Any(instruction =>
                (byte)Property(Property(instruction, "Definition"), "Opcode") == BeamEmissionFormat.MakeFun
                && (BigInteger)Property(Items(instruction, "Operands")[0], "Value") == lambda);
            if (createsSelf != escaped.Contains(name))
            {
                throw new InvalidOperationException(SelfCreation + " " + name);
            }
        }

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term one = Term.A("one");
            Term done = Term.A("done");
            Term values = Term.List(one, one);
            (string Function, Term[] Arguments, Term Expected)[] cases =
            [
                ("direct", [values], done),
                ("captured", [values, one], values),
                ("outer_shadow", [values, one], Term.Tuple(one, done)),
                ("nested_shadow", [one], Term.Tuple(one, Term.A("value"))),
                ("protected", [Term.List(one)], Term.Tuple(Term.A("ok"), done)),
                ("protected", [Term.List(one, Term.A("signal"))], Term.A("recovered")),
                ("self_escape", [values], Term.A("true")),
                ("nested_capture", [values], Term.A("true")),
                ("dynamic", [Term.List(Nil.Value)], done),
                ("wrong_arity", [], Term.A("caught"))
            ];
            foreach (var (function, arguments, expected) in cases)
            {
                if (!expected.Equals(await module.Invoke(context, function, arguments)))
                {
                    throw new InvalidOperationException(ChangedResult + " " + function);
                }
            }

            return Nil.Value;
        });
    }

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();
}
