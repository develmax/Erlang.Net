using Erlang;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class IdentifierOriginal403Checks
{
    internal static Task PidWhole() => Compare(IdentifierOriginal403Names.Pid);

    internal static Task PortWhole() => Compare(IdentifierOriginal403Names.Port);

    internal static Task ReferenceWhole() => Compare(IdentifierOriginal403Names.Reference);

    internal static Task FunctionPidWhole() => Compare(IdentifierOriginal403Names.FunctionPid);

    internal static Task NestedFunctionReferenceWhole() => Compare(IdentifierOriginal403Names.NestedFunctionReference);

    private static async Task Compare(string kind)
    {
        string output = Path.Combine(IdentifierOriginal403Names.ArtifactRoot, kind, Guid.NewGuid().ToString(IdentifierOriginal403Names.GuidFormat));
        Directory.CreateDirectory(output);
        var failures = new List<Exception>();
        var rows = new List<object>();
        int strict = 0;
        foreach (string requested in IdentifierOriginal403Names.Requests)
        {
            string name = IdentifierOriginal403Names.Prefix + kind + "-" + requested;
            try
            {
                var suite = new LibraryDependencyOriginalSuite(
                    name,
                    IdentifierOriginal403Names.Module,
                    [IdentifierOriginal403Names.Module],
                    [IdentifierOriginal403Names.NetKernel],
                    1,
                    3
                );
                await using ProcessRuntime runtime = await LibraryDependencyOriginalHostChecks.StartRetainedSuite(suite);
                await using var actor = new RuntimeNodeFoundationHostActor(runtime);
                Term actual = await actor.InvokeAsync(async context => requested == IdentifierOriginal403Names.Value
                    ? await runtime.Modules.Call(
                        context,
                        suite.Observer,
                        IdentifierOriginal403Names.Run,
                        Term.A(kind)
                    )
                    : await runtime.Modules.Call(
                        context,
                        suite.Observer,
                        IdentifierOriginal403Names.Profile,
                        Term.A(kind),
                        requested == IdentifierOriginal403Names.Default ? Term.A(requested) : Term.I(int.Parse(requested,
                            System.Globalization.CultureInfo.InvariantCulture))
                    ))
                    .WaitAsync(IdentifierOriginal403Names.Deadline);
                byte[] encoded = ExternalTermFormat.Encode(actual);
                File.WriteAllBytes(Path.Combine(output, name + IdentifierOriginal403Names.ActualExtension), encoded);
                // Decode expected bytes outside runtime admission: golden DTOs must not add owner roots before the guest.
                byte[] original = LibraryDependencyOriginalHostChecks.ReadRetainedResource(name + IdentifierOriginal403Names.Extension);
                Term expected = ExternalTermFormat.Decode(original);
                bool typed = expected.Equals(actual);
                bool raw = original.AsSpan().SequenceEqual(encoded);
                rows.Add(new
                {
                    Case = kind,
                    Requested = requested,
                    TypedWhole = typed,
                    RawWhole = raw,
                    OriginalSha256 = Convert.ToHexString(SHA256.HashData(original)),
                    ActualSha256 = Convert.ToHexString(SHA256.HashData(encoded)),
                    ProjectedFields = false
                });
                if (!typed || !raw)
                    throw new InvalidOperationException(IdentifierOriginal403Names.Failure);
                strict++;
            }
            catch (Exception error)
            {
                failures.Add(error);
                rows.Add(new { Case = kind, Requested = requested, Error = error.ToString() });
            }
        }
        File.WriteAllText(
            Path.Combine(output, IdentifierOriginal403Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Case = kind,
                Examined = IdentifierOriginal403Names.ExpectedPerKind,
                StrictRawTypedWhole = strict,
                Failed = failures.Count,
                Rows = rows,
                FullApiCompatibilityClaim = false
            })
        );
        if (failures.Count != 0)
            throw new AggregateException(failures);
        if (strict != IdentifierOriginal403Names.ExpectedPerKind)
            throw new InvalidOperationException(IdentifierOriginal403Names.Failure);
    }
}
