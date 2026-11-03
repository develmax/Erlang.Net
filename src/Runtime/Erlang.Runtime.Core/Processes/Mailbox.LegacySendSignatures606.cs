namespace Erlang;

public sealed partial class Mailbox
{
    internal void SendTraced(
        Term message,
        SequenceTraceToken? token,
        bool preserve = false,
        bool priority = false,
        Term? from = null,
        bool traceAdmission = true,
        Term? distributionTraceToken = null
    ) => SendTracedSnapshot593(
        message,
        token,
        preserve,
        priority,
        from,
        traceAdmission,
        distributionTraceToken,
        null
    );

    internal void SendPending(
        Term message,
        Func<bool?> admission,
        SequenceTraceToken? token = null,
        bool preserve = false,
        Term? from = null,
        bool traceAdmission = true,
        Term? distributionTraceToken = null
    ) => SendPendingSnapshot593(
        message,
        admission,
        token,
        preserve,
        from,
        traceAdmission,
        distributionTraceToken,
        null
    );

    private void SendPendingCore428(
        Term message,
        Func<bool?> admission,
        SequenceTraceToken? token = null,
        bool preserve = false,
        Term? from = null,
        bool traceAdmission = true,
        Term? distributionTraceToken = null,
        ProcessSchedulingPriority? notificationPriority428 = null
    ) => SendPendingCore593(
        message,
        admission,
        token,
        preserve,
        from,
        traceAdmission,
        distributionTraceToken,
        notificationPriority428,
        null
    );
}
