using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.GameMenus;


namespace TLWorkshopCurrentRatingMod
{
    public class TLWorkshopCurrentRatingCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnGameLoaded));
        }
        public override void SyncData(IDataStore dataStore)
        {

        }
        private static void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            InitializeMod(campaignGameStarter);
        }
        private static void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            InitializeMod(campaignGameStarter);
        }
        private static void InitializeMod(CampaignGameStarter campaignGameStarter)
        {
            if (campaignGameStarter != null)
            {
                campaignGameStarter.AddGameMenuOption("town", "TLWorkshopCurrentRatingCondition", "Show Ratings of Workshops", new GameMenuOption.OnConditionDelegate(TownCondition), new GameMenuOption.OnConsequenceDelegate(TownConsequence), false, -1, false);
            }
            OnSessionLaunched();
        }
        // copied code from TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.WorkshopsCampaignBehavior.OnSessionLaunched()
        //associate all items with diffrent categorys 将每种item与category关联
        private static void OnSessionLaunched()
        {
            foreach (ItemObject itemObject in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
            {
                if (IsProducable(itemObject))
                {
                    ItemCategory itemCategory = itemObject.ItemCategory;
                    List<ItemObject> list;
                    if (!itemsInCategory.TryGetValue(itemCategory, out list))
                    {
                        list = new List<ItemObject>();
                        itemsInCategory[itemCategory] = list;
                    }
                    list.Add(itemObject);
                }
            }
        }
        private static bool TownCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return true;
        }
        private static void TownConsequence(MenuCallbackArgs args)
        {
            GetRatingOfWrokshops();
        }

        private static void GetRatingOfWrokshops()
        {
            Town town = Settlement.CurrentSettlement.Town;
            float policyEffectToProduction = Campaign.Current.Models.WorkshopModel.GetPolicyEffectToProduction(town);
            ExplainedNumber explainedNumber = new ExplainedNumber(1f, null);
            if (town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Trade.VillagerConnections))
            {
                Helpers.PerkHelper.AddPerkBonusForTown(DefaultPerks.Trade.VillagerConnections, town, ref explainedNumber);
            }
            float Speed_Effect = policyEffectToProduction * explainedNumber.ResultNumber;
            string messagestring = "";
            foreach (WorkshopType workshopType in WorkshopType.All)
            {
                //defualt workshop salary = -25 一级工坊工资
                float Profit = -25;
                foreach (WorkshopType.Production workshopTypeProduction in workshopType.Productions)
                {
                    bool Has_nonTradeGood = false;
                    //weapon armor ...won't effect workshop's value 当时木工坊铁匠铺就是因为生产的武器盔甲值钱而成为印钞厂 现在生产这些东西不会影响工坊价值
                    if (workshopType.Productions.Count > 2)
                    {
                        foreach (ValueTuple<ItemCategory, int> valueTuple in workshopTypeProduction.Inputs)
                        {
                            if (valueTuple.Item1 != null && !valueTuple.Item1.IsTradeGood)
                            {
                                Has_nonTradeGood = true;
                                break;
                            }
                        }
                        if (!Has_nonTradeGood)
                        {
                            foreach (ValueTuple<ItemCategory, int> valueTuple2 in workshopTypeProduction.Outputs)
                            {
                                if (valueTuple2.Item1 != null && !valueTuple2.Item1.IsTradeGood)
                                {
                                    Has_nonTradeGood = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (Has_nonTradeGood)
                    {
                        continue;
                    }

                    float Inputs_cost = 0;
                    float Outputs_profit = 0;
                    for (int i = 0; i < workshopTypeProduction.Inputs.Count; i++)
                    {
                        List<ItemObject> Item_list;
                        if (!itemsInCategory.TryGetValue(workshopTypeProduction.Inputs[i].Item1, out Item_list))
                        {
                            InformationManager.DisplayMessage(new InformationMessage("An error occurred"));
                            return;
                        }
                        ItemObject input_item = Item_list[0];
                        int itemPrice = Math.Min(1000, town.GetItemPrice(input_item, null, false));
                        Inputs_cost += workshopTypeProduction.Inputs[i].Item2 * itemPrice * Speed_Effect * workshopTypeProduction.ConversionSpeed;
                    }
                    for (int i = 0; i < workshopTypeProduction.Outputs.Count; i++)
                    {
                        int item = workshopTypeProduction.Outputs[i].Item2;
                        ItemModifier itemModifier = null;
                        ItemObject randomItem = GetRandomItem(workshopTypeProduction.Outputs[i].Item1, town);
                        if (randomItem.ItemCategory == DefaultItemCategories.Garment && randomItem != null)
                        {
                            Outputs_profit += Math.Min(1000, DefaultItemCategories.Garment.AverageValue) * item * Speed_Effect * workshopTypeProduction.ConversionSpeed;
                        }
                        else if (randomItem.ItemCategory == DefaultItemCategories.LightArmor && randomItem != null)
                        {
                            Outputs_profit += Math.Min(1000, DefaultItemCategories.LightArmor.AverageValue) * item * Speed_Effect * workshopTypeProduction.ConversionSpeed;
                        }
                        else if (randomItem != null)
                        {
                            int itemPrice = town.GetItemPrice(new EquipmentElement(randomItem, itemModifier), null, false);
                            itemPrice = Math.Min(1000, itemPrice);
                            Outputs_profit += itemPrice * item * Speed_Effect * workshopTypeProduction.ConversionSpeed;
                        }
                    }
                    Profit += Outputs_profit - Inputs_cost;
                }
                //don't know how to handle dynamic eco
                //不知道如何处理动态经济问题
                float dynamiceconomyeffect = 1f;
                Profit = (int)Profit * dynamiceconomyeffect;
                messagestring = messagestring + town.Name.ToString() + " " + workshopType.Name.ToString() + " " + Profit.ToString() + "|";
            }
            //you can change deiscription here according to your language
            InformationManager.DisplayMessage(new InformationMessage("Ratings of Workshops: " + messagestring));

        }

        // copied code from TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.WorkshopsCampaignBehavior.IsProducable()
        private static bool IsProducable(ItemObject item)
        {
            return !item.MultiplayerItem && !item.NotMerchandise;
        }

        private static ItemObject GetRandomItem(ItemCategory itemGroupBase, Town townComponent)
        {
            ItemObject randomItemAux = GetRandomItemAux(itemGroupBase, townComponent);
            if (randomItemAux != null)
            {
                return randomItemAux;
            }
            return GetRandomItemAux(itemGroupBase, null);
        }

        private static ItemObject GetRandomItemAux(ItemCategory itemCategory, Town townComponent = null)
        {
            if (itemCategory == DefaultItemCategories.Unassigned)
            {
                return null;
            }
            float num = 0f;
            ItemObject result = null;
            List<ItemObject> list;
            if (!itemsInCategory.TryGetValue(itemCategory, out list))
            {
                return null;
            }
            foreach (ItemObject itemObject in list)
            {
                if ((townComponent == null || IsItemPreferredForTown(itemObject, townComponent)) && itemObject.ItemCategory == itemCategory)
                {
                    float num2 = 1f / (Math.Max(100f, (float)itemObject.Value) + 100f);
                    if (MBRandom.RandomFloat * (num + num2) >= num)
                    {
                        result = itemObject;
                    }
                    num += num2;
                }
            }
            return result;
        }

        private static bool IsItemPreferredForTown(ItemObject item, Town townComponent)
        {
            return item.Culture == null || item.Culture == townComponent.Culture;
        }

        //copied code from TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.WorkshopsCampaignBehavior
        //every category has a list of items 每个category拥有多种items
        private static Dictionary<ItemCategory, List<ItemObject>> itemsInCategory = new Dictionary<ItemCategory, List<ItemObject>>();
    }
}
