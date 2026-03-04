using System.ComponentModel.DataAnnotations;

namespace appWeb2.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string nombre { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string correo { get; set; }

        [Required]
        [StringLength (255)]
        public string password { get; set; }

        [Required]
        public DateTime FechaRegistro  { get; set; } = DateTime.Now;

        public ICollection<Compra> Compras { get; set; }

    }
}
