using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackEnd_WebSocket.Models
{
    public class VentanaDb
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Handle { get; set; }


        public string Title { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime UltimaActualizacion { get; set; } = DateTime.Now;
    }
}
