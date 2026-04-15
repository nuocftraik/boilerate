using System.ComponentModel.DataAnnotations;
using Boilerate.Domain.Common.Contracts;

namespace Boilerate.Domain.Identity;

/// <summary>
/// Đại diện một hành động trong hệ thống phân quyền (View, Create, Update, Delete...).
/// Dữ liệu được seed tự động từ Shared/Authorization/AppAction constants.
/// </summary>
public class Action : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;
}
