# Genies Camera

<p align="center">
  <img src="Assets/Project/Textures/app-icon.png" width="256" />
</p>

# Genies Camera

Project Documentation

## **1. Description**

### **1.1 Overview**

**Genies Camera** is an XR sample project for iOS that demonstrates how virtual characters can be integrated into real world environments and brought to life through real-time puppeteering. Using Genies’ modular avatar tech stack in combination with facial motion tracking and GUI-based user input, Genies Camera enables users to animate, photograph, and record their avatar for a new era of whimsical self-expression.

**This open-source project mirrors the official Genies Camera application, available on the iOS App Store.** 

### **1.2 What’s in the box**

Genies Camera demonstrates:

- Genies User accounts and login flow
- Animation controllers for locomotion with PlayableGraph emote overlays
- Blendshape animation driven by facial tracking
- Bipedal animation driven by GUI input
- Eye look-at target animation via blendshapes
- Spatial meshing with occlusion and shadowcasting
- Photography and video recording

## **2. Use Cases**

The Genies Camera sample project serves as a foundation for a wide range of XR applications. Some example use cases include:

### **2.1 AR Games with Genies Avatars**

Build augmented reality games where avatar identities are created and maintained in Genies Party. Users who invest in the creation and socialization of a virtual identity may be interested to port that identity along with them into your application!

### **2.2 Mobile Applications with Avatar Identities**

Build games or utilities for mobile that include personified characters, such as chatbots or virtual companions. Using Genies Camera as a jumping off point, developers can learn how Genies Accounts and Login systems work, borrow animation controller implementations, or leverage our approach to animation subsystems like Emote controllers or Look-at implementations.

## **3. Getting Started**

This section outlines the system requirements and installation steps necessary to set up your project for development.

### **3.1 System Requirements**

