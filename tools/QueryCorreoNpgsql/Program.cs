using Npgsql;
await using var conn = new NpgsqlConnection("Host=localhost;Port=5432;Database=tienda_virtual_db;Username=postgres;Password=75818549;SSL Mode=Disable");
await conn.OpenAsync();
await using var cmd = new NpgsqlCommand("SELECT correo_id, remitente, correo_electronico, puerto, CASE WHEN contrasenia IS NOT NULL AND length(contrasenia)>0 THEN '********' ELSE '(vacío)' END AS contrasenia, servidor_smtp FROM xqm_configuracion.correo ORDER BY correo_id", conn);
await using var r = await cmd.ExecuteReaderAsync();
while (await r.ReadAsync()) {
  Console.WriteLine($"correo_id={r.GetInt32(0)} | remitente={r.GetString(1)} | correo_electronico={r.GetString(2)} | puerto={r.GetInt16(3)} | contrasenia={r.GetString(4)} | servidor_smtp={r.GetString(5)}");
}
