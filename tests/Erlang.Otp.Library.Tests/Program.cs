using Erlang;
using Erlang.Otp;

int passed = 0;
void Check(bool condition)
{
    if (!condition)
        throw new InvalidOperationException("Original OTP library packaging contract failed");
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
Check(OtpStdlib.Modules.Count == 98);
foreach (string module in OtpStdlib.Modules)
{
    byte[] image = OtpStdlib.ReadImage(module);
    BeamArtifactInfo info = BeamArtifactInspector.Inspect(image);
    Check(info.Name == module);
    image[0] ^= byte.MaxValue;
    Check(BeamArtifactInspector.Inspect(OtpStdlib.ReadImage(module)) == info);
}
Reject(() => OtpStdlib.ReadImage("../lists"));
byte[] application = OtpStdlib.ReadApplication();
byte[] upgrade = OtpStdlib.ReadApplicationUpgrade();
Check(application.Length > 0 && upgrade.Length > 0);
application[0] ^= byte.MaxValue;
upgrade[0] ^= byte.MaxValue;
Check(application[0] != OtpStdlib.ReadApplication()[0]);
Check(upgrade[0] != OtpStdlib.ReadApplicationUpgrade()[0]);
Reject(() => OtpStdlib.ReadImage("missing"));
Reject(() => OtpStdlib.Select("lists", (RuntimeModuleBackend)int.MaxValue));
Reject(() => OtpStdlib.Select("sets", RuntimeModuleBackend.Managed));
Reject(() => OtpStdlib.Select("gen"));
Reject(() => OtpStdlib.Select("lists", RuntimeModuleBackend.Managed, ["queue"]));
Reject(() => new RuntimeLibrarySelection([OtpStdlib.Select("queue")]));
string[] collections = ["binary", "dict", "gb_sets", "gb_trees", "lists", "maps", "orddict", "ordsets", "proplists", "queue", "sets"];
var selection = new RuntimeLibrarySelection(collections.Select(module => OtpStdlib.Select(module)));
await using (var runtime = await ProcessRuntime.CreateAsync(selection))
{
    Check(runtime.LibrarySelection.Modules.Count == collections.Length);
    foreach (RuntimeLibraryModuleSnapshot module in runtime.LibrarySelection.Modules)
    {
        Check(module.Backend == RuntimeModuleBackend.Beam);
        Check(ReferenceEquals(module.Image, runtime.Modules.CurrentCodeImage(module.Module)));
        Check(module.Exports.All(export => runtime.Modules.SourceOwner(module.Module, export.Name, export.Arity) is PreparedBeamImage));
    }
}
Console.WriteLine($"Original OTP packaging: {passed} checks passed");
