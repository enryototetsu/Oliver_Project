using System;
using System.ComponentModel.DataAnnotations;

namespace Oliver_Project.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Facility description is required.")]
        [StringLength(255, ErrorMessage = "Facility description cannot exceed 255 characters.")]
        public string FacilityDescription { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.DateTime)]
        public DateTime BookingDateFrom { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.DateTime)]
        [BookingDateValidation("BookingDateFrom", ErrorMessage = "End date must be after start date.")]
        public DateTime BookingDateTo { get; set; }

        [Required(ErrorMessage = "BookedBy field is required.")]
        [StringLength(100, ErrorMessage = "BookedBy cannot exceed 100 characters.")]
        public string BookedBy { get; set; }

        [Required(ErrorMessage = "Booking status is required.")]
        [RegularExpression("^(Pending|Approved|Rejected|Cancelled|Completed)$", ErrorMessage = "Invalid booking status. Allowed values: Pending, Approved, Rejected, Cancelled, Completed.")]
        public string BookingStatus { get; set; }
    }

    public class BookingDateValidation : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public BookingDateValidation(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var currentValue = (DateTime)value;
            var comparisonProperty = validationContext.ObjectType.GetProperty(_comparisonProperty);

            if (comparisonProperty == null)
                return new ValidationResult($"Unknown property: {_comparisonProperty}");

            var comparisonValue = (DateTime)comparisonProperty.GetValue(validationContext.ObjectInstance);

            if (currentValue <= comparisonValue)
                return new ValidationResult(ErrorMessage);

            return ValidationResult.Success;
        }
    }
}
