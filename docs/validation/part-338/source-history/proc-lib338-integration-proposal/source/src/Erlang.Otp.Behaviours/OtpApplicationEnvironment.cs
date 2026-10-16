using System.Runtime.CompilerServices;

namespace Erlang.Otp;

internal sealed class OtpApplicationEnvironment
{
    private static readonly ConditionalWeakTable<ProcessRuntime, OtpApplicationEnvironment> States = new();
    private readonly object gate = new();
    private readonly Dictionary<(Term Application, Term Key), Term> values = new();

    public static OtpApplicationEnvironment For(ProcessRuntime runtime) => States.GetValue(runtime, _ => new());

    public Term Get(Term application, Term key, Term? fallback = null)
    {
        lock (gate)
        {
            if (values.TryGetValue((application, key), out var value))
                return fallback is null ? Term.Tuple(OtpLoggerTerms.Ok, value) : value;
        }

        return fallback ?? OtpLoggerTerms.Undefined;
    }

    public Term Set(Term application, Term key, Term value)
    {
        lock (gate)
            values[(application, key)] = value;

        return OtpLoggerTerms.Ok;
    }

    public Term Unset(Term application, Term key)
    {
        lock (gate)
            values.Remove((application, key));

        return OtpLoggerTerms.Ok;
    }
}
