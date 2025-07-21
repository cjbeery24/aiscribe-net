using System.ComponentModel.DataAnnotations;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for changing user password
/// </summary>
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("NewPassword", ErrorMessage = "Password confirmation does not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
