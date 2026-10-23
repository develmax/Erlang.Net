using System.Linq.Expressions;
using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class DriverOwnTerm455Checks
{
    internal static Task Run()
    {
        Assembly assembly = typeof(ProcessRuntime).Assembly;
        Type tokensType = assembly.GetType(DriverOwnTerm455Names.Tokens)!;
        Type bindingType = assembly.GetType(DriverOwnTerm455Names.Binding)!;
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
        using var tokens = (IDisposable)Activator.CreateInstance(tokensType, nonPublic: true)!;
        var foreign = new PortTerm(DriverOwnTerm455Names.OldNode, DriverOwnTerm455Names.OwnPort);
        object token = tokensType.GetMethod(DriverOwnTerm455Names.Port, instance)!.Invoke(tokens, [DriverOwnTerm455Names.OwnPort, foreign])!;
        Term own = (Term)tokensType.GetMethod(DriverOwnTerm455Names.Resolve, instance)!.Invoke(tokens, [token])!;
        object capture = tokensType.GetMethod(DriverOwnTerm455Names.Capture, instance)!.Invoke(tokens, [Term.Tuple(own, foreign)])!;
        byte[] bytes = (byte[])capture.GetType().GetProperty(DriverOwnTerm455Names.Bytes)!.GetValue(capture)!;
        var bindings = (Array)capture.GetType().GetProperty(DriverOwnTerm455Names.Bindings)!.GetValue(capture)!;
        Require(bindings.Length == 1 && (int)bindingType.GetProperty(DriverOwnTerm455Names.Occurrence)!.GetValue(bindings.GetValue(0))! == 0);
        var current = new PortTerm(DriverOwnTerm455Names.NewNode, DriverOwnTerm455Names.OwnPort, 1);
        Type resolverType = typeof(Func<,>).MakeGenericType(bindingType, typeof(Term));
        var parameter = Expression.Parameter(bindingType);

        Delegate Resolver(Term term) => Expression.Lambda(resolverType, Expression.Constant(term, typeof(Term)), parameter).Compile();
        MethodInfo decode = assembly.GetType(DriverOwnTerm455Names.Codec)!.GetMethod(DriverOwnTerm455Names.Decode,
            BindingFlags.Static | BindingFlags.NonPublic)!;

        Term Decode(byte[] raw, Array selected, Term target) => (Term)decode.Invoke(null, [raw, selected, Resolver(target)])!;
        Require(Decode(bytes, bindings, current).Equals(Term.Tuple(current, foreign)));

        // Duplicate wire spellings must be rebound before map insertion. Build
        // a valid two-key map, then give its second key the first key's bytes.
        var other = new PortTerm(DriverOwnTerm455Names.OldNode, DriverOwnTerm455Names.OtherPort);
        byte[] mapBytes = ExternalTermFormat.Encode(new MapTerm([
            new KeyValuePair<Term, Term>(foreign, Term.I(1)),
            new KeyValuePair<Term, Term>(other, Term.I(2))]));
        byte[] oldKey = ExternalTermFormat.Encode(foreign)[1..];
        byte[] otherKey = ExternalTermFormat.Encode(other)[1..];
        int offset = mapBytes.AsSpan().IndexOf(otherKey);
        Require(offset >= 0 && oldKey.Length == otherKey.Length);
        oldKey.CopyTo(mapBytes, offset);
        var restored = (MapTerm)Decode(mapBytes, bindings, current);
        Require(restored.Entries.Count == 2 && restored.Get(current).Equals(Term.I(1)) && restored.Get(foreign).Equals(Term.I(2)));

        Array Rows(params (int Occurrence, bool Actor, ulong Handle)[] rows)
        {
            Array result = Array.CreateInstance(bindingType, rows.Length);
            for (int index = 0; index < rows.Length; index++)
                result.SetValue(Activator.CreateInstance(
                    bindingType,
                    rows[index].Occurrence,
                    rows[index].Actor,
                    rows[index].Handle
                ), index);

            return result;
        }
        foreach (Array invalid in new[] {
            Rows((-1, false, DriverOwnTerm455Names.OwnPort)),
            Rows((0, false, 0)),
            Rows((0, false, DriverOwnTerm455Names.OwnPort), (0, false, DriverOwnTerm455Names.OwnPort)),
            Rows((0, true, DriverOwnTerm455Names.OwnPort)),
            Rows((2, false, DriverOwnTerm455Names.OwnPort))})
        {
            try
            {
                _ = Decode(bytes, invalid, current);
                throw new InvalidOperationException(DriverOwnTerm455Names.Failure);
            }
            catch (TargetInvocationException failure) when (failure.InnerException is InvalidDataException
                || failure.InnerException is ErlangException guest && guest.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            { }
        }
        Term longList = Nil.Value;
        for (int index = 0; index < DriverOwnTerm455Names.LongListLength; index++) longList = new Cons(Term.I(0), longList);
        _ = tokensType.GetMethod(DriverOwnTerm455Names.Capture, instance)!.Invoke(tokens, [longList]);

        return Task.CompletedTask;
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DriverOwnTerm455Names.Failure);
    }
}
