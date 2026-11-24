# Clonar, ejecutar y revisar Nexus Copy

## Abrir el proyecto

1. Clona `https://github.com/estluismiguelyucrate-blip/repooooo.git` en una carpeta nueva.
2. Abre `NexusCopy.sln` o `NexusCopia.csproj` en Visual Studio.
3. Restaura los paquetes NuGet y ejecuta `NexusCopia` en Windows con .NET 10.
4. Para editar el menú visualmente, abre `src/UI/MainMenuForm.cs` y selecciona **Ver diseñador**.

La aplicación requiere SQL Server LocalDB o una instancia accesible. LocalDB es la conexión predeterminada; para otro servidor configura `NEXUS_SQLSERVER_CONNECTION`. La primera conexión crea la base y las tablas si la cuenta tiene permisos.

## Compilar y publicar

Desde la carpeta que contiene `NexusCopy.sln`:

```powershell
dotnet restore .\NexusCopy.sln
dotnet build .\NexusCopy.sln --configuration Release --no-restore
dotnet publish .\NexusCopia.csproj --configuration Release --no-restore --output .\artifacts\NexusCopy
```

## GitHub Actions

`.github/workflows/main.yml` compila en Windows para cada push o pull request hacia `main`. También permite una ejecución manual en la pestaña **Actions**, opción **Nexus Copy CI → Run workflow**. El resultado se publica como artefacto descargable `NexusCopy-Windows` durante 14 días.

Si la pestaña **Actions** no muestra el workflow después de subirlo a `main`, verifica que GitHub Actions esté habilitado en **Settings → Actions → General** y recarga la pestaña. El workflow solo solicita `contents: read`; no necesita secretos ni permisos de escritura.
