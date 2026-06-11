namespace OpeApp.Models;

public class SesionEstudio
{
    public List<Pregunta> Preguntas { get; set; } = [];
    public int IndiceActual { get; set; } = 0;
    public List<int?> RespuestasUsuario { get; set; } = [];

    public Pregunta? PreguntaActual =>
        IndiceActual < Preguntas.Count ? Preguntas[IndiceActual] : null;

    public bool EsUltima => IndiceActual == Preguntas.Count - 1;
    public int Total => Preguntas.Count;
    public int Aciertos => RespuestasUsuario.Count(r => r.HasValue && Preguntas[RespuestasUsuario.IndexOf(r)].Correcta == r.Value);

    public void Inicializar(List<Pregunta> preguntas, bool mantenerOrden = false)
    {
        Preguntas = mantenerOrden ? preguntas : preguntas.OrderBy(_ => Random.Shared.Next()).ToList();
        IndiceActual = 0;
        RespuestasUsuario = new List<int?>(new int?[Preguntas.Count]);
    }

    public void ResponderActual(int indiceRespuesta)
    {
        if (IndiceActual < Preguntas.Count)
            RespuestasUsuario[IndiceActual] = indiceRespuesta;
    }

    public bool? RespuestaActualEsCorrecta()
    {
        var r = RespuestasUsuario[IndiceActual];
        if (r is null) return null;
        return r.Value == PreguntaActual!.Correcta;
    }

    public void Siguiente() { if (!EsUltima) IndiceActual++; }

    public int ContarAciertos() =>
        RespuestasUsuario.Select((r, i) => r.HasValue && r.Value == Preguntas[i].Correcta)
                         .Count(x => x);

    public List<(Pregunta Pregunta, int? RespuestaUsuario)> ObtenerFalladas() =>
        Preguntas.Select((p, i) => (p, RespuestasUsuario[i]))
                 .Where(x => x.Item2.HasValue && x.Item2.Value != x.p.Correcta)
                 .ToList();
}
