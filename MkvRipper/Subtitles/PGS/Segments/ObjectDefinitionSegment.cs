using MkvRipper.Utils;

namespace MkvRipper.Subtitles.PGS.Segments;

public struct ObjectDefinitionSegment : IPresentationGraphicSegment
{
    /// <summary>
    /// The type byte in the PGS.
    /// </summary>
    public const byte Type = 0x15;

    /// <inheritdoc />
    static byte IPresentationGraphicSegment.Type => Type;

    public ObjectDefinitionSegment()
    {
        Id = 0;
        VersionNumber = 0;
        LastInSequenceFlag = 0;
        Width = 0;
        Height = 0;
        Data = Array.Empty<byte>();
    }
    
    public ushort Id { get; set; }
    public byte VersionNumber { get; set; }
    public byte LastInSequenceFlag { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public byte[] Data { get; set; }
    
    /// <summary>
    /// Gets if there is an empty object.
    /// </summary>
    public bool IsEmpty => Data.Length == 0;

    /// <inheritdoc />
    public void Read(BigEndianBinaryReader reader, ushort segmentLength)
    {
        Id = reader.ReadUInt16();
        VersionNumber = reader.ReadByte();
        LastInSequenceFlag = reader.ReadByte();
        var length = reader.ReadUInt24();
        Width = reader.ReadUInt16();
        Height = reader.ReadUInt16();
        Data = reader.ReadBytes(length - 4);
    }

    /// <inheritdoc />
    public void Write(BigEndianBinaryWriter writer)
    {
        writer.Write(Id);
        writer.Write(VersionNumber);
        writer.Write(LastInSequenceFlag);
        writer.WriteUInt24(Data.Length + 4);
        writer.Write(Width);
        writer.Write(Height);
        writer.Write(Data);
    }

    /// <inheritdoc />
    public ushort GetSegmentLength()
    {
        return (ushort)(11 + Data.Length);
    }
}