using System.Net.Sockets;

namespace System.Net
{
	/// <summary>
	/// Helpers for encoding <see cref="IPAddress"/> values as fixed-width sortable bytes for Lucene.
	/// </summary>
	public static class IPAddressExtensions
	{
		/// <summary>Family byte written for IPv4 addresses after normalization.</summary>
		public const byte FamilyIPv4 = 4;

		/// <summary>Family byte written for IPv6 addresses.</summary>
		public const byte FamilyIPv6 = 6;

		/// <summary>Indexed/sort encoding length: 1 family byte + 16 address bytes.</summary>
		public const int EncodedLength = 17;

		/// <summary>Compact stored length for IPv4: 1 family byte + 4 address bytes.</summary>
		public const int StoredLengthIPv4 = 5;

		/// <summary>Compact stored length for IPv6 (same layout as indexed encoding).</summary>
		public const int StoredLengthIPv6 = EncodedLength;

		/// <summary>
		/// Maps IPv4-mapped IPv6 addresses to IPv4 so the same address has a single storage form.
		/// </summary>
		public static IPAddress NormalizeForStorage(this IPAddress address)
		{
			ArgumentNullException.ThrowIfNull(address);
			if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6)
				return address.MapToIPv4();
			return address;
		}

		/// <summary>
		/// Encodes an IPv4/IPv6 address as a 17-byte sortable payload:
		/// family (4 or 6) followed by a 16-byte address (IPv4 is right-aligned with 12 leading zeros).
		/// </summary>
		public static byte[] ToSortableBytes(this IPAddress address)
		{
			Span<byte> buffer = stackalloc byte[EncodedLength];
			address.WriteSortableBytes(buffer);
			return buffer.ToArray();
		}

		/// <summary>
		/// Writes the 17-byte sortable encoding of <paramref name="address"/> into <paramref name="destination"/>.
		/// </summary>
		public static void WriteSortableBytes(this IPAddress address, Span<byte> destination)
		{
			ArgumentNullException.ThrowIfNull(address);
			if (destination.Length < EncodedLength)
				throw new ArgumentException($"Destination must be at least {EncodedLength} bytes.", nameof(destination));

			IPAddress normalized = address.NormalizeForStorage();
			ReadOnlySpan<byte> bytes = normalized.GetAddressBytes();
			destination[..EncodedLength].Clear();

			if (normalized.AddressFamily == AddressFamily.InterNetwork)
			{
				if (bytes.Length != 4)
					throw new ArgumentException("Invalid IPv4 address length.", nameof(address));

				destination[0] = FamilyIPv4;
				bytes.CopyTo(destination.Slice(1 + 12, 4));
				return;
			}

			if (normalized.AddressFamily == AddressFamily.InterNetworkV6)
			{
				if (bytes.Length != 16)
					throw new ArgumentException("Invalid IPv6 address length.", nameof(address));

				destination[0] = FamilyIPv6;
				bytes.CopyTo(destination.Slice(1, 16));
				return;
			}

			throw new ArgumentException("IPv4 or IPv6 address required.", nameof(address));
		}

		/// <summary>
		/// Encodes an IPv4/IPv6 address as a compact stored payload:
		/// IPv4 = 5 bytes (<see cref="FamilyIPv4"/> + 4), IPv6 = 17 bytes (<see cref="FamilyIPv6"/> + 16).
		/// </summary>
		public static byte[] ToStoredBytes(this IPAddress address)
		{
			IPAddress normalized = address.NormalizeForStorage();
			int length = normalized.AddressFamily == AddressFamily.InterNetwork
				? StoredLengthIPv4
				: StoredLengthIPv6;
			byte[] buffer = new byte[length];
			address.WriteStoredBytes(buffer);
			return buffer;
		}

		/// <summary>
		/// Writes the compact stored encoding of <paramref name="address"/> into <paramref name="destination"/>.
		/// Destination must be at least <see cref="StoredLengthIPv4"/> (IPv4) or <see cref="StoredLengthIPv6"/> (IPv6).
		/// </summary>
		public static int WriteStoredBytes(this IPAddress address, Span<byte> destination)
		{
			ArgumentNullException.ThrowIfNull(address);

			IPAddress normalized = address.NormalizeForStorage();
			ReadOnlySpan<byte> bytes = normalized.GetAddressBytes();

			if (normalized.AddressFamily == AddressFamily.InterNetwork)
			{
				if (bytes.Length != 4)
					throw new ArgumentException("Invalid IPv4 address length.", nameof(address));
				if (destination.Length < StoredLengthIPv4)
					throw new ArgumentException($"Destination must be at least {StoredLengthIPv4} bytes.", nameof(destination));

				destination[0] = FamilyIPv4;
				bytes.CopyTo(destination.Slice(1, 4));
				return StoredLengthIPv4;
			}

			if (normalized.AddressFamily == AddressFamily.InterNetworkV6)
			{
				if (bytes.Length != 16)
					throw new ArgumentException("Invalid IPv6 address length.", nameof(address));
				if (destination.Length < StoredLengthIPv6)
					throw new ArgumentException($"Destination must be at least {StoredLengthIPv6} bytes.", nameof(destination));

				destination[0] = FamilyIPv6;
				bytes.CopyTo(destination.Slice(1, 16));
				return StoredLengthIPv6;
			}

			throw new ArgumentException("IPv4 or IPv6 address required.", nameof(address));
		}

		/// <summary>
		/// Returns whether <paramref name="encoded"/> looks like a compact stored or fixed sortable IP payload.
		/// </summary>
		public static bool IsEncodedAddress(ReadOnlySpan<byte> encoded)
			=> TryToIPAddress(encoded, out _);

		/// <summary>
		/// Reconstructs an <see cref="IPAddress"/> from a compact stored payload (5 or 17 bytes)
		/// or the 17-byte sortable encoding produced by <see cref="ToSortableBytes"/>.
		/// </summary>
		public static IPAddress ToIPAddress(ReadOnlySpan<byte> encoded)
		{
			if (!TryToIPAddress(encoded, out IPAddress? address))
				throw new ArgumentException("Encoded address must be a 5-byte IPv4 or 17-byte IPv4/IPv6 payload.", nameof(encoded));
			return address!;
		}

		/// <summary>
		/// Tries to reconstruct an <see cref="IPAddress"/> from a compact stored or sortable payload.
		/// </summary>
		public static bool TryToIPAddress(ReadOnlySpan<byte> encoded, out IPAddress? address)
		{
			address = null;
			if (encoded.IsEmpty)
				return false;

			byte family = encoded[0];
			if (encoded.Length == StoredLengthIPv4)
			{
				if (family != FamilyIPv4)
					return false;
				address = new IPAddress(encoded.Slice(1, 4));
				return true;
			}

			if (encoded.Length == EncodedLength)
			{
				ReadOnlySpan<byte> addressBytes = encoded.Slice(1, 16);
				if (family == FamilyIPv4)
				{
					address = new IPAddress(addressBytes.Slice(12, 4));
					return true;
				}

				if (family == FamilyIPv6)
				{
					address = new IPAddress(addressBytes);
					return true;
				}
			}

			return false;
		}
	}
}
