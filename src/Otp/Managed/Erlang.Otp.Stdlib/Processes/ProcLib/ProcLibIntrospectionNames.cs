namespace Erlang.Otp;

internal static class ProcLibIntrospectionNames
{
    public const string InitialCall = "initial_call";
    public const string TranslateInitialCall = "translate_initial_call";
    public const string SetLabel = "set_label";
    public const string GetLabel = "get_label";
    public const string ProcessLabel = "$process_label";
    public const string Get = "get";
    public const string Put = "put";
    public const string DummyArgumentPrefix = "Argument__";
    public const string PidFormat = "<{0}.{1}.{2}>";
    public const int DefaultInitializerArity = 5;
}
