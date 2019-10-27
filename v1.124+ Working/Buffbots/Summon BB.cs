/* Summon Buffbot Command - Created By Deathwish, with a BIG THANKS to geshi for his help!
 * Version 1.0 (13/07/2010) For the use of all Dol Members.
 * 
 * Please share any updates or changes.
 * 
 * This script will summon a Buffbot for the cost of 5bps, that lasts 30 seconds.
 * This script contains everything you need to run the script.
 * 
 * How to use: Place script into your scripts folder.
 * InGame Use: /bb to summon (5 bps is needed to summon or you cant summon the Buffbot)
 * 
 * To change Buffbots name guild etc see line 204.
 * (I am not the creator of the buffbot script i have added to this script,
 * its only there for people that dont have a working bb and to make life easier for people that cant use C#!)
 * 
 * Update V1.1 (27/07/10)
 * Now summon buffbot will load in rvr zones, also remove the loading up error
 * 
 * Updated V1.2 (02/08/10)
 * Added a timer for 30 sec so player cant abuse the script.
 * 
 * Added check to to prevent recasting of buffs on buffed player
 * Removed the Bounty Cost
*/



#region

using System;
using System.Collections;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

#endregion

namespace DOL.GS.Commands
{
    [Cmd(
        "&bb",
        ePrivLevel.Player, // Set to player.
        "/bb - To Summon a Buffbot for the cost of 50bps")]
    public class SummonbbCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        [ScriptLoadedEvent]
        public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            Spell load;
            load = BuffBotSpell;
        }

        #region Command Timer

        public const string SummonBuff = "SummonBuff";

        public void OnCommand(GameClient client, string[] args)
        {
            var player = client.Player;
            var buffTick = player.TempProperties.getProperty(SummonBuff, 0L);
            var changeTime = player.CurrentRegion.Time - buffTick;
            if (changeTime < 30000)
            {
                player.Out.SendMessage(
                    "You must wait " + ((30000 - changeTime)/1000) + " more second to attempt to use this command!",
                    eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }
            player.TempProperties.setProperty(SummonBuff, player.CurrentRegion.Time);

            #endregion Command timer
            
            #region Command spell Loader   
		if (client.Player.BountyPoints >= 0) // how many bps are need to summon the buffbot
		{

            var line = new SpellLine("BuffBotCast", "BuffBot Cast", "unknown", false);
            var spellHandler = ScriptMgr.CreateSpellHandler(client.Player, BuffBotSpell, line);
            if (spellHandler != null)
                spellHandler.StartSpell(client.Player);
			client.Player.RemoveBountyPoints(0); // removes the amount of bps from the player
            client.Player.Out.SendMessage("You have summoned a Buffbot!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            client.Player.SaveIntoDatabase(); // saves new amount of bps
            client.Player.Out.SendUpdatePlayer(); // updates players bp
		}

            #endregion command spell loader
			else client.Player.Out.SendMessage("You don't have enough Bounty Pounts to summon a Buffbot!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        #region Spell

        protected static Spell MBuffbotSpell;

        public static Spell BuffBotSpell
        {
            get
            {
                if (MBuffbotSpell == null)
                {
                    var spell = new DBSpell {CastTime = 0, ClientEffect = 0, Duration = 15};
                    spell.Description = "Summons a Buffbot to your location for " + spell.Duration + " seconds.";
                    spell.Name = "BuffBot Spell";
                    spell.Type = "Summon A Buffbot";
                    spell.Range = 0;
                    spell.SpellID = 121232;
                    spell.Target = "Self";
                    spell.Value = BuffbotTemplate.TemplateId;
                    MBuffbotSpell = new Spell(spell, 1);
                    SkillBase.GetSpellList(GlobalSpellsLines.Item_Effects).Add(MBuffbotSpell);
                }
                return MBuffbotSpell;
            }
        }

        #endregion

        #region Npc

        protected static NpcTemplate MBuffbotTemplate;

        public static NpcTemplate BuffbotTemplate
        {
            get
            {
                if (MBuffbotTemplate == null)
                {
                    MBuffbotTemplate = new NpcTemplate();
                    MBuffbotTemplate.Flags += (byte) GameNPC.eFlags.GHOST + (byte) GameNPC.eFlags.PEACE;
                    MBuffbotTemplate.Name = "Buffbot";
                    MBuffbotTemplate.ClassType = "DOL.GS.Scripts.SummonedBuffbot";
                    MBuffbotTemplate.Model = "50";
                    MBuffbotTemplate.TemplateId = 93049;
                    NpcTemplateMgr.AddTemplate(MBuffbotTemplate);
                }
                return MBuffbotTemplate;
            }
        }

        #endregion
    }
}

namespace DOL.GS.Scripts
{
    public class SummonedBuffbot : GameNPC
    {
        
        private const bool BuffsPlayerPet = true;
        private Queue _mBuffs = new Queue();

        public override int Concentration
        {
            get
            {
                return 10000;
            }
        }

        public override int Mana
        {
            get
            {
                return 10000;
            }
        }

        public override bool AddToWorld()
        {
            switch (Realm)
            {
                case eRealm.Albion:Model = 10;break;
                case eRealm.Hibernia: Model = 307;break;
                case eRealm.Midgard:Model = 158;break;
                case eRealm.None: Model = 10;break;
            }
           
            GuildName = "Powered By Dawn Of Light";
            Realm = eRealm.None;
            return base.AddToWorld();
        }

        public void BuffPlayer(GamePlayer player, Spell spell, SpellLine spellLine)
        {
            if (_mBuffs == null)
            {
                _mBuffs = new Queue();
            }

            _mBuffs.Enqueue(new Container(spell, spellLine, player));

            // don't forget his pet !
            if (player.ControlledBrain != null)
            {
                if (player.ControlledBrain.Body != null)
                {
                    _mBuffs.Enqueue(new Container(spell, spellLine, player.ControlledBrain.Body));
                }
            }

            CastBuffs();
        }

        public void CastBuffs()
        {
            while (_mBuffs.Count > 0)
            {
                var con = (Container) _mBuffs.Dequeue();

                var spellHandler = ScriptMgr.CreateSpellHandler(this, con.Spell, con.SpellLine);

                if (spellHandler != null)
                {
                    spellHandler.StartSpell(con.Target);
                }
            }
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;
            if (player.InCombat)
            {
                player.Out.SendMessage("Buffbot says \"stop your combat if you want me to buff you!\"", eChatType.CT_Say,
                    eChatLoc.CL_ChatWindow);
                return false;
            }

            if (GetDistanceTo(player) > WorldMgr.INTERACT_DISTANCE)
            {
                player.Out.SendMessage("You are too far away " + GetName(0, false) + ".", eChatType.CT_System,
                    eChatLoc.CL_SystemWindow);
                return false;
            }


            TurnTo(player, 3000);
            if (player.CharacterClass.ClassType == eClassType.ListCaster)
            {
                if (!player.HasEffect(CasterMerchBaseAfBuff))
                    BuffPlayer(player, CasterMerchBaseAfBuff, MerchBaseSpellLine);
                if (!player.HasEffect(CasterMerchStrBuff)) BuffPlayer(player, CasterMerchStrBuff, MerchBaseSpellLine);
                if (!player.HasEffect(CasterMerchDexBuff)) BuffPlayer(player, CasterMerchDexBuff, MerchBaseSpellLine);
                if (!player.HasEffect(CasterMerchConBuff)) BuffPlayer(player, CasterMerchConBuff, MerchBaseSpellLine);
                if (!player.HasEffect(CasterMerchSpecAfBuff))
                    BuffPlayer(player, CasterMerchSpecAfBuff, MerchSpecSpellLine);
                if (!player.HasEffect(CasterMerchStrConBuff))
                    BuffPlayer(player, CasterMerchStrConBuff, MerchSpecSpellLine);
                if (!player.HasEffect(CasterMerchDexQuiBuff))
                    BuffPlayer(player, CasterMerchDexQuiBuff, MerchSpecSpellLine);
                if (!player.HasEffect(CasterMerchAcuityBuff))
                    BuffPlayer(player, CasterMerchAcuityBuff, MerchSpecSpellLine);
                if (!player.HasEffect(MerchHasteBuff)) BuffPlayer(player, MerchHasteBuff, MerchSpecSpellLine);
            }
            else
            {
                if (!player.HasEffect(MerchBaseAfBuff)) BuffPlayer(player, MerchBaseAfBuff, MerchBaseSpellLine);
                if (!player.HasEffect(MerchStrBuff)) BuffPlayer(player, MerchStrBuff, MerchBaseSpellLine);
                if (!player.HasEffect(MerchDexBuff)) BuffPlayer(player, MerchDexBuff, MerchBaseSpellLine);
                if (!player.HasEffect(MerchConBuff)) BuffPlayer(player, MerchConBuff, MerchBaseSpellLine);
                if (!player.HasEffect(MerchSpecAfBuff)) BuffPlayer(player, MerchSpecAfBuff, MerchSpecSpellLine);
                if (!player.HasEffect(MerchStrConBuff)) BuffPlayer(player, MerchStrConBuff, MerchSpecSpellLine);
                if (!player.HasEffect(MerchDexQuiBuff)) BuffPlayer(player, MerchDexQuiBuff, MerchSpecSpellLine);
                if (!player.HasEffect(MerchHasteBuff)) BuffPlayer(player, MerchHasteBuff, MerchSpecSpellLine);
            }

            if (!player.InCombat)
            {
                player.Health = MaxHealth;
                if (player.CharacterClass.ID != (int) eCharacterClass.MaulerAlb &&
                    player.CharacterClass.ID != (int) eCharacterClass.MaulerHib &&
                    player.CharacterClass.ID != (int) eCharacterClass.MaulerMid &&
                    player.CharacterClass.ID != (int) eCharacterClass.Vampiir)
                {
                    player.Mana = MaxMana;
                }
            }

            return true;
        }

        public class Container
        {
            private Spell m_spell;

            public Spell Spell
            {
                get { return m_spell; }
            }

            private SpellLine m_spellLine;

            public SpellLine SpellLine
            {
                get { return m_spellLine; }
            }

            private GameLiving m_target;

            public GameLiving Target
            {
                get { return m_target; }
                set { m_target = value; }
            }

            public Container(Spell spell, SpellLine spellLine, GameLiving target)
            {
                m_spell = spell;
                m_spellLine = spellLine;
                m_target = target;
            }
        }

        #region SpellCasting

        private static SpellLine _mMerchBaseSpellLine;
        private static SpellLine _mMerchSpecSpellLine;
        private static SpellLine _mMerchOtherSpellLine;

        /// <summary>
        ///     Spell line used by Merchs
        /// </summary>
        public static SpellLine MerchBaseSpellLine
        {
            get
            {
                if (_mMerchBaseSpellLine == null)
                    _mMerchBaseSpellLine = new SpellLine("MerchBaseSpellLine", "BuffMerch Spells", "unknown", true);

                return _mMerchBaseSpellLine;
            }
        }

        public static SpellLine MerchSpecSpellLine
        {
            get
            {
                if (_mMerchSpecSpellLine == null)
                    _mMerchSpecSpellLine = new SpellLine("MerchSpecSpellLine", "BuffMerch Spells", "unknown", false);

                return _mMerchSpecSpellLine;
            }
        }

        public static SpellLine MerchOtherSpellLine
        {
            get
            {
                if (_mMerchOtherSpellLine == null)
                    _mMerchOtherSpellLine = new SpellLine("MerchOtherSpellLine", "BuffMerch Spells", "unknown", true);

                return _mMerchOtherSpellLine;
            }
        }

        private static Spell _mBaseaf;
        private static Spell _mBasestr;
        private static Spell _mBasecon;
        private static Spell _mBasedex;
        private static Spell _mStrcon;
        private static Spell _mDexqui;
        private static Spell _mAcuity;
        private static Spell _mSpecaf;
        private static Spell _mCasterbaseaf;
        private static Spell _mCasterbasestr;
        private static Spell _mCasterbasecon;
        private static Spell _mCasterbasedex;
        private static Spell _mCasterstrcon;
        private static Spell _mCasterdexqui;
        private static Spell _mCasteracuity;
        private static Spell _mCasterspecaf;
        private static Spell _mHaste;

        #region Spells

        /// <summary>
        ///     Merch Base AF buff (VERIFIED)
        /// </summary>
        public static Spell MerchBaseAfBuff
        {
            get
            {
                if (_mBaseaf == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1467,
                        Icon = 1467,
                        Value = 78,
                        Name = "Armor of the Realm",
                        Description =
                            "Adds to the recipient's Armor Factor (AF) resulting in better protection againts some forms of attack. It acts in addition to any armor the target is wearing.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 88001,
                        Target = "Realm",
                        Type = "ArmorFactorBuff",
                        EffectGroup = 1
                    };

                    //Effective buff 58

                    _mBaseaf = new Spell(spell, 50);
                }
                return _mBaseaf;
            }
        }

        /// <summary>
        ///     Merch Caster Base AF buff (VERIFIED)
        /// </summary>
        public static Spell CasterMerchBaseAfBuff
        {
            get
            {
                if (_mCasterbaseaf == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1467,
                        Icon = 1467,
                        Value = 58,
                        Name = "Armor of the Realm",
                        Description =
                            "Adds to the recipient's Armor Factor (AF) resulting in better protection againts some forms of attack. It acts in addition to any armor the target is wearing.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 89001,
                        Target = "Realm",
                        Type = "ArmorFactorBuff",
                        EffectGroup = 1
                    };

                    //Effective buff 58

                    _mCasterbaseaf = new Spell(spell, 50);
                }
                return _mCasterbaseaf;
            }
        }

        /// <summary>
        ///     Merch Base Str buff (VERIFIED)
        /// </summary>
        public static Spell MerchStrBuff
        {
            get
            {
                if (_mBasestr == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1457,
                        Icon = 1457,
                        Value = 74,
                        Name = "Strength of the Realm",
                        Description = "Increases target's Strength.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 88002,
                        Target = "Realm",
                        Type = "StrengthBuff",
                        EffectGroup = 4
                    };

                    //effective buff 55

                    _mBasestr = new Spell(spell, 50);
                }
                return _mBasestr;
            }
        }

        /// <summary>
        ///     Merch Caster Base Str buff (VERIFIED)
        /// </summary>
        public static Spell CasterMerchStrBuff
        {
            get
            {
                if (_mCasterbasestr == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1457,
                        Icon = 1457,
                        Value = 55,
                        Name = "Strength of the Realm",
                        Description = "Increases target's Strength.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 89002,
                        Target = "Realm",
                        Type = "StrengthBuff",
                        EffectGroup = 4
                    };

                    //effective buff 55

                    _mCasterbasestr = new Spell(spell, 50);
                }
                return _mCasterbasestr;
            }
        }

        /// <summary>
        ///     Merch Base Con buff (VERIFIED)
        /// </summary>
        public static Spell MerchConBuff
        {
            get
            {
                if (_mBasecon == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1486,
                        Icon = 1486,
                        Value = 74,
                        Name = "Fortitude of the Realm",
                        Description = "Increases target's Constitution.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 88003,
                        Target = "Realm",
                        Type = "ConstitutionBuff",
                        EffectGroup = 201
                    };

                    //effective buff 55

                    _mBasecon = new Spell(spell, 50);
                }
                return _mBasecon;
            }
        }

        /// <summary>
        ///     Merch Caster Base Con buff (VERIFIED)
        /// </summary>
        public static Spell CasterMerchConBuff
        {
            get
            {
                if (_mCasterbasecon == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1486,
                        Icon = 1486,
                        Value = 55,
                        Name = "Fortitude of the Realm",
                        Description = "Increases target's Constitution.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 89003,
                        Target = "Realm",
                        Type = "ConstitutionBuff",
                        EffectGroup = 201
                    };

                    //effective buff 55

                    _mCasterbasecon = new Spell(spell, 50);
                }
                return _mCasterbasecon;
            }
        }

        /// <summary>
        ///     Merch Base Dex buff (VERIFIED)
        /// </summary>
        public static Spell MerchDexBuff
        {
            get
            {
                if (_mBasedex == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1476,
                        Icon = 1476,
                        Value = 74,
                        Name = "Dexterity of the Realm",
                        Description = "Increases Dexterity for a character.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 88004,
                        Target = "Realm",
                        Type = "DexterityBuff",
                        EffectGroup = 202
                    };

                    //effective buff 55

                    _mBasedex = new Spell(spell, 50);
                }
                return _mBasedex;
            }
        }

        /// <summary>
        ///     Merch Caster Base Dex buff (VERIFIED)
        /// </summary>
        public static Spell CasterMerchDexBuff
        {
            get
            {
                if (_mCasterbasedex == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1476,
                        Icon = 1476,
                        Value = 55,
                        Name = "Dexterity of the Realm",
                        Description = "Increases Dexterity for a character.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 89004,
                        Target = "Realm",
                        Type = "DexterityBuff",
                        EffectGroup = 202
                    };

                    //effective buff 55

                    _mCasterbasedex = new Spell(spell, 50);
                }
                return _mCasterbasedex;
            }
        }

        /// <summary>
        ///     Merch Spec Str/Con buff (VERIFIED)
        /// </summary>
        public static Spell MerchStrConBuff
        {
            get
            {
                if (_mStrcon == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1517,
                        Icon = 1517,
                        Value = 114,
                        Name = "Might of the Realm",
                        Description = "Increases Str/Con for a character",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 88005,
                        Target = "Realm",
                        Type = "StrengthConstitutionBuff",
                        EffectGroup = 204
                    };

                    //effective buff 85

                    _mStrcon = new Spell(spell, 50);
                }
                return _mStrcon;
            }
        }

        /// <summary>
        ///     Merch Caster Spec Str/Con buff (VERIFIED)
        /// </summary>
        public static Spell CasterMerchStrConBuff
        {
            get
            {
                if (_mCasterstrcon == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1517,
                        Icon = 1517,
                        Value = 85,
                        Name = "Might of the Realm",
                        Description = "Increases Str/Con for a character",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 89005,
                        Target = "Realm",
                        Type = "StrengthConstitutionBuff",
                        EffectGroup = 204
                    };

                    //effective buff 85

                    _mCasterstrcon = new Spell(spell, 50);
                }
                return _mCasterstrcon;
            }
        }

        /// <summary>
        ///     Merch Spec Dex/Qui buff (VERIFIED)
        /// </summary>
        public static Spell MerchDexQuiBuff
        {
            get
            {
                if (_mDexqui == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1526,
                        Icon = 1526,
                        Value = 114,
                        Name = "Deftness of the Realm",
                        Description = "Increases Dexterity and Quickness for a character.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 88006,
                        Target = "Realm",
                        Type = "DexterityQuicknessBuff",
                        EffectGroup = 203
                    };

                    //effective buff 85

                    _mDexqui = new Spell(spell, 50);
                }
                return _mDexqui;
            }
        }

        /// <summary>
        ///     Merch Caster Spec Dex/Qui buff (VERIFIED)
        /// </summary>
        public static Spell CasterMerchDexQuiBuff
        {
            get
            {
                if (_mCasterdexqui == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1526,
                        Icon = 1526,
                        Value = 85,
                        Name = "Deftness of the Realm",
                        Description = "Increases Dexterity and Quickness for a character.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 89006,
                        Target = "Realm",
                        Type = "DexterityQuicknessBuff",
                        EffectGroup = 203
                    };

                    //effective buff 85

                    _mCasterdexqui = new Spell(spell, 50);
                }
                return _mCasterdexqui;
            }
        }

        /// <summary>
        ///     Merch Spec Acuity buff (VERIFIED)
        /// </summary>
        public static Spell MerchAcuityBuff
        {
            get
            {
                if (_mAcuity == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1538,
                        Icon = 1538,
                        Value = 96,
                        Name = "Acuity of the Realm",
                        Description = "Increases Acuity (casting attribute) for a character.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 88007,
                        Target = "Realm",
                        Type = "AcuityBuff",
                        EffectGroup = 200
                    };

                    //effective buff 72;

                    _mAcuity = new Spell(spell, 50);
                }
                return _mAcuity;
            }
        }

        /// <summary>
        ///     Merch Caster Spec Acuity buff (VERIFIED)
        /// </summary>
        public static Spell CasterMerchAcuityBuff
        {
            get
            {
                if (_mCasteracuity == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1538,
                        Icon = 1538,
                        Value = 72,
                        Name = "Acuity of the Realm",
                        Description = "Increases Acuity (casting attribute) for a character.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 89007,
                        Target = "Realm",
                        Type = "AcuityBuff",
                        EffectGroup = 200
                    };

                    //effective buff 72;

                    _mCasteracuity = new Spell(spell, 50);
                }
                return _mCasteracuity;
            }
        }

        /// <summary>
        ///     Merch Spec Af buff (VERIFIED)
        /// </summary>
        public static Spell MerchSpecAfBuff
        {
            get
            {
                if (_mSpecaf == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1506,
                        Icon = 1506,
                        Value = 90,
                        Name = "Armor of the Realm",
                        Description =
                            "Adds to the recipient's Armor Factor (AF), resulting in better protection against some forms of attack. It acts in addition to any armor the target is wearing.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 88014,
                        Target = "Realm",
                        Type = "ArmorFactorBuff",
                        EffectGroup = 2
                    };

                    //effective buff 67

                    _mSpecaf = new Spell(spell, 50);
                }
                return _mSpecaf;
            }
        }

        /// <summary>
        ///     Merch Caster Spec Af buff (VERIFIED)
        /// </summary>
        public static Spell CasterMerchSpecAfBuff
        {
            get
            {
                if (_mCasterspecaf == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 1506,
                        Icon = 1506,
                        Value = 67,
                        Name = "Armor of the Realm",
                        Description =
                            "Adds to the recipient's Armor Factor (AF), resulting in better protection against some forms of attack. It acts in addition to any armor the target is wearing.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 89014,
                        Target = "Realm",
                        Type = "ArmorFactorBuff",
                        EffectGroup = 2
                    };

                    //effective buff 67

                    _mCasterspecaf = new Spell(spell, 50);
                }
                return _mCasterspecaf;
            }
        }

        /// <summary>
        ///     Merch Haste buff (VERIFIED)
        /// </summary>
        public static Spell MerchHasteBuff
        {
            get
            {
                if (_mHaste == null)
                {
                    var spell = new DBSpell
                    {
                        AllowAdd = false,
                        CastTime = 0,
                        Concentration = 1,
                        ClientEffect = 407,
                        Icon = 407,
                        Value = 15,
                        Name = "Haste of the Realm",
                        Description = "Increases the target's combat speed.",
                        Range = WorldMgr.VISIBILITY_DISTANCE,
                        SpellID = 88010,
                        Target = "Realm",
                        Type = "CombatSpeedBuff",
                        EffectGroup = 100
                    };


                    _mHaste = new Spell(spell, 50);
                }
                return _mHaste;
            }
        }

        #endregion Spells

        #endregion SpellCasting
    }
}

