namespace Erlang.Tests;

public static class AtomChannelIndexContractConstants
{
    public const string Prefix = "atom_channel_contract_";
    public const string RetentionError = "Atom channel indexes must be unique and retain each name exactly once";
    public const string ChangedIndexError = "Repeated atom observation changed its channel index";
}
