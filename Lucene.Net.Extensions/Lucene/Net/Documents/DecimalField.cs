using Lucene.Net.Search;
using System.Buffers.Binary;
using System.Numerics;

namespace Lucene.Net.Documents
{
    /// <summary>
    /// Indexes a <see cref="decimal"/> as exact <c>decimal.GetBits</c> Int32 parts plus order-preserving
    /// sortable limbs (see <see cref="DecimalSortableField"/>) for range queries and sorting.
    /// </summary>
    public static class DecimalField
    {
        /// <summary>Suffix for the low 32 bits of the mantissa (<c>decimal.GetBits</c> index 0).</summary>
        public const string LowPartSuffix = "_DL";
        /// <summary>Suffix for the mid 32 bits of the mantissa (<c>decimal.GetBits</c> index 1).</summary>
        public const string MidPartSuffix = "_DM";
        /// <summary>Suffix for the high 32 bits of the mantissa (<c>decimal.GetBits</c> index 2).</summary>
        public const string HighPartSuffix = "_DH";

        /// <summary>
        /// Creates stored/indexed GetBits parts plus non-stored sortable limbs for <paramref name="value"/>.
        /// </summary>
        /// <param name="name">Base field name (flags / scale part).</param>
        /// <param name="value">Value to index.</param>
        /// <param name="stored">Whether to store the GetBits parts.</param>
        public static IEnumerable<Field> CreateFields(string name, decimal value, Field.Store stored)
        {
            int[] parts = decimal.GetBits(value);
            yield return new Int32Field(name + LowPartSuffix, parts[0], stored);
            yield return new Int32Field(name + MidPartSuffix, parts[1], stored);
            yield return new Int32Field(name + HighPartSuffix, parts[2], stored);
            yield return new Int32Field(name, parts[3], stored);
            foreach (Field f in DecimalSortableField.CreateFields(name, value))
                yield return f;
        }

        /// <summary>
        /// Creates sort fields for GetBits parts and sortable limbs matching <see cref="CreateFields"/> order.
        /// Prefer the sortable limbs for numeric order; GetBits parts alone are not lexicographically ordered.
        /// </summary>
        /// <param name="name">Base field name.</param>
        /// <param name="reverse">Whether to reverse the sort order.</param>
        public static IEnumerable<SortField> CreateSortField(string name, bool reverse = false)
        {
            yield return new SortField(name + LowPartSuffix, SortFieldType.INT32, reverse);
            yield return new SortField(name + MidPartSuffix, SortFieldType.INT32, reverse);
            yield return new SortField(name + HighPartSuffix, SortFieldType.INT32, reverse);
            yield return new SortField(name, SortFieldType.INT32, reverse);
            foreach (SortField f in DecimalSortableField.CreateSortField(name, reverse))
                yield return f;
        }

