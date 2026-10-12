using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Erlang;

public static class ModuleCodeFingerprints
{
    public static Term Compilation(Type owner)
    {
        var assembly = owner.Assembly.GetName();

        return Term.List(
            Term.Tuple(Term.A(ModuleCodeMetadataNames.Backend), Term.A(ModuleCodeMetadataNames.ManagedBackend)),
            Term.Tuple(Term.A(ModuleCodeMetadataNames.Assembly), Term.String(assembly.Name ?? string.Empty)),
            Term.Tuple(Term.A(ModuleCodeMetadataNames.Version), Term.String(assembly.Version?.ToString() ?? string.Empty))
        );
    }

    public static ModuleCodeDescriptor Owner(Type owner)
    {
        var image = Encoding.UTF8.GetBytes(owner.Module.ModuleVersionId.ToString() + ModuleCodeMetadataNames.MethodSeparator + owner.FullName);

        return new ModuleCodeDescriptor(
            [],
            [],
            new BitString(MD5.HashData(image)),
            Compilation(owner)
        );
    }

    internal static BitString Registered(string module, IEnumerable<((string Name, int Arity) Signature, ErlangFunction Body)> entries, BitString? root)
    {
        var text = new StringBuilder(module);
        foreach (var entry in entries.OrderBy(entry => entry.Signature.Name, StringComparer.Ordinal).ThenBy(entry => entry.Signature.Arity))
        {
            var method = entry.Body.Method;
            text.Append(ModuleCodeMetadataNames.LineSeparator);
            text.Append(entry.Signature.Name);
            text.Append(ModuleCodeMetadataNames.SignatureSeparator);
            text.Append(entry.Signature.Arity.ToString(CultureInfo.InvariantCulture));
            text.Append(ModuleCodeMetadataNames.MethodSeparator);
            text.Append(method.Module.ModuleVersionId);
            text.Append(ModuleCodeMetadataNames.MethodSeparator);
            text.Append(method.MetadataToken.ToString(CultureInfo.InvariantCulture));
        }
        byte[] manifest = Encoding.UTF8.GetBytes(text.ToString());
        byte[] data = root is null ? manifest : root.ToArray().Concat(manifest).ToArray();

        return new BitString(MD5.HashData(data));
    }
}
