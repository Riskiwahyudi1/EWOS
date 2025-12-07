using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EWOS_MVC.Models
{
    public class ItemFabricationModel
    {
        public int Id { get; set; }

        public long ItemRequestId { get; set; }

        [ForeignKey("ItemRequestId")]
        [ValidateNever]
        public ItemRequestModel ItemRequest { get; set; }

        [Required(ErrorMessage = "Id Machinne wajib diisi")]
        public int MachineId { get; set; }

        [ForeignKey("MachineId")]
        [ValidateNever]
        public MachineModel? Machine { get; set; }

        public long? RepeatOrderId { get; set; }

        [ForeignKey("RepeatOrderId")]
        [ValidateNever]
        public RepeatOrderModel? RepeatOrder { get; set; }

        [Required(ErrorMessage = "Id week wajib diisi")]
        public int WeeksSettingId { get; set; }

        [ForeignKey("WeeksSettingId")]
        [ValidateNever]
        public WeeksSettingModel WeeksSetting { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        [ValidateNever]
        public UserModel Users { get; set; }

        [Required(ErrorMessage = "Quantity wajib diisi")]
        public int Quantity { get; set; }
        public string? FabCode { get; set; }

        [Required(ErrorMessage = "Status wajib diisi")]
        public string Status { get; set; }
        public decimal? TotalSaving { get; set; }
        public decimal FabricationTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}