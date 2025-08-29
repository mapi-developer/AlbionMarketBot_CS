// PriceChecker checker = new PriceChecker();
// checker.UpdatePrices();

OrderWriter orderWriter = new OrderWriter();
orderWriter.MakeOrders(
    categories: ["Shapeshifter Staff"],
    tiers: [4, 5, 6, 7],
    enchantments: [0, 1]
    );
