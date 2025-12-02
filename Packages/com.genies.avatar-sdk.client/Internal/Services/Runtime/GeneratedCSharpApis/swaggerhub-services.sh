#!/bin/zsh

# for post processing the auto generated swaggerhub files
# because the swaggerhub output file generation is inconsistent
# this script will not work on windows (needs to be bin/bash)

# run this in ~/genies-unity-packages/com.genies.services/Runtime/GeneratedCSharpApis

set -e  # Exit immediately if a command exits with a non-zero status.
set -u  # Treat unset variables as an error and exit immediately.

UNITY_VERSION="2022.3.32f1"
PROJECT_NAME="Generate-Meta-Files"

# Define source and target directories
src_dir="tmp/src/Genies.Services"
dest_dir="."

# Check if source directories exist
if [ ! -d "$src_dir" ]; then
    echo "Source directory $src_dir does not exist."
    exit 1
fi

# Update all Api/ generated files except the MobilecmsApi.cs and CmsApi.cs file
if [ -d "$src_dir/Api" ]; then
    find "$src_dir/Api" -maxdepth 1 -type f ! \( -name 'MobilecmsApi.cs' -o -name 'CmsApi.cs' \) -exec mv -v {} Api/ \;
else
    echo "Directory $src_dir/Api does not exist."
fi

# Update all Client/ generated files except the ApiClient and ApiException files
if [ -d "$src_dir/Client" ]; then
    find "$src_dir/Client" -maxdepth 1 -type f ! \( -name 'ApiClient.cs' -o -name 'ApiException.cs' \) -exec mv -v {} Client/ \;
else
    echo "Directory $src_dir/Client does not exist."
fi

# Move Model/ files
if [ -d "$src_dir/Model" ]; then
    mv -v "$src_dir/Model/"* Model/
else
    echo "Directory $src_dir/Model does not exist."
fi

# Move Properties/ files
if [ -d "$src_dir/Properties" ]; then
    mv -v "$src_dir/Properties/"* Properties/
else
    echo "Directory $src_dir/Properties does not exist."
fi

# Move README.md
if [ -f "tmp/README.md" ]; then
    mv -v "tmp/README.md" README.md
else
    echo "File tmp/README.md does not exist."
fi

# Remove the tmp directory if it exists
if [ -d "tmp" ]; then
    rm -rf tmp
    echo "Removed tmp directory."
else
    echo "tmp directory does not exist."
fi

# Directory for API files
directory="Api"

# Replace specific content in .cs files
for file in "$directory"/*.cs; do
    if [ -f "$file" ]; then
        echo "Processing file: $file"
        sed -i '' 's|return new ReadOnlyDictionary<string, string>(this\.Configuration\.DefaultHeader);|return new Client.ReadOnlyDictionary<string, string>(this\.Configuration\.DefaultHeader);|g' "$file"
    else
        echo "File $file not found."
    fi
done

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

# Generate Unity meta files manually
echo "Generating Unity meta files manually..."

# Function to generate a Unity GUID
generate_unity_guid() {
    python3 -c "import uuid; print(str(uuid.uuid4()).replace('-', ''))"
}

# Function to create a meta file for a C# script
create_cs_meta_file() {
    local cs_file="$1"
    local meta_file="${cs_file}.meta"

    if [ ! -f "$meta_file" ]; then
        local guid=$(generate_unity_guid)
        echo "Creating meta file for: $cs_file"

        cat > "$meta_file" <<EOL
fileFormatVersion: 2
guid: $guid
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
EOL
    fi
}

# Function to create a meta file for a directory
create_directory_meta_file() {
    local dir_path="$1"
    local meta_file="${dir_path}.meta"

    if [ ! -f "$meta_file" ]; then
        local guid=$(generate_unity_guid)
        echo "Creating meta file for directory: $dir_path"

        cat > "$meta_file" <<EOL
fileFormatVersion: 2
guid: $guid
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
EOL
    fi
}

# Generate meta files for all directories that don't have them
find . -type d -name ".*" -prune -o -type d -print | while read -r dir; do
    if [ "$dir" != "." ]; then
        create_directory_meta_file "$dir"
    fi
done

# Generate meta files for all C# files that don't have them
find . -name "*.cs" -type f | while read -r cs_file; do
    create_cs_meta_file "$cs_file"
done

echo "Meta file generation completed!"
echo "Script execution completed."
