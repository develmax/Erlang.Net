using System.Collections;
using System.Runtime.CompilerServices;

namespace Erlang.Tests;

internal static class FunEntryPublication407Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(FunEntryPublication407Names.Failure);
    }

    internal static int FunctionEntries(ProcessRuntime runtime)
        => ((IDictionary)typeof(RuntimeFunctionEntries).GetField(FunEntryPublication407Names.EntriesField,
            FunEntryPublication407Names.Fields)!.GetValue(runtime.Functions)!).Count;

    internal static void PrimitiveArchive(ProcessRuntime runtime)
    {
        var archive = (IDictionary)typeof(ModuleRegistry).GetField(FunEntryPublication407Names.MetadataField,
            FunEntryPublication407Names.Fields)!.GetValue(runtime.Modules)!;
        Require(archive.Count > 0);
        foreach (DictionaryEntry entry in archive)
        {
            var key = (ITuple)entry.Key;
            Require(key.Length == 4 && key[0] is string && key[1] is int && key[2] is int && key[3] is string);
            var fields = entry.Value!.GetType().GetFields(FunEntryPublication407Names.Fields);
            Require(fields.Length == 2 && fields.All(field => field.FieldType == typeof(int)));
        }
    }

    internal static void Purge(ProcessRuntime runtime, string module)
    {
        Require(runtime.Modules.DeleteModule(module).Equals(Term.A(ErlangBooleanAtoms.True)));
        object session = NifAdmissionHostBridge.Call(runtime.Modules, BeamStoredFunction406Names.PreparePurge, module)!;
        Require((bool)NifAdmissionHostBridge.Call(
            runtime.Modules,
            BeamStoredFunction406Names.FinishPurge,
            session,
            true
        )!);
    }

    internal static FunctionTerm ImportForged(ProcessContext context)
    {
        var identity = new LocalFunctionIdentity(
            Term.A(BeamClosureHostNames.Module),
            null,
            0,
            FunEntryPublication407Names.ForgedOldUnique,
            new BitString(Convert.FromHexString(BeamClosureHostNames.Checksum)),
            FunEntryPublication407Names.ForgedOldIndex
        );
        var wire = new FunctionTerm(
            identity,
            1,
            [Term.I(BeamClosureHostNames.Capture)],
            (_, _) => throw new InvalidOperationException(FunEntryPublication407Names.Failure)
        );
        var options = new EtfEncodingOptions { FunctionOwner = context.Self };

        return (FunctionTerm)context.Runtime.ImportExternalTerm(ExternalTermFormat.Encode(wire, options)).Term;
    }

    internal static void Metadata(FunctionTerm value, bool canonical)
        => Require(value.LocalIdentity!.OldUnique == (canonical ? BeamClosureHostNames.OriginalUnique : FunEntryPublication407Names.ForgedOldUnique)
            && value.LocalIdentity.OldIndex == (canonical ? 0 : FunEntryPublication407Names.ForgedOldIndex));
}
