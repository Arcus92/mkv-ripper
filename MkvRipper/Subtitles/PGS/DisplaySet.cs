using MkvRipper.Subtitles.PGS.Segments;
using MkvRipper.Utils;

namespace MkvRipper.Subtitles.PGS;

/// <summary>
/// The display set contains all information to display a graphic subtitle from a PGS (Presentation Graphic Stream).
/// </summary>
public class DisplaySet
{
    /// <summary>
    /// The file magic number.
    /// </summary>
    private const ushort MagicNumber = 0x5047;
    
    /// <summary>
    /// Gets the presentation timestamp in 90 kHz.
    /// </summary>
    public uint PresentationTimestamp { get; set; }
    
    /// <summary>
    /// Gets the decoding timestamp in 90 kHz.
    /// </summary>
    public uint DecodingTimestamp { get; set; }
    
    /// <summary>
    /// Gets the palette definition.
    /// </summary>
    public PaletteDefinitionSegment PaletteDefinition { get; set; } = new();
    
    /// <summary>
    /// Gets the object definition.
    /// </summary>
    public ObjectDefinitionSegment ObjectDefinition { get; set; } = new();
    
    /// <summary>
    /// Gets the presentation composition.
    /// </summary>
    public PresentationCompositionSegment PresentationComposition { get; set; } = new();
    
    /// <summary>
    /// Gets the window definition.
    /// </summary>
    public WindowDefinitionSegment WindowDefinition { get; set; } = new();
    
    /// <summary>
    /// Reads the PGS format.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <param name="includeHeader">Should the header be included? The PG header is written before every segment.</param>
    public void Read(BigEndianBinaryReader reader, bool includeHeader = true)
    {
        PaletteDefinition = new PaletteDefinitionSegment();
        ObjectDefinition = new ObjectDefinitionSegment();
        PresentationComposition = new PresentationCompositionSegment();
        WindowDefinition = new WindowDefinitionSegment();
        
        while (true)
        {
            // The header is included before every segment. Even for the end segment.
            if (includeHeader)
            {
                var magicNumber = reader.ReadUInt16();
                if (magicNumber != MagicNumber) 
                    throw new InvalidDataException("Magic number mismatch. Expected: 0x5047.");
                PresentationTimestamp = reader.ReadUInt32();
                DecodingTimestamp = reader.ReadUInt32();
            }
            
            // Read the segment...
            var type = reader.ReadByte();
            var size = reader.ReadUInt16();
            switch (type)
            {
                case PaletteDefinitionSegment.Type:
                    var pds = new PaletteDefinitionSegment();
                    pds.Read(reader, size);
                    PaletteDefinition = pds;
                    break;
                case ObjectDefinitionSegment.Type:
                    var ods = new ObjectDefinitionSegment();
                    ods.Read(reader, size);
                    ObjectDefinition = ods;
                    break;
                case PresentationCompositionSegment.Type:
                    var pcs = new PresentationCompositionSegment();
                    pcs.Read(reader, size);
                    PresentationComposition = pcs;
                    break;
                case WindowDefinitionSegment.Type:
                    var wds = new WindowDefinitionSegment();
                    wds.Read(reader, size);
                    WindowDefinition = wds;
                    break;
                case EndSegment.Type:
                    return;
                default:
                    throw new InvalidDataException($"Unknown segment type: {type}.");
            }
        }
    }

    /// <summary>
    /// Writes the PGS format.
    /// </summary>
    /// <param name="writer">The binary writer.</param>
    /// <param name="includeHeader">Should the header be included? The PG header is written before every segment.</param>
    public void Write(BigEndianBinaryWriter writer, bool includeHeader = true)
    {
        WriteSegment(PresentationComposition, writer, includeHeader);
        WriteSegment(WindowDefinition, writer, includeHeader);
        if (!PaletteDefinition.IsEmpty) WriteSegment(PaletteDefinition, writer, includeHeader);
        if (!ObjectDefinition.IsEmpty) WriteSegment(ObjectDefinition, writer, includeHeader);
        WriteSegment(EndSegment.Shared, writer, includeHeader);
    }
    
    /// <summary>
    /// Writes a segment to the stream.
    /// </summary>
    /// <param name="segment">The segment to write.</param>
    /// <param name="writer">The writer.</param>
    /// <param name="includeHeader">Should the header be included?</param>
    /// <typeparam name="T">The segment type.</typeparam>
    private void WriteSegment<T>(T segment, BigEndianBinaryWriter writer, bool includeHeader = true) where T : IPresentationGraphicSegment
    {
        if (includeHeader)
        {
            writer.Write(MagicNumber);
            writer.Write(PresentationTimestamp);
            writer.Write(DecodingTimestamp);
        }
            
        var length = segment.GetSegmentLength();
        writer.Write(T.Type);
        writer.Write(length);
        segment.Write(writer);
    }
}