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

// PriceChecker checker = new PriceChecker();
// checker.UpdatePrices(
//     cityName: "Caerleon",
//     categoriesToUpdate: null
//     );

OrderWriter orderWriter = new OrderWriter(minimalProfitRateToOrder: 1.25m);
orderWriter.MakeOrders(
    removeOldOrders: false,
    cityName: "Caerleon",
    categories: ["Arcane Staff", "Axe", "Crossbow",
"Cursed Staff", "Dagger", "Fire Staff",
"Frost Staff", "Hammer", "Holy Staff",
"War Gloves", "Mace", "Nature Staff",
"Quarterstaff", "Shapeshifter Staff", "Spear",
"Sword", "Bow"],
    except_categories: ["Bag", "Capes"],
    tiers: [6, 7, 8],
    enchantments: [0, 1]
    );


void UpdateOrdersMain()
{
    AlbionTraveler travaler = new AlbionTraveler();
    OrderWriter orderWriter = new OrderWriter(minimalProfitRateToOrder: 1.25m);

    // travaler.FromIslandToTravaler();
    // travaler.TeleportToIsland("lymhurst");
    // travaler.WalkTo("from_lymhurst_to_market");

    // orderWriter.MakeOrders(
    //     removeOldOrders: false,
    //     cityName: "Caerleon",
    //     categories: null,
    //     except_categories: ["Bag", "Capes"],
    //     tiers: [6, 7, 8],
    //     enchantments: [0, 1]
    //     );

    // travaler.WalkTo("from_lymhurst_market_to_traveler");
    // travaler.TeleportToIsland("bridgewatch");
    // travaler.WalkTo("from_bridgewatch_to_market");

    // orderWriter.MakeOrders(
    //     removeOldOrders: true,
    //     cityName: "Caerleon",
    //     categories: null,
    //     except_categories: ["Bag", "Capes"],
    //     tiers: [6, 7, 8],
    //     enchantments: [0, 1]
    //     );

    // travaler.WalkTo("from_bridgewatch_market_to_traveler");
    // travaler.TeleportToIsland("fort_sterling");
    // travaler.WalkTo("from_fort_sterling_to_market");

    orderWriter.MakeOrders(
        removeOldOrders: false,
        cityName: "Caerleon",
        categories: null,
        except_categories: ["Bag", "Capes"],
        tiers: [6, 7, 8],
        enchantments: [0, 1]
        );
}

// UpdateOrdersMain();

// WindowCapture.InitializeDpiAwareness();

// using var wc = WindowCapture.FromTitle("Albion Online Client", matchMode: WindowCapture.TitleMatch.Contains);
// using var bmp = wc.Capture(WindowCapture.CaptureMode.Auto, includeFrame: true);

// // 1) init once
// using var ocr = new SimpleOcr(
//     Path.Combine(AppContext.BaseDirectory, "tessdata"), // folder that contains traineddata files
//     "eng"                                              // language
// );


// // 3) pick a zone and read text
// var zone = new Rectangle(x: 0, y: 0, width: 2560, height: 1600);
// string text = ocr.ReadText(bmp, zone);
// Console.WriteLine(text);
