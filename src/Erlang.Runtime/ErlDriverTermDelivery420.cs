namespace Erlang;

internal sealed record ErlDriverTermDelivery420(
    ulong RecipientActor,
    byte[] RecipientEtf,
    byte[] MessageEtf,
    bool UndefinedSender
);
