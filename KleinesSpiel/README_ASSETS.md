# Kleines Spiel — juego, assets y validación

## Abrir y jugar

1. Extrae el ZIP actualizado en una carpeta vacía.
2. Importa `KleinesSpiel/project.godot` con **Godot 4.3 .NET** y **.NET SDK 8**. La edición estándar no ejecuta C#.
3. Compila con Build del editor o `dotnet build` desde esta carpeta.
4. Pulsa **F5** y elige **Play**.

WASD: movimiento. Flechas: disparo. Espacio: dash. Escape: pausa.

En todos los menús puedes seleccionar con cualquiera de las cuatro flechas, confirmar con Enter/Espacio o usar el ratón. La primera opción recibe el foco al abrirlos y la navegación vuelve al inicio al llegar al final. Espacio al confirmar no activa el dash al regresar al juego.

El juego empieza en inglés. El botón de idioma del menú principal alterna inglés y alemán y actualiza el texto inmediatamente. La preferencia se guarda en `user://settings.cfg`, fuera del proyecto, y se conserva al reiniciar. Los menús, HUD, instrucciones, nombres de niveles, portales, jefes y objetos se muestran en el idioma elegido. El título propio sigue siendo «Kleines Spiel».

![Menú en inglés](Documentation/menu_english.png)

[Menú en alemán](Documentation/menu_german.png) · [Selección por teclado durante la pausa](Documentation/pause_keyboard.png)

## Seis niveles y dos entornos

| Niveles | Entorno | Final del nivel |
| --- | --- | --- |
| 1–3 | Ash Crypt / Aschekrypta | Jefe y portal garantizado. El portal del nivel 3 lleva al castillo. |
| 4–5 | Castle / Schloss | Jefe más resistente y portal garantizado. |
| 6 | Castle / Schloss | The Castle Regent / Der Schlossregent; su derrota termina en victoria. |

Cada nivel genera un árbol conectado de 12 habitaciones en una cuadrícula de 5 × 5: una entrada, nueve salas de combate, un tesoro y una sala de jefe. Las conexiones pueden ir arriba, abajo, izquierda y derecha. El jefe ocupa uno de los extremos más alejados de la entrada. Las salas de combate distribuyen seis variantes de obstáculos; aparecen todas en cada nivel. También varían la mezcla y posición de los enemigos.

Las puertas bloquean el paso durante el combate y se abren al eliminar a todos los enemigos. Las habitaciones se conservan durante la exploración del mismo nivel: volver no regenera enemigos ni tesoros recogidos, y mantiene los corazones que hayan quedado. El minimapa revela las habitaciones descubiertas. Al pasar por un portal se conservan vida, jugador y mejoras; el mapa y las salas se sustituyen por los del siguiente nivel.

Los niveles 1–3 mantienen la cripta. En el cuarto cambia el entorno al castillo, con mármol, alfombras, estandartes y ventanas; este aspecto se conserva en los niveles 4–6. La resistencia y cantidad de enemigos aumentan gradualmente. Su velocidad normal sigue por debajo de la velocidad inicial del jugador; las embestidas del jefe final son ataques breves y avisados.

El autoload `DungeonManager` expone `SeedOverride`: 0 elige una semilla aleatoria. La semilla determina los mapas, sus contenidos y los objetos. Los corazones soltados mantienen su tirada independiente. Los portales ya no se sortean: aparecen siempre en los niveles 1–5. El sexto termina con la pantalla de victoria.

![Cripta](Documentation/dungeon_rooms.png)

![Castillo](Documentation/castle_rooms.png)

[Portal de la cripta al castillo](Documentation/zone_portal.png) · [Vista previa de la progresión](Documentation/roguelike_preview.gif)

## Un pasivo por nivel

La partida baraja seis mejoras y reparte exactamente una por nivel, sin repetición. Cada una tiene un icono distinto que representa su efecto. El objeto mostrado, el texto traducido y la mejora proceden del mismo recurso `ItemData`.

| Icono / objeto | Efecto |
| --- | --- |
| Heart Medallion / Herzmedaillon | +1 corazón máximo completo y curación de esa cantidad. |
| Winged Boot / Geflügelter Stiefel | +15 % de velocidad de movimiento. |
| Spell Hourglass / Sanduhr des Zaubers | −20 % de espera entre disparos. |
| Ruby Blade / Rubinklinge | +25 % de daño de los proyectiles. |
| Piercing Tip / Durchdringende Spitze | Los proyectiles atraviesan un enemigo adicional. |
| Arcane Lens / Arkane Linse | +25 % de velocidad de los proyectiles. |

![Iconos y efectos](Documentation/passive_items.png)

La hoja y la punta representan mejoras pasivas; el mago sigue llevando su vara. La lente nueva usa un SVG propio, con un rayo y trazos de velocidad, y se presenta al mismo tamaño que los demás objetos.

## Contacto cuerpo a cuerpo y jefe final

Tras tocar al jugador, el enemigo retrocede durante 0,22 s y se recupera durante 0,28 s antes de perseguirlo de nuevo. El retroceso usa colisiones reales y busca una dirección libre cuando hay una pared detrás. El jugador recibe empuje solo si el contacto ha causado daño: la invulnerabilidad por daño o dash impide nuevos empujes. El comportamiento también se aplica a jefes que golpean por contacto.

