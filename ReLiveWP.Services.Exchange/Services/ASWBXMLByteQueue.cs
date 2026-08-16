using System.Text;

namespace ReLiveWP.Services.Exchange.Services;

// mostly yoinked from https://learn.microsoft.com/en-us/previous-versions/office/developer/exchange-server-interoperability-guidance/hh361570(v=exchg.140)
class ASWBXMLByteQueue : Queue<byte>
{
    public ASWBXMLByteQueue(byte[] bytes)
        : base(bytes)
    {
    }

    public int DequeueMultibyteInt()
    {
        int iReturn = 0;
        byte singleByte = 0xFF;

        do
        {
            iReturn <<= 7;

            singleByte = this.Dequeue();
            iReturn += (int)(singleByte & 0x7F);
        }
        while (CheckContinuationBit(singleByte));

        return iReturn;
    }

    private bool CheckContinuationBit(byte byteval)
    {
        byte continuationBitmask = 0x80;
        return (continuationBitmask & byteval) != 0;
    }

    // STR_I inline text: UTF-8 (charset 0x6A), NUL-terminated. A UTF-8 sequence never
    // contains a 0x00 byte, so terminating on 0x00 can't split a multi-byte character.
    public string DequeueString()
    {
        List<byte> bytes = new List<byte>();
        byte currentByte;
        while ((currentByte = this.Dequeue()) != 0x00)
        {
            bytes.Add(currentByte);
        }

        return Encoding.UTF8.GetString([.. bytes]);
    }

    // Length-prefixed OPAQUE data is a raw byte channel; the caller decides whether the run is
    // binary or text, so it never gets a decoding forced on it here.
    public byte[] DequeueBytes(int length)
    {
        var bytes = new byte[length];

        for (int i = 0; i < length; i++)
            bytes[i] = this.Dequeue();

        return bytes;
    }
}
