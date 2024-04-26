using System.ComponentModel.DataAnnotations;

public class CreateGroupDTO
{
    public string Name { get; set; }

    [MinLength(1)]
    [MaxLength(255)]
    public string? Description { get; set; }

}