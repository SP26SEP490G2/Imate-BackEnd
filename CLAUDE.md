# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project scope
- This repository currently contains a single .NET 8 Web API project: `Imate.API/Imate.API.csproj`.
- The root solution file `Imate.API.slnx` points to that single project.

## Common development commands
Run from repository root (`D:\Do An\Imate-BackEnd`).

- Restore dependencies:
  - `dotnet restore Imate.API.slnx`
- Build:
  - `dotnet build Imate.API.slnx -c Debug`
  - `dotnet build Imate.API.slnx -c Release`
- Run API:
  - `dotnet run --project Imate.API/Imate.API.csproj --launch-profile https`
  - `dotnet watch --project Imate.API/Imate.API.csproj run`
- Lint/format (SDK formatter):
  - `dotnet format Imate.API/Imate.API.csproj --verify-no-changes`
  - `dotnet format Imate.API/Imate.API.csproj`
- Tests:
  - `dotnet test Imate.API.slnx`
  - Note: no test project is currently present in this repository, so `dotnet test` will discover 0 tests.
  - Single-test pattern (when a test project is added):
    - `dotnet test path/to/YourTests.csproj --filter "FullyQualifiedName~Namespace.ClassName.MethodName"`
- EF Core migrations/database:
  - `dotnet ef migrations list --project Imate.API/Imate.API.csproj --startup-project Imate.API/Imate.API.csproj`
  - `dotnet ef migrations add <MigrationName> --project Imate.API/Imate.API.csproj --startup-project Imate.API/Imate.API.csproj --output-dir Migrations`
  - `dotnet ef database update --project Imate.API/Imate.API.csproj --startup-project Imate.API/Imate.API.csproj`

Useful local URLs from launch profile:
- `https://localhost:7283/swagger`
- `http://localhost:5067/swagger`

## High-level architecture
The codebase follows a layered ASP.NET Core architecture:

1. **Presentation layer** (`Imate.API/Presentation`)
   - Controllers organized by domains (`AuthManagement`, `UserManagement`, `Classification`, `Mentors`, `QuestionBank`).
   - Request/response DTOs under `Presentation/RequestModels` and `Presentation/ResponseModels`.

2. **Business layer** (`Imate.API/Business`)
   - Service interfaces and implementations by domain.
   - Domain exceptions (`BadRequestException`, `NotFoundException`, `ConflictException`, etc.).
   - Shared helpers such as pagination (`Business/Helper/PagedList.cs`).

3. **Data access layer** (`Imate.API/DataAccess`)
   - EF Core `ImateDbContext` with many domain entities (`Models/Entities`) and per-entity configurations (`DataAccess/Configurations`).
   - Repository pattern (`RepositoryBase<T>`, concrete repositories) plus `IUnitOfWork`/`UnitOfWork`.

4. **Infrastructure / cross-cutting**
   - DI and auth configuration in:
     - `Imate.API/Configurations/ServiceExtensions.cs`
     - `Imate.API/Infrastructure/Configurations/ServiceExtensions.cs`
   - JWT options and auth setup, Firebase Admin bootstrap, mail settings binding.
   - External integrations in `Imate.API/ExternalServices` (AWS S3, email, OpenAI, PayOS).
   - Global exception middleware in `Imate.API/Middleware/GlobalExceptionMiddleware.cs`.
   - Hosted background worker in `Imate.API/BackgroundServices/SubscriptionExpirationBackgroundService.cs`.

## Request flow (typical)
- HTTP request -> Controller (`Presentation`) -> Business service (`Business/Services`) -> Repository/UnitOfWork (`DataAccess`) -> `ImateDbContext` -> SQL Server.
- Auth endpoints verify Firebase tokens, then map to local accounts/roles and issue local JWT + refresh tokens.
- API route constants are centralized in `Imate.API/Common/URIs/APIConfig.cs` and mixed with literal route attributes in controllers.

## Important implementation notes
- `Program.cs` wires **both** service-extension classes (`Configurations` and `Infrastructure/Configurations`), so DI registrations can be split/duplicated. Check both files before adding/changing registrations.
- `GlobalExceptionMiddleware` maps unhandled exceptions to HTTP 500 `ProblemDetails`; many controllers also do explicit per-action exception handling.
- Firebase startup (`Infrastructure/Configurations/FirebaseServiceExtensions.cs`) expects `serviceAccountKey.json` to exist at runtime in `AppContext.BaseDirectory`; missing file throws during startup.
- Authentication is JWT Bearer; query-string token extraction is enabled for path `/api/systemNotificationHub` in JWT events.

## Coding Conventions
- **Naming:** 
  - Interface bắt đầu bằng `I` (VD: `IUserService`).
  - Hàm bất đồng bộ (Async) phải có hậu tố `Async` (VD: `GetUserByIdAsync`).
  - Biến private readonly trong class phải có tiền tố `_` camelCase (VD: `_userRepository`).
- **Dependency Injection:** Luôn sử dụng Constructor Injection.
- **Error Handling:** 
  - Tầng Business **không bao giờ** ném ra các lỗi mang tính chất HTTP (như HTTP Status Code).
  - Thay vào đó, throw các Domain Exceptions (`BadRequestException`, `NotFoundException`, `ConflictException`, v.v.) để `GlobalExceptionMiddleware` tự động map sang HTTP Status Code tương ứng.
- **DTOs:** Khuyến khích sử dụng `record` của C# cho các Request/Response Models để đảm bảo tính bất biến.
- **LINQ & EF Core:** Sử dụng `.AsNoTracking()` cho các truy vấn chỉ đọc (Read-only queries) trong Repository để tối ưu hiệu suất.
