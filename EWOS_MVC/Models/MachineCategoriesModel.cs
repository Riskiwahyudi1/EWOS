using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EWOS_MVC.Models
{
    public class MachineCategoriesModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama Kategori wajib Diisi!")]
        public string CategoryName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
