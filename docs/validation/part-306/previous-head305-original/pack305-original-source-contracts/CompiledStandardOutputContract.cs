namespace Erlang.Differential;

public static class CompiledStandardOutputContract
{
    public static bool Matches(CompiledModuleCase fixture, string outputHex, string errorHex) =>
        errorHex == fixture.ExpectedStandardErrorHex &&
        (outputHex == fixture.ExpectedStandardOutputHex || fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true);
}
