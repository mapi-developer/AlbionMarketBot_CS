// ITEMS CATEGORIES
// "Cloth Helmet", "Leather Helmet", "Plate Helmet", 
// "Cloth Armor", "Leather Armor", "Plate Armor",
// "Cloth Shoes", "Leather Shoes", "Plate Shoes",
// "Arcane Staff", "Axe", "Crossbow"
// "Cursed Staff", "Dagger", "Fire Staff"
// "Frost Staff", "Hammer", "Holy Staff"
// "War Gloves", "Mace", "Nature Staff"
// "Quarterstaff", "Shapeshifter Staff", "Spear"
// "Sword", "Bow", "Bag",
// "Off Mage", "Off Hunter", "Off Warrior"

PriceChecker checker = new PriceChecker();
checker.UpdatePrices(
    cityName: "Caerleon",
    categoriesToUpdate: null
    );

// OrderWriter orderWriter = new OrderWriter(minimalProfitRateToOrder: 1.25m);
// orderWriter.MakeOrders(removeOldOrders: false,
//     cityName: "Caerleon",
//     categories: null,
//     tiers: [6, 7, 8],
//     enchantments: [0, 1]
//     );
