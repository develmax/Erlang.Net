namespace Erlang;

public static partial class DistributionPassThroughCodec
{
    public static bool IsSpawnRequest(TupleTerm control)
        => control.Items.Count > 0 && control.Items[0] is Integer opcode &&
            (opcode.Value == DistributionControlOpcodes.SpawnRequest || opcode.Value == DistributionControlOpcodes.SpawnRequestTrace);

    public static bool IsSpawnReply(TupleTerm control)
        => control.Items.Count > 0 && control.Items[0] is Integer opcode &&
            (opcode.Value == DistributionControlOpcodes.SpawnReply || opcode.Value == DistributionControlOpcodes.SpawnReplyTrace);

    private static void ValidateSpawnControl(TupleTerm control, ulong negotiatedFlags)
    {
        if ((negotiatedFlags & DistributionPassThroughFlags.Spawn) == 0)
            throw new NotSupportedException(DistributionPassThroughDiagnostics.Flags);
        var items = control.Items;
        int opcode = (int)((Integer)items[0]).Value;
        bool valid;
        if (IsSpawnRequest(control))
        {
            valid = items.Count == (opcode == DistributionControlOpcodes.SpawnRequest
                ? DistributionSpawnLayout.RequestArity : DistributionSpawnLayout.RequestTraceArity) &&
                items[DistributionSpawnLayout.ReferenceIndex] is ReferenceTerm &&
                items[DistributionSpawnLayout.ParentIndex] is Pid &&
                items[DistributionSpawnLayout.GroupLeaderIndex] is Pid &&
                items[DistributionSpawnLayout.MfaIndex] is TupleTerm { Items: [Atom, Atom, Integer { Value.Sign: >= 0 }] } &&
                IsProperSpawnList(items[DistributionSpawnLayout.OptionsIndex]);
        }
        else
        {
            valid = items.Count == (opcode == DistributionControlOpcodes.SpawnReply
                ? DistributionSpawnLayout.ReplyArity : DistributionSpawnLayout.ReplyTraceArity) &&
                items[DistributionSpawnLayout.ReferenceIndex] is ReferenceTerm &&
                items[DistributionSpawnLayout.ParentIndex] is Pid &&
                items[DistributionSpawnLayout.ReplyFlagsIndex] is Integer flags && flags.Value >= 0 &&
                flags.Value <= DistributionSpawnLayout.MaximumReplyFlags &&
                items[DistributionSpawnLayout.ReplyResultIndex] is Pid or Atom;
        }
        if (!valid)
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(control));
    }

    private static void ValidateSpawnArguments(TupleTerm control, Term payload)
    {
        if (!IsProperSpawnList(payload) ||
            ((Integer)((TupleTerm)control.Items[DistributionSpawnLayout.MfaIndex]).Items[2]).Value != Cons.Items(payload).Count())
            throw new ArgumentException(DistributionPassThroughDiagnostics.PacketFormat, nameof(payload));
    }

    private static bool IsProperSpawnList(Term value)
    {
        while (value is Cons cell)
            value = cell.Tail;

        return value is Nil;
    }
}
