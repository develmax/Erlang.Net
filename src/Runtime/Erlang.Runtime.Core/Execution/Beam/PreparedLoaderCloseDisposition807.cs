namespace Erlang;

// Privileged close metadata, not a new image/term acquisition capability.
// Native image reads remain authoritative through the real payload handle.
internal sealed record PreparedLoaderCloseDisposition807(
    PreparedLoaderOwner807 Owner807,
    PreparedBeamImage Image807,
    Task CodeClose807,
    bool Consumed807
);
