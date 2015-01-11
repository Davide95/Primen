using Primen.Properties;
using SQLite;
using System;
using System.Numerics;

namespace Primen
{
    internal class RainbowTable
    {
        [PrimaryKey]
        public BigIntegerSerializable Key { get; set; }

        public BigIntegerSerializable Factor { get; set; }

        public TimeSpan TimeOfCalculation { get; set; }
    }

    internal class Database : SQLiteConnection
    {
        public static Database getDatabase()
        {
            if (instance == null)
                instance = new Database(Resources.DbPath);

            return instance;
        }

        private Database(string path)
            : base(Resources.DbPath)
        {
            CreateTable<RainbowTable>();
        }

        // Singleton
        private static Database instance = null;
    }
}
