namespace IzTestTask.Tests.Helpers;

public class FakeNetworkStream : Stream
{
    private readonly MemoryStream _innerStream = new();

    public override void Flush()
    {
        _innerStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _innerStream.Read(buffer, offset, count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("Seek operation is not supported.");
    }

    public override void SetLength(long value)
    {
        _innerStream.SetLength(value);
    }

    public byte[] GetSentMessages()
    {
        return _innerStream.ToArray();
    }
}