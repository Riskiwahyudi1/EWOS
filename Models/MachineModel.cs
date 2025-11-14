using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EWOS_MVC.Models
{
    public class MachineModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori wajib dipilih")]
        public int MachineCategoryId { get; set; }

        [ForeignKey("MachineCategoryId")]
        [ValidateNever]
        public MachineCategoriesModel MachineCategories { get; set; }

        [Required(ErrorMessage = "Nama mesin wajib diisi")]
        public string MachineName { get; set; }

        [Required(ErrorMessage = "Daya mesin wajib diisi")]
        public decimal MachinePower { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
