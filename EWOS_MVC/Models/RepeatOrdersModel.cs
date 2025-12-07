using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EWOS_MVC.Models
{
    public class RepeatOrderModel
    {
        [Key]
        public long Id { get; set; }

        public int UsersId { get; set; }

        [ForeignKey("UsersId")]
        [ValidateNever]
        public UserModel? Users { get; set; }

        [Required(ErrorMessage = "Item Request wajib diisi")]
        public long ItemRequestId { get; set; }

        [ForeignKey("ItemRequestId")]
        [ValidateNever]
        public ItemRequestModel ItemRequests { get; set; }

        [Required(ErrorMessage = "CRD wajib diisi")]
        [Column(TypeName = "date")]
        public DateTime CRD { get; set; }

        [Column(TypeName = "date")]
        public DateTime? OCD { get; set; }

        [Required(ErrorMessage = "Quantity Request wajib diisi")]
        public int QuantityReq { get; set; }

        public int? QtyOnFab { get; set; }

        public string? COCPath{ get; set; }

        public int? QuantityDone { get; set; }

        [Required(ErrorMessage = "Deskripsi wajib diisi")]
        public string Description { get; set; }


        public string? Status { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        public ICollection<RequestStatusModel> RequestStatus { get; set; } = new List<RequestStatusModel>();
    }
}