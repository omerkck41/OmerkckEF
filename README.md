# OmerkckEF (Modern .NET 10 High-Performance ORM)

OmerkckEF, ADO.NET üzerinde inşa edilmiş, saniyede binlerce sorgu atan yüksek trafikli sistemler için tasarlanmış, hafif (lightweight) ve modern bir ORM kütüphanesidir. Geleneksel ORM'lerin yavaşlığından ve karmaşıklığından kurtulup, **Native ADO.NET** performansını **SOLID** prensipleriyle birleştirir.

---

## 🚀 Öne Çıkan Özellikler

- **.NET 10.0 LTS:** En güncel .NET runtime ve C# 14 avantajları.
- **Zero-Allocation Mapping:** Expression Trees ile derleme zamanında (compile-time) üretilen mapping fonksiyonları sayesinde sıfır yansıma (reflection) maliyeti.
- **SIMD Hızlandırmalı Güvenlik:** `SearchValues<char>` ile donanım düzeyinde SQL Injection koruması.
- **Sequential GUID (v7):** Veritabanı index performansını maksimize eden sıralı GUID desteği.
- **Tier 3 Enterprise Ready:** Serilog ile yapılandırılmış loglama ve dahili HealthCheck desteği.

---

## 📦 Kurulum ve Yapılandırma

### 1. Servis Kaydı (Dependency Injection)

Modern ASP.NET Core veya Worker Service projelerinizde `Program.cs` içerisine aşağıdaki satırları ekleyerek kütüphaneyi kullanıma hazır hale getirebilirsiniz:

```csharp
using OmerkckEF.Biscom;

var builder = WebApplication.CreateBuilder(args);

// OmerkckEF Servis Kaydı
builder.Services.AddOmerkckEF(config => {
    config.DbServerId = 1;
    config.DbSchema = "MusteriDb";
    config.DbUser = "admin";
    config.DbPassword = "YourSecurePassword";
    config.DBModel = DataBaseType.PostgreSQL; // SQL, MySql, Oracle, PostgreSQL, SQLite desteği
});
```

---

## 🛠️ Temel Kullanım (CRUD İşlemleri)

### 1. Model (Entity) Tanımlama
Veritabanı tablolarınızı temsil eden sınıflarda `Key` ve `DataName` niteliklerini (attribute) kullanın:

```csharp
using System.ComponentModel.DataAnnotations;
using OmerkckEF.Biscom.ToolKit;

public class Product
{
    [Key] // Birincil anahtar (Primary Key)
    public int Id { get; set; }

    [DataName] // Veritabanı sütunu olduğunu belirtir
    public string Name { get; set; } = string.Empty;

    [DataName]
    public decimal Price { get; set; }

    [DataName]
    public Guid ProductCode { get; set; }
}
```

### 2. Veri Okuma (Read)
`EntityContext` sınıfını constructor üzerinden enjekte ederek kullanın:

```csharp
public class ProductService(EntityContext dbContext)
{
    // Tüm listeyi al
    public List<Product> GetAllProducts() 
    {
        var result = dbContext.GetMapClass<Product>();
        return result.IsSuccess ? result.Data : new List<Product>();
    }

    // ID ile tekil kayıt al
    public Product? GetById(int id)
    {
        return dbContext.GetMapClassById<Product>(id).Data;
    }

    // Filtreleme (Expression Tree desteği)
    public List<Product> GetCheapProducts()
    {
        return dbContext.GetMapClass<Product>(p => p.Price < 100).Data;
    }
}
```

### 3. Veri Ekleme (Create)
```csharp
public int AddNewProduct(Product product)
{
    // getById: true parametresi, eklenen kaydın Identity (ID) değerini döner.
    var result = dbContext.DoMapInsert(product, getById: true);
    return result.Data; 
}
```

### 4. Veri Güncelleme (Update)
Kütüphane sadece değişen alanları algılar ve sadece ilgili alanları güncelleyerek performansı artırır:
```csharp
public bool UpdatePrice(int productId, decimal newPrice)
{
    var product = dbContext.GetMapClassById<Product>(productId).Data;
    if (product == null) return false;

    product.Price = newPrice;
    return dbContext.DoMapUpdate(product).IsSuccess;
}
```

### 5. Veri Silme (Delete)
```csharp
public bool DeleteProduct(int id)
{
    var product = new Product { Id = id };
    return dbContext.DoMapDelete(product).IsSuccess;
}
```

---

## ⚡ İleri Düzey Özellikler

### Sağlık Kontrolü (Health Checks)
Kütüphane otomatik olarak bir HealthCheck kaydı yapar. API projenizde şu endpoint'i aktif edebilirsiniz:
```csharp
app.MapHealthChecks("/health");
```

### Loglama (Observability)
Veritabanı hataları veya bağlantı sorunları otomatik olarak `Serilog` üzerinden loglanır. Hata ayıklama sırasında Exception detaylarını log sink'lerinizde (Console, File, Seq vb.) görebilirsiniz.

---

## 🧪 Test Edilebilirlik
Kütüphane arayüzler (`ISqlGenerator`, `IMetadataProvider`) üzerinden çalıştığı için Unit Test projelerinizde kolayca **Mock**'lanabilir. Örnek testler için `OmerkckEF.Bisco.Tests` projesine göz atabilirsiniz.

---
*OmerkckEF - Sade, Hızlı, Modern.*
