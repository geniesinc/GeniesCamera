using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UMA.CharacterSystem
{
	[Serializable]
	public class UMAPreset
	{
		public UMAPredefinedDNA PredefinedDNA;
		public DynamicCharacterAvatar.WardrobeRecipeList DefaultWardrobe;
		public DynamicCharacterAvatar.ColorValueList DefaultColors;
	}
}
