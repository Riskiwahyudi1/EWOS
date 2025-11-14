using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EWOS_MVC.Models
{
    public class YearsSettingModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tahun wajib dipilih")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Hari mulai wajib diisi")]
        public DateTime StartDate { get; set; }
        public decimal? ElectricalCost { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}