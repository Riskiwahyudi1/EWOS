using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace EWOS_MVC.Services
{
    public class AdUserService
    {
        //cari user di AD dengan windows autentication
        public UserPrincipal? GetUserInfo(string username)
        {
            using (var context = new PrincipalContext(ContextType.Domain, "EXCELITAS"))
            {
                return UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
            }
        }
      
        //cari user di AD dengan properti lengkap
        public Dictionary<string, object> GetAllAdProperties(string username)
        {
            var result = new Dictionary<string, object>();

            using (var context = new PrincipalContext(ContextType.Domain, "EXCELITAS"))
            {
                var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
                if (user?.GetUnderlyingObject() is DirectoryEntry entry)
                {
                    foreach (string propName in entry.Properties.PropertyNames)
                    {
                        var propValues = entry.Properties[propName];

                        if (propValues != null && propValues.Count > 0)
                        {
                            if (propValues.Count > 1)
                            {
                                var allValues = new List<string>();
                                foreach (var val in propValues)
                                    allValues.Add(val?.ToString() ?? "");
                                result[propName] = string.Join(", ", allValues);
                            }
                            else
                            {
                                result[propName] = propValues[0]?.ToString() ?? "";
                            }
                        }

                    }
                    //menampilkan atasan dari AD
                    var managerDn = entry.Properties["manager"]?.Value?.ToString();
                    if (!string.IsNullOrEmpty(managerDn))
                    {
                        try
                        {
                            using (var managerEntry = new DirectoryEntry($"LDAP://{managerDn}"))
                            {
                                var managerEmail = managerEntry.Properties["mail"]?.Value?.ToString();
                                var managerDisplayName = managerEntry.Properties["displayName"]?.Value?.ToString();

                                // Simpan ke hasil dengan nama properti khusus
                                result["managerEmail"] = managerEmail ?? "(tidak ada email)";
                                result["managerDisplayName"] = managerDisplayName ?? "(tidak ada nama)";
                            }
                        }
                        catch (Exception ex)
                        {
                            result["managerEmail"] = $"(gagal membaca email manager: {ex.Message})";
                        }
                    }

                }
            }

            return result;
        }

        //untuk searching email user di AD
        public List<(string DisplayName, string Email)> SearchUsersByEmail(string keyword)
        {
            var result = new List<(string, string)>();

            using (var context = new PrincipalContext(ContextType.Domain, "EXCELITAS"))
            {
                var userFilter = new UserPrincipal(context)
                {
                    EmailAddress = $"*{keyword}*"
                };

                using (var searcher = new PrincipalSearcher(userFilter))
                {
                    foreach (var found in searcher.FindAll())
                    {
                        if (found is UserPrincipal user)
                        {
                            result.Add((user.DisplayName ?? "-", user.EmailAddress ?? "-"));
                        }
                    }
                }
            }

            return result;
        }

    }
}
