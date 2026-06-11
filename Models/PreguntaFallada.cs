namespace OpeApp.Models;

public class PreguntaFallada
{
    public TipoBateria Bateria { get; set; }
    public int Id { get; set; }
    public string Enunciado { get; set; } = string.Empty;
    public List<string> Respuestas { get; set; } = [];
    public int Correcta { get; set; }
    public int FallosCount { get; set; } = 1;
    public DateTime UltimaFecha { get; set; } = DateTime.UtcNow;

    public string Clave => $"{(int)Bateria}-{Id}";
}
