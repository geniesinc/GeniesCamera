using System;

namespace Genies.Models
{
    public enum BodyType
    {
        none,
        male,
        female,
        nonbinary,
        unified
    }

    public static class BodyTypeExtensions
    {
        public static BodyType FromString(string bodyTypeLabel)
        {
            if (Enum.TryParse(bodyTypeLabel, out BodyType bodyType))
            {
                return bodyType;
            }

            return BodyType.none;
        }

        public static string GetPrefix(this BodyType type)
        {
            switch (type)
            {
                case BodyType.male:
                    return "m_";
                case BodyType.female:
                    return "f_";
                case BodyType.nonbinary:
                    return "nb_";
                case BodyType.unified:
                    return "uh_";
                default:
                    return "";
            }
        }

#if UNITY_EDITOR
        public static string Nicify(this BodyType type)
        {
            return UnityEditor.ObjectNames.NicifyVariableName(type.ToString());
        }

        public static BodyType DetermineBodyTypeFromFileName(string fileName)
        {
            fileName = fileName.ToLower();
            switch (fileName)
            {
                case string a when a.Contains("female"):
                    return BodyType.female;
                case string a when a.Contains("male"):
                    return BodyType.male;
                case string a when a.Contains("nonbinary"):
                    return BodyType.nonbinary;
                case string a when a.Contains("unified"):
                    return BodyType.unified;
                default:
                    return BodyType.none;
            }
        }
#endif
    }
}