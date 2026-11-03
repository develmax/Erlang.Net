using System.Collections.Frozen;

namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal IReadOnlyDictionary<(string Function, int Arity), ErlangFunction> CaptureBeamPrimitives(BeamLinkedCode code)
    {
        var primitives = new Dictionary<(string Function, int Arity), ErlangFunction>();
        lock (gate)
        {
            foreach (BeamLinkedFunction function in code.Functions.Where(function => BeamNativePrimitives.IsStub(code, function)))
            {
                var signature = (function.Module, function.Name, function.Arity);
                if (!sourceOwners.ContainsKey(signature) && IsNativeEntry(signature.Module, signature.Name, signature.Arity))
                    primitives.Add((function.Name, function.Arity), functions[signature]);
                else if (sourceOwners.GetValueOrDefault(signature) is PreparedBeamImage previous
                    && ReferenceEquals(codeImages.GetValueOrDefault(signature.Module), previous.ExecutionIdentity.Image)
                    && previous.ExecutionIdentity.NativePrimitives.TryGetValue((function.Name, function.Arity), out ErlangFunction? primitive))
                    primitives.Add((function.Name, function.Arity), primitive);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && UnicodeNative.TryCapture(
                        this,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction unicode
                    )
                    && code.Exports.Any(export => export.Name == UnicodeNativeNames.ToBinaryTrap && export.Arity == 2)
                    && code.Exports.Any(export => export.Name == UnicodeNativeNames.ToListTrap && export.Arity == 2))
                    primitives.Add((function.Name, function.Arity), unicode);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && MathNative.TryCapture(
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction math
                    ))
                    primitives.Add((function.Name, function.Arity), math);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && FileNameEncodingNative.TryCapture(
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction encoding
                    ))
                    primitives.Add((function.Name, function.Arity), encoding);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && WarningMapNative.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction warningMap
                    ))
                    primitives.Add((function.Name, function.Arity), warningMap);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && OsNative.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction os
                    ))
                    primitives.Add((function.Name, function.Arity), os);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && NetKernelNative.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction netKernel
                    ))
                    primitives.Add((function.Name, function.Arity), netKernel);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && IoPrintableRangeNative.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction printableRange
                    ))
                    primitives.Add((function.Name, function.Arity), printableRange);
                else
                    throw new NotSupportedException(BeamRegisterNames.Unsupported);
            }
        }

        return primitives.ToFrozenDictionary();
    }
}
