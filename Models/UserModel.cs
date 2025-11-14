namespace EWOS_MVC.Models
{

    //Note : relasi user dan role adalah many to many dan di join pada model UserRoles
    public class UserModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public string? Name { get; set; }
        public string? Badge { get; set; }
        public string Email { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<UserRoleModel> UserRoles { get; set; } = new List<UserRoleModel>();

    }
}
