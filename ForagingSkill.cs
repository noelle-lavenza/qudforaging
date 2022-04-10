using System;
using System.Collections.Generic;
using System.Linq;
using XRL.Language;
using XRL.UI;
using XRL.Liquids;
using QudForaging.Utilities;
using System.Text;

namespace XRL.World.Parts.Skill
{
	class QudForaging_ForagingSkill : BaseSkill
	{
		public Guid ForageAbilityID = Guid.Empty;

		public override bool AllowStaticRegistration()
		{
			return true;
		}

		public override void Register(GameObject Object)
		{
			Object.RegisterPartEvent(this, "CommandQudForage");
			base.Register(Object);
		}
		public override bool FireEvent(Event E)
		{
			if (E.ID == "CommandQudForage")
			{
				if (ParentObject.IsPlayer())
				{
					return TryForagePlayer();
				}
				return false;
			}
			return base.FireEvent(E);
		}

		public bool TryForagePlayer()
		{
			string objectTypeForZone = ZoneManager.GetObjectTypeForZone(The.Player.CurrentZone.ZoneID);
			string tag = GameObjectFactory.Factory.Blueprints[objectTypeForZone].GetTag("Terrain");
			int depth = 10 - The.Player.CurrentZone.Z;
			MetricsManager.LogInfo($"Depth {depth}, Z {The.Player.CurrentZone.Z}");
			if (depth > 30)
			{
				tag += "_DeepUnderground";
			}
			else if (depth < 0)
			{
				tag += "_Underground";
			}
			else if (depth > 0)
			{
				tag +="_Sky";
			}
			else if (depth == 0)
			{
				tag += "_Surface";
			}
			
			ParentObject.ApplyEffect(new XRL.World.Effects.Foraging(50, tag)); // 50 turns = 1 hour
			return true;
		}
		
		public override bool AddSkill(GameObject GO)
		{
			ForageAbilityID = AddMyActivatedAbility("Forage", "CommandQudForage", "Skill", "You forage in the local area for materials.", "-", null, Toggleable: false, DefaultToggleState: false, ActiveToggle: false, IsAttack: false);
			return base.AddSkill(GO);
		}

		public override bool RemoveSkill(GameObject GO)
		{
			RemoveMyActivatedAbility(ref ForageAbilityID);
			return base.RemoveSkill(GO);
		}
	}
}