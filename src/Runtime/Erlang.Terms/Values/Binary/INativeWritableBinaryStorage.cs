namespace Erlang;

internal interface INativeWritableBinaryStorage : INativeBinaryStorage524
{
    void PinWritable();

    byte ReadWritableByte(int index);

    IDisposable RetainCopyRead();
}
