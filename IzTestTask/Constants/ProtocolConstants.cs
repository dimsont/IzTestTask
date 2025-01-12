namespace IzTestTask.Constants;

public static class ProtocolConstants
{
    public const byte StartCode = 0xA5;
    public const byte EndCode = 0x5A;
    public const byte AckResponse = 0x41;
    public const byte NakResponse = 0x42;

    public static class ControlItems
    {
        public const byte ReceiverState = 0x18;
        public const byte ReceiverFrequency = 0x20;
    }

    public static class Ports
    {
        public const int DefaultTcp = 50000;
        public const int DefaultUdp = 60000;
    }

    public static class IqData
    {
        public const int SampleSize = 4; // 2 bytes for I + 2 bytes for Q
    }
}