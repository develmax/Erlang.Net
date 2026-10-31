namespace Erlang.NifNativeBackend;

public static class MailboxPolicyConversionCaseNames
{
    public const string Reversal = "mailbox/policy-requested-reversal-keeps-effective-off-until-real-move-and-post-gc-import";
    internal const string ControlProperty = "Control";
    internal const string AdmissionProperty = "Admission";
    internal static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(1);
    public const string OnToOff = "mailbox/policy-on-to-off-existing-physical-message";
    public const string OffToOn = "mailbox/policy-off-to-on-gc-adoption";
}
