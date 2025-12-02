# Genies SDK Bootstrap

## Overview

Automated setup wizard that configures Unity project prerequisites and installs the Genies Avatar SDK.

## Quick Start

1. The wizard opens automatically on startup if prerequisites aren't met or SDK isn't installed
2. Click "Fix" buttons next to any red ✗ marks to configure prerequisites
3. Click "Install 'Genies Avatar SDK'" once all prerequisites show green ✓
4. After installation, click the **gear icon** to configure SDK project settings
5. Manual access: **Tools > Genies > SDK Bootstrap Wizard**

## Prerequisites

The wizard automatically configures:

### Universal Prerequisites
- **IL2CPP Scripting Backend** - Required for all platforms
- **.NET Framework 4.8** - Required for all platforms
- **Active Input Handling** - Configures Unity input system (new Input System recommended)

### Platform-Specific Prerequisites

#### Windows (Experimental)
- **Vulkan Graphics API** - Required for Windows Standalone builds

#### Android
- **Vulkan Graphics API** - Required for Android builds
- **ARM64 Architecture** - Required for Android builds
- **Minimum Android 12.0 (API Level 31)** - Required minimum API level

### Prerequisite Actions
- **Fix Active**: Configure current build target only
- **Fix All**: Configure all supported build targets

### Active Input Handling Options
- **Use New ★** (Recommended): Modern Input System Package - best performance and features
- **Use Old**: Legacy Input Manager - limited functionality, not recommended
- **Use Both**: Enable both systems - not recommended, causes Android build errors

## Supported Platforms

- **Android** - Full support
- **iOS** - Full support
- **Windows Standalone** - Experimental support (may have limitations)

## Features

### Automatic SDK Version Detection
The wizard automatically detects when the SDK is installed or updated during your Unity session and prompts you to restart the editor for changes to take effect.

### Resources & Help
Quick access to:
- **Genies Hub** - Main portal for Genies resources
- **Technical Documentation** - Comprehensive SDK documentation
- **Genies Support** - Get help from the support team

## Configuration

**Disable auto-show on startup**: Check the box at the bottom of the wizard to prevent automatic display. Warnings will be logged to the console instead.

**SDK Settings**: After installing the SDK, click the gear icon button to open Project Settings and configure required SDK settings.

**Note**: The wizard is disabled during Play mode and compilation.

## Troubleshooting

- **Prerequisites won't configure**: Exit Play mode, ensure write permissions, click "Refresh Status"
- **Platform not supported**: Switch to a supported platform in **File > Build Settings**
- **SDK installation fails**: Check internet connection and package registry access
- **Wizard not showing**: Check if auto-show is disabled, or open manually via Tools menu
- **Input System errors on Android**: Ensure Active Input Handling is set to "New" or "Old", not "Both"
- **Editor restart required**: If SDK was installed/updated, restart Unity for changes to take full effect
