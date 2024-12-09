using System.ComponentModel.DataAnnotations;



public class CreateGroupDTO
{
    public string Name { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

}
