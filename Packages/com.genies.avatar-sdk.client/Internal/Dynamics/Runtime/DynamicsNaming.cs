using System;
using UnityEngine;

namespace Genies.Dynamics
{
    /// <summary>
    /// This class provides functionality related to the naming conventions used in the Dynamics system.
    /// Specifically, the naming conventions used for dynamic joints and their hierarchies.
    ///
    /// The naming conventions are documented within this package in the readme file:
    /// + Documentation/Genies Dynamics.md).
    ///
    /// The naming conventions are also documented in the following Notion page:
    /// + https://www.notion.so/geniesinc/Dynamics-Joint-Naming-Conventions-d6f58d3814d447c8b05044912f13b634
    /// </summary>
    public static class DynamicsNaming
    {
        public static readonly string[] SideNames = new string[] { "Left", "Right", "Center" };

        public const string DynamicJointSignifier = "Dyn";

        public static bool ValidateJointName(string jointName)
        {
            // The joint name must start with a side name.
            bool startsWithSideName = false;
            int indexOfStructureName = -1;
            foreach (string sideName in SideNames)
            {
                if (jointName.StartsWith(sideName))
                {
                    startsWithSideName = true;
                    indexOfStructureName = sideName.Length;
                    break;
                }
            }

            if (!startsWithSideName)
            {
                Debug.LogWarning($"The joint name '{jointName}' does not start with a side name.");
                return false;
            }

            // The joint name must contain the dynamic joint signifier at the end of the joint name (before the numeric digit).
            int indexOfDynamicJointSignifier = jointName.IndexOf(DynamicJointSignifier, jointName.Length - (DynamicJointSignifier.Length+1));
            if (indexOfDynamicJointSignifier == -1)
            {
                Debug.LogWarning($"The joint name '{jointName}' does not contain the dynamic joint signifier '{DynamicJointSignifier}'.");
                return false;
            }

            // The joint name must contain a character after the dynamic joint signifier.
            if (indexOfDynamicJointSignifier + DynamicJointSignifier.Length >= jointName.Length)
            {
                Debug.LogWarning($"The joint name '{jointName}' does not contain a character after the dynamic joint signifier '{DynamicJointSignifier}'.");
                return false;
            }

            // The character after the dynamic joint signifier must be a numeric digit.
            if (!char.IsDigit(jointName[indexOfDynamicJointSignifier + DynamicJointSignifier.Length]))
            {
                Debug.LogWarning($"The character after the dynamic joint signifier '{DynamicJointSignifier}' in the joint name '{jointName}' is not a numeric digit.");
                return false;
            }

            // There must be a character before the dynamic joint signifier.
            if (indexOfDynamicJointSignifier == 0)
            {
                Debug.LogWarning($"The joint name '{jointName}' does not contain a character before the dynamic joint signifier '{DynamicJointSignifier}'.");
                return false;
            }

            // The character before the dynamic joint signifier must be a numeric digit.
            if (!char.IsDigit(jointName[indexOfDynamicJointSignifier - 1]))
            {
                Debug.LogWarning($"The character before the dynamic joint signifier '{DynamicJointSignifier}' in the joint name '{jointName}' is not a numeric digit.");
                return false;
            }

            // The joint name must contain a structure name.
            // The structure name must contain only letters.
            // The structure name must be followed by a numeric digit.
            int endIndexOfStructureName = indexOfDynamicJointSignifier - 2;
            if (endIndexOfStructureName < indexOfStructureName)
            {
                Debug.LogWarning($"The joint name '{jointName}' does not contain a structure name.");
                return false;
            }
            string structureName = jointName.Substring(indexOfStructureName, endIndexOfStructureName - indexOfStructureName + 1);
            foreach (char c in structureName)
            {
                if (!char.IsLetter(c))
                {
                    Debug.LogWarning($"The structure name '{structureName}' in the joint name '{jointName}' contains a character that is not a letter.");
                    return false;
                }
            }

            // The dynamic joint signifier must be followed by a single character that is a numeric digit.
            if (indexOfDynamicJointSignifier + DynamicJointSignifier.Length + 1 != jointName.Length)
            {
                Debug.LogWarning($"The joint name '{jointName}' contains characters after the dynamic joint signifier '{DynamicJointSignifier}' that should not be there.");
                return false;
            }

            return true;
        }

        public static string GetStructureNameFromJointName(string jointName)
        {
            // Validate the joint name.
            if (!ValidateJointName(jointName))
            {
                throw new ArgumentException($"The joint name '{jointName}' is not valid.");
            }

            // Parse the joint name to find the structure name.
            // The side (Left, Right, Center) is the first part of the joint name.
            string nameSansSide = jointName;
            foreach (string sideName in SideNames)
            {
                if (jointName.StartsWith(sideName))
                {
                    nameSansSide = jointName.Substring(sideName.Length);
                    break;
                }
            }

            // The structure name is next. To find where it terminates, we find the first occurence of a numeric digit.
            int indexOfFirstDigit = nameSansSide.IndexOfAny("0123456789".ToCharArray());

            return nameSansSide.Substring(0, indexOfFirstDigit);
        }
    }
}
