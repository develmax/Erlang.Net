using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

public static class SourceCallBindingHostChecks
{
    public static Task Check(string name)
    {
        var fixture = SourceCallBindingCases.Controls.Single(value => value.Name == name);
        try
        {
            new Parser(fixture.Source).ParseModule();
        }
        catch (CompileException error) when (error.Code == fixture.Diagnostic)
        {
            return Task.CompletedTask;
        }
        if (fixture.Diagnostic is not null)
            throw new InvalidOperationException(name);
        using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);

        return Task.CompletedTask;
    }

    public static Task GeneratedBindingRoundtrip()
    {
        foreach (var fixture in SourceCallBindingCases.Modules)
        {
            var parsed = new Parser(fixture.Source).ParseModule();
            using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
            var original = parsed.CallBindings;
            var generated = artifact.Definition.CallBindings;
            if (original.SuppressAll != generated.SuppressAll ||
                !original.Suppressed.SequenceEqual(generated.Suppressed) ||
                !original.Imports.SequenceEqual(generated.Imports) ||
                artifact.Definition.Attributes.Any(attribute => attribute.Name is SourceCallBindingCaseNames.ImportAttribute or SourceCallBindingCaseNames.CompileAttribute))
                throw new InvalidOperationException(fixture.Name);
        }
        using var first = GeneratedModuleCompiler.Compile("-module(binding_identity). -export([run/0]). run()->done.");
        using var second = GeneratedModuleCompiler.Compile("-module(binding_identity). -compile(no_auto_import). -export([run/0]). run()->done.");
        if (first.CSharpSha256 == second.CSharpSha256 || first.Definition.CallBindings.SuppressAll || !second.Definition.CallBindings.SuppressAll)
            throw new InvalidOperationException(SourceCallBindingCaseNames.BrokenIdentity);

        return Task.CompletedTask;
    }
}
