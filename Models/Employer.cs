using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebServiceCosmetics.Models;

namespace WebServiceCosmetics.Models
{
    [Table("Employees")]

    public class Employer
    {
        [Key]

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // Автоинкремент для Id

        public int Id { get; set; }
        public string Full_Name { get; set; }
        public int? Position_id { get; set; }

        // Навигационное свойство для связи с единицей измерения
        [ForeignKey("Position_id")]
        public Positions Positions { get; set; }
        public decimal Salary { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
    }
}
[Table("Positions")]

public class Positions
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Name { get; set; }
    public ICollection<Employer> Employer { get; set; }
}

    

