using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System.Numerics;

namespace Lucene.Net.Documents
{
	/// <summary>
	/// Emits prefix-coded trie terms for one fixed-width sortable byte value.
	/// </summary>
	internal sealed class SortableBytesTokenStream : TokenStream
	{
		/// <summary>
		/// Attribute carrying the current prefix-coded term bytes.
		/// </summary>
		public interface ISortableBytesTermAttribute : ITermToBytesRefAttribute
		{
			int Shift { get; set; }

			void Init(BigInteger value, int bitLength, int precisionStep, int shift);

			int IncShift();
		}

		private sealed class SortableBytesAttributeFactory : AttributeFactory
		{
			private readonly AttributeFactory _delegate;

			public SortableBytesAttributeFactory(AttributeFactory @delegate)
			{
				_delegate = @delegate;
			}

			public override Lucene.Net.Util.Attribute CreateAttributeInstance<T>()
			{
				Type type = typeof(T);
				if (typeof(ICharTermAttribute).IsAssignableFrom(type))
					throw new ArgumentException("SortableBytesTokenStream does not support ICharTermAttribute.");
				if (typeof(ISortableBytesTermAttribute).IsAssignableFrom(type))
					return new SortableBytesTermAttribute();
				return _delegate.CreateAttributeInstance<T>();
			}
		}

		/// <summary>
		/// Implementation of <see cref="ISortableBytesTermAttribute"/>.
		/// </summary>
		public sealed class SortableBytesTermAttribute : Lucene.Net.Util.Attribute, ISortableBytesTermAttribute, IAttribute, ITermToBytesRefAttribute
		{
			private BigInteger _value;
			private int _bitLength;
			private int _precisionStep;
			private readonly BytesRef _bytes = new BytesRef(64);

			public BytesRef BytesRef => _bytes;

			public int Shift { get; set; }

			public void FillBytesRef()
			{
				SortableBytesNumericUtils.ToPrefixCoded(_value, Shift, _bitLength, _bytes);
			}

			public void Init(BigInteger value, int bitLength, int precisionStep, int shift)
			{
				_value = value;
				_bitLength = bitLength;
				_precisionStep = precisionStep;
				Shift = shift;
			}

			public int IncShift()
				=> Shift += _precisionStep;

			public override void Clear()
			{
			}

			public override void CopyTo(IAttribute target)
			{
				SortableBytesTermAttribute other = (SortableBytesTermAttribute)target;
				other.Init(_value, _bitLength, _precisionStep, Shift);
				other._bytes.CopyBytes(_bytes);
			}
		}

		private readonly ISortableBytesTermAttribute _termAtt;
		private readonly int _precisionStep;
		private readonly int _bitLength;
		private bool _initialized;

		public SortableBytesTokenStream(int bitLength, int precisionStep = SortableBytesNumericUtils.PrecisionStepDefault)
			: base(new SortableBytesAttributeFactory(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY))
		{
			if (bitLength < 1 || bitLength > SortableBytesNumericUtils.MaxSupportedBitLength)
				throw new ArgumentOutOfRangeException(nameof(bitLength));
			if (precisionStep < 1)
				throw new ArgumentOutOfRangeException(nameof(precisionStep));

			_bitLength = bitLength;
			_precisionStep = precisionStep;
			_termAtt = AddAttribute<ISortableBytesTermAttribute>();
		}

		public SortableBytesTokenStream SetValue(ReadOnlySpan<byte> bytes)
		{
			if (bytes.Length * 8 != _bitLength)
				throw new ArgumentException($"Expected {_bitLength / 8} bytes.", nameof(bytes));

			BigInteger value = SortableBytesNumericUtils.ToUnsignedBigInteger(bytes);
			// Mirror NumericTokenStream: first IncrementToken() advances to shift 0.
			_termAtt.Init(value, _bitLength, _precisionStep, shift: -_precisionStep);
			_initialized = true;
			return this;
		}

		public override bool IncrementToken()
		{
			if (!_initialized)
				throw new InvalidOperationException("Call SetValue before consuming this stream.");

			ClearAttributes();
			int shift = _termAtt.IncShift();
			return shift < _bitLength;
		}

		public override void Reset()
		{
			base.Reset();
			if (_initialized)
				_termAtt.Shift = -_precisionStep;
		}
	}
}
