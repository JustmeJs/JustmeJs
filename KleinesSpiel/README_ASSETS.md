# Kleines Spiel — assets y animaciones

## Abrir y jugar

1. Descarga y extrae el ZIP nuevo en una carpeta vacía, conservando las subcarpetas.
2. Importa `KleinesSpiel/project.godot` con **Godot 4.3 .NET** y **.NET SDK 8** instalados. La edición estándar de Godot no ejecuta C#.
3. Compila con Build del editor o `dotnet build` desde la carpeta del proyecto.
4. Pulsa **F5** y después **Spielen**.

WASD: movimiento. Flechas: disparo. Espacio: dash. Escape: pausa. Se mantienen los textos del juego en alemán.

## Animaciones integradas

Los ocho personajes (héroe, tres enemigos y cuatro variantes de jefe) usan ahora `AnimatedSprite2D` y fotogramas distintos, en lugar de un sprite fijo. Hay 64 poses dibujadas en el atlas nuevo.

| Elemento | Animación y activación |
| --- | --- |
| Héroe | Respiración en reposo, pasos al desplazarse, ataque al disparar, reacción al daño, dash con estela y pose de derrota. |
| Enemigos y jefes | Reposo, desplazamiento, ataque al disparar o al hacer contacto, reacción al daño y caída al morir. Los vigilantes también flotan. |
| Cinco mejoras | Flotación, oscilación, brillo y ascenso/desvanecimiento al recogerlas. Cada una conserva su icono. |
| Corazón | Latido y flotación; destello y desaparición al recogerlo. |
| Proyectiles | Pulso durante el vuelo, rastro y explosión de impacto o al agotarse su duración. |
| Puertas | Apertura/cierre gradual de la reja y destello, con inversión fluida si cambia el estado durante la transición. |
| Mazmorra | Antorchas con llamas, chispas y luz oscilante; el suelo y las paredes conservan su geometría fija. |
| Interfaz | Latido de corazones, sacudida al perder vida, héroe animado en el menú, entrada gradual de pantallas y botones que reaccionan al cursor o al foco del teclado. |

Las poses son frontales y se espejan horizontalmente; no hay juegos de fotogramas independientes para vistas de espalda. No se añadieron sonidos ni un nuevo ataque cuerpo a cuerpo para el jugador: la animación de ataque acompaña su disparo existente.

## Estados y comportamiento

`ActorAnimator` selecciona: **muerte > daño > dash > ataque > movimiento > reposo**. Los estados breves vuelven automáticamente al movimiento o al reposo. Los ataques repetidos reinician la preparación.

Al morir un enemigo, sus colisiones y comportamiento se desactivan inmediatamente. Su evento de derrota se emite una vez y la presentación se elimina a los 0,55 segundos. La animación final puede terminar cuando el juego muestra Victoria o Game Over y está pausado. Las animaciones de los personajes vivos se congelan al pausar.

La recogida aplica la mejora o curación una sola vez, desactiva la detección y termina su efecto visual antes de eliminar el objeto. Las estelas y efectos tienen una duración limitada. No quedan tweens de recogida apuntando a objetos destruidos.

## Archivos y edición

- `Assets/Sprites/characters_animated.png`: atlas transparente nuevo, generado con IA, con ocho poses para cada uno de los ocho personajes.
- `Resources/Animations/*_frames.tres`: recursos SpriteFrames editables en Godot. Ajusta sus velocidades o fotogramas desde el Inspector.
- `Assets/Sprites/animation_manifest.json`: regiones del atlas y secuencias utilizadas. Las regiones se ajustan a las filas del dibujo y usan márgenes para mantener un lienzo de 192 × 192.
- `Scripts/Common/ActorAnimator.cs`: transiciones y presentación del personaje. Los efectos no cambian la forma de sus colisiones.
- `Scripts/Effects/BurstEffect.cs`: destellos, partículas y estela del dash, con limpieza automática.
- `Scripts/Items/PickupMotion.cs`: animación de objetos y corazones.
- `Assets/Sprites/Tiles/door_transition.gdshader`: transición de puertas; cada puerta tiene su propio material.
- `Scripts/Rooms/RoomArt.cs`: paredes a partir de colisiones y antorchas animadas.
- `Scripts/UI/MenuHero.cs`, `MenuMotion.cs` y `HealthIcons.cs`: animación de la interfaz.

El atlas anterior `dungeon_atlas.png` sigue proporcionando iconos y proyectiles. Los gráficos originales aportados en el ZIP se conservan. No se atribuye una licencia nueva a esos archivos originales ni se usaron packs comerciales.

## Pruebas

- `dotnet build`: 0 errores y 0 advertencias con SDK 8.0.426.
- **180 comprobaciones de integración**, tanto sin ventana como con renderizado OpenGL en Godot 4.3 .NET.
- Se verifican transiciones y avance de fotogramas de todos los personajes, disparos reales de enemigos/jefes, contacto cuerpo a cuerpo, pausa, muerte sin combate residual, recogida única, limpieza de efectos de recogida, vuelo y expiración de proyectiles, transición y reversión de puertas y foco del menú.
- Inspección de una grabación renderizada por Godot. `Documentation/animations_preview.gif` muestra una galería de animaciones en poses controladas; no representa una partida ni un encuentro aleatorio.
- El ZIP incluye las entradas de carpetas necesarias para el importador de Godot. Se comprueba su extracción y se excluyen cachés `.godot`, metadatos `.import` regenerables y compilados.

Para ejecutar la prueba desde esta carpeta con el ejecutable de Godot .NET en PATH:

```sh
dotnet build
godot --headless --editor --path . --quit
godot --headless --fixed-fps 60 --path . --script res://Tests/AnimationSmoke.gd
```

No se verificó una partida completa hasta derrotar al jefe.
