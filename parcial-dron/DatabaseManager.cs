using Npgsql;

namespace ParcialDron;

/// <summary>
/// Clase que gestiona toda la persistencia en PostgreSQL usando ADO.NET síncrono.
/// Sin ORMs, sin async/await, con transacciones, using blocks y parametrización.
/// </summary>
public class DatabaseManager
{
    private readonly string _connectionString;

    public DatabaseManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Guarda la cabecera y los movimientos en la base de datos dentro de una transacción.
    /// Aplica la ofuscación en nro_paso según las reglas de la Parte D.
    /// </summary>
    /// <returns>El ID generado por tb_master_control.</returns>
    public int GuardarSimulacion(
        int n,
        int coordX,
        int coordY,
        List<(int Paso, int Fila, int Columna)> secuencia)
    {
        int idMaster = 0;

        using (NpgsqlConnection conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();

            using (NpgsqlTransaction tx = conn.BeginTransaction())
            {
                try
                {
                    // -- PARTE D.1: Insertar cabecera y recuperar el ID con RETURNING --
                    string sqlMaster = @"
                        INSERT INTO tb_master_control (fecha, n, coord_x, coord_y)
                        VALUES (NOW(), @n, @coordX, @coordY)
                        RETURNING id";

                    using (NpgsqlCommand cmdMaster = new NpgsqlCommand(sqlMaster, conn, tx))
                    {
                        cmdMaster.Parameters.AddWithValue("@n",      n);
                        cmdMaster.Parameters.AddWithValue("@coordX", coordX);
                        cmdMaster.Parameters.AddWithValue("@coordY", coordY);

                        // ExecuteScalar retorna el id generado por RETURNING
                        object? resultado = cmdMaster.ExecuteScalar();
                        idMaster = Convert.ToInt32(resultado);
                    }

                    // -- PARTE D.2 y D.3: Insertar movimientos con while + ofuscación --
                    string sqlDetalle = @"
                        INSERT INTO tb_det_log (id_master, nro_paso, coord_x, coord_y)
                        VALUES (@idMaster, @nroPaso, @coordX, @coordY)";

                    int i = 0;                                          // índice manual
                    while (i < secuencia.Count)                        // condición manual
                    {
                        var mov = secuencia[i];

                        // REGLA DE OFUSCACIÓN (Parte D.3):
                        // PAR  -> guardar multiplicado por 2
                        // IMPAR -> guardar como número negativo
                        int pasoOfuscado;
                        if (mov.Paso % 2 == 0)
                            pasoOfuscado = mov.Paso * 2;
                        else
                            pasoOfuscado = -mov.Paso;

                        using (NpgsqlCommand cmdDet = new NpgsqlCommand(sqlDetalle, conn, tx))
                        {
                            cmdDet.Parameters.AddWithValue("@idMaster", idMaster);
                            cmdDet.Parameters.AddWithValue("@nroPaso",  pasoOfuscado);
                            cmdDet.Parameters.AddWithValue("@coordX",   mov.Fila);
                            cmdDet.Parameters.AddWithValue("@coordY",   mov.Columna);

                            cmdDet.ExecuteNonQuery();
                        }

                        i++;                                            // avance manual
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        return idMaster;
    }

    /// <summary>
    /// PARTE E.5: Lee los últimos 5 registros de tb_det_log para una simulación
    /// y aplica ingeniería inversa para recuperar el paso real.
    /// </summary>
    public void MostrarUltimos5(int idMaster)
    {
        string sql = @"
            SELECT id, nro_paso, coord_x, coord_y
            FROM tb_det_log
            WHERE id_master = @idMaster
            ORDER BY id DESC
            LIMIT 5";

        using (NpgsqlConnection conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();

            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@idMaster", idMaster);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    Console.WriteLine();
                    Console.WriteLine("=== REPORTE INVERSO: últimos 5 pasos (reconstruidos) ===");
                    Console.WriteLine($"{"ID Det",-8} {"Paso Real",-10} {"Fila",-6} {"Columna",-8}");
                    Console.WriteLine(new string('-', 36));

                    while (reader.Read())
                    {
                        int idDet     = reader.GetInt32(0);
                        int nroPasoOF = reader.GetInt32(1);
                        int coordX    = reader.GetInt32(2);
                        int coordY    = reader.GetInt32(3);

                        // INGENIERÍA INVERSA (Parte E.5):
                        // Negativo -> era IMPAR -> paso real = -nroPasoOfuscado
                        // >= 0     -> era PAR  -> paso real = nroPasoOfuscado / 2
                        int pasoReal;
                        if (nroPasoOF < 0)
                            pasoReal = -nroPasoOF;
                        else
                            pasoReal = nroPasoOF / 2;

                        Console.WriteLine($"{idDet,-8} {pasoReal,-10} {coordX,-6} {coordY,-8}");
                    }
                }
            }
        }
    }
}