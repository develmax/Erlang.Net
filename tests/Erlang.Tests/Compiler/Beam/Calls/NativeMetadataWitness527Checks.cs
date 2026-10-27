using Erlang;
using static Erlang.Tests.DynamicApplyNativeMetadata527Names;
using static Erlang.Tests.NativeMetadataWitness527Names;

namespace Erlang.Tests;

internal static class NativeMetadataWitness527Checks
{
    internal static Task Run()
    {
        var expected = new BitString(Enumerable.Range(0, Md5Bytes).Select(value => (byte)value).ToArray());
        var wrong = new BitString(new byte[Md5Bytes]);
        Term valid = Term.Tuple(Term.A(WitnessModule), Term.A(PreloadedOwner), expected, Nil.Value);
        DynamicApplyNativeMetadata527Admission.VerifyWitness(valid, expected);
        Reject(Term.Tuple(Term.A(WrongModule), Term.A(PreloadedOwner), expected, Nil.Value), expected);
        Reject(Term.Tuple(Term.A(WitnessModule), Term.A(NonPreloaded), expected, Nil.Value), expected);
        Reject(Term.Tuple(Term.A(WitnessModule), Term.A(PreloadedOwner), wrong, Nil.Value), expected);
        Reject(Term.Tuple(Term.A(WitnessModule), Term.A(PreloadedOwner), expected), expected);

        return Task.CompletedTask;
    }

    private static void Reject(Term witness, BitString expected)
    {
        try
        {
            DynamicApplyNativeMetadata527Admission.VerifyWitness(witness, expected);
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException(NativeMetadataWitness527Names.Failure);
    }
}
