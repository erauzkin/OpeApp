using System.Net.Http.Json;
using OpeApp.Models;

namespace OpeApp.Services;

public class PreguntaService(HttpClient http)
{
    private List<Pregunta>? _comun;
    private List<Pregunta>? _especifica;

    public async Task<List<Pregunta>> ObtenerPorBateria(TipoBateria bateria)
    {
        return bateria switch
        {
            TipoBateria.Comun => await CargarComun(),
            TipoBateria.Especifica => await CargarEspecifica(),
            _ => []
        };
    }

    public async Task<List<Pregunta>> ObtenerTodas()
    {
        var comun = await CargarComun();
        var especifica = await CargarEspecifica();
        return [.. comun, .. especifica];
    }

    private async Task<List<Pregunta>> CargarComun()
    {
        if (_comun is not null) return _comun;
        try
        {
            var preguntas = await http.GetFromJsonAsync<List<PreguntaJson>>("data/comun.json") ?? [];
            _comun = preguntas.Select((p, i) => Mapear(p, TipoBateria.Comun)).ToList();
        }
        catch (Exception)
        {
            var preguntas = await http.GetFromJsonAsync<List<PreguntaJson>>("data/_comun.json") ?? [];
            _comun = preguntas.Select((p, i) => Mapear(p, TipoBateria.Comun)).ToList();
        }
       
        return _comun;
    }

    private async Task<List<Pregunta>> CargarEspecifica()
    {
        if (_especifica is not null) return _especifica;
        try
        {
            var preguntas = await http.GetFromJsonAsync<List<PreguntaJson>>("data/especifica.json") ?? [];
            _especifica = preguntas.Select((p, i) => Mapear(p, TipoBateria.Especifica)).ToList();
        }
        catch (Exception)
        {
            var preguntas = await http.GetFromJsonAsync<List<PreguntaJson>>("data/_especifica.json") ?? [];
            _especifica = preguntas.Select((p, i) => Mapear(p, TipoBateria.Especifica)).ToList();
        }
        return _especifica;
    }

    private static Pregunta Mapear(PreguntaJson p, TipoBateria bateria) => new()
    {
        Id = p.Id,
        Enunciado = p.Pregunta,
        Respuestas = p.Respuestas,
        Correcta = p.Correcta,
        Bateria = bateria
    };

    private record PreguntaJson(int Id, string Pregunta, List<string> Respuestas, int Correcta);
}
