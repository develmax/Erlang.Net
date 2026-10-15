namespace Erlang;

public sealed partial class ModuleRegistry
{
    private readonly Dictionary<(string Module, string Function, int Arity), IntrinsicBinding> intrinsicBindings = new();

    private sealed record IntrinsicBinding(ErlangFunction Body, ModuleCodeImage Image);

    internal void RegisterIntrinsic(
        string module,
        string function,
        int arity,
        ErlangFunction body
    )
    {
        using var modification = CodeModification.Enter();
        lock (gate)
        {
            Register(
                module,
                function,
                arity,
                body
            );
            intrinsicBindings.Add((module, function, arity), new(body, codeImages[module]));
        }
    }

    private bool IsIntrinsic(
        (string Module, string Function, int Arity) signature,
        ErlangFunction? body,
        ModuleCodeImage? image
    )
    {
        return body is not null && image is not null && intrinsicBindings.TryGetValue(signature, out var binding) &&
            ReferenceEquals(binding.Body, body) && ReferenceEquals(binding.Image, image);
    }
}
