namespace Boilerate.Infrastructure.Persistence.Initialization;

/// <summary>
/// Marker interface cho các custom seeder.
/// Mọi class implement ICustomSeeder sẽ được tự động phát hiện qua DI và chạy sau khi seed dữ liệu nền tảng.
/// </summary>
public interface ICustomSeeder
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
