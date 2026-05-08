# NavSnap Web

## Menjalankan Aplikasi (Bind ke 0.0.0.0)

PowerShell:

```powershell
$env:NAVSNAP_POSTGRES="Host=YOUR_HOST;Port=5432;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASS;Search Path=navsnap,public"
$env:ASPNETCORE_URLS="http://0.0.0.0:5164"
cd .\NavSnap
dotnet run
```

## Publish ke Folder

```powershell
cd .\NavSnap
dotnet publish -c Release -o "D:\Publish\Navsnap"
```

## Catatan

- Kredensial database tidak disimpan di repository.
- Gunakan environment variable `NAVSNAP_POSTGRES` untuk koneksi database.
