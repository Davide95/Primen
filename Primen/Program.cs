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
            using (new MPI.Environment(ref args))
            {
                // The process has the highest possible priority.
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

                WelcomeMessage();

                var key = readKey();
                var trialDivision = new TrialDivision(key);

                BigInteger factor = TrialDivision.NOT_VALID_FACTOR;
                Stopwatch swFactorization = null;

                if (Communicator.world.Rank == ROOT_RANK)
                    swFactorization = Stopwatch.StartNew();

                try
                {
                    factor = trialDivision.Factorization();
                }
                catch(ArithmeticException)
                {
                    Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                        Resources.Error113, key));
                    Communicator.world.Abort(113);
                }

                if (Communicator.world.Rank == ROOT_RANK)
                {
                    swFactorization.Stop();

                    Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                       Resources.FactorizationCompletedMessage, factor, key / factor));

                    Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                       Resources.TimeElapsedMessage, swFactorization.Elapsed));
                }

#if DEBUG
                Console.ReadLine();
#endif

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

            if (args.Length < (KEY_POSITION + 1))
            {
                if (Communicator.world.Rank == ROOT_RANK)
                {
                    Console.WriteLine(Resources.Error111);
                    Communicator.world.Abort(111);
                }
            }

            BigInteger key;
            if (!BigInteger.TryParse(args[KEY_POSITION], out key))
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

        /// <summary>
        /// The key position in the command line arguments array.
        /// </summary>
        private const int KEY_POSITION = 1;
    }
}
