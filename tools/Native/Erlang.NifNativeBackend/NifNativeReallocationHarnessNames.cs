using System.Reflection;

namespace Erlang.NifNativeBackend;

internal static class NifNativeReallocationHarnessNames
{
    internal const string Mode = "--reallocation-probe";
    internal const string Broken = "Trusted native reallocation probe invariant failed.";
    internal const string Usage = "Reallocation mode requires newFixture/hash, oldFixture/hash and WindowsLayout/hash or oldBridge/hash plus extension/hash.";
    internal const string CaseRole = "actual-native-reallocation-case";
    internal const string ControlsRole = "actual-native-reallocation-controls";
    internal const string NullPositive = "null_positive";
    internal const string NullZero = "null_zero";
    internal const string Sequence = "resize_sequence";
    internal const string SizesKind = "AuthoredControl";
    internal const string Greeting = "native realloc probe";
    internal const string WrongModule = "reallocation_one_shot_wrong_module";
    internal const string AllocationsField = "allocations";
    internal const string GateField = "gate";
    internal const string GrantField = "reallocationEnabled";
    internal const string RuntimeIdentity = "Runtime";
    internal const string NewIdentity = "New";
    internal const string OldIdentity = "Old";
    internal const string PointerBytes = "PointerBytes";
    internal const string TableBytes = "TableBytes";
    internal const string FieldCount = "FieldCount";
    internal const string ReallocationSlot = "ReallocationSlot";
    internal const string ReallocationOffset = "ReallocationOffset";
    internal const string FirstTailSlot = "FirstTailSlot";
    internal const string SecondTailSlot = "SecondTailSlot";
    internal const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    internal const BindingFlags Constructors = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
}
