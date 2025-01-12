using IzTestTask.Constants;
using System.Buffers.Binary;

namespace IzTestTask.Models;

public class NetSdrMessage
{
    public byte ControlItemCode { get; set; }
    public byte[] Data { get; set; } = [];

    public byte[] ToByteArray()
    {
        // Calculate message length (control item + data length + data)
        var length = (ushort)(1 + 1 + Data.Length);

        using var memoryStream = new MemoryStream();
        memoryStream.WriteByte(ProtocolConstants.StartCode);

        var lengthBytes = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(lengthBytes, length);
        memoryStream.Write(lengthBytes);

        memoryStream.WriteByte(ControlItemCode);
        memoryStream.WriteByte((byte)Data.Length);
        memoryStream.Write(Data);
        memoryStream.WriteByte(ProtocolConstants.EndCode);

        return memoryStream.ToArray();
    }
}