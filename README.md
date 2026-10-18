# Kleines Spiel

## Actualización de combate, sonido y exploración

- **Una tienda garantizada por nivel**, marcada desde el inicio con una moneda verde en el minimapa. Dos corazones a 3 monedas y un pasivo a 15; compra con E.
- Los arqueros disparan **flechas orientadas**. El castillo usa arqueros humanos, caballeros, hechiceros y guardianes acorazados; la cripta conserva sus criaturas. Los invocadores y zombis llevan hábitos de culto distintos en cada zona, con movimiento, ataque, daño y derrota animados.
- El **invocador no ataca ni hace daño por contacto**. Cada cinco segundos intenta crear un zombi más rápido que el melee habitual, en una posición libre y alejada del jugador. Hay un máximo de cuatro zombis vivos por sala; no dan monedas ni corazones y deben derrotarse para abrir las puertas.
- El jefe melee se detiene, tiembla y marca una línea antes de embestir hacia la posición fijada del jugador. El Castle Warden alterna cruz y X; cada sexta ráfaga forma un anillo con un hueco para escapar.
- El Regent reduce los avisos de abanico/embestida a 0,40/0,45 segundos (0,33/0,38 en fase dos), aumenta el abanico y el tamaño y alcance de la embestida. Su anillo tiene una animación de 0,18 segundos, sin marcas de aviso sobre el suelo, y conserva un hueco para esquivarlo.
- Corazones de suelo y tienda más pequeños. Curación aleatoria: **5 %** por enemigo normal y **25 %** por jefe intermedio; los invocados y el jefe final no sueltan corazones.
- Los portales colocan al jugador **en el centro** y cancelan cualquier dash o retroceso pendiente. Se mantienen los movimientos diagonales normalizados, también al disparar y deslizarse junto a paredes.
- La vida se muestra con corazones, sin el contador numérico duplicado. Se retira la instrucción de buscar el tesoro como objetivo principal.
- Mantén **Tab** para ver el tiempo de la partida, incluso en pausa. Conserva el tiempo entre niveles, se detiene en pausa/derrota/victoria y se reinicia al empezar una nueva partida.
- El castillo tiene seis diseños de suelo y decoración: pasillo con alfombra cruzada, galería circular, biblioteca, alfombras gemelas, sala de banquetes y mosaico ceremonial. Los obstáculos dibujados coinciden con sus colisiones.

### Música y efectos

Nueve pistas instrumentales originales: exploración de cripta y castillo, tres encuentros de jefe en la cripta y cuatro en el castillo, incluido el Regent. La música se repite sin reiniciarse al cambiar entre salas normales; cambia al entrar en combate con un jefe y vuelve al tema de exploración al derrotarlo. Se pausa con el juego. Nueve efectos breves cubren vara, arco, magia enemiga, impactos, golpes, daño, derrotas y carga. La muerte del jugador tiene un efecto discreto de 0,6 segundos.

No se utilizaron grabaciones, melodías ni samples de terceros. Los archivos generados se ofrecen bajo CC0 1.0; la licencia y procedencia están en [Assets/Audio/LICENSE.md](KleinesSpiel/Assets/Audio/LICENSE.md). El generador reproducible es `Tools/generate_audio.py` en la raíz del repositorio (necesita Python, NumPy y FFmpeg; no son necesarios para jugar).

[Demostración del juego con audio](KleinesSpiel/Documentation/gameplay_with_audio.mp4) · [Diseños del castillo](KleinesSpiel/Documentation/castle_design_0.png) · [Invocador y zombi](KleinesSpiel/Documentation/crypt_summoner.png)


Roguelike 2D en Godot y C#: un mago con vara explora una cripta, asciende al castillo y se enfrenta al Regente.

## Descargar y jugar

