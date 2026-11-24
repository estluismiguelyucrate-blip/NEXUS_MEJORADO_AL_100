# Nexus Copy

Proyecto independiente de **NEXUS**, concebido como una aventura 2D de cazadores y mazmorras: exploración, combate, progresión de rango, personajes y jefes. Esta copia tiene su propio proyecto, menú y catálogo de mapas para evolucionar sin alterar la aplicación principal.

## Ejecutar

Requiere Windows, el SDK de .NET 10 con Windows Forms y una instancia de SQL Server LocalDB o SQL Server Express en ejecución. Clona el repositorio y ejecuta la restauración desde la carpeta recién creada (la misma que contiene `NexusCopy.sln`):

```powershell
git clone https://github.com/estluismiguelyucrate-blip/repooooo.git
cd repooooo
dotnet restore .\NexusCopy.sln
dotnet run --project .\NexusCopia.csproj
```

En Visual Studio usa **Archivo → Abrir → Proyecto o solución** y selecciona `NexusCopy.sln` en la carpeta que acabas de clonar. Espera a que termine la restauración y ejecuta el proyecto `NexusCopia` con la configuración `Release | Any CPU`. No abras una carpeta antigua desde Descargas: Visual Studio conserva la última ruta aunque el repositorio se haya clonado en otro lugar.

Si Visual Studio muestra `NETSDK1004` o no encuentra `NexusCopia.exe`, abre una terminal en la carpeta que contiene `NexusCopy.sln` y ejecuta:

```powershell
dotnet restore .\NexusCopy.sln
dotnet build .\NexusCopy.sln --configuration Release
dotnet run --project .\NexusCopia.csproj --configuration Release
```

Para generar una carpeta ejecutable de entrega (requiere el runtime de escritorio .NET 10 en el equipo destino):

```powershell
dotnet publish .\NexusCopia.csproj --configuration Release --output .\artifacts\NexusCopy
```

Después inicia `artifacts\NexusCopy\NexusCopia.exe`.


Controles: `A/D` o flechas para moverse, `Espacio/W` para saltar y `J/X` para atacar.

## Sistemas jugables

- Menú principal con entrada a partida, selector de mapa y controles.
- Tres cazadores originales seleccionables: vanguardia, arcanista y asesino, cada uno con alcance, daño y aura visual propios.
- Tres mazmorras jugables con trazados, paletas, enemigos y jefes distintos.
- Combate, fragmentos coleccionables, puntos de control, vidas, barra de jefe y extracción tras derrotarlo.
- Progresión persistente de rango E a S, experiencia, créditos, récord y mazmorras completadas.
- Música original sintetizada en ejecución para menú y combate; tecla `M` para activarla o silenciarla.
- El cazador del menú tiene una animación ambiental continua con balanceo, aura violeta, partículas y brillo de espada.
- Recursos de arte organizados por personaje, fondo, tileset, jefe, interfaz y audio.

La propuesta toma como inspiración general la fantasía de cazadores y portales, con universo, nombres, personajes y arte originales.

## Música y recursos

El juego sintetiza una banda sonora original de fantasía con energía de anime: cuerdas y arpegios ambientales en el menú, y percusión y capas más intensas en combate. No incorpora la canción oficial del anime ni grabaciones comerciales. El audio se genera localmente al iniciar y no requiere archivos de música externos.

## Base de datos SQL Server

El progreso se guarda **exclusivamente en SQL Server**, en la base `NexusCopyDb` y tabla `dbo.HunterProgress`. SQL Server LocalDB es la conexión predeterminada. Al iniciar, el juego crea la base, la tabla y el perfil si aún no existen. No usa archivos JSON ni otro almacenamiento para el progreso. Si SQL Server no está instalado, iniciado o accesible, el juego muestra un error y no inicia una partida que no pueda guardar.

La aplicación ya fue abierta correctamente contra SQL Server LocalDB después de corregir la consulta de creación de base. Si vas a compilar desde Visual Studio, asegúrate de restaurar los paquetes NuGet y de tener iniciada la instancia `MSSQLLocalDB`.

Para una instancia de SQL Server distinta, define `NEXUS_SQLSERVER_CONNECTION` antes de iniciar el juego. Ejemplo para SQL Server Express:

```powershell
$env:NEXUS_SQLSERVER_CONNECTION = "Server=.\SQLEXPRESS;Initial Catalog=NexusCopyDb;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15"
dotnet run --project .\NexusCopia.csproj
```

El usuario de Windows debe tener permiso para crear la base la primera vez. También puedes ejecutar `database/create_database.sql` desde SQL Server Management Studio y conceder permisos sobre `NexusCopyDb`. La barra inferior y la ventana de configuración indican el estado de la conexión a SQL Server.
