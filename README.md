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


Controles: `A/D` o flechas para moverse, `Espacio/W` para saltar, `J/X` para la habilidad y `F5` para guardar la partida en SQL. `Esc` guarda y vuelve al menú. En el menú, `V` cambia Recluta, Cazador y Monarca; el botón **Continuar partida · SQL** reanuda la última expedición guardada.

## Sistemas jugables

- Menú principal con entrada a partida, selector de mapa y controles.
- Tres cazadores originales seleccionables con ilustraciones propias y habilidades con nombre, alcance, daño y efectos diferentes: Eclipse Gemelo, Lanza Solar y Ráfaga Esmeralda.
- Tres mazmorras jugables con los fondos ilustrados de cada mundo: caverna del Umbral, bosque mutado y bastión en ruinas. Las rutas llegan a 3.900 unidades, incluyen nuevos saltos y checkpoints.
- Un jefe ilustrado y dos ataques telegráficos propios por mazmorra; los centinelas patrullan, disparan, resisten varios golpes y se pueden esquivar.
- Trampas de púas con aviso visual, ciclo de activación, variaciones de dificultad y paso directo a la siguiente mazmorra después de cada jefe.
- Dificultades con vidas y resistencia/ritmo de ataque diferentes. La expedición mantiene las vidas y el puntaje entre mundos.
- Menú inicial conservando el fondo artístico original, con botones nativos de Windows Forms ubicados sobre los controles del diseño y editables en el Designer (`MainMenuForm.Designer.cs`).
- Guardado y reanudación de la partida desde SQL Server: posición, checkpoint, puntaje, vidas, dificultad, jefe, fragmentos, centinelas y ciclo de trampas. Los checkpoints y las victorias también activan el guardado automático.
- Habilidades con proyectiles reales: la Lanza Solar de Ariadna, la Ráfaga Esmeralda triple de Kael y el combo cuerpo a cuerpo Eclipse Gemelo de Soren.
- Combate, fragmentos coleccionables, puntos de control, vidas, barra de jefe y extracción tras derrotarlo.
- Progresión persistente de rango E a S, experiencia, créditos, récord y mazmorras completadas.
- Música original sintetizada en ejecución para menú y combate; tecla `M` para activarla o silenciarla.
- El menú mantiene el arte de fondo original; los botones de juego, selección, configuración y controles son controles reales de Windows Forms editables en el diseñador.
- Recursos de arte organizados por personaje, fondo, tileset, jefe, interfaz y audio.

La propuesta toma como inspiración general la fantasía de cazadores y portales, con universo, nombres, personajes y arte originales.

## Música y recursos

El juego sintetiza una banda sonora original de fantasía con energía de anime: cuerdas y arpegios ambientales en el menú, y percusión y capas más intensas en combate. No incorpora la canción oficial del anime ni grabaciones comerciales. El audio se genera localmente desde C# al iniciar y no requiere archivos de música externos.

## Base de datos SQL Server

El progreso y las partidas se guardan **exclusivamente en SQL Server**, en la base `NexusCopyDb`. El perfil usa `dbo.HunterProgress`; las partidas usan `dbo.ExpeditionSave` y sus tablas relacionadas para fragmentos, centinelas y trampas. SQL Server LocalDB es la conexión predeterminada. Al iniciar, el juego crea las tablas necesarias si aún no existen. No usa archivos JSON ni otro almacenamiento para el progreso o las partidas. Si SQL Server no está instalado, iniciado o accesible, el juego muestra un error y no inicia una partida que no pueda guardar.

La aplicación y su esquema SQL se implementan en C#. Al iniciarla, crea la base y sus tablas si la cuenta tiene permisos. Si vas a compilar desde Visual Studio, asegúrate de restaurar los paquetes NuGet y de tener iniciada la instancia `MSSQLLocalDB`.

Para una instancia de SQL Server distinta, define `NEXUS_SQLSERVER_CONNECTION` antes de iniciar el juego. Ejemplo para SQL Server Express:

```powershell
$env:NEXUS_SQLSERVER_CONNECTION = "Server=.\SQLEXPRESS;Initial Catalog=NexusCopyDb;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15"
dotnet run --project .\NexusCopia.csproj
```

El usuario de Windows debe tener permiso para crear la base la primera vez. La configuración y los guardados se ejecutan desde el cliente C# de SQL Server; el proyecto no necesita scripts Python ni SQL externos para iniciar.
