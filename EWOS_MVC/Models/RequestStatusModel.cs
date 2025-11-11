using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EWOS_MVC.Models
{
    public class RequestStatusModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public long? ItemRequestId { get; set; }
        public ItemRequestModel ItemRequest { get; set; } 

        //[Required]
        //public long? RepeatOrderId { get; set; }
        //public RepeatOrderModel RepeatOrders { get; set; } 

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [ValidateNever]
        public UserModel Users { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; }

        [StringLength(255)]
        public string? Reason { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

    }
}