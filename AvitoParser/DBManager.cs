using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;

namespace AvitoParser
{
    public static class DBManager
    {
        static Type ThisType = typeof(DBManager);

        public enum QueryActions
        {
            CreateTable,
            InsertRows
        };

        static string
            Table = "Resumes",
            Id = "Id",
            AvitoId = "AvitoId",
            Price = "Price",
            Experience = "Experience",
            Age = "Age",
            RemovalReady = "RemovalReady",
            Url = "Url",
            Address = "Address",
            Education = "Education",
            Position = "Position",
            Sex = "Sex",
            ActionSphere = "ActionSphere",
            WorkingSchedule = "WorkingSchedule",
            Description = "Description",
            Citizenship = "Citizenship",
            BusinessTripReady = "BusinessTripReady",
            CreatingDate = "CreatingDate";

        static string DbName = @"C:\AvitoResumeDB.db";
        static string Comma = ", ";
        static string TextNull = " TEXT NULL";
        static string TextNotNull = " TEXT NOT NULL";
        static string IntNull = " INTEGER NULL";
        static string IntNotNull = " INTEGER NOT NULL";


        static string Columns =
                     AvitoId + Comma
                   + Price + Comma
                   + Experience + Comma
                   + Age + Comma
                   + RemovalReady + Comma
                   + Url + Comma
                   + Address + Comma
                   + Education + Comma
                   + Position + Comma
                   + Sex + Comma
                   + ActionSphere + Comma
                   + WorkingSchedule + Comma
                   + Description + Comma
                   + Citizenship + Comma
                   + BusinessTripReady + Comma
                   + CreatingDate;

        static string SqlInsertRowsPart1 = "INSERT INTO " + Table + " ("
                   + Columns
                   + ")"
                   + " VALUES (";

        static string SqlCreateTableResumes =
                    "CREATE TABLE IF NOT EXISTS " + Table + " ("
                    + Id + IntNotNull + " PRIMARY KEY AUTOINCREMENT, " 
                    + AvitoId + IntNotNull + Comma
                    + Price + IntNotNull + Comma
                    + Experience + IntNull + Comma
                    + Age + IntNull + Comma
                    + RemovalReady + IntNotNull + " CHECK (RemovalReady IN (0,1))" + Comma
                    + Url + TextNull + Comma
                    + Address + TextNull + Comma
                    + Education + TextNull + Comma
                    + Position + TextNull + Comma
                    + Sex + TextNull + Comma
                    + ActionSphere + TextNull + Comma
                    + WorkingSchedule + TextNull + Comma
                    + Description + TextNull + Comma
                    + Citizenship + TextNull + Comma
                    + BusinessTripReady + TextNull + Comma
                    + CreatingDate + TextNotNull + ");";

        static string SqlSelectRows = "SELECT * FROM " + Table + ";";

        public static bool ExecuteSqlQuery(QueryActions queryAction, Candidate candidate = null)
        {
            SQLiteCommand command;
            int rowsAffected = 0;
            string sqlQuery = String.Empty;


            switch (queryAction)
            {
                case QueryActions.CreateTable:
                    sqlQuery = SqlCreateTableResumes;
                    break;
                case QueryActions.InsertRows:
                    if (candidate != null)
                        sqlQuery = GetSqlInsertQuery(candidate);
                    break;
            }

            try
            {
                if (!System.IO.File.Exists(DbName))
                {
                    SQLiteConnection.CreateFile(DbName);
                }

                if (!String.IsNullOrEmpty(sqlQuery))
                {
                    using (SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0}", DbName)))
                    {
                        connection.Open();
                        command = new SQLiteCommand(sqlQuery, connection);
                        rowsAffected = command.ExecuteNonQuery();
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.For(ThisType).Error(queryAction.ToString() + " was not processed.", ex);
            }

            if (rowsAffected > 0)
                return true;
            else
                return false;
        }


        private static string GetSqlInsertQuery(Candidate candidate)
        {
            string sqlInsertRows = String.Empty;

            int isRemovalReady = candidate.RemovalReady ? 1 : 0;
            sqlInsertRows = SqlInsertRowsPart1 + String.Format("{0}, {1}, {2}, {3}, {4}, '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}', '{15}')", candidate.AvitoId, candidate.Salary, candidate.Experience, candidate.Age, isRemovalReady, candidate.Url, candidate.Address, candidate.Education, candidate.Position, candidate.Sex, candidate.ActionSphere, candidate.WorkingSchedule, candidate.Description, candidate.Citizenship, candidate.BusinessTripReady, candidate.CreatingDate.ToString());

            return sqlInsertRows;
        }



        internal static void CreateTableIfNotExists()
        {
            DBManager.ExecuteSqlQuery(DBManager.QueryActions.CreateTable);
        }


        internal static void SaveCandidatesToDB(List<Candidate> candidates)
        {
            bool success = false;

            foreach (Candidate candidate in candidates)
            {
                try
                {
                    success = DBManager.ExecuteSqlQuery(DBManager.QueryActions.InsertRows, candidate);

                    if (!success)
                        throw new Exception();
                }
                catch (Exception ex)
                {
                    Log.For(ThisType).Error("Candidate №: " + candidate.AvitoId + " was not inserted into DB.", ex);
                }

            }
        }



        internal static List<Candidate> GetCandidatesFromDB()
        {
            DateTime date;
            bool parsed = false;
            Candidate candidate;
            List<Candidate> candidates = new List<Candidate>();

            Log.For(ThisType).Info("START getting candidates from DB.");

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0}", DbName)))
                {
                    connection.Open();
                    SQLiteCommand command = new SQLiteCommand(SqlSelectRows, connection);
                    SQLiteDataReader reader = command.ExecuteReader();


                    foreach (DbDataRecord record in reader)
                    {
                        candidate = new Candidate();
                        candidate.Id = Int32.Parse(record[Id].ToString());
                        candidate.AvitoId = Int32.Parse(record[AvitoId].ToString());
                        candidate.Salary = Int32.Parse(record[Price].ToString());
                        candidate.Experience = Int32.Parse(record[Experience].ToString());
                        candidate.Age = Int32.Parse(record[Age].ToString());
                        candidate.RemovalReady = Int32.Parse(record[RemovalReady].ToString()) == 1 ? true : false;
                        candidate.Url = record[Url].ToString();
                        candidate.Address = record[Address].ToString();
                        candidate.Education = record[Education].ToString();
                        candidate.Position = record[Position].ToString();
                        candidate.Sex = record[Sex].ToString();
                        candidate.ActionSphere = record[ActionSphere].ToString();
                        candidate.WorkingSchedule = record[WorkingSchedule].ToString();
                        candidate.Description = record[Description].ToString();
                        candidate.Citizenship = record[Citizenship].ToString();
                        candidate.BusinessTripReady = record[BusinessTripReady].ToString();

                        parsed = DateTime.TryParse(record[CreatingDate].ToString(), out date);

                        if (parsed)
                            candidate.CreatingDate = date;
                        else
                            candidate.CreatingDate = null;

                        candidates.Add(candidate);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.For(ThisType).Error("Error while getting candidates from DB", ex);                
            }

            Log.For(ThisType).Info("FINISH getting candidates from DB.");

            return candidates;
        }



    }
}