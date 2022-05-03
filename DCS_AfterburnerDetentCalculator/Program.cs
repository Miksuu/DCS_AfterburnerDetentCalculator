using System;
using System.Collections.Generic;
using SlimDX.DirectInput;

namespace DCS_AfterburnerDetentCalculator
{
    internal class Program
    {
        static void Main()
        {
            DetentSetup detentSetup = new DetentSetup();

            detentSetup.DetentSetupProcess();
        }
    }

    internal class DetentSetup
    {
        int selectedJoystick = 0;

        int axisToCalibrateWith = 0;
        bool invertedThrottle = false;

        int afterburnerDetentValue = 0;
        int idleDetentValue = 0;

        bool setIdleDetentValue = false;

        int jsIndex = 1;

        public void DetentSetupProcess()
        {
            Calculator calculator = new Calculator();

            JoystickReader jr = new JoystickReader();
            jr.Sticks = jr.GetSticks(calculator.totalValue);

            Console.WriteLine("##### DCS Detent Curve Calculator by Miksuu ##### \n");

            Console.WriteLine("Select your throttle from the list. \n MAKE SURE THAT ALL OF YOUR AXISES ON THAT DEVICE ARE CENTERED BEFORE PROCEEDING!!! \n");

            // Prints all the devices and their GUID's
            foreach (var item in jr.Sticks)
            {
                Console.WriteLine(jsIndex + "||" + item.Information.ProductName + " " + (item.Information.InstanceGuid));

                jsIndex++;
            }

            // Asks the user to enter the desired device, checks for out of range exceptions
            while (true)
            {
                ConsoleKeyInfo UserInputSelectedDevice = Console.ReadKey();

                if (char.IsDigit(UserInputSelectedDevice.KeyChar))
                {
                    selectedJoystick = int.Parse(UserInputSelectedDevice.KeyChar.ToString());

                    if (selectedJoystick > 0 && selectedJoystick <= jr.Sticks.Length)
                    {
                        Console.WriteLine();
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Invalid input! You selected: " + selectedJoystick +
                                          ". Select from the range: 1" + "-" + jr.Sticks.Length + "\n");
                    }
                }
                else
                {
                    Console.WriteLine( "Error! Invalid input. Enter a integer from 1 to " + jr.Sticks.Length + "\n");
                }
            }

            // Asks the user to enter the desired device, checks for out of range exceptions

            Console.WriteLine("Selected: " +  selectedJoystick + "\n Do you want to setup afterburner detent only [a]" +
                " or both afterburner detent and idle detent [b]? It may stop plane moving while the throttle is fully aft" +
                " without going over the stop detent.\n Press [a] if you never had this issue." );

            // Checks if the user wants to go for setting up only afterburner, or both afterburner and stop engines detent
            while (true)
            {
                ConsoleKeyInfo UserInputSelectSetupMode = Console.ReadKey();

                char selectedOption = 'f';

                if (char.IsLetter(UserInputSelectSetupMode.KeyChar))
                {
                    selectedOption = char.Parse(UserInputSelectSetupMode.KeyChar.ToString());

                    if (selectedOption == 'a' || selectedOption == 'A')
                    {
                        setIdleDetentValue = false;
                        Console.WriteLine();
                        break;
                    }
                    else if (selectedOption == 'b' || selectedOption == 'B')
                    {
                        setIdleDetentValue = true;
                        Console.WriteLine();
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Invalid input! Type a or b");
                    }
                }
                else
                {
                    Console.WriteLine("Error! Your input was not a letter. Enter a or b");
                }
            }

            Console.WriteLine("Choose an axis to set the detents on by moving the throttle axis fully forward(100 %) \n");

            // Reads the axis values of the selected device
            while (true)
            {
                bool canBreak = false;

                jr.ReadThrottleAxises(jr.Sticks[selectedJoystick-1]);
                Console.Write("\r[0 X] {0}     |[1 Y]{1}     |[2 Z]{2}     [3 rX] {3}     |[4 rY]{4}     |[5 rZ]{5}     ",
                    jr.monitorArray[0], jr.monitorArray[1], jr.monitorArray[2], jr.monitorArray[3], jr.monitorArray[4], jr.monitorArray[5]);

                for (int i = 0; i < jr.monitorArray.Length; i++)
                {
                    if (jr.monitorArray[i] > calculator.totalValue - calculator.totalValue * 0.01)
                    {
                        axisToCalibrateWith = i;
                        invertedThrottle = false;
                        canBreak = true;
                        break;
                    }
                    else if (jr.monitorArray[i] < calculator.totalValue * 0.01)
                    {
                        axisToCalibrateWith = i;
                        invertedThrottle = true;
                        canBreak = true;
                        break;
                    }
                }

                if (canBreak) break;

                System.Threading.Thread.Sleep(10);
            }

            Console.WriteLine("Detected: " + axisToCalibrateWith + " is inverted:" + invertedThrottle + "\n");

