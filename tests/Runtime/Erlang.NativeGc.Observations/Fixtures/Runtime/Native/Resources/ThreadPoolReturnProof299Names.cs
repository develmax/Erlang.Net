namespace Erlang.Tests;

internal static class ThreadPoolReturnProof299Names
{
    internal const string MethodName = "UnsafeQueueUserWorkItem";
    internal const string MissingMethod = "Exact IThreadPoolWorkItem/bool overload is absent.";
    internal const string ReturnType = "Return type is not bool.";
    internal const string MissingBody = "No inspectable IL body.";
    internal const string ExceptionRegion = "Exception regions require a separate proof; refusing.";
    internal const string MissingIl = "No IL bytes.";
    internal const string InvalidTarget = "Branch target is not an instruction boundary.";
    internal const string JumpOpcode = "jmp";
    internal const string JumpRefused = "jmp requires an interprocedural proof; refusing.";
    internal const string ReturnOpcode = "ret";
    internal const string TrueOpcode = "ldc.i4.1";
    internal const string NonConstantReturn = "A ret can execute without its adjacent constant-true producer; refusing.";
    internal const string NoReturns = "No normal ret found; refusing a vacuous proof.";
    internal const string ProvedStatus = "PROVED_ALL_NORMAL_RET_CONSTANT_TRUE";
    internal const string ProofScope = "Exact pinned managed IL normal-return values only. No queue execution, forced failure, shutdown inference or other runtime claim.";
    internal const string RefusedStatus = "REFUSED";
    internal const string InvalidSwitch = "Invalid switch length.";
    internal const string TargetSeparator = ", ";
    internal const string RoundTripFormat = "R";
    internal const string UnnamedOpcode = "Unnamed opcode.";
    internal const string UnresolvedToken = "Unresolved metadata token.";
    internal const string TruncatedOperand = "Truncated IL operand.";
    internal const string RuntimeMismatch = "Expected the pinned .NET runtime version 10.0.11.";
    internal const string ProofRefused = "Pinned ThreadPool normal-return IL proof was refused.";
    internal const string UnknownOpcode = "Unknown opcode at offset: ";
    internal const string UnsupportedOperand = "Unsupported operand kind: ";
}
