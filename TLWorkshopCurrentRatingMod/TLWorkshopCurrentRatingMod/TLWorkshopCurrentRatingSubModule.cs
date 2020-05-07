using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TLWorkshopCurrentRatingMod
{
    public class TLWorkshopCurrentRatingSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
        }
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            InformationManager.DisplayMessage(new InformationMessage("TLWorkshopCurrentRating beta1.3.0.0 is successfully loaded."));
        }
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            bool flag = !(game.GameType is Campaign);
            if (!flag)
            {
                ((CampaignGameStarter)gameStarterObject).AddBehavior(new TLWorkshopCurrentRatingCampaignBehavior());
            }
        }
    }
}
