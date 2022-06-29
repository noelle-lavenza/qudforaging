using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ConsoleLib.Console;
using QudForaging;
using QudForaging.Utilities;
using XRL.Core;
using XRL.Language;
using XRL.Messages;
using XRL.Rules;
using XRL.UI;
using XRL.World.Parts;

namespace XRL.World.Effects
{
	[Serializable]
	public class Foraging : Effect
	{
		private Foraging()
		{
			base.DisplayName = "Foraging";
		}

		private bool inMenu = false;

		private string ForageTag;
		private int FailureStrikes = 0;

		public Foraging(int Duration, string ForageTag)
			: this()
		{
			this.Duration = Duration;
			this.ForageTag = ForageTag;
		}

		public override string GetDetails()
		{
			return $"Foraging for resources.\nCan be interrupted if damage is taken.";
		}

		public override string GetDescription()
		{
			return "{{leafy|foraging}}";
		}

		public override bool Apply(GameObject Object)
		{
			Object.pBrain.PushGoal(new AI.GoalHandlers.WanderRandomly(Duration));
			Object.ForfeitTurn();
			return base.Apply(Object);
		}

		public override void Expired()
		{
			if (Object.IsPlayer())
			{
				MessageQueue.AddPlayerMessage("You finish foraging.", "&y");
			}	
			base.Expired();
		}

		public override void Remove(GameObject Object)
		{
			Object?.pBrain?.Goals.Peek()?.FailToParent();
			base.Remove(Object);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
			{
				return ID == CommandTakeActionEvent.ID || ID == BeforeRenderEvent.ID;
			}
			return true;
		}

		public override void Register(GameObject Object)
		{
			Object.RegisterEffectEvent(this, "TakeDamage");
			Object.RegisterEffectEvent(this, "AfterMoved");
			Object.RegisterEffectEvent(this, "MoveFailed");
			base.Register(Object);
		}

		public override void Unregister(GameObject Object)
		{
			Object.UnregisterEffectEvent(this, "TakeDamage");
			Object.UnregisterEffectEvent(this, "AfterMoved");
			Object.UnregisterEffectEvent(this, "MoveFailed");
			base.Unregister(Object);
		}

		public override bool Render(RenderEvent E)
		{
			if (base.Duration > 0)
			{
				int num = XRLCore.CurrentFrame % 60;
				if (num > 25 && num < 35)
				{
					E.Tile = null;
					E.RenderString = "F";
					E.ColorString = "&g";
				}
			}
			return true;
		}

		public override bool UseStandardDurationCountdown()
		{
			return true;
		}

		public override bool HandleEvent(CommandTakeActionEvent E)
		{
			inMenu = true;
			if (Object.IsPlayer() && Object.ArePerceptibleHostilesNearby(logSpot: true, popSpot: true, Description: "foraging", IgnoreEasierThan: Options.AutoexploreIgnoreEasyEnemies, IgnoreFartherThan: Options.AutoexploreIgnoreDistantEnemies))
			{
				Object.RemoveEffect(this);
				inMenu = false;
				return false;
			}
			inMenu = false;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeforeRenderEvent E)
		{
			if (!inMenu)
			{
				Keyboard.KeyEvent.WaitOne(20);
				if (Keyboard.kbhit())
				{
					Keys ourKey = Keyboard.getvk(false);
					if (ourKey != Keys.Escape)
					{
						Keyboard.ReverseKeymap.TryGetValue(ourKey, out var unityKey);
						Keyboard.PushKey(unityKey);
					}
					else
					{
						if (Object.IsPlayer())
						{
							MessageQueue.AddPlayerMessage("You stop foraging.", "&y");
						}
						Object.RemoveEffect(this);
						return true;
					}
				}
			}
			return base.HandleEvent(E);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "TakeDamage")
			{
				if ((E.GetParameter("Damage") as Damage).Amount > 0)
				{
					inMenu = true;
					if (!base.Object.IsPlayer() || (Popup.ShowYesNo("You're taking damage, stop foraging?", AllowEscape: true, DialogResult.Yes) == DialogResult.Yes))
					{
						if (Object.IsPlayer())
						{
							MessageQueue.AddPlayerMessage("You stop foraging.", "&y");
						}
						Object.RemoveEffect(this);
						inMenu = false;
						return true;
					}
					inMenu = false;
				}
			}
			else if (E.ID == "AfterMoved")
			{
				if (!Object.CanMoveExtremities(ShowMessage: true))
				{
					Object.RemoveEffect(this);
					if (Object.IsPlayer())
					{
						MessageQueue.AddPlayerMessage("You can't forage in your condition.", "&y");
					}
					return true;
				}
				if (FailureStrikes > 0)
				{
					FailureStrikes--;
					return true;
				}
				DoForageTick();
			}
			else if (E.ID == "MoveFailed")
			{
				FailureStrikes++;
				if (FailureStrikes > 3)
				{
					Object.RemoveEffect(this);
					return true;
				}
			}
			return base.FireEvent(E);
		}

