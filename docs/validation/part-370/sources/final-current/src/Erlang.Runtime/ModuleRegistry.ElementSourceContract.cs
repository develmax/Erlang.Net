namespace Erlang;

public sealed partial class ModuleRegistry
{
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ErlangFunction, ModuleCodeImage> elementSourceContracts = new();

    internal void SealElementSourceContract()
    {
        lock (gate)
        {
            var body = functions[(ErlangModuleNames.Module, ErlangModuleNames.Element, 2)];
            elementSourceContracts.Add(body, codeImages[ErlangModuleNames.Module]);
        }
    }

    private bool IsElementSourceContract(
        (string Module, string Function, int Arity) signature,
        ErlangFunction? body,
        ModuleCodeImage? image
    ) => signature == (ErlangModuleNames.Module, ErlangModuleNames.Element, 2)
        && body is not null && elementSourceContracts.TryGetValue(body, out var canonicalImage)
        && ReferenceEquals(canonicalImage, image);
}
