using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace appWeb2.Models
{
    public class VideoJuegos
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string titulo { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal precio { get; set; }

        [Required]
        public string descripcion { get; set; }

        public string? imagen { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Required]
        public bool EnPromocion { get; set; } = false;

        [Required]
        [StringLength(10)]
        public string EdadMinima { get; set; } = "+12";

        [ForeignKey("Categoria")]
        public int? idcategoria { get; set; }

        public Categoria? Categoria { get; set; }

        //public ICollection<Compra> Compras { get; set; }
    }
}
