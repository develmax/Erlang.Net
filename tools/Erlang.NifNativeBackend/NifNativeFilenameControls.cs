using System.Text;

namespace Erlang.NifNativeBackend;

internal static class NifNativeFilenameControls
{
    internal static void Run()
    {
        string suffix = OperatingSystem.IsWindows()
            ? NifNativeDefaultLoaderNames.WindowsSuffix : NifNativeDefaultLoaderNames.UnixSuffix;

        string Expected(string stem) => Path.Combine(Environment.CurrentDirectory, stem + suffix);

        void Require(string actual, string expected) => NifNativeHarness.Require(actual == expected);

        Require(
            NifNativeFilename.Resolve(new BitString(Encoding.UTF8.GetBytes(NifNativeFilenameControlNames.Stem))),
            Expected(NifNativeFilenameControlNames.Stem)
        );
        Require(
            NifNativeFilename.Resolve(Term.List(Term.String(NifNativeFilenameControlNames.Stem), Nil.Value)),
            Expected(NifNativeFilenameControlNames.Stem)
        );
        byte[] nul = Encoding.UTF8.GetBytes(NifNativeFilenameControlNames.AsciiStem + "\0" + NifNativeFilenameControlNames.Ignored);
        Require(NifNativeFilename.Resolve(new BitString(nul)), Expected(NifNativeFilenameControlNames.AsciiStem));
        Require(NifNativeFilename.Resolve(new BitString(new byte[] { 0 })), Expected(string.Empty));
        Require(
            NifNativeFilename.Resolve(Term.String(NifNativeFilenameControlNames.AsciiStem + suffix)),
            Expected(NifNativeFilenameControlNames.AsciiStem + suffix)
        );
        byte[] latin1 = Encoding.Latin1.GetBytes(NifNativeFilenameControlNames.Stem);
        if (OperatingSystem.IsWindows())
        {
            Require(NifNativeFilename.Resolve(new BitString(latin1)), Expected(NifNativeFilenameControlNames.Stem));
            byte[] nativePrefix = latin1.Concat(new byte[] { 0, byte.MaxValue }).ToArray();
            Require(NifNativeFilename.Resolve(new BitString(nativePrefix)), Expected(NifNativeFilenameControlNames.Stem));
        }
        else
        {
            bool refused = false;
            try
            {
                NifNativeFilename.Resolve(new BitString(latin1));
            }
            catch (NotSupportedException failure) when (failure.Message == NifNativeDefaultLoaderNames.BinaryFilenameEncoding)
            {
                refused = true;
            }
            NifNativeHarness.Require(refused);
        }
    }
}
