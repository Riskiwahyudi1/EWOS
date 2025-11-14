using System.ComponentModel.DataAnnotations.Schema;

namespace EWOS_MVC.Models
{
    [Table("UserRoles")]
    public class UserRoleModel
    {
        public int UserId { get; set; }
        public UserModel User { get; set; } = null!;

        public int RoleId { get; set; }
        public RoleModel Role { get; set; } = null!;
    }
}
