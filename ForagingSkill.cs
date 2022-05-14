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
					return TryForage();
				}
				return false;
			}
			return base.FireEvent(E);
		}

		public string GetZoneForageTable(Zone CurrentZone)
		{
			string bp_tag = GetZoneForageTableInternal(CurrentZone, "AlternateTerrainName");
			if(bp_tag != null)
			{
				return bp_tag;
			}
			return GetZoneForageTableInternal(CurrentZone, "Terrain");
		}

		public string GetZoneForageTableInternal(Zone CurrentZone, string bp_tag)
		{
			string objectTypeForZone = ZoneManager.GetObjectTypeForZone(CurrentZone.ZoneID);
			GameObjectBlueprint zoneObjectBlueprint = GameObjectFactory.Factory.Blueprints[objectTypeForZone];
			string tag = zoneObjectBlueprint.GetTag(bp_tag);
			int depth = 10 - CurrentZone.Z;
			//MetricsManager.LogInfo($"Depth {depth}, Z {CurrentZone.Z}");
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
			return tag;
		}

		public bool TryForage()
		{
			ParentObject.ApplyEffect(new XRL.World.Effects.Foraging(50, GetZoneForageTable(ParentObject.CurrentZone))); // 50 turns = 1 hour
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