namespace Erlang.Tests;

internal static class BeamHostCheckNames
{
    public const int ConcurrentOwners = 8;
    public const int NoArgumentsArity = 0;
    public const byte InvalidFormFirstByte = 0;
    public const long ExpectedConstantNumber = 42;
    public const long ExpectedAddResult = 42;
    public const long LocalListFirst = 1;
    public const long LocalListSecond = 2;
    public const long LocalListThird = 3;
    public const string Test = "BEAM host actual decoded exports, native results and lifecycle";
    public const string ResultTag = "ok";
    public const string ConstantFirstAtom = "alpha";
    public const string ConstantSecondAtom = "beta";
    public const string BeamExtension = ".beam";
    public const string ExpectedEtfExtension = ".expected.etf";
    public const string ResourceSeparator = ".";
    public const string InvalidMutableInput = "Mutable invalid bytes were accepted as a BEAM image";
    public const string UnexpectedInvocation = "BEAM host accepted an unsupported or stopped-context invocation";
    public const string FailedContract = "Actual BEAM host contract failed";
    public const string Constant = "beam_fixture_constant";
    public const string Add = "beam_fixture_add";
    public const string Local = "beam_fixture_local";
    public const string Receive = "beam_fixture_receive";
    public const string OnLoad = "beam_fixture_on_load";
    public const string Run = "run";
    public const string ModuleInfo = "module_info";
    public const string MissingExport = "beam_host_missing_export";
}
