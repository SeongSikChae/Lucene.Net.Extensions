# Lucene.Net namespace Extensions

## Lucene.Net.Documents namespace Extensions

* ByteField
* SByteField
* Int16Field
* UInt16Field
* UInt32Field
* UInt64Field
* HalfField
* IPAddressField

### Lucene.Net.Documents.Document Extensions

* IPAddress? GetIPAddressValue(string name)

## Lucene.Net.Index namespace Extensions

### IIndexableField interface Extensions

* sbyte? GetSByteValue()
* ushort? GetUInt16Value()
* uint? GetUInt32Value()
* ulong? GetUInt64Value()
* Half? GetHalfValue()
* IPAddress? GetIPAddressValue(Document document)

## System namespace Extensions

### System.Byte Extensions

* sbyte ToSByte()

### System.SByte Extensions

* byte ToByte()

### System.Int16 Extensions

* ushort ToUInt16()

### System.UInt16 Extensions

* short ToInt16()

### System.Int32 Extensions

* uint ToUInt32()
* IPAddress ToIPAddress()

### System.UInt32 Extensions

* int ToInt32()
* IPAddress ToIPAddress()

### System.Int64 Extensions

* ulong ToUInt64()

### System.UInt64 Extensions

* long ToInt64()

## System.Net namespace Extensions

### System.Net.IPAddress Extensions

* IPAddress NormalizeForStorage()
* int ToInt32()
* void ToInt64Pair()
* IPAddress ToIPv6Address(long high, long low)