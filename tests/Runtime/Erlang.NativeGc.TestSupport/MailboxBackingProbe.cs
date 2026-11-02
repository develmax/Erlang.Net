using System.Collections;
using System.Reflection;

namespace Erlang.NifNativeBackend;

internal static class MailboxBackingProbe
{
    internal static ProcessTermSequence506? Storage(Term value)
        => (ProcessTermSequence506?)typeof(ProcessTermHeap506).GetMethod(
            MailboxBackingCaseNames.StorageMethod, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [value]);

    internal static (Term Value, MailboxPayloadLease593? Work) ReadSingle(Mailbox mailbox)
    {
        object gate = typeof(Mailbox).GetField(MailboxBackingCaseNames.GateField,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mailbox)!;
        lock (gate)
        {
            var entries = (IEnumerable)typeof(Mailbox).GetField(MailboxBackingCaseNames.MessagesField,
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mailbox)!;
            object[] rows = entries.Cast<object>().ToArray();
            CpuStatisticsAccess.Require(rows.Length == 1);
            object entry = rows[0];
            Term value = (Term)entry.GetType().GetProperty(MailboxBackingCaseNames.ValueProperty)!.GetValue(entry)!;
            var custody = (MailboxEntryCustody593)entry.GetType()
                .GetProperty(MailboxBackingCaseNames.CustodyProperty)!.GetValue(entry)!;
            if (custody is null) return (value, null);
            var work = (MailboxPayloadLease593)typeof(MailboxEntryCustody593)
                .GetField(MailboxBackingCaseNames.WorkField, BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(custody)!;

            return (value, work);
        }
    }
}
