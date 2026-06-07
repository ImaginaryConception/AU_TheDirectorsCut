using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal abstract class ISection
	{
		public readonly string name = "";
		public Vector2 scrollVector;

		public ISection(string name)
		{
			this.name = name;
		}

		public abstract void Render();
	}
}