# Lucene.Net namespace Extensions

.NET 데이터 타입을 Lucene.Net에서 인덱싱·검색·정렬할 수 있게 확장하고, Windows에 맞춘 `Directory` 구현을 제공합니다.  
대상 프레임워크: **.NET 10**, Lucene.Net **4.8.0-beta00018**, 패키지 버전 **4.8.0-beta00018-5**.

> **Breaking change (4.8.0-beta00018-4):** `IPAddressField` / `DecimalField`는 limb AND 구조를 폐기하고, **논리값당 prefix-coded trie term** 으로 재설계했습니다. 기존 인덱스는 재색인이 필요합니다. 정렬은 `CreateSortValueFields`로 **단일값 `_$Sort` SortedDocValues** 를 쓰고, 범위 검색은 NumericUtils 스타일 **precision-step trie** 로 동작합니다.

## Lucene.Net.Store namespace Extensions (Windows)

Windows 전용 FSDirectory입니다. 쓰기는 공통(`WinFSDirectoryBase`)으로 SequentialScan + 대용량 OS 버퍼를 쓰고, 읽기 전략만 구현체별로 다릅니다.

| 클래스 | 읽기 방식 | 언제 쓰면 좋은지 |
| --- | --- | --- |
| `WinMMapDirectory` | Memory-mapped + unsafe pointer copy | 큰 세그먼트·랜덤 읽기가 많은 검색 위주 |
| `WinRandomAccessDirectory` | `FILE_FLAG_RANDOM_ACCESS` FileStream | mmap 부담을 피하고 싶을 때, 작은 파일 |
| `WinHybridDirectory` | 임계값 기준으로 mmap / RA 자동 선택 | 기본 권장 (기본 임계값 1 MiB) |

* 플랫폼: **Windows only** (`[SupportedOSPlatform("windows")]`).
* Clone/slice와 dispose 경합을 막기 위해 공유 리소스를 ref-count합니다.
* 빈 파일(0 bytes)은 Windows에서 mmap할 수 없어 `WinHybridDirectory`가 RA 경로로 처리합니다.
* `WinHybridDirectory`는 merge 컨텍스트에서 임계값의 1/4 이상이면 mmap을 선호합니다.

```csharp
using Lucene.Net.Store;

// 권장: 파일 크기에 따라 mmap / random-access 자동 선택
using Directory dir = new WinHybridDirectory(@"C:\indexes\demo");

// 또는 읽기 전략을 고정
using Directory mmap = new WinMMapDirectory(@"C:\indexes\demo");
using Directory ra = new WinRandomAccessDirectory(@"C:\indexes\demo");

// 임계값 커스터마이즈 (예: 512 KiB 이상이면 mmap)
using Directory hybrid = new WinHybridDirectory(
    new DirectoryInfo(@"C:\indexes\demo"),
    lockFactory: null,
    mmapThresholdBytes: 512 * 1024);
```

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
* IEnumerable<IPAddress> GetIPAddressValues(string name)
* decimal? GetDecimalValue(string name)
* IEnumerable<decimal> GetDecimalValues(string name)

### IPAddressField

검색·정렬용은 **17바이트 고정폭** (`family` 1바이트 + 주소 16바이트, IPv4는 우측 정렬 패딩)입니다.  
`Store.YES`일 때 같은 이름에 **가변폭 stored**를 둡니다 (IPv4=5바이트, IPv6=17바이트).

* IPv4-mapped IPv6(`::ffff:a.b.c.d`)는 저장 전 IPv4로 정규화됩니다.
* `family`는 인코딩 선두 바이트입니다 (`4`=IPv4, `6`=IPv6). 정렬 시 모든 IPv4가 모든 IPv6보다 앞입니다.
* IPv6 **scope ID**(`fe80::1%12` 등)는 인코딩에 포함되지 않으며, 복원 시 `fe80::1`처럼 scope 없이 돌아옵니다.
* exact/range/exists는 문서당 다중값에서도 교차곱 없이 안전합니다.
* exists는 `name + "_$Exists"` marker term(`"1"`)을 사용합니다.
* 정렬은 `CreateSortValueFields` → `name + "_$Sort"` (문서당 **단일** SortedDocValues).
* **예약 suffix:** `_$Exists`, `_$Sort` — 내부 전용입니다. 사용자 필드명과 충돌하지 않도록 피하세요.

```csharp
Document doc = [
    // search + store (multi-value OK)
    ..IPAddressField.CreateFields("ip", IPAddress.Parse("192.168.0.1"), Field.Store.YES),
    ..IPAddressField.CreateFields("ip", IPAddress.Parse("2001:db8::1"), Field.Store.YES),
    // sort representative (single value)
    ..IPAddressField.CreateSortValueFields("ip", IPAddress.Parse("192.168.0.1")),
];
writer.AddDocument(doc);

IPAddress? first = storedDoc.GetIPAddressValue("ip");
IEnumerable<IPAddress> all = storedDoc.GetIPAddressValues("ip");

Sort sort = new Sort([..IPAddressField.CreateSortField("ip")]);

Query exact = IPAddressField.NewExactQuery("ip", IPAddress.Parse("192.168.0.10"));
Query range = IPAddressField.NewRangeQuery(
    "ip",
    IPAddress.Parse("192.168.0.1"),
    IPAddress.Parse("192.168.0.100"));
Query openMax = IPAddressField.NewRangeQuery("ip", IPAddress.Parse("10.0.0.1"), null);
Query exists = IPAddressField.NewExistsQuery("ip");
```

### DecimalField

수치 순서 보존용 **24바이트 sortable** 값을 trie로 인덱싱하고,  
`Store.YES`일 때 `decimal.GetBits` **16바이트**를 같은 필드명에 저장합니다.

* exact/range는 sortable 기준이라 `20.0m`과 `20.00m`은 같은 수치로 매칭됩니다.
* 저장된 복원 값은 GetBits scale을 유지합니다.
* 정렬은 `CreateSortValueFields`로 단일 대표값을 `_$Sort`에 넣습니다.
* **예약 suffix:** `_$Exists`, `_$Sort` — 내부 전용입니다. 사용자 필드명과 충돌하지 않도록 피하세요.

```csharp
Document doc = [
    ..DecimalField.CreateFields("amount", 123.45m, Field.Store.YES),
    ..DecimalField.CreateFields("amount", 10.5m, Field.Store.YES),
    ..DecimalField.CreateSortValueFields("amount", 10.5m), // sort by min example
];
writer.AddDocument(doc);

decimal? first = storedDoc.GetDecimalValue("amount");
IEnumerable<decimal> all = storedDoc.GetDecimalValues("amount");

Sort sort = new Sort([..DecimalField.CreateSortField("amount")]);

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
* IPAddress? GetIPAddressValue()
* IPAddress? GetIPAddressValue(Document document)
* decimal? GetDecimalValue()
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
* byte[] ToSortableBytes()
* void WriteSortableBytes(Span<byte> destination)
* byte[] ToStoredBytes()
* int WriteStoredBytes(Span<byte> destination)
* bool IsEncodedAddress(ReadOnlySpan<byte> encoded)
* bool TryToIPAddress(ReadOnlySpan<byte> encoded, out IPAddress? address)
* IPAddress ToIPAddress(ReadOnlySpan<byte> encoded)
