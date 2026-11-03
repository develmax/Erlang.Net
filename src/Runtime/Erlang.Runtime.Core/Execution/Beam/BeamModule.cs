using System.Security.Cryptography;

namespace Erlang;

public sealed partial class BeamModule
{
    private readonly BeamLinkedCode code;
    private readonly BeamExecutionIdentity execution;

    private BeamModule(BeamLinkedCode code, string inputSha256)
    {
        this.code = code;
        InputSha256 = inputSha256;
        Name = code.Structure.Atoms[1]!;
        execution = new(new ModuleCodeImage(Name), this);
        Exports = Array.AsReadOnly(code.Exports.Select(value => new BeamFunctionSignature(value.Name, value.Arity)).ToArray());
        Functions = Array.AsReadOnly(code.Functions.Select(value => new BeamFunctionSignature(value.Name, value.Arity)).ToArray());
    }

    public string Name
    {
        get;
    }
    public string InputSha256
    {
        get;
    }
    public IReadOnlyList<BeamFunctionSignature> Exports
    {
        get;
    }
    public IReadOnlyList<BeamFunctionSignature> Functions
    {
        get;
    }
    public bool HasOnLoad => code.OnLoadEntryLabel is not null;

    public IReadOnlySet<string> RegistryModuleDependencies => code.RegistryModuleDependencies;

    public static BeamModule Prepare(ReadOnlyMemory<byte> bytes)
        => Prepare(bytes, new BeamDependencySelection(Array.Empty<string>()));

    public static BeamModule Prepare(ReadOnlyMemory<byte> bytes, BeamDependencySelection dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        byte[] owned = bytes.ToArray();
        BeamLinkedCode linked = BeamLinkedCodeReader.Read(BeamStructureReader.Read(owned)) with
        {
            RegistryModuleDependencies = dependencies.RegistryModules
        };

        return new(linked, Convert.ToHexString(SHA256.HashData(owned)));
    }

    public bool Supports(string function, int arity)
    {
        if (HasOnLoad || code.Functions.Any(function => BeamNativePrimitives.IsStub(code, function)))
            return false;
        BeamLinkedFunction? entry = code.Exports.SingleOrDefault(value => value.Name == function && value.Arity == arity);
        if (entry is null)
        {
            return false;
        }
        try
        {
            BeamRegisterValidation.Validate(code, entry);

            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    public ValueTask<Term> Invoke(ProcessContext context, string function, params Term[] arguments)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(arguments);
        context.Cancellation.ThrowIfCancellationRequested();
        foreach (Term literal in code.Structure.Literals)
            context.Runtime.ObserveIncomingTerm(literal);
        foreach (Term argument in arguments)
            context.Runtime.ObserveIncomingTerm(argument);
        if (HasOnLoad || code.Functions.Any(value => BeamNativePrimitives.IsStub(code, value)))
        {
            if (!code.Exports.Any(entry => entry.Name == function && entry.Arity == arguments.Length))
                throw new InvalidDataException(BeamModuleDiagnostics.MissingExport);
            throw new NotSupportedException(BeamRegisterNames.Unsupported);
        }

        IReadOnlyList<Term> selectedArguments = Array.AsReadOnly(arguments.ToArray());
        if (RuntimeActorExecutionTurn.IsOwnedBy(context))
        {
            BeamLinkedFunction entry = code.Exports.SingleOrDefault(value => value.Name == function && value.Arity == selectedArguments.Count)
                ?? throw new InvalidDataException(BeamModuleDiagnostics.MissingExport);

            return InvokePublicOwned(
                context,
                function,
                selectedArguments,
                entry
            );
        }

        return BeamRegisterInterpreter.Execute(
            context,
            code,
            function,
            selectedArguments,
            execution
        );
    }
}
