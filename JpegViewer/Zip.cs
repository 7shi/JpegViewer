// public domain

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

public static class Zip
{
    private static readonly uint[] crc32_table = new Func<uint[]>(() =>
    {
        var ret = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var crc = i;
            for (int j = 0; j < 8; j++)
            {
                var crc2 = crc >> 1;
                crc = (crc & 1) == 0 ? crc2 : crc2 ^ 0xedb88320u;
            }
        }
        return ret;
    })();

    public static uint Crc32(byte[] buf)
    {
        var crc = ~0u;
        foreach (var b in buf)
            crc = (crc >> 8) ^ crc32_table[(crc ^ b) & 0xff];
        return ~crc;
    }

    public static ZipDirHeader[] GetFiles(BinaryReader br, Func<ZipDirHeader, bool> f)
    {
        var list = new List<ZipDirHeader>();

        var fs = br.BaseStream;
        if (fs.Length < 22)
            throw new Exception("ファイルが小さ過ぎます。");

        fs.Position = fs.Length - 22;
        if (br.ReadInt32() != 0x06054b50)
            throw new Exception("ヘッダが見付かりません。");

        fs.Position += 6;
        int count = br.ReadUInt16();
        var dir_len = br.ReadUInt32();
        var dir_start = br.ReadUInt32();

        fs.Position = dir_start;
        for (int i = 0; i < count; i++)
        {
            if (br.ReadInt32() != 0x02014b50)
                throw new Exception("ファイルが壊れています。");
            var zipdh = new ZipDirHeader(br);
            if (f(zipdh)) list.Add(zipdh);
        }

        return list.ToArray();
    }
}

public class ZipHeader
{
    public ushort Version, Flags, Compression, DosTime, DosDate;
    public uint Crc32, CompressedSize, UncompressedSize;
    public ushort FilenameLength, ExtraFieldLength;

    public ZipHeader(BinaryReader br)
    {
        Version = br.ReadUInt16();
        Flags = br.ReadUInt16();
        Compression = br.ReadUInt16();
        DosTime = br.ReadUInt16();
        DosDate = br.ReadUInt16();
        Crc32 = br.ReadUInt32();
        CompressedSize = br.ReadUInt32();
        UncompressedSize = br.ReadUInt32();
        FilenameLength = br.ReadUInt16();
        ExtraFieldLength = br.ReadUInt16();
    }
}

public class ZipDirHeader
{
    public ushort Version;
    public ZipHeader Header;
    public ushort FileCommentLength, DiskNumberStart, InternalFileAttrs;
    public uint Attrs, Position;
    public byte[] Filename;

    public ZipDirHeader(BinaryReader br)
    {
        Version = br.ReadUInt16();
        Header = new ZipHeader(br);
        FileCommentLength = br.ReadUInt16();
        DiskNumberStart = br.ReadUInt16();
        InternalFileAttrs = br.ReadUInt16();
        Attrs = br.ReadUInt32();
        Position = br.ReadUInt32();
        Filename = br.ReadBytes(Header.FilenameLength);
        var exlen = Header.ExtraFieldLength + FileCommentLength;
        br.BaseStream.Seek(exlen, SeekOrigin.Current);
    }

    public SubStream GetSubStream(BinaryReader br)
    {
        var fs = br.BaseStream;
        fs.Position = Position;
        if (br.ReadInt32() != 0x04034b50)
            throw new Exception("ファイルが壊れています。");

        var ziph = new ZipHeader(br);
        fs.Position += ziph.FilenameLength + ziph.ExtraFieldLength;
        return new SubStream(fs, Header.CompressedSize);
    }
}

public class SubStream : Stream
{
    private Stream s;
    private long start, length, pos;

    public SubStream(Stream s, long length)
    {
        this.s = s;
        this.start = s.Position;
        this.length = length;
    }

    public override long Length { get { return length; } }
    public override bool CanRead { get { return pos < length; } }
    public override bool CanWrite { get { return false; } }
    public override bool CanSeek { get { return true; } }
    public override void Flush() { }

    public override long Position
    {
        get { return pos; }
        set { s.Position = start + (pos = value); }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!CanRead) return 0;
        count = (int)Math.Min(length - pos, count);
        int ret = s.Read(buffer, offset, count);
        pos += ret;
        return ret;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin: Position = offset; break;
            case SeekOrigin.Current: Position += offset; break;
            case SeekOrigin.End: Position = length + offset; break;
        }
        return pos;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }
}
