using Microsoft.Extensions.Configuration;
using ParcialDron;

// ============================================================
// PARTE C: Cargar configuración desde appsettings.json
// ============================================================
IConfiguration config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

string? connectionString = config.GetConnectionString("PostgreSQL");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("ERROR: No se encontró la cadena de conexión en appsettings.json.");
    return;
}

// ============================================================
// PARTE E: Interfaz de consola
// ============================================================
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║      SIMULADOR DE TRAYECTORIA DE DRON AUTOMATIZADO   ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();

// -- PARTE E.2: Solicitar y validar N --
int n = 0;
while (true)
{
    Console.Write("Ingrese el tamaño del terreno N (entero >= 1): ");
    string? inputN = Console.ReadLine();

    if (int.TryParse(inputN, out n) && n >= 1)
        break;

    Console.WriteLine("  [!] N debe ser un entero mayor o igual a 1. Intente nuevamente.");
}

// -- PARTE E.2: Solicitar y validar coordenadas de despegue --
int x = 0, y = 0;
while (true)
{
    Console.Write($"Ingrese la coordenada X de despegue (fila, rango [0, {n - 1}]): ");
    string? inputX = Console.ReadLine();

    if (int.TryParse(inputX, out x) && x >= 0 && x <= n - 1)
        break;

    Console.WriteLine($"  [!] X debe estar en el rango [0, {n - 1}]. Intente nuevamente.");
}

while (true)
{
    Console.Write($"Ingrese la coordenada Y de despegue (columna, rango [0, {n - 1}]): ");
    string? inputY = Console.ReadLine();

    if (int.TryParse(inputY, out y) && y >= 0 && y <= n - 1)
        break;

    Console.WriteLine($"  [!] Y debe estar en el rango [0, {n - 1}]. Intente nuevamente.");
}

Console.WriteLine();
Console.WriteLine($"Iniciando simulación: terreno {n}x{n}, despegue en ({x}, {y})...");
Console.WriteLine();

// ============================================================
// PARTE B: Ejecutar el algoritmo recursivo
// ============================================================
AlgoritmoDron dron = new AlgoritmoDron(n);
bool exito = dron.Resolver(x, y);

// ============================================================
// PARTE E.3: Mostrar la matriz del recorrido
// ============================================================
int[,] terreno = dron.ObtenerTerreno();
int totalAlcanzables = dron.TotalAlcanzables;

Console.WriteLine($"Parcelas alcanzables desde ({x},{y}): {totalAlcanzables} de {n * n}");
Console.WriteLine();

if (!exito)
{
    Console.WriteLine("══════════════════════════════════════════════════");
    Console.WriteLine("  RESULTADO: SIN SOLUCIÓN");
    Console.WriteLine($"  El dron puede alcanzar {totalAlcanzables} parcelas, pero no existe");
    Console.WriteLine("  una ruta que las cubra todas sin repetir.");
    Console.WriteLine("══════════════════════════════════════════════════");

    // Mostrar de todas formas el estado del terreno al momento de fallar
    Console.WriteLine();
    Console.WriteLine("Estado final del terreno (parcial):");
    MostrarMatriz(terreno, n);
    return;
}

// Éxito: mostrar la matriz completa
Console.WriteLine("══════════════════════════════════════════════════");
Console.WriteLine($"  RESULTADO: ÉXITO — {totalAlcanzables} parcelas cubiertas");
Console.WriteLine("══════════════════════════════════════════════════");
Console.WriteLine();
Console.WriteLine("Matriz del recorrido (número = orden de pisada, '.' = no alcanzable):");
MostrarMatriz(terreno, n);

// ============================================================
// PARTE D: Guardar en PostgreSQL
// ============================================================
Console.WriteLine();
Console.Write("Guardando simulación en la base de datos...");

try
{
    DatabaseManager db = new DatabaseManager(connectionString);
    int idGenerado = db.GuardarSimulacion(n, x, y, dron.Secuencia);

    Console.WriteLine($" OK");
    Console.WriteLine($"  -> ID de simulación generado: {idGenerado}");

    // ============================================================
    // PARTE E.5: Reporte inverso — últimos 5 pasos reconstruidos
    // ============================================================
    db.MostrarUltimos5(idGenerado);
}
catch (Exception ex)
{
    Console.WriteLine($" ERROR");
    Console.WriteLine($"  -> {ex.Message}");
    Console.WriteLine("  (Verifique que el contenedor PostgreSQL esté activo y el script DDL ejecutado)");
}

Console.WriteLine();
Console.WriteLine("Presione cualquier tecla para salir...");
Console.ReadKey();

// ============================================================
// Función local: dibuja la matriz en consola
// ============================================================
static void MostrarMatriz(int[,] terreno, int n)
{
    // Calcular ancho de celda para alinear bien
    int maxVal = n * n - 1;
    int ancho  = maxVal.ToString().Length + 1;

    for (int f = 0; f < n; f++)
    {
        for (int c = 0; c < n; c++)
        {
            if (terreno[f, c] == -1)
                Console.Write(".".PadLeft(ancho));
            else
                Console.Write(terreno[f, c].ToString().PadLeft(ancho));
        }
        Console.WriteLine();
    }
}