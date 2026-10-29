using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text.Json;
using static Erlang.Tests.BeamPassive647Names;

namespace Erlang.Tests;

internal static class BeamPassive647Checks
{
    internal static async Task Run(string mode)
    {
        string output = Path.GetFullPath(Path.Combine(Output, Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(output);
        var failures = new List<System.Exception>();
        CodePurger432Fixture? fixture = null;
        bool cleanupCompleted = false;
        try
        {
            fixture = await CodePurger432Fixture.Create();
            await Observe(fixture, mode, output);
        }
        catch (System.Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (fixture is not null)
                try
                {
                    await fixture.DisposeAsync();
                    cleanupCompleted = true;
                }
                catch (System.Exception error)
                {
                    failures.Add(error);
                }
            System.IO.File.WriteAllText(
                Path.Combine(output, Cleanup),
                JsonSerializer.Serialize(new
                {
                    Completed = cleanupCompleted,
                    Errors = failures.Select(error => error.ToString()).ToArray()
                })
            );
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(Failure, failures);
    }

    private static async Task Observe(CodePurger432Fixture fixture, string mode, string output)
    {
        Require(mode is Complete or Abort or Reload);
        var dependencies = new BeamDependencySelection(new[]
        {
            RuntimeModule, CodePurger432HostNames.Module, CodePurger432HostNames.Subject,
            BeamPassive647Names.Module, EntryModule
        });
        byte[] observerBytes = Resource(SubjectImage);
        byte[] entryBytes = Resource(EntryImage);
        PreparedBeamImage observer = fixture.Runtime.Modules.PrepareBeam(observerBytes, dependencies);
        PreparedBeamImage entry = fixture.Runtime.Modules.PrepareBeam(entryBytes, dependencies);
        Require(fixture.Runtime.Modules.PublishPreparedBeam([observer, entry]).Status == BeamPublicationStatus.Published);
        Require(ReferenceEquals(fixture.Runtime.Modules.SourceOwner(BeamPassive647Names.Module, mode, 2), observer));
        Require(ReferenceEquals(fixture.Runtime.Modules.SourceOwner(EntryModule, EntryRun, 3), entry));
        Require(ReferenceEquals(fixture.Runtime.Modules.SourceOwner(EntryModule, EntryWorker, 5), entry));
        Term actual = await fixture.Actor.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
            context,
            EntryModule,
            EntryRun,
            Term.A(mode),
            new BitString(CodePurgerOriginal432Access.Resource(CodePurgerOriginal432Names.FirstBeam)),
            new BitString(CodePurgerOriginal432Access.Resource(CodePurgerOriginal432Names.SecondBeam))
        ));
        byte[] bytes = await EncodeTerm(fixture, actual);
        System.IO.File.WriteAllBytes(Path.Combine(output, Raw), bytes);
        Require(actual is TupleTerm { Items: [Atom { Name: WholeMarker }, _, Atom { Name: StackMarker }, _] });
        var envelope = (TupleTerm)actual;
        byte[] stackBytes = await EncodeTerm(fixture, envelope.Items[3]);
        byte[] originalStack = Resource(mode + ModeFileSeparator + StackFile);
        System.IO.File.WriteAllBytes(Path.Combine(output, StackFile), stackBytes);
        bool stackEqual = stackBytes.AsSpan().SequenceEqual(originalStack);
        bool wholeEqual = mode == Complete || bytes.AsSpan().SequenceEqual(Resource(mode + ModeFileSeparator + ObservedFile));
        System.IO.File.WriteAllText(
            Path.Combine(output, Receipt),
            JsonSerializer.Serialize(new
            {
                Mode = mode,
                CallerBackend,
                SubjectBackend,
                FullStackRawBytesEqual = stackEqual,
                WholeOutcomeOpaqueIdsCompared = false,
                NonOpaqueWholeOutcomeEqual = mode == Complete ? (bool?)null : wholeEqual,
                ActualStackSha256 = Convert.ToHexString(SHA256.HashData(stackBytes)),
                OriginalStackSha256 = Convert.ToHexString(SHA256.HashData(originalStack)),
                WorkerSha256 = Convert.ToHexString(SHA256.HashData(observerBytes)),
                EntrySha256 = Convert.ToHexString(SHA256.HashData(entryBytes)),
                RawWholeOutcomeSha256 = Convert.ToHexString(SHA256.HashData(bytes)),
                Normalization = false,
                CleanupRecordedSeparately = true
            })
        );
        Require(stackEqual && wholeEqual);
        Require(envelope.Items[1] is TupleTerm { Items.Count: 5 });
        var row = (TupleTerm)envelope.Items[1];
        Require(row.Items[0].Equals(Term.Tuple(Term.A(Value), Term.I(1))));
        Require(row.Items[1].Equals(Term.A(ErlangBooleanAtoms.False)));
        Require(row.Items[2].Equals(Term.A(mode == Abort ? ErlangBooleanAtoms.False : ErlangBooleanAtoms.True)));
        Require(row.Items[4].Equals(Term.I(mode == Reload ? 1 : 2)));
        if (mode == Complete)
            Require(row.Items[3] is TupleTerm
            {
                Items: [Atom { Name: BeamPassive647Names.Exception }, Atom { Name: BeamPassive647Names.Error },
                    TupleTerm { Items: [Atom { Name: BeamPassive647Names.BadFun }, FunctionTerm] }, _, Atom { Name: ErlangBooleanAtoms.True }]
            });
        else
            Require(row.Items[3].Equals(Term.Tuple(Term.A(Value), Term.I(1))));
    }

    private static async Task<byte[]> EncodeTerm(CodePurger432Fixture fixture, Term value)
    {
        var encoded = (BitString)await fixture.Actor.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
            context,
            RuntimeModule,
            Encode,
            value
        ));

        return encoded.ToArray();
    }

    private static byte[] Resource(string name)
    {
        var assembly = typeof(BeamPassive647Checks).Assembly;

        byte[] Read(string file)
        {
            using Stream source = assembly.GetManifestResourceStream(ResourcePrefix + file)
                ?? throw new InvalidOperationException(Failure);
            using var output = new MemoryStream();
            source.CopyTo(output);

            return output.ToArray();
        }
        using JsonDocument manifest = JsonDocument.Parse(Read(ManifestFile));
        Require(manifest.RootElement.GetProperty(ManifestPinField).GetString() == Pin);
        byte[] bytes = Read(name);
        Require(Convert.ToHexString(SHA256.HashData(bytes))
            == manifest.RootElement.GetProperty(ManifestFilesField).GetProperty(name).GetString());

        return bytes;
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
