using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Erlang;

// Evidence on the actual executing implementation, not a guest-selected label,
// empty fake opcode vector, invented address span, or CLR allocation sample.
internal sealed record DebugCodeProfile427(
    Type Owner,
    MethodInfo Entry,
    MethodInfo MachineBody,
    Guid ModuleIdentity,
    string EntryIlSha256,
    string MachineIlSha256
)
{
    internal static DebugCodeProfile427? CaptureManagedRegisterOwner(Type owner, string entry, Type registerState)
    {
        const BindingFlags methods = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo? method = owner.GetMethod(entry, methods);
        Type? machine = method?.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
        MethodInfo? body = machine?.GetMethod(DebugCodeNames427.MachineMoveNext, methods);
        if (method is null || body is null || !method.IsStatic || body.IsStatic || method.DeclaringType != owner
            || machine!.Assembly != owner.Assembly || body.Module != method.Module
            || (method.MethodImplementationFlags & MethodImplAttributes.CodeTypeMask) != MethodImplAttributes.IL
            || (body.MethodImplementationFlags & MethodImplAttributes.CodeTypeMask) != MethodImplAttributes.IL
            || method.IsDefined(typeof(DllImportAttribute), inherit: false) || body.IsDefined(typeof(DllImportAttribute), inherit: false)
            || !machine.GetFields(methods).Any(field => field.FieldType == registerState)) return null;
        byte[]? outer;
        byte[]? inner;
        try
        {
            outer = method.GetMethodBody()?.GetILAsByteArray();
            inner = body.GetMethodBody()?.GetILAsByteArray();
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        if (outer is not { Length: > 0 } || inner is not { Length: > 0 }) return null;

        return new(
            owner,
            method,
            body,
            method.Module.ModuleVersionId,
            Convert.ToHexString(SHA256.HashData(outer)),
            Convert.ToHexString(SHA256.HashData(inner))
        );
    }
}
