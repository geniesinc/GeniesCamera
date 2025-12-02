#!/bin/zsh

# for post processing the auto generated swaggerhub files
# because the swaggerhub output file generation is inconsistent
# for Genies Devkit API - updates to existing SDKServices

# run this in ~/genies-shared-unity-components/com.genies.components.services/Runtime/GeneratedCSharpApis

UNITY_VERSION="2022.3.32f1"
PROJECT_NAME="Generate-Meta-Files"


mv tmp/src/Genies.SDKServices/Api/* Genies.SDKServices/Api/
mv tmp/src/Genies.SDKServices/Client/* Genies.SDKServices/Client/
mv tmp/src/Genies.SDKServices/Model/* Genies.SDKServices/Model/

rm -rf tmp


# Find the Unity executable based on the OS
# (because this file is bin/zsh it won't work on windows, but keeping this if we want cross-platform support later)
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    UNITY_PATH="/opt/Unity/Editor/$UNITY_VERSION/Unity"
    PROJECT_PATH="$HOME/Documents/$PROJECT_NAME"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    UNITY_PATH="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"
    PROJECT_PATH="$HOME/Documents/$PROJECT_NAME"
elif [[ "$OSTYPE" == "cygwin"* || "$OSTYPE" == "msys"* ]]; then
    UNITY_PATH="C:/Program Files/Unity/Editor/$UNITY_VERSION/Unity.exe"
    PROJECT_PATH="$USERPROFILE/Documents/$PROJECT_NAME"
else
    echo "Unsupported OS type: $OSTYPE"
    exit 1
fi

# Check if path to Unity exists
if [ ! -f "$UNITY_PATH" ]; then
    echo "Unity executable not found at $UNITY_PATH"
    exit 1
fi

# Create empty Unity project if there isn't already one
if [ ! -d "$PROJECT_PATH" ]; then
    echo "Creating a new Unity project at $PROJECT_PATH..."
    "$UNITY_PATH" -batchmode -nographics -quit -createProject "$PROJECT_PATH"
fi

# Get the absolute path of the services package (2 levels up from this folder)
SCRIPT_DIR=$(realpath "$(dirname "$0")")
PARENT_DIR=$(realpath "$SCRIPT_DIR/..")
GRANDPARENT_DIR=$(realpath "$PARENT_DIR/..")
echo "Script directory: $SCRIPT_DIR"
echo "Parent directory: $PARENT_DIR"
echo "Grandparent directory: $GRANDPARENT_DIR"

# Create/update the manifest.json file in project to include the local services package and scoped registries
MANIFEST_PATH="$PROJECT_PATH/Packages/manifest.json"
echo "Updating manifest at $MANIFEST_PATH"

# Create a temporary file for the new manifest content
TMP_MANIFEST=$(mktemp)

cat > "$TMP_MANIFEST" <<EOL
{
  "dependencies": {
    "com.genies.services": "file:$GRANDPARENT_DIR",
    "com.unity.addressables": "1.19.19",
    "com.unity.collab-proxy": "2.5.2",
    "com.unity.ide.rider": "3.0.34",
    "com.unity.ide.visualstudio": "2.0.22",
    "com.unity.ide.vscode": "1.2.5",
    "com.unity.render-pipelines.universal": "14.0.11",
    "com.unity.test-framework": "1.1.33",
    "com.unity.textmeshpro": "3.0.8",
    "com.unity.timeline": "1.7.6",
    "com.unity.ugui": "1.0.0",
    "com.unity.visualscripting": "1.9.4",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.cloth": "1.0.0",
    "com.unity.modules.director": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.terrainphysics": "1.0.0",
    "com.unity.modules.tilemap": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.umbra": "1.0.0",
    "com.unity.modules.unityanalytics": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequestassetbundle": "1.0.0",
    "com.unity.modules.unitywebrequestaudio": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0",
    "com.unity.modules.unitywebrequestwww": "1.0.0",
    "com.unity.modules.vehicles": "1.0.0",
    "com.unity.modules.video": "1.0.0",
    "com.unity.modules.vr": "1.0.0",
    "com.unity.modules.wind": "1.0.0",
    "com.unity.modules.xr": "1.0.0"
  },
  "scopedRegistries": [
    {
      "name": "Genies Internal",
      "url": "https://npm-internal.genies.com/",
      "scopes": [
        "com.genies"
      ]
    },
    {
      "name": "Open UPM",
      "url": "https://package.openupm.com/",
      "scopes": [
        "com.coffee.softmask-for-ugui",
        "com.coffee.ui-effect",
        "com.coffee.ui-particle",
        "com.coffee.unmask"
      ]
    }
  ]
}
EOL

# Replace the manifest file
mv "$TMP_MANIFEST" "$MANIFEST_PATH"
echo "Manifest updated."

# Trigger Unity to generate meta files
echo "Triggering Unity to resolve packages and set API compatibility level..."
"$UNITY_PATH" -batchmode -nographics -ignorecompilererrors -projectPath "$PROJECT_PATH" -executeMethod UnityEditor.AssetDatabase.Refresh -quit

echo "Script execution completed." 