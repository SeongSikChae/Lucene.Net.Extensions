# Lucene.Net namespace Extensions

.NET 데이터 타입을 Lucene.Net에서 인덱싱·검색·정렬할 수 있게 확장합니다.  
대상 프레임워크: **.NET 10**, Lucene.Net **4.8.0-beta00018**, 패키지 버전 **4.8.0-beta00018-2**.

## Lucene.Net.Documents namespace Extensions

* ByteField
* SByteField
* Int16Field
* UInt16Field
* UInt32Field
* UInt64Field
* HalfField
* IPAddressField
* DecimalField

### Lucene.Net.Documents.Document Extensions

* IPAddress? GetIPAddressValue(string name)
* decimal? GetDecimalValue(string name)

### IPAddressField

IPv4/IPv6를 `Int64` 페어(`name`, `name + "_L"`)로 저장합니다. IPv4-mapped IPv6는 저장 전 IPv4로 정규화됩니다.  
범위/정확/존재 쿼리와 정렬을 지원합니다.

```csharp
// Index
Document doc = [
    ..IPAddressField.CreateFields("ip", IPAddress.Parse("192.168.0.1"), Field.Store.YES),
    ..IPAddressField.CreateFields("ip6", IPAddress.Parse("2001:db8::1"), Field.Store.YES),
];
writer.AddDocument(doc);

// Read
IPAddress? ip = storedDoc.GetIPAddressValue("ip");

// Sort
Sort sort = new Sort([..IPAddressField.CreateSortField("ip")]);

// Query
Query exact = IPAddressField.NewExactQuery("ip", IPAddress.Parse("192.168.0.10"));
Query range = IPAddressField.NewRangeQuery(
    "ip",
    IPAddress.Parse("192.168.0.1"),
    IPAddress.Parse("192.168.0.100"));
Query openMax = IPAddressField.NewRangeQuery("ip", IPAddress.Parse("10.0.0.1"), null);
Query exists = IPAddressField.NewExistsQuery("ip");
```

### DecimalField

`decimal.GetBits` 결과를 4개의 `Int32Field`(`name + "_DL"`, `name + "_DM"`, `name + "_DH"`, `name`)로 저장해 정확한 값을 복원합니다.  
동시에 순서 보존용 sortable limbs(`name + "_S0"`, `"_S1"`, `"_S2"`)를 인덱싱해 수치 범위 쿼리·정렬이 가능합니다.

```csharp
// Index
Document doc = [..DecimalField.CreateFields("amount", 123.45m, Field.Store.YES)];
writer.AddDocument(doc);

// Read
decimal? amount = storedDoc.GetDecimalValue("amount");

// Sort (sortable limbs 기준 수치 순)
Sort sort = new Sort([..DecimalField.CreateSortField("amount")]);

// Query
Query exact = DecimalField.NewExactQuery("amount", 20.0m);
Query range = DecimalField.NewRangeQuery("amount", 10.5m, 30.25m);
Query openMin = DecimalField.NewRangeQuery("amount", null, 10.5m);
Query exists = DecimalField.NewExistsQuery("amount");
```

## Lucene.Net.Index namespace Extensions

### IIndexableField interface Extensions

* sbyte? GetSByteValue()
* ushort? GetUInt16Value()
* uint? GetUInt32Value()
* ulong? GetUInt64Value()
* Half? GetHalfValue()
* IPAddress? GetIPAddressValue(Document document)
* decimal? GetDecimalValue(Document document)

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
* void ToInt64Pair()
* IPAddress ToIPAddress(long high, long low)
