using Npgsql;
using NpgsqlTypes;

class PosatgreSQLHandler
{
    // Move this to environment variables in real code.
    // !!! Rotate the leaked password on Railway first.
    private string _connectionString =
        "Server=centerbeam.proxy.rlwy.net;Port=57660;User Id=postgres;Password=cbokidEBKWIVuaZHczExhVeuXWIXtAjO;Database=railway;";

    private readonly NpgsqlDataSource _dataSource;

    private Dictionary<string, string> _namesDict = new Dictionary<string, string>
    {
        {"BAG", "Bag"},
        {"BAG_INSIGHT", "Satchel of Insight"},
        {"CAPE", "Cape"},
        {"CAPEITEM_FW_BRIDGEWATCH", "Bridgewatch Cape"},
        {"CAPEITEM_FW_FORTSTERLING", "Fort Sterling Cape"},
        {"CAPEITEM_FW_LYMHURST", "Lymhurst Cape"},
        {"CAPEITEM_FW_MARTLOCK", "Martlock Cape"},
        {"CAPEITEM_FW_THETFORD", "Thetford Cape"},
        {"CAPEITEM_FW_CAERLEON", "Caerleon Cape"},
        {"CAPEITEM_FW_BRECILIEN", "Brecilien Cape"},
        {"CAPEITEM_AVALON", "Avalonian Cape"},
        {"CAPEITEM_SMUGGLER", "Smuggler Cape"},
        {"CAPEITEM_HERETIC", "Heretic Cape"},
        {"CAPEITEM_UNDEAD", "Undead Cape"},
        {"CAPEITEM_KEEPER", "Keeper Cape"},
        {"CAPEITEM_MORGANA", "Morgana Cape"},
        {"CAPEITEM_DEMON", "Demon Cape"},

        { "HEAD_CLOTH_SET1", "Scholar Cowl"},
        {"HEAD_CLOTH_SET2", "Cleric Cowl"},
        {"HEAD_CLOTH_SET3", "Mage Cowl"},
        {"HEAD_CLOTH_KEEPER", "Druid Cowl"},
        {"HEAD_CLOTH_HELL", "Fiend Cowl"},
        {"HEAD_CLOTH_MORGANA", "Cultist Cowl"},
        {"HEAD_CLOTH_FEY", "Feyscale Hat"},
        {"HEAD_CLOTH_AVALON", "Cowl of Purity"},
        {"HEAD_CLOTH_ROYAL", "Royal Cowl"},

        {"HEAD_LEATHER_SET1", "Mercenary Hood"},
        {"HEAD_LEATHER_SET2", "Hunter Hood"},
        {"HEAD_LEATHER_SET3", "Assassin Hood"},
        {"HEAD_LEATHER_MORAGNA", "Stalker Hood"},
        {"HEAD_LEATHER_HELL", "Hellion Hood"},
        {"HEAD_LEATHER_UNDEAD", "Specter Hood"},
        {"HEAD_LEATHER_FEY", "Mistwalker Hood"},
        {"HEAD_LEATHER_AVALON", "Hood of Tenacity"},
        {"HEAD_LEATHER_ROYAL", "Royal Hood"},

        {"HEAD_PLATE_SET1", "Soldier Helmet"},
        {"HEAD_PLATE_SET2", "Knight Helmet"},
        {"HEAD_PLATE_SET3", "Guardian Helmet"},
        {"HEAD_PLATE_UNDEAD", "Graveguard Helmet"},
        {"HEAD_PLATE_HELL", "Demon Helmet"},
        {"HEAD_PLATE_KEEPER", "Judicator Helmet"},
        {"HEAD_PLATE_FEY", "Duskweaver Helmet"},
        {"HEAD_PLATE_AVALON", "Helmet of Valor"},
        {"HEAD_PLATE_ROYAL", "Royal Helmet`"},

        {"ARMOR_CLOTH_SET1", "Scholar Robe"},
        {"ARMOR_CLOTH_SET2", "Cleric Robe"},
        {"ARMOR_CLOTH_SET3", "Mage Robe"},
        {"ARMOR_CLOTH_KEEPER", "Druid Robe"},
        {"ARMOR_CLOTH_HELL", "Fiend Robe"},
        {"ARMOR_CLOTH_MORGANA", "Cultist Robe"},
        {"ARMOR_CLOTH_FEY", "Feyscale Robe"},
        {"ARMOR_CLOTH_AVALON", "Robe of Purity"},
        {"ARMOR_CLOTH_ROYAL", "Royal Robe"},

        {"ARMOR_LEATHER_SET1", "Mercenary Jacket"},
        {"ARMOR_LEATHER_SET2", "Hunter Jacket"},
        {"ARMOR_LEATHER_SET3", "Assassin Jacket"},
        {"ARMOR_LEATHER_MORGANA", "Stalker Jacket"},
        {"ARMOR_LEATHER_HELL", "Hellion Jacket"},
        {"ARMOR_LEATHER_UNDEAD", "Specter Jacket"},
        {"ARMOR_LEATHER_FEY", "Mistwalker Jacket"},
        {"ARMOR_LEATHER_AVALON", "Jacket of Tenacity"},
        {"ARMOR_LEATHER_ROYAL", "Royal Jacket"},

        {"ARMOR_PLATE_SET1", "Soldier Armor"},
        {"ARMOR_PLATE_SET2", "Knight Armor"},
        {"ARMOR_PLATE_SET3", "Guardian Armor"},
        {"ARMOR_PLATE_UNDEAD", "Graveguard Armor"},
        {"ARMOR_PLATE_HELL", "Demon Armor"},
        {"ARMOR_PLATE_KEEPER", "Judicator Armor"},
        {"ARMOR_PLATE_FEY", "Duskweaver Armor"},
        {"ARMOR_PLATE_AVALON", "Armor of Valor"},
        {"ARMOR_PLATE_ROYAL", "Royal Armor"},

        {"SHOES_CLOTH_SET1", "Scholar Sandals"},
        {"SHOES_CLOTH_SET2", "Cleric Sandals"},
        {"SHOES_CLOTH_SET3", "Mage Sandals"},
        {"SHOES_CLOTH_KEEPER", "Druid Sandals"},
        {"SHOES_CLOTH_HELL", "Fiend Sandals"},
        {"SHOES_CLOTH_MORGANA", "Cultist Sandals"},
        {"SHOES_CLOTH_FEY", "Feyscale Sndals"},
        {"SHOES_CLOTH_AVALON", "Sandals of Purity"},
        {"SHOES_CLOTH_ROYAL", "Royal Sandals"},

        {"SHOES_LEATHER_SET1", "Mercenary Shoes"},
        {"SHOES_LEATHER_SET2", "Hunter Shoes"},
        {"SHOES_LEATHER_SET3", "Assassin Shoes"},
        {"SHOES_LEATHER_MORGANA", "Stalker Shoes"},
        {"SHOES_LEATHER_HELL", "Hellion Shoes"},
        {"SHOES_LEATHER_UNDEAD", "Specter Shoes"},
        {"SHOES_LEATHER_FEY", "Mistwalker Shoes"},
        {"SHOES_LEATHER_AVALON", "Shoes of Tenacity"},
        {"SHOES_LEATHER_ROYAL", "Royal Shoes"},

        {"SHOES_PLATE_SET1", "Soldier Boots"},
        {"SHOES_PLATE_SET2", "Knight Boots"},
        {"SHOES_PLATE_SET3", "Guardian Boots"},
        {"SHOES_PLATE_UNDEAD", "Graveguard Boots"},
        {"SHOES_PLATE_HELL", "Demon Boots"},
        {"SHOES_PLATE_KEEPER", "Judicator Boots"},
        {"SHOES_PLATE_FEY", "Duskweaver Boots"},
        {"SHOES_PLATE_AVALON", "Boots of Valor"},
        {"SHOES_PLATE_ROYAL", "Royal Boots"},

        {"MAIN_ARCANESTAFF", "Arcane Staff"},
        {"2H_ARCANESTAFF", "Great Arcane Staff"},
        {"2H_ENIGMATICSTAFF", "Enigmatic Staff"},
        {"MAIN_ARCANESTAFF_UNDEAD", "Witchwork Staff"},
        {"2H_ARCANESTAFF_HELL", "Occult Staff"},
        {"2H_ENIGMATICORB_MORGANA", "Malevolent Locus"},
        {"2H_ARCANE_RINGPAIR_AVALON", "Evensong"},

        {"MAIN_AXE", "Battleaxe"},
        {"2H_AXE", "Greataxe"},
        {"2H_HALBERD", "Halberd"},
        {"2H_HALBERD_MORGANA", "Carrioncaller"},
        {"2H_SCYTHE_HELL", "Infernal Scythe"},
        {"2H_DUALAXE_KEEPER", "Bear Paws"},
        {"2H_AXE_AVALON", "Realmbreaker"},

        {"2H_CROSSBOW", "Crossbow"},
        {"2H_CROSSBOWLARGE", "Heavy Crossbow"},
        {"MAIN_1HCROSSBOW", "Light Crossbow"},
        {"2H_REPEATINGCROSSBOW_UNDEAD", "Weeping Repeater"},
        {"2H_DUALCROSSBOW_HELL", "Boltcasters"},
        {"2H_CROSSBOWLARGE_MORGANA", "Siegebow"},
        {"2H_CROSSBOW_CANNON_AVALON", "Energy Shaper"},

        {"MAIN_CURSEDSTAFF", "Cursed Staff"},
        {"2H_CURSEDSTAFF", "Great Cursed Staff"},
        {"2H_DEMONICSTAFF", "Demonic Staff"},
        {"MAIN_CURSEDSTAFF_UNDEAD", "Lifecurse Staff"},
        {"2H_SKULLORB_HELL", "Cursed Skull"},
        {"2H_CURSEDSTAFF_MORGANA", "Damnation Staff"},
        {"MAIN_CURSEDSTAFF_AVALON", "Shadowcaller"},

        {"MAIN_DAGGER", "Dagger"},
        {"2H_DAGGERPAIR", "Dagger Pair"},
        {"2H_CLAWPAIR", "Claws"},
        {"MAIN_RAPIER_MORGANA", "Bloodletter"},
        {"MAIN_DAGGER_HELL", "Demonfang"},
        {"2H_DUALSICKLE_UNDEAD", "Deathgivers"},
        {"2H_DAGGER_KATAR_AVALON", "Bridled Fury"},

        {"MAIN_FIRESTAFF", "Fire Staff"},
        {"2H_FIRESTAFF", "Great Fire Staff"},
        {"2H_INFERNOSTAFF", "Infernal Staff"},
        {"MAIN_FIRESTAFF_KEEPER", "Wildfire Staff"},
        {"2H_FIRESTAFF_HELL", "Brimstone Staff"},
        {"2H_INFERNOSTAFF_MORGANA", "Blazing Staff"},
        {"2H_FIRE_RINGPAIR_AVALON", "Dawnsong"},

        {"MAIN_FROSTSTAFF", "Frost Staff"},
        {"2H_FROSTSTAFF", "Great Frost Staff"},
        {"2H_GLACIALSTAFF", "Glacial Staff"},
        {"MAIN_FROSTSTAFF_KEEPER", "Hoarfrost Staff"},
        {"2H_ICEGAUNTLETS_HELL", "Icicle Staff"},
        {"2H_ICECRYSTAL_UNDEAD", "Permafrost Prism"},
        {"MAIN_FROSTSTAFF_AVALON", "Chillhowl"},

        {"MAIN_HAMMER", "Hammer"},
        {"2H_POLEHAMMER", "Polehammer"},
        {"2H_HAMMER", "Great Hammer"},
        {"2H_HAMMER_UNDEAD", "Tombhammer"},
        {"2H_DUALHAMMER_HELL", "Forge Hammers"},
        {"2H_RAM_KEEPER", "Grovekeeper"},
        {"2H_HAMMER_AVALON", "Hand of Justice"},

        {"MAIN_HOLYSTAFF", "Holy Staff"},
        {"2H_HOLYSTAFF", "Great Holy Staff"},
        {"2H_DIVINESTAFF", "Divine Staff"},
        {"MAIN_HOLYSTAFF_MORGANA", "Lifetouch Staff"},
        {"2H_HOLYSTAFF_HELL", "Fallen Staff"},
        {"2H_HOLYSTAFF_UNDEAD", "Redemption Staff"},
        {"MAIN_HOLYSTAFF_AVALON", "Hallowfall"},

        {"2H_KNUCKLES_SET1", "Brawler Gloves"},
        {"2H_KNUCKLES_SET2", "Battle Bracers"},
        {"2H_KNUCKLES_SET3", "Spiked Gauntlets"},
        {"2H_KNUCKLES_KEEPER", "Ursine Maulers"},
        {"2H_KNUCKLES_HELL", "Hellfire Hands"},
        {"2H_KNUCKLES_MORGANA", "Ravenstrike Cestus"},
        {"2H_KNUCKLES_AVALON", "Fists of Avalon"},

        {"MAIN_MACE", "Mace"},
        {"2H_MACE", "Heavy Mace"},
        {"2H_FLAIL", "Morning Star"},
        {"MAIN_ROCKMACE_KEEPER", "Bedrock Mace"},
        {"MAIN_MACE_HELL", "Incubus Mace"},
        {"2H_MACE_MORGANA", "Camlann Mace"},
        {"2H_DUALMACE_AVALON", "Oathkeepers"},

        {"MAIN_NATURESTAFF", "Nature Staff"},
        {"2H_NATURESTAFF", "Great Nature Staff"},
        {"2H_WILDSTAFF", "Wild Staff"},
        {"MAIN_NATURESTAFF_KEEPER", "Druidic Staff"},
        {"2H_NATURESTAFF_HELL", "Blight Staff"},
        {"2H_NATURESTAFF_KEEPER", "Rampant Staff"},
        {"MAIN_NATURESTAFF_AVALON", "Ironroot Staff"},

        {"2H_QUARTERSTAFF", "Quarterstaff"},
        {"2H_IRONCLADEDSTAFF", "Iron-clad Staff"},
        {"2H_DOUBLEBLADEDSTAFF", "Double Bladed Staff"},
        {"2H_COMBATSTAFF_MORGANA", "Black Monk Stave"},
        {"2H_TWINSCYTHE_HELL", "Soulscythe"},
        {"2H_ROCKSTAFF_KEEPER", "Staff of Balance"},
        {"2H_QUARTERSTAFF_AVALON", "Grailseeker"},

        {"2H_SHAPESHIFTER_SET1", "Prowling Staff"},
        {"2H_SHAPESHIFTER_SET2", "Rootbound Staff"},
        {"2H_SHAPESHIFTER_SET3", "Primal Staff"},
        {"2H_SHAPESHIFTER_MORGANA", "Bloodmoon Staff"},
        {"2H_SHAPESHIFTER_HELL", "Hellspawn Staff"},
        {"2H_SHAPESHIFTER_KEEPER", "Earthrune Staff"},
        {"2H_SHAPESHIFTER_AVALON", "Lightcaller"},

        {"MAIN_SPEAR", "Spear"},
        {"2H_SPEAR", "Pike"},
        {"2H_GLAIVE", "Glaive"},
        {"MAIN_SPEAR_KEEPER", "Heron Spear"},
        {"2H_HARPOON_HELL", "Spirithunter"},
        {"2H_TRIDENT_UNDEAD", "Trinity Spear"},
        {"MAIN_SPEAR_LANCE_AVALON", "Daybreaker"},

        {"MAIN_SWORD", "Broadsword"},
        {"2H_CLAYMORE", "Claymore"},
        {"2H_DUALSWORD", "Dual Swords"},
        {"MAIN_SCIMITAR_MORGANA", "Clarent Blade"},
        {"2H_CLEAVER_HELL", "Carving Sword"},
        {"2H_DUALSCIMITAR_UNDEAD", "Galatine Pair"},
        {"2H_CLAYMORE_AVALON", "Kingmaker"},

        {"2H_BOW", "Bow"},
        {"2H_WARBOW", "Warbow"},
        {"2H_LONGBOW", "Longbow"},
        {"2H_LONGBOW_UNDEAD", "Whispering Bow"},
        {"2H_BOW_HELL", "Wailing Bow"},
        {"2H_BOW_KEEPER", "Bow of Badon"},
        {"2H_BOW_AVALON", "Mistpiercer"},

        {"OFF_BOOK", "Tome of Spells"},
        {"OFF_ORB_MORGANA", "Eye of Secrets"},
        {"OFF_DEMONSKULL_HELL", "Muisak"},
        {"OFF_TOTEM_KEEPER", "Taproot"},
        {"OFF_CENSER_AVALON", "Celestial Censer"},

        {"OFF_SHIELD", "Shield"},
        {"OFF_TOWERSHIELD_UNDEAD", "Sarcophagus"},
        {"OFF_SHIELD_HELL", "Caitiff Shield"},
        {"OFF_SPIKEDSHIELD_MORGANA", "Facebreaker"},
        {"OFF_SHIELD_AVALON", "Astral Aegis"},

        {"OFF_TORCH", "Torch"},
        {"OFF_HORN_KEEPER", "Mistcaller"},
        {"OFF_TALISMAN_AVALON", "Sacred Scepter"},
        {"OFF_LAMP_UNDEAD", "Cryptcandle"},
        {"OFF_JESTERCANE_HELL", "Leering Cane"},
    };

