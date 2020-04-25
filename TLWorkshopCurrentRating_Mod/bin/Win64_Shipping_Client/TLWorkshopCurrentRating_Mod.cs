using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;


namespace TLWorkshopCurrentRating_Mod
{
    public class TLWorkshopCurrentRating_Mod : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            bool flag = !(game.GameType is Campaign);
            if (!flag)
            {
                ((CampaignGameStarter)gameStarterObject).AddBehavior(new TLWorkshopCurrentRating_Behavior());
                InformationManager.DisplayMessage(new InformationMessage("TL Workshop Current Rating Mod is successfully loaded ver.1.1.2"));
            }
        }
    }

    public class TLWorkshopCurrentRating_Behavior : CampaignBehaviorBase
    {
        private static void InitializeMod(CampaignGameStarter campaignGameStarter)
        {
            if (campaignGameStarter != null)
            {
                //you can change "当前工坊动态评分" here according to your language
                campaignGameStarter.AddGameMenuOption("town", "TLWorkshopCurrentRatingCondition", "当前工坊动态评分", new TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.OnConditionDelegate(TownCondition), new TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.OnConsequenceDelegate(TownConsequence), false, -1, false);
            }
            // copied code from TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.WorkshopsCampaignBehavior.OnSessionLaunched()
            //associate all items with diffrent categorys 将每种item与category关联
            foreach (ItemObject itemObject in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
            {
                if (IsProducable(itemObject))
                {
                    ItemCategory itemCategory = itemObject.ItemCategory;
                    System.Collections.Generic.List<ItemObject> list;
                    if (!itemsInCategory.TryGetValue(itemCategory, out list))
                    {
                        list = new System.Collections.Generic.List<ItemObject>();
                        itemsInCategory[itemCategory] = list;
                    }
                    list.Add(itemObject);
                }
            }
        }
        private static void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            InitializeMod(campaignGameStarter);
        }
        private static void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            InitializeMod(campaignGameStarter);
        }
        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnGameLoaded));
        }
        private static bool TownCondition(TaleWorlds.CampaignSystem.GameMenus.MenuCallbackArgs args)
        {
            args.optionLeaveType = TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.LeaveType.Trade;
            return true;
        }
        private static void TownConsequence(TaleWorlds.CampaignSystem.GameMenus.MenuCallbackArgs args)
        {
            //unused codes wrote according to TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.WorkshopsCampaignBehavior.DecideBestWorkshopType()
            //WorkshopType bestworkshop = DecideBestWorkshopType(Settlement.CurrentSettlement, 0);
            //string messagestring = "根据原料特产远近最佳工坊:" + bestworkshop.Name.ToString();
            //InformationManager.DisplayMessage(new InformationMessage(messagestring));
            PredictWorkshopRating();
        }

        //most copied from TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.WorkshopsCampaignBehavior
        private static void PredictWorkshopRating()
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
                    float Inputs_cost = 0;
                    float Outputs_profit = 0;
                    for (int i = 0; i < workshopTypeProduction.Inputs.Count; i++)
                    {
                        ItemRoster itemRoster = town.Owner.ItemRoster;
                        int num = itemRoster.FindIndex((ItemObject x) => x.ItemCategory == workshopTypeProduction.Inputs[i].Item1);
                        if (num >= 0)
                        {
                            ItemObject itemAtIndex = itemRoster.GetItemAtIndex(num);
                            //weapon armor ...won't effect workshop's value 当时木工坊铁匠铺就是因为生产的武器盔甲值钱而成为印钞厂 现在生产这些东西不会影响工坊价值
                            if (itemAtIndex != null && itemAtIndex.IsTradeGood)
                            {
                                int itemPrice = town.GetItemPrice(itemAtIndex, null, false);
                                Inputs_cost += workshopTypeProduction.Inputs[i].Item2 * itemPrice * Speed_Effect * workshopTypeProduction.ConversionSpeed;
                            }
                            else
                            {
                                Inputs_cost = 0;
                                Outputs_profit = 0;
                            }
                        }
                    }
                    for (int i = 0; i < workshopTypeProduction.Outputs.Count; i++)
                    {
                        int item = workshopTypeProduction.Outputs[i].Item2;
                        ItemModifier itemModifier = null;
                        ItemObject randomItem = GetRandomItem(workshopTypeProduction.Outputs[i].Item1, town);
                        if (randomItem != null && randomItem.IsTradeGood)
                        {
                            int itemPrice = town.GetItemPrice(new EquipmentElement(randomItem, itemModifier), null, false);
                            Outputs_profit += itemPrice * item * Speed_Effect * workshopTypeProduction.ConversionSpeed;
                        }
                        //For now,tannery only produce garment,which is not tradegood.but players still get income from it.So i used arvergevalue for it.
                        //皮革厂只生产衣服，但衣服又不是tradegood，但还是有收入，原因不明,只能用平均价值代替了.
                        else if (randomItem.ItemCategory == DefaultItemCategories.Garment)
                        {
                            Outputs_profit += DefaultItemCategories.Garment.AverageValue * item * Speed_Effect * workshopTypeProduction.ConversionSpeed;
                        }
                        else
                        {
                            Inputs_cost = 0;
                            Outputs_profit = 0;
                        }
                    }
                    Profit += Outputs_profit - Inputs_cost;
                }
                //don't know how to handle dynamic eco
                //不知道如何处理动态经济问题
                float economyeffect = 1f;
                Profit = (int) Profit * economyeffect;
                messagestring = messagestring + town.Name.ToString() + workshopType.Name.ToString() + Profit.ToString() + " ";
            }
            //you can change deiscription here according to your language
            InformationManager.DisplayMessage(new InformationMessage("评分标准：当日产物获利-当日原料花费-工资 受动态经济影响 收入并不准确 若需要较稳定的收入 请确保原料充足 如邻近特产村 开厂分数后会下降直到达到动态平衡 当前实时评分统计: " + messagestring));

        }

        public override void SyncData(IDataStore dataStore)
        {

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
            System.Collections.Generic.List<ItemObject> list;
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
        private static System.Collections.Generic.Dictionary<ItemCategory, System.Collections.Generic.List<ItemObject>> itemsInCategory = new System.Collections.Generic.Dictionary<ItemCategory, System.Collections.Generic.List<ItemObject>>();

        //unused codes from TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.WorkshopsCampaignBehavior.DecideBestWorkshopType()
        //private static WorkshopType DecideBestWorkshopType(Settlement currentSettlement, int shopIndex)
        //{
        //    System.Collections.Generic.IDictionary<ItemCategory, float> dictionary = new System.Collections.Generic.Dictionary<ItemCategory, float>();
        //    float num = Campaign.AverageDistanceBetweenTwoTowns * 3f;
        //    foreach (Settlement settlement in Settlement.All)
        //    {
        //        if (settlement.IsVillage)
        //        {
        //            float num2 = settlement.Village.TradeBound.Position2D.Distance(currentSettlement.Position2D);
        //            if (num2 < num)
        //            {
        //                Village component = settlement.GetComponent<Village>();
        //                foreach (ValueTuple<ItemObject, float> valueTuple in component.VillageType.Productions)
        //                {
        //                    ItemCategory itemCategory = valueTuple.Item1.ItemCategory;
        //                    float item = valueTuple.Item2;
        //                    if (itemCategory == DefaultItemCategories.Cow || itemCategory == DefaultItemCategories.Sheep || itemCategory == DefaultItemCategories.Hog)
        //                    {
        //                        itemCategory = DefaultItemCategories.Hides;
        //                    }
        //                    float num3;
        //                    if (currentSettlement == component.TradeBound)
        //                    {
        //                        num3 = 3f;
        //                    }
        //                    else
        //                    {
        //                        num3 = (num - num2) / num;
        //                    }
        //                    num3 *= item;
        //                    float num4;
        //                    if (dictionary.TryGetValue(itemCategory, out num4))
        //                    {
        //                        dictionary[itemCategory] = num4 + num3;
        //                    }
        //                    else
        //                    {
        //                        dictionary.Add(itemCategory, num3);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    float num5 = 0f;
        //    foreach (WorkshopType workshopType in WorkshopType.All)
        //    {
        //        if (!workshopType.IsHidden)
        //        {
        //            float num6 = FindTotalInputDensityScore(currentSettlement, workshopType, dictionary);
        //            num5 += num6;
        //        }
        //    }
        //    float valueNormalized = currentSettlement.Random.GetValueNormalized(shopIndex);
        //    float num7 = num5 * valueNormalized;
        //    WorkshopType result = null;
        //    foreach (WorkshopType workshopType2 in WorkshopType.All)
        //    {
        //        if (!workshopType2.IsHidden)
        //        {
        //            float num8 = FindTotalInputDensityScore(currentSettlement, workshopType2, dictionary);
        //            num7 -= num8;
        //            if (num7 < 0f)
        //            {
        //                result = workshopType2;
        //                break;
        //            }
        //        }
        //    }
        //    return result;
        //}

        //unused codes from TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.WorkshopsCampaignBehavior.FindTotalInputDensityScore()
        //private static float FindTotalInputDensityScore(Settlement bornSettlement, WorkshopType workshop, System.Collections.Generic.IDictionary<ItemCategory, float> productionDict)
        //{
        //    int num = 0;
        //    for (int i = 0; i < bornSettlement.GetComponent<Town>().Workshops.Length; i++)
        //    {
        //        if (bornSettlement.GetComponent<Town>().Workshops[i].WorkshopType == workshop)
        //        {
        //            num++;
        //        }
        //    }
        //    float num2 = 0f;
        //    foreach (WorkshopType.Production production in workshop.Productions)
        //    {
        //        num2 += production.ConversionSpeed;
        //    }
        //    float num3 = 0f;
        //    foreach (WorkshopType.Production production2 in workshop.Productions)
        //    {
        //        foreach (ValueTuple<ItemCategory, int> valueTuple in production2.Inputs)
        //        {
        //            ItemCategory item = valueTuple.Item1;
        //            float num4 = (item != DefaultItemCategories.Hides) ? Campaign.Current.Models.VillageProductionCalculatorModel.CalculateProductionSpeedOfItemCategory(item) : 3f;
        //            float num5 = 0f;
        //            if (productionDict.TryGetValue(item, out num5))
        //            {
        //                num3 += num5 / num4 * (production2.ConversionSpeed / num2);
        //            }
        //        }
        //    }
        //    num3 *= (float)workshop.Frequency * (1f / ((1f + (float)num) * (1f + (float)num) * (1f + (float)num)));
        //    num3 = (float)Math.Pow((double)num3, 0.800000011920929);
        //    return num3;
        //}

    }


















}


