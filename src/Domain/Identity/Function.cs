using System.ComponentModel.DataAnnotations;

namespace Boilerate.Domain.Identity;

/// <summary>
/// Đại diện một module/feature trong hệ thống phân quyền (Dashboard, Users, Roles...).
/// Dữ liệu được seed tự động từ Shared/Authorization/AppFunction constants.
/// </summary>
public class Function
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;
}
