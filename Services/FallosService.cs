using System.Text.Json;
using Microsoft.JSInterop;
using OpeApp.Models;

namespace OpeApp.Services;

public class FallosService(IJSRuntime js)
{
    private const string StorageKey = "opeapp_fallos";
    private List<PreguntaFallada>? _cache;

    public async Task<List<PreguntaFallada>> ObtenerFallos()
    {
        if (_cache is not null) return _cache;

        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            _cache = string.IsNullOrWhiteSpace(json)
                ? []
                : (JsonSerializer.Deserialize<List<PreguntaFallada>>(json) ?? []);
        }
        catch
        {
            _cache = [];
        }

        return _cache;
    }

    public async Task AgregarFallo(Pregunta pregunta)
    {
        if (pregunta.Correcta < 0)
        {
            return;
        }
        var fallos = await ObtenerFallos();
        var clave = $"{(int)pregunta.Bateria}-{pregunta.Id}";
        var existente = fallos.FirstOrDefault(f => $"{(int)f.Bateria}-{f.Id}" == clave);

        if (existente is not null)
        {
            existente.FallosCount++;
            existente.UltimaFecha = DateTime.UtcNow;
        }
        else
        {
            fallos.Add(new PreguntaFallada
            {
                Bateria = pregunta.Bateria,
                Id = pregunta.Id,
                Enunciado = pregunta.Enunciado,
                Respuestas = pregunta.Respuestas,
                Correcta = pregunta.Correcta
            });
        }

        await Guardar(fallos);
    }

    public async Task EliminarFallo(TipoBateria bateria, int id)
    {
        var fallos = await ObtenerFallos();
        fallos.RemoveAll(f => f.Bateria == bateria && f.Id == id);
        await Guardar(fallos);
    }

    public async Task LimpiarFallos()
    {
        _cache = [];
        try
        {
            await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        catch
        {
        }
    }

    private async Task Guardar(List<PreguntaFallada> fallos)
    {
        try
        {
            var json = JsonSerializer.Serialize(fallos);
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch
        {
        }
    }
}
