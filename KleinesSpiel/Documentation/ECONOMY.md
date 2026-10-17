# Economía y equilibrio

El objetivo es que la moneda sirva para decidir entre curarse ahora o ahorrar para una mejora permanente. Se mantiene un tesoro gratuito por nivel; la tienda añade una elección de gasto, no sustituye ese tesoro.

## Referencias y límites de la consulta

La referencia solicitada es la estructura de tiendas y moneda de The Binding of Isaac. Se intentó consultar las siguientes páginas el 17 de octubre de 2026:

- [Isaac: Shop](https://bindingofisaacrebirth.wiki.gg/wiki/Shop)
- [Isaac: Coins](https://bindingofisaacrebirth.wiki.gg/wiki/Coins)
- [Isaac: Room Clear Awards](https://bindingofisaacrebirth.wiki.gg/wiki/Room_Clear_Awards)
- [Enter the Gungeon: Pickups](https://enterthegungeon.fandom.com/wiki/Pickups)

El acceso a estas páginas fue bloqueado por la red del entorno con HTTP 403, también al intentar fuentes alternativas. Por tanto, **no se presenta ningún porcentaje externo como verificado**. Los precios y probabilidades que siguen son decisiones propias para este juego; el 35 % no se atribuye a Isaac ni a Gungeon.

## Valores implementados

| Concepto | Valor |
| --- | --- |
| Enemigo normal | 35 % de probabilidad de soltar 1 moneda. |
| Jefe de los niveles 1–5 | 3 monedas garantizadas en una pila. |
| Jefe final | Termina la partida; no ofrece moneda que ya no se pueda gastar. |
| Monedero | Empieza en 0, conserva saldo entre niveles, máximo 99. |
| Tiendas | Una por nivel; dos corazones y un pasivo. |
| Corazón completo | 3 monedas; cura hasta 2 medios corazones, sin aumentar la vida máxima. |
| Pasivo | 15 monedas; su mejora se acumula. La tienda excluye el pasivo de penetración. |
| Reposición | Ninguna dentro del mismo nivel. El siguiente nivel tiene stock nuevo. |

Las tiradas de moneda se fijan con la semilla de contenido de cada habitación. Volver a una sala no vuelve a tirar el botín, ni regenera enemigos, monedas recogidas o artículos vendidos. El dinero se concede al recoger físicamente la moneda, no al matar. El monedero solo descuenta el precio si la compra es válida. A vida completa el corazón se conserva y no se cobra.

## Comprobación del equilibrio

`Tests/LootBalanceSmoke.gd` genera y recorre 30 semillas para cada uno de los niveles 1, 3 y 6. Inspecciona los enemigos que realmente aparecen, los derrota y comprueba que las tiradas generan las monedas correspondientes.

Muestra actual: **2.151 enemigos, 759 monedas**, un **35,29 %** de apariciones.

| Nivel | Monedas medias en sus ocho salas normales | Recompensa de jefe |
| --- | --- | --- |
| 1 | 6,83 | 3 |
| 3 | 8,70 | 3 |
| 6 | 9,77 | Victoria final |

En la muestra, completar el primer nivel da unas 9,83 monedas contando al jefe. Un corazón de 3 permite recuperarse sin agotar necesariamente el ahorro, mientras que el pasivo de 15 suele exigir guardar monedas de más de un nivel. Los valores no garantizan un ingreso por partida: dependen de las tiradas y de cuánto explores. Puedes volver a la tienda después del jefe antes de entrar en el portal.

La muestra usa derrotas controladas; comprueba distribución y presupuesto, no sustituye pruebas de dificultad con jugadores. Las cifras se centralizan en `Scripts/Items/Economy.cs` para poder ajustar el equilibrio con experiencia de juego.
