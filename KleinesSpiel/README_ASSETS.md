# Kleines Spiel — assets integrados

## Abrir y jugar

1. Extrae el ZIP completo conservando las carpetas.
2. Importa `project.godot` con **Godot 4.3 .NET** y **.NET SDK 8** instalados. La edición estándar de Godot no ejecuta C#.
3. Compila el proyecto con el botón Build del editor o `dotnet build` desde esta carpeta.
4. Pulsa F6 en MainMenu.tscn o F5 para iniciar el juego.

Controles originales: WASD para moverse, flechas para disparar, espacio para dash y Escape para pausa. Se conservan los textos del juego en alemán.

## Qué se añadió

- Atlas PNG transparente con 16 sprites: héroe, tres enemigos, cuatro variantes de jefe, corazón, cinco mejoras y dos proyectiles.
- Suelo y paredes de piedra, puertas abiertas/cerradas, icono del proyecto y tema del menú.
- Sprites conectados a las escenas existentes: no hace falta arrastrarlos manualmente.
- `ItemData.Icon` y asignación del icono correspondiente al generar cada mejora.
- Corazones del HUD con estados lleno, medio y vacío, además del contador original.
- Paredes dibujadas siguiendo las colisiones existentes y puertas orientadas según la escena.
- Encuadre fijo de las salas: se desactivó la cámara que seguía al jugador y recortaba la sala. No se cambiaron las colisiones, el daño ni las reglas de combate.

Son sprites estáticos; no se añadieron animaciones de caminar ni sonido. Los tres tipos de enemigo corresponden a los tres comportamientos ya implementados. No se añadió un cuarto comportamiento.

## Archivos y edición

- `Assets/Sprites/dungeon_atlas.png`: atlas generado para este proyecto con IA, fondo transparente. No se descargaron packs comerciales.
- `Resources/Art/*.tres`: regiones AtlasTexture independientes. Se usan celdas de 313.5 × 313.5 porque la imagen mide 1254 × 1254; `filter_clip` evita sangrado entre regiones.
- `Assets/Sprites/Tiles/*.svg`: gráficos originales de piedra y puertas, con geometría sobre píxeles enteros.
- `Resources/Art/dungeon_theme.tres`: colores y estados del botón del menú.
- `Scripts/Rooms/RoomArt.cs`: presentación de las paredes a partir de las colisiones.
- `Scripts/UI/HealthIcons.cs`: presentación de los corazones.
- `Assets/Sprites/atlas_manifest.json`: nombres, regiones y recursos del atlas.

Para sustituir un sprite, edita su `.tres` o cambia `Visual/Sprite.texture` en la escena. Los gráficos originales del ZIP se conservan en sus rutas, aunque las escenas usan el nuevo atlas. No se atribuye una licencia nueva a los archivos originales aportados por el usuario.

## Validación realizada

- `dotnet build`: 0 errores y 0 advertencias con SDK 8.0.426.
- Importación y ejecución con Godot 4.3 .NET.
- 62 comprobaciones de integración: inicialización C#, texturas de enemigos y proyectiles, cinco iconos diferentes y su asignación al crear las mejoras, suelo/paredes de ocho salas, cambios de textura de puertas y capturas renderizadas.
- Inspección visual del menú, sala inicial y composición de sprites.

No se verificó una partida completa hasta derrotar al jefe. `Documentation/assets_preview.png` es una composición estática de los assets en la sala inicial, no una captura de un encuentro generado al azar.

El ZIP excluye cachés `.godot`, compilados, herramientas de validación locales y archivos personales. Godot reimportará los gráficos al abrirlo.
