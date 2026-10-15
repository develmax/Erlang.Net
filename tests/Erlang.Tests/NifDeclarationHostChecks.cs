using Erlang.Compiler;

namespace Erlang.Tests;

public static class NifDeclarationHostChecks
{
    public static Task Run()
    {
        var absent = Parse(string.Empty);
        var empty = Parse(NifDeclarationHostNames.Empty);
        var repeated = Parse(NifDeclarationHostNames.Repeated);
        Check(absent.DeclaredNifs is null);
        Check(empty.DeclaredNifs is { Count: 0 });
        Check(repeated.DeclaredNifs is { Count: 1 } declarations && declarations[0] == (NifDeclarationHostNames.Fallback, 0));
        Check(repeated.Attributes.All(attribute => attribute.Name != NifDeclarationHostNames.Nifs));
        var registry = new ModuleRegistry();
        repeated.Register(registry);
        Check(registry.ModuleInformation(NifDeclarationHostNames.Module, NifDeclarationHostNames.Nifs) is Nil);
        Check(!CodeGeneration.CompileModule(NifDeclarationHostNames.Source + NifDeclarationHostNames.Body, NifDeclarationHostNames.SourcePath).Contains(NifDeclarationHostNames.Initializer, StringComparison.Ordinal));
        Check(CodeGeneration.CompileModule(
            NifDeclarationHostNames.Source + NifDeclarationHostNames.Empty + NifDeclarationHostNames.Body,
            NifDeclarationHostNames.SourcePath
        ).Contains(NifDeclarationHostNames.Initializer, StringComparison.Ordinal));
        var values = new[] { (Name: NifDeclarationHostNames.Fallback, Arity: 0) };
        var owned = absent with
        {
            DeclaredNifs = values
        };
        var descriptor = new ModuleCodeDescriptor(
            [],
            [],
            new BitString(new byte[16]),
            Nil.Value
        )
        { DeclaredNifs = values };
        values[0] = (NifDeclarationHostNames.Other, 1);
        Check(owned.DeclaredNifs![0] == (NifDeclarationHostNames.Fallback, 0));
        Check(descriptor.DeclaredNifs![0] == (NifDeclarationHostNames.Fallback, 0));
        RejectMutation(owned.DeclaredNifs!);
        RejectMutation(descriptor.DeclaredNifs!);
        Check(typeof(ModuleCodeDescriptor).GetConstructors().Any(constructor => constructor.GetParameters().Length == 8));
        Check(typeof(ModuleDefinition).GetConstructors().Any(constructor => constructor.GetParameters().Length == 4));
        bool rejected = false;
        try
        {
            Parse(NifDeclarationHostNames.UndefinedDeclaration);
        }
        catch (CompileException exception) when (exception.Code == NifDeclarationHostNames.FunctionCode && exception.Message == NifDeclarationHostNames.Undefined)
        {
            rejected = true;
        }
        Check(rejected);

        return Task.CompletedTask;
    }

    private static ModuleDefinition Parse(string declaration) => new Parser(NifDeclarationHostNames.Source + declaration + NifDeclarationHostNames.Body).ParseModule();

    private static void RejectMutation(IReadOnlyList<(string Name, int Arity)> values)
    {
        bool rejected = false;
        try
        {
            ((IList<(string Name, int Arity)>)values)[0] = (NifDeclarationHostNames.Other, 1);
        }
        catch (NotSupportedException)
        {
            rejected = true;
        }
        Check(rejected);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifDeclarationHostNames.Broken);
    }
}