		public void DoForageTick()
		{
			// scale chance to roll with intelligence
			// The.Player.StatMod("Intelligence")

			int intRoll = Stat.Random(1, Math.Max(2, 10 - Object.StatMod("Intelligence")));
			MetricsManager.LogWarning($"Forage tag: \"Forage_Terrain{this.ForageTag}\"; roll: {intRoll}/{Math.Max(2, 10 - Object.StatMod("Intelligence"))}");
			if(intRoll != 1)
			{
				return; // you failed the vibe check
			}
			// we've succeeded, now roll a result from the table
			List<PopulationResult> forageResults = PopulationManager.Generate("Forage_Terrain" + this.ForageTag);
			if (forageResults.Count() <= 0)
			{
				if (Object.IsPlayer())
				{
					MessageQueue.AddPlayerMessage("You forage nothing.", "&y");
				}
				return;
			}
			List<string> foragedItems = new List<string>();
			foreach (PopulationResult forageResult in forageResults)
			{
				MetricsManager.LogWarning($"Foraged {forageResult.Number}x {forageResult.Blueprint} with hint {forageResult.Hint}");
				foragedItems.Add(HandleForageSpawn(forageResult.Blueprint, forageResult.Number, forageResult.Hint));
			}
			if (Object.IsPlayer())
			{
				MessageQueue.AddPlayerMessage($"You forage {Grammar.MakeAndList(foragedItems)}.", "&y");
			}
		}

		public string HandleForageSpawn(string Blueprint, int Number, string Hint) // returns a descriptive string of what was spawned
		{
			if (Hint == "Liquid")
			{
				LiquidVolume tempLiquid = GameObject.create("Water").LiquidVolume;
				tempLiquid.InitialLiquid = Blueprint;
				tempLiquid.Volume = Number;
				tempLiquid.Update(); // to normalise proportions/primary
				Blueprint = tempLiquid.GetLiquidDesignation();
				StringBuilder liquidName = new StringBuilder();
				tempLiquid.AppendLiquidName(liquidName);
				int leftover = Number - Object.GetStorableDrams(Blueprint);
				// excess liquid gets spilled on the floor
				if (leftover <= 0)
				{
					Object.GiveDrams(Number, Blueprint);
				}
				else
				{
					Object.GiveDrams(Number - leftover, Blueprint);
					tempLiquid.Volume = leftover;
					tempLiquid.PourIntoCell(Object.CurrentCell, leftover);
				}
				tempLiquid.ParentObject.Destroy();
				return (Number > 1 ? $"{Grammar.Cardinal(Number)} drams" : "a dram") + " of " + liquidName;
			}
			else
			{
				GameObjectBlueprint blueprint = GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint);
				if (blueprint != null && Number > 0)
				{
					GameObject lastPart = null;
					for (int i=0; i<Number; i++)
					{
						lastPart = GameObjectFactory.Factory.CreateObject(Blueprint);
						Object.ReceiveObject(lastPart);
					}
					return QudForaging_Grammar.NumericalPluralize(lastPart, Number);
				}
			}
			return "nothing";
		}
	}
}