**[Descargar KleinesSpiel_con_assets.zip](https://github.com/JustmeJs/JustmeJs/raw/refs/heads/main/KleinesSpiel_con_assets.zip)**

Extrae el ZIP en una carpeta nueva e importa `KleinesSpiel/project.godot` con **Godot 4.3 .NET** y **.NET SDK 8**. Compila, pulsa **F5** y selecciona **Play**.

- Juego: WASD para moverse, flechas para disparar, espacio para dash y Escape para pausar y **E para comprar** cerca de un expositor.
- Todos los menús: flechas para seleccionar y **Enter o Espacio** para confirmar; también admiten ratón.
- Idioma: inglés predeterminado. El botón **Language: English / Sprache: Deutsch** del menú principal cambia el idioma y guarda la elección entre sesiones.

![Menú con selección por teclado e idioma](KleinesSpiel/Documentation/menu_english.png)

## De la cripta al castillo

La partida tiene **seis niveles**: tres de cripta y tres de castillo. Cada nivel genera **12 habitaciones** conectadas en las cuatro direcciones, con bifurcaciones, seis distribuciones de obstáculos y enemigos variados. El minimapa revela las habitaciones descubiertas; las salas limpiadas y los objetos recogidos conservan su estado al regresar.

Hay **un objeto pasivo gratuito de tesoro por nivel**, elegido sin repetir entre las seis mejoras, y **una tienda** que vende dos corazones y un pasivo adicional. Después de cada uno de los cinco primeros jefes aparece siempre un portal al siguiente nivel. El tercer portal conduce al castillo, con suelo de mármol, alfombras, estandartes y ventanas. Mantienes vida y mejoras al avanzar.

En el sexto nivel espera **The Castle Regent**, un jefe de 140 puntos de vida con abanicos de proyectiles, anillos con una abertura para esquivar y embestidas que avisan de su dirección. Por debajo de la mitad de vida intensifica sus ataques. Derrotarlo completa la partida.

![Recorrido por los seis niveles y jefe final](KleinesSpiel/Documentation/roguelike_preview.gif)

La vista previa usa encuentros, derrotas y monedas de demostración controladas para mostrar la progresión y las compras.

## Pantalla completa

El juego arranca a pantalla completa y se adapta a la resolución del monitor, incluido Full HD (1920 × 1080). Conserva el diseño 16:9 de las salas y los menús; en monitores con otra proporción aparecen bandas para evitar deformaciones o recortes. La ventana, si se fuerza desde el editor, tiene un tamaño previsto de 1920 × 1080.

## Tiendas, monedas y dificultad

Los enemigos normales tienen un **35 %** de probabilidad de soltar una moneda; los cinco jefes intermedios dan **3 monedas**. Cada tienda vende dos corazones completos a **3 monedas cada uno** y una mejora pasiva a **15**. Acércate y pulsa **E**: los precios y el efecto aparecen antes de comprar. No se cobra curación si ya tienes toda la vida. El dinero y las compras se conservan al volver a una sala; el monedero llega hasta 99 y se reinicia con la partida.

![Tienda y monedas](KleinesSpiel/Documentation/shop.png)

Los enemigos **no disparan durante los primeros 1,5 segundos** de entrar en una habitación. El primer nivel reduce su velocidad de movimiento y de proyectil y espacia los disparos; cada nivel aumenta la dificultad. Los melee calculan rutas alrededor de los obstáculos y las actualizan al moverse el jugador.

[Equilibrio económico, ratios y referencias consultadas](KleinesSpiel/Documentation/ECONOMY.md)

## Objetos y combate

![Seis mejoras pasivas](KleinesSpiel/Documentation/passive_items.png)

Los enemigos cuerpo a cuerpo retroceden y se recuperan brevemente después del contacto, dejando espacio para escapar. Los golpes bloqueados por invulnerabilidad no vuelven a empujar al jugador. La embestida del jefe final termina cuando choca.

El mago apunta la vara en la dirección del disparo aunque camines hacia otro lado. Personajes, objetos, proyectiles, puertas y menús tienen animaciones.

[Disparos en cuatro direcciones](KleinesSpiel/Documentation/mage_casting_preview.gif) · [Galería de animaciones](KleinesSpiel/Documentation/animations_preview.gif)

## Desarrollo y comprobaciones

[Documentación de implementación y pruebas](KleinesSpiel/README_ASSETS.md)

Las pruebas automatizadas cubren 1.200 mapas generados, un recorrido de los seis niveles, navegación real con teclado, ambos idiomas y su persistencia, separación de enemigos melee, los tres ataques del jefe final y las animaciones existentes. También cubren los 1,5 segundos de preparación, rutas alrededor de pilares, compras, persistencia del dinero y una muestra de 2.151 enemigos para comprobar el reparto de monedas.

Desde la raíz del repositorio:

```sh
python3 Tools/audit_assets.py
python3 Tools/package_project.py
```

El empaquetador incluye los archivos del proyecto registrados en el índice de Git y crea las entradas explícitas de carpetas que necesita el importador de Godot. Excluye cachés y compilados. El ZIP es el proyecto editable y requiere Godot .NET para ejecutarlo.
