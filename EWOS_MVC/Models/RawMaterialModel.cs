using EWOS_MVC.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InhouseFabricationSystem.Models
{
    public class RawMaterialModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mesin Kategori wajib dipilih")]
        public int MachineCategoryId { get; set; }

        [ForeignKey("MachineCategoryId")]
        [ValidateNever]
        public MachineCategoriesModel MachineCategories { get; set; }

        [Required(ErrorMessage = "Sap ID  wajib diisi")]
        public int SAPID { get; set; }

        [Required(ErrorMessage = "Nama Raw Material wajib diisi")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Price wajib diisi")]
        public decimal Price { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}