El Regente tiene 140 puntos de vida, una corona y un aura propias sobre su sprite animado. Alterna tres ataques:

- Abanicos de proyectiles dirigidos hacia la posición anunciada del jugador.
- Anillos de proyectiles con una abertura de 72 grados, marcada durante el aviso.
- Una embestida con dirección fijada durante el aviso, que termina al chocar o agotar su recorrido.

A la mitad de vida entra en una segunda fase: más proyectiles y ráfagas, avisos algo más cortos y una embestida más rápida. Todos los ataques tienen preparación y recuperación. La pausa congela ataques y proyectiles. Al morir, el jefe detiene sus ataques y elimina sus proyectiles restantes.

![Regente y aviso de ataque](Documentation/final_boss.png)

[Victoria tras el sexto nivel](Documentation/victory.png)

## Animaciones y archivos

El mago tiene poses propias en cuatro direcciones. La animación del lanzamiento sigue la dirección del disparo aunque camines hacia otro lado; proyectiles y destellos salen de la punta de la vara usando los cuatro valores `CastOffset`. Los enemigos usan sus secuencias de reposo, movimiento, ataque, daño y muerte con volteo horizontal.

La prioridad de estados es muerte, daño, dash, ataque, movimiento y reposo. Al morir un enemigo se desactiva su combate inmediatamente; la presentación permanece brevemente para terminar la animación. Los objetos aplican el efecto una sola vez y desaparecen con una animación. Proyectiles, puertas, portales, antorchas e interfaz tienen efectos animados.

[Disparos del mago](Documentation/mage_casting_preview.gif) · [Galería de animaciones](Documentation/animations_preview.gif)

- `Scripts/Common/Localization.cs`: elección persistente del idioma y textos bilingües.
- `Scripts/UI/MenuMotion.cs`: foco, vecinos de navegación y presentación de botones.
- `Scripts/Rooms/DungeonFloor.cs`: generación por semilla y elección de habitaciones/jefe.
- `DungeonManager.cs`: partida, caché de habitaciones y progresión de seis niveles.
- `Room.cs`, `RoomLayouts.cs`, `RoomArt.cs` y `ZonePortal.cs`: contenido, geometría, entornos y portales.
- `Scripts/Enemies/Enemy.cs`: combate y separación después del contacto.
- `Scripts/Enemies/CastleBoss.cs`: ataques, fases y presentación del jefe final.
- `Scripts/Items/ItemCatalog.cs` y `Resources/Items/`: seis mejoras, iconos y textos en ambos idiomas.
- `Assets/Sprites/hero_mage_directional.png` y `hero_mage_manifest.json`: atlas y regiones del mago.
- `Assets/Sprites/characters_animated.png` y `Resources/Animations/`: secuencias de enemigos y jefes.
- `Assets/Sprites/dungeon_atlas.png`: iconos y proyectiles activos; `arcane_lens.svg`: sexto icono.

Los gráficos originales de reemplazo, recursos estáticos sustituidos y escenas del recorrido fijo ya fueron eliminados. Los atlas compartidos permanecen completos porque contienen regiones activas. Las imágenes de `Documentation/` son referencias y no se cargan durante el juego. El atlas del mago fue generado con IA en una revisión anterior; no se usaron packs comerciales ni se atribuye una licencia nueva a los archivos originales.

## Comprobaciones

Desde esta carpeta, con Godot **.NET** y .NET SDK 8 en PATH:

```sh
dotnet build
godot --headless --editor --path . --quit
godot --headless --fixed-fps 60 --path . --script res://Tests/RoguelikeSmoke.gd
godot --headless --fixed-fps 60 --path . --script res://Tests/MenuLanguageSmoke.gd
godot --headless --fixed-fps 60 --path . --script res://Tests/MeleeContactSmoke.gd
godot --headless --fixed-fps 60 --path . --script res://Tests/FinalBossSmoke.gd
godot --headless --fixed-fps 60 --path . --script res://Tests/AnimationSmoke.gd
godot --headless --fixed-fps 60 --path . --script res://Tests/MageCastingSmoke.gd
```

Las pruebas comprueban 1.200 mapas, puertas físicas en las cuatro direcciones, recorrido de los seis niveles, un objeto distinto por nivel, transición de entorno en el cuarto, conservación de mejoras, portales garantizados, victoria y reinicio. Las pruebas de menú envían teclas reales y arrancan procesos nuevos de Godot para comprobar la persistencia. Las pruebas de combate comprueban separación física, paredes, invulnerabilidad y los tres ataques del jefe final, incluyendo contacto real durante su embestida.

Desde la raíz del repositorio, `python3 Tools/audit_assets.py` comprueba referencias, recursos visuales sin uso y seis iconos/efectos distintos. El paquete excluye `.godot`, metadatos `.import` regenerables y compilados; contiene entradas de carpetas para Godot. Antes de distribuirlo se valida extracción, compilación y pruebas desde una copia limpia.

Las pruebas de progresión y las vistas previas usan derrotas controladas y protección del jugador; no equivalen a una partida completa ganada sin asistencia.
