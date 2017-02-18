using Patchwork;
using Game;
using UnityEngine;

namespace TrainerFix.pw
{
    [ModifiesType]
    public class mod_CharacterStats : CharacterStats
    {
        [ModifiesMember("LevelUpTo")]
        public void LevelUpToNew(int level, bool naturalProgression)
        {
            Player component = base.GetComponent<Player>();
            bool flag = !component && !StoredCharacterInfo.RestoringPackedCharacter && GameState.Mode.Option.GetOption(GameOption.BoolOption.AUTO_LEVEL_COMPANIONS);
            int level2 = this.Level;
            while (this.Level < level)
            {
                this.Level++;
                this.GainLevelUpPoints(this.Level);
                if (flag)
                {
                    this.AutoLevelAttributes(this.Level);
                }
            }
            if (level2 != level)
            {
                this.AbilityProgressionTable.AddAutoGrantAbilitiesToCharacter(this);
                if (flag)
                {
                    this.AutoLevelTalents();
                }
                if (naturalProgression)
                {
                    this.OnLevelUp(this, null);
                }
                //this.m_numSkillsTrainedThisLevel = 0;
            }
        }
    }
    
    [ModifiesType]
    public class mod_UISkillTrainingManager : UISkillTrainingManager
    {
        [ModifiesMember("LoadSelectedCharacterInformation")]
        private void LoadSelectedCharacterInformationNew()
        {
            this.LblCharacterName.text = this.m_selectedCharacter.Name();
            this.LblCharacterLvl.text = string.Format(SDK.GUIUtils.GetText(1107), this.m_selectedCharacter.Level);
            this.LblTrainingsThisLevel.text = string.Format(SDK.GUIUtils.GetText(2123), this.m_selectedCharacter.SkillsTrainedThisLevel, this.m_selectedCharacter.Level * 5);
            int num = CharacterStats.ExperienceNeededForLevel(this.m_selectedCharacter.Level);
            int num2 = CharacterStats.ExperienceNeededForNextLevel(this.m_selectedCharacter.Level);
            int num3 = this.m_selectedCharacter.Experience - num;
            int num4 = num2 - num;
            this.LblProgressXP.text = string.Format(SDK.GUIUtils.GetText(291), num3, num4);
            float num5 = (float)num3 / (float)num4;
            Vector3 localScale = this.ProgressSize.transform.localScale;
            localScale.x = num5 * localScale.x;
            this.ProgressLevelXP.transform.localScale = localScale;
        }

        [ModifiesMember("OnTrainButtonClicked")]
        private void OnTrainButtonClickedNew(UISkillTrainingUpgrade upgradeView)
        {
            Game.CharacterStats.SkillType skillType = upgradeView.SkillType;
            float skillPerRankMultiplier = StatsData.Instance.GetSkillPerRankMultiplier(skillType);
            int skillRank = this.m_selectedCharacter.GetSkillRank(skillType);
            int num = skillRank + 1;
            if (num > this.m_trainer.maxSkillLevel)
            {
                UIMessageBox uIMessageBox = UIWindowManager.ShowMessageBox(UIMessageBox.UIDialogButtons.OK, SDK.GUIUtils.GetText(2131), string.Format(SDK.GUIUtils.GetText(2129), this.m_trainer.maxSkillLevel));
                uIMessageBox.ShowWindow();
                return;
            }
            if (this.m_selectedCharacter.SkillsTrainedThisLevel >= this.m_selectedCharacter.Level * 5)
            {
                UIMessageBox uIMessageBox2 = UIWindowManager.ShowMessageBox(UIMessageBox.UIDialogButtons.OK, SDK.GUIUtils.GetText(2131), SDK.GUIUtils.GetText(2132));
                uIMessageBox2.ShowWindow();
                return;
            }
            int num2 = Game.CharacterStats.CopperCostToTrainSkillToNextRank(num);
            Game.PlayerInventory component = Game.GameState.s_playerCharacter.GetComponent<Game.PlayerInventory>();
            if (component == null || component.currencyTotalValue < (float)num2)
            {
                UIMessageBox uIMessageBox3 = UIWindowManager.ShowMessageBox(UIMessageBox.UIDialogButtons.OK, SDK.GUIUtils.GetText(2131), SDK.GUIUtils.GetText(2128));
                uIMessageBox3.ShowWindow();
                return;
            }
            if (Game.TelemetryManager.Instance && Game.Stronghold.Instance)
            {
                Game.TelemetryManager.Instance.QueueEvent_SkyPillarUpgradeUsed(SDK.TelemetryManager.Destination.Developer | SDK.TelemetryManager.Destination.Publisher, Game.Stronghold.Instance.GetUpgradeLocation(StrongholdUpgrade.Type.Weapons_Training), StrongholdUpgrade.Type.Weapons_Training, string.Empty, skillType);
            }
            GlobalAudioPlayer.SPlay(UIAudioList.UIAudioType.TrainSkill);
            int xp = Game.CharacterStats.ExperienceNeededForNextSkillRank(skillRank, skillPerRankMultiplier) - this.m_selectedCharacter.SkillXP[(int)skillType];
            component.RemoveCurrency((float)num2, 1);
            this.m_selectedCharacter.SkillsTrainedThisLevel++;
            this.m_selectedCharacter.CalculateAndApplySkillXP(upgradeView.SkillType, xp, null, 0, 0);
            this.Reload();
        }
    }

