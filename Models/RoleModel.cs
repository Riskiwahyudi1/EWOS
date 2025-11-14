using System.ComponentModel.DataAnnotations.Schema;

namespace EWOS_MVC.Models
{
    [Table("Roles")]
    public class RoleModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public virtual ICollection<UserRoleModel> UserRoles { get; set; } = new List<UserRoleModel>();
    }
}
