using System.Collections.Generic;
using EWOS_MVC.Models;

namespace EWOS_MVC.Models.ViewModels
{
    public class UserRolesViewModel
    {
        public List<UserModel> Users { get; set; }

        public UserModel SelectedUser { get; set; }
        public List<RoleModel> Roles { get; set; }
        public List<UserRoleModel> UserRoles { get; set; }
    }
}
