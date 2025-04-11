namespace BackEnd_WebSocket.Models
{
    public class VentanaState
    {
        public int Id { get; set; }
        public long Handle { get; set; }
        public string Title { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
