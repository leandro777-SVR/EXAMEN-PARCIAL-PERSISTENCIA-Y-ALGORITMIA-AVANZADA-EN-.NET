namespace ParcialDron;

/// <summary>
/// Clase que implementa el algoritmo de vuelo recursivo del dron
/// con heurística de Warnsdorff (menor grado primero) y backtracking.
/// </summary>
public class AlgoritmoDron
{
    // Los 8 movimientos posibles del patrón 2x1 (como un caballo de ajedrez)
    private static readonly int[] MovFila   = { -2, -2, +2, +2, -1, -1, +1, +1 };
    private static readonly int[] MovColumna = { -1, +1, -1, +1, -2, +2, -2, +2 };

    private int _n;
    private int[,] _terreno;
    private int _totalAlcanzables;

    // Secuencia de movimientos: lista de (paso, fila, columna)
    public List<(int Paso, int Fila, int Columna)> Secuencia { get; private set; } = new();

    public AlgoritmoDron(int n)
    {
        _n = n;
        _terreno = new int[n, n];

        // Inicializar todo como -1 (no visitado)
        for (int f = 0; f < n; f++)
            for (int c = 0; c < n; c++)
                _terreno[f, c] = -1;

        _totalAlcanzables = 0;
    }

    /// <summary>
    /// Punto de entrada principal. Determina las parcelas alcanzables,
    /// luego lanza la recursión.
    /// </summary>
    public bool Resolver(int filaInicio, int colInicio)
    {
        // Contar las parcelas alcanzables desde el punto de despegue (BFS/DFS)
        _totalAlcanzables = ContarAlcanzables(filaInicio, colInicio);

        // Marcar la posición de despegue como el paso 0
        _terreno[filaInicio, colInicio] = 0;
        Secuencia.Add((0, filaInicio, colInicio));

        // Si solo hay 1 parcela alcanzable (el propio inicio), ya terminamos
        if (_totalAlcanzables == 1)
            return true;

        // Lanzar la recursión desde el inicio
        return Recursion(filaInicio, colInicio, 1);
    }

    /// <summary>
    /// Método recursivo principal con heurística de menor grado y backtracking.
    /// </summary>
    private bool Recursion(int fila, int col, int pasoActual)
    {
        // Condición de éxito: se cubrieron todas las parcelas alcanzables
        if (pasoActual == _totalAlcanzables)
            return true;

        // Obtener candidatos válidos desde la posición actual
        List<(int Fila, int Col, int Grado)> candidatos = ObtenerCandidatosOrdenados(fila, col);

        // Probar cada candidato en orden de menor grado
        foreach (var (candidatoFila, candidatoCol, _) in candidatos)
        {
            // Avanzar: marcar la parcela con el número de paso
            _terreno[candidatoFila, candidatoCol] = pasoActual;
            Secuencia.Add((pasoActual, candidatoFila, candidatoCol));

            // Llamada recursiva
            if (Recursion(candidatoFila, candidatoCol, pasoActual + 1))
                return true;

            // Backtracking: deshacer el último paso
            _terreno[candidatoFila, candidatoCol] = -1;
            Secuencia.RemoveAt(Secuencia.Count - 1);
        }

        // No se encontró solución desde aquí
        return false;
    }

    /// <summary>
    /// Obtiene los candidatos válidos desde una posición, ordenados por grado ascendente
    /// (heurística de Warnsdorff: probar primero el destino con MENOS salidas libres).
    /// </summary>
    private List<(int Fila, int Col, int Grado)> ObtenerCandidatosOrdenados(int fila, int col)
    {
        var candidatos = new List<(int Fila, int Col, int Grado)>();

        for (int i = 0; i < 8; i++)
        {
            int nuevaFila = fila + MovFila[i];
            int nuevaCol  = col  + MovColumna[i];

            if (EsValida(nuevaFila, nuevaCol) && _terreno[nuevaFila, nuevaCol] == -1)
            {
                int grado = CalcularGrado(nuevaFila, nuevaCol);
                candidatos.Add((nuevaFila, nuevaCol, grado));
            }
        }

        // Ordenar por grado ascendente (menor grado primero)
        candidatos.Sort((a, b) => a.Grado.CompareTo(b.Grado));
        return candidatos;
    }

    /// <summary>
    /// Calcula el "grado" de una parcela: cuántas salidas libres tiene desde ella.
    /// </summary>
    private int CalcularGrado(int fila, int col)
    {
        int grado = 0;
        for (int i = 0; i < 8; i++)
        {
            int nf = fila + MovFila[i];
            int nc = col  + MovColumna[i];
            if (EsValida(nf, nc) && _terreno[nf, nc] == -1)
                grado++;
        }
        return grado;
    }

    /// <summary>
    /// Cuenta las parcelas alcanzables desde el inicio usando DFS.
    /// Esto determina el objetivo real que debe cubrir el dron.
    /// </summary>
    private int ContarAlcanzables(int filaInicio, int colInicio)
    {
        bool[,] visitado = new bool[_n, _n];
        int contador = 0;
        DfsContar(filaInicio, colInicio, visitado, ref contador);
        return contador;
    }

    private void DfsContar(int fila, int col, bool[,] visitado, ref int contador)
    {
        if (!EsValida(fila, col) || visitado[fila, col])
            return;

        visitado[fila, col] = true;
        contador++;

        for (int i = 0; i < 8; i++)
        {
            DfsContar(fila + MovFila[i], col + MovColumna[i], visitado, ref contador);
        }
    }

    /// <summary>
    /// Verifica si una coordenada está dentro del terreno.
    /// </summary>
    private bool EsValida(int fila, int col)
    {
        return fila >= 0 && fila < _n && col >= 0 && col < _n;
    }

    /// <summary>
    /// Devuelve el terreno interno para visualización.
    /// </summary>
    public int[,] ObtenerTerreno() => _terreno;

    /// <summary>
    /// Devuelve la cantidad de parcelas alcanzables.
    /// </summary>
    public int TotalAlcanzables => _totalAlcanzables;
}