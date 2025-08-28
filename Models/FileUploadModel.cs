using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ABC_Retail_StorageApp.Models
{
    public class FileUploadModel
    {
        [Required]
        [Display(Name = "Proof of Payment")]
        public IFormFile ProofOfPayment { get; set; }

        [Display(Name = "Order ID")]
        public string? OrderID { get; set; }

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }
    }
}