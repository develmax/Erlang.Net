using Erlang.Differential;

namespace Erlang.Tests;

public static class ProcLibMetadataMfaContracts
{
    public static bool Owns(string module, string function) => module == ProcLibMetadataCaseNames.Module && function is
        ProcLibMetadataCaseNames.InitialCall or ProcLibMetadataCaseNames.TranslateInitialCall or ProcLibMetadataCaseNames.SetLabel or ProcLibMetadataCaseNames.GetLabel;

    public static async Task ParseAll()
    {
        foreach (var fixture in ProcLibMetadataCases.Modules)
            new Erlang.Compiler.Parser(fixture.Source).ParseModule();

        await Task.CompletedTask;
    }

    public static async Task RunAll()
    {
        await ParseAll();
        foreach (var fixture in ProcLibMetadataCases.Modules)
        {
            using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
            Term actual = await CompiledModuleExecution.Run(artifact);
            bool passed = actual.Equals(fixture.Expected);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { fixture.Name, Expected = fixture.Expected.ToString(), Actual = actual.ToString(), Passed = passed }));
            if (!passed)
                throw new InvalidOperationException(ProcLibMetadataCaseNames.Mismatch);
        }
        foreach (string function in new[] { ProcLibMetadataCaseNames.InitialCall, ProcLibMetadataCaseNames.TranslateInitialCall, ProcLibMetadataCaseNames.SetLabel, ProcLibMetadataCaseNames.GetLabel })
        {
            await Run(function);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { Function = function, Arity = 1, Direct = true, Passed = true }));
        }
    }

    public static async Task Run(string function)
    {
        string name = function switch
        {
            ProcLibMetadataCaseNames.InitialCall => ProcLibMetadataCaseNames.InitialDictionary,
            ProcLibMetadataCaseNames.TranslateInitialCall => ProcLibMetadataCaseNames.PidTupleSelf,
            ProcLibMetadataCaseNames.SetLabel => ProcLibMetadataCaseNames.LabelOverwrite,
            ProcLibMetadataCaseNames.GetLabel => ProcLibMetadataCaseNames.LabelLiveChild,
            _ => throw new InvalidOperationException(ProcLibMetadataCaseNames.Mismatch)
        };
        var fixture = ProcLibMetadataCases.Modules.Single(value => value.Name == name);
        using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
        Term actual = await CompiledModuleExecution.Run(artifact);
        if (!actual.Equals(fixture.Expected))
            throw new InvalidOperationException(ProcLibMetadataCaseNames.Mismatch);
    }
}
