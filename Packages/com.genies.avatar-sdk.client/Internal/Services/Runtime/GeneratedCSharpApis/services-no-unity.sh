#!/bin/zsh

# for post processing the auto generated swaggerhub files
# because the swaggerhub output file generation is inconsistent
# this script will not work on windows (needs to be bin/bash)
#
# Modified version that generates meta files without Unity dependency

# run this in ~/genies-unity-packages/com.genies.services/Runtime/GeneratedCSharpApis

set -e  # Exit immediately if a command exits with a non-zero status.
set -u  # Treat unset variables as an error and exit immediately.

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

# Generate Unity meta files manually (without Unity dependency)
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
