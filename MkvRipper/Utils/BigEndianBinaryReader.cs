namespace MkvRipper.Utils;

/// <summary>
/// A variant of the <see cref="BinaryReader"/> but in big-endian.
/// </summary>
public class BigEndianBinaryReader : IDisposable
{
    public BigEndianBinaryReader(Stream stream)
    {
        BaseStream = stream;
        _reader = new BinaryReader(BaseStream);
    }

    /// <summary>
    /// Gets the base stream.
    /// </summary>
    public Stream BaseStream { get; }

    /// <summary>
    /// The little-endian reader.
    /// </summary>
    private readonly BinaryReader _reader;

    /// <summary>
    /// The current position.
    /// </summary>
    private long _position;

    /// <summary>
    /// Gets and sets the byte position in the given stream.
    /// </summary>
    public long Position
    {
        get => _position;
        set
        {
            if (_position == value) return;
            _position = value;
            BaseStream.Position = value;
        }
    }

    /// <summary>
    /// Gets the length of the buffer to read.
    /// </summary>
    public long Length => BaseStream.Length;
    
    /// <inheritdoc />
    public void Dispose()
    {
        BaseStream.Dispose();
    }
    
    /// <summary>
    /// Reads an 8-bit byte.
    /// </summary>
    /// <returns></returns>
    public byte ReadByte()
    {
        _position++;
        return _reader.ReadByte();
    }
    
    /// <summary>
    /// Reads an 16-bit integer.
    /// </summary>
    /// <returns></returns>
    public short ReadInt16()
    {
        var b1 = ReadByte();
        var b2 = ReadByte();
        return (short)((b1 << 8) + b2);
    }
    
    /// <summary>
    /// Reads an 16-bit unsigned integer.
    /// </summary>
    /// <returns></returns>
    public ushort ReadUInt16()
    {
        var b1 = ReadByte();
        var b2 = ReadByte();
        return (ushort)((b1 << 8) + b2);
    }
    
    /// <summary>
    /// Reads an 24-bit unsigned integer.
    /// </summary>
    /// <returns></returns>
    public int ReadUInt24()
    {
        var b1 = ReadByte();
        var b2 = ReadByte();
        var b3 = ReadByte();
        return (b1 << 16) + (b2 << 8) + b3;
    }
    
    /// <summary>
    /// Reads an 32-bit integer.
    /// </summary>
    /// <returns></returns>
    public int ReadInt32()
    {
        var b1 = ReadByte();
        var b2 = ReadByte();
        var b3 = ReadByte();
        var b4 = ReadByte();
        return (b1 << 24) + (b2 << 16) + (b3 << 8) + b4;
    }
    
    /// <summary>
    /// Reads an 32-bit unsigned integer.
    /// </summary>
    /// <returns></returns>
    public uint ReadUInt32()
    {
        var b1 = ReadByte();
        var b2 = ReadByte();
        var b3 = ReadByte();
        var b4 = ReadByte();
        return (uint)((b1 << 24) + (b2 << 16) + (b3 << 8) + b4);
    }

    /// <summary>
    /// Reads the number of bytes.
    /// </summary>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns></returns>
    public byte[] ReadBytes(int count)
    {
        _position += count;
        return _reader.ReadBytes(count);
    }
}