﻿using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using Winch.Core;
using Winch.Serialization;
using Winch.Serialization.Item;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using Winch.Data.Item;
using UnityEngine.AddressableAssets;

namespace Winch.Util;

public static class ItemUtil
{
    private static readonly Dictionary<Type, IDredgeTypeConverter> Converters = new()
    {
        { typeof(NonSpatialItemData), new NonSpatialItemDataConverter() },
        { typeof(MessageItemData), new MessageItemDataConverter() },
        { typeof(ResearchableItemData), new ResearchableItemDataConverter() },
        { typeof(SpatialItemData), new SpatialItemDataConverter() },
        { typeof(EngineItemData), new EngineItemDataConverter() },
        { typeof(HarvestableItemData), new HarvestableItemDataConverter() },
        { typeof(AberrationableFishItemData), new FishItemDataConverter() },
        { typeof(RelicItemData), new RelicItemDataConverter() },
        { typeof(GridConfigDeployableItemData), new DeployableItemDataConverter() },
        { typeof(CrabPotItemData), new CrabPotItemDataConverter() },
        { typeof(TrawlNetItemData), new TrawlNetItemDataConverter() },
        { typeof(DredgeItemData), new DredgeItemDataConverter() },
        { typeof(RodItemData), new RodItemDataConverter() },
        { typeof(LightItemData), new LightItemDataConverter() },
        { typeof(DamageItemData), new DamageItemDataConverter() },
        { typeof(DurableItemData), new DurableItemDataConverter() },
        { typeof(ThawableItemData), new ThawableItemDataConverter() },
        { typeof(GadgetItemData), new GadgetItemDataConverter() },
    };

    internal static bool PopulateObjectFromMetaWithConverters<T>(T item, Dictionary<string, object> meta) where T : ItemData
    {
        return UtilHelpers.PopulateObjectFromMeta<T>(item, meta, Converters);
    }

    internal static List<string> VanillaItemIDList = new();

    internal static void Initialize()
    {
        Addressables.LoadAssetsAsync<ItemData>(AddressablesUtil.GetLocations<ItemData>("ItemData"), itemData => VanillaItemIDList.SafeAdd(itemData.id));
    }

    internal static Dictionary<string, ItemData> AllItemDataDict = new();
    internal static Dictionary<string, NonSpatialItemData> NonSpatialItemDataDict = new();
    internal static Dictionary<string, SpatialItemData> SpatialItemDataDict = new();
    internal static Dictionary<string, HarvestableItemData> HarvestableItemDataDict = new();
    internal static Dictionary<string, FishItemData> FishItemDataDict = new();
    internal static Dictionary<string, ItemData> ModdedItemDataDict = new();

    public static ItemData GetModdedItemData(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (ModdedItemDataDict.TryGetValue(id, out ItemData itemData))
            return itemData;
        else
            return null;
    }

    internal static void AddModdedItemData(IList<ItemData> list)
    {
        foreach (var item in ModdedItemDataDict.Values)
        {
            list.SafeAdd(item);
        }
    }

    public static void AddModdedFishItemData(IList<FishItemData> list)
    {
        foreach (var item in ModdedItemDataDict.Values.WhereType<ItemData, FishItemData>())
        {
            list.SafeAdd(item);
        }
    }

    /// <summary>
    /// Encyclopedia doesn't run <see cref="Encyclopedia.Awake"/> until it is opened so we just search for it with Resources and add the fish
    /// </summary>
    internal static void Encyclopedia()
    {
        WinchCore.Log.Info("[Encyclopedia] AddModdedFishItemData");
        var encyclopedia = Resources.FindObjectsOfTypeAll<Encyclopedia>().FirstOrDefault();
        AddModdedFishItemData(encyclopedia.allFish);
        AddModdedEncyclopediaButton(encyclopedia);
    }

    internal static void AddModdedEncyclopediaButton(Encyclopedia encyclopedia)
    {
        var container = encyclopedia.transform.parent.gameObject;
        container.SetActive(true);
        container.SetActive(false);
        var zones = encyclopedia.dlc1ZoneButton.transform.parent as RectTransform;
        zones.sizeDelta = new Vector2(zones.sizeDelta.x, zones.sizeDelta.y + 50);
        var moddedButton = encyclopedia.dlc1ZoneButton.gameObject.InstantiateInactive();
        moddedButton.transform.SetParent(zones, false);
        var moddedButtonWrapper = moddedButton.GetComponent<BasicButtonWrapper>();
        moddedButtonWrapper.localizedString.StringReference = LocalizationUtil.Unknown;
        moddedButton.name = "ModdedEncyclopediaTabButton";
        moddedButton.SetActive(true);
        encyclopedia.zoneButtons.Add(moddedButtonWrapper);
        encyclopedia.SetZoneButtonPositions();
        var image = moddedButton.GetComponent<Image>();
        image.sprite = TextureUtil.GetSprite("EncyclopediaTabWinch");
    }

