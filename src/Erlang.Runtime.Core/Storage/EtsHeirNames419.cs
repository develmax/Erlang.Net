namespace Erlang;

internal static class EtsHeirNames419
{
    internal const string Module = "ets", SetOptions = "setopts", GiveAway = "give_away";
    internal const string Heir = "heir", Protection = "protection", None = "none", Transfer = "ETS-TRANSFER";
    internal const string NotOwner = "not_owner", Owner = "owner";
    internal const string Foreign = "ETS heir native entry requires its actual runtime actor and registry";
    internal const string Conflict = "ETS heir native entry conflicts with an existing binding";
    internal const string MailboxAdmission = "ETS transfer mailbox admission is not its prepared owner or already committed";
    internal const string Invalid = "ETS heir native entry arity is not admitted";
    internal static readonly (string Function, int Arity)[] Signatures = [(SetOptions, 2), (GiveAway, 3)];
}
