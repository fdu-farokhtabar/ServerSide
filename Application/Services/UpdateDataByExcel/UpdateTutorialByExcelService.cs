using Application.Data;
using Application.Services.UpdateDataByExcel.Helper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace Application.Services.UpdateDataByExcel
{
    using Application.SeedWork;
    using Domain.Entity;
    using System.Text.RegularExpressions;

    public class UpdateTutorialByExcelService
    {        
        public async Task Update(Stream stream)
        {
            if (stream is null)
                throw new Exception("Please upload correct excel file.");
            List<Tutorial> Tutorials = new();            
            try
            {
                var Tables = UpdateByExcelHelper.ReadExcel(stream);
                if (Tables?.Count > 0 && Tables[0].Rows?.Count > 0)
                {
                    for (int i = 0; i < Tables[0].Rows.Count; i++)
                    {
                        var Row = Tables[0].Rows[i];
                        Tutorial newTutorial = CreateTutorial(Tables, Row);
                        Tutorials.Add(newTutorial);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Tutorial, there are some errors during reading data from excel.[" + ex.Message + "]");
            }
            await UpdateDatabase(Tutorials);
        }

        private Tutorial CreateTutorial(DataTableCollection Tables, DataRow Row)
        {
            return new()
            {
                Title = Row["Title"].ToString().Trim().Length>100 ? Row["Title"].ToString().Trim()[..100] : Row["Title"].ToString().Trim(),
                Abstract = Row["Abstract"].ToString().Trim().Length> 230 ?  Row["Abstract"].ToString().Trim()[..230] : Row["Abstract"].ToString().Trim(),
                Description = Row["Description"].ToString().Trim(),
                VideoUrls = CreateJsonForData("VideoUrls", Tables[0].Columns, Row),
                ImageUrls = CreateJsonForData("ImageUrls", Tables[0].Columns, Row),
                Roles = ConvertStringWithbracketsToArrayString(Row["Roles"].ToString().Trim())
            };
        }
        private static async Task UpdateDatabase(List<Tutorial> tutorials)
        {
            try
            {
                using var Db = new Context();
                using var Trans = await Db.Database.BeginTransactionAsync();
                try
                {
                    Db.Database.ExecuteSqlRaw("DELETE FROM \"Tutorial\"");
                    Db.Tutorials.AddRange(tutorials);
                    await Db.SaveChangesAsync();
                    Trans.Commit();
                }
                catch (Exception Ex)
                {
                    Trans.Rollback();
                    throw new Exception("Cannot update database Error = [" + Ex.Message + "]");
                }
            }
            catch(Exception Ex)
            {                
                throw new Exception("Cannot connect To the database Error = [" + Ex.Message + "]");
            }
        }

        private string[] CreateJsonForData(string ColStartWith, DataColumnCollection Columns, DataRow Row)
        {
            List<string> list = new();
            foreach (var Column in Columns)
            {
                string ColumnName = Column.ToString();
                if (ColumnName.StartsWith(ColStartWith, StringComparison.OrdinalIgnoreCase))
                    list.Add(Row[ColumnName].ToString().Trim());
            }
            return list?.ToArray();
        }
        public static string[] ConvertStringWithbracketsToArrayString(string Value)
        {
            List<string> Result = new List<string>();
            if (!string.IsNullOrWhiteSpace(Value))
            {
                var Matches = Regex.Matches(Value, @"(\[[^\[\]]*\])");
                if (Matches?.Count > 0)
                {
                    foreach (var Match in Matches)
                    {
                        string Expression = Match?.ToString();
                        if (!string.IsNullOrWhiteSpace(Expression))
                        {
                            Expression = Expression.Replace("[", "").Replace("]", "");
                            if (!string.IsNullOrWhiteSpace(Expression))
                                Result.Add(Expression);
                        }
                    }
                }
            }
            return Result.Count > 0 ? Result.ToArray() : null;
        }

    }
}
