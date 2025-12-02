using System;
using System.Collections.Generic;
using GUnityGLTF.Plugins;
using UnityEditor;
using UnityEngine;

namespace GUnityGLTF
{
	internal static class RegisteredImportPlugins
	{
		internal static readonly List<GLTFImportPlugin> Plugins = new List<GLTFImportPlugin>();

		[InitializeOnLoadMethod]
		public static void Init()
		{
		}

		private static void OnAfterGUI(GltfSettingsProvider obj)
		{
		}
	}
}
