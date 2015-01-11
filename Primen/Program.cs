using MPI;
using Primen.Properties;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Primen
{
    static class Program
    {
        static void Main(string[] args)
        {
            using (var mpi = new MPI.Environment(ref args))
            {
                // The process has the highest possible priority.
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

                WelcomeMessage();

                BigInteger key = readKey();
                var trialDivision = new TrialDivision(key);

                try
                {
                    var factor = trialDivision.Factorization();

                    if (Communicator.world.Rank == ROOT_RANK)
                    {
                        Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                           Resources.FactorizationCompletedMessage, factor, key / factor));
                    }
                }
                catch(ArithmeticException)
                {
                    Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                        Resources.Error113, key));
                    Communicator.world.Abort(113);
                }

                Console.ReadLine();
                Communicator.world.Abort(0);
            }
        }

        /// <summary>
        /// Shows a welcome message with informations about the MPI world.
        /// </summary>
        private static void WelcomeMessage()
        {
            Console.WriteLine(
                    String.Format(CultureInfo.CurrentCulture, Resources.WelcomeMessage
                    , Communicator.world.Rank, MPI.Environment.ProcessorName));

            if (Communicator.world.Rank == ROOT_RANK)
                Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                    Resources.NumberOfProcessesMessage, Communicator.world.Size));
        }

        /// <summary>Read the key to factorize from the command line arguments.</summary>
        /// <returns>Returns the key to factorize.</returns>
        private static BigInteger readKey()
        {
            string[] args = System.Environment.GetCommandLineArgs();

            if (args.Length < 2)
            {
                if (Communicator.world.Rank == ROOT_RANK)
                {
                    Console.WriteLine(Resources.Error111);
                    Communicator.world.Abort(111);
                }
            }

            BigInteger key;
            if (!BigInteger.TryParse(args[1], out key))
            {
                if (Communicator.world.Rank == ROOT_RANK)
                { 
                    Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                        Resources.Error112, args[1]));
                    Communicator.world.Abort(112);
                }
            }

            return key;
        }

        public const int ROOT_RANK = 0;
    }
}
