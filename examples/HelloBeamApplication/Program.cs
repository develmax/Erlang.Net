using System.Security.Cryptography;
using ApplicationResources;
using Erlang;

string root = Path.Combine(
    AppContext.BaseDirectory,
    ApplicationExampleNames.LibraryDirectory,
    ApplicationExampleNames.Application + "-" + ApplicationExampleNames.Version
);
void Require(bool value)
{
    if (!value)
        throw new InvalidOperationException(ApplicationExampleNames.Failure);
}
void Hash(string relative, string expected)
    => Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, relative)))) == expected);
Hash(
    ApplicationExampleNames.EbinDirectory + "/" + ApplicationExampleNames.Application + ApplicationExampleNames.AppExtension,
    ApplicationExampleNames.DescriptorSha256
);
Hash(ApplicationExampleNames.PrivFile, ApplicationExampleNames.PrivSha256);
Hash(ApplicationExampleNames.BaseConfig, ApplicationExampleNames.BaseConfigSha256);
Hash(ApplicationExampleNames.OverlayConfig, ApplicationExampleNames.OverlayConfigSha256);
Require(File.Exists(Path.Combine(root, ApplicationExampleNames.Manifest)));
Require(!File.Exists(Path.Combine(
    AppContext.BaseDirectory,
    ApplicationExampleNames.ModulesDirectory,
    ApplicationExampleNames.Module + ApplicationExampleNames.BeamExtension
)));
await using var runtime = new ProcessRuntime();
PreparedBeamImage prepared = runtime.Modules.PrepareBeam(File.ReadAllBytes(Path.Combine(root, ApplicationExampleNames.EbinDirectory, ApplicationExampleNames.Module + ApplicationExampleNames.BeamExtension)));
Require(prepared.Name == ApplicationExampleNames.Module);
Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
Require(ReferenceEquals(runtime.Modules.SourceOwner(prepared.Name, ApplicationExampleNames.Function, 0), prepared));
var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle actor = runtime.Spawn(async context =>
{
    try
    {
        result.TrySetResult(await runtime.Modules.Call(context, ApplicationExampleNames.Module, ApplicationExampleNames.Function));
    }
    catch (Exception failure)
    {
        result.TrySetException(failure);
        throw;
    }

    return Nil.Value;
});
Require((await result.Task.WaitAsync(ApplicationExampleNames.Deadline)).Equals(Term.Tuple(
    Term.A(ApplicationExampleNames.Module),
    Term.I(ApplicationExampleNames.BodyValue)
)));
Require((await actor.Completion.WaitAsync(ApplicationExampleNames.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
Console.WriteLine(ApplicationExampleNames.Success);
