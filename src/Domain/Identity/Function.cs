using System.ComponentModel.DataAnnotations;
using Boilerate.Domain.Common.Contracts;

namespace Boilerate.Domain.Identity;

/// <summary>
/// Đại diện một module/feature trong hệ thống phân quyền (Dashboard, Users, Roles...).
/// Dữ liệu được seed tự động từ Shared/Authorization/AppFunction constants.
/// </summary>
public class Function : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;
}
