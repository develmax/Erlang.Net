namespace Erlang;

internal sealed partial class ProcessTermFrame506
{
    // Existing same-heap label/sender retain their actual identity. The token
    // type and normal token validation are the only construction capability.
    internal TupleTerm CreateMutableSequenceTokenInto550(int slot, SequenceTraceToken token)
    {
        var snapshot = new SequenceTraceToken(
            token.Flags,
            token.Label,
            token.Serial,
            token.Sender,
            token.LastCount
        );
        lock (owner.Gate) ValidateSlot(slot);
        using ModuleLiteralRoots801? literalRoots = ModuleLiteralRoots801.Capture801(owner,
            Term.Tuple(snapshot.Label, snapshot.Sender));
        lock (owner.Gate)
        {
            using var literalAdmission = owner.EnterLiteralAdmission801(literalRoots);
            ValidateSlot(slot);
            owner.ValidateGuestGraph507(snapshot.Label);
            owner.ValidateGuestGraph507(snapshot.Sender);
            TupleTerm value = owner.CreateMutableSequenceToken550(snapshot);
            PublishRoot512(slot, value);

            return value;
        }
    }

    // Native preparation remains outside R. Import the two arbitrary fields
    // with real construction custody, then allocate exactly the final six-word
    // tuple; no unused imported tuple or shadow VM allocation is introduced.
    internal TupleTerm PrepareMutableSequenceTokenInto550(int slot, SequenceTraceToken portable)
        => PrepareMutableSequenceTokenWithLiteral814(slot, portable, null);

    internal TupleTerm PrepareMutableSequenceTokenWithLiteral814(
        int slot, SequenceTraceToken portable, ModuleLiteralRoots801? carriedLiteral801)
    {
        owner.Runtime524.AssertSequenceTracePreparation();
        SequenceTraceToken snapshot = SequenceTraceToken.FromPortable((TupleTerm)portable.ToTerm());
        using PreparedHeapImport label = owner.PrepareHeapImportWithLiteral801(this, snapshot.Label, carriedLiteral801);
        using PreparedHeapImport sender = owner.PrepareHeapImportWithLiteral801(this, snapshot.Sender, carriedLiteral801);
        var imported = new SequenceTraceToken(
            snapshot.Flags,
            label.Value,
            snapshot.Serial,
            sender.Value,
            snapshot.LastCount
        );

        return CreateMutableSequenceTokenInto550(slot, imported);
    }
}
