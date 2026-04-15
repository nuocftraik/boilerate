using System.ComponentModel.DataAnnotations;

namespace Boilerate.Domain.Identity;

/// <summary>
/// Đại diện một hành động trong hệ thống phân quyền (View, Create, Update, Delete...).
/// Dữ liệu được seed tự động từ Shared/Authorization/AppAction constants.
/// </summary>
public class Action
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;
}
