using System.ComponentModel.DataAnnotations;
namespace Book_Store.ViewModel.users
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Xac nhan mat khau khong khop.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
