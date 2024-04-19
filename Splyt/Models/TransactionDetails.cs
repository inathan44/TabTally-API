using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class TransactionDetails
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    public int Id { get; set; }

    [Required]
    public int TransactionId { get; set; }

    [Required]
    public int PayerId { get; set; }

    [ForeignKey("PayerId")]
    public virtual User? Payer { get; set; }

    [Required]
    public int RecipientId { get; set; }

    [ForeignKey("RecipientId")]
    public virtual User? Recipient { get; set; }

    [Required]
    public int GroupId { get; set; }

    [ForeignKey("GroupId")]
    public Group? Group { get; set; }

    [Required]
    [Range(0, 999999999)]
    public decimal Amount { get; set; }
}