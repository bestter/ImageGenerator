using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace ImageGeneratorApp.Tests
{
    // Independent container checks: do not rely on the encoder's attribute dictionary.
    internal static class ImageFormatInspector
    {
        internal static IEnumerable<(string Type, byte[] Data)> PngChunks(byte[] bytes)
        {
            bytes.Take(8).Should().Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            for (int offset = 8; offset < bytes.Length;)
            {
                int length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4)));
                string type = Encoding.ASCII.GetString(bytes, offset + 4, 4);
                yield return (type, bytes.AsSpan(offset + 8, length).ToArray());
                offset = checked(offset + 12 + length);
            }
        }

        internal static Dictionary<string, string> PngText(byte[] bytes)
        {
            var result = new Dictionary<string, string>();
            foreach ((string type, byte[] data) in PngChunks(bytes))
            {
                if (type != "tEXt" && type != "iTXt" && type != "zTXt")
                    continue;
                int separator = Array.IndexOf(data, (byte)0);
                string key = Encoding.Latin1.GetString(data, 0, separator);
                int start = separator + 1;
                bool compressed = type == "zTXt" || (type == "iTXt" && data[start] == 1);
                if (type == "zTXt") start++;
                if (type == "iTXt")
                {
                    start += 2;
                    start = Array.IndexOf(data, (byte)0, start) + 1;
                    start = Array.IndexOf(data, (byte)0, start) + 1;
                }
                byte[] text = data.AsSpan(start).ToArray();
                if (compressed)
                {
                    using var input = new MemoryStream(text);
                    using var decoder = new ZLibStream(input, CompressionMode.Decompress);
                    using var output = new MemoryStream();
                    decoder.CopyTo(output);
                    text = output.ToArray();
                }
                result[key] = (type == "iTXt" ? Encoding.UTF8 : Encoding.Latin1).GetString(text);
            }
            return result;
        }

        internal static byte[] Profile(byte[] bytes, string format, string profile)
        {
            if (format == "png")
            {
                if (profile == "xmp") return Encoding.UTF8.GetBytes(PngText(bytes)["XML:com.adobe.xmp"]);
                return PngChunks(bytes).Single(c => c.Type == "eXIf").Data;
            }
            if (format == "webp")
            {
                Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("RIFF");
                for (int offset = 12; offset + 8 <= bytes.Length;)
                {
                    int length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 4, 4)));
                    string type = Encoding.ASCII.GetString(bytes, offset, 4);
                    if (type == (profile == "xmp" ? "XMP " : "EXIF"))
                        return bytes.AsSpan(offset + 8, length).ToArray();
                    offset += 8 + length + (length % 2);
                }
            }
            else
            {
                bytes.Take(2).Should().Equal(new byte[] { 255, 216 });
                for (int offset = 2; offset + 4 <= bytes.Length;)
                {
                    byte marker = bytes[offset + 1];
                    if (marker == 0xda || marker == 0xd9) break;
                    int length = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 2, 2));
                    if (marker == 0xe1)
                    {
                        byte[] data = bytes.AsSpan(offset + 4, length - 2).ToArray();
                        byte[] prefix = Encoding.ASCII.GetBytes(profile == "xmp" ? "http://ns.adobe.com/xap/1.0/\0" : "Exif\0\0");
                        if (data.AsSpan().StartsWith(prefix)) return data.AsSpan(prefix.Length).ToArray();
                    }
                    offset += length + 2;
                }
            }
            throw new InvalidDataException("Missing serialized profile: " + profile);
        }

        internal static Dictionary<ushort, string> ExifStrings(byte[] data)
        {
            int basis = data.AsSpan().StartsWith("Exif\0\0"u8) ? 6 : 0;
            bool little = data[basis] == (byte)'I';
            ushort U16(int at) => little ? BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(at, 2)) : BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(at, 2));
            uint U32(int at) => little ? BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(at, 4)) : BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(at, 4));
            var result = new Dictionary<ushort, string>();
            void ReadDirectory(int offset)
            {
                int count = U16(offset);
                for (int index = 0; index < count; index++)
                {
                    int entry = offset + 2 + (12 * index);
                    ushort tag = U16(entry);
                    if (tag == 0x8769) ReadDirectory(checked(basis + (int)U32(entry + 8)));
                    if (U16(entry + 2) != 2) continue;
                    int size = checked((int)U32(entry + 4));
                    int start = size <= 4 ? entry + 8 : checked(basis + (int)U32(entry + 8));
                    result[tag] = Encoding.ASCII.GetString(data, start, size).TrimEnd('\0');
                }
            }
            ReadDirectory(checked(basis + (int)U32(basis + 4)));
            return result;
        }
    }
}