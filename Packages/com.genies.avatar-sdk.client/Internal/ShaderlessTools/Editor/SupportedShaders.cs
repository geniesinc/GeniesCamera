using System.Collections.Generic;

namespace Genies.Components.ShaderlessTools
{
    public static class SupportedShaders
    {
        public static List<string> GroupList => new()
        {
            "Universal Render Pipeline",
            "Shader Graphs",
            "Genies",
            "UnityGLTF"
        };
    }
}
