using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BitchManager.Protocols;

public static class ArtNetPacketBuilder
{
    public static byte[] BuildDmxPacket(int universe, IReadOnlyList<byte> channels, byte sequence = 0)
    {
        var data = new List<byte>(530);
        data.AddRange("Art-Net\0"u8.ToArray());     // header
        data.AddRange([0x00, 0x50]);                // OpDmx 0x5000, little endian
        data.AddRange([0x00, 0x0E]);                // protocol version 14
        data.AddRange([sequence, 0x00]);            // sequence, physical
        data.Add((byte)(universe & 0xFF));          // 15-bit port-address, little endian
        data.Add((byte)((universe >> 8) & 0x7F));
        data.AddRange([0x02, 0x00]);                // length 512, big endian
        for (var i = 0; i < 512; i++) data.Add(i < channels.Count ? channels[i] : (byte)0);
        return data.ToArray();
    }
}

public static class SacnPacketBuilder
{
    private static readonly byte[] SenderCid =
    [
        0x42, 0x42, 0x4D, 0x41, 0x4E, 0x41, 0x47, 0x45,
        0x52, 0x2D, 0x53, 0x41, 0x43, 0x4E, 0x30, 0x31,
    ];

    public static byte[] BuildDmxPacket(int universe, byte priority, IReadOnlyList<byte> channels, byte sequence = 0,
        string sourceName = "BitchManager Pro Show Control")
    {
        var data = new List<byte>(638);
        // Root layer
        data.AddRange([0x00, 0x10, 0x00, 0x00]);
        data.AddRange("ASC-E1.17\0\0\0"u8.ToArray());
        data.AddRange([0x72, 0x6E]);
        data.AddRange([0x00, 0x00, 0x00, 0x04]);
        data.AddRange(SenderCid);
        // Framing layer
        data.AddRange([0x72, 0x58]);
        data.AddRange([0x00, 0x00, 0x00, 0x02]);
        var name = Encoding.UTF8.GetBytes(sourceName);
        for (var i = 0; i < 64; i++) data.Add(i < Math.Min(63, name.Length) ? name[i] : (byte)0);
        data.Add(priority);
        data.AddRange([0x00, 0x00]);                // sync address
        data.Add(sequence);
        data.Add(0x00);                             // options
        data.Add((byte)((universe >> 8) & 0xFF));
        data.Add((byte)(universe & 0xFF));
        // DMP layer
        data.AddRange([0x72, 0x0B]);
        data.Add(0x02);
        data.Add(0xA1);
        data.AddRange([0x00, 0x00]);
        data.AddRange([0x00, 0x01]);
        data.AddRange([0x02, 0x01]);
        data.Add(0x00);                             // DMX start code
        for (var i = 0; i < 512; i++) data.Add(i < channels.Count ? channels[i] : (byte)0);
        return data.ToArray();
    }
}

public static class OscPacketBuilder
{
    private static void AddPadded(List<byte> data, byte[] bytes)
    {
        data.AddRange(bytes);
        data.Add(0);
        while (data.Count % 4 != 0) data.Add(0);
    }

    public static byte[] BuildMessage(string address, string argType, string argValue)
    {
        var data = new List<byte>();
        AddPadded(data, Encoding.UTF8.GetBytes(address));
        Span<byte> buf = stackalloc byte[4];
        switch (argType)
        {
            case "FLOAT":
                AddPadded(data, ",f"u8.ToArray());
                var f = float.TryParse(argValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var fv) ? fv : 1.0f;
                BinaryPrimitives.WriteSingleBigEndian(buf, f);
                data.AddRange(buf.ToArray());
                break;
            case "INT":
                AddPadded(data, ",i"u8.ToArray());
                var n = int.TryParse(argValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv) ? iv : 1;
                BinaryPrimitives.WriteInt32BigEndian(buf, n);
                data.AddRange(buf.ToArray());
                break;
            case "STRING":
                AddPadded(data, ",s"u8.ToArray());
                AddPadded(data, Encoding.UTF8.GetBytes(argValue));
                break;
            case "BOOLEAN_TRUE":
                AddPadded(data, ",T"u8.ToArray());
                break;
            case "BOOLEAN_FALSE":
                AddPadded(data, ",F"u8.ToArray());
                break;
            default: // NONE / bang
                data.AddRange([0x2C, 0x00, 0x00, 0x00]);
                break;
        }
        return data.ToArray();
    }
}

/// <summary>Fire-and-forget UDP sender used for OSC, Art-Net and sACN.</summary>
public static class Udp
{
    public static async Task<string?> SendAsync(string host, int port, byte[] payload)
    {
        try
        {
            using var client = new UdpClient();
            if (IPAddress.TryParse(host, out var ip))
            {
                if (ip.Equals(IPAddress.Broadcast) || host.EndsWith(".255")) client.EnableBroadcast = true;
                await client.SendAsync(payload, payload.Length, new IPEndPoint(ip, port)).ConfigureAwait(false);
            }
            else
            {
                await client.SendAsync(payload, payload.Length, host, port).ConfigureAwait(false);
            }
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
