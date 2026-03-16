# OmerkckEF (Modern .NET 10 ORM)

OmerkckEF, ADO.NET üzerinde inşa edilmiş, hafif (lightweight), yüksek performanslı ve modern .NET 10 özelliklerini kullanan bir ORM kütüphanesidir. Geleneksel ORM'lerin karmaşıklığından ve performans yükünden kaçınmak isteyenler için "Zero-Allocation" ve "Strict-SOLID" prensipleriyle tasarlanmıştır.

## 🚀 Öne Çıkan Özellikler

- **.NET 10.0 LTS:** En güncel .NET runtime avantajları.
- **High Performance Mapping:** Expression Trees ve Caching kullanarak Reflection maliyetini sıfıra indirir.
- **SearchValues<char> & SIMD:** SQL özel karakterleri donanım hızlandırmalı taranır ve temizlenir.
- **Sequential GUID (v7):** Yüksek veritabanı index performansı için sıralı GUID desteği.
- **Enterprise Ready (Tier 3):** 
  - **Structured Logging:** `Serilog` entegrasyonu ile tüm DB süreçleri loglanabilir.
  - **Health Checks:** Standart `.NET HealthCheck` API desteği ile veritabanı sağlığı izlenebilir.
- **Dependency Injection (DI):** Modern ASP.NET Core projeleriyle tam uyumlu.
- **SQL Injection Protection:** Tamamen parametreli sorgu yapısı ve yüksek performanslı sanitizasyon.

## 📦 Kurulum

`.csproj` dosyanıza `AddOmerkckEF` extension metodunu ekleyerek başlayın:

```csharp
builder.Services.AddOmerkckEF(config => {
    config.DbServerId = 1;
    config.DbSchema = "YourDatabase";
    config.DbUser = "root";
    config.DbPassword = "password";
});
```

## 🛠️ Kurumsal Özelliklerin Kullanımı

### Sağlık Kontrolü (Health Checks)
Projenize HealthCheck endpoint'lerini eklemeniz yeterlidir:
```csharp
app.MapHealthChecks("/health");
```

### Loglama
Kütüphane içinde oluşan tüm hatalar, DI üzerinden gelen `ILogger` aracılığıyla loglanır. Serilog ile loglarınızı istediğiniz bir sink'e (Elasticsearch, File, Console) aktarabilirsiniz.

### Sequential GUID
Veritabanı index performansı için `Guid.NewGuid()` yerine kütüphane içindeki sıralı GUID yapısını kullanabilirsiniz:
```csharp
var sequentialId = metadataProvider.CreateSequentialGuid();
```

## 🧪 Unit Test

Proje içinde `OmerkckEF.Bisco.Tests` klasörü altında DI, Mapping ve Health Check testleri bulunmaktadır. Testleri çalıştırmak için:
```bash
dotnet test
```

## 📄 Mimari Kararlar (ADR)
Proje gelişimi sırasında alınan tüm kritik mimari kararlar `docs/adr/` klasörü altında belgelenmiştir.

---
*OmerkckEF - Sade, Hızlı, Modern.*