    internal static List<ItemData> TryGetItems(List<string> ids)
    {
        List<ItemData> items = new List<ItemData>();

        if (ids == null)
            return items;

        foreach (var item in ids)
        {
            if (!string.IsNullOrWhiteSpace(item) && AllItemDataDict.TryGetValue(item, out var itemData))
            {
                items.Add(itemData);
            }
        }

        return items;
    }

    internal static List<HarvestableItemData> TryGetHarvestables(List<string> ids)
    {
        List<HarvestableItemData> harvestables = new List<HarvestableItemData>();

        if (ids == null)
            return harvestables;

        foreach (var harvestable in ids)
        {
            if (!string.IsNullOrWhiteSpace(harvestable) && HarvestableItemDataDict.TryGetValue(harvestable, out var itemData))
            {
                harvestables.Add(itemData);
            }
        }

        return harvestables;
    }

    internal static List<FishItemData> TryGetFish(List<string> ids)
    {
        List<FishItemData> fishes = new List<FishItemData>();

        if (ids == null)
            return fishes;

        foreach (var fish in ids)
        {
            if (!string.IsNullOrWhiteSpace(fish) && FishItemDataDict.TryGetValue(fish, out var itemData))
            {
                fishes.Add(itemData);
            }
        }

        return fishes;
    }

    internal static void PopulateItemData(IList<ItemData> result)
    {
        foreach (var item in result)
        {
            AllItemDataDict.Add(item.id, item);
            WinchCore.Log.Debug($"Added item {item.id} to AllItemDataDict");
            if (item is SpatialItemData sitem)
            {
                SpatialItemDataDict.Add(item.id, sitem);
                WinchCore.Log.Debug($"Added item {item.id} to SpatialItemDataDict");
                if (item is HarvestableItemData hitem) // Fish and Relics
                {
                    HarvestableItemDataDict.Add(item.id, hitem);
                    WinchCore.Log.Debug($"Added item {item.id} to HarvestableItemDataDict");
                    if (hitem is FishItemData fitem)
                    {
                        FishItemDataDict.Add(item.id, fitem);
                        WinchCore.Log.Debug($"Added item {item.id} to FishItemDataDict");
                    }
                }
            }
            if (item is NonSpatialItemData nsitem)
            {
                NonSpatialItemDataDict.Add(item.id, nsitem);
                WinchCore.Log.Debug($"Added item {item.id} to NonSpatialItemDataDict");
            }
        }
    }

    internal static void ClearItemData()
    {
        AllItemDataDict.Clear();
        WinchCore.Log.Debug($"AllItemDataDict cleared");
        SpatialItemDataDict.Clear();
        WinchCore.Log.Debug($"SpatialItemDataDict cleared");
        HarvestableItemDataDict.Clear();
        WinchCore.Log.Debug($"HarvestableItemDataDict cleared");
        FishItemDataDict.Clear();
        WinchCore.Log.Debug($"FishItemDataDict cleared");
        NonSpatialItemDataDict.Clear();
        WinchCore.Log.Debug($"NonSpatialItemDataDict cleared");
    }

    internal static void AddItemFromMeta<T>(string metaPath) where T : ItemData
    {
        var meta = UtilHelpers.ParseMeta(metaPath);
        if (meta == null)
        {
            WinchCore.Log.Error($"Meta file {metaPath} is empty");
            return;
        }
        var item = UtilHelpers.GetScriptableObjectFromMeta<T>(meta, metaPath);
        if (item == null)
        {
            WinchCore.Log.Error($"Couldn't create {typeof(T).FullName}");
            return;
        }
        var id = (string)meta["id"];
        if (VanillaItemIDList.Contains(id))
        {
            WinchCore.Log.Error($"Item {id} already exists in vanilla.");
            return;
        }
        if (ModdedItemDataDict.ContainsKey(id))
        {
            WinchCore.Log.Error($"Duplicate item {id} at {metaPath} failed to load");
            return;
        }
        if (PopulateObjectFromMetaWithConverters<T>(item, meta))
        {
            ModdedItemDataDict.Add(id, item);
            AddressablesUtil.AddResourceAtLocation("ItemData", id, id, item);
        }
        else
        {
            WinchCore.Log.Error($"No item data converter found for type {typeof(T)}");
        }
    }
}
