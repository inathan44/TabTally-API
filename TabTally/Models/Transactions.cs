using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Transaction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    public int Id { get; set; }


    [Required]
    public string CreatedBy { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User? User { get; set; }

    [Required]
    public string PayerId { get; set; }

    [ForeignKey("PayerId")]
    public virtual User? Payer { get; set; }

    [Required]
    [Range(0, 999999999)]
    public decimal Amount { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    [Required]
    public int GroupId { get; set; }

    [ForeignKey("GroupId")]
    public virtual Group? Group { get; set; }

    public ICollection<TransactionDetails>? TransactionDetails { get; set; }

}