    public PosatgreSQLHandler()
    {
        _dataSource = NpgsqlDataSource.Create(_connectionString);
        EnsureUniqueDbNameOnce();   // safe to call repeatedly
    }

    /// <summary>
    /// Idempotently ensures:
    ///  - items.id is BIGINT identity and PK
    ///  - items.db_name is UNIQUE (needed for ON CONFLICT)
    /// </summary>
    private void EnsureUniqueDbNameOnce()
    {
        const string ddl = @"
DO $$
BEGIN
  -- Is there ANY unique index/constraint on items(db_name)?
  IF NOT EXISTS (
    SELECT 1
    FROM pg_index i
    JOIN pg_class  t ON t.oid = i.indrelid
    JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey)
    WHERE t.relname = 'items'
      AND i.indisunique
      AND a.attname = 'db_name'
  ) THEN
    ALTER TABLE items ADD CONSTRAINT items_db_name_key UNIQUE (db_name);
  END IF;
END $$;";
        using var cmd = _dataSource.CreateCommand(ddl);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Single, atomic upsert. Lets Postgres auto-generate id and returns ids by db_name.
    /// Uses timestamp WITHOUT time zone (UTC value) to match your previous pattern.
    /// If your column is TIMESTAMPTZ, switch the two lines marked [TZ].
    /// </summary>
    public Dictionary<string, long> UpdateItemsData(Dictionary<string, int> marketData)
    {
        var dbNames = marketData.Keys.ToArray();
        var prices = marketData.Values.ToArray();
        if (dbNames.Length == 0) return new();

        var mapKeys = Array.Empty<string>();
        var mapVals = Array.Empty<string>();
        if (_namesDict != null && _namesDict.Count > 0)
        {
            mapKeys = _namesDict.Keys.ToArray();
            mapVals = _namesDict.Values.ToArray();
        }

        const string sql = @"
WITH d AS (
  SELECT unnest(@db_names::text[]) AS db_name,
         unnest(@prices::int[])    AS price
),
base AS (
  -- base name without '@enchant'
  SELECT
    db_name,
    price,
    split_part(db_name, '@', 1) AS base_no_enchant
  FROM d
),
-- compute the dictionary key by removing the leading 'T{tier}_'
k AS (
  SELECT
    db_name,
    price,
    regexp_replace(base_no_enchant, '^T[0-9]+_', '') AS dict_key
  FROM base
),
md AS (
  SELECT
    unnest(@map_keys::text[]) AS key,
    unnest(@map_vals::text[]) AS val
),
ins AS (
  INSERT INTO items (
    db_name,
    price_black_market,
    price_black_market_last_updated,
    tier,
    enchantment,
    item_type,
    title,
    additional_title,
    name
  )
  SELECT
    d.db_name,
    d.price,
    (now() AT TIME ZONE 'UTC'),
    CASE
      WHEN left(base.base_no_enchant, 1) = 'T'
      THEN NULLIF(replace(split_part(base.base_no_enchant, '_', 1), 'T', ''), '')::int
      ELSE NULL
    END AS tier,
    COALESCE(NULLIF(split_part(d.db_name, '@', 2), ''), '0')::int AS enchantment,
    NULLIF(split_part(base.base_no_enchant, '_', 2), '') AS item_type,
    NULLIF(split_part(base.base_no_enchant, '_', 3), '') AS title,
    NULLIF(split_part(base.base_no_enchant, '_', 4), '') AS additional_title,
    (SELECT md.val FROM md WHERE md.key = k.dict_key LIMIT 1) AS name
  FROM d
  JOIN base ON base.db_name = d.db_name
  JOIN k    ON k.db_name    = d.db_name
  ON CONFLICT (db_name) DO UPDATE
  SET price_black_market              = EXCLUDED.price_black_market,
      price_black_market_last_updated = EXCLUDED.price_black_market_last_updated,
      tier                            = EXCLUDED.tier,
      enchantment                     = EXCLUDED.enchantment,
      item_type                       = EXCLUDED.item_type,
      title                           = EXCLUDED.title,
      additional_title                = EXCLUDED.additional_title,
      -- overwrite name only if a mapped value exists this time
      name = COALESCE(EXCLUDED.name, items.name)
  RETURNING id, db_name
)
SELECT id, db_name FROM ins;";

        using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.Add(new NpgsqlParameter("db_names", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = dbNames });
        cmd.Parameters.Add(new NpgsqlParameter("prices", NpgsqlDbType.Array | NpgsqlDbType.Integer) { Value = prices });
        cmd.Parameters.Add(new NpgsqlParameter("map_keys", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = mapKeys });
        cmd.Parameters.Add(new NpgsqlParameter("map_vals", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = mapVals });

        var ids = new Dictionary<string, long>(dbNames.Length);
        using var r = cmd.ExecuteReader();
        while (r.Read()) ids[r.GetString(1)] = r.GetInt64(0);
        return ids;
    }

    public void GetData()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var cmd = new NpgsqlCommand("SELECT id, db_name, price_black_market FROM items ORDER BY id DESC;", connection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            Console.WriteLine($"id={reader.GetInt64(0)} name={reader.GetString(1)} price={reader.GetInt32(2)}");
        }
    }
}
