using System.Collections;
using System.Reflection;
using Erlang;
using static Erlang.Tests.BeamMissingHeaderLineNames;

namespace Erlang.Tests;

internal static class BeamMissingHeaderLineHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage image = runtime.Modules.PrepareBeam(BeamMissingHeaderLineBytes.Image());
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object code = typeof(PreparedBeamImage).GetProperty(Code, instance)!.GetValue(image)!;
        var functions = ((IEnumerable)code.GetType().GetProperty(Functions, instance)!.GetValue(code)!).Cast<object>().ToArray();

        int FirstOf(string name)
        {
            object function = functions.Single(value => (string)value.GetType().GetProperty(Name, instance)!.GetValue(value)! == name);

            return (int)function.GetType().GetProperty(First, instance)!.GetValue(function)!;
        }
        Type type = typeof(ProcessRuntime).Assembly.GetType(Locations, throwOnError: true)!;
        MethodInfo read = type.GetMethod(Read, BindingFlags.Static | BindingFlags.NonPublic)!;
        var locations = (IReadOnlyList<Term>)read.Invoke(null, [code])!;
        int first = FirstOf(BeamOpcodeEdgeNames.Run);
        int second = FirstOf(BeamOpcodeEdgeNames.Middle);
        int third = FirstOf(BeamOpcodeEdgeNames.Leaf);
        Require(HasLine(locations[first], 10));
        Require(locations[second - 2] is Nil && locations[second] is Nil);
        Require(HasLine(locations[second + 1], 12) && HasLine(locations[second + 2], 12));
        Require(locations[third - 2] is Nil && locations[third] is Nil && locations[third + 1] is Nil);
    }

    private static bool HasLine(Term metadata, int line)
        => Cons.Items(metadata).Any(value => value is TupleTerm { Items.Count: 2 } pair
            && pair.Items[0].Equals(Term.A(Line)) && pair.Items[1].Equals(Term.I(line)));

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
