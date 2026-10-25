namespace Erlang.Otp.Gui.Library.Tests;

internal sealed record GuiLibraryAsset(
    IReadOnlyList<string> Modules,
    Func<string, byte[]> ReadImage,
    Func<byte[]> ReadApplication,
    Func<byte[]> ReadUpgrade,
    IReadOnlyList<string> SupportFiles,
    Func<string, byte[]> ReadSupport,
    Func<string, IEnumerable<string>, RuntimeModuleSelection> Beam,
    int ExpectedModules,
    int ExpectedSupport
);
