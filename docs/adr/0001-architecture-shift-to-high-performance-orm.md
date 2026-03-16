# ADR 0001: Architecture Shift to High-Performance ADO.NET Wrapper

## Date
2026-03-16

## Status
Accepted

## Context
OmerkckEF aims to be a Tier 3, open-source, high-throughput ORM alternative to heavy frameworks like EF Core. The current implementation relies on runtime reflection (`EntityContext`/`Bisco`), legacy packages (`System.Data.SqlClient`), and static classes (`Tools`, `Extensions`). These practices hinder high-performance scenarios (causing unnecessary allocations and GC pressure) and limit testability due to hardcoded dependencies and static state. 

To meet the requirements of modern .NET 10 development, zero-allocation goals, and strict security (SQL Injection prevention, encrypted connection strings), the architecture must evolve.

## Decision
1. **Target Framework:** Upgrade the project from `.NET 8.0` to `.NET 10.0 LTS`.
2. **Database Drivers:** Replace `System.Data.SqlClient` with the modern and secure `Microsoft.Data.SqlClient`.
3. **Mapping Engine:** Transition from runtime reflection to high-performance alternatives (e.g., cached delegates, IL Emit, or C# Source Generators) for object-relational mapping.
4. **Memory Management:** Utilize `.NET 10` features like `Span<T>`, `ReadOnlySpan<T>`, and `SearchValues<T>` in SQL generation to achieve zero-allocation (or near-zero) during high-frequency queries.
5. **Clean Architecture:** Replace `static class` utilities with Dependency Injection (DI) friendly interfaces (e.g., `ISqlBuilder`, `IDbConnectionFactory`) following SOLID principles.

## Consequences
- **Positive:** Significantly higher throughput, minimal memory footprint, strong SQL injection prevention, and improved testability via DI.
- **Negative:** Increased development complexity (especially regarding high-performance mapping techniques) and breaking changes for existing users of the .NET 8 version.
