# Nexus Copy

Proyecto independiente de **NEXUS**, concebido como una aventura 2D de cazadores y mazmorras: exploración, combate, progresión de rango, personajes y jefes. Esta copia tiene su propio proyecto, menú y catálogo de mapas para evolucionar sin alterar la aplicación principal.

## Ejecutar

Requiere Windows y el SDK de .NET 10 con Windows Forms. Al clonar, primero restaura los recursos de compilación, porque `obj/` no se guarda en Git:

```powershell
dotnet restore .\NexusCopy.sln
dotnet run --project .\NexusCopia.csproj
```

En Visual Studio, abre `NexusCopy.sln`, espera a que termine la restauración de NuGet y luego inicia `NexusCopia`. Si sale `NETSDK1004` indicando que falta `obj\project.assets.json`, ejecuta el comando `dotnet restore` anterior en la terminal ubicada en la carpeta del proyecto.

Controles: `A/D` o flechas para moverse, `Espacio/W` para saltar y `J/X` para atacar.

## Sistemas jugables

- Menú principal con entrada a partida, selector de mapa y controles.
- Tres cazadores originales seleccionables: vanguardia, arcanista y asesino, cada uno con alcance, daño y aura visual propios.
- Tres mazmorras jugables con trazados, paletas, enemigos y jefes distintos.
- Combate, fragmentos coleccionables, puntos de control, vidas, barra de jefe y extracción tras derrotarlo.
- Progresión persistente de rango E a S, experiencia, créditos, récord y mazmorras completadas.
- Música original sintetizada en ejecución para menú y combate; tecla `M` para activarla o silenciarla.
- Recursos de arte organizados por personaje, fondo, tileset, jefe, interfaz y audio.

La propuesta toma como inspiración general la fantasía de cazadores y portales, con universo, nombres, personajes y arte originales. Los datos de progreso se guardan en `%LOCALAPPDATA%/NexusCopy/hunter-save.json`.

## Música y recursos

El juego sintetiza música original y no incorpora canciones comerciales.

