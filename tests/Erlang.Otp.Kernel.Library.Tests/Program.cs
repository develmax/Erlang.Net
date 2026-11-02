using Erlang;
using Erlang.Otp;

int passed = 0;
void Check(bool condition)
{
    if (!condition)
        throw new InvalidOperationException("Original kernel library packaging contract failed");
    passed++;
}
void Reject(Action action)
{
    bool rejected = false;
    try
    {
        action();
    }
    catch (ArgumentException)
    {
        rejected = true;
    }
    Check(rejected);
}
Check(OtpKernel.Modules.Count == 104);
foreach (string module in OtpKernel.Modules)
{
    byte[] image = OtpKernel.ReadImage(module);
    BeamArtifactInfo info = BeamArtifactInspector.Inspect(image);
    Check(info.Name == module);
    image[0] ^= byte.MaxValue;
    Check(BeamArtifactInspector.Inspect(OtpKernel.ReadImage(module)) == info);
}
byte[] application = OtpKernel.ReadApplication();
byte[] upgrade = OtpKernel.ReadApplicationUpgrade();
Check(application.Length > 0 && upgrade.Length > 0);
application[0] ^= byte.MaxValue;
upgrade[0] ^= byte.MaxValue;
Check(application[0] != OtpKernel.ReadApplication()[0]);
Check(upgrade[0] != OtpKernel.ReadApplicationUpgrade()[0]);
Reject(() => OtpKernel.ReadImage("../net_kernel"));
Reject(() => OtpKernel.ReadImage("missing"));
Reject(() => OtpKernel.Beam("net_kernel", null!));
await using var runtime = new ProcessRuntime();
await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
    runtime,
    (registry, scope) =>
{
    PreparedBeamImage image = scope.Prepare(() => registry.PrepareBeam(
        OtpKernel.ReadImage("inet_parse"),
        new BeamDependencySelection(["application", "erlang", "error_logger", "file", "io", "lists", "string"])
    ));

    return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
    {
        Check(registry.PublishPreparedBeam([image]).Status == BeamPublicationStatus.Published);
        Check(ReferenceEquals(registry.SourceOwner("inet_parse", "address", 1), image));
    });
}
);
var completed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle actor = runtime.Spawn(async context =>
{
    try
    {
        var results = new List<Term>();
        (string Function, Term Argument)[] calls = [
            ("address", Term.String("127.0.0.1")),
            ("ipv4_address", Term.String("127.1")),
            ("ipv4strict_address", Term.String("127.1")),
            ("ipv6_address", Term.String("::1")),
            ("address", Term.String("invalid")),
            ("strict_address", Term.I(42)),
            ("ntoa", Term.Tuple(
                Term.I(127),
                Term.I(0),
                Term.I(0),
                Term.I(1)
            )),
            ("ntoa", Term.Tuple(
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(1)
            ))
        ];
        foreach ((string function, Term argument) in calls)
            results.Add(await runtime.Modules.Call(
                context,
                "inet_parse",
                function,
                argument
            ));
        completed.TrySetResult(Term.List(results.ToArray()));
    }
    catch (Exception error)
    {
        completed.TrySetException(error);
    }

    return Nil.Value;
});
Term outcome = await completed.Task.WaitAsync(TimeSpan.FromSeconds(30));
await actor.Completion.WaitAsync(TimeSpan.FromSeconds(30));
string expected = "[{ok,{127,0,0,1}},{ok,{127,0,0,1}},{error,einval},{ok,{0,0,0,0,0,0,0,1}},{error,einval},{error,einval},[49,50,55,46,48,46,48,46,49],[58,58,49]]";
Check(outcome.ToString() == expected);
if (args.Length == 1)
    File.WriteAllText(args[0], outcome.ToString());
Console.WriteLine(outcome);
Console.WriteLine($"Original kernel packaging: {passed} checks passed");
