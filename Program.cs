using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Services;

namespace FB3EmbedDotNetExample
{
    class Program
    {
        static void Main(string[] args)
        {
            var databasefilename = System.IO.Path.GetFullPath("demo.fdb");
            Console.WriteLine("databasefilename: {0}", databasefilename);

            var firebirdNativeClientLibraryFilename = System.IO.Path.GetFullPath(@"Firebird-3.0.4.33054-x64-embedded\fbclient.dll");
            Console.WriteLine("firebirdNativeClientLibraryFilename: {0}", firebirdNativeClientLibraryFilename);

            if (File.Exists(firebirdNativeClientLibraryFilename) == false)
            {
                throw new Exception("Firebird Embedded Not Found At Expected Location!");
            }

            var csb = new FbConnectionStringBuilder();
            csb.ServerType = FbServerType.Embedded;
            csb.ClientLibrary = firebirdNativeClientLibraryFilename;
            csb.Database = databasefilename;
            csb.UserID = "ThisCanBeAnything";
            csb.Password = "AnyValueAtAll";

            var connectionstring = csb.ToString();


            if (File.Exists(databasefilename) == false)
            {
                Console.WriteLine("Creating database {0}", databasefilename);
                FbConnection.CreateDatabase(connectionstring, false);
                Console.WriteLine("Created database {0}", databasefilename);
            }


            Console.WriteLine("Connecting");
            using (var connection = new FbConnection(csb.ToString()))
            {
                connection.StateChange += Connection_StateChange;
                connection.InfoMessage += Connection_InfoMessage;
                connection.Disposed += Connection_Disposed;

                connection.Open();

                using (var trn = connection.BeginTransaction())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = trn;
                        cmd.CommandText = @"
RECREATE TABLE ATABLE 
(
  PK                INTEGER         NOT NULL,
  NAME              VARCHAR(   128),
 CONSTRAINT PK_ATABLE PRIMARY KEY (PK)
);";
                        cmd.ExecuteNonQuery();
                    }

                    trn.Commit();
                }

                using (var trn = connection.BeginTransaction())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = trn;
                        cmd.CommandText = "INSERT INTO ATABLE (PK,NAME) VALUES (@PK,@NAME)";
                        cmd.Parameters.Add("PK", 1);
                        cmd.Parameters.Add("NAME", "Adam");
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = trn;
                        cmd.CommandText = "INSERT INTO ATABLE (PK,NAME) VALUES (@PK,@NAME)";
                        cmd.Parameters.Add("PK", 2);
                        cmd.Parameters.Add("NAME", "Eve");
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = trn;
                        cmd.CommandText = "SELECT * from ATABLE";
                        using (var reader = cmd.ExecuteReader())
                        {
                            int RowIndex = 0;
                            while (reader.Read())
                            {
                                RowIndex += 1;
                                var PK = reader.GetFieldValue<int>(0);
                                var Name = reader.GetString(1);
                                Console.WriteLine("Row #{0} PK={1} Name={2}", RowIndex, PK, Name);
                            }
                        }
                    }

                    trn.Commit();
                }


                connection.Close();
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

        }

        private static void Connection_Disposed(object sender, EventArgs e)
        {
            Console.WriteLine("Connection Disposed");
        }

        private static void Connection_InfoMessage(object sender, FbInfoMessageEventArgs e)
        {
            Console.WriteLine("Connection InfoMessage:{0}", e.Message);
        }

        private static void Connection_StateChange(object sender, System.Data.StateChangeEventArgs e)
        {
            Console.WriteLine("Connection StateChange:{0}", e.CurrentState);
        }
    }
}
