using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Diagnostics;


/*
    Collision Check Info Retrieval
    This program targets .NET Framework 4.6.1

    This program is a stand-alone command line program, however it is NOT designed to be used on its own and is EXPRESSELY ONLY for use by my collision check program to do SQL queries for it.
    Back when I had access to the Aria DB this worked, but not anymore
    The connection string to connect to the DB goes in the App.config file. I deleted the string that was there because it contains usenames and passwords, as well as in the connection string in the code here

    Copyright (C) 2021 Zackary Thomas Ricci Morelli
    
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    any later version.
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
        I can be contacted at: zackmorelli@gmail.com
       
*/


namespace CollisionCheck_InfoRetrieval
{
    static class CollisionCheck_InfoRetrieval
    {
        [STAThread]
        static void Main(string[] args)
        {
            double GantryStartAngle = -1.0;
            double? GantryEndAngle = -1.0;
            string ArcDirection = null;   // either CW, CC, or NONE (meaning no Arc!)

            Console.Title = "Querying ARIA database (takes about 10 seconds)";

            try
            {
                string argnet = null;
                foreach (string rg in args)
                {
                    argnet = argnet + rg;
                }

                // The arguments were separated by commas when they were passed to the process that started this program in Collision Check. we know what each one is becaue we know the order we passed them in Collision Check. 
                string[] arglist = argnet.Split(',');

                string patientId = arglist[0];     // LINQ can't handle array indexes with entities (i.e. EF database entities)
                string courseId = arglist[1];
                string planId = arglist[2];
                string beamId = arglist[3];

              //  MessageBox.Show("PatientId: " + patientId);
              //  MessageBox.Show("courseId: " + courseId);
              //  MessageBox.Show("planId: " + planId);
              //  MessageBox.Show("beamId: " + beamId);

                long patser = -1;
                long cser = -1;
                long plser = -1;
                long rser = -1;

                string ConnectionString = @"Data Source=WVVRNDBP01SS;Initial Catalog=variansystem;Integrated Security=False;User Id=;Password=";
                //data source=WVVRNDBP01SS;initial catalog=variansystem;integrated security=False;User Id=;Password=

                SqlConnection conn = new SqlConnection(ConnectionString);
                SqlCommand command;
                SqlDataReader datareader;
                string sql;

                conn.Open();
                sql = "USE variansystem SELECT PatientSer FROM dbo.Patient WHERE PatientId = '" + patientId + "'"; 
                command = new SqlCommand(sql, conn);
                datareader = command.ExecuteReader();

                while(datareader.Read())
                {
                    patser = (long)datareader["PatientSer"];
                }
                conn.Close();

                if(patser == -1)
                {
                    MessageBox.Show("Error: Could not find this patient in the database!");
                }

                //MessageBox.Show("patser: " + patser);

                conn.Open();
                sql = "USE variansystem SELECT CourseSer FROM dbo.Course WHERE PatientSer = " + patser;
                command = new SqlCommand(sql, conn);
                datareader = command.ExecuteReader();

                while (datareader.Read())
                {
                    cser = (long)datareader["CourseSer"];
                }
                conn.Close();

                if (cser == -1)
                {
                    MessageBox.Show("Error: Could not find this treatment course in the database!");
                }

                //MessageBox.Show("Course Ser: " + cser);

                conn.Open();
                sql = "USE variansystem SELECT PlanSetupSer FROM dbo.PlanSetup WHERE CourseSer = " + cser;
                command = new SqlCommand(sql, conn);
                datareader = command.ExecuteReader();

                while (datareader.Read())
                {
                    plser = (long)datareader["PlanSetupSer"];
                }
                conn.Close();

                if (plser == -1)
                {
                    MessageBox.Show("Error: Could not find this plan in the database!");
                }

                conn.Open();
                sql = "USE variansystem SELECT RadiationSer FROM dbo.Radiation WHERE PlanSetupSer = " + plser;
                command = new SqlCommand(sql, conn);
                datareader = command.ExecuteReader();

                while (datareader.Read())
                {
                    rser = (long)datareader["RadiationSer"];
                }
                conn.Close();

                if (rser == -1)
                {
                    MessageBox.Show("Error: Could not find beam " + beamId + " in the database!");
                }

                conn.Open();
                sql = "USE variansystem SELECT GantryRtn, GantryRtnDirection, StopAngle FROM dbo.ExternalField WHERE RadiationSer = " + rser;
                command = new SqlCommand(sql, conn);
                datareader = command.ExecuteReader();

                //MessageBox.Show("Trig");

                while (datareader.Read())
                {
                    GantryStartAngle = Convert.ToDouble(datareader["GantryRtn"]);

                    object EA = (object)datareader["StopAngle"];

                    if(EA is DBNull)
                    {
                        GantryEndAngle = -1.0;
                    }
                    else
                    {
                        GantryEndAngle = Convert.ToDouble(EA);
                    }

                    ArcDirection = (string)datareader["GantryRtnDirection"];
                }
                conn.Close();

                if (GantryStartAngle == -1.0)
                {
                    MessageBox.Show("Error: Could not find the gantry rotation information for beam " + beamId + " in the database!");
                }

                if(GantryEndAngle == null)
                {
                    GantryEndAngle = -1.0;
                }


              //  MessageBox.Show("Arc Direction: " + ArcDirection);
                 
              //  MessageBox.Show("Gantry Start Angle: " + GantryStartAngle);
                
              //  MessageBox.Show("Gantry End Angle: " + GantryEndAngle);

                // The standard output of the console has been redirected to the main collision check program that this calls, so the lines below are the output of this program.
                // The collision check program starts this program as a separate process on the computer and then saves the output of the process as a streamreader that it passes on to the rest of the collisioncheck program
                Console.WriteLine(ArcDirection);
                Console.WriteLine(GantryStartAngle);
                Console.WriteLine(GantryEndAngle);
               
            }
            catch (Exception e)
            {
                MessageBox.Show("Database querying Error  - \n\n" + e.ToString() + "\n\n" + " Stack Trace: \n\n " + e.StackTrace);
            }
        }
    }
}
