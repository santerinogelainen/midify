using System;
using Midis;
using Waves;

namespace ConsoleApplication3 {
    class midify {

        public static readonly string[] options = {"help", "tc", "make"};

        static void Main(string[] args) {

            if (!CheckArgs(args)) {
                return;
            }

            string option = args[0];

            switch (option) {
                case "help":
                    break;


                case "tc":
                    break;


                case "make":
                    if (args.Length > 1) {
                        Make(args[1]);
                    } else {
                        Console.WriteLine("Syntax: midify make <midifilepath>");
                    }
                    break;
                default:
                    Console.WriteLine("Unknown command '{0}'. The command you requested has been added, but has not been implemented yet.", option);
                    return;
            }
        }

        static void Make(string filepath) {
            Midi m = new Midi(filepath);
            Wave w = m.Tracks[0].ToWave("wave.wav");
            w.Save("test.wav");
        }

        /// <summary>
        /// check the validity of the command line arguments
        /// </summary>
        /// <param name="args">command line arguments</param>
        /// <returns>true if valid</returns>
        static bool CheckArgs(string[] args) {
            if (args == null || args.Length < 1) {
                Console.WriteLine("Syntax: midify <option>");
                return false;
            }
            string option = args[0];
            return CheckOption(option);
        }

        /// <summary>
        /// check the validity/existance of the option given
        /// </summary>
        /// <param name="option">option given</param>
        /// <returns>true if valid and command exists</returns>
        static bool CheckOption(string option) {
            foreach (string o in options) {
                if (o == option) {
                    return true;
                }
            }
            Console.WriteLine("Unknown command '{0}'. The command you requested does not exist.", option);
            return false;
        }
    }
}
