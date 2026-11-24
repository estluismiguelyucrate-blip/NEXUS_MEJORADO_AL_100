# Nexus Copy

Proyecto independiente de **NEXUS**, concebido como una aventura 2D de cazadores y mazmorras: exploración, combate, progresión de rango, personajes y jefes. Esta copia tiene su propio proyecto, menú y catálogo de mapas para evolucionar sin alterar la aplicación principal.

## Ejecutar

Requiere Windows y .NET 10 SDK con Windows Forms:

```powershell
dotnet run --project "NexusCopia.csproj"
```

Controles: `A/D` o flechas para moverse, `Espacio/W` para saltar y `J/X` para atacar.

## Estado del prototipo

- Menú principal con entrada a partida, selector de mapa y controles.
- Nivel jugable con movimiento, salto, ataque, enemigos, fragmentos y checkpoint.
- Jefe de nivel con barra de integridad, contacto dañino, combate cuerpo a cuerpo y puerta de salida bloqueada hasta vencerlo.
- Catálogo preparado para Ruinas del Umbral, Bosque Carmesí y Ciudad Sumergida.
- Carpetas de arte y datos listas para fondos, tilesets, animaciones, personajes y jefes.

El primer prototipo reutiliza el modo arcade existente y añade el menú y jefe en este proyecto. El sistema de rangos, clases, cazadores seleccionables, mazmorras con rutas, inventario persistente, progresión entre misiones y banda sonora aún son trabajo pendiente. La inspiración de fantasía de cazadores se mantendrá en un universo, nombres, personajes y arte originales.

## Música y recursos

No se incluye música de terceros. Para añadir audio, usar composiciones propias o pistas con licencia compatible, guardar el archivo de licencia y atribución junto al recurso y registrar su origen en `assets/audio/README.md`.

