extends "res://Tests/RoguelikeSmoke.gd"

func press_buy() -> void:
 var event := InputEventKey.new()
 event.keycode = KEY_E
 event.physical_keycode = KEY_E
 event.pressed = true
 Input.parse_input_event(event)
 await frames(3)
 event = InputEventKey.new()
 event.keycode = KEY_E
 event.physical_keycode = KEY_E
 event.pressed = false
 Input.parse_input_event(event)
 await frames(3)

func run() -> void:
 manager = root.get_node("DungeonManager")
 manager.SeedOverride = 1042
 change_scene_to_file("res://Scenes/Main.tscn")
 await frames(4)
 protect_player()
 verify(player.GetCoins() == 0, "New run starts with no coins")
 verify(not player.TrySpendCoins(3) and not player.TrySpendCoins(-1), "Wallet rejects insufficient funds and negative prices")
 graph = index_graph(manager.GetFloorSnapshot())
 var shops: Array = graph.values().filter(func(r): return r.kind == "Shop")
 verify(shops.size() == 1, "Exactly one shop exists per floor")
 var shop_id: String = shops[0].id
 await navigate(shop_id)
 await clear_room()
 var shop = manager.GetCurrentRoom()
 var stock = shop.get_node("ShopStock")
 verify(stock.get_child_count() == 3 and shop.GetAliveCount() == 0, "Shop safely stocks two hearts and one passive")
 var first_heart = stock.get_node("Offer0")
 var second_heart = stock.get_node("Offer1")
 var passive = stock.get_node("Offer2")
 verify(first_heart.GetPrice() == 3 and second_heart.GetPrice() == 3 and passive.GetPrice() == 15, "Passives cost five times a full heart")
 verify(shops[0].shop_item != 4, "Shop excludes non-stacking piercing upgrade")
 player.TrySpendCoins(player.GetCoins())
 player.position = first_heart.global_position
 await frames(3)
 var health: int = player.GetCurrentHealthHalfHearts()
 await press_buy()
 verify(not first_heart.IsSold() and player.GetCurrentHealthHalfHearts() == health and player.GetCoins() == 0, "Insufficient funds do not grant a heart")
 player.AddCoins(30)
 player.Heal(100)
 await press_buy()
 verify(player.GetCoins() == 30 and not first_heart.IsSold(), "Full health purchase is refused without spending or consuming stock")
 # Start another real run to obtain an injured, protected player again.
 change_scene_to_file("res://Scenes/Main.tscn")
 await frames(5)
 protect_player()
 graph = index_graph(manager.GetFloorSnapshot())
 visited_instances.clear()
 await navigate(shop_id)
 shop = manager.GetCurrentRoom()
 stock = shop.get_node("ShopStock")
 first_heart = stock.get_node("Offer0")
 second_heart = stock.get_node("Offer1")
 passive = stock.get_node("Offer2")
 player.TrySpendCoins(player.GetCoins())
 player.AddCoins(30)
 player.position = first_heart.global_position
 await frames(3)
 health = player.GetCurrentHealthHalfHearts()
 await press_buy()
 verify(first_heart.IsSold() and player.GetCoins() == 27 and player.GetCurrentHealthHalfHearts() == mini(health + 2, player.GetMaxHealthHalfHearts()), "E buys one full heart and charges exactly three coins")
 await press_buy()
 verify(player.GetCoins() == 27, "Sold heart cannot charge again")
 player.position = passive.global_position
 await frames(3)
 var before := player_stats()
 await press_buy()
 verify(passive.IsSold() and player.GetCoins() == 12, "E buys a passive for fifteen coins")
 verify(player_stats().slice(1,6) != before.slice(1,6), "Paid passive really changes player stats")
 await press_buy()
 verify(player.GetCoins() == 12, "Sold passive cannot be bought twice")
 var neighbor: String = graph[shop_id].neighbors.values()[0]
 await navigate(neighbor)
 await navigate(shop_id)
 verify(manager.GetCurrentRoom() == shop and first_heart.IsSold() and passive.IsSold() and not second_heart.IsSold(), "Purchased stock persists on revisiting; no free restock")
 player.position = second_heart.global_position
 await frames(3)
 paused = true
 await press_buy()
 verify(player.GetCoins() == 12 and not second_heart.IsSold(), "Pause prevents shop purchases")
 paused = false
 # Normal enemy coins must survive backtracking and award their single value.
 var normal_coin_found := false
 for entry in graph.values():
  if entry.kind != "Normal": continue
  await navigate(entry.id)
  await clear_room()
  var coin_room = manager.GetCurrentRoom()
  var normal_coins = coin_room.get_children().filter(func(n): return n.is_in_group("coins"))
  if normal_coins.is_empty(): continue
  var normal_coin = normal_coins[0]
  var back: String = entry.neighbors.values().filter(func(id): return graph[id].kind != "Boss")[0]
  await navigate(back)
  await navigate(entry.id)
  verify(is_instance_valid(normal_coin) and normal_coin.get_parent() == coin_room, "Uncollected enemy coin survives revisiting its room")
  var before_coin: int = player.GetCoins()
  player.position = normal_coin.global_position
  await frames(4)
  verify(player.GetCoins() == before_coin + 1 and not is_instance_valid(normal_coin), "Normal enemy coin credits exactly one coin on physical pickup")
  normal_coin_found = true
  break
 verify(normal_coin_found, "Normal enemies produce collectible currency")
 # Collect a real coin pile dropped by a boss, then ensure it cannot be duplicated.
 var boss_id: String
 for entry in graph.values():
  if entry.kind == "Boss": boss_id = entry.id
 await navigate(boss_id)
 var boss_room = manager.GetCurrentRoom()
 var boss = boss_room.get_node("Enemies").get_child(0)
 var coin_value: int = boss.CoinDropValue
 verify(coin_value == 3, "Intermediate bosses guarantee a three-coin reward")
 boss.HeartDropChance = 0
 boss.TakeDamage(10000)
 await frames(4)
 var coins = boss_room.get_children().filter(func(n): return n.is_in_group("coins"))
 verify(coins.size() == 1 and coins[0].Value == 3, "Boss defeat produces one real three-coin pickup")
 var pile = coins[0]
 var balance: int = player.GetCoins()
 player.position = pile.global_position
 await frames(4)
 verify(player.GetCoins() == balance + 3 and not is_instance_valid(pile), "Coin collision credits the wallet once")
 var adjacent: String = graph[boss_id].neighbors.values()[0]
 await navigate(adjacent)
 await navigate(boss_id)
 verify(boss_room.get_children().filter(func(n): return n.is_in_group("coins")).is_empty(), "Revisiting a cleared boss cannot farm coins")
 var portal = boss_room.get_node("ZonePortal")
 player.position = Vector2(512,450)
 await frames(3)
 balance = player.GetCoins()
 player.position = portal.global_position
 await frames(5)
 verify(manager.GetZoneIndex() == 2 and player.GetCoins() == balance, "Coins persist across floor portals")
 graph = index_graph(manager.GetFloorSnapshot())
 shops = graph.values().filter(func(r): return r.kind == "Shop")
 verify(shops.size() == 1, "Next floor has its own shop")
 visited_instances.clear()
 await navigate(shops[0].id)
 verify(manager.GetCurrentRoom().get_node("ShopStock").get_children().all(func(n): return not n.IsSold()), "Next floor has fresh stock")
 verify(player.AddCoins(1000) > 0 and player.GetCoins() == 99 and player.AddCoins(1) == 0, "Wallet caps at 99 without overflow")
 change_scene_to_file("res://Scenes/Main.tscn")
 await frames(4)
 player = get_first_node_in_group("player")
 verify(player.GetCoins() == 0, "Retry resets the economy")
 print("ECONOMY CHECKS: ", passed, " passed; ", failed, " failed")
 await preload("res://Tests/AudioTestCleanup.gd").drain(self)
 quit(1 if failed else 0)
