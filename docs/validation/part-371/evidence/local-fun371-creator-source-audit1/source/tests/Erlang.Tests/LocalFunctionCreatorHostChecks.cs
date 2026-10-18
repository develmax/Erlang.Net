using System.Buffers.Binary;

namespace Erlang.Tests;

internal static class LocalFunctionCreatorHostChecks
{
    internal static async Task Canonicalization()
    {
        await using var runtime = new ProcessRuntime();
        var foreign = new Pid(
            LocalFunctionCreatorHostNames.ForeignNode,
            LocalFunctionCreatorHostParameters.ForeignId,
            LocalFunctionCreatorHostParameters.ForeignCreation
        );
        var other = new Pid(
            LocalFunctionCreatorHostNames.OtherForeignNode,
            LocalFunctionCreatorHostParameters.OtherForeignId,
            LocalFunctionCreatorHostParameters.OtherForeignCreation
        );
        FunctionTerm source = Create();
        byte[] firstPacket = Encode(source, foreign);
        byte[] secondPacket = Encode(source, other);
        Require(Creator(firstPacket).Equals(foreign));
        Require(Creator(secondPacket).Equals(other));
        var first = (FunctionTerm)runtime.ImportExternalTerm(firstPacket).Term;
        var second = (FunctionTerm)runtime.ImportExternalTerm(secondPacket).Term;
        Require(first.Equals(second));
        Require(first.GetHashCode() == second.GetHashCode());
        Require(first.CompareTo(second) == 0);
        byte[] firstCanonical = Encode(first, runtime.InitializationPid);
        byte[] secondCanonical = Encode(second, runtime.InitializationPid);
        Require(Creator(firstCanonical).Equals(runtime.InitializationPid));
        Require(firstCanonical.AsSpan().SequenceEqual(secondCanonical));
        Require(!firstCanonical.AsSpan().SequenceEqual(firstPacket));
        Exception? failure = null;
        var actor = runtime.Spawn(async context =>
        {
            try
            {
                foreach (FunctionTerm function in new[] { source, first, second })
                {
                    Term actual = await runtime.Modules.Call(
                        context,
                        LocalFunctionCreatorHostNames.Erlang,
                        LocalFunctionCreatorHostNames.FunInfo,
                        function,
                        Term.A(LocalFunctionCreatorHostNames.Pid)
                    );
                    Require(actual.Equals(Term.Tuple(Term.A(LocalFunctionCreatorHostNames.Pid), runtime.InitializationPid)));
                }
            }
            catch (Exception error)
            {
                failure = error;
                throw;
            }

            return Nil.Value;
        });
        Term exit = await actor.Completion.WaitAsync(LocalFunctionCreatorHostParameters.Deadline);
        if (failure is not null)
            throw failure;
        Require(exit.Equals(Term.A(LocalFunctionCreatorHostNames.Normal)));
    }

    internal static Task Validation()
    {
        var foreign = new Pid(
            LocalFunctionCreatorHostNames.ForeignNode,
            LocalFunctionCreatorHostParameters.ForeignId,
            LocalFunctionCreatorHostParameters.ForeignCreation
        );
        byte[] valid = Encode(Create(), foreign);
        byte[] encodedPid = ExternalTermFormat.Encode(foreign);
        byte[] encodedAtom = ExternalTermFormat.Encode(Term.A(LocalFunctionCreatorHostNames.InvalidCreator));
        int creatorOffset = valid.Length - encodedPid.Length + LocalFunctionCreatorHostParameters.VersionBytes;
        byte[] malformed = valid.AsSpan(0, creatorOffset).ToArray()
            .Concat(encodedAtom.Skip(LocalFunctionCreatorHostParameters.VersionBytes)).ToArray();
        BinaryPrimitives.WriteUInt32BigEndian(
            malformed.AsSpan(LocalFunctionCreatorHostParameters.FunSizeOffset, LocalFunctionCreatorHostParameters.FunSizeBytes),
            (uint)(malformed.Length - LocalFunctionCreatorHostParameters.FunSizeOffset)
        );
        int calls = 0;
        bool rejected = false;
        try
        {
            ExternalTermFormat.Decode(
                malformed,
                new EtfDecodeOptions
                {
                    LocalFunctionFactory = (_, _, _) =>
                    {
                        calls++;

                        return Create();
                    }
                }
            );
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            rejected = true;
        }
        Require(rejected && calls == 0);

        return Task.CompletedTask;
    }

    private static FunctionTerm Create() => new(
        new LocalFunctionIdentity(
            Term.A(LocalFunctionCreatorHostNames.Module),
            null,
            LocalFunctionCreatorHostParameters.CodeIndex,
            LocalFunctionCreatorHostParameters.OldUnique,
            new BitString(new byte[LocalFunctionCreatorHostParameters.UniqueBytes]),
            LocalFunctionCreatorHostParameters.OldIndex
        ),
        LocalFunctionCreatorHostParameters.Arity,
        [],
        (_, _) => ValueTask.FromResult<Term>(Nil.Value)
    );

    private static byte[] Encode(FunctionTerm function, Pid owner) => ExternalTermFormat.Encode(
        function,
        new EtfEncodingOptions { FunctionOwner = owner }
    );

    private static Pid Creator(byte[] packet)
    {
        Pid? creator = null;
        ExternalTermFormat.Decode(
            packet,
            new EtfDecodeOptions
            {
                PidFactory = value =>
                {
                    Require(creator is null);
                    creator = value;

                    return value;
                },
                LocalFunctionFactory = (_, _, _) => Create()
            }
        );
        Require(creator is not null);

        return creator!;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidDataException(LocalFunctionCreatorHostNames.Failure);
    }
}