        /// <summary>
        /// Inclusive/exclusive numeric range over the sortable limbs. Null bounds are open.
        /// </summary>
        public static Query NewRangeQuery(string name, decimal? min, decimal? max, bool minInclusive = true, bool maxInclusive = true)
        {
            return DecimalSortableField.NewRangeQuery(name, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Exact match over the sortable limbs (same as an inclusive range with equal bounds).
        /// </summary>
        public static Query NewExactQuery(string name, decimal value)
        {
            return DecimalSortableField.NewExactQuery(name, value);
        }

        /// <summary>
        /// Matches any document that has sortable decimal limbs for <paramref name="name"/>.
        /// </summary>
        public static Query NewExistsQuery(string name)
        {
            return DecimalSortableField.NewExistsQuery(name);
        }

        /// <summary>
        /// Financial-grade order-preserving encoding for <see cref="decimal"/>, stored as three Int64 limbs
        /// so Lucene can run exact numeric range queries (unlike <see cref="DecimalField"/> GetBits layout).
        /// </summary>
        /// <remarks>
        /// Encoding:
        /// <list type="number">
        /// <item><description>Interpret the decimal as ±mantissa × 10<sup>-scale</sup>.</description></item>
        /// <item><description>Normalize to a non-negative integer <c>mantissa × 10<sup>(28-scale)</sup></c> (fits in 192 bits).</description></item>
        /// <item><description>Map into a signed-sortable 192-bit space around offset 2<sup>191</sup>: negatives below, positives above.</description></item>
        /// <item><description>Split into 3 big-endian Int64 limbs and XOR each with <see cref="long.MinValue"/> so unsigned limb order matches Lucene signed Int64 order (same trick as <c>IPAddressField</c>).</description></item>
        /// </list>
        /// Field names: <c>{name}_S0</c> (most significant), <c>{name}_S1</c>, <c>{name}_S2</c> (least significant).
        /// Keep using <see cref="DecimalField"/> for stored exact bits; use these limbs for range / exists / sort.
        /// </remarks>
        public static class DecimalSortableField
        {
            /// <summary>Number of Int64 limbs in the sortable encoding.</summary>
            private const int LimbCount = 3;
            private const string Limb0Suffix = "_S0";
            private const string Limb1Suffix = "_S1";
            private const string Limb2Suffix = "_S2";
            private const ulong SignXor = 0x8000_0000_0000_0000UL;

            /// <summary>2^191 — midpoint of the unsigned 192-bit space.</summary>
            private static readonly BigInteger Offset = BigInteger.One << 191;

            private static void Encode(decimal value, Span<long> limbs)
            {
                if (limbs.Length < LimbCount)
                    throw new ArgumentException($"Need at least {LimbCount} limbs.", nameof(limbs));

                int[] bits = decimal.GetBits(value);
                bool negative = (bits[3] & unchecked((int)0x80000000)) != 0;
                int scale = (bits[3] >> 16) & 0xFF;

                BigInteger mantissa =
                    (uint)bits[0]
                    | ((BigInteger)(uint)bits[1] << 32)
                    | ((BigInteger)(uint)bits[2] << 64);

                BigInteger scaled = mantissa * BigInteger.Pow(10, 28 - scale);
                BigInteger sortable = negative ? Offset - scaled : Offset + scaled;

                Span<byte> bytes = stackalloc byte[24];
                WriteUnsigned192BigEndian(sortable, bytes);

                for (int i = 0; i < LimbCount; i++)
                {
                    ulong limb = BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(i * sizeof(ulong), sizeof(ulong)));
                    limbs[i] = unchecked((long)(limb ^ SignXor));
                }
            }

            private static void WriteUnsigned192BigEndian(BigInteger value, Span<byte> destination)
            {
                if (destination.Length != 24)
                    throw new ArgumentException("Destination must be 24 bytes.", nameof(destination));
                if (value.Sign < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Expected non-negative sortable value.");

                byte[] encoded = value.ToByteArray(isUnsigned: true, isBigEndian: true);
                destination.Clear();
                if (encoded.Length > destination.Length)
                    throw new InvalidOperationException("Sortable decimal encoding overflowed 192 bits.");

                encoded.CopyTo(destination[(destination.Length - encoded.Length)..]);
            }

            /// <summary>
            /// Creates non-stored Int64 fields used for range queries and sorting.
            /// </summary>
            public static IEnumerable<Int64Field> CreateFields(string name, decimal value)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(name);


                long[] limbs = new long[LimbCount];
                Encode(value, limbs);

                string[] fieldNames = [name + Limb0Suffix, name + Limb1Suffix, name + Limb2Suffix];
                for (int i = 0; i < LimbCount; i++)
                    yield return new Int64Field(fieldNames[i], limbs[i], Field.Store.NO);
            }

            /// <summary>
            /// Creates sort fields (high → low) matching <see cref="CreateFields"/> order.
            /// </summary>
            public static IEnumerable<SortField> CreateSortField(string name, bool reverse = false)
            {
                string[] fieldNames = [name + Limb0Suffix, name + Limb1Suffix, name + Limb2Suffix];
                foreach (string fieldName in fieldNames)
                    yield return new SortField(fieldName, SortFieldType.INT64, reverse);
            }

            /// <summary>
            /// Exact match over the sortable limbs (same as an inclusive range with equal bounds).
            /// </summary>
            public static Query NewExactQuery(string name, decimal value)
            {
                return NewRangeQuery(name, value, value);
            }

            /// <summary>
            /// Inclusive numeric range over the sortable limbs. Null bounds are open.
            /// </summary>
            public static Query NewRangeQuery(string name, decimal? min, decimal? max, bool minInclusive = true, bool maxInclusive = true)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(name);

                Span<long> startBuf = stackalloc long[LimbCount];
                Span<long> endBuf = stackalloc long[LimbCount];

                if (min is null)
                    startBuf.Fill(long.MinValue);
                else
                    Encode(min.Value, startBuf);

                if (max is null)
                    endBuf.Fill(long.MaxValue);
                else
                    Encode(max.Value, endBuf);

                if (!minInclusive && min is not null)
                {
                    if (!TryIncrement(startBuf))
                        return new BooleanQuery(); // min was MaxValue encoding edge
                }

                if (!maxInclusive && max is not null)
                {
                    if (!TryDecrement(endBuf))
                        return new BooleanQuery();
                }

                string[] fieldNames = [name + Limb0Suffix, name + Limb1Suffix, name + Limb2Suffix];
                return CreateLexicographicRangeQuery(fieldNames, startBuf.ToArray(), endBuf.ToArray());
            }

            private static bool TryIncrement(Span<long> limbs)
            {
                for (int i = limbs.Length - 1; i >= 0; i--)
                {
                    if (limbs[i] < long.MaxValue)
                    {
                        limbs[i]++;
                        return true;
                    }

                    limbs[i] = long.MinValue;
                }

                return false;
            }

            private static bool TryDecrement(Span<long> limbs)
            {
                for (int i = limbs.Length - 1; i >= 0; i--)
                {
                    if (limbs[i] > long.MinValue)
                    {
                        limbs[i]--;
                        return true;
                    }

                    limbs[i] = long.MaxValue;
                }

                return false;
            }

            /// <summary>
            /// Builds an inclusive lexicographic range over Int64 limbs (high → low), same pattern as IP high/low.
            /// </summary>
            private static Query CreateLexicographicRangeQuery(string[] fields, long[] start, long[] end)
            {
                ArgumentNullException.ThrowIfNull(fields);
                ArgumentNullException.ThrowIfNull(start);
                ArgumentNullException.ThrowIfNull(end);

                if (fields.Length == 0 || start.Length != fields.Length || end.Length != fields.Length)
                    throw new ArgumentException("fields/start/end length mismatch.");

                int cmp = CompareLimbs(start, end);
                if (cmp > 0)
                    return new BooleanQuery();
                if (cmp == 0)
                    return CreateExactQuery(fields, start);

                return CreateRangeRecursive(fields, start, end, depth: 0);
            }

            private static int CompareLimbs(ReadOnlySpan<long> left, ReadOnlySpan<long> right)
            {
                for (int i = 0; i < left.Length; i++)
                {
                    int cmp = left[i].CompareTo(right[i]);
                    if (cmp != 0)
                        return cmp;
                }

                return 0;
            }

            private static BooleanQuery CreateExactQuery(string[] fields, long[] value)
            {
                BooleanQuery query = [];
                for (int i = 0; i < fields.Length; i++)
                {
                    query.Add(
                        NumericRangeQuery.NewInt64Range(fields[i], value[i], value[i], true, true),
                        Occur.MUST);
                }

                return query;
            }

            private static Query CreateRangeRecursive(IReadOnlyList<string> fields, long[] start, long[] end, int depth)
            {
                int last = fields.Count - 1;
                if (depth == last)
                {
                    return NumericRangeQuery.NewInt64Range(fields[depth], start[depth], end[depth], true, true);
                }

                long s = start[depth];
                long e = end[depth];

                if (s == e)
                {
                    return new BooleanQuery
                    {
                        { NumericRangeQuery.NewInt64Range(fields[depth], s, s, true, true), Occur.MUST },
                        { CreateRangeRecursive(fields, start, end, depth + 1), Occur.MUST },
                    };
                }

                // s < e:
                //   (dim==s AND suffix >= startSuffix)
                //   OR (s < dim < e)
                //   OR (dim==e AND suffix <= endSuffix)
                BooleanQuery result = new() { MinimumNumberShouldMatch = 1 };

                long[] lowerEnd = CreateFilled(fields.Count, depth, start, equalAtDepth: s, fillAfter: long.MaxValue);
                result.Add(
                    new BooleanQuery
                    {
                        { NumericRangeQuery.NewInt64Range(fields[depth], s, s, true, true), Occur.MUST },
                        { CreateRangeRecursive(fields, start, lowerEnd, depth + 1), Occur.MUST },
                    }, 
                    Occur.SHOULD
                );

                long midMin = s + 1;
                long midMax = e - 1;
                if (midMin <= midMax)
                {
                    result.Add(
                        NumericRangeQuery.NewInt64Range(fields[depth], midMin, midMax, true, true),
                        Occur.SHOULD);
                }

                long[] upperStart = CreateFilled(fields.Count, depth, end, equalAtDepth: e, fillAfter: long.MinValue);
                result.Add(
                    new BooleanQuery
                    {
                        { NumericRangeQuery.NewInt64Range(fields[depth], e, e, true, true), Occur.MUST },
                        { CreateRangeRecursive(fields, upperStart, end, depth + 1), Occur.MUST },
                    }, 
                    Occur.SHOULD
                );

                return result;
            }

            private static long[] CreateFilled(int length, int depth, long[] template, long equalAtDepth, long fillAfter)
            {
                long[] values = new long[length];
                for (int i = 0; i < length; i++)
                {
                    if (i < depth)
                        values[i] = template[i];
                    else if (i == depth)
                        values[i] = equalAtDepth;
                    else
                        values[i] = fillAfter;
                }

                return values;
            }

            /// <summary>
            /// Matches any document that has sortable decimal limbs for <paramref name="name"/>.
            /// </summary>
            public static Query NewExistsQuery(string name)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(name);

                string[] fieldNames = [name + Limb0Suffix, name + Limb1Suffix, name + Limb2Suffix];
                return NumericRangeQuery.NewInt64Range(
                    fieldNames[0],
                    min: null,
                    max: null,
                    minInclusive: true,
                    maxInclusive: true);
            }
        }
    }
}
