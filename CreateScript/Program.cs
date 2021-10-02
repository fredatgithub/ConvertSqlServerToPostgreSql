using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace CreateScript
{
  class Program
  {
    static void Main(string[] args)
    {
      Action<string> display = Console.WriteLine;
      display("creating a script for a database");
      display(string.Empty);
      display("Please wait for the script to gather all SQL schema objects...");
      string serverName = "yourDatabaseNameHere"; // yourDatabaseNameHere
      string result = CreateSMOScript(serverName);
      using (StreamWriter sw = new StreamWriter($"CreateAllTablesFor{serverName}.sql"))
      {
        sw.WriteLine(result);
      }

      display(string.Empty);
      display("The script has been written");
      display("Press any key to exit:");
      Console.ReadKey();
    }

    private static string CreateSMOScript(string serverName)
    {
      // Connect to the local, default instance of SQL Server. 
      Server srv = new Server();

      // Reference the database.  
      Database db = srv.Databases[serverName];

      Scripter scrp = new Scripter(srv);
      scrp.Options.ScriptDrops = false;
      scrp.Options.WithDependencies = true;
      scrp.Options.Indexes = true;   // To include indexes
      scrp.Options.DriAllConstraints = true;   // to include referential constraints in the script
      scrp.Options.Triggers = true;
      scrp.Options.FullTextIndexes = true;
      scrp.Options.NoCollation = false;
      scrp.Options.Bindings = true;
      scrp.Options.IncludeIfNotExists = true;
      scrp.Options.ScriptBatchTerminator = true;
      scrp.Options.ExtendedProperties = true;

      scrp.PrefetchObjects = true; // some sources suggest this may speed things up

      var urns = new List<Urn>();

      // Iterate through the tables in database and script each one   
      foreach (Table tb in db.Tables)
      {
        // check if the table is not a system table
        if (!tb.IsSystemObject)
        {
          urns.Add(tb.Urn);
        }
      }

      // Iterate through the views in database and script each one. Display the script.   
      foreach (View view in db.Views)
      {
        // check if the view is not a system object
        if (!view.IsSystemObject)
        {
          urns.Add(view.Urn);
        }
      }

      // Iterate through the stored procedures in database and script each one. Display the script.   
      foreach (StoredProcedure sp in db.StoredProcedures)
      {
        // check if the procedure is not a system object
        if (!sp.IsSystemObject)
        {
          urns.Add(sp.Urn);
        }
      }

      StringBuilder builder = new StringBuilder();
      StringCollection theScript = scrp.Script(urns.ToArray());
      foreach (string scryptLine in theScript)
      {
        // It seems each string is a sensible batch, and putting GO after it makes it work in tools like SSMS.
        // Wrapping each string in an 'exec' statement would work better if using SqlCommand to run the script.
        builder.AppendLine(scryptLine);
        builder.AppendLine("GO");
      }

      return builder.ToString();
    }
  }
}
