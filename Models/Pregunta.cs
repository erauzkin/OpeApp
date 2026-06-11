namespace OpeApp.Models;

public class Pregunta
{
    public int Id { get; set; }
    public string Enunciado { get; set; } = string.Empty;
    public List<string> Respuestas { get; set; } = [];
    public int Correcta { get; set; }
    public TipoBateria Bateria { get; set; }
}
