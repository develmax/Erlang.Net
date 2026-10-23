using System.Runtime.InteropServices;
using System.Text;

namespace Erlang;

// Own VM term identities are authenticated handles, never borrowed ERTS heap
// words. PID/port maps retain immutable identifiers, not ProcessContext objects.
internal sealed partial class ErlDriverTermTokens420 : IDisposable
{
    private const int MaximumAtomCharacters = 255;
    private const nuint NilToken = 1;
    private readonly object gate = new();
    private readonly Dictionary<nuint, Term> values = new() { [NilToken] = Nil.Value };
    private readonly Dictionary<string, nuint> atoms = new(StringComparer.Ordinal);
    private readonly Dictionary<ulong, nuint> actors = new();
    private readonly Dictionary<ulong, nuint> ports = new();
    private nuint next = NilToken;
    private bool closed;

    internal nuint NilValue => NilToken;

    internal Term Resolve(nuint token)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            if (!values.TryGetValue(token, out var value))
                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

            return value;
        }
    }

    private nuint Add(Term value)
    {
        nuint token = checked(next + 1);
        values.Add(token, value);
        next = token;

        return token;
    }

    internal nuint Atom(nint latin1)
    {
        if (latin1 == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        var bytes = new byte[MaximumAtomCharacters];
        int length = 0;
        while (length < bytes.Length)
        {
            byte character = Marshal.ReadByte(latin1, length);
            if (character == 0) break;
            bytes[length++] = character;
        }
        // Pinned driver_mk_atom uses LATIN1 with truncation enabled.
        string name = Encoding.Latin1.GetString(bytes, 0, length);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            if (atoms.TryGetValue(name, out nuint existing)) return existing;
            nuint token = Add(Term.A(name));
            try
            {
                atoms.Add(name, token);
            }
            catch
            {
                values.Remove(token);
                throw;
            }

            return token;
        }
    }

    internal nuint Actor(ulong identity, Pid value)
        => Bind(actors, identity, value);

    internal nuint Port(ulong identity, PortTerm value)
        => Bind(ports, identity, value);

    internal ulong ActorIdentity(nuint token)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            foreach (var actor in actors)
                if (actor.Value == token) return actor.Key;
        }
        throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
    }

    internal bool TryInternalActor455(nuint token, out ulong actor)
    {
        lock (gate)
        {
            if (!closed)
                foreach (var pair in actors)
                    if (pair.Value == token)
                    {
                        actor = pair.Key;

                        return true;
                    }
            actor = 0;

            return false;
        }
    }

    private nuint Bind(Dictionary<ulong, nuint> identities, ulong identity, Term value)
    {
        if (identity == 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            if (identities.TryGetValue(identity, out nuint existing))
            {
                switch (values[existing], value)
                {
                    case (Pid admitted, Pid fresh) when admitted.Id == fresh.Id:
                        admitted.LocalIdentity!.Publish(fresh.Node, fresh.Creation);
                        break;
                    case (PortTerm admitted, PortTerm fresh) when admitted.Id == fresh.Id:
                        admitted.LocalIdentity!.Publish(fresh.Node, fresh.Creation);
                        break;
                    default:
                        throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                }

                return existing;
            }
            Term owned = value switch
            {
                Pid pid => new Pid(new LocalNodeIdentity(pid.Node, pid.Creation), pid.Id),
                PortTerm port => new PortTerm(new LocalNodeIdentity(port.Node, port.Creation), port.Id),
                _ => throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame)
            };
            nuint token = Add(owned);
            try
            {
                ownIdentities455.Add(owned, new(value is Pid, identity));
                identities.Add(identity, token);
            }
            catch
            {
                ownIdentities455.Remove(owned);
                values.Remove(token);
                throw;
            }

            return token;
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (closed) return;
            closed = true;
            values.Clear();
            atoms.Clear();
            actors.Clear();
            ports.Clear();
            ownIdentities455.Clear();
        }
    }
}