            Console.WriteLine("Move your throttle SLOWLY backwards until it sits on the afterburner detent. Then press spacebar. \n");

            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Spacebar) break;
            }

            afterburnerDetentValue = jr.ReadSpecificAxis(jr.Sticks[selectedJoystick-1], axisToCalibrateWith);

            Console.WriteLine("Done setting afterburner detent value at: " + afterburnerDetentValue + "\n");

            // If the user wants to set the idle/engine stop hard detent to the curve
            if (setIdleDetentValue)
            {
                Console.WriteLine("Move your throttle fully backwards to engine idle detent value on the throttle and then press spacebar. \n");

                while (true)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Spacebar) break;
                }

                idleDetentValue = jr.ReadSpecificAxis(jr.Sticks[selectedJoystick-1], axisToCalibrateWith);

                Console.WriteLine("Done setting idle value at: " + idleDetentValue + "\n");
            }

            // Checks for the inverted throttle (100 to 0% when moving forward to aft), and reverses the numbers if this is the case
            if (invertedThrottle)
            {
                afterburnerDetentValue = calculator.totalValue - afterburnerDetentValue;
                idleDetentValue = calculator.totalValue - idleDetentValue;
            }

            Console.WriteLine("Your values: " + afterburnerDetentValue + "|||" + idleDetentValue + "\n");

            calculator.Calculations(invertedThrottle, afterburnerDetentValue, idleDetentValue, setIdleDetentValue);
        }
    }

    // Reads the throttles axis values used for the configuration
    internal class JoystickReader
    {
        DirectInput Input = new DirectInput();
        public Joystick[] Sticks;
        Joystick stick;

        public int[] monitorArray = new int[6];

        // Reads the XYZ and rotation values of the throttle
        public void ReadThrottleAxises(Joystick _stick)
        {
            JoystickState state = new JoystickState();
            state = _stick.GetCurrentState();

            monitorArray[0] = state.X;
            monitorArray[1] = state.Y;
            monitorArray[2] = state.Z;
            monitorArray[3] = state.RotationX;
            monitorArray[4] = state.RotationY;
            monitorArray[5] = state.RotationZ;
        }

        public int ReadSpecificAxis (Joystick _stick, int _axis)
        {
            JoystickState state = new JoystickState();
            state = _stick.GetCurrentState();

            //Console.WriteLine( "axis: " + _axis +  "ROT x:" + state.RotationX);

            switch (_axis)
            {
                case 0:
                    monitorArray[0] = state.X;
                    break;
                case 1:
                    monitorArray[1] = state.Y;
                    break;
                case 2:
                    monitorArray[2] = state.Z;
                    break;
                case 3:
                    monitorArray[3] = state.RotationX;
                    break;
                case 4:
                    monitorArray[4] = state.RotationY;
                    break;
                case 5:
                    monitorArray[5] = state.RotationZ;
                    break;
                default:
                    Console.WriteLine("Error on reading specific axis: " + _axis);
                    break;
            }

            return monitorArray[_axis];
        }

        // Gets all of the input devices in an array and returns them for the main program
        public Joystick[] GetSticks(int _totalValue)
        {
            List<Joystick> sticks = new List<Joystick>();
            foreach (DeviceInstance device in Input.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
            {
                stick = new Joystick(Input, device.InstanceGuid);
                stick.Acquire();

                foreach (DeviceObjectInstance deviceObject in stick.GetObjects())
                {
                    if ((deviceObject.ObjectType & ObjectDeviceType.Axis) != 0)
                    {
                        stick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(0, _totalValue);
                    }
                }
                sticks.Add(stick);
            }
            return sticks.ToArray();
        }
    }

    internal class Calculator
    {
        // MOST OF THE VARIABLES MOVED TO THE METHOD CALL
        // Tested using this, do not edit this!
        //int originalValue = 16384;

        // ## EDIT THESE ACCORDING TO YOUR SETUP ######
        // (I looked up mine from the VPC software)

        // Throttle at 100%, the total range of the axis
        public int totalValue = 16384;

        // The value of the physical detent on the throttle,
        // each user has their own value depending where their physical detent is located 
        //int detentValue = 12330;

        // Set up your hard detent value that stops the engines from shutting down
        // The curve will try to optimise so when your physical throttle is at for example, 5%, hardstop,
        // can't continue without lifting detents, the plane will stay at idle throttle to avoid being moved on the ground
        // This is not tested properly yet, pretty much work in progress
        //int hardDetentValue = 754;
        //bool setIdleDetentValue = true;

        // If false, throttle will go from 0 to 100 (left to right), otherwise 100 to 0 (right to left)
        // Set according to what type of throttle you have
        //bool ReverseThrottle = true;

        // If true, prints the value with a dot (for pasting in to dcs config),
        // otherwise with a comma (for pasting to programs such as JoyPro)
        bool printForDcsConfigLuaFile = true;

        // #############################################

        // Aircraft afterburner values
        Dictionary<string, int> AircraftKVPs = new Dictionary<string, int>();

        void InitAircraftDictionary()
        {
            AircraftKVPs.Add("FA-18C", 12330);
            AircraftKVPs.Add("F-16C", 12330);
            AircraftKVPs.Add("MiG-21", 14900);
            AircraftKVPs.Add("F-14", 13110); // Could tune this up to 13250, but the afterburner stays on after putting the throttle back to soft detent for some reason
            AircraftKVPs.Add("F-5E", 13360);
            AircraftKVPs.Add("AJS37", 13400);
        }

        public void Calculations(bool _reversed, int _detentValue, int _idleDetentValue, bool _setIdleDetentValue)
        {
            InitAircraftDictionary();

            // Calculate a percentage out of those 
            float detentPercentage = (float)_detentValue / (float)totalValue;

            // Used for calculating the planes multiplier (is 1 when your throttle has 16384 axis values)
            //float multiplier = totalValue / originalValue;

            // Loops through all of the aircrafts
            foreach (KeyValuePair<string, int> kvp in AircraftKVPs)
            {
                //int planeAfterburnerValue = kvp.Value * (int)multiplier; // Maybe get rid of the castings later on, use floats only?

                // Calculates all the necessary values for the curve optimisation
                float hardDententValuePercentage = (float)_idleDetentValue / (float)totalValue;
                float afterburnerTurnOnPercentage = kvp.Value / (float)totalValue;
                float modifyCurveBy = afterburnerTurnOnPercentage - detentPercentage;

                // Calculate the initial values
                // Idk if the reverse forloop is needed here anymore tbh, it's flipped to normal order for rest of the calculations
                float[] userCurve = new float[11];
                for (int i = userCurve.Length - 1; i >= 0; i--)
                {
                    if (i == userCurve.Length - 1)
                    {
                        userCurve[i] = 1;
                    }
                    else
                    {
                        userCurve[i] = (float)i / 10 + modifyCurveBy;
                    }
                }

                // Starts setting up the hard detent positions
                userCurve[0] = 0f;
                if (_setIdleDetentValue)
                {
                    userCurve[1] = hardDententValuePercentage / 2.2f;
                }

                // How many times the means of the curve values will be calculated
                int smoothingIterations = 120;

                // Calculates the point which it will flatten out the curve
                int countHowManyBeforeTheSoftDetent = 0;

                for (int i = 1; i < userCurve.Length; i++)
                {
                    if (userCurve[i] > afterburnerTurnOnPercentage)
                    {
                        break;
                    }
                    else countHowManyBeforeTheSoftDetent++;
                }

                // Smoothens the curve according to the int set earlier
                for (int s = 0; s < smoothingIterations; s++)
                {
                    for (int i = 1; i < countHowManyBeforeTheSoftDetent; i++)
                    {
                        // Skips the first and 2nd element, the hard detent is defined already between 0-1 (curve number 1-2 in configs)
                        if (i > 1)
                        {
                            userCurve[i] = CalculateMean(userCurve[i - 1], userCurve[i + 1]);
                        }
                        else if (!_setIdleDetentValue && i == 1)
                        {
                            userCurve[i] = CalculateMean(userCurve[i - 1], userCurve[i + 1]);
                        }
                    }
                }

                // If the curve goes over 1 at any point, correct it
                for (int i = 0; i < userCurve.Length; i++)
                {
                    if (userCurve[i] > 1)
                    {
                        userCurve[i] = 1 - (userCurve[i - 1] - userCurve[i - 2]) / 10;
                        //Console.WriteLine("calculated: " + userCurve[i]);
                    }
                }

                if (_reversed)
                {
                    userCurve = RevertCurve(userCurve);
                }

                // Prints the results, place them to program such as JoyPro etc (printed with commas)
                if (!printForDcsConfigLuaFile)
                {
                    Console.WriteLine(kvp.Key.ToString() + "'s curve with commas: ");
                    for (int i = 0; i < userCurve.Length; i++)
                    {
                        Console.WriteLine(userCurve[i]);
                    }
                }
                // Place in dcs config (printed with dots)
                else
                {
                    System.Globalization.NumberFormatInfo nfi = new System.Globalization.NumberFormatInfo();
                    nfi.NumberDecimalSeparator = ".";

                    Console.WriteLine(kvp.Key.ToString() + "'s curve with dots: ");
                    for (int i = 0; i < userCurve.Length; i++)
                    {
                        Console.WriteLine("[" + (i + 1) + "] = " + userCurve[i].ToString(nfi) + ",");
                    }
                }
                Console.WriteLine();
            }
            Console.ReadKey();
        }
        static float CalculateMean(float _previousNum, float _nextNum)
        {
            return (_previousNum + _nextNum) / 2;
        }

        // Reverses the curve for users thats throttles go from right to left
        static float[] RevertCurve(float[] _userCurve)
        {
            float[] newUserCurve = new float[_userCurve.Length];
            int valueToReplaceWith = _userCurve.Length - 1;
            for (int i = 0; i < _userCurve.Length - 1; i++)
            {
                newUserCurve[i] = 1 - _userCurve[valueToReplaceWith];
                valueToReplaceWith--;
            }
            newUserCurve[10] = 1;
            return newUserCurve;
        }
    }
}