    [ModifiesType]
    public class mod_UISkillTrainingUpgrade : UISkillTrainingUpgrade
    {
        [ModifiesMember("LoadSkill")]
        public void LoadSkillNew(Game.CharacterStats character, Game.CharacterStats.SkillType skillToLoad, int trainerMaxRank)
        {
            this.m_trainFailReason = string.Empty;
            this.m_skillType = skillToLoad;
            this.LblName.text = Game.GUIUtils.GetSkillTypeString(this.m_skillType);
            bool flag = false;
            float skillPerRankMultiplier = StatsData.Instance.GetSkillPerRankMultiplier(this.m_skillType);
            int skillRank = character.GetSkillRank(this.m_skillType);
            this.m_skillXP = character.SkillXP[(int)this.m_skillType];
            this.m_nextRankXP = Game.CharacterStats.ExperienceNeededForNextSkillRank(skillRank, skillPerRankMultiplier);
            this.LblRank.text = string.Format(SDK.GUIUtils.GetText(291), skillRank, trainerMaxRank);
            this.LblRank.color = UIGlobalColor.FetchColor(UIGlobalColor.ColorLookupID.TEXT_NORMAL_DEFAULT);
            this.ProgressRank.fillAmount = (float)this.m_skillXP / (float)this.m_nextRankXP;
            if (skillRank + 1 > trainerMaxRank)
            {
                this.m_trainFailReason = string.Format(SDK.GUIUtils.GetText(2129), trainerMaxRank);
                this.LblRank.color = UIGlobalColor.FetchColor(UIGlobalColor.ColorLookupID.TEXT_UNAFFORDABLE);
                flag = true;
            }
            if (character.SkillsTrainedThisLevel >= character.Level * 5)
            {
                this.m_trainFailReason = SDK.GUIUtils.GetText(2132);
            }
            int num = Game.CharacterStats.CopperCostToTrainSkillToNextRank(skillRank + 1);
            Game.Player s_playerCharacter = Game.GameState.s_playerCharacter;
            if (s_playerCharacter == null)
            {
                return;
            }
            Game.PlayerInventory component = s_playerCharacter.GetComponent<Game.PlayerInventory>();
            if (component == null)
            {
                return;
            }
            bool flag2 = component.currencyTotalValue >= (float)num;
            this.CostView.ShowCost(num, flag2);
            if (!flag2)
            {
                this.m_trainFailReason = SDK.GUIUtils.GetText(2128);
            }
            this.ButtonMain.IsEnabled = (this.m_trainFailReason.Length == 0);
            if (!flag && !this.ButtonMain.IsEnabled)
            {
                this.LblRank.color = UIGlobalColor.FetchColor(UIGlobalColor.ColorLookupID.TEXT_NORMAL_DISABLED);
            }
            if (this.m_isHovered && this.m_trainFailReason.Length != 0)
            {
                this.OnTrainSkillHovered(this.ButtonTrain.gameObject, true);
            }
        }

    }

}