To build and run Genies Camera, you will need:
- **Git LFS installation** This repository uses [Git LFS](https://docs.github.com/en/repositories/working-with-files/managing-large-files/installing-git-large-file-storage).
- **Compatible Mac computer**
- **iPhone** or **iPad** with TrueDepth sensor (Facial Recognition abilities)
- **Unity Editor** version 2022.3.32f1 or later
    
    *(Note: Higher versions may require a project upgrade)*
    
- **Xcode** 1.2 or later
*(Note: Higher versions may require a Unity Editor upgrade for compatibility)*

### **3.2 Project Installation Steps**

- Clone the repository to your local drive
- Editor
    - Open “Main.unity”
    - Hit the Play button
    - See “End-User Genies Camera Experience *for Developers”* section for more information.
- Build
    - Build the Project in Unity
    - Open the Build in XCode
    - Deploy to iOS Device

## **4. End-User Genies Camera Experience**

Whether you’re running the App Store version or your own build of Genies Camera, here’s a breakdown of the end-user experience and application capabilities.

*NOTE: From here on, you may see the word “Genie” to describe the character or avatar. We at Genies colloquially use the term “Genie” to describe such a character, so you’ll see this used frequently in the codebase as well.*

### **4.1 Launch Sequence**

1. The user will be taken through an initial login and spatial mesh scanning phase. You can use the “skip” button in the upper right hand corner if you wish to bypass the login.
2. The purpose of the scanning phase is to give the app a baseline understanding of the area. The app will continue to scan throughout the life of the experience.
3. At the end of the launch sequence, the Genie will spawn within the user’s camera frustum.
4. From here, the user can animate the Genie via their own tracked facial expressions, as well as the locomotion and Emote UI buttons on screen.

### **4.2 Main Menu**

The Main Menu is an overlay that exists on top of the Camera view at all times.

The Menu contains the following UIs and functionalities:

- **User Account Button**: Use this button to Log In or Log Out of the current session.
- **Genies Avatar Submenu:** This button will toggle a submenu of available Avatar options. Included “baked in” avatars are sourced from our [Genies Starter Pack](https://github.com/geniesinc/GeniesStarterPack) repo.
- **Background Environments Submenu:** This button will toggle a submenu of available Background environment options.
    - **AR**: This mode will show your Genie integrated with the real world in 3D
    - **Passthrough**: This mode will show your Genie on top of the passthrough camera, like a decal.
    - **Color**: This mode will show your Genie on top of the color of your choice
    - **Media**: This mode will show your Genie on top of an image or video from your Camera Roll
- **Lighting Button**: This button will launch a menu with toggles that control various aspects of the virtual lighting in-scene.
    - **Light Saturation**: Adjust the intensity of color on the main light
    - **Light Hue**: Adjust the color of the main light
    - **Light Brightness**: Adjust the main light brightness
    - **Shadow Direction**: Adjust the XZ angle of the main light
    - **Shadow Length**: Adjust the Y angle of the main light
    - **Shadow Darkness**: Adjust the opacity of the shadow cast
    - **Restore Default Lighting:** Reset the lighting to maximum beauty
- **Eye-lock Button:** This button will enter a state in which the Genie is making continued eye contact with the active Camera
- **Joystick:** This is a draggable UI element that controls the Genie’s relative position, including a walking animation.
- **Shutter Button:** This button will take a photo or video of the current scene, depending on which mode you have active.
- **Photo / Video Toggle:** Toggle between single image capture or full video capture, including audio,
- **Quick Actions Menu:** This is a collection of three “emote style” animations and one “jump” animation which is part of locomotion.
    - Peace: Genie emotes with a Peace sign, if she is not locomoting
    - Wave: Genie emotes with a Wave, if she is not locomoting
    - Vogue: Genie emotes with a Vogue, if she is not locomoting
    - Jump: Genie can Jump in combination with her Joystick locomotion, or as a solo act

### **4.3 Multi-Touch and Drag**

- **Single finger drag**: This will spin the Genie on the y-axis
- **Double finger drag**: This will translate the Genie on the XZ plane
- **Triple finger drag:** This will translate the Genie on the Y axis
- **Double-tap (single finger):** This will teleport the Genie to the location beneath your finger
- **Single finger touch:** While touching the Genie, she will maintain eye contact with you

### **4.4 Spatial Scanning Tips**

If your iOS device has a LiDAR sensor, scanning your environment will be relatively instantaneous. For users without LiDAR, ARKit will fall back on ARPlane detection.

For the most effective ARPlane detection, move your phone in gentle arcs while examining the floor with your phone tilted perhaps 30 degrees downward.

## **5. End-User Genies Camera Experience *for Developers***

### 5.0 Build Settings

Please make sure your project is set to an iOS build.

### 5.1 Login

Thank you for using Genies Camera! To create your own Genies avatar, for the moment please reach out to [devrelations@genies.com](mailto:devrelations@genies.com) for a TestFlight invitation.

Once you have a Genies Party account, this avatar will now be recalled when you Login to the Main scene using your phone number. 

### 5.2 ARFace

In Editor mode, you do not have access to Face Tracking. For this reason, we have created an ARFace debug object. Changing the Pose of this GameObject will cause your Genie’s head and spine rotation to update. 

### 5.3 ARKit Blendshapes

Press and hold the “b” key to cycle through every blendshape from 0-1, to ensure they are working as expected.

## **6. Project Architecture**

This project is intended as an example that you can freely use, modify and build off from as you please. You don’t have to use all our designs or methodologies, but they worked for us, and we hope you’ll find them inspiring in your own work!

### **6.1 Main Scene Structure**

In the Main scene, you’ll see several GameObjects.

- **AppManager**: This is our bootstrapper, responsible for spawning other managers and initializing dependencies.
- **RoundedCorners_DO_NOT_DELETE**: We love the [Nobi Rounded Corners](https://github.com/kirevdokimov/Unity-UI-Rounded-Corners) project and use it throughout our UI, but it does seem to require this sort of initialization object to function properly.
- **LightController_DO_NOT_DELETE**: We have found that without a Directional Light in the scene at launch, Unity’s post processing stack behaves oddly. Since our light lives within our lighting setup, we just went ahead and included our whole lighting setup!

### **6.2 Core Managers**

1. **App Manager**: This class manages the instantiation and dependency injection for all other managers.
2. **Launch Sequence Controller**: This class manages the launch sequence in the following order (1) login (2) find the floor (3) spawn Genie.
3. **Input Manager**: This class tracks and manages touch input from the user.
4. **Genies Manager**: This class manages the spawned Genie(s) in the scene.
5. **Camera Manager**: This class manages the active camera, whether it’s a worldspace or screenspace camera.
6. **Bg Controller:** This class manages the background environment, whether it’s real-world AR, passthrough camera, a solid color, or a selection from the User’s camera roll.
7. **Main Menu Controller**: This class manages the UI and shares events with other managers.
8. **User Genie Loader**: This class loads the Genie from the Genies cloud.
9. **Light Controller**: This class manages the main light in the scene.

### **6.3 Genies Avatar Components**

1. **Genie Controller**: This component is the main Genie controller. It manages user interaction, locomotion, and animation. 
2. **Emote Manager**: This component manages “emotes” which override the default Animator controller, by crossfading influence of the Genie’s PlayableGraph.
3. **Genie Face Controller**: This component listens for updates of ARKit’s facial tracking system and propagates that data to the Genie’s facial blendshapes and head pose.

# Contact
For questions or support, please contact: devrelations@genies.com
