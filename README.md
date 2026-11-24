# Nexus Copy

Proyecto independiente de **NEXUS**, concebido como una aventura 2D de cazadores y mazmorras: exploración, combate, progresión de rango, personajes y jefes. Esta copia tiene su propio proyecto, menú y catálogo de mapas para evolucionar sin alterar la aplicación principal.

## Ejecutar

Requiere Windows y el SDK de .NET 10 con Windows Forms. Al clonar, abre la solución del repositorio y restaura los recursos de compilación, porque `obj/` no se guarda en Git:

```powershell
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

## Entrega para Windows

La compilación final probada está disponible en [NexusCopy-Final-Windows.zip](https://github.com/estluismiguelyucrate-blip/repooooo/raw/refs/heads/main/NexusCopy-Final-Windows.zip). Descomprime el archivo y ejecuta `NexusCopia.exe`. El equipo necesita el runtime de escritorio de .NET 10.

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
