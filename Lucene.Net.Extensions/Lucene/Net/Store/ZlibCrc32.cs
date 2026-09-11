namespace Lucene.Net.Store;

/// <summary>ZIP/zlib CRC-32 (same family Lucene uses for IndexOutput checksums).</summary>
internal sealed class ZlibCrc32
{
	private static readonly uint[] Table = CreateTable();
	private uint _crc = 0xFFFFFFFF;

	public void Update(byte b)
	{
		_crc = Table[(_crc ^ b) & 0xFF] ^ (_crc >> 8);
	}

	public void Update(byte[] buffer, int offset, int length)
	{
		uint crc = _crc;
		int end = offset + length;
		for (int i = offset; i < end; i++)
			crc = Table[(crc ^ buffer[i]) & 0xFF] ^ (crc >> 8);
		_crc = crc;
	}

	public void Update(ReadOnlySpan<byte> buffer)
	{
		uint crc = _crc;
		for (int i = 0; i < buffer.Length; i++)
			crc = Table[(crc ^ buffer[i]) & 0xFF] ^ (crc >> 8);
		_crc = crc;
	}

	public long Value => ~_crc & 0xFFFFFFFFL;

	private static uint[] CreateTable()
	{
		uint[] table = new uint[256];
		for (uint i = 0; i < 256; i++)
		{
			uint crc = i;
			for (int j = 0; j < 8; j++)
				crc = (crc & 1) != 0 ? (0xEDB88320u ^ (crc >> 1)) : (crc >> 1);
			table[i] = crc;
		}

		return table;
	}
}
