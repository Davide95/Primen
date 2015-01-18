using MPI;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Primen
{
    internal class TrialDivision
    {
        public TrialDivision(BigInteger key)
        {
            n = BigInteger.Abs(key);
            isFinished = !IS_FINISHED;
        }

        public BigInteger Factorization()
        {
            // Zero and one can't be factorized.
            if (n.IsZero || (n == BigInteger.One))
                throw new ArithmeticException("NaN");

            else if (n == 2)
                return BigInteger.One;

            else if (n % 2 == 0)
                return 2;

            var sqrtOfN = n.Sqrt();
            if (sqrtOfN < MIN_FROM)
                return BigInteger.One;
            
            if (Communicator.world.Size == 1)
            {
                if (Communicator.world.Rank == Program.ROOT_RANK)
                    return ParallelFactorization(MIN_FROM, n.Sqrt());

                else
                    return NOT_VALID_FACTOR;
            }
                
            return MpiFactorization();
        }

        private BigInteger MpiFactorization()
        {
            var sqrtOfN = n.Sqrt();

            // Rank 0 doesn't calculate anything.
            var worldSize = Communicator.world.Size - 1;
            var myRank = Communicator.world.Rank - 1;
            var blockSize = sqrtOfN / worldSize;

            // If the key is too small, it uses only tasks parallelization.
            if (blockSize < MIN_FROM)
            {
                if (Communicator.world.Rank == Program.ROOT_RANK)
                    return ParallelFactorization(MIN_FROM, sqrtOfN);

                // Only root rank know the result.
                return NOT_VALID_FACTOR;
            }

            if (Communicator.world.Rank == Program.ROOT_RANK)
            {
                // Waiting for other sources.
                do
                {
                    BigIntegerSerializable factorS;
                    Communicator.world.Receive<BigIntegerSerializable>(
                        MPI.Communicator.anySource, FACTOR_TAG, out factorS);

                    BigInteger factor = factorS;
                    if (!factor.IsOne)
                    {
                        for (int rank = Program.ROOT_RANK + 1; rank < Communicator.world.Size; rank++)
                            Communicator.world.ImmediateSend(IS_FINISHED, rank, STOP_TAG);

                        return factor;
                    }

                    worldSize--;
                } while (worldSize != 0);

                // If there aren't factor, it is prime.
                return BigInteger.One;
            }
            else
            {
                // Check if someone has found one factor
                isFinished = !IS_FINISHED;
                Task.Run( () => {
                    isFinished = Communicator.world.Receive<bool>(Program.ROOT_RANK, STOP_TAG);
                });

                var from = (myRank == Program.ROOT_RANK) ? MIN_FROM : (myRank * blockSize);
                var to = (myRank == worldSize - 1) ? (sqrtOfN-1) : ((myRank + 1) * blockSize);

                Communicator.world.Send((BigIntegerSerializable)ParallelFactorization(from, to), 0, FACTOR_TAG);

                // Only root rank know the result.
                return BigInteger.Zero;
            }
        }

        private BigInteger ParallelFactorization(BigInteger from, BigInteger to)
        {
            if (from < MIN_FROM)
                throw new ArgumentOutOfRangeException("from");
            if (from > to)
                throw new ArgumentOutOfRangeException("from");
            if (to > n)
                throw new ArgumentOutOfRangeException("to");

            var blockSize = (to - from) / System.Environment.ProcessorCount;

            // If the key is too small, it doesn't use tasks parallelization.
            if (blockSize < MIN_FROM)
                return Factorization(from, to);

            BigInteger factor = BigInteger.One;
            Parallel.For(0, System.Environment.ProcessorCount, (i, loopState) =>
                {
                    var fromP = from + i * blockSize;
                    var toP =  (i == System.Environment.ProcessorCount - 1) ? to : (from + (i + 1) * blockSize);

                    var result = Factorization(fromP, toP, ref loopState);

                    if ((!result.IsOne) && (!result.IsZero))
                        factor = result;
                });

            return factor;
        }

        private BigInteger Factorization(BigInteger from, BigInteger to)
        {
            ParallelLoopState fakeLoop = null;
            return Factorization(from, to, ref fakeLoop);
        }

        private BigInteger Factorization(BigInteger from, BigInteger to,
            ref ParallelLoopState loopState)
        {
            if (from < MIN_FROM)
                throw new ArgumentOutOfRangeException("from");
            if (from > to)
                throw new ArgumentOutOfRangeException("from");
            if (to > n)
                throw new ArgumentOutOfRangeException("to");

            while(from <= to)
            {
                // If the parallel loop is stopped, it returns 0.
                if ((loopState != null) && (loopState.IsStopped))
                    return BigInteger.Zero;

                // If someone has found one factor, all ranks have to stop.
                if (isFinished)
                    return BigInteger.Zero;

                if((n % from).IsZero)
                {
                    if(loopState != null)
                        loopState.Stop();

                    return from;
                }

                from += 2;
            }

            // If it didn't find any factor, it returns 1.
            return BigInteger.One;
        }

        public const int NOT_VALID_FACTOR = 0;

        private BigInteger n;
        private bool isFinished;
        private const int MIN_FROM = 3;

        private const int FACTOR_TAG = 1;
        private const int STOP_TAG = 2;
        private const bool IS_FINISHED = true;
    }
}
