using System;
using XRL.Rules;

namespace XRL.World.Parts
{
	[Serializable]
	public class RandomFlowerCrown : IPart
	{
		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override void Register(GameObject Object)
		{
			Object.RegisterPartEvent(this, "ObjectCreated");
			base.Register(Object);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "ObjectCreated")
			{
				Render render = ParentObject.GetPart<Render>();
				switch (Stat.Random(1, 7))
				{
				case 1:
					render.ColorString = "&R";
					break;
				case 2:
					render.ColorString = "&M";
					break;
				case 3:
					render.ColorString = "&B";
					break;
				case 4:
					render.ColorString = "&C";
					break;
				case 5:
					render.ColorString = "&Y";
					break;
				case 6:
					render.ColorString = "&G";
					break;
				case 7:
					render.ColorString = "&W";
					break;
				}
				if (Stat.Random(0, 1) == 0)
				{
					render.ColorString = render.ColorString.ToLower();
				}
                render.DetailColor = "g";
                ParentObject.DisplayName = render.ColorString + "flower&y crown";
				ParentObject.RemovePart(this);
			}
			return base.FireEvent(E);
		}
	}
}
