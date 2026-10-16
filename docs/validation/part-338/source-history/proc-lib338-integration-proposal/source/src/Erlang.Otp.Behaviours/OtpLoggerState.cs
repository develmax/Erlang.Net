using System.Runtime.CompilerServices;

namespace Erlang.Otp;

internal sealed class OtpLoggerState
{
    private static readonly ConditionalWeakTable<ProcessRuntime, OtpLoggerState> States = new();
    public readonly object Gate = new();
    public MapTerm Primary = OtpLoggerTerms.PrimaryDefaults();
    public readonly Dictionary<Term, MapTerm> Handlers = new();
    public readonly List<Term> HandlerOrder = new();
    public readonly Dictionary<Atom, Term> ModuleLevels = new();

    private OtpLoggerState()
    {
        Handlers.Add(OtpLoggerTerms.Default, OtpLoggerTerms.HandlerDefaults(
            OtpLoggerTerms.Default,
            Term.A(OtpLoggerNames.LoggerStdH)
        ));
        HandlerOrder.Add(OtpLoggerTerms.Default);
    }

    public static OtpLoggerState For(ProcessRuntime runtime) => States.GetValue(runtime, _ => new());

    public bool Allow(Term level, Atom? module)
    {
        int severity = OtpLoggerTerms.Severity(level);
        lock (Gate)
        {
            Term threshold = module is not null && ModuleLevels.TryGetValue(module, out var configured)
                ? configured
                : OtpLoggerTerms.Get(Primary, OtpLoggerNames.Level);

            return severity <= OtpLoggerTerms.Severity(threshold, true);
        }
    }
}
