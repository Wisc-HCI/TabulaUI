# TabulaUI
A mobile UI for Tabula.

### Description

This UI is a robot programming tablet that works with the TabulaSynthesizer backend. While the backend can be tested without the UI, you need the ROS backend running for the UI to work.

It was developed for a Samsung Galaxy S8 tablet using Unity Editor version 2020.3.21f1. This system uses speech input, which works best on the Android tablet but may also work from Windows (but is not supported with Unity on Linux). Further, building for the tablet is most accessible through Windows. To test on an S8 tablet, ensure that the Android Build support was installed with your Unity Editor (version 2020.3.21f1). You will need to change the BuildSettings to Android.

Note, if you are on a Mac, I have had success guided by this blog post: https://medium.com/@surathchatterji/unity3d-devs-setting-up-your-mac-for-android-builds-6fb937aec31c

### Setting Up
Note that the setup steps may require you to enter the Unity Editor in safe mode while you configure the ROS TCP Connector.
1. Clone this project
2. Clone Unity's [`ROS TCP Connector`](https://github.com/Unity-Technologies/ROS-TCP-Connector) to the TabulaUI > Packages folder.
3. Find the Window tab on the top > Package Manager and [install the ROS TCP Connector package from disk](https://docs.unity3d.com/Manual/upm-ui-local.html).
4. Go to TabulaUI > Packages > manifest.json and change the following line: 
      > "com.unity.robotics.ros-tcp-connector": "file:**[your local TabulaUI Path]**/TabulaUI/Packages/ROS-TCP-Connector/com.unity.robotics.ros-tcp-connector",

Once you open the project in Unity, there is one additional setup step. In the Project window, click on the `Resources/RosConnectionPrefab` file. You will need to set the `Ros IP Address` field to your own IP address --> This should be the ROS master IP!!!

### Running
To run, you need to open in the Unity Editor the scene `SampleScene.` If the tablet is connected to the computer, you should be able to Build and Run (which will deploy the app to the tablet). Otherwise, you can test it in the editor with the play button, but speech input will not work as expected.

### Troubleshooting

#### "the type or namespace Robotics is missing" Unity compiler error
This can happen when the ROS TCP Connector has updated, but your ROS TCP Connector has not. If this happens, try:

1. Uninstalling and reinstalling the ROS TCP connector (make sure the manifest.json file is changed to your local package)
2. Check that the IDE you use with Unity (Visual Studio, Rider, etc.) is also up-to-date
3. If all else fails, push any changes to your remote local branch, delete your local repository, and re-clone (this is what worked for me)

#### Null Reference Exception: a script is missing error while drawing lines in recording mode.
This is because the LineBrush prefab needs PositionsManager to draw lines.

A fix to the issue is coming, but in the meantime:

1. Go to Assets/Resources/LineBrush.prefab and click on "Open Prefab" in the Inspector tab. <img width="1440" alt="Adding Positions Manager Script Pt 1" src="https://user-images.githubusercontent.com/64796985/173611325-729e8aa5-ef54-4f79-a13a-1779a2c97a62.png">
2. Scroll down to the bottom of the LineBrush's Inspector tab and find the component labelled "Missing Script".
3. Click on Assets/Scripts and then drag the PositionsManager script into the Missing Script slot. <img width="1440" alt="Adding Positions Manager script Pt 2" src="https://user-images.githubusercontent.com/64796985/173612513-0ce8cc26-e7ba-487a-8aa2-6f0c5504dafc.png">

# Attributions
Please see the [ATTRIBUTIONS.md](ATTRIBUTIONS.md) for a list of icon attributions.


