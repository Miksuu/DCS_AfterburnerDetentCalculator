Makes the process for setting DCS planes afterburner and idle detent easier for users with physical detents on their throttle controllers. Planes tested and supported: FA-18C, F-16C, MiG-21, F-14, F-5E, AJS37. More planes will be added soon (or you can do so too.) Currently the program only shows the console log output, and does not change your controls. In the future, I may add features such setting all of your planes afterburner detent in less than minute with this program (so you don't need to manually copypaste them in to dcs config). Use at your own risk still, backup your DCS.openbeta folder in your Saved Games before using this program!

Instructions (updated for V2 on 06/01/2022):
1) Download the zip, go to builds/[newest version]. Start the exe. If you do not trust this file, you may review the code and build the solution yourself using Visual Studio.
2) If the program does not start, try downloading ".Net Framework 4.8" from here: https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48 . If it still doesn't work, try contacting me on Discord (Miksuu#4278) and we can try to figure it out.
3) Follow the instructions on the console app. The program will figure out how your throttle axis works by detecting the if it's inverted or not. The key part here is to have ALL of your axises on your throttle not on 1% or 99% of the axis (keep them centered)
4) After the program prints the list of the planes, copypaste the 11 rows from [1] to [11] of a plane. We will paste them to the DCS config later on.
5) Start up DCS, go to controls, select a plane. Go to axis settings of that plane, open Thrust axis tuning. Enable Slider. If you see that the black box moves from left to right, while moving your throttle from aft to forward, you need to invert the axis. Enable User Curve. Press ok and save the file.
6) Locate the file on your disk, and open it (preferably with program such as Notepad++).
7) Scroll down until you see an usercurve such as:
 						["curvature"] = {
							[1] = 0,
							[2] = 0.1,
							[3] = 0.2,
							[4] = 0.3,
							[5] = 0.4,
							[6] = 0.5,
							[7] = 0.6,
							[8] = 0.7,
							[9] = 0.8,
							[10] = 0.9,
							[11] = 1,
						},
8) Replace it with the input from the program, for example: 
  					["curvature"] = {
              [1] = 0,
              [2] = 0.02005837,
              [3] = 0.1443581,
              [4] = 0.2686579,
              [5] = 0.3929577,
              [6] = 0.5172575,
              [7] = 0.6415572,
              [8] = 0.7658569,
              [9] = 0.8658569,
              [10] = 0.9658569,
              [11] = 1,
						},
9) Save the file, and load it in DCS for your throttle.
10) Test that it works. If it doesn't, you probably forgot to invert the axis or set it as slider. The program is still on it's very early stage. Feel free to contact me if you have any issues with it.

Changelog:
v2:
- Added a prompt for the user that he can select between setting up the detent that prevents the engine from shutting down accidentally and the afterburner detent, or only the afterburner detent.

v1:
Initial build
