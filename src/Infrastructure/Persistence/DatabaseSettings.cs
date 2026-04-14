using System.ComponentModel.DataAnnotations;

namespace Boilerate.Infrastructure.Persistence;

public class DatabaseSettings
{
    [Required]
    public string DBProvider { get; set; } = string.Empty;

    [Required]
    public string ConnectionString { get; set; } = string.Empty;
}
