using EWOS_MVC.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EWOS_MVC.Models
{
    public class ItemRequestModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Nama part wajib diisi")]
        [StringLength(100, ErrorMessage = "Nama part tidak boleh lebih dari 100 karakter")]
        public string PartName { get; set; }

        [Required(ErrorMessage = "Unit part wajib diisi")]
        public string Unit { get; set; }

        [Required(ErrorMessage = "Kategori wajib dipilih")]
        public int MachineCategoryId { get; set; }

        [ForeignKey("MachineCategoryId")]
        [ValidateNever]
        public MachineCategoriesModel MachineCategories { get; set; }

        [Required(ErrorMessage = "UserId Tidak boleh kosong")]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        [ValidateNever]
        public UserModel Users { get; set; }

        [Required(ErrorMessage = "CRD wajib diisi")]
        public DateTime CRD { get; set; }
       
        [Required(ErrorMessage = "Deskripsi wajib diisi")]
        [StringLength(255, ErrorMessage = "Deskripsi tidak boleh lebih dari 255 karakter")]
        public string Description { get; set; }

        public string? QuantationPath { get; set; }
        public string? DrawingPath { get; set; }
        public string? COCPath { get; set; }
        public string? DesignPath { get; set; }
        public decimal? FabricationTime { get; set; }
        public int? RawMaterialId { get; set; }
        public long? SAPID { get; set; }

        [ForeignKey("RawMaterialId")]
        [ValidateNever]
        public RawMaterialModel RawMaterials { get; set; }

        public decimal? Weight { get; set; }
        public decimal? ExternalFabCost { get; set; }

        [StringLength(50, ErrorMessage = "Part Code tidak boleh lebih dari 50 karakter")]
        public string? PartCode { get; set; }

        [StringLength(50, ErrorMessage = "Status tidak boleh lebih dari 50 karakter")]
        public string? Status { get; set; }
        public int? RevisiNo { get; set; }
        public bool IsCalculateSaving { get; set; } = true;
        public DateTime? OCD { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<RequestStatusModel> RequestStatus { get; set; } = new List<RequestStatusModel>();

    }
}