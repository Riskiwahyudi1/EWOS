using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EWOS_MVC.Models
{
    public class WeeksSettingModel
    {
        public int Id { get; set; }

        public int Week { get; set; }

        public int Month { get; set; }
        public int YearSettingId { get; set; }
        [ForeignKey("YearSettingId")]
        [ValidateNever]
        public YearsSettingModel YearsSetting { get; set; }
        public Decimal WorkingDays { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}