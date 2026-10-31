using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

internal static unsafe class NifNativeBridgeFixedHostChecks
{
    internal static void Layout(ProcessContext context)
    {
        NifNativeBridgeTable table = Table();
        using var environmentTable = new NifNativeWindowsCallbacks();
        environmentTable.BindIndependentEnvironments(context.Runtime, new ModuleCodeImage(NifNativeBridgeFixedHostNames.Module));
        NifNativeReferenceBridgeTable.Bind(ref table, ((nint*)environmentTable.Pointer)[NifNativeEnvironmentLayout.Allocate]);
        Require(table.Version == NifNativeBridgeFixedHostNames.ExpectedVersion);
        Require(table.Size == NifNativeBridgeFixedHostNames.ExpectedTableBytes);
        Require(sizeof(NifNativeBridgeTable) == NifNativeBridgeFixedHostNames.ExpectedTableBytes);
        string[] prefix =
        [
            "PrivateData", "Allocate", "Free", "GetInt", "BadArgument", "MakeInt",
            "MakeString", "IsException", "HasPendingException", "RaiseException", "MakeTuple", "MakeList"
        ];
        string[] extension =
        [
            "IsAtom", "IsBinary", "IsReference", "IsFunction", "IsPid", "IsPort",
            "IsEmptyList", "IsList", "IsTuple", "IsNumber", "IsMap", "GetListCell",
            "MakeTupleFromArray", "MakeListFromArray", "GetUnsignedLong", "GetDouble",
            "GetUnsignedInt", "GetLong", "GetInt64", "GetUInt64", "MakeUnsignedLong",
            "MakeDouble", "MakeUnsignedInt", "MakeLong", "MakeInt64", "MakeUInt64",
            "GetMapSize", "MakeReference", "AllocateEnvironment", "FreeEnvironment", "ClearEnvironment", "CopyTerm", "ThreadType521"
        ];
        Require(prefix.Length == NifNativeBridgeFixedHostNames.LegacyPointerCount);
        Require(extension.Length == NifNativeBridgeFixedHostNames.AddedPointerCount);
        string[] resources =
        [
            nameof(NifNativeBridgeTable.OpenResourceType527),
            nameof(NifNativeBridgeTable.AllocateResource527),
            nameof(NifNativeBridgeTable.ReleaseResource527),
            nameof(NifNativeBridgeTable.MakeResource527),
            nameof(NifNativeBridgeTable.GetResource527),
            nameof(NifNativeBridgeTable.SizeOfResource527),
            nameof(NifNativeBridgeTable.KeepResource527),
            nameof(NifNativeBridgeTable.MakeResourceBinary527),
            nameof(NifNativeBridgeTable.OpenResourceTypeX527),
            nameof(NifNativeBridgeTable.InitResourceType527),
            nameof(NifNativeBridgeTable.DynamicResourceCall527)
        ];
        string[] cons = [nameof(NifNativeBridgeTable.MakeListCell539)];
        string[] textTuple719 = [nameof(NifNativeBridgeTable.GetString719), nameof(NifNativeBridgeTable.GetTuple719), nameof(NifNativeBridgeTable.MakeAtom719)];
        Require(textTuple719.Length == NifNativeBridgeFixedHostNames.TextTuplePointerCount719);
        Require(resources.Length == NifNativeBridgeFixedHostNames.ResourcePointerCount);
        Require(cons.Length == NifNativeBridgeFixedHostNames.ConsPointerCount);
        Require(Marshal.OffsetOf<NifNativeBridgeTable>(resources[0]).ToInt64() == NifNativeBridgeFixedHostNames.ResourceAppendOffset);
        Require(Marshal.OffsetOf<NifNativeBridgeTable>(cons[0]).ToInt64() == NifNativeBridgeFixedHostNames.ConsAppendOffset);
        string[] printf791 = [nameof(NifNativeBridgeTable.FormatTerm791)];
        Require(printf791.Length == NifNativeBridgeFixedHostNames.PrintfPointerCount791);
        Require(Marshal.OffsetOf<NifNativeBridgeTable>(printf791[0]).ToInt64() == NifNativeBridgeFixedHostNames.PrintfAppendOffset791);
        string[] fields = [.. prefix, .. extension, .. resources, .. cons, .. textTuple719, .. printf791];
        Require(NifNativeBridgeFixedHostNames.MetadataBytes + fields.Length * sizeof(nint) == NifNativeBridgeFixedHostNames.ExpectedTableBytes);
        var pointers = (nint*)((byte*)&table + NifNativeBridgeFixedHostNames.MetadataBytes);
        for (int index = 0; index < fields.Length; index++)
        {
            Require(Marshal.OffsetOf<NifNativeBridgeTable>(fields[index]).ToInt64() ==
                NifNativeBridgeFixedHostNames.MetadataBytes + index * sizeof(nint));
            Require(pointers[index] != 0);
        }
        Require(table.ThreadType521() == NifNativeScheduling521Names.NormalThread);
        Require((nint)table.GetLong == (nint)table.GetInt64);
        Require((nint)table.GetUnsignedLong == (nint)table.GetUInt64);
        Require((nint)table.MakeLong == (nint)table.MakeInt64);
        Require((nint)table.MakeUnsignedLong == (nint)table.MakeUInt64);
    }

