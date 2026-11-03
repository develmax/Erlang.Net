namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Term ReadDriverThreadPoolSize1195(ProcessContext context)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);

            return Term.I(driverFrontend422?.AsyncAccountingExpectedCount1170 ?? 0);
        }
    }
}
