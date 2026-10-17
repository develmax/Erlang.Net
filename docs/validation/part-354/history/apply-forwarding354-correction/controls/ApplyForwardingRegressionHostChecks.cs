using Erlang.Differential;

namespace Erlang.Tests;

internal static class ApplyForwardingRegressionHostChecks
{
    internal static async Task Run()
    {
        var fixtures = ApplyFrameCases.Modules;
        if (fixtures.Count != ApplyForwardingRegressionHostNames.ExpectedCases)
            throw new InvalidOperationException(ApplyForwardingRegressionHostNames.WrongCount);
        foreach (var fixture in fixtures)
        {
            using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
            var actual = await CompiledModuleExecution.Run(artifact, fixture.CallerContext);
            if (!actual.Equals(fixture.Expected))
                throw new InvalidOperationException(ApplyForwardingRegressionHostNames.FailurePrefix + fixture.Name);
        }
    }
}
