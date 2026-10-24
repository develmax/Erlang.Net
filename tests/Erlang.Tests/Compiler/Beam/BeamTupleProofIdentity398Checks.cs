using static Erlang.Tests.BeamTupleProofIdentity398Names;
using System.Collections;
using System.Reflection;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTupleProofIdentity398Checks
{

    internal static Task WarmedCacheStillRejectsForeignInstruction()
    {
        byte[] bytes = BeamGuardedTuple398Checks.Image();
        object firstCode = Code(bytes);
        object secondCode = Code(bytes);
        object firstInstruction = Bif(firstCode);
        object foreignInstruction = Bif(secondCode);
        object clonedInstruction = typeof(object).GetMethod(CloneMethod, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(firstInstruction, null)!;
        object proof = Type(ProofType);
        Require(!ReferenceEquals(firstInstruction, foreignInstruction));
        Require(!ReferenceEquals(firstInstruction, clonedInstruction));
        Require(Property(firstInstruction, Offset).Equals(Property(foreignInstruction, Offset)));
        Require(Property(firstInstruction, Offset).Equals(Property(clonedInstruction, Offset)));
        // Warm the legitimate positive first: an offset-key cache would now accept both foreign objects.
        Require((bool)Call(
            proof,
            ProofMethod,
            firstCode,
            firstInstruction
        ));
        Require(!(bool)Call(
            proof,
            ProofMethod,
            firstCode,
            foreignInstruction
        ));
        Require(!(bool)Call(
            proof,
            ProofMethod,
            firstCode,
            clonedInstruction
        ));
        Require((bool)Call(
            proof,
            ProofMethod,
            firstCode,
            firstInstruction
        ));
        Require((bool)Call(
            proof,
            ProofMethod,
            secondCode,
            foreignInstruction
        ));

        return Task.CompletedTask;
    }

    private static object Bif(object code)
        => ((IEnumerable)Property(code, Instructions)).Cast<object>()
            .Single(value => (string)Property(Property(value, Definition), Name) == Unary);

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