    internal static void LongWidth(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(NifNativeBridgeFixedHostNames.Module),
            0,
            state
        );
        NifNativeBridgeTable table = Table();
        // LP64 long is eight bytes even while these managed table controls run on Windows.
        foreach (long value in new[] { long.MinValue, (long)int.MinValue - 1, 0L, (long)int.MaxValue + 1, long.MaxValue })
        {
            nuint token = table.MakeLong(owner.Pointer, value);
            long output = 0;
            Require(owner.Get(token) is Integer number && number.Value == value);
            Require(table.GetLong(0, token, &output) == 1 && output == value);
        }
        foreach (ulong value in new[] { 0UL, (ulong)uint.MaxValue + 1, ulong.MaxValue })
        {
            nuint token = table.MakeUnsignedLong(owner.Pointer, value);
            ulong output = 0;
            Require(owner.Get(token) is Integer number && number.Value == value);
            Require(table.GetUnsignedLong(0, token, &output) == 1 && output == value);
        }
        ulong refused = ulong.MaxValue;
        Require(table.GetUnsignedLong(0, owner.Add(Term.I(-1)), &refused) == 0);
        Require(refused == NifNativeBridgeFixedHostNames.ExpectedBadArgumentOutput);
        Require(table.GetUnsignedLong(0, owner.Add(new Integer(BigInteger.One << 64)), &refused) == 0);
        Require(refused == NifNativeBridgeFixedHostNames.ExpectedSystemLimitOutput);
        state.ThrowPending();
    }

    internal static void CompoundTerms(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(NifNativeBridgeFixedHostNames.Module),
            0,
            state
        );
        NifNativeBridgeTable table = Table();
        nuint atom = owner.Add(Term.A(NifNativeBridgeFixedHostNames.Module));
        nuint integer = owner.Add(Term.I(7));
        nuint* values = stackalloc nuint[] { atom, integer, atom };
        nuint tuple = table.MakeTupleFromArray(owner.Pointer, values, 3);
        nuint list = table.MakeListFromArray(owner.Pointer, values, 3);
        values[0] = integer;
        Require(owner.Get(tuple).Equals(Term.Tuple(owner.Get(atom), Term.I(7), owner.Get(atom))));
        Require(owner.Get(list).Equals(Term.List(owner.Get(atom), Term.I(7), owner.Get(atom))));
        Require(table.IsTuple(0, tuple) == 1 && table.IsList(0, list) == 1);
        Require(table.IsAtom(0, atom) == 1 && table.IsNumber(0, integer) == 1);
        nuint head = 0, tail = 0;
        Require(table.GetListCell(
            0,
            list,
            &head,
            &tail
        ) == 1);
        Require(owner.Get(head).Equals(owner.Get(atom)));
        Require(owner.Get(tail).Equals(Term.List(Term.I(7), owner.Get(atom))));
        Require(table.GetListCell(
            0,
            atom,
            null,
            null
        ) == 0);
        // Invoke the appended constructor, not merely compare a non-NULL slot.
        nuint improper = table.MakeListCell539(owner.Pointer, atom, integer);
        Require(improper != 0 && owner.Get(improper).Equals(new Cons(owner.Get(atom), Term.I(7))));
        nuint proper = table.MakeListCell539(owner.Pointer, atom, list);
        Require(proper != 0 && owner.Get(proper).Equals(new Cons(owner.Get(atom), owner.Get(list))));
        nuint constructedHead = 0, constructedTail = 0;
        Require(table.GetListCell(
            owner.Pointer,
            improper,
            &constructedHead,
            &constructedTail
        ) == 1);
        Require(owner.Get(constructedHead).Equals(owner.Get(atom)) && owner.Get(constructedTail).Equals(Term.I(7)));
        Require(owner.Get(table.MakeTupleFromArray(owner.Pointer, null, 0)).Equals(Term.Tuple()));
        Require(owner.Get(table.MakeListFromArray(owner.Pointer, null, 0)) is Nil);
        state.ThrowPending();
    }

    private static NifNativeBridgeTable Table()
        => (NifNativeBridgeTable)typeof(NifNativeExportRegistration)
            .GetMethod(NifNativeBridgeFixedHostNames.Factory, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, null)!;

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeBridgeFixedHostNames.Failure);
    }
}