#region Summon 

namespace DOL.GS.Spells
{
    [SpellHandler("Summon A Buffbot")]
    public class SummonBuffbotSpellHandler : SpellHandler
    {
        protected GameNPC Npc;

        public SummonBuffbotSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            var template = NpcTemplateMgr.GetTemplate((int) Spell.Value);

            base.ApplyEffectOnTarget(target, effectiveness);

            if (template.ClassType == "")
                Npc = new GameNPC();
            else
            {
                try
                {
                    Npc = new GameNPC();
                    Npc = (GameNPC) Assembly.GetAssembly(typeof (GameServer)).CreateInstance(template.ClassType, false);
                }
                catch (Exception e)
                {
                }
                if (Npc == null)
                {
                    try
                    {
                        Npc = (GameNPC) Assembly.GetExecutingAssembly().CreateInstance(template.ClassType, false);
                    }
                    catch (Exception e)
                    {
                    }
                }
                if (Npc == null)
                {
                    MessageToCaster("There was an error creating an instance of " + template.ClassType + "!",
                        eChatType.CT_System);
                    return;
                }
                Npc.LoadTemplate(template);
            }
           
            int x, y;
            Caster.GetSpotFromHeading(64, out x, out y);
            Npc.X = x;
            Npc.Y = y;
            Npc.Z = Caster.Z;
            Npc.CurrentRegion = Caster.CurrentRegion;
            Npc.Heading = (ushort) ((Caster.Heading + 2048)%4096);
            Npc.Realm = Caster.Realm;
            Npc.CurrentSpeed = 0;
            Npc.Level = Caster.Level;
            Npc.Name = Caster.Name + " Buffbot " + "";
            Npc.SetOwnBrain(new BlankBrain());
            Npc.AddToWorld();
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (Npc != null)
                Npc.Delete();
            return base.OnEffectExpires(effect, noMessages);
        }

        public override bool IsOverwritable(GameSpellEffect compare)
        {
            return false;
        }
    }
}

